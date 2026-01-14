namespace AIntern.Core.Models;

/// <summary>
/// Encapsulates parameters for a full-text search operation.
/// </summary>
/// <remarks>
/// <para>
/// This record provides a structured way to configure search behavior:
/// </para>
/// <list type="bullet">
///   <item><description><b>QueryText:</b> The search term(s) to find</description></item>
///   <item><description><b>MaxResults:</b> Limit on returned results (default: 50)</description></item>
///   <item><description><b>IncludeConversations:</b> Whether to search conversation titles</description></item>
///   <item><description><b>IncludeMessages:</b> Whether to search message content</description></item>
///   <item><description><b>MinRank:</b> Minimum relevance threshold (default: -10.0)</description></item>
/// </list>
/// <para>
/// <b>FTS5 Query Syntax:</b> The QueryText supports FTS5 query syntax including:
/// </para>
/// <list type="bullet">
///   <item><description>Simple terms: <c>hello world</c> (implicit AND)</description></item>
///   <item><description>Phrases: <c>"hello world"</c> (exact phrase)</description></item>
///   <item><description>Prefix: <c>hel*</c> (prefix matching)</description></item>
///   <item><description>Boolean: <c>hello OR world</c>, <c>hello NOT world</c></description></item>
/// </list>
/// <para>
/// <b>BM25 Ranking:</b> The MinRank parameter filters results by their BM25 relevance score.
/// BM25 scores are negative, with more negative values indicating better matches.
/// The default of -10.0 is very permissive and includes most matches.
/// </para>
/// <para>Added in v0.2.5a.</para>
/// </remarks>
/// <param name="QueryText">The search term(s) to find. Supports FTS5 query syntax.</param>
/// <param name="MaxResults">Maximum number of results to return. Default: 50.</param>
/// <param name="IncludeConversations">Whether to search conversation titles. Default: true.</param>
/// <param name="IncludeMessages">Whether to search message content. Default: true.</param>
/// <param name="MinRank">Minimum BM25 rank threshold. Default: -10.0 (very permissive).</param>
/// <example>
/// Creating a simple search query:
/// <code>
/// var query = SearchQuery.Simple("hello world");
/// </code>
///
/// Creating a filtered search:
/// <code>
/// var query = new SearchQuery(
///     QueryText: "project",
///     MaxResults: 20,
///     IncludeConversations: true,
///     IncludeMessages: false);
/// </code>
/// </example>
public sealed record SearchQuery(
    string QueryText,
    int MaxResults = SearchQuery.DefaultMaxResults,
    bool IncludeConversations = true,
    bool IncludeMessages = true,
    double MinRank = SearchQuery.DefaultMinRank)
{
    #region Constants

    /// <summary>
    /// Default maximum results if not specified.
    /// </summary>
    /// <value>50</value>
    public const int DefaultMaxResults = 50;

    /// <summary>
    /// Minimum allowed value for MaxResults.
    /// </summary>
    /// <value>1</value>
    public const int MinMaxResults = 1;

    /// <summary>
    /// Maximum allowed value for MaxResults.
    /// </summary>
    /// <value>500</value>
    public const int MaxMaxResults = 500;

    /// <summary>
    /// Default minimum rank threshold (very permissive).
    /// </summary>
    /// <value>-10.0</value>
    /// <remarks>
    /// BM25 scores are negative; -10.0 allows most matches through.
    /// </remarks>
    public const double DefaultMinRank = -10.0;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether the query is valid (non-empty QueryText after trimming).
    /// </summary>
    /// <value>
    /// True if <see cref="QueryText"/> contains non-whitespace characters.
    /// </value>
    /// <remarks>
    /// An invalid query will return empty results without executing a database search.
    /// </remarks>
    public bool IsValid => !string.IsNullOrWhiteSpace(QueryText);

    /// <summary>
    /// Gets the trimmed query text for actual search execution.
    /// </summary>
    /// <value>
    /// The <see cref="QueryText"/> with leading and trailing whitespace removed,
    /// or an empty string if QueryText is null.
    /// </value>
    /// <remarks>
    /// Always use this property when executing the actual FTS5 query to ensure
    /// consistent behavior.
    /// </remarks>
    public string NormalizedQueryText => QueryText?.Trim() ?? string.Empty;

    /// <summary>
    /// Gets whether this query will search any content type.
    /// </summary>
    /// <value>
    /// True if at least one of <see cref="IncludeConversations"/> or
    /// <see cref="IncludeMessages"/> is true.
    /// </value>
    /// <remarks>
    /// If both are false, the search will return empty results.
    /// </remarks>
    public bool HasContentTypeFilter => IncludeConversations || IncludeMessages;

    /// <summary>
    /// Gets a summary of the query for logging purposes.
    /// </summary>
    /// <value>
    /// A string summarizing the query parameters.
    /// </value>
    public string LogSummary =>
        $"Query='{NormalizedQueryText}', Max={MaxResults}, Conv={IncludeConversations}, Msg={IncludeMessages}, MinRank={MinRank}";

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a simple search query with default settings.
    /// </summary>
    /// <param name="queryText">The search term(s).</param>
    /// <returns>A new <see cref="SearchQuery"/> with default settings.</returns>
    /// <remarks>
    /// Searches both conversations and messages with up to 50 results.
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = SearchQuery.Simple("machine learning");
    /// </code>
    /// </example>
    public static SearchQuery Simple(string queryText) => new(queryText);

    /// <summary>
    /// Creates a search query targeting only conversations.
    /// </summary>
    /// <param name="queryText">The search term(s).</param>
    /// <returns>A new <see cref="SearchQuery"/> searching only conversation titles.</returns>
    /// <remarks>
    /// Useful when the user wants to find conversations by title only.
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = SearchQuery.ConversationsOnly("project planning");
    /// </code>
    /// </example>
    public static SearchQuery ConversationsOnly(string queryText)
        => new(queryText, IncludeConversations: true, IncludeMessages: false);

    /// <summary>
    /// Creates a search query targeting only messages.
    /// </summary>
    /// <param name="queryText">The search term(s).</param>
    /// <returns>A new <see cref="SearchQuery"/> searching only message content.</returns>
    /// <remarks>
    /// Useful when searching for specific content within conversations.
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = SearchQuery.MessagesOnly("error handling");
    /// </code>
    /// </example>
    public static SearchQuery MessagesOnly(string queryText)
        => new(queryText, IncludeConversations: false, IncludeMessages: true);

    #endregion
}
