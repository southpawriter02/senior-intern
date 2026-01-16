namespace AIntern.Desktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for the editor panel managing multiple tabs and editor operations.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel orchestrates the code editor subsystem, providing:
/// </para>
/// <list type="bullet">
///   <item><description>Tab collection management (open, close, navigate)</description></item>
///   <item><description>File operations (save, save as, save all)</description></item>
///   <item><description>Editor commands (undo, redo, find, replace, go-to-line)</description></item>
///   <item><description>Unsaved changes prompt dialog flow</description></item>
/// </list>
/// <para>Added in v0.3.3b.</para>
/// </remarks>
public partial class EditorPanelViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly IFileSystemService _fileSystemService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<EditorPanelViewModel>? _logger;
    private int _untitledCounter = 1;
    private bool _disposed;

    #endregion

    #region Observable Properties

    /// <summary>Collection of open editor tabs.</summary>
    [ObservableProperty]
    private ObservableCollection<EditorTabViewModel> _tabs = new();

    /// <summary>Currently active/focused tab.</summary>
    [ObservableProperty]
    private EditorTabViewModel? _activeTab;

    #endregion

    #region Computed Properties

    /// <summary>Whether any tab has unsaved changes.</summary>
    public bool HasUnsavedChanges => Tabs.Any(t => t.IsDirty);

    /// <summary>Whether there are any open tabs.</summary>
    public bool HasOpenTabs => Tabs.Count > 0;

    /// <summary>Number of tabs with unsaved changes.</summary>
    public int UnsavedTabsCount => Tabs.Count(t => t.IsDirty);

    /// <summary>
    /// Whether there is currently a text selection in the active editor.
    /// Updated by the view when selection changes (v0.3.4f).
    /// </summary>
    [ObservableProperty]
    private bool _hasSelection;

    /// <summary>
    /// Updates the HasSelection property based on editor selection state.
    /// Called by the view when selection changes (v0.3.4f).
    /// </summary>
    /// <param name="hasSelection">True if there is a selection.</param>
    public void UpdateSelectionState(bool hasSelection)
    {
        HasSelection = hasSelection;
    }

    #endregion

    #region Events

    /// <summary>Raised when undo should be executed on the active editor.</summary>
    public event EventHandler? UndoRequested;

    /// <summary>Raised when redo should be executed on the active editor.</summary>
    public event EventHandler? RedoRequested;

    /// <summary>Raised when find panel should be opened.</summary>
    public event EventHandler? FindRequested;

    /// <summary>Raised when replace panel should be opened.</summary>
    public event EventHandler? ReplaceRequested;

    /// <summary>Raised when should navigate to a line number.</summary>
    public event EventHandler<int>? GoToLineRequested;

    /// <summary>Raised when file should be revealed in file explorer.</summary>
    public event EventHandler<string>? RevealInExplorerRequested;

    /// <summary>Raised when a tab is opened.</summary>
    public event EventHandler<EditorTabViewModel>? TabOpened;

    /// <summary>Raised when a tab is closed.</summary>
    public event EventHandler<EditorTabViewModel>? TabClosed;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new EditorPanelViewModel.
    /// </summary>
    /// <param name="fileSystemService">File system service for read/write.</param>
    /// <param name="dialogService">Dialog service for prompts.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public EditorPanelViewModel(
        IFileSystemService fileSystemService,
        IDialogService dialogService,
        ILogger<EditorPanelViewModel>? logger = null)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger;

        _logger?.LogDebug("[INIT] EditorPanelViewModel created");

        // Update computed properties when tabs change
        Tabs.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(HasOpenTabs));
            OnPropertyChanged(nameof(UnsavedTabsCount));
        };
    }

    partial void OnActiveTabChanged(EditorTabViewModel? oldValue, EditorTabViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsActive = false;
        if (newValue != null)
        {
            newValue.IsActive = true;
            _logger?.LogDebug("[TAB] Active tab changed to: {FileName}", newValue.FileName);
        }
    }

    #endregion

    #region File Operations

    /// <summary>Opens a file in a new tab or activates existing tab if already open.</summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <param name="ct">Cancellation token.</param>
    [RelayCommand]
    public async Task OpenFileAsync(string filePath, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ACTION] Opening file: {FilePath}", filePath);

        // Check if already open
        var existingTab = GetTabByPath(filePath);
        if (existingTab != null)
        {
            _logger?.LogDebug("[INFO] File already open, activating existing tab");
            ActiveTab = existingTab;
            return;
        }

        try
        {
            var content = await _fileSystemService.ReadFileAsync(filePath, ct);
            var tab = EditorTabViewModel.FromFile(filePath, content);

            Tabs.Add(tab);
            ActiveTab = tab;
            TabOpened?.Invoke(this, tab);

            _logger?.LogInformation("[INFO] File opened in {ElapsedMs}ms: {FileName}",
                sw.ElapsedMilliseconds, tab.FileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] Failed to open file: {FilePath}", filePath);
            await _dialogService.ShowErrorAsync(
                "Error Opening File",
                $"Could not open {Path.GetFileName(filePath)}: {ex.Message}",
                ct);
        }
    }

    /// <summary>Creates a new untitled file in a new tab.</summary>
    [RelayCommand]
    public void NewFile()
    {
        var fileName = $"Untitled-{_untitledCounter++}";
        _logger?.LogDebug("[ACTION] Creating new file: {FileName}", fileName);

        var tab = EditorTabViewModel.CreateNew(fileName);
        Tabs.Add(tab);
        ActiveTab = tab;
        TabOpened?.Invoke(this, tab);

        _logger?.LogInformation("[INFO] New file created: {FileName}", fileName);
    }

    /// <summary>Saves the active tab.</summary>
    [RelayCommand]
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (ActiveTab == null) return;
        await SaveTabAsync(ActiveTab, ct);
    }

    /// <summary>Saves the active tab with a new file name.</summary>
    [RelayCommand]
    public async Task SaveAsAsync(CancellationToken ct = default)
    {
        if (ActiveTab == null) return;
        await SaveTabAsAsync(ActiveTab, ct);
    }

    /// <summary>Saves all tabs with unsaved changes.</summary>
    [RelayCommand]
    public async Task SaveAllAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug("[ACTION] SaveAll - {Count} dirty tabs", UnsavedTabsCount);

        foreach (var tab in Tabs.Where(t => t.IsDirty).ToList())
        {
            if (!await SaveTabAsync(tab, ct)) break;
        }
    }

    private async Task<bool> SaveTabAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        if (tab.IsNewFile) return await SaveTabAsAsync(tab, ct);

        _logger?.LogDebug("[ACTION] Saving file: {FilePath}", tab.FilePath);

        try
        {
            await _fileSystemService.WriteFileAsync(tab.FilePath, tab.GetContent(), ct);
            tab.MarkAsSaved();
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(UnsavedTabsCount));

            _logger?.LogInformation("[INFO] File saved: {FileName}", tab.FileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] Failed to save file: {FilePath}", tab.FilePath);
            await _dialogService.ShowErrorAsync(
                "Error Saving File",
                $"Could not save {tab.FileName}: {ex.Message}",
                ct);
            return false;
        }
    }

    private async Task<bool> SaveTabAsAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        var suggestedName = tab.IsNewFile ? tab.FileName + ".txt" : tab.FileName;
        _logger?.LogDebug("[ACTION] SaveAs dialog for: {FileName}", suggestedName);

        var filePath = await _dialogService.ShowSaveDialogAsync(
            "Save File As",
            suggestedName,
            GetFileFilters(tab.Language),
            ct);

        if (string.IsNullOrEmpty(filePath))
        {
            _logger?.LogDebug("[INFO] SaveAs cancelled");
            return false;
        }

        try
        {
            await _fileSystemService.WriteFileAsync(filePath, tab.GetContent(), ct);
            tab.MarkAsSaved(filePath);
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(UnsavedTabsCount));

            _logger?.LogInformation("[INFO] File saved as: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] Failed to save file as: {FilePath}", filePath);
            await _dialogService.ShowErrorAsync(
                "Error Saving File",
                $"Could not save to {filePath}: {ex.Message}",
                ct);
            return false;
        }
    }

    #endregion

    #region Tab Management

    /// <summary>Activates a specific tab.</summary>
    [RelayCommand]
    public void ActivateTab(EditorTabViewModel tab)
    {
        if (Tabs.Contains(tab)) ActiveTab = tab;
    }

    /// <summary>Closes a specific tab, prompting for save if dirty.</summary>
    [RelayCommand]
    public async Task CloseTabAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        _logger?.LogDebug("[ACTION] Closing tab: {FileName}", tab.FileName);

        if (!await PromptSaveChangesAsync(tab, ct))
        {
            _logger?.LogDebug("[INFO] Close cancelled by user");
            return;
        }

        var index = Tabs.IndexOf(tab);
        var wasActive = ActiveTab == tab;

        tab.Dispose();
        Tabs.Remove(tab);
        TabClosed?.Invoke(this, tab);

        // Activate adjacent tab if this was active
        if (wasActive && Tabs.Count > 0)
        {
            var newIndex = Math.Min(index, Tabs.Count - 1);
            ActiveTab = Tabs[newIndex];
        }
        else if (Tabs.Count == 0)
        {
            ActiveTab = null;
        }

        _logger?.LogInformation("[INFO] Tab closed: {FileName}", tab.FileName);
    }

    /// <summary>Closes all tabs, prompting for save if any are dirty.</summary>
    [RelayCommand]
    public async Task CloseAllTabsAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug("[ACTION] CloseAll - {Count} tabs", Tabs.Count);

        for (int i = Tabs.Count - 1; i >= 0; i--)
        {
            var tab = Tabs[i];
            if (!await PromptSaveChangesAsync(tab, ct))
            {
                _logger?.LogDebug("[INFO] CloseAll cancelled at tab: {FileName}", tab.FileName);
                return;
            }

            tab.Dispose();
            Tabs.RemoveAt(i);
            TabClosed?.Invoke(this, tab);
        }
        ActiveTab = null;

        _logger?.LogInformation("[INFO] All tabs closed");
    }

    /// <summary>Closes all tabs except the specified one.</summary>
    [RelayCommand]
    public async Task CloseOtherTabsAsync(EditorTabViewModel keepTab, CancellationToken ct = default)
    {
        _logger?.LogDebug("[ACTION] CloseOthers - keeping: {FileName}", keepTab.FileName);

        var tabsToClose = Tabs.Where(t => t != keepTab).ToList();

        foreach (var tab in tabsToClose)
        {
            if (!await PromptSaveChangesAsync(tab, ct)) return;
            tab.Dispose();
            Tabs.Remove(tab);
            TabClosed?.Invoke(this, tab);
        }
        ActiveTab = keepTab;
    }

    /// <summary>Closes tabs to the right of the specified tab.</summary>
    [RelayCommand]
    public async Task CloseTabsToRightAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        var index = Tabs.IndexOf(tab);
        if (index < 0) return;

        _logger?.LogDebug("[ACTION] CloseToRight from index {Index}", index);

        for (int i = Tabs.Count - 1; i > index; i--)
        {
            var tabToClose = Tabs[i];
            if (!await PromptSaveChangesAsync(tabToClose, ct)) return;
            tabToClose.Dispose();
            Tabs.RemoveAt(i);
            TabClosed?.Invoke(this, tabToClose);
        }
    }

    /// <summary>Activates the next tab (wraps around).</summary>
    [RelayCommand]
    public void NextTab()
    {
        if (Tabs.Count < 2 || ActiveTab == null) return;

        var index = Tabs.IndexOf(ActiveTab);
        var nextIndex = (index + 1) % Tabs.Count;
        ActiveTab = Tabs[nextIndex];

        _logger?.LogDebug("[NAV] NextTab: {Index} -> {NextIndex}", index, nextIndex);
    }

    /// <summary>Activates the previous tab (wraps around).</summary>
    [RelayCommand]
    public void PreviousTab()
    {
        if (Tabs.Count < 2 || ActiveTab == null) return;

        var index = Tabs.IndexOf(ActiveTab);
        var prevIndex = (index - 1 + Tabs.Count) % Tabs.Count;
        ActiveTab = Tabs[prevIndex];

        _logger?.LogDebug("[NAV] PreviousTab: {Index} -> {PrevIndex}", index, prevIndex);
    }

    /// <summary>Reorders tabs via drag-drop.</summary>
    public void MoveTab(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Tabs.Count) return;
        if (toIndex < 0 || toIndex >= Tabs.Count) return;
        if (fromIndex == toIndex) return;

        var tab = Tabs[fromIndex];
        Tabs.RemoveAt(fromIndex);
        Tabs.Insert(toIndex, tab);

        _logger?.LogDebug("[MOVE] Tab moved: {From} -> {To}", fromIndex, toIndex);
    }

    #endregion

    #region Editor Commands

    /// <summary>Requests undo operation on active editor.</summary>
    [RelayCommand]
    public void Undo()
    {
        _logger?.LogDebug("[CMD] Undo requested");
        UndoRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Requests redo operation on active editor.</summary>
    [RelayCommand]
    public void Redo()
    {
        _logger?.LogDebug("[CMD] Redo requested");
        RedoRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Requests find panel to open.</summary>
    [RelayCommand]
    public void Find()
    {
        _logger?.LogDebug("[CMD] Find requested");
        FindRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Requests replace panel to open.</summary>
    [RelayCommand]
    public void Replace()
    {
        _logger?.LogDebug("[CMD] Replace requested");
        ReplaceRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Shows go-to-line dialog and navigates.</summary>
    [RelayCommand]
    public async Task GoToLineAsync()
    {
        if (ActiveTab == null) return;

        _logger?.LogDebug("[CMD] GoToLine dialog - max: {Max}, current: {Current}",
            ActiveTab.LineCount, ActiveTab.CaretLine);

        var lineNumber = await _dialogService.ShowGoToLineDialogAsync(
            ActiveTab.LineCount,
            ActiveTab.CaretLine);

        if (lineNumber.HasValue)
        {
            _logger?.LogDebug("[CMD] GoToLine: {Line}", lineNumber.Value);
            GoToLineRequested?.Invoke(this, lineNumber.Value);
        }
    }

    /// <summary>Requests file to be revealed in explorer.</summary>
    [RelayCommand]
    public void RevealInExplorer()
    {
        if (ActiveTab == null || ActiveTab.IsNewFile) return;

        _logger?.LogDebug("[CMD] RevealInExplorer: {Path}", ActiveTab.FilePath);
        RevealInExplorerRequested?.Invoke(this, ActiveTab.FilePath);
    }

    #endregion

    #region Helper Methods

    /// <summary>Prompts user to save changes if tab is dirty.</summary>
    /// <returns>True if operation should continue, false if cancelled.</returns>
    public async Task<bool> PromptSaveChangesAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        if (!tab.IsDirty) return true;

        _logger?.LogDebug("[PROMPT] Save changes for: {FileName}", tab.FileName);

        var result = await _dialogService.ShowConfirmDialogAsync(
            "Unsaved Changes",
            $"Do you want to save changes to {tab.FileName}?",
            new[] { "Save", "Don't Save", "Cancel" },
            ct);

        _logger?.LogDebug("[PROMPT] User selected: {Result}", result ?? "null");

        return result switch
        {
            "Save" => await SaveTabAsync(tab, ct),
            "Don't Save" => true,
            _ => false // Cancel or null
        };
    }

    /// <summary>Checks if it's safe to close the application.</summary>
    public async Task<bool> CanCloseAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug("[CHECK] CanClose - {Count} dirty tabs", UnsavedTabsCount);

        foreach (var tab in Tabs.Where(t => t.IsDirty).ToList())
        {
            if (!await PromptSaveChangesAsync(tab, ct)) return false;
        }
        return true;
    }

    /// <summary>Gets the tab for a specific file path, if open.</summary>
    public EditorTabViewModel? GetTabByPath(string filePath) =>
        Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

    /// <summary>Checks if a file is currently open.</summary>
    public bool IsFileOpen(string filePath) => GetTabByPath(filePath) != null;

    private static IReadOnlyList<(string Name, string[] Extensions)> GetFileFilters(string? language)
    {
        return language switch
        {
            "csharp" => new[] { ("C# Files", new[] { "*.cs" }), ("All Files", new[] { "*.*" }) },
            "javascript" => new[] { ("JavaScript Files", new[] { "*.js", "*.jsx" }), ("All Files", new[] { "*.*" }) },
            "typescript" => new[] { ("TypeScript Files", new[] { "*.ts", "*.tsx" }), ("All Files", new[] { "*.*" }) },
            "python" => new[] { ("Python Files", new[] { "*.py" }), ("All Files", new[] { "*.*" }) },
            "json" => new[] { ("JSON Files", new[] { "*.json" }), ("All Files", new[] { "*.*" }) },
            "xml" => new[] { ("XML Files", new[] { "*.xml" }), ("All Files", new[] { "*.*" }) },
            "html" => new[] { ("HTML Files", new[] { "*.html", "*.htm" }), ("All Files", new[] { "*.*" }) },
            "css" => new[] { ("CSS Files", new[] { "*.css" }), ("All Files", new[] { "*.*" }) },
            "markdown" => new[] { ("Markdown Files", new[] { "*.md" }), ("All Files", new[] { "*.*" }) },
            _ => new[] { ("All Files", new[] { "*.*" }) }
        };
    }

    #endregion

    #region IDisposable

    /// <summary>Disposes all tabs and clears the collection.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger?.LogDebug("[DISPOSE] EditorPanelViewModel disposing {Count} tabs", Tabs.Count);

        foreach (var tab in Tabs)
            tab.Dispose();
        Tabs.Clear();
    }

    #endregion
}
