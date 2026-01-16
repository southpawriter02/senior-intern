namespace AIntern.Desktop.Models;

/// <summary>
/// Information about a code selection for attachment to chat context.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4f.</para>
/// </remarks>
public sealed class SelectionInfo
{
    /// <summary>
    /// Full path to the file containing the selection.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// File name for display.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Detected language identifier.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// The selected text content.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Starting line number (1-based).
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// Ending line number (1-based).
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// Starting column number (1-based).
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// Ending column number (1-based).
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    /// Whether this represents the entire file content.
    /// </summary>
    public bool IsFullFile { get; init; }

    /// <summary>
    /// Number of lines in the selection.
    /// </summary>
    public int LineCount => IsFullFile
        ? Content.Split('\n').Length
        : (EndLine >= StartLine ? EndLine - StartLine + 1 : 1);
}
