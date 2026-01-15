namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

/// <summary>
/// Service for formatting file contexts into LLM prompts and UI displays.
/// </summary>
/// <remarks>
/// <para>
/// Provides methods to format attached file contexts for different purposes:
/// </para>
/// <list type="bullet">
///   <item><description>FormatForPrompt: Structured LLM prompt with code blocks</description></item>
///   <item><description>FormatForDisplay: UI-friendly markdown with previews</description></item>
///   <item><description>FormatForStorage: JSON metadata for persistence</description></item>
/// </list>
/// <para>Added in v0.3.4b.</para>
/// </remarks>
public interface IContextFormatter
{
    /// <summary>
    /// Formats contexts for inclusion in an LLM prompt.
    /// Creates a structured prompt with file headers, code blocks, and instructions.
    /// </summary>
    /// <param name="contexts">Collection of file contexts to format.</param>
    /// <returns>Formatted prompt string ready for LLM consumption.</returns>
    string FormatForPrompt(IEnumerable<FileContext> contexts);

    /// <summary>
    /// Formats a single context for prompt inclusion.
    /// </summary>
    /// <param name="context">The file context to format.</param>
    /// <returns>Formatted context with header and code block.</returns>
    string FormatSingleContext(FileContext context);

    /// <summary>
    /// Formats contexts for display in the chat history UI.
    /// </summary>
    /// <param name="contexts">Collection of file contexts to format.</param>
    /// <param name="expanded">If true, shows full content; if false, shows truncated preview.</param>
    /// <returns>Markdown-formatted display string.</returns>
    string FormatForDisplay(IEnumerable<FileContext> contexts, bool expanded = false);

    /// <summary>
    /// Creates a markdown code block with syntax highlighting hint.
    /// </summary>
    /// <param name="content">The code content.</param>
    /// <param name="language">Language identifier for syntax highlighting.</param>
    /// <returns>Formatted code block string.</returns>
    string FormatCodeBlock(string content, string? language);

    /// <summary>
    /// Formats the context header with file metadata.
    /// </summary>
    /// <param name="context">The file context.</param>
    /// <returns>Formatted header with filename, language, and path.</returns>
    string FormatContextHeader(FileContext context);

    /// <summary>
    /// Formats contexts for storage in message history.
    /// Stores metadata without full content (uses hash for change detection).
    /// </summary>
    /// <param name="contexts">Collection of file contexts to serialize.</param>
    /// <returns>JSON string for persistence.</returns>
    string FormatForStorage(IEnumerable<FileContext> contexts);
}
