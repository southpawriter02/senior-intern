using AIntern.Core.Enums;
using AIntern.Core.Models;
using Xunit;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="SearchResults"/> record.
/// Verifies construction, computed properties, and the Empty factory method.
/// </summary>
/// <remarks>
/// <para>
/// These tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>Record construction with all properties</description></item>
///   <item><description>Empty factory method behavior</description></item>
///   <item><description>HasResults computed property</description></item>
///   <item><description>HasMoreResults and TruncatedCount for pagination</description></item>
///   <item><description>ConversationResultCount and MessageResultCount</description></item>
///   <item><description>SearchDurationMs conversion</description></item>
///   <item><description>Summary text generation</description></item>
/// </list>
/// <para>Added in v0.2.5a.</para>
/// </remarks>
public class SearchResultsTests
{
    #region Test Data Helpers

    /// <summary>
    /// Creates a conversation search result for testing.
    /// </summary>
    private static SearchResult CreateConversationResult(double rank = -5.0)
    {
        var id = Guid.NewGuid();
        return new SearchResult(
            Id: id,
            ResultType: SearchResultType.Conversation,
            Title: "Test Conversation",
            Preview: "Test Preview",
            Rank: rank,
            Timestamp: DateTime.UtcNow,
            ConversationId: id,
            MessageId: null);
    }

    /// <summary>
    /// Creates a message search result for testing.
    /// </summary>
    private static SearchResult CreateMessageResult(double rank = -3.0)
    {
        var id = Guid.NewGuid();
        return new SearchResult(
            Id: id,
            ResultType: SearchResultType.Message,
            Title: "Parent Conversation",
            Preview: "...matched <mark>text</mark>...",
            Rank: rank,
            Timestamp: DateTime.UtcNow,
            ConversationId: Guid.NewGuid(),
            MessageId: id);
    }

    /// <summary>
    /// Creates a SearchResults with the specified results.
    /// </summary>
    private static SearchResults CreateSearchResults(
        IReadOnlyList<SearchResult>? results = null,
        int? totalCount = null,
        TimeSpan? duration = null)
    {
        var resultList = results ?? Array.Empty<SearchResult>();
        return new SearchResults(
            Results: resultList,
            TotalCount: totalCount ?? resultList.Count,
            Query: SearchQuery.Simple("test"),
            SearchDuration: duration ?? TimeSpan.FromMilliseconds(10));
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that SearchResults can be constructed with all properties.
    /// </summary>
    [Fact]
    public void Constructor_WithAllProperties_CreatesInstance()
    {
        // Arrange
        var results = new List<SearchResult> { CreateConversationResult() };
        var query = SearchQuery.Simple("test");
        var duration = TimeSpan.FromMilliseconds(25.5);

        // Act
        var searchResults = new SearchResults(
            Results: results,
            TotalCount: 10,
            Query: query,
            SearchDuration: duration);

        // Assert
        Assert.Single(searchResults.Results);
        Assert.Equal(10, searchResults.TotalCount);
        Assert.Same(query, searchResults.Query);
        Assert.Equal(duration, searchResults.SearchDuration);
    }

    #endregion

    #region Empty Factory Method Tests

    /// <summary>
    /// Verifies Empty creates an instance with no results.
    /// </summary>
    [Fact]
    public void Empty_CreatesEmptyResults()
    {
        // Arrange
        var query = SearchQuery.Simple("empty test");

        // Act
        var results = SearchResults.Empty(query);

        // Assert
        Assert.Empty(results.Results);
        Assert.Equal(0, results.TotalCount);
        Assert.Same(query, results.Query);
        Assert.Equal(TimeSpan.Zero, results.SearchDuration);
    }

    /// <summary>
    /// Verifies Empty preserves the original query.
    /// </summary>
    [Fact]
    public void Empty_PreservesQuery()
    {
        // Arrange
        var query = new SearchQuery("test", 100, false, true, -5.0);

        // Act
        var results = SearchResults.Empty(query);

        // Assert
        Assert.Equal(query, results.Query);
    }

    #endregion

    #region HasResults Tests

    /// <summary>
    /// Verifies HasResults is true when results exist.
    /// </summary>
    [Fact]
    public void HasResults_WithResults_ReturnsTrue()
    {
        // Arrange
        var results = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult() });

