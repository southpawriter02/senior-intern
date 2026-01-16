namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET APPLY OPTIONS (v0.4.5c)                                         │
// │ Configuration for applying a snippet to a file.                         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration for applying a code snippet to a file.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5c.</para>
/// </remarks>
public sealed record SnippetApplyOptions
{
    // ═══════════════════════════════════════════════════════════════
    // Location Specification
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// How the snippet should be inserted.
    /// </summary>
    public SnippetInsertMode InsertMode { get; init; } = SnippetInsertMode.ReplaceFile;

    /// <summary>
    /// Target line for InsertBefore/InsertAfter modes (1-indexed).
    /// </summary>
    public int? TargetLine { get; init; }

    /// <summary>
    /// Line range to replace for Replace mode.
    /// </summary>
    public LineRange? ReplaceRange { get; init; }

    /// <summary>
    /// Text-based anchor for locating insertion point.
    /// </summary>
    public SnippetAnchor? Anchor { get; init; }

    // ═══════════════════════════════════════════════════════════════
    // Formatting Options
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether to preserve the indentation style of the target location.
    /// </summary>
    public bool PreserveIndentation { get; init; } = true;

    /// <summary>
    /// Override indentation with this string (e.g., "    " or "\t").
    /// </summary>
    public string? IndentationOverride { get; init; }

    /// <summary>
    /// Number of indent levels to add to the snippet.
    /// </summary>
    public int IndentLevelOffset { get; init; } = 0;

    /// <summary>
    /// Whether to add a blank line before the snippet.
    /// </summary>
    public bool AddBlankLineBefore { get; init; }

    /// <summary>
    /// Whether to add a blank line after the snippet.
    /// </summary>
    public bool AddBlankLineAfter { get; init; }

    /// <summary>
    /// Whether to trim trailing whitespace from each line.
    /// </summary>
    public bool TrimTrailingWhitespace { get; init; } = true;

    /// <summary>
    /// Whether to normalize line endings to match the file.
    /// </summary>
    public bool NormalizeLineEndings { get; init; } = true;

    // ═══════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Replace the entire file content.
    /// </summary>
    public static SnippetApplyOptions FullReplace() => new()
    {
        InsertMode = SnippetInsertMode.ReplaceFile
    };

    /// <summary>
    /// Replace specific lines in the file.
    /// </summary>
    public static SnippetApplyOptions ReplaceLines(int startLine, int endLine) => new()
    {
        InsertMode = SnippetInsertMode.Replace,
        ReplaceRange = new LineRange(startLine, endLine)
    };

    /// <summary>
    /// Replace a range of lines in the file.
    /// </summary>
    public static SnippetApplyOptions ReplaceLines(LineRange range) => new()
    {
        InsertMode = SnippetInsertMode.Replace,
        ReplaceRange = range
    };

    /// <summary>
    /// Insert after a specific line.
    /// </summary>
    public static SnippetApplyOptions InsertAfterLine(int line) => new()
    {
        InsertMode = SnippetInsertMode.InsertAfter,
        TargetLine = line
    };

    /// <summary>
    /// Insert before a specific line.
    /// </summary>
    public static SnippetApplyOptions InsertBeforeLine(int line) => new()
    {
        InsertMode = SnippetInsertMode.InsertBefore,
        TargetLine = line
    };

    /// <summary>
    /// Append to the end of the file.
    /// </summary>
    public static SnippetApplyOptions AppendToFile(bool addBlankLineBefore = true) => new()
    {
        InsertMode = SnippetInsertMode.Append,
        AddBlankLineBefore = addBlankLineBefore
    };

    /// <summary>
    /// Prepend to the beginning of the file.
    /// </summary>
    public static SnippetApplyOptions PrependToFile(bool addBlankLineAfter = true) => new()
    {
        InsertMode = SnippetInsertMode.Prepend,
        AddBlankLineAfter = addBlankLineAfter
    };

    /// <summary>
    /// Insert using a text-based anchor.
    /// </summary>
    public static SnippetApplyOptions FromAnchor(
        SnippetAnchor anchor,
        SnippetInsertMode mode = SnippetInsertMode.InsertAfter) => new()
    {
        InsertMode = mode,
        Anchor = anchor
    };

    // ═══════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates that required options are set for the insert mode.
    /// </summary>
    public (bool IsValid, string? Error) Validate()
    {
        return InsertMode switch
        {
            SnippetInsertMode.Replace when ReplaceRange is null || !ReplaceRange.Value.IsValid =>
                (false, "ReplaceRange must be specified for Replace mode"),
            SnippetInsertMode.InsertBefore when TargetLine is null or <= 0 && Anchor is null =>
                (false, "TargetLine or Anchor must be specified for InsertBefore mode"),
            SnippetInsertMode.InsertAfter when TargetLine is null or <= 0 && Anchor is null =>
                (false, "TargetLine or Anchor must be specified for InsertAfter mode"),
            _ => (true, null)
        };
    }
}
