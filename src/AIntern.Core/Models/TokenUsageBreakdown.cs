namespace AIntern.Core.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Breakdown of token usage across multiple content items.
/// </summary>
/// <remarks>
/// <para>
/// Provides a summary of token usage with per-item breakdown,
/// usage percentages, and limit warnings for context attachment.
/// </para>
/// <para>Added in v0.3.4a.</para>
/// </remarks>
public sealed class TokenUsageBreakdown
{
    /// <summary>
    /// Total tokens across all items.
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Recommended maximum token limit.
    /// </summary>
    public int RecommendedLimit { get; init; }

    /// <summary>
    /// Usage as a percentage of the limit (0-100+).
    /// </summary>
    /// <remarks>
    /// May exceed 100% if over limit.
    /// </remarks>
    public double UsagePercentage => RecommendedLimit > 0
        ? (double)TotalTokens / RecommendedLimit * 100
        : 0;

    /// <summary>
    /// Whether total tokens exceed the recommended limit.
    /// </summary>
    public bool IsOverLimit => TotalTokens > RecommendedLimit;

    /// <summary>
    /// Number of tokens remaining before hitting limit.
    /// </summary>
    /// <remarks>
    /// Returns 0 if already over limit.
    /// </remarks>
    public int RemainingTokens => Math.Max(0, RecommendedLimit - TotalTokens);

    /// <summary>
    /// Whether usage is above the warning threshold (80%) but not over limit.
    /// </summary>
    public bool IsWarning => UsagePercentage >= 80 && !IsOverLimit;

    /// <summary>
    /// Per-item token breakdown.
    /// </summary>
    public IReadOnlyList<TokenUsageItem> Items { get; init; } = Array.Empty<TokenUsageItem>();
}

/// <summary>
/// Token usage for a single content item.
/// </summary>
/// <param name="Label">Display label for the item.</param>
/// <param name="Tokens">Estimated token count.</param>
public sealed record TokenUsageItem(string Label, int Tokens);