        // Act & Assert
        Assert.True(results.HasResults);
    }

    /// <summary>
    /// Verifies HasResults is false when no results exist.
    /// </summary>
    [Fact]
    public void HasResults_WithNoResults_ReturnsFalse()
    {
        // Arrange
        var results = CreateSearchResults(results: Array.Empty<SearchResult>());

        // Act & Assert
        Assert.False(results.HasResults);
    }

    /// <summary>
    /// Verifies HasResults is false for Empty results.
    /// </summary>
    [Fact]
    public void HasResults_EmptyResults_ReturnsFalse()
    {
        // Arrange
        var results = SearchResults.Empty(SearchQuery.Simple("test"));

        // Act & Assert
        Assert.False(results.HasResults);
    }

    #endregion

    #region HasMoreResults and TruncatedCount Tests

    /// <summary>
    /// Verifies HasMoreResults is true when TotalCount exceeds Results.Count.
    /// </summary>
    [Fact]
    public void HasMoreResults_WhenTotalCountExceedsResultsCount_ReturnsTrue()
    {
        // Arrange
        var results = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult() },
            totalCount: 10);

        // Act & Assert
        Assert.True(results.HasMoreResults);
    }

    /// <summary>
    /// Verifies HasMoreResults is false when all results are returned.
    /// </summary>
    [Fact]
    public void HasMoreResults_WhenAllResultsReturned_ReturnsFalse()
    {
        // Arrange
        var resultList = new List<SearchResult>
        {
            CreateConversationResult(),
            CreateMessageResult()
        };
        var results = CreateSearchResults(results: resultList, totalCount: 2);

        // Act & Assert
        Assert.False(results.HasMoreResults);
    }

    /// <summary>
    /// Verifies TruncatedCount returns correct value.
    /// </summary>
    [Fact]
    public void TruncatedCount_ReturnsCorrectValue()
    {
        // Arrange
        var results = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult() },
            totalCount: 15);

        // Act & Assert
        Assert.Equal(14, results.TruncatedCount);
    }

    /// <summary>
    /// Verifies TruncatedCount is zero when no truncation.
    /// </summary>
    [Fact]
    public void TruncatedCount_NoTruncation_ReturnsZero()
    {
        // Arrange
        var results = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult() },
            totalCount: 1);

        // Act & Assert
        Assert.Equal(0, results.TruncatedCount);
    }

    /// <summary>
    /// Verifies TruncatedCount never returns negative value.
    /// </summary>
    [Fact]
    public void TruncatedCount_TotalCountLessThanResultsCount_ReturnsZero()
    {
        // Arrange - Edge case where TotalCount is less (shouldn't happen but test guards)
        var results = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult(), CreateMessageResult() },
            totalCount: 1);

        // Act & Assert - Should return 0, not negative
        Assert.Equal(0, results.TruncatedCount);
    }

    #endregion

    #region ConversationResultCount and MessageResultCount Tests

    /// <summary>
    /// Verifies ConversationResultCount returns correct count.
    /// </summary>
    [Fact]
    public void ConversationResultCount_ReturnsCorrectCount()
    {
        // Arrange
        var resultList = new List<SearchResult>
        {
            CreateConversationResult(),
            CreateConversationResult(),
            CreateMessageResult()
        };
        var results = CreateSearchResults(results: resultList);

        // Act & Assert
        Assert.Equal(2, results.ConversationResultCount);
    }

    /// <summary>
    /// Verifies MessageResultCount returns correct count.
    /// </summary>
    [Fact]
    public void MessageResultCount_ReturnsCorrectCount()
    {
        // Arrange
        var resultList = new List<SearchResult>
        {
            CreateConversationResult(),
            CreateMessageResult(),
            CreateMessageResult(),
            CreateMessageResult()
        };
        var results = CreateSearchResults(results: resultList);

        // Act & Assert
        Assert.Equal(3, results.MessageResultCount);
    }

    /// <summary>
    /// Verifies counts are zero when no results of that type.
    /// </summary>
    [Fact]
    public void ResultCounts_NoResultsOfType_ReturnsZero()
    {
        // Arrange
        var convOnlyResults = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult() });

        var msgOnlyResults = CreateSearchResults(
            results: new List<SearchResult> { CreateMessageResult() });

        // Act & Assert
        Assert.Equal(0, convOnlyResults.MessageResultCount);
        Assert.Equal(0, msgOnlyResults.ConversationResultCount);
    }

    #endregion

    #region SearchDurationMs Tests

    /// <summary>
    /// Verifies SearchDurationMs converts TimeSpan correctly.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    [InlineData(1000, 1000)]
    public void SearchDurationMs_ConvertsCorrectly(double ms, double expected)
    {
        // Arrange
        var results = CreateSearchResults(duration: TimeSpan.FromMilliseconds(ms));

        // Act & Assert
        Assert.Equal(expected, results.SearchDurationMs);
    }

    /// <summary>
    /// Verifies SearchDurationMs handles fractional milliseconds.
    /// </summary>
    [Fact]
    public void SearchDurationMs_HandlesFractionalMilliseconds()
    {
        // Arrange
        var duration = TimeSpan.FromTicks(125000); // 12.5 ms
        var results = CreateSearchResults(duration: duration);

        // Act & Assert
        Assert.Equal(12.5, results.SearchDurationMs, 2);
    }

    #endregion

    #region Summary Tests

    /// <summary>
    /// Verifies Summary for empty results.
    /// </summary>
    [Fact]
    public void Summary_NoResults_ReturnsNoResultsMessage()
    {
        // Arrange
        var results = SearchResults.Empty(SearchQuery.Simple("test"));

        // Act
        var summary = results.Summary;

        // Assert
        Assert.Equal("No results found", summary);
    }

    /// <summary>
    /// Verifies Summary contains result count.
    /// </summary>
    [Fact]
    public void Summary_WithResults_ContainsResultCount()
    {
        // Arrange
        var resultList = new List<SearchResult>
        {
            CreateConversationResult(),
            CreateMessageResult()
        };
        var results = CreateSearchResults(results: resultList);

        // Act
        var summary = results.Summary;

        // Assert
        Assert.Contains("Found 2 result", summary);
    }

    /// <summary>
    /// Verifies Summary contains type breakdown.
    /// </summary>
    [Fact]
    public void Summary_MixedResults_ContainsTypeBreakdown()
    {
        // Arrange
        var resultList = new List<SearchResult>
        {
            CreateConversationResult(),
            CreateConversationResult(),
            CreateMessageResult()
        };
        var results = CreateSearchResults(results: resultList);

        // Act
        var summary = results.Summary;

        // Assert
        Assert.Contains("2 conversations", summary);
        Assert.Contains("1 message", summary);
    }

    /// <summary>
    /// Verifies Summary contains truncation info when applicable.
    /// </summary>
    [Fact]
    public void Summary_WithTruncation_ContainsMoreAvailableText()
    {
        // Arrange
        var results = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult() },
            totalCount: 10);

        // Act
        var summary = results.Summary;

        // Assert
        Assert.Contains("9 more available", summary);
    }

    /// <summary>
    /// Verifies Summary contains duration.
    /// </summary>
    [Fact]
    public void Summary_ContainsDuration()
    {
        // Arrange
        var results = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult() },
            duration: TimeSpan.FromMilliseconds(12.34));

        // Act
        var summary = results.Summary;

        // Assert
        Assert.Contains("12.34 ms", summary);
    }

    /// <summary>
    /// Verifies correct pluralization for single result.
    /// </summary>
    [Fact]
    public void Summary_SingleResult_UsesSingularForm()
    {
        // Arrange
        var results = CreateSearchResults(
            results: new List<SearchResult> { CreateConversationResult() });

        // Act
        var summary = results.Summary;

        // Assert
        Assert.Contains("Found 1 result", summary);
        Assert.Contains("1 conversation)", summary);
    }

    /// <summary>
    /// Verifies correct pluralization for multiple results.
    /// </summary>
    [Fact]
    public void Summary_MultipleResults_UsesPluralForm()
    {
        // Arrange
        var results = CreateSearchResults(
            results: new List<SearchResult>
            {
                CreateConversationResult(),
                CreateConversationResult()
            });

        // Act
        var summary = results.Summary;

        // Assert
        Assert.Contains("Found 2 results", summary);
        Assert.Contains("2 conversations)", summary);
    }

    #endregion

    #region Record Equality Tests

    /// <summary>
    /// Verifies that SearchResults with same values are equal.
    /// </summary>
    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var resultList = new List<SearchResult> { CreateConversationResult() };
        var query = SearchQuery.Simple("test");
        var duration = TimeSpan.FromMilliseconds(10);

        var results1 = new SearchResults(resultList, 1, query, duration);
        var results2 = new SearchResults(resultList, 1, query, duration);

        // Act & Assert
        Assert.Equal(results1, results2);
    }

    /// <summary>
    /// Verifies 'with' expression creates modified copy.
    /// </summary>
    [Fact]
    public void With_ModifiesProperty_CreatesNewInstance()
    {
        // Arrange
        var original = CreateSearchResults(totalCount: 5);

        // Act
        var modified = original with { TotalCount = 100 };

        // Assert
        Assert.NotEqual(original, modified);
        Assert.Equal(5, original.TotalCount);
        Assert.Equal(100, modified.TotalCount);
    }

    #endregion
}
