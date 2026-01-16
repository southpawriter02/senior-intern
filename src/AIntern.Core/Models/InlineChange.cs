namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INLINE CHANGE (v0.4.2a)                                                  │
// │ Character-level change within a modified line.                           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a character-level change within a modified line.
/// Used to highlight exactly which characters changed when a line was modified.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2a.</para>
/// <para>
/// InlineChanges are computed by performing a word-level or character-level
/// diff between the original and proposed versions of a modified line.
/// </para>
/// </remarks>
/// <example>
/// Original: "var count = 10;"
/// Proposed: "var count = 20;"
///
/// InlineChanges on original line:
///   - Removed: StartColumn=12, Length=2, Text="10"
///
/// InlineChanges on proposed line:
///   - Added: StartColumn=12, Length=2, Text="20"
/// </example>
public sealed class InlineChange
{
    /// <summary>
    /// Starting column position (0-based index into the line content).
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// Length of the changed text segment in characters.
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// The changed text content.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Type of inline change (Added, Removed, or Unchanged).
    /// </summary>
    public InlineChangeType Type { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// End column position (exclusive, 0-based).
    /// </summary>
    public int EndColumn => StartColumn + Length;

    /// <summary>
    /// Whether this represents an actual change (not unchanged context).
    /// </summary>
    public bool IsChange => Type != InlineChangeType.Unchanged;

    // ═══════════════════════════════════════════════════════════════════════
    // Static Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an added inline change.
    /// </summary>
    /// <param name="startColumn">Starting column position (0-based).</param>
    /// <param name="text">The added text content.</param>
    /// <returns>A new <see cref="InlineChange"/> representing an addition.</returns>
    public static InlineChange Added(int startColumn, string text) => new()
    {
        StartColumn = startColumn,
        Length = text.Length,
        Text = text,
        Type = InlineChangeType.Added
    };

    /// <summary>
    /// Creates a removed inline change.
    /// </summary>
    /// <param name="startColumn">Starting column position (0-based).</param>
    /// <param name="text">The removed text content.</param>
    /// <returns>A new <see cref="InlineChange"/> representing a removal.</returns>
    public static InlineChange Removed(int startColumn, string text) => new()
    {
        StartColumn = startColumn,
        Length = text.Length,
        Text = text,
        Type = InlineChangeType.Removed
    };

    /// <summary>
    /// Creates an unchanged inline segment (context).
    /// </summary>
    /// <param name="startColumn">Starting column position (0-based).</param>
    /// <param name="text">The unchanged text content.</param>
    /// <returns>A new <see cref="InlineChange"/> representing unchanged context.</returns>
    public static InlineChange Unchanged(int startColumn, string text) => new()
    {
        StartColumn = startColumn,
        Length = text.Length,
        Text = text,
        Type = InlineChangeType.Unchanged
    };
}

/// <summary>
/// Type of character-level change within a line.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2a.</para>
/// </remarks>
public enum InlineChangeType
{
    /// <summary>
    /// Text segment is unchanged (context between changes).
    /// </summary>
    Unchanged = 0,

    /// <summary>
    /// Text segment was added.
    /// </summary>
    Added = 1,

    /// <summary>
    /// Text segment was removed.
    /// </summary>
    Removed = 2
}
