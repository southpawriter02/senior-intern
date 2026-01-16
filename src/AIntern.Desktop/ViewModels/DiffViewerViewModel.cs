namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF VIEWER VIEWMODEL (v0.4.2d)                                          │
// │ Main ViewModel for diff viewer UI with navigation and actions.           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Main ViewModel for the diff viewer, coordinating display, navigation, and actions.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2d.</para>
/// <para>
/// This ViewModel serves as the primary coordinator for the diff viewing experience:
/// - Loading and displaying diff results from code blocks
/// - Navigation between diff hunks
/// - View options like inline changes, word wrap, and synchronized scrolling
/// - User actions like apply, reject, and copy
/// </para>
/// </remarks>
public partial class DiffViewerViewModel : ViewModelBase
{
    private readonly IDiffService _diffService;
    private readonly IInlineDiffService _inlineDiffService;
    private readonly ILogger<DiffViewerViewModel>? _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties - Diff Data
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>The computed diff result being displayed.</summary>
    [ObservableProperty]
    private DiffResult? _diffResult;

    /// <summary>The source code block that generated this diff.</summary>
    [ObservableProperty]
    private CodeBlock? _sourceBlock;

    /// <summary>Full path to the file being diffed.</summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>File name for display.</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>Programming language for syntax highlighting.</summary>
    [ObservableProperty]
    private string? _language;

    /// <summary>Whether this diff represents a new file creation.</summary>
    [ObservableProperty]
    private bool _isNewFile;

    /// <summary>Whether this diff represents a file deletion.</summary>
    [ObservableProperty]
    private bool _isDeleteFile;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties - Loading State
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Whether a diff computation is in progress.</summary>
    [ObservableProperty]
    private bool _isLoading;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties - View Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Whether to show character-level inline changes.</summary>
    [ObservableProperty]
    private bool _showInlineChanges = true;

    /// <summary>Whether to wrap long lines.</summary>
    [ObservableProperty]
    private bool _wordWrap;

    /// <summary>Whether to synchronize scrolling between panels.</summary>
    [ObservableProperty]
    private bool _synchronizedScroll = true;

    /// <summary>Whether to display line numbers.</summary>
    [ObservableProperty]
    private bool _showLineNumbers = true;

    /// <summary>Number of context lines around changes.</summary>
    [ObservableProperty]
    private int _contextLines = 3;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties - Navigation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Index of the currently focused hunk (0-based).</summary>
    [ObservableProperty]
    private int _currentHunkIndex;

    /// <summary>Total number of hunks in the diff.</summary>
    [ObservableProperty]
    private int _totalHunks;

    /// <summary>Collection of hunk ViewModels.</summary>
    [ObservableProperty]
    private ObservableCollection<DiffHunkViewModel> _hunks = [];

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties - Scroll State
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Vertical scroll offset for original panel.</summary>
    [ObservableProperty]
    private double _originalScrollOffset;

    /// <summary>Vertical scroll offset for proposed panel.</summary>
    [ObservableProperty]
    private double _proposedScrollOffset;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Human-readable summary of diff statistics.</summary>
    public string StatsDisplay => DiffResult?.Stats.Summary ?? string.Empty;

    /// <summary>Whether this diff contains any changes.</summary>
    public bool HasChanges => DiffResult?.HasChanges ?? false;

    /// <summary>Whether navigation to previous hunk is possible.</summary>
    public bool CanNavigatePrevious => CurrentHunkIndex > 0;

    /// <summary>Whether navigation to next hunk is possible.</summary>
    public bool CanNavigateNext => CurrentHunkIndex < TotalHunks - 1;

    /// <summary>Display text for hunk position (e.g., "2/5").</summary>
    public string HunkPositionDisplay => TotalHunks > 0
        ? $"{CurrentHunkIndex + 1}/{TotalHunks}"
        : "0/0";

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Raised when the view should scroll to a hunk.</summary>
    public event EventHandler<int>? HunkNavigationRequested;

