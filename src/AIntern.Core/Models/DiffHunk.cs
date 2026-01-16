namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF HUNK (v0.4.2a)                                                      │
// │ Contiguous group of changes in a diff.                                   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a contiguous group of changes in a diff.
/// A hunk contains context lines (unchanged) surrounding the actual changes,
/// similar to the hunks in a unified diff format.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2a.</para>
/// <para>
/// Hunk header format follows Git/unified diff convention:
/// @@ -originalStart,originalCount +proposedStart,proposedCount @@ [context]
/// </para>
/// </remarks>
public sealed class DiffHunk
{
    /// <summary>
    /// Unique identifier for this hunk.
    /// Used for navigation and per-hunk apply/reject actions.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Starting line number in the original content (1-based).
    /// </summary>
    public int OriginalStartLine { get; init; }

    /// <summary>
    /// Number of lines from the original content in this hunk.
    /// Includes context lines and removed lines.
    /// </summary>
    public int OriginalLineCount { get; init; }

    /// <summary>
    /// Starting line number in the proposed content (1-based).
    /// </summary>
    public int ProposedStartLine { get; init; }

    /// <summary>
    /// Number of lines from the proposed content in this hunk.
    /// Includes context lines and added lines.
    /// </summary>
    public int ProposedLineCount { get; init; }

    /// <summary>
    /// All lines in this hunk with their change types.
    /// Includes context lines (Unchanged), additions (Added),
    /// removals (Removed), and modifications (Modified).
    /// </summary>
    public IReadOnlyList<DiffLine> Lines { get; init; } = [];

    /// <summary>
    /// Optional header/context for this hunk.
    /// Typically contains the function or class name where changes occur.
    /// </summary>
    /// <example>"public void ProcessData()"</example>
    public string? ContextHeader { get; init; }

    /// <summary>
    /// Index of this hunk in the parent DiffResult (0-based).
    /// Useful for navigation: "Hunk 2 of 5".
    /// </summary>
    public int Index { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Standard hunk header in unified diff format.
    /// </summary>
    /// <example>"@@ -10,5 +10,7 @@"</example>
    public string Header =>
        $"@@ -{OriginalStartLine},{OriginalLineCount} +{ProposedStartLine},{ProposedLineCount} @@";

    /// <summary>
    /// Full header including context if available.
    /// </summary>
    /// <example>"@@ -10,5 +10,7 @@ public void ProcessData()"</example>
    public string FullHeader => ContextHeader is not null
        ? $"{Header} {ContextHeader}"
        : Header;

    /// <summary>
    /// Lines that were added in this hunk.
    /// </summary>
    public IEnumerable<DiffLine> AddedLines =>
        Lines.Where(l => l.Type == DiffLineType.Added);

    /// <summary>
    /// Lines that were removed in this hunk.
    /// </summary>
    public IEnumerable<DiffLine> RemovedLines =>
        Lines.Where(l => l.Type == DiffLineType.Removed);

    /// <summary>
    /// Lines that were modified in this hunk.
    /// </summary>
    public IEnumerable<DiffLine> ModifiedLines =>
        Lines.Where(l => l.Type == DiffLineType.Modified);

    /// <summary>
    /// Context (unchanged) lines in this hunk.
    /// </summary>
    public IEnumerable<DiffLine> UnchangedLines =>
        Lines.Where(l => l.Type == DiffLineType.Unchanged);

    /// <summary>
    /// Whether this hunk contains only additions (no removals).
    /// </summary>
    public bool IsInsertOnly => Lines.All(l =>
        l.Type is DiffLineType.Added or DiffLineType.Unchanged);

    /// <summary>
    /// Whether this hunk contains only deletions (no additions).
    /// </summary>
    public bool IsDeleteOnly => Lines.All(l =>
        l.Type is DiffLineType.Removed or DiffLineType.Unchanged);

    /// <summary>
    /// Whether this hunk contains modifications (paired removed/added lines).
    /// </summary>
    public bool HasModifications => Lines.Any(l => l.Type == DiffLineType.Modified);

    /// <summary>
    /// Count of added lines in this hunk.
    /// </summary>
    public int AddedCount => Lines.Count(l => l.Type == DiffLineType.Added);

    /// <summary>
    /// Count of removed lines in this hunk.
    /// </summary>
    public int RemovedCount => Lines.Count(l => l.Type == DiffLineType.Removed);

    /// <summary>
    /// Count of modified lines in this hunk.
    /// </summary>
    public int ModifiedCount => Lines.Count(l => l.Type == DiffLineType.Modified);
}
