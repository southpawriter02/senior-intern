using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the editor panel managing multiple tabs and editor operations.
/// </summary>
public partial class EditorPanelViewModel : ViewModelBase, IDisposable
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IDialogService _dialogService;
    private int _untitledCounter = 1;
    private bool _disposed;

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

    #endregion

    public EditorPanelViewModel(
        IFileSystemService fileSystemService,
        IDialogService dialogService)
    {
        _fileSystemService = fileSystemService;
        _dialogService = dialogService;

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
        if (newValue != null) newValue.IsActive = true;
    }

    #region File Operations

    /// <summary>Opens a file in a new tab or activates existing tab if already open.</summary>
    [RelayCommand]
    public async Task OpenFileAsync(string filePath, CancellationToken ct = default)
    {
        // Check if already open
        var existingTab = Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existingTab != null)
        {
            ActiveTab = existingTab;
            return;
        }

        try
        {
            var content = await _fileSystemService.ReadFileAsync(filePath, ct);
            var tab = EditorTabViewModel.FromFile(filePath, content);

            Tabs.Add(tab);
            ActiveTab = tab;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Error Opening File",
                $"Could not open {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }

    /// <summary>Creates a new untitled file in a new tab.</summary>
    [RelayCommand]
    public void NewFile()
    {
        var fileName = $"Untitled-{_untitledCounter++}";
        var tab = EditorTabViewModel.CreateNew(fileName);

        Tabs.Add(tab);
        ActiveTab = tab;
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
        foreach (var tab in Tabs.Where(t => t.IsDirty).ToList())
        {
            await SaveTabAsync(tab, ct);
        }
    }

    internal async Task<bool> SaveTabAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        if (tab.IsNewFile) return await SaveTabAsAsync(tab, ct);

        try
        {
            await _fileSystemService.WriteFileAsync(tab.FilePath, tab.GetContent(), ct);
            tab.MarkAsSaved();
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(UnsavedTabsCount));
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Error Saving File",
                $"Could not save {tab.FileName}: {ex.Message}");
            return false;
        }
    }

    internal async Task<bool> SaveTabAsAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        var suggestedName = tab.IsNewFile ? tab.FileName + ".txt" : tab.FileName;

        var filePath = await _dialogService.ShowSaveDialogAsync(
            "Save File As",
            suggestedName,
            GetFileFilters(tab.Language));

        if (string.IsNullOrEmpty(filePath)) return false;

        try
        {
            await _fileSystemService.WriteFileAsync(filePath, tab.GetContent(), ct);
            tab.MarkAsSaved(filePath);
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(UnsavedTabsCount));
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Error Saving File",
                $"Could not save to {filePath}: {ex.Message}");
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
        if (!await PromptSaveChangesAsync(tab, ct)) return;

        var index = Tabs.IndexOf(tab);
        var wasActive = ActiveTab == tab;
        
        tab.Dispose();
        Tabs.Remove(tab);

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
    }

    /// <summary>Closes all tabs, prompting for save if any are dirty.</summary>
    [RelayCommand]
    public async Task CloseAllTabsAsync(CancellationToken ct = default)
    {
        for (int i = Tabs.Count - 1; i >= 0; i--)
        {
            var tab = Tabs[i];
            if (!await PromptSaveChangesAsync(tab, ct)) return;

            tab.Dispose();
            Tabs.RemoveAt(i);
        }
        ActiveTab = null;
    }

    /// <summary>Closes all tabs except the specified one.</summary>
    [RelayCommand]
    public async Task CloseOtherTabsAsync(EditorTabViewModel keepTab, CancellationToken ct = default)
    {
        var tabsToClose = Tabs.Where(t => t != keepTab).ToList();

        foreach (var tab in tabsToClose)
        {
            if (!await PromptSaveChangesAsync(tab, ct)) return;
            tab.Dispose();
            Tabs.Remove(tab);
        }
        ActiveTab = keepTab;
    }

    /// <summary>Closes tabs to the right of the specified tab.</summary>
    [RelayCommand]
    public async Task CloseTabsToRightAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        var index = Tabs.IndexOf(tab);
        if (index < 0) return;

        for (int i = Tabs.Count - 1; i > index; i--)
        {
            var tabToClose = Tabs[i];
            if (!await PromptSaveChangesAsync(tabToClose, ct)) return;
            tabToClose.Dispose();
            Tabs.RemoveAt(i);
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
    }

    /// <summary>Activates the previous tab (wraps around).</summary>
    [RelayCommand]
    public void PreviousTab()
    {
        if (Tabs.Count < 2 || ActiveTab == null) return;

        var index = Tabs.IndexOf(ActiveTab);
        var prevIndex = (index - 1 + Tabs.Count) % Tabs.Count;
        ActiveTab = Tabs[prevIndex];
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
    }

    #endregion

    #region Editor Commands

    [RelayCommand]
    public void Undo() => UndoRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    public void Redo() => RedoRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    public void Find() => FindRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    public void Replace() => ReplaceRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    public async Task GoToLineAsync()
    {
        if (ActiveTab == null) return;

        var lineNumber = await _dialogService.ShowGoToLineDialogAsync(
            ActiveTab.LineCount,
            ActiveTab.CaretLine);

        if (lineNumber.HasValue)
            GoToLineRequested?.Invoke(this, lineNumber.Value);
    }

    [RelayCommand]
    public void RevealInExplorer()
    {
        if (ActiveTab == null || ActiveTab.IsNewFile) return;
        RevealInExplorerRequested?.Invoke(this, ActiveTab.FilePath);
    }

    #endregion

    #region Helper Methods

    /// <summary>Prompts user to save changes if tab is dirty.</summary>
    /// <returns>True if operation should continue, false if cancelled.</returns>
    public async Task<bool> PromptSaveChangesAsync(EditorTabViewModel tab, CancellationToken ct = default)
    {
        if (!tab.IsDirty) return true;

        var result = await _dialogService.ShowConfirmDialogAsync(
            "Unsaved Changes",
            $"Do you want to save changes to {tab.FileName}?",
            new[] { "Save", "Don't Save", "Cancel" });

        return result switch
        {
            "Save" => await SaveTabAsync(tab, ct),
            "Don't Save" => true,
            _ => false // Cancel
        };
    }

    /// <summary>Checks if it's safe to close the application.</summary>
    public async Task<bool> CanCloseAsync(CancellationToken ct = default)
    {
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
            _ => new[] { ("All Files", new[] { "*.*" }) }
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var tab in Tabs)
            tab.Dispose();
        Tabs.Clear();
    }

    #endregion
}
