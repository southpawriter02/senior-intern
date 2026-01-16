namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF HUNK VIEWMODEL (v0.4.2d)                                            │
// │ ViewModel for a diff hunk with side-by-side aligned lines.               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for a diff hunk, providing side-by-side aligned lines for display.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2d.</para>
/// <para>
/// This ViewModel transforms a DiffHunk into two aligned collections of lines:
/// one for the original side and one for the proposed side. Placeholder lines
/// are inserted to maintain visual alignment between the two panels.
/// </para>
/// </remarks>
public partial class DiffHunkViewModel : ViewModelBase
{
    private readonly IInlineDiffService _inlineDiffService;
    private readonly ILogger<DiffHunkViewModel>? _logger;
    private readonly DiffHunk _hunk;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Unique identifier for this hunk.
    /// </summary>
    [ObservableProperty]
    private Guid _id;

    /// <summary>
    /// Index of this hunk within the diff (0-based).
    /// </summary>
    [ObservableProperty]
    private int _index;

    /// <summary>
    /// Standard unified diff header (e.g., "@@ -10,5 +10,7 @@").
    /// </summary>
    [ObservableProperty]
    private string _header = string.Empty;

    /// <summary>
    /// Optional context header showing function/class name.
    /// </summary>
    [ObservableProperty]
    private string? _contextHeader;

    /// <summary>
    /// Lines for the original (left) side panel.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiffLineViewModel> _originalLines = [];

    /// <summary>
    /// Lines for the proposed (right) side panel.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiffLineViewModel> _proposedLines = [];

    /// <summary>
    /// Whether to show inline character-level changes.
    /// </summary>
    [ObservableProperty]
    private bool _showInlineChanges = true;

    /// <summary>
    /// Whether this hunk is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// Whether this hunk is currently focused/selected.
    /// </summary>
    [ObservableProperty]
    private bool _isFocused;

    // ═══════════════════════════════════════════════════════════════════════
    // Read-Only Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Number of lines added in this hunk.</summary>
    public int AddedCount { get; }

    /// <summary>Number of lines removed in this hunk.</summary>
    public int RemovedCount { get; }

    /// <summary>Number of lines modified in this hunk.</summary>
    public int ModifiedCount { get; }

    /// <summary>Total changed lines.</summary>
    public int TotalChanges => AddedCount + RemovedCount + ModifiedCount;

    /// <summary>Starting line number in original file.</summary>
    public int OriginalStartLine { get; }

    /// <summary>Starting line number in proposed file.</summary>
    public int ProposedStartLine { get; }

