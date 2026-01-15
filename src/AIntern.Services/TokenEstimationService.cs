namespace AIntern.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for estimating token counts using various methods.
/// </summary>
/// <remarks>
/// <para>
/// Provides three estimation algorithms with different speed/accuracy trade-offs:
/// </para>
/// <list type="bullet">
///   <item><description>CharacterBased: Fast O(1), ~3.5 chars/token</description></item>
///   <item><description>WordBased: Balanced O(n), considers words + punctuation</description></item>
///   <item><description>BpeApproximate: Accurate O(n×m), simulates BPE tokenization</description></item>
/// </list>
/// <para>Added in v0.3.4a.</para>
/// </remarks>
public sealed partial class TokenEstimationService : ITokenEstimationService
{
    #region Constants

    /// <summary>
    /// Default context limit - can be adjusted based on model.
    /// </summary>
    private const int DefaultContextLimit = 8000;

    /// <summary>
    /// Character-based estimation ratio (conservative for code).
    /// </summary>
    private const double CharsPerToken = 3.5;

    /// <summary>
    /// Word-based estimation: tokens per word ratio.
    /// </summary>
    private const double WordsPerToken = 0.75;

    /// <summary>
    /// Weight for punctuation in word-based estimation.
    /// </summary>
    private const double PunctuationWeight = 0.5;

    /// <summary>
    /// Truncation indicator appended to truncated content.
    /// </summary>
    private const string TruncationIndicator = "\n... (truncated)";

    #endregion

    #region Fields

    private readonly ILogger<TokenEstimationService>? _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEstimationService"/>.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public TokenEstimationService(ILogger<TokenEstimationService>? logger = null)
    {
        _logger = logger;
    }

    #endregion

    #region ITokenEstimationService Implementation

    /// <inheritdoc />
    public int EstimateTokens(string content)
    {
        return EstimateTokens(content, TokenEstimationMethod.WordBased);
    }

    /// <inheritdoc />
    public int EstimateTokens(string content, TokenEstimationMethod method)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        var result = method switch
        {
            TokenEstimationMethod.CharacterBased => EstimateByCharacters(content),
            TokenEstimationMethod.WordBased => EstimateByWords(content),
            TokenEstimationMethod.BpeApproximate => EstimateByBpe(content),
            _ => EstimateByWords(content)
        };

        _logger?.LogTrace(
            "[TOKEN] Estimated {Tokens} tokens for {Length} chars using {Method}",
            result, content.Length, method);

