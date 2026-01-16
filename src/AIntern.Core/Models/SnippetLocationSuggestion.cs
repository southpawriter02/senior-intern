namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET LOCATION SUGGESTION (v0.4.5d)                                   │
// │ AI-suggested location for inserting a snippet.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// AI-suggested location for inserting a snippet.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5d.</para>
/// </remarks>
public sealed record SnippetLocationSuggestion
{
    /// <summary>
    /// Suggested insertion mode.
    /// </summary>
    public SnippetInsertMode SuggestedMode { get; init; }

    /// <summary>
    /// Target line for InsertBefore/InsertAfter modes.
    /// </summary>
    public int? SuggestedLine { get; init; }

    /// <summary>
    /// Line range for Replace mode.
    /// </summary>
    public LineRange? SuggestedRange { get; init; }

    /// <summary>
    /// Confidence of the suggestion (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Human-readable reason for the suggestion.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Context text that was matched (for display).
    /// </summary>
    public string? MatchedContext { get; init; }

    /// <summary>
    /// Whether this is a high-confidence suggestion.
    /// </summary>
    public bool IsHighConfidence => Confidence >= 0.7;

    /// <summary>
    /// Converts to SnippetApplyOptions.
    /// </summary>
    public SnippetApplyOptions ToApplyOptions() => SuggestedMode switch
    {
        SnippetInsertMode.Replace when SuggestedRange.HasValue =>
            SnippetApplyOptions.ReplaceLines(SuggestedRange.Value),
        SnippetInsertMode.InsertBefore when SuggestedLine.HasValue =>
            SnippetApplyOptions.InsertBeforeLine(SuggestedLine.Value),
        SnippetInsertMode.InsertAfter when SuggestedLine.HasValue =>
            SnippetApplyOptions.InsertAfterLine(SuggestedLine.Value),
        SnippetInsertMode.Append => SnippetApplyOptions.AppendToFile(),
        SnippetInsertMode.Prepend => SnippetApplyOptions.PrependToFile(),
        _ => SnippetApplyOptions.FullReplace()
    };
}