    /// <summary>Number of lines displayed.</summary>
    public int DisplayLineCount => Math.Max(OriginalLines.Count, ProposedLines.Count);

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new DiffHunkViewModel.
    /// </summary>
    public DiffHunkViewModel(
        DiffHunk hunk,
        IInlineDiffService inlineDiffService,
        bool showInlineChanges,
        ILogger<DiffHunkViewModel>? logger = null)
    {
        _hunk = hunk ?? throw new ArgumentNullException(nameof(hunk));
        _inlineDiffService = inlineDiffService ?? throw new ArgumentNullException(nameof(inlineDiffService));
        _logger = logger;

        // Copy hunk properties
        Id = hunk.Id;
        Index = hunk.Index;
        Header = hunk.Header;
        ContextHeader = hunk.ContextHeader;
        ShowInlineChanges = showInlineChanges;

        // Store start lines
        OriginalStartLine = hunk.OriginalStartLine;
        ProposedStartLine = hunk.ProposedStartLine;

        // Calculate statistics
        AddedCount = hunk.Lines.Count(l => l.Type == DiffLineType.Added);
        RemovedCount = hunk.Lines.Count(l => l.Type == DiffLineType.Removed);
        ModifiedCount = hunk.Lines.Count(l => l.Type == DiffLineType.Modified);

        _logger?.LogDebug("Creating DiffHunkViewModel for hunk {Index}: +{Added} -{Removed}",
            Index, AddedCount, RemovedCount);

        // Build side-by-side aligned lines
        BuildSideBySideLines(hunk);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Side-by-Side Building
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Build aligned side-by-side line collections from the hunk.
    /// </summary>
    private void BuildSideBySideLines(DiffHunk hunk)
    {
        var originalList = new List<DiffLineViewModel>();
        var proposedList = new List<DiffLineViewModel>();

        int i = 0;
        while (i < hunk.Lines.Count)
        {
            var line = hunk.Lines[i];

            switch (line.Type)
            {
                case DiffLineType.Unchanged:
                    // Add to both sides
                    originalList.Add(CreateLineViewModel(line, DiffSide.Original));
                    proposedList.Add(CreateLineViewModel(line, DiffSide.Proposed));
                    i++;
                    break;

                case DiffLineType.Removed:
                    // Check if followed by Added (modification pair)
                    if (i + 1 < hunk.Lines.Count &&
                        hunk.Lines[i + 1].Type == DiffLineType.Added &&
                        line.PairedLine != null)
                    {
                        var removedLine = line;
                        var addedLine = hunk.Lines[i + 1];

                        originalList.Add(CreateLineViewModel(removedLine, DiffSide.Original));
                        proposedList.Add(CreateLineViewModel(addedLine, DiffSide.Proposed));
                        i += 2;
                    }
                    else
                    {
                        // Pure removal
                        originalList.Add(CreateLineViewModel(line, DiffSide.Original));
                        proposedList.Add(DiffLineViewModel.Placeholder());
                        i++;
                    }
                    break;

                case DiffLineType.Added:
                    // Pure addition
                    originalList.Add(DiffLineViewModel.Placeholder());
                    proposedList.Add(CreateLineViewModel(line, DiffSide.Proposed));
                    i++;
                    break;

                case DiffLineType.Modified:
                    if (line.OriginalLineNumber.HasValue)
                        originalList.Add(CreateLineViewModel(line, DiffSide.Original));
                    if (line.ProposedLineNumber.HasValue)
                        proposedList.Add(CreateLineViewModel(line, DiffSide.Proposed));
                    i++;
                    break;

                default:
                    i++;
                    break;
            }
        }

        // Align lists
        AlignLists(originalList, proposedList);

        OriginalLines = new ObservableCollection<DiffLineViewModel>(originalList);
        ProposedLines = new ObservableCollection<DiffLineViewModel>(proposedList);

        _logger?.LogTrace("Built side-by-side lines: {OrigCount} original, {PropCount} proposed",
            originalList.Count, proposedList.Count);
    }

    private DiffLineViewModel CreateLineViewModel(DiffLine line, DiffSide side)
    {
        IReadOnlyList<InlineSegment>? segments = null;
        if (line.HasInlineChanges && ShowInlineChanges)
        {
            segments = _inlineDiffService.GetInlineSegments(
                line.Content,
                line.InlineChanges!,
                side);
        }

        return DiffLineViewModel.FromDiffLine(line, side, segments);
    }

    private static void AlignLists(
        List<DiffLineViewModel> original,
        List<DiffLineViewModel> proposed)
    {
        while (original.Count < proposed.Count)
            original.Add(DiffLineViewModel.Placeholder());
        while (proposed.Count < original.Count)
            proposed.Add(DiffLineViewModel.Placeholder());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Property Change Handlers
    // ═══════════════════════════════════════════════════════════════════════

    partial void OnShowInlineChangesChanged(bool value)
    {
        _logger?.LogTrace("ShowInlineChanges changed to {Value}, rebuilding lines", value);
        BuildSideBySideLines(_hunk);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Toggle the expanded state.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        _logger?.LogTrace("Hunk {Index} expanded: {IsExpanded}", Index, IsExpanded);
    }
}
