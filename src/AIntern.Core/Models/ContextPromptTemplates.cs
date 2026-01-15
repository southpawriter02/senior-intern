namespace AIntern.Core.Models;

/// <summary>
/// Templates for context formatting in prompts and displays.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4b.</para>
/// </remarks>
public static class ContextPromptTemplates
{
    /// <summary>
    /// Template for single file context header.
    /// Placeholders: {FileName}, {Language}
    /// </summary>
    public const string FileHeaderTemplate = "### File: `{FileName}` ({Language})";

    /// <summary>
    /// Template for selection context header.
    /// Placeholders: {FileName}, {StartLine}, {EndLine}
    /// </summary>
    public const string SelectionHeaderTemplate = "### Selected Code from `{FileName}` (lines {StartLine}-{EndLine})";

    /// <summary>
    /// Template for context introduction in prompt.
    /// </summary>
    public const string PromptIntroduction = """
        I'm providing you with the following code context:

        """;

    /// <summary>
    /// Template for prompt conclusion with instructions.
    /// </summary>
    public const string PromptConclusion = """
        Please consider this context when responding to my question below.

        """;

    /// <summary>
    /// Template for path display.
    /// Placeholder: {Path}
    /// </summary>
    public const string PathTemplate = "_Path: {Path}_";

    /// <summary>
    /// Template for line range display.
    /// Placeholders: {StartLine}, {EndLine}
    /// </summary>
    public const string LineRangeTemplate = "**Lines {StartLine}-{EndLine}**";

    /// <summary>
    /// Template for truncation indicator in preview.
    /// Placeholder: {RemainingLines}
    /// </summary>
    public const string PreviewTruncation = "// ... ({RemainingLines} more lines)";

    /// <summary>
    /// Separator between multiple contexts.
    /// </summary>
    public const string ContextSeparator = "\n";

    /// <summary>
    /// Template for truncation notice.
    /// Placeholders: {TotalLines}, {TotalTokens}
    /// </summary>
    public const string TruncationNotice = "*Note: This file has been truncated to fit within context limits. The full file is {TotalLines} lines ({TotalTokens} tokens estimated).*";
}
