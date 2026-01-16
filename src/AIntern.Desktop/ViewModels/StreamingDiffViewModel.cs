using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STREAMING DIFF VIEW MODEL (v0.4.5b)                                     │
// │ ViewModel for streaming diff preview state on a code block.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for streaming diff preview state on a code block.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public partial class StreamingDiffViewModel : ViewModelBase
{
    /// <summary>
    /// The code block ID this diff is for.
    /// </summary>
    [ObservableProperty]
    private Guid _blockId;

    /// <summary>
    /// Current computation status.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComputing))]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    [NotifyPropertyChangedFor(nameof(IsFailed))]
    [NotifyPropertyChangedFor(nameof(ShowStats))]
    [NotifyPropertyChangedFor(nameof(ShowError))]
    [NotifyPropertyChangedFor(nameof(ShowSpinner))]
    private DiffComputationStatus _status = DiffComputationStatus.Pending;

    /// <summary>
    /// The computed diff result.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyPropertyChangedFor(nameof(StatsDisplay))]
    [NotifyPropertyChangedFor(nameof(AddedLines))]
    [NotifyPropertyChangedFor(nameof(RemovedLines))]
    [NotifyPropertyChangedFor(nameof(ModifiedLines))]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private DiffResult? _result;

    /// <summary>
    /// Error message if computation failed.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Whether this is a new file (no original).
    /// </summary>
    [ObservableProperty]
    private bool _isNewFile;

    /// <summary>
    /// Whether the diff has been finalized.
    /// </summary>
    [ObservableProperty]
    private bool _isFinalized;

    // ═══════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether computation is in progress.
    /// </summary>
    public bool IsComputing => Status == DiffComputationStatus.Computing;

    /// <summary>
    /// Whether computation completed successfully.
    /// </summary>
    public bool IsCompleted => Status == DiffComputationStatus.Completed;

    /// <summary>
    /// Whether computation failed.
    /// </summary>
    public bool IsFailed => Status == DiffComputationStatus.Failed;

    /// <summary>
    /// Whether a result is available.
    /// </summary>
    public bool HasResult => Result is not null && IsCompleted;

    /// <summary>
    /// Whether stats should be shown.
    /// </summary>
    public bool ShowStats => HasResult && HasChanges;

    /// <summary>
    /// Whether error should be shown.
    /// </summary>
    public bool ShowError => IsFailed && !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Whether spinner should be shown.
    /// </summary>
    public bool ShowSpinner => IsComputing || Status == DiffComputationStatus.Pending;

    /// <summary>
    /// Whether there are any changes.
    /// </summary>
    public bool HasChanges => Result?.Stats is { } stats &&
        (stats.AddedLines > 0 || stats.RemovedLines > 0 || stats.ModifiedLines > 0);

    /// <summary>
    /// Number of added lines.
    /// </summary>
    public int AddedLines => Result?.Stats?.AddedLines ?? 0;

    /// <summary>
    /// Number of removed lines.
    /// </summary>
    public int RemovedLines => Result?.Stats?.RemovedLines ?? 0;

    /// <summary>
    /// Number of modified lines.
    /// </summary>
    public int ModifiedLines => Result?.Stats?.ModifiedLines ?? 0;

    /// <summary>
    /// Stats display string (e.g., "+5 -3 ~2").
    /// </summary>
    public string StatsDisplay
    {
        get
        {
            if (Result?.Stats is not { } stats)
                return string.Empty;

            var parts = new List<string>(3);
            if (stats.AddedLines > 0) parts.Add($"+{stats.AddedLines}");
            if (stats.RemovedLines > 0) parts.Add($"-{stats.RemovedLines}");
            if (stats.ModifiedLines > 0) parts.Add($"~{stats.ModifiedLines}");
            return parts.Count > 0 ? string.Join(" ", parts) : "No changes";
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Update from a computation state.
    /// </summary>
    /// <param name="state">The state to update from.</param>
    public void UpdateFromState(DiffComputationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        BlockId = state.BlockId;
        Status = state.Status;
        Result = state.Result;
        ErrorMessage = state.ErrorMessage;
        IsNewFile = state.IsNewFile;
        IsFinalized = state.IsFinalized;
    }
}
