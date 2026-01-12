namespace AIntern.Core.Utilities;

/// <summary>
/// Estimates token count for content to help manage LLM context budgets.
/// </summary>
public static class TokenEstimator
{
    /// <summary>
    /// Average characters per token. Varies by language and content type.
    /// Code typically has more symbols resulting in more tokens per character.
    /// </summary>
    private const double DefaultCharsPerToken = 3.5;

    /// <summary>
    /// Estimates the number of tokens in the given content.
    /// </summary>
    public static int Estimate(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        return (int)Math.Ceiling(content.Length / DefaultCharsPerToken);
    }

    /// <summary>
    /// Estimates tokens with language-specific adjustment.
    /// Code languages tend to have more tokens per character due to symbols.
    /// </summary>
    public static int Estimate(string? content, string? language)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var multiplier = GetLanguageMultiplier(language);
        return (int)Math.Ceiling(content.Length / DefaultCharsPerToken * multiplier);
    }

    /// <summary>
    /// Gets the language-specific token multiplier.
    /// </summary>
    public static double GetLanguageMultiplier(string? language)
    {
        return language?.ToLowerInvariant() switch
        {
            // Symbol-heavy languages (more tokens per char)
            "csharp" or "c#" => 1.2,
            "java" => 1.2,
            "cpp" or "c++" => 1.25,
            "c" => 1.2,
            "typescript" => 1.15,
            "javascript" => 1.1,
            "rust" => 1.2,
            "go" => 1.15,

            // Readable/whitespace languages
            "python" => 1.0,
            "ruby" => 1.0,
            "yaml" => 0.95,

            // Verbose markup
            "json" => 1.3,
            "xml" => 1.35,
            "html" => 1.3,

            // Prose-heavy
            "markdown" or "md" => 0.9,
            "text" or "txt" => 0.85,

            // Default
            _ => 1.0
        };
    }

    /// <summary>
    /// Estimates the maximum content length for a given token budget.
    /// </summary>
    public static int MaxContentLength(int tokenBudget, string? language = null)
    {
        var multiplier = GetLanguageMultiplier(language);
        return (int)(tokenBudget * DefaultCharsPerToken / multiplier);
    }

    /// <summary>
    /// Checks if content exceeds a token budget.
    /// </summary>
    public static bool ExceedsBudget(string content, int tokenBudget, string? language = null)
        => Estimate(content, language) > tokenBudget;
}
