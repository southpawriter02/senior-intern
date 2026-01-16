namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF LINE (v0.4.2a)                                                      │
// │ Single line in a diff with change type and optional inline changes.     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a single line in a diff with its change type and optional inline changes.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2a.</para>
/// <para>
/// Each line has both original and proposed line numbers (one may be null depending
/// on the change type). Modified lines may also contain inline character-level changes.
/// </para>
/// </remarks>
public sealed class DiffLine
{
    /// <summary>
    /// Line number in the original content (1-based).
    /// Null if this line was added (doesn't exist in original).
    /// </summary>
    public int? OriginalLineNumber { get; init; }

    /// <summary>
    /// Line number in the proposed content (1-based).
    /// Null if this line was removed (doesn't exist in proposed).
    /// </summary>
    public int? ProposedLineNumber { get; init; }

    /// <summary>
    /// The text content of this line (without line ending).
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// The type of change for this line.
    /// </summary>
    public DiffLineType Type { get; init; }

    /// <summary>
    /// Inline character-level changes within this line.
    /// Only populated for Modified lines to show exactly what changed.
    /// </summary>
    public IReadOnlyList<InlineChange>? InlineChanges { get; init; }

    /// <summary>
    /// The paired line for modified lines.
    /// For a removed line that was modified, this points to the corresponding added line.
    /// For an added line that was modified, this points to the corresponding removed line.
    /// </summary>
    public DiffLine? PairedLine { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether this line has inline character-level changes.
    /// </summary>
    public bool HasInlineChanges => InlineChanges is { Count: > 0 };

    /// <summary>
    /// Display prefix character for unified diff format.
    /// </summary>
    public char Prefix => Type switch
    {
        DiffLineType.Added => '+',
        DiffLineType.Removed => '-',
        DiffLineType.Modified => '~',
        DiffLineType.Unchanged => ' ',
        _ => ' '
    };

    /// <summary>
    /// Whether this line exists on the original (left) side.
    /// </summary>
    public bool ExistsOnOriginalSide => Type is not DiffLineType.Added;

    /// <summary>
    /// Whether this line exists on the proposed (right) side.
    /// </summary>
    public bool ExistsOnProposedSide => Type is not DiffLineType.Removed;

    /// <summary>
    /// Gets the appropriate line number based on the diff side.
    /// </summary>
    /// <param name="side">Which side of the diff to get the line number for.</param>
    /// <returns>The line number, or null if the line doesn't exist on that side.</returns>
    public int? GetLineNumber(DiffSide side) => side switch
    {
        DiffSide.Original => OriginalLineNumber,
        DiffSide.Proposed => ProposedLineNumber,
        _ => null
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Static Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an unchanged context line.
    /// </summary>
    /// <param name="originalLine">Line number in original content (1-based).</param>
    /// <param name="proposedLine">Line number in proposed content (1-based).</param>
    /// <param name="content">The line text content.</param>
    /// <returns>A new <see cref="DiffLine"/> representing unchanged content.</returns>
    public static DiffLine Unchanged(int originalLine, int proposedLine, string content) => new()
    {
        OriginalLineNumber = originalLine,
        ProposedLineNumber = proposedLine,
        Content = content,
        Type = DiffLineType.Unchanged
    };

    /// <summary>
    /// Creates an added line (only exists in proposed).
    /// </summary>
    /// <param name="proposedLine">Line number in proposed content (1-based).</param>
    /// <param name="content">The line text content.</param>
    /// <returns>A new <see cref="DiffLine"/> representing an addition.</returns>
    public static DiffLine Added(int proposedLine, string content) => new()
    {
        OriginalLineNumber = null,
        ProposedLineNumber = proposedLine,
        Content = content,
        Type = DiffLineType.Added
    };

    /// <summary>
    /// Creates a removed line (only exists in original).
    /// </summary>
    /// <param name="originalLine">Line number in original content (1-based).</param>
    /// <param name="content">The line text content.</param>
    /// <returns>A new <see cref="DiffLine"/> representing a removal.</returns>
    public static DiffLine Removed(int originalLine, string content) => new()
    {
        OriginalLineNumber = originalLine,
        ProposedLineNumber = null,
        Content = content,
        Type = DiffLineType.Removed
    };

    /// <summary>
    /// Creates a modified line with inline changes.
    /// </summary>
    /// <param name="originalLine">Line number in original content (1-based), or null if added.</param>
    /// <param name="proposedLine">Line number in proposed content (1-based), or null if removed.</param>
    /// <param name="content">The line text content.</param>
    /// <param name="inlineChanges">Character-level changes within the line.</param>
    /// <returns>A new <see cref="DiffLine"/> representing a modification.</returns>
    public static DiffLine Modified(
        int? originalLine,
        int? proposedLine,
        string content,
        IReadOnlyList<InlineChange>? inlineChanges = null) => new()
    {
        OriginalLineNumber = originalLine,
        ProposedLineNumber = proposedLine,
        Content = content,
        Type = DiffLineType.Modified,
        InlineChanges = inlineChanges
    };
}

/// <summary>
/// Type of change for a diff line.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2a.</para>
/// </remarks>
public enum DiffLineType
{
    /// <summary>
    /// Line is unchanged between original and proposed.
    /// Serves as context around changes.
    /// </summary>
    Unchanged = 0,

    /// <summary>
    /// Line was added in proposed content.
    /// Does not exist in original.
    /// </summary>
    Added = 1,

    /// <summary>
    /// Line was removed from original content.
    /// Does not exist in proposed.
    /// </summary>
    Removed = 2,

    /// <summary>
    /// Line was modified (exists in both but content differs).
    /// Has a PairedLine and may have InlineChanges.
    /// </summary>
    Modified = 3
}

/// <summary>
/// Which side of a side-by-side diff view.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2a.</para>
/// </remarks>
public enum DiffSide
{
    /// <summary>
    /// The original (left) side showing content before changes.
    /// </summary>
    Original = 0,

    /// <summary>
    /// The proposed (right) side showing content after changes.
    /// </summary>
    Proposed = 1
}
