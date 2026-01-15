namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

/// <summary>
/// Service for estimating token counts in text content.
/// </summary>
/// <remarks>
/// <para>
/// Provides methods for estimating how many tokens a piece of text will
/// consume when sent to an LLM, supporting multiple estimation algorithms
/// with different speed/accuracy trade-offs.
/// </para>
/// <para>Added in v0.3.4a.</para>
/// </remarks>
public interface ITokenEstimationService
{
    /// <summary>
    /// Estimates the token count for the given content using the default method (WordBased).
    /// </summary>
    /// <param name="content">The text content to estimate.</param>
    /// <returns>Estimated token count.</returns>
    int EstimateTokens(string content);

    /// <summary>
    /// Estimates token count using a specific estimation method.
    /// </summary>
    /// <param name="content">The text content to estimate.</param>
    /// <param name="method">The estimation algorithm to use.</param>
    /// <returns>Estimated token count.</returns>
    int EstimateTokens(string content, TokenEstimationMethod method);

    /// <summary>
    /// Gets the recommended token limit for context based on model capabilities.
    /// </summary>
    /// <returns>Recommended maximum tokens for context.</returns>
    int GetRecommendedContextLimit();

    /// <summary>
    /// Checks if adding content would exceed the context limit.
    /// </summary>
    /// <param name="currentTokens">Current token count.</param>
    /// <param name="newContent">New content to add.</param>
    /// <returns>True if adding would exceed limit.</returns>
    bool WouldExceedLimit(int currentTokens, string newContent);

    /// <summary>
    /// Truncates content to fit within a token limit, respecting word boundaries.
    /// </summary>
    /// <param name="content">Content to truncate.</param>
    /// <param name="maxTokens">Maximum tokens allowed.</param>
    /// <returns>Truncated content with indicator if truncation occurred.</returns>
    string TruncateToTokenLimit(string content, int maxTokens);

    /// <summary>
    /// Gets a breakdown of token usage across multiple content items.
    /// </summary>
    /// <param name="contents">Collection of content items.</param>
    /// <returns>Usage breakdown with totals and per-item counts.</returns>
    TokenUsageBreakdown GetUsageBreakdown(IEnumerable<string> contents);

    /// <summary>
    /// Gets a breakdown with labeled items.
    /// </summary>
    /// <param name="labeledContents">Collection of (label, content) pairs.</param>
    /// <returns>Usage breakdown with labeled items.</returns>
    TokenUsageBreakdown GetUsageBreakdown(IEnumerable<(string Label, string Content)> labeledContents);
}
