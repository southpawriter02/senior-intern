using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>Provides full-text search functionality using FTS5.</summary>
public interface ISearchService
{
    /// <summary>Searches conversations and messages using FTS5.</summary>
    /// <param name="query">The search query with options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Grouped search results with ranking.</returns>
    Task<SearchResults> SearchAsync(SearchQuery query, CancellationToken ct = default);

    /// <summary>Rebuilds the FTS5 indexes (for maintenance).</summary>
    /// <remarks>Use sparingly - rebuilds entire index.</remarks>
    Task RebuildIndexAsync(CancellationToken ct = default);

    /// <summary>Gets search suggestions based on recent searches.</summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <param name="maxSuggestions">Maximum number of suggestions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching recent searches.</returns>
    Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string prefix,
        int maxSuggestions = 5,
        CancellationToken ct = default);
}
