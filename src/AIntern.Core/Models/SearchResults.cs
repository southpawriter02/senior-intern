namespace AIntern.Core.Models;

/// <summary>
/// Encapsulates the complete result of a full-text search operation.
/// </summary>
/// <remarks>
/// <para>
/// This record bundles the search results with metadata about the search:
/// </para>
/// <list type="bullet">
///   <item><description><b>Results:</b> Ordered list of matching items</description></item>
///   <item><description><b>TotalCount:</b> Total matches found (may exceed Results.Count)</description></item>
///   <item><description><b>Query:</b> The original query for reference</description></item>
///   <item><description><b>SearchDuration:</b> How long the search took</description></item>
/// </list>
/// <para>
/// Results are pre-sorted by relevance (Rank ascending, since lower is better in BM25).
/// </para>
/// <para>
/// <b>Pagination:</b> When TotalCount exceeds Results.Count, use <see cref="HasMoreResults"/>
/// to determine if more results are available. The <see cref="TruncatedCount"/> property
/// indicates how many results were omitted.
/// </para>
/// <para>Added in v0.2.5a.</para>
/// </remarks>
/// <param name="Results">Ordered list of search results, sorted by relevance.</param>
/// <param name="TotalCount">Total number of matches found (before MaxResults limit).</param>
/// <param name="Query">The original search query.</param>
/// <param name="SearchDuration">Time taken to execute the search.</param>
/// <example>
/// Processing search results:
/// <code>
/// var results = await dbContext.SearchAsync(SearchQuery.Simple("hello"));
///
/// if (results.HasResults)
/// {
///     foreach (var result in results.Results)
///     {
///         Console.WriteLine($"{result.TypeLabel}: {result.Title}");
///     }
///
///     if (results.HasMoreResults)
///     {
///         Console.WriteLine($"...and {results.TruncatedCount} more results");
///     }
///
///     Console.WriteLine($"Search completed in {results.SearchDurationMs:F2}ms");
/// }
/// </code>
/// </example>
public sealed record SearchResults(
    IReadOnlyList<SearchResult> Results,
    int TotalCount,
    SearchQuery Query,
    TimeSpan SearchDuration)
{
    #region Static Members

    /// <summary>
    /// Creates an empty search result (no matches).
    /// </summary>
    /// <param name="query">The query that produced no results.</param>
    /// <returns>A <see cref="SearchResults"/> instance with zero results.</returns>
    /// <remarks>
    /// Used when:
    /// <list type="bullet">
    ///   <item><description>The query is invalid (empty or whitespace)</description></item>
    ///   <item><description>No content type filters are enabled</description></item>
    ///   <item><description>No matches were found in the database</description></item>
    /// </list>
    /// </remarks>
    public static SearchResults Empty(SearchQuery query) => new(
        Array.Empty<SearchResult>(),
        TotalCount: 0,
        Query: query,
        SearchDuration: TimeSpan.Zero);

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether any results were found.
    /// </summary>
    /// <value>True if <see cref="Results"/> contains at least one item.</value>
    /// <remarks>
    /// Use this property for conditional rendering in the UI.
    /// </remarks>
    public bool HasResults => Results.Count > 0;

    /// <summary>
    /// Gets whether there are more results beyond what was returned.
    /// </summary>
    /// <value>
    /// True if <see cref="TotalCount"/> exceeds the number of returned results.
    /// </value>
    /// <remarks>
    /// When true, the user may want to refine their search or increase MaxResults.
    /// </remarks>
    public bool HasMoreResults => TotalCount > Results.Count;

    /// <summary>
    /// Gets the number of results truncated by the MaxResults limit.
    /// </summary>
    /// <value>
    /// The difference between <see cref="TotalCount"/> and the returned result count,
    /// or 0 if no truncation occurred.
    /// </value>
    /// <remarks>
    /// Display this to inform users that more results exist.
    /// </remarks>
    public int TruncatedCount => Math.Max(0, TotalCount - Results.Count);

    /// <summary>
    /// Gets the count of conversation results.
    /// </summary>
    /// <value>
    /// The number of results where <see cref="SearchResult.IsConversationResult"/> is true.
    /// </value>
    /// <remarks>
    /// Useful for displaying a breakdown of results by type.
    /// </remarks>
    public int ConversationResultCount => Results.Count(r => r.IsConversationResult);

    /// <summary>
    /// Gets the count of message results.
    /// </summary>
    /// <value>
    /// The number of results where <see cref="SearchResult.IsMessageResult"/> is true.
    /// </value>
    /// <remarks>
    /// Useful for displaying a breakdown of results by type.
    /// </remarks>
    public int MessageResultCount => Results.Count(r => r.IsMessageResult);

    /// <summary>
    /// Gets the search duration in milliseconds for display.
    /// </summary>
    /// <value>
    /// The <see cref="SearchDuration"/> converted to milliseconds.
    /// </value>
    /// <remarks>
    /// Format with <c>:F2</c> for display, e.g., "12.34 ms".
    /// </remarks>
    public double SearchDurationMs => SearchDuration.TotalMilliseconds;

    /// <summary>
    /// Gets a formatted summary of the search results for display.
    /// </summary>
    /// <value>
    /// A human-readable string summarizing the search results.
    /// </value>
    /// <example>
    /// "Found 15 results (8 conversations, 7 messages) in 12.34 ms"
    /// </example>
    public string Summary
    {
        get
        {
            if (!HasResults)
            {
                return "No results found";
            }

            var parts = new List<string>();

            if (ConversationResultCount > 0)
            {
                parts.Add($"{ConversationResultCount} conversation{(ConversationResultCount == 1 ? "" : "s")}");
            }

            if (MessageResultCount > 0)
            {
                parts.Add($"{MessageResultCount} message{(MessageResultCount == 1 ? "" : "s")}");
            }

            var typeBreakdown = parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";
            var moreText = HasMoreResults ? $", {TruncatedCount} more available" : "";

            return $"Found {Results.Count} result{(Results.Count == 1 ? "" : "s")}{typeBreakdown}{moreText} in {SearchDurationMs:F2} ms";
        }
    }

    #endregion
}
