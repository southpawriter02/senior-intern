using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ I SNIPPET APPLY SERVICE (v0.4.5d)                                       │
// │ Service interface for applying code snippets to specific file locations.│
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service interface for applying code snippets to specific file locations.
/// Provides preview generation, indentation detection, and smart location suggestions.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5d.</para>
/// </remarks>
public interface ISnippetApplyService
{
    /// <summary>
    /// Applies a code snippet to a file using the specified options.
    /// Creates a backup if enabled in settings before making changes.
    /// </summary>
    /// <param name="filePath">Target file path (may not exist for new files)</param>
    /// <param name="snippetContent">The code snippet content to apply</param>
    /// <param name="options">Options controlling how the snippet is applied</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success/failure with metrics</returns>
    Task<SnippetApplyResult> ApplySnippetAsync(
        string filePath,
        string snippetContent,
        SnippetApplyOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a preview of applying a snippet without writing to disk.
    /// Useful for showing diffs to the user before confirming changes.
    /// </summary>
    /// <param name="filePath">Target file path</param>
    /// <param name="snippetContent">The code snippet content to apply</param>
    /// <param name="options">Options controlling how the snippet is applied</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview containing the result content, diff, and metrics</returns>
    Task<SnippetApplyPreview> PreviewSnippetAsync(
        string filePath,
        string snippetContent,
        SnippetApplyOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the indentation style used in a file.
    /// Analyzes leading whitespace patterns to determine tabs vs spaces.
    /// </summary>
    /// <param name="filePath">File to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detected indentation style with confidence score</returns>
    Task<IndentationStyle> DetectIndentationAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests an optimal location for inserting a code snippet.
    /// Uses pattern matching to find appropriate insertion points.
    /// </summary>
    /// <param name="filePath">Target file to analyze</param>
    /// <param name="snippetContent">Snippet to find a location for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Suggestion with confidence score, or null if no suggestion</returns>
    Task<SnippetLocationSuggestion?> SuggestLocationAsync(
        string filePath,
        string snippetContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates apply options against a specific file context.
    /// Checks that line ranges are valid, anchors can be found, etc.
    /// </summary>
    /// <param name="filePath">Target file path</param>
    /// <param name="options">Options to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any issues found</returns>
    Task<SnippetOptionsValidationResult> ValidateOptionsAsync(
        string filePath,
        SnippetApplyOptions options,
        CancellationToken cancellationToken = default);
}
