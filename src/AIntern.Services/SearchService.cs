using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data;

namespace AIntern.Services;

/// <summary>
/// Search service implementation using SQLite FTS5 for full-text search.
/// </summary>
public sealed partial class SearchService : ISearchService
{
    private readonly IDbContextFactory<AInternDbContext> _contextFactory;
    private readonly ILogger<SearchService> _logger;
    private readonly List<string> _recentSearches = new(capacity: 20);
    private readonly object _recentSearchesLock = new();

    public SearchService(
        IDbContextFactory<AInternDbContext> contextFactory,
        ILogger<SearchService> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SearchResults> SearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.Text))
            return SearchResults.Empty;

        var stopwatch = Stopwatch.StartNew();
        var sanitizedQuery = SanitizeFtsQuery(query.Text);

        if (string.IsNullOrWhiteSpace(sanitizedQuery))
            return SearchResults.Empty;

        _logger.LogDebug("Searching FTS5 for: {Query} -> {Sanitized}", query.Text, sanitizedQuery);

        TrackRecentSearch(query.Text);

        var conversations = new List<SearchResult>();
        var messages = new List<SearchResult>();

        // Search conversations if not filtered to messages only
        if (query.FilterType is null or SearchResultType.Conversation)
        {
            conversations = await SearchConversationsAsync(
                sanitizedQuery, query.MaxResults / 2, query.SnippetLength, ct);
        }

        // Search messages if not filtered to conversations only
        if (query.FilterType is null or SearchResultType.Message)
        {
            messages = await SearchMessagesAsync(
                sanitizedQuery, query.MaxResults / 2, query.SnippetLength, ct);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Search completed in {Duration}ms: {ConvCount} conversations, {MsgCount} messages",
            stopwatch.ElapsedMilliseconds, conversations.Count, messages.Count);

        return new SearchResults
        {
            Conversations = conversations,
            Messages = messages,
            TotalCount = conversations.Count + messages.Count,
            SearchDuration = stopwatch.Elapsed
        };
    }

    private async Task<List<SearchResult>> SearchConversationsAsync(
        string query, int maxResults, int snippetLength, CancellationToken ct)
    {
        // snippet() args: table, column-index (1=Title), start-marker, end-marker, ellipsis, max-tokens
        const string sql = """
            SELECT
                c.Id,
                c.Title,
                snippet(ConversationsFts, 1, '<mark>', '</mark>', '...', @tokens) as Snippet,
                bm25(ConversationsFts) as Rank,
                c.UpdatedAt
            FROM ConversationsFts
            INNER JOIN Conversations c ON c.Id = ConversationsFts.EntityId
            WHERE ConversationsFts MATCH @query
            ORDER BY Rank
            LIMIT @limit
            """;

        var results = new List<SearchResult>();

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var connection = context.GetConnection();
            await context.OpenConnectionAsync(ct);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameter(command, "@query", query);
                AddParameter(command, "@tokens", Math.Max(snippetLength / 10, 5));
                AddParameter(command, "@limit", maxResults);

                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var snippet = reader.IsDBNull(2) ? reader.GetString(1) : reader.GetString(2);
                    results.Add(new SearchResult
                    {
                        Id = Guid.Parse(reader.GetString(0)),
                        Type = SearchResultType.Conversation,
                        Title = reader.GetString(1),
                        Snippet = StripHtmlTags(snippet),
                        HighlightedSnippet = snippet,
                        Rank = Math.Abs(reader.GetDouble(3)), // BM25 returns negative values
                        Timestamp = DateTime.Parse(reader.GetString(4))
                    });
                }
            }
            finally
            {
                await context.CloseConnectionAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching conversations for query: {Query}", query);
        }

        return results;
    }

    private async Task<List<SearchResult>> SearchMessagesAsync(
        string query, int maxResults, int snippetLength, CancellationToken ct)
    {
        // snippet() args: table, column-index (1=Content), start-marker, end-marker, ellipsis, max-tokens
        const string sql = """
            SELECT
                m.Id,
                m.Content,
                snippet(MessagesFts, 1, '<mark>', '</mark>', '...', @tokens) as Snippet,
                bm25(MessagesFts) as Rank,
                m.Timestamp,
                m.ConversationId,
                c.Title as ConversationTitle
            FROM MessagesFts
            INNER JOIN Messages m ON m.Id = MessagesFts.EntityId
            INNER JOIN Conversations c ON c.Id = m.ConversationId
            WHERE MessagesFts MATCH @query
            ORDER BY Rank
            LIMIT @limit
            """;

        var results = new List<SearchResult>();

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var connection = context.GetConnection();
            await context.OpenConnectionAsync(ct);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                AddParameter(command, "@query", query);
                AddParameter(command, "@tokens", Math.Max(snippetLength / 10, 5));
                AddParameter(command, "@limit", maxResults);

                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var content = reader.GetString(1);
                    var snippet = reader.IsDBNull(2) ? TruncateContent(content, snippetLength) : reader.GetString(2);
                    results.Add(new SearchResult
                    {
                        Id = Guid.Parse(reader.GetString(0)),
                        Type = SearchResultType.Message,
                        Title = TruncateContent(content, 50),
                        Snippet = StripHtmlTags(snippet),
                        HighlightedSnippet = snippet,
                        Rank = Math.Abs(reader.GetDouble(3)), // BM25 returns negative values
                        Timestamp = DateTime.Parse(reader.GetString(4)),
                        ConversationId = Guid.Parse(reader.GetString(5)),
                        ConversationTitle = reader.GetString(6)
                    });
                }
            }
            finally
            {
                await context.CloseConnectionAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching messages for query: {Query}", query);
        }

        return results;
    }

    public async Task RebuildIndexAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Rebuilding FTS5 indexes...");

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO ConversationsFts(ConversationsFts) VALUES('rebuild');", ct);

        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO MessagesFts(MessagesFts) VALUES('rebuild');", ct);

        _logger.LogInformation("FTS5 indexes rebuilt successfully");
    }

    public Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string prefix, int maxSuggestions = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return Task.FromResult<IReadOnlyList<string>>([]);

        lock (_recentSearchesLock)
        {
            var suggestions = _recentSearches
                .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Take(maxSuggestions)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(suggestions);
        }
    }

    #region Utility Methods

    private void TrackRecentSearch(string query)
    {
        lock (_recentSearchesLock)
        {
            _recentSearches.Remove(query);
            _recentSearches.Insert(0, query);

            while (_recentSearches.Count > 20)
                _recentSearches.RemoveAt(_recentSearches.Count - 1);
        }
    }

    private static string SanitizeFtsQuery(string query)
    {
        // Escape special FTS5 characters
        var sanitized = query
            .Replace("\"", "\"\"")
            .Replace("*", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace(":", "")
            .Replace("^", "")
            .Replace("-", " "); // Treat hyphen as space

        // Split into terms and add prefix matching
        var terms = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (terms.Length == 0)
            return string.Empty;

        return string.Join(" ", terms.Select(t => $"\"{t}\"*"));
    }

    private static string StripHtmlTags(string html)
        => StripHtmlRegex().Replace(html, string.Empty);

    private static string TruncateContent(string content, int maxLength)
        => content.Length <= maxLength ? content : content[..maxLength] + "...";

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex StripHtmlRegex();

    #endregion
}
