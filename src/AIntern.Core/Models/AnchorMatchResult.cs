namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ ANCHOR MATCH RESULT (v0.4.5c)                                           │
// │ Result of searching for an anchor in file content.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Result of searching for an anchor in file content.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5c.</para>
/// </remarks>
public sealed class AnchorMatchResult
{
    /// <summary>
    /// Whether a match was found.
    /// </summary>
    public bool Found { get; init; }

    /// <summary>
    /// The line number where the anchor was found (1-indexed).
    /// </summary>
    public int? MatchedLine { get; init; }

    /// <summary>
    /// The actual text that was matched.
    /// </summary>
    public string? MatchedText { get; init; }

    /// <summary>
    /// Confidence level of the match (0.0 to 1.0).
    /// 1.0 = exact match, lower values for fuzzy matches.
    /// </summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>
    /// Alternative line numbers if multiple matches were found.
    /// </summary>
    public IReadOnlyList<int> Alternatives { get; init; } = [];

    /// <summary>
    /// Error message if matching failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    // ═══════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether there are multiple possible matches.
    /// </summary>
    public bool HasAlternatives => Alternatives.Count > 0;

    /// <summary>
    /// Whether the match is ambiguous (multiple equally-valid options).
    /// </summary>
    public bool IsAmbiguous => HasAlternatives && Confidence < 1.0;

    /// <summary>
    /// Total number of matches found (including primary).
    /// </summary>
    public int TotalMatches => Found ? 1 + Alternatives.Count : Alternatives.Count;

    // ═══════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a successful match result.
    /// </summary>
    public static AnchorMatchResult Success(
        int line,
        string matchedText,
        double confidence = 1.0,
        IReadOnlyList<int>? alternatives = null) => new()
    {
        Found = true,
        MatchedLine = line,
        MatchedText = matchedText,
        Confidence = confidence,
        Alternatives = alternatives ?? []
    };

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static AnchorMatchResult NotFound(string? errorMessage = null) => new()
    {
        Found = false,
        ErrorMessage = errorMessage ?? "No match found"
    };

    /// <summary>
    /// Creates an ambiguous result with multiple matches.
    /// </summary>
    public static AnchorMatchResult Ambiguous(IReadOnlyList<int> matchedLines) => new()
    {
        Found = false,
        Confidence = 0.5,
        Alternatives = matchedLines,
        ErrorMessage = $"Multiple matches found ({matchedLines.Count})"
    };
}
