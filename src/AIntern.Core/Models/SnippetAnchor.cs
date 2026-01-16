namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET ANCHOR (v0.4.5c)                                                │
// │ Text-based anchor for locating insertion points in a file.              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a text-based anchor for locating insertion points in a file.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5c.</para>
/// </remarks>
public sealed class SnippetAnchor
{
    /// <summary>
    /// Type of anchor matching to use.
    /// </summary>
    public SnippetAnchorType AnchorType { get; init; } = SnippetAnchorType.ExactText;

    /// <summary>
    /// The pattern to match against.
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Which occurrence to match.
    /// Positive numbers: 1 = first, 2 = second, etc.
    /// Negative numbers: -1 = last, -2 = second-to-last, etc.
    /// </summary>
    public int Occurrence { get; init; } = 1;

    /// <summary>
    /// Line offset from the anchor.
    /// Positive: lines after the anchor.
    /// Negative: lines before the anchor.
    /// Zero: the anchor line itself.
    /// </summary>
    public int Offset { get; init; } = 0;

    /// <summary>
    /// Whether pattern matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; init; } = true;

    /// <summary>
    /// Whether to match whole words only.
    /// </summary>
    public bool WholeWordOnly { get; init; } = false;

    /// <summary>
    /// Description of what this anchor represents (for UI display).
    /// </summary>
    public string? Description { get; init; }

    // ═══════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an anchor for exact text matching.
    /// </summary>
    public static SnippetAnchor ExactText(string text, int occurrence = 1) => new()
    {
        AnchorType = SnippetAnchorType.ExactText,
        Pattern = text,
        Occurrence = occurrence,
        Description = $"Text: \"{TruncateForDisplay(text)}\""
    };

    /// <summary>
    /// Creates an anchor for regex pattern matching.
    /// </summary>
    public static SnippetAnchor RegexPattern(string pattern, int occurrence = 1) => new()
    {
        AnchorType = SnippetAnchorType.Regex,
        Pattern = pattern,
        Occurrence = occurrence,
        Description = $"Pattern: {TruncateForDisplay(pattern)}"
    };

    /// <summary>
    /// Creates an anchor for a function or method.
    /// </summary>
    public static SnippetAnchor Function(string functionName, int occurrence = 1) => new()
    {
        AnchorType = SnippetAnchorType.FunctionSignature,
        Pattern = functionName,
        Occurrence = occurrence,
        WholeWordOnly = true,
        Description = $"Function: {functionName}"
    };

    /// <summary>
    /// Creates an anchor for a class, struct, or interface.
    /// </summary>
    public static SnippetAnchor Class(string className, int occurrence = 1) => new()
    {
        AnchorType = SnippetAnchorType.ClassDeclaration,
        Pattern = className,
        Occurrence = occurrence,
        WholeWordOnly = true,
        Description = $"Class: {className}"
    };

    /// <summary>
    /// Creates an anchor for a comment marker.
    /// </summary>
    public static SnippetAnchor CommentMarker(string marker, int occurrence = 1) => new()
    {
        AnchorType = SnippetAnchorType.CommentMarker,
        Pattern = marker,
        Occurrence = occurrence,
        Description = $"Marker: {marker}"
    };

    /// <summary>
    /// Creates an anchor with line offset.
    /// </summary>
    public SnippetAnchor WithOffset(int lineOffset) => new()
    {
        AnchorType = AnchorType,
        Pattern = Pattern,
        Occurrence = Occurrence,
        Offset = lineOffset,
        CaseSensitive = CaseSensitive,
        WholeWordOnly = WholeWordOnly,
        Description = Description
    };

    private static string TruncateForDisplay(string text, int maxLength = 30) =>
        text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
}
