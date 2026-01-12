using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the file explorer sidebar.
/// </summary>
public partial class FileExplorerViewModel : ViewModelBase, IFileExplorerParent, IDisposable
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IFileSystemService _fileSystemService;
    private readonly ISettingsService _settingsService;
    private readonly IStorageProvider? _storageProvider;
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

    /// <summary>Number of items matching current filter.</summary>
    [ObservableProperty]
    private int _filteredItemCount;

    #endregion

    #region Events

    /// <summary>Raised when a file should be opened in the editor.</summary>
    public event EventHandler<FileOpenRequestedEventArgs>? FileOpenRequested;

    /// <summary>Raised when a file should be attached to chat context.</summary>
    public event EventHandler<FileAttachRequestedEventArgs>? FileAttachRequested;

    #endregion

    #region Constructor

    public FileExplorerViewModel(
        IWorkspaceService workspaceService,
        IFileSystemService fileSystemService,
        ISettingsService settingsService,
        IStorageProvider? storageProvider,
        ILogger<FileExplorerViewModel> logger)
    {
        _workspaceService = workspaceService;
        _fileSystemService = fileSystemService;
        _settingsService = settingsService;
        _storageProvider = storageProvider;
        _logger = logger;

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
        await Dispatcher.UIThread.InvokeAsync(() => ApplyFilter(_pendingFilter));
    }

    private void ApplyFilter(string filter)
    {
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
    }

    #endregion

    #region Workspace Commands

    /// <summary>Opens a folder selection dialog to choose a workspace.</summary>
    [RelayCommand]
    private async Task OpenWorkspaceAsync()
    {
        if (_storageProvider == null) return;

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
        await _workspaceService.CloseWorkspaceAsync();
    }

    /// <summary>Refreshes the entire tree.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_workspaceService.CurrentWorkspace == null)
            return;

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

        FileOpenRequested?.Invoke(this, new FileOpenRequestedEventArgs(item.Path));
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

            // Create view model and add to tree
            var viewModel = FileTreeItemViewModel.FromFileSystemItem(newItem, this);

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

            // Create view model
            var viewModel = FileTreeItemViewModel.FromFileSystemItem(newItem, this);

            if (parentFolder != null)
            {
                await EnsureExpandedAsync(parentFolder);
                // Insert folders before files
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
    }

    /// <summary>Copies the relative path to clipboard.</summary>
    [RelayCommand]
    private async Task CopyRelativePathAsync(FileTreeItemViewModel? item)
    {
        item ??= SelectedItem;
        if (item == null || _workspaceService.CurrentWorkspace == null) return;

        var relativePath = _workspaceService.CurrentWorkspace.GetRelativePath(item.Path);
        await CopyToClipboardAsync(relativePath);
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
            var clipboard = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (clipboard?.MainWindow?.Clipboard != null)
            {
                await clipboard.MainWindow.Clipboard.SetTextAsync(text);
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

        FileAttachRequested?.Invoke(this, new FileAttachRequestedEventArgs(item.Path));
    }

    #endregion

    #region Tree Loading

    private async Task LoadWorkspaceAsync(Workspace workspace)
    {
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

            // Create ViewModels on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
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

            _logger.LogInformation("Loaded workspace: {Name} with {Count} root items",
                workspace.DisplayName, RootItems.Count);
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

    #region IFileExplorerParent Implementation

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
            throw;
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

    /// <summary>Gets relative path from workspace root.</summary>
    public string GetRelativePath(string absolutePath)
    {
        return _workspaceService.CurrentWorkspace?.GetRelativePath(absolutePath) ?? absolutePath;
    }

    /// <summary>Shows an error message.</summary>
    public void ShowError(string message)
    {
        ErrorMessage = message;
    }

    #endregion

    #region Helper Methods

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
        if (item == null)
            return _workspaceService.CurrentWorkspace?.RootPath;

        return item.IsDirectory ? item.Path : Path.GetDirectoryName(item.Path);
    }

    private async Task EnsureExpandedAsync(FileTreeItemViewModel folder)
    {
        if (!folder.IsExpanded)
        {
            folder.IsExpanded = true;
            await folder.LoadChildrenAsync();
        }
    }

    private FileTreeItemViewModel? FindItemByPath(string path)
    {
        return FindItemByPathRecursive(RootItems, path);
    }

    private FileTreeItemViewModel? FindItemByPathRecursive(
        IEnumerable<FileTreeItemViewModel> items,
        string path)
    {
        foreach (var item in items)
        {
            if (item.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                return item;

            if (item.IsDirectory && item.Children.Count > 0)
            {
                var found = FindItemByPathRecursive(item.Children, path);
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

        RemoveFromTreeRecursive(RootItems, item);
    }

    private bool RemoveFromTreeRecursive(
        IEnumerable<FileTreeItemViewModel> items,
        FileTreeItemViewModel target)
    {
        foreach (var item in items)
        {
            if (item.Children.Remove(target))
                return true;

            if (RemoveFromTreeRecursive(item.Children, target))
                return true;
        }
        return false;
    }

    #endregion

    #region Event Handlers

    private async void OnWorkspaceChanged(object? sender, WorkspaceChangedEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (e.ChangeType == WorkspaceChangeType.Closed)
            {
                HasWorkspace = false;
                WorkspaceName = string.Empty;
                WorkspacePath = string.Empty;
                RootItems.Clear();
                SearchFilter = string.Empty;
            }
            else if (e.CurrentWorkspace != null)
            {
                await LoadWorkspaceAsync(e.CurrentWorkspace);
            }
        });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _filterDebounceTimer.Dispose();
        _workspaceService.WorkspaceChanged -= OnWorkspaceChanged;
        GC.SuppressFinalize(this);
    }

    #endregion
}

#region Event Args

/// <summary>Event args when a file open is requested.</summary>
public sealed class FileOpenRequestedEventArgs : EventArgs
{
    public string FilePath { get; }

    public FileOpenRequestedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

/// <summary>Event args when file attach to context is requested.</summary>
public sealed class FileAttachRequestedEventArgs : EventArgs
{
    public string FilePath { get; }

    public FileAttachRequestedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

#endregion
