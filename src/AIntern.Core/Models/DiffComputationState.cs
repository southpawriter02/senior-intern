namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF COMPUTATION STATE (v0.4.5b)                                        │
// │ Tracks state of a streaming diff computation for a code block.          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Tracks the state of a streaming diff computation for a single code block.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public sealed class DiffComputationState
{
    /// <summary>
    /// Identifier of the code block being diffed.
    /// </summary>
    public Guid BlockId { get; init; }

    /// <summary>
    /// When the first computation was requested.
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the diff was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the computation.
    /// </summary>
    public DiffComputationStatus Status { get; set; } = DiffComputationStatus.Pending;

    /// <summary>
    /// The computed diff result (null if not yet computed or failed).
    /// </summary>
    public DiffResult? Result { get; set; }

    /// <summary>
    /// Error message if computation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Hash of the content that was last diffed (for change detection).
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// Number of times this diff has been recomputed.
    /// </summary>
    public int ComputationCount { get; set; }

    /// <summary>
    /// Target file path being diffed.
    /// </summary>
    public string? TargetFilePath { get; set; }

    /// <summary>
    /// Whether this is a new file (no original to compare).
    /// </summary>
    public bool IsNewFile { get; set; }

    /// <summary>
    /// Time spent computing (for diagnostics).
    /// </summary>
    public TimeSpan ComputationDuration { get; set; }

    /// <summary>
    /// Whether the diff has been finalized (streaming complete).
    /// </summary>
    public bool IsFinalized { get; set; }

    /// <summary>
    /// Whether a new computation is needed (content changed since last compute).
    /// </summary>
    public bool NeedsRecompute { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether the computation completed successfully.
    /// </summary>
    public bool IsCompleted => Status == DiffComputationStatus.Completed;

    /// <summary>
    /// Whether the computation is currently in progress.
    /// </summary>
    public bool IsComputing => Status == DiffComputationStatus.Computing;

    /// <summary>
    /// Whether the computation failed.
    /// </summary>
    public bool IsFailed => Status == DiffComputationStatus.Failed;

    /// <summary>
    /// Whether a valid diff result is available.
    /// </summary>
    public bool HasResult => Result is not null && IsCompleted;

    /// <summary>
    /// Summary stats string for display (e.g., "+5 -3 ~2").
    /// </summary>
    public string StatsDisplay => Result?.Stats is { } stats
        ? FormatStats(stats)
        : string.Empty;

    private static string FormatStats(DiffStats stats)
    {
        var parts = new List<string>(3);
        if (stats.AddedLines > 0) parts.Add($"+{stats.AddedLines}");
        if (stats.RemovedLines > 0) parts.Add($"-{stats.RemovedLines}");
        if (stats.ModifiedLines > 0) parts.Add($"~{stats.ModifiedLines}");
        return parts.Count > 0 ? string.Join(" ", parts) : "No changes";
    }
}