    /// <summary>Raised when user requests to apply changes.</summary>
    public event EventHandler? ApplyRequested;

    /// <summary>Raised when user requests to reject/dismiss.</summary>
    public event EventHandler? RejectRequested;

    /// <summary>Raised for scroll synchronization.</summary>
    public event EventHandler<ScrollSyncEventArgs>? ScrollSyncRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new DiffViewerViewModel.
    /// </summary>
    public DiffViewerViewModel(
        IDiffService diffService,
        IInlineDiffService inlineDiffService,
        ILogger<DiffViewerViewModel>? logger = null)
    {
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _inlineDiffService = inlineDiffService ?? throw new ArgumentNullException(nameof(inlineDiffService));
        _logger = logger;

        _logger?.LogDebug("DiffViewerViewModel initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Load and compute a diff from a code block.
    /// </summary>
    public async Task LoadDiffAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Loading diff for block {BlockId}, workspace: {Path}",
                block.Id, workspacePath);

            IsLoading = true;
            ClearError();

            SourceBlock = block;
            FilePath = block.TargetFilePath ?? string.Empty;
            FileName = Path.GetFileName(FilePath);
            Language = block.DisplayLanguage;

            var result = await _diffService.ComputeDiffForBlockAsync(
                block,
                workspacePath,
                cancellationToken);

            SetDiffResult(result);

            _logger?.LogInformation("Diff loaded: {Stats}, {HunkCount} hunks",
                result.Stats.Summary, result.Hunks.Count);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Diff loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to compute diff");
            SetError($"Failed to compute diff: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load a pre-computed diff result.
    /// </summary>
    public void LoadDiff(DiffResult result, string? filePath = null)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        _logger?.LogDebug("Loading pre-computed diff: {Stats}", result.Stats.Summary);

        FilePath = filePath ?? result.OriginalFilePath;
        FileName = Path.GetFileName(FilePath);

        SetDiffResult(result);
    }

    /// <summary>
    /// Scroll to a specific line number.
    /// </summary>
    public void ScrollToLine(int lineNumber, DiffSide side)
    {
        for (int i = 0; i < Hunks.Count; i++)
        {
            var hunk = Hunks[i];
            var lines = side == DiffSide.Original ? hunk.OriginalLines : hunk.ProposedLines;

            if (lines.Any(l => l.LineNumber == lineNumber))
            {
                CurrentHunkIndex = i;
                ScrollToCurrentHunk();
                return;
            }
        }
    }

    /// <summary>
    /// Clear the current diff and reset state.
    /// </summary>
    public void Clear()
    {
        _logger?.LogDebug("Clearing diff viewer state");

        DiffResult = null;
        SourceBlock = null;
        FilePath = string.Empty;
        FileName = string.Empty;
        Language = null;
        IsNewFile = false;
        IsDeleteFile = false;
        ClearError();
        TotalHunks = 0;
        CurrentHunkIndex = 0;
        Hunks.Clear();

        NotifyComputedPropertiesChanged();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════

    private void SetDiffResult(DiffResult result)
    {
        DiffResult = result;
        IsNewFile = result.IsNewFile;
        IsDeleteFile = result.IsDeleteFile;
        TotalHunks = result.Hunks.Count;
        CurrentHunkIndex = 0;

        Hunks.Clear();
        foreach (var hunk in result.Hunks)
        {
            var hunkVm = new DiffHunkViewModel(
                hunk,
                _inlineDiffService,
                ShowInlineChanges);
            Hunks.Add(hunkVm);
        }

        NotifyComputedPropertiesChanged();
    }

    private void ScrollToCurrentHunk()
    {
        _logger?.LogTrace("Navigating to hunk {Index}", CurrentHunkIndex);
        HunkNavigationRequested?.Invoke(this, CurrentHunkIndex);
    }

    private void NotifyComputedPropertiesChanged()
    {
        OnPropertyChanged(nameof(StatsDisplay));
        OnPropertyChanged(nameof(HasChanges));
        OnPropertyChanged(nameof(HunkPositionDisplay));
        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Property Change Handlers
    // ═══════════════════════════════════════════════════════════════════════

    partial void OnShowInlineChangesChanged(bool value)
    {
        _logger?.LogTrace("ShowInlineChanges changed to {Value}", value);
        foreach (var hunk in Hunks)
            hunk.ShowInlineChanges = value;
    }

    partial void OnCurrentHunkIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
        OnPropertyChanged(nameof(HunkPositionDisplay));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Navigation Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Navigate to next hunk.</summary>
    [RelayCommand(CanExecute = nameof(CanNavigateNext))]
    private void NextHunk()
    {
        if (CanNavigateNext)
        {
            CurrentHunkIndex++;
            ScrollToCurrentHunk();
        }
    }

    /// <summary>Navigate to previous hunk.</summary>
    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private void PreviousHunk()
    {
        if (CanNavigatePrevious)
        {
            CurrentHunkIndex--;
            ScrollToCurrentHunk();
        }
    }

    /// <summary>Navigate to specific hunk.</summary>
    [RelayCommand]
    private void GoToHunk(int index)
    {
        if (index >= 0 && index < TotalHunks)
        {
            CurrentHunkIndex = index;
            ScrollToCurrentHunk();
        }
    }

    /// <summary>Navigate to first hunk.</summary>
    [RelayCommand]
    private void FirstHunk()
    {
        if (TotalHunks > 0)
        {
            CurrentHunkIndex = 0;
            ScrollToCurrentHunk();
        }
    }

    /// <summary>Navigate to last hunk.</summary>
    [RelayCommand]
    private void LastHunk()
    {
        if (TotalHunks > 0)
        {
            CurrentHunkIndex = TotalHunks - 1;
            ScrollToCurrentHunk();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Toggle Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Toggle inline changes display.</summary>
    [RelayCommand]
    private void ToggleInlineChanges() => ShowInlineChanges = !ShowInlineChanges;

    /// <summary>Toggle word wrap.</summary>
    [RelayCommand]
    private void ToggleWordWrap() => WordWrap = !WordWrap;

    /// <summary>Toggle synchronized scrolling.</summary>
    [RelayCommand]
    private void ToggleSynchronizedScroll() => SynchronizedScroll = !SynchronizedScroll;

    /// <summary>Toggle line numbers.</summary>
    [RelayCommand]
    private void ToggleLineNumbers() => ShowLineNumbers = !ShowLineNumbers;

    // ═══════════════════════════════════════════════════════════════════════
    // Action Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Request to apply changes.</summary>
    [RelayCommand]
    private void RequestApply()
    {
        _logger?.LogInformation("Apply requested for {FilePath}", FilePath);
        ApplyRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Request to reject/dismiss.</summary>
    [RelayCommand]
    private void RequestReject()
    {
        _logger?.LogInformation("Reject requested for {FilePath}", FilePath);
        RejectRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Copy proposed content to clipboard.</summary>
    [RelayCommand]
    private async Task CopyProposedAsync()
    {
        if (DiffResult?.ProposedContent != null)
        {
            _logger?.LogDebug("Copying proposed content to clipboard");
            // Clipboard implementation handled by platform service
            await Task.CompletedTask;
        }
    }

    /// <summary>Copy original content to clipboard.</summary>
    [RelayCommand]
    private async Task CopyOriginalAsync()
    {
        if (DiffResult?.OriginalContent != null)
        {
            _logger?.LogDebug("Copying original content to clipboard");
            await Task.CompletedTask;
        }
    }
}

/// <summary>
/// Event args for scroll synchronization.
/// </summary>
public sealed class ScrollSyncEventArgs : EventArgs
{
    /// <summary>Scroll offset to synchronize to.</summary>
    public double Offset { get; init; }

    /// <summary>Which panel initiated the scroll.</summary>
    public DiffSide SourceSide { get; init; }
}