        return result;
    }

    /// <inheritdoc />
    public int GetRecommendedContextLimit()
    {
        return DefaultContextLimit;
    }

    /// <inheritdoc />
    public bool WouldExceedLimit(int currentTokens, string newContent)
    {
        var newTokens = EstimateTokens(newContent);
        var wouldExceed = (currentTokens + newTokens) > GetRecommendedContextLimit();

        _logger?.LogDebug(
            "[TOKEN] Would exceed limit: {Result} (current={Current}, new={New}, limit={Limit})",
            wouldExceed, currentTokens, newTokens, GetRecommendedContextLimit());

        return wouldExceed;
    }

    /// <inheritdoc />
    public string TruncateToTokenLimit(string content, int maxTokens)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content ?? string.Empty;
        }

        var currentTokens = EstimateTokens(content);
        if (currentTokens <= maxTokens)
        {
            return content;
        }

        _logger?.LogDebug(
            "[TOKEN] Truncating from {Current} to {Max} tokens",
            currentTokens, maxTokens);

        // Estimate characters needed for target tokens
        var targetChars = (int)(maxTokens * CharsPerToken);

        // Binary search for optimal truncation point
        var low = 0;
        var high = Math.Min(content.Length, targetChars + 100);

        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            var truncated = content[..mid];
            var tokens = EstimateTokens(truncated);

            if (tokens <= maxTokens)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        // Truncate at word boundary if possible
        var result = content[..low];
        var lastSpace = result.LastIndexOf(' ');
        if (lastSpace > low * 0.8) // Only if we don't lose too much
        {
            result = result[..lastSpace];
        }

        return result + TruncationIndicator;
    }

    /// <inheritdoc />
    public TokenUsageBreakdown GetUsageBreakdown(IEnumerable<string> contents)
    {
        var items = contents
            .Select((content, index) => new TokenUsageItem(
                $"Item {index + 1}",
                EstimateTokens(content)
            ))
            .ToList();

        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = items.Sum(i => i.Tokens),
            RecommendedLimit = GetRecommendedContextLimit(),
            Items = items
        };

        _logger?.LogDebug(
            "[TOKEN] Usage breakdown: {Total}/{Limit} tokens ({Percentage:F1}%)",
            breakdown.TotalTokens, breakdown.RecommendedLimit, breakdown.UsagePercentage);

        return breakdown;
    }

    /// <inheritdoc />
    public TokenUsageBreakdown GetUsageBreakdown(IEnumerable<(string Label, string Content)> labeledContents)
    {
        var items = labeledContents
            .Select(item => new TokenUsageItem(
                item.Label,
                EstimateTokens(item.Content)
            ))
            .ToList();

        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = items.Sum(i => i.Tokens),
            RecommendedLimit = GetRecommendedContextLimit(),
            Items = items
        };

        _logger?.LogDebug(
            "[TOKEN] Labeled usage breakdown: {Total}/{Limit} tokens ({Percentage:F1}%)",
            breakdown.TotalTokens, breakdown.RecommendedLimit, breakdown.UsagePercentage);

        return breakdown;
    }

    #endregion

    #region Character-Based Estimation

    /// <summary>
    /// Estimates tokens using simple character count (~3.5 chars/token).
    /// </summary>
    private static int EstimateByCharacters(string content)
    {
        // Simple: ~3.5 characters per token (conservative for code)
        return (int)Math.Ceiling(content.Length / CharsPerToken);
    }

    #endregion

    #region Word-Based Estimation

    /// <summary>
    /// Regex pattern for matching words (sequences of word characters).
    /// </summary>
    [GeneratedRegex(@"\b\w+\b")]
    private static partial Regex WordPattern();

    /// <summary>
    /// Regex pattern for matching punctuation and special characters.
    /// </summary>
    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex PunctuationPattern();

    /// <summary>
    /// Regex pattern for matching whitespace sequences (indentation).
    /// </summary>
    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex WhitespaceSequencePattern();

    /// <summary>
    /// Estimates tokens using word, punctuation, and whitespace analysis.
    /// </summary>
    private static int EstimateByWords(string content)
    {
        // Count words (sequences of word characters)
        var wordCount = WordPattern().Matches(content).Count;

        // Count punctuation and special characters
        var punctuationCount = PunctuationPattern().Matches(content).Count;

        // Count newlines (often separate tokens)
        var newlineCount = content.Count(c => c == '\n');

        // Count whitespace sequences > 1 (indentation)
        var whitespaceSequences = WhitespaceSequencePattern().Matches(content).Count;

        // Weighted combination
        var estimate = (int)Math.Ceiling(
            wordCount / WordsPerToken +
            punctuationCount * PunctuationWeight +
            newlineCount * 0.5 +
            whitespaceSequences * 0.3
        );

        return Math.Max(1, estimate);
    }

    #endregion

    #region BPE-Approximate Estimation

    /// <summary>
    /// Common programming tokens that are typically single BPE tokens.
    /// </summary>
    private static readonly string[] CommonTokens =
    {
        // Keywords
        "public", "private", "protected", "internal", "static", "readonly",
        "void", "class", "interface", "struct", "enum", "record",
        "async", "await", "return", "yield", "throw",
        "string", "int", "long", "float", "double", "bool", "object",
        "var", "const", "let", "function", "export", "import", "from",
        "namespace", "using", "override", "virtual", "abstract", "sealed",
        "null", "true", "false", "this", "base", "new", "typeof",
        "if", "else", "while", "for", "foreach", "switch", "case",
        "try", "catch", "finally", "where", "select", "from",

        // Operators (multi-character)
        "=>", "->", "==", "!=", "<=", ">=", "&&", "||",
        "++", "--", "+=", "-=", "*=", "/=", "%=", "&=", "|=",
        "<<", ">>", "::", "..", "??", "?.", "?[",

        // Comments
        "/**", "*/", "///", "//", "/*"
    };

    /// <summary>
    /// Estimates tokens by simulating BPE tokenization.
    /// </summary>
    private static int EstimateByBpe(string content)
    {
        var tokens = 0;
        var i = 0;

        while (i < content.Length)
        {
            var remaining = content.AsSpan(i);

            // Try to match common multi-character tokens first
            if (TryMatchCommonToken(remaining, out var matchLength))
            {
                tokens++;
                i += matchLength;
                continue;
            }

            var c = content[i];

            if (char.IsWhiteSpace(c))
            {
                tokens++;
                // Skip consecutive whitespace of same type
                while (i + 1 < content.Length && content[i + 1] == c)
                {
                    i++;
                }
            }
            else if (char.IsLetterOrDigit(c))
            {
                var wordStart = i;
                while (i < content.Length && char.IsLetterOrDigit(content[i]))
                {
                    i++;
                }
                var wordLength = i - wordStart;

                // Estimate tokens for word (longer words = more tokens)
                tokens += Math.Max(1, (wordLength + 3) / 4);
                continue;
            }
            else
            {
                // Punctuation/symbols - usually single tokens
                tokens++;
            }

            i++;
        }

        return Math.Max(1, tokens);
    }

    /// <summary>
    /// Attempts to match a common programming token at the start of the span.
    /// </summary>
    private static bool TryMatchCommonToken(ReadOnlySpan<char> text, out int length)
    {
        foreach (var token in CommonTokens)
        {
            if (text.StartsWith(token.AsSpan()))
            {
                // Ensure it's a complete match (not part of a longer word)
                if (token.Length >= text.Length ||
                    !char.IsLetterOrDigit(text[token.Length]) ||
                    !char.IsLetterOrDigit(token[^1]))
                {
                    length = token.Length;
                    return true;
                }
            }
        }

        length = 0;
        return false;
    }

    #endregion
}
