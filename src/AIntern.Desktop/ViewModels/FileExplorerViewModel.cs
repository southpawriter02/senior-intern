namespace AIntern.Desktop.ViewModels;

using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;

/// <summary>
/// ViewModel for the file explorer sidebar.
/// Manages workspace state, tree loading, file operations, and filtering.
/// </summary>
/// <remarks>Added in v0.3.2b.</remarks>
public partial class FileExplorerViewModel : ViewModelBase, IFileTreeItemParent, IDisposable
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IFileSystemService _fileSystemService;
    private readonly ISettingsService _settingsService;
    private readonly IStorageProvider? _storageProvider;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<FileExplorerViewModel> _logger;
    private readonly Timer _filterDebounceTimer;

    private string _pendingFilter = string.Empty;
    private IReadOnlyList<string> _ignorePatterns = [];
    private bool _disposed;

    #region Observable Properties

    /// <summary>Root items in the tree (workspace root contents).</summary>
    public ObservableCollection<FileTreeItemViewModel> RootItems { get; } = [];

    /// <summary>Currently selected item in the tree.</summary>
    [ObservableProperty]
    private FileTreeItemViewModel? _selectedItem;

    /// <summary>Whether a workspace is currently open.</summary>
    [ObservableProperty]
    private bool _hasWorkspace;

    /// <summary>Display name of the current workspace.</summary>
    [ObservableProperty]
    private string _workspaceName = string.Empty;

    /// <summary>Root path of the current workspace.</summary>
    [ObservableProperty]
    private string _workspacePath = string.Empty;

    /// <summary>Whether tree is currently loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Current filter text.</summary>
    [ObservableProperty]
    private string _searchFilter = string.Empty;

    /// <summary>Whether filter is currently being applied.</summary>
    [ObservableProperty]
    private bool _isFiltering;

    /// <summary>Error message to display (if any).</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Number of items matching current filter.</summary>
    [ObservableProperty]
    private int _filteredItemCount;

    #endregion

    #region Events

    /// <summary>Raised when a file should be opened in the editor.</summary>
    public event EventHandler<FileOpenRequestedEventArgs>? FileOpenRequested;

    /// <summary>Raised when a file should be attached to chat context.</summary>
    public event EventHandler<FileAttachRequestedEventArgs>? FileAttachRequested;

    /// <summary>Raised when delete confirmation is needed.</summary>
    public event EventHandler<DeleteConfirmationEventArgs>? DeleteConfirmationRequested;

    #endregion

    #region Constructor

    public FileExplorerViewModel(
        IWorkspaceService workspaceService,
        IFileSystemService fileSystemService,
        ISettingsService settingsService,
        IStorageProvider? storageProvider,
        IDispatcher dispatcher,
        ILogger<FileExplorerViewModel> logger)
    {
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _storageProvider = storageProvider; // Null in tests
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("FileExplorerViewModel created");

        // Filter debounce timer (200ms)
        _filterDebounceTimer = new Timer(200) { AutoReset = false };
        _filterDebounceTimer.Elapsed += OnFilterDebounceElapsed;

        // Subscribe to workspace events
        _workspaceService.WorkspaceChanged += OnWorkspaceChanged;

        // Initialize from current workspace if already open
        if (_workspaceService.CurrentWorkspace != null)
        {
            _ = LoadWorkspaceAsync(_workspaceService.CurrentWorkspace);
        }
    }

    #endregion

    #region Filter Handling

    partial void OnSearchFilterChanged(string value)
    {
        _pendingFilter = value;
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Start();
    }

    private async void OnFilterDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        await _dispatcher.InvokeAsync(() => ApplyFilter(_pendingFilter));
    }

    private void ApplyFilter(string filter)
    {
        var sw = Stopwatch.StartNew();
        IsFiltering = true;
        var matchCount = 0;

        try
        {
            foreach (var item in RootItems)
            {
                if (item.ApplyFilter(filter))
                {
                    matchCount++;
                }
            }

            FilteredItemCount = matchCount;
            _logger.LogDebug("Applied filter '{Filter}' in {ElapsedMs}ms, {Count} matches",
                filter, sw.ElapsedMilliseconds, matchCount);
        }
        finally
        {
            IsFiltering = false;
        }
    }

    /// <summary>Clears the current filter.</summary>
    [RelayCommand]
    private void ClearFilter()
    {
        SearchFilter = string.Empty;
        foreach (var item in RootItems)
        {
            item.ClearFilter();
        }
    }

    #endregion

    #region Workspace Commands

    /// <summary>Opens a folder selection dialog to choose a workspace.</summary>
    [RelayCommand]
    private async Task OpenWorkspaceAsync()
    {
        if (_storageProvider == null)
        {
            _logger.LogWarning("Storage provider not available for folder picker");
            return;
        }

        try
        {
            var folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Open Folder",
                AllowMultiple = false
            });

            if (folders.Count == 0)
                return;

            var folder = folders[0];
            var path = folder.Path.LocalPath;

            _logger.LogInformation("Opening workspace: {Path}", path);
            await _workspaceService.OpenWorkspaceAsync(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open workspace");
            ErrorMessage = $"Failed to open folder: {ex.Message}";
        }
    }

    /// <summary>Closes the current workspace.</summary>
    [RelayCommand]
    private async Task CloseWorkspaceAsync()
    {
        _logger.LogInformation("Closing workspace");
        await _workspaceService.CloseWorkspaceAsync();
    }

    /// <summary>Refreshes the entire tree.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_workspaceService.CurrentWorkspace == null)
            return;

        _logger.LogInformation("Refreshing workspace");
        await LoadWorkspaceAsync(_workspaceService.CurrentWorkspace);
    }

    #endregion

    #region File/Folder Operation Commands

    /// <summary>Opens a file in the editor.</summary>
    [RelayCommand]
    private void OpenFile(FileTreeItemViewModel? item)
    {
        if (item == null || !item.IsFile)
            return;

        _logger.LogDebug("Opening file: {Path}", item.Path);
        FileOpenRequested?.Invoke(this, new FileOpenRequestedEventArgs(item.Path));
    }

    /// <summary>
    /// Opens a folder as workspace by path (v0.3.5f).
    /// Used for drag-drop folder opening.
    /// </summary>
    public void OpenFolderByPath(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return;

        _logger.LogDebug("[DROP] Opening folder as workspace: {Path}", folderPath);
        _ = _workspaceService.OpenWorkspaceAsync(folderPath);
    }

    /// <summary>
    /// Requests a file to be opened in the editor (v0.3.5f).
    /// Used for drag-drop file opening.
    /// </summary>
    public void RequestOpenFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;

        _logger.LogDebug("[DROP] Opening file: {Path}", filePath);
        FileOpenRequested?.Invoke(this, new FileOpenRequestedEventArgs(filePath));
    }

    /// <summary>Expands a folder.</summary>
    [RelayCommand]
    private async Task ExpandFolderAsync(FileTreeItemViewModel? item)
    {
        if (item == null || !item.IsDirectory)
            return;

        item.IsExpanded = true;
        await item.LoadChildrenAsync();
    }

    /// <summary>Collapses a folder.</summary>
    [RelayCommand]
    private void CollapseFolder(FileTreeItemViewModel? item)
    {
        if (item == null || !item.IsDirectory)
            return;

        item.IsExpanded = false;
    }

    /// <summary>Creates a new file in the specified folder (or workspace root).</summary>
    [RelayCommand]
    private async Task NewFileAsync(FileTreeItemViewModel? parentFolder)
    {
        var targetPath = GetTargetDirectoryPath(parentFolder);
        if (targetPath == null) return;

        try
        {
            // Generate unique name
            var baseName = "untitled";
            var extension = ".txt";
            var name = baseName + extension;
            var counter = 1;

            while (File.Exists(Path.Combine(targetPath, name)))
            {
                name = $"{baseName}-{counter++}{extension}";
            }

            var filePath = Path.Combine(targetPath, name);
            var newItem = await _fileSystemService.CreateFileAsync(filePath);

            // Add to tree and start rename
            var viewModel = FileTreeItemViewModel.FromFileSystemItem(newItem, this,
                parentFolder?.Depth + 1 ?? 0);

            if (parentFolder != null)
            {
                await EnsureExpandedAsync(parentFolder);
                parentFolder.Children.Add(viewModel);
            }
            else
            {
                RootItems.Add(viewModel);
            }

            SelectedItem = viewModel;
            viewModel.BeginRename();

            _logger.LogInformation("Created new file: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create file");
            ErrorMessage = $"Failed to create file: {ex.Message}";
        }
    }

    /// <summary>Creates a new folder in the specified location.</summary>
    [RelayCommand]
    private async Task NewFolderAsync(FileTreeItemViewModel? parentFolder)
    {
        var targetPath = GetTargetDirectoryPath(parentFolder);
        if (targetPath == null) return;

        try
        {
            // Generate unique name
            var baseName = "New Folder";
            var name = baseName;
            var counter = 1;

            while (Directory.Exists(Path.Combine(targetPath, name)))
            {
                name = $"{baseName} {counter++}";
            }

            var folderPath = Path.Combine(targetPath, name);
            var newItem = await _fileSystemService.CreateDirectoryAsync(folderPath);

            // Add to tree and start rename
            var viewModel = FileTreeItemViewModel.FromFileSystemItem(newItem, this,
                parentFolder?.Depth + 1 ?? 0);

            if (parentFolder != null)
            {
                await EnsureExpandedAsync(parentFolder);
                // Insert folders before files (maintain sort order)
                var insertIndex = parentFolder.Children.TakeWhile(c => c.IsDirectory).Count();
                parentFolder.Children.Insert(insertIndex, viewModel);
            }
            else
            {
                var insertIndex = RootItems.TakeWhile(c => c.IsDirectory).Count();
                RootItems.Insert(insertIndex, viewModel);
            }

            SelectedItem = viewModel;
            viewModel.BeginRename();

            _logger.LogInformation("Created new folder: {Path}", folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder");
            ErrorMessage = $"Failed to create folder: {ex.Message}";
        }
    }

    /// <summary>Starts rename mode for the selected item.</summary>
    [RelayCommand]
    private void Rename(FileTreeItemViewModel? item)
    {
        item ??= SelectedItem;
        item?.BeginRename();
    }

    /// <summary>Deletes the selected item.</summary>
    [RelayCommand]
    private async Task DeleteAsync(FileTreeItemViewModel? item)
    {
        item ??= SelectedItem;
        if (item == null) return;

        await DeleteItemInternalAsync(item);
    }

    /// <summary>Actually deletes an item (called after confirmation).</summary>
    internal async Task DeleteItemInternalAsync(FileTreeItemViewModel item)
    {
        try
        {
            if (item.IsDirectory)
            {
                await _fileSystemService.DeleteDirectoryAsync(item.Path);
            }
            else
            {
                await _fileSystemService.DeleteFileAsync(item.Path);
            }

            RemoveItemFromTree(item);
            _logger.LogInformation("Deleted: {Path}", item.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {Path}", item.Path);
            ErrorMessage = $"Failed to delete: {ex.Message}";
        }
    }

    #endregion

    #region Path Commands

    /// <summary>Copies the absolute path to clipboard.</summary>
    [RelayCommand]
    private async Task CopyPathAsync(FileTreeItemViewModel? item)
    {
        item ??= SelectedItem;
        if (item == null) return;

        await CopyToClipboardAsync(item.Path);
        _logger.LogDebug("Copied path: {Path}", item.Path);
    }

    /// <summary>Copies the relative path to clipboard.</summary>
    [RelayCommand]
    private async Task CopyRelativePathAsync(FileTreeItemViewModel? item)
    {
        item ??= SelectedItem;
        if (item == null || _workspaceService.CurrentWorkspace == null) return;

        var relativePath = _workspaceService.CurrentWorkspace.GetRelativePath(item.Path);
        await CopyToClipboardAsync(relativePath);
        _logger.LogDebug("Copied relative path: {Path}", relativePath);
    }

    /// <summary>Opens the item's location in the system file manager.</summary>
    [RelayCommand]
    private void RevealInFinder(FileTreeItemViewModel? item)
    {
        item ??= SelectedItem;
        if (item == null) return;

        var path = item.IsDirectory ? item.Path : Path.GetDirectoryName(item.Path);
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", $"-R \"{item.Path}\"");
            }
            else if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", $"/select,\"{item.Path}\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", path);
            }

            _logger.LogDebug("Revealed in finder: {Path}", item.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reveal in finder: {Path}", item.Path);
        }
    }

    private async Task CopyToClipboardAsync(string text)
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy to clipboard");
        }
    }

    #endregion

    #region Context Attachment

    /// <summary>Attaches the selected file to the chat context.</summary>
    [RelayCommand]
    private void AttachToContext(FileTreeItemViewModel? item)
    {
        item ??= SelectedItem;
        if (item == null || !item.IsFile) return;

        _logger.LogDebug("Attaching to context: {Path}", item.Path);
        FileAttachRequested?.Invoke(this, new FileAttachRequestedEventArgs(item.Path));
    }

    #endregion

    #region Tree Loading

    private async Task LoadWorkspaceAsync(Workspace workspace)
    {
        var sw = Stopwatch.StartNew();
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            HasWorkspace = true;
            WorkspaceName = workspace.DisplayName;
            WorkspacePath = workspace.RootPath;

            // Load ignore patterns
            _ignorePatterns = workspace.GitIgnorePatterns;

            // Load settings
            var settings = _settingsService.CurrentSettings;

            // Load root contents
            var contents = await _fileSystemService.GetDirectoryContentsAsync(
                workspace.RootPath,
                includeHidden: settings.ShowHiddenFiles);

            // Filter by ignore patterns
            var filteredContents = contents
                .Where(item => !_fileSystemService.ShouldIgnore(
                    item.Path,
                    workspace.RootPath,
                    _ignorePatterns))
                .ToList();

            // Create ViewModels
            await _dispatcher.InvokeAsync(() =>
            {
                RootItems.Clear();
                foreach (var item in filteredContents)
                {
                    var viewModel = FileTreeItemViewModel.FromFileSystemItem(item, this, depth: 0);
                    RootItems.Add(viewModel);
                }
            });

            // Restore expanded folders
            await RestoreExpandedFoldersAsync(workspace.ExpandedFolders);

            _logger.LogInformation("Loaded workspace: {Name} with {Count} items in {ElapsedMs}ms",
                workspace.DisplayName, RootItems.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workspace");
            ErrorMessage = $"Failed to load workspace: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RestoreExpandedFoldersAsync(IReadOnlyList<string> expandedPaths)
    {
        if (expandedPaths.Count == 0 || _workspaceService.CurrentWorkspace == null)
            return;

        foreach (var relativePath in expandedPaths)
        {
            var absolutePath = _workspaceService.CurrentWorkspace.GetAbsolutePath(relativePath);
            var item = FindItemByPath(absolutePath);

            if (item != null && item.IsDirectory)
            {
                item.IsExpanded = true;
                await item.LoadChildrenAsync();
            }
        }
    }

    #endregion

    #region IFileTreeItemParent Implementation

    /// <summary>Loads children for a directory item. Called by FileTreeItemViewModel.</summary>
    public async Task<IReadOnlyList<FileTreeItemViewModel>> LoadChildrenForItemAsync(FileTreeItemViewModel parent)
    {
        var settings = _settingsService.CurrentSettings;

        var contents = await _fileSystemService.GetDirectoryContentsAsync(
            parent.Path,
            includeHidden: settings.ShowHiddenFiles);

        // Filter by ignore patterns
        var workspaceRoot = _workspaceService.CurrentWorkspace?.RootPath ?? parent.Path;
        var filteredContents = contents
            .Where(item => !_fileSystemService.ShouldIgnore(item.Path, workspaceRoot, _ignorePatterns))
            .ToList();

        return filteredContents
            .Select(item => FileTreeItemViewModel.FromFileSystemItem(item, this, parent.Depth + 1))
            .ToList();
    }

    /// <summary>Renames an item (called from FileTreeItemViewModel).</summary>
    public async Task RenameItemAsync(FileTreeItemViewModel item, string newName)
    {
        try
        {
            var renamed = await _fileSystemService.RenameAsync(item.Path, newName);
            item.Name = renamed.Name;
            item.Path = renamed.Path;

            _logger.LogInformation("Renamed to: {Path}", renamed.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename {Path} to {NewName}", item.Path, newName);
            throw; // Rethrow for item to handle
        }
    }

    /// <summary>Called when an item's expansion state changes.</summary>
    public void OnItemExpansionChanged(FileTreeItemViewModel item)
    {
        if (_workspaceService.CurrentWorkspace == null)
            return;

        var expandedPaths = CollectExpandedPaths(RootItems)
            .Select(p => _workspaceService.CurrentWorkspace.GetRelativePath(p))
            .ToList();

        _workspaceService.UpdateExpandedFolders(expandedPaths);
    }

    /// <summary>Displays an error message to the user.</summary>
    public void ShowError(string message)
    {
        ErrorMessage = message;
        _logger.LogWarning("Error shown: {Message}", message);
    }

    /// <summary>Gets the relative path from workspace root.</summary>
    public string GetRelativePath(string absolutePath)
    {
        return _workspaceService.CurrentWorkspace?.GetRelativePath(absolutePath) ?? absolutePath;
    }

    #endregion

    #region State Management / Helpers

    private IEnumerable<string> CollectExpandedPaths(IEnumerable<FileTreeItemViewModel> items)
    {
        foreach (var item in items)
        {
            if (item.IsDirectory && item.IsExpanded)
            {
                yield return item.Path;

                foreach (var childPath in CollectExpandedPaths(item.Children))
                {
                    yield return childPath;
                }
            }
        }
    }

    private string? GetTargetDirectoryPath(FileTreeItemViewModel? item)
    {
        if (!HasWorkspace)
            return null;

        if (item == null)
            return WorkspacePath;

        if (item.IsDirectory)
            return item.Path;

        return Path.GetDirectoryName(item.Path);
    }

    private async Task EnsureExpandedAsync(FileTreeItemViewModel folder)
    {
        if (!folder.IsExpanded)
        {
            folder.IsExpanded = true;
            await folder.LoadChildrenAsync();
        }
    }

    /// <summary>Finds an item by its absolute path.</summary>
    public FileTreeItemViewModel? FindItemByPath(string absolutePath)
    {
        return FindItemByPath(RootItems, absolutePath);
    }

    private FileTreeItemViewModel? FindItemByPath(
        IEnumerable<FileTreeItemViewModel> items,
        string absolutePath)
    {
        foreach (var item in items)
        {
            if (string.Equals(item.Path, absolutePath, StringComparison.Ordinal))
                return item;

            if (item.IsDirectory && item.ChildrenLoaded)
            {
                var found = FindItemByPath(item.Children, absolutePath);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private void RemoveItemFromTree(FileTreeItemViewModel item)
    {
        if (RootItems.Remove(item))
            return;

        // Search in children
        foreach (var root in RootItems)
        {
            if (RemoveItemFromChildren(root, item))
                return;
        }
    }

    private bool RemoveItemFromChildren(FileTreeItemViewModel parent, FileTreeItemViewModel item)
    {
        if (parent.Children.Remove(item))
            return true;

        foreach (var child in parent.Children.Where(c => c.IsDirectory))
        {
            if (RemoveItemFromChildren(child, item))
                return true;
        }

        return false;
    }

    #endregion

    #region Event Handlers

    private async void OnWorkspaceChanged(object? sender, WorkspaceChangedEventArgs e)
    {
        await _dispatcher.InvokeAsync(async () =>
        {
            switch (e.ChangeType)
            {
                case WorkspaceChangeType.Closed:
                    HasWorkspace = false;
                    WorkspaceName = string.Empty;
                    WorkspacePath = string.Empty;
                    RootItems.Clear();
                    SearchFilter = string.Empty;
                    _logger.LogInformation("Workspace closed");
                    break;

                case WorkspaceChangeType.Opened:
                case WorkspaceChangeType.Refreshed:
                    if (e.CurrentWorkspace != null)
                    {
                        await LoadWorkspaceAsync(e.CurrentWorkspace);
                    }
                    break;
            }
        });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
            return;

        _filterDebounceTimer.Elapsed -= OnFilterDebounceElapsed;
        _filterDebounceTimer.Dispose();

        _workspaceService.WorkspaceChanged -= OnWorkspaceChanged;

        _disposed = true;
        _logger.LogDebug("FileExplorerViewModel disposed");
    }

    #endregion
}
