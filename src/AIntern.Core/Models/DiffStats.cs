namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF STATS (v0.4.2a)                                                     │
// │ Summary statistics for a diff computation.                               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Summary statistics for a diff computation.
/// Implemented as a record for value equality and immutability.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2a.</para>
/// <para>
/// Provides line counts and computed properties for displaying diff summaries
/// in UI components and logs.
/// </para>
/// </remarks>
public sealed record DiffStats
{
    /// <summary>
    /// Total number of lines involved in the diff.
    /// Sum of all line types.
    /// </summary>
    public int TotalLines { get; init; }

    /// <summary>
    /// Number of lines added (exist only in proposed).
    /// </summary>
    public int AddedLines { get; init; }

    /// <summary>
    /// Number of lines removed (exist only in original).
    /// </summary>
    public int RemovedLines { get; init; }

    /// <summary>
    /// Number of lines modified (exist in both but content differs).
    /// </summary>
    public int ModifiedLines { get; init; }

    /// <summary>
    /// Number of unchanged context lines.
    /// </summary>
    public int UnchangedLines { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Net line change (additions minus removals).
    /// Positive means file grew, negative means file shrank.
    /// </summary>
    public int NetChange => AddedLines - RemovedLines;

    /// <summary>
    /// Percentage of lines that were changed.
    /// </summary>
    public double ChangePercentage => TotalLines > 0
        ? (double)(AddedLines + RemovedLines + ModifiedLines) / TotalLines * 100
        : 0;

    /// <summary>
    /// Total number of changed lines (added + removed + modified).
    /// </summary>
    public int ChangedLines => AddedLines + RemovedLines + ModifiedLines;

    /// <summary>
    /// Human-readable summary in compact format.
    /// </summary>
    /// <example>"+5 -2" or "+5 -2 ~1"</example>
    public string Summary
    {
        get
        {
            var parts = new List<string>(3);
            
            // Always show additions if there are any, or if there are no other changes
            if (AddedLines > 0 || (RemovedLines == 0 && ModifiedLines == 0))
                parts.Add($"+{AddedLines}");
            
            // Always show removals if there are any, or if there are no other changes  
            if (RemovedLines > 0 || (AddedLines == 0 && ModifiedLines == 0))
                parts.Add($"-{RemovedLines}");
            
            // Show modifications only if present
            if (ModifiedLines > 0)
                parts.Add($"~{ModifiedLines}");
            
            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Verbose summary with labels.
    /// </summary>
    /// <example>"5 additions, 2 deletions, 1 modification"</example>
    public string VerboseSummary
    {
        get
        {
            var parts = new List<string>(3);
            
            if (AddedLines > 0)
                parts.Add($"{AddedLines} addition{(AddedLines != 1 ? "s" : "")}");
            if (RemovedLines > 0)
                parts.Add($"{RemovedLines} deletion{(RemovedLines != 1 ? "s" : "")}");
            if (ModifiedLines > 0)
                parts.Add($"{ModifiedLines} modification{(ModifiedLines != 1 ? "s" : "")}");
            
            return parts.Count > 0 ? string.Join(", ", parts) : "No changes";
        }
    }

    /// <summary>
    /// Whether there are any changes.
    /// </summary>
    public bool HasChanges => AddedLines > 0 || RemovedLines > 0 || ModifiedLines > 0;

    // ═══════════════════════════════════════════════════════════════════════
    // Static Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Empty stats instance for no changes.
    /// </summary>
    public static DiffStats Empty => new();

    /// <summary>
    /// Creates stats from line counts.
    /// </summary>
    /// <param name="added">Number of added lines.</param>
    /// <param name="removed">Number of removed lines.</param>
    /// <param name="modified">Number of modified lines.</param>
    /// <param name="unchanged">Number of unchanged context lines.</param>
    /// <returns>A new <see cref="DiffStats"/> instance with calculated total.</returns>
    public static DiffStats FromCounts(int added, int removed, int modified, int unchanged) => new()
    {
        AddedLines = added,
        RemovedLines = removed,
        ModifiedLines = modified,
        UnchangedLines = unchanged,
        TotalLines = added + removed + modified + unchanged
    };
}
