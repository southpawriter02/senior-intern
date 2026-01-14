namespace AIntern.Core.Utilities;

/// <summary>
/// Estimates token count for content to help manage LLM context budgets.
/// Provides approximate estimates sufficient for UI display and budget warnings.
/// </summary>
/// <remarks>
/// <para>
/// Token estimation is inherently approximate since actual tokenization depends
/// on the specific model and tokenizer used. This utility provides reasonable
/// estimates based on character count with language-specific adjustments.
/// </para>
/// <para>
/// For code, the average is roughly 3-4 characters per token due to symbols,
/// keywords, and identifiers. Prose tends to have more characters per token.
/// </para>
/// </remarks>
public static class TokenEstimator
{
    /// <summary>
    /// Average characters per token for general content.
    /// Code typically has more symbols resulting in more tokens per character.
    /// </summary>
    private const double DefaultCharsPerToken = 3.5;

    /// <summary>
    /// Estimates the number of tokens in the given content.
    /// </summary>
    /// <param name="content">The text content to estimate.</param>
    /// <returns>Estimated token count, or 0 if content is null/empty.</returns>
    public static int Estimate(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        return (int)Math.Ceiling(content.Length / DefaultCharsPerToken);
    }

    /// <summary>
    /// Estimates tokens with language-specific adjustment.
    /// Different languages have varying token densities based on syntax.
    /// </summary>
    /// <param name="content">The text content to estimate.</param>
    /// <param name="language">Language identifier for adjustment.</param>
    /// <returns>Estimated token count with language adjustment.</returns>
    public static int Estimate(string? content, string? language)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var multiplier = GetLanguageMultiplier(language);
        return (int)Math.Ceiling(content.Length / DefaultCharsPerToken * multiplier);
    }

    /// <summary>
    /// Gets the language-specific token multiplier.
    /// Higher values indicate more tokens per character (e.g., symbol-heavy code).
    /// </summary>
    /// <param name="language">Language identifier.</param>
    /// <returns>Multiplier for token estimation (1.0 = baseline).</returns>
    public static double GetLanguageMultiplier(string? language)
    {
        return language?.ToLowerInvariant() switch
        {
            // Symbol-heavy languages (more tokens per character)
            "csharp" or "c#" => 1.2,
            "java" => 1.2,
            "cpp" or "c++" => 1.25,
            "c" => 1.2,
            "rust" => 1.2,
            "go" => 1.15,
            "typescript" => 1.15,
            "javascript" => 1.1,
            "swift" => 1.15,
            "kotlin" => 1.15,

            // Whitespace-significant / readable languages
            "python" => 1.0,
            "ruby" => 1.0,
            "yaml" or "yml" => 0.95,

            // Verbose markup/data formats
            "json" => 1.3,
            "xml" => 1.35,
            "html" => 1.3,

            // Prose-heavy formats
            "markdown" or "md" => 0.9,
            "text" or "txt" or "plaintext" => 0.85,
            "restructuredtext" => 0.9,

            // Shell/scripting
            "shellscript" or "bash" or "sh" => 1.05,
            "powershell" => 1.1,

            // SQL
            "sql" => 1.15,

            // Default
            _ => 1.0
        };
    }

    /// <summary>
    /// Estimates the maximum content length for a given token budget.
    /// Useful for determining how much content can fit in a context window.
    /// </summary>
    /// <param name="tokenBudget">Maximum tokens available.</param>
    /// <param name="language">Optional language for adjustment.</param>
    /// <returns>Approximate maximum character count.</returns>
    public static int MaxContentLength(int tokenBudget, string? language = null)
    {
        if (tokenBudget <= 0)
            return 0;

        var multiplier = GetLanguageMultiplier(language);
        return (int)(tokenBudget * DefaultCharsPerToken / multiplier);
    }

    /// <summary>
    /// Checks if content exceeds a token budget.
    /// </summary>
    /// <param name="content">Content to check.</param>
    /// <param name="tokenBudget">Maximum allowed tokens.</param>
    /// <param name="language">Optional language for adjustment.</param>
    /// <returns>True if estimated tokens exceed the budget.</returns>
    public static bool ExceedsBudget(string? content, int tokenBudget, string? language = null)
        => Estimate(content, language) > tokenBudget;

    /// <summary>
    /// Gets the base characters-per-token ratio used for estimation.
    /// </summary>
    public static double BaseCharsPerToken => DefaultCharsPerToken;
}
