using AIntern.Core.Models;
using Xunit;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="SearchQuery"/> record.
/// Verifies default values, validation, factory methods, and computed properties.
/// </summary>
/// <remarks>
/// <para>
/// These tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>Default parameter values for optional parameters</description></item>
///   <item><description>Query validation via <see cref="SearchQuery.IsValid"/></description></item>
///   <item><description>Query normalization via <see cref="SearchQuery.NormalizedQueryText"/></description></item>
///   <item><description>Factory methods: Simple, ConversationsOnly, MessagesOnly</description></item>
///   <item><description>Content type filtering via <see cref="SearchQuery.HasContentTypeFilter"/></description></item>
///   <item><description>Log summary formatting</description></item>
/// </list>
/// <para>Added in v0.2.5a.</para>
/// </remarks>
public class SearchQueryTests
{
    #region Constants Tests

    /// <summary>
    /// Verifies the DefaultMaxResults constant value.
    /// </summary>
    [Fact]
    public void DefaultMaxResults_Is50()
    {
        // Assert
        Assert.Equal(50, SearchQuery.DefaultMaxResults);
    }

    /// <summary>
    /// Verifies the MinMaxResults constant value.
    /// </summary>
    [Fact]
    public void MinMaxResults_Is1()
    {
        // Assert
        Assert.Equal(1, SearchQuery.MinMaxResults);
    }

    /// <summary>
    /// Verifies the MaxMaxResults constant value.
    /// </summary>
    [Fact]
    public void MaxMaxResults_Is500()
    {
        // Assert
        Assert.Equal(500, SearchQuery.MaxMaxResults);
    }

    /// <summary>
    /// Verifies the DefaultMinRank constant value.
    /// </summary>
    [Fact]
    public void DefaultMinRank_IsNegative10()
    {
        // Assert
        Assert.Equal(-10.0, SearchQuery.DefaultMinRank);
    }

    #endregion

    #region Constructor Default Values Tests

    /// <summary>
    /// Verifies that MaxResults defaults to DefaultMaxResults.
    /// </summary>
    [Fact]
    public void Constructor_MaxResults_DefaultsTo50()
    {
        // Arrange & Act
        var query = new SearchQuery("test");

        // Assert
        Assert.Equal(SearchQuery.DefaultMaxResults, query.MaxResults);
    }

    /// <summary>
    /// Verifies that IncludeConversations defaults to true.
    /// </summary>
    [Fact]
    public void Constructor_IncludeConversations_DefaultsToTrue()
    {
        // Arrange & Act
        var query = new SearchQuery("test");

        // Assert
        Assert.True(query.IncludeConversations);
    }

    /// <summary>
    /// Verifies that IncludeMessages defaults to true.
    /// </summary>
    [Fact]
    public void Constructor_IncludeMessages_DefaultsToTrue()
    {
        // Arrange & Act
        var query = new SearchQuery("test");

        // Assert
        Assert.True(query.IncludeMessages);
    }

    /// <summary>
    /// Verifies that MinRank defaults to DefaultMinRank.
    /// </summary>
    [Fact]
    public void Constructor_MinRank_DefaultsToNegative10()
    {
        // Arrange & Act
        var query = new SearchQuery("test");

        // Assert
        Assert.Equal(SearchQuery.DefaultMinRank, query.MinRank);
    }

    /// <summary>
    /// Verifies custom parameter values are preserved.
    /// </summary>
    [Fact]
    public void Constructor_CustomValues_ArePreserved()
    {
        // Arrange & Act
        var query = new SearchQuery(
            QueryText: "custom query",
            MaxResults: 100,
            IncludeConversations: false,
            IncludeMessages: true,
            MinRank: -5.0);

        // Assert
        Assert.Equal("custom query", query.QueryText);
        Assert.Equal(100, query.MaxResults);
        Assert.False(query.IncludeConversations);
        Assert.True(query.IncludeMessages);
        Assert.Equal(-5.0, query.MinRank);
    }

    #endregion

    #region IsValid Tests

    /// <summary>
    /// Verifies IsValid returns true for non-empty query text.
    /// </summary>
    [Theory]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a")]
    [InlineData("  valid  ")]  // Whitespace around valid text
    public void IsValid_NonEmptyQueryText_ReturnsTrue(string queryText)
    {
        // Arrange
        var query = new SearchQuery(queryText);

        // Act & Assert
        Assert.True(query.IsValid);
    }

    /// <summary>
    /// Verifies IsValid returns false for empty or whitespace query text.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("  \t  \n  ")]
    public void IsValid_EmptyOrWhitespaceQueryText_ReturnsFalse(string queryText)
    {
        // Arrange
        var query = new SearchQuery(queryText);

        // Act & Assert
        Assert.False(query.IsValid);
    }

    /// <summary>
    /// Verifies IsValid returns false for null query text.
    /// </summary>
    [Fact]
    public void IsValid_NullQueryText_ReturnsFalse()
    {
        // Arrange
        var query = new SearchQuery(null!);

        // Act & Assert
        Assert.False(query.IsValid);
    }

    #endregion

    #region NormalizedQueryText Tests

    /// <summary>
    /// Verifies NormalizedQueryText trims whitespace.
    /// </summary>
    [Theory]
    [InlineData("  hello  ", "hello")]
    [InlineData("\thello\t", "hello")]
    [InlineData(" hello world ", "hello world")]
    [InlineData("test", "test")]
    public void NormalizedQueryText_TrimsWhitespace(string input, string expected)
    {
        // Arrange
        var query = new SearchQuery(input);

        // Act & Assert
        Assert.Equal(expected, query.NormalizedQueryText);
    }

    /// <summary>
    /// Verifies NormalizedQueryText returns empty string for null.
    /// </summary>
    [Fact]
    public void NormalizedQueryText_NullInput_ReturnsEmptyString()
    {
        // Arrange
        var query = new SearchQuery(null!);

        // Act & Assert
        Assert.Equal(string.Empty, query.NormalizedQueryText);
    }

    /// <summary>
    /// Verifies NormalizedQueryText returns empty string for whitespace-only input.
    /// </summary>
    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void NormalizedQueryText_WhitespaceOnly_ReturnsEmptyString(string input)
    {
        // Arrange
        var query = new SearchQuery(input);

        // Act & Assert
        Assert.Equal(string.Empty, query.NormalizedQueryText);
    }

    #endregion

    #region HasContentTypeFilter Tests

    /// <summary>
    /// Verifies HasContentTypeFilter is true when both types are included.
    /// </summary>
    [Fact]
    public void HasContentTypeFilter_BothTypesIncluded_ReturnsTrue()
    {
        // Arrange
        var query = new SearchQuery("test", IncludeConversations: true, IncludeMessages: true);

        // Act & Assert
        Assert.True(query.HasContentTypeFilter);
    }

    /// <summary>
    /// Verifies HasContentTypeFilter is true when only conversations are included.
    /// </summary>
    [Fact]
    public void HasContentTypeFilter_OnlyConversations_ReturnsTrue()
    {
        // Arrange
        var query = new SearchQuery("test", IncludeConversations: true, IncludeMessages: false);

        // Act & Assert
        Assert.True(query.HasContentTypeFilter);
    }

    /// <summary>
    /// Verifies HasContentTypeFilter is true when only messages are included.
    /// </summary>
    [Fact]
    public void HasContentTypeFilter_OnlyMessages_ReturnsTrue()
    {
        // Arrange
        var query = new SearchQuery("test", IncludeConversations: false, IncludeMessages: true);

        // Act & Assert
        Assert.True(query.HasContentTypeFilter);
    }

    /// <summary>
    /// Verifies HasContentTypeFilter is false when neither type is included.
    /// </summary>
    [Fact]
    public void HasContentTypeFilter_NeitherIncluded_ReturnsFalse()
    {
        // Arrange
        var query = new SearchQuery("test", IncludeConversations: false, IncludeMessages: false);

        // Act & Assert
        Assert.False(query.HasContentTypeFilter);
    }

    #endregion

    #region Factory Method Tests

    /// <summary>
    /// Verifies Simple factory method creates query with default settings.
    /// </summary>
    [Fact]
    public void Simple_CreatesQueryWithDefaults()
    {
        // Arrange & Act
        var query = SearchQuery.Simple("test query");

        // Assert
        Assert.Equal("test query", query.QueryText);
        Assert.Equal(SearchQuery.DefaultMaxResults, query.MaxResults);
        Assert.True(query.IncludeConversations);
        Assert.True(query.IncludeMessages);
        Assert.Equal(SearchQuery.DefaultMinRank, query.MinRank);
    }

    /// <summary>
    /// Verifies ConversationsOnly factory method creates correct query.
    /// </summary>
    [Fact]
    public void ConversationsOnly_SearchesOnlyConversations()
    {
        // Arrange & Act
        var query = SearchQuery.ConversationsOnly("test");

        // Assert
        Assert.Equal("test", query.QueryText);
        Assert.True(query.IncludeConversations);
        Assert.False(query.IncludeMessages);
    }

    /// <summary>
    /// Verifies MessagesOnly factory method creates correct query.
    /// </summary>
    [Fact]
    public void MessagesOnly_SearchesOnlyMessages()
    {
        // Arrange & Act
        var query = SearchQuery.MessagesOnly("test");

        // Assert
        Assert.Equal("test", query.QueryText);
        Assert.False(query.IncludeConversations);
        Assert.True(query.IncludeMessages);
    }

    #endregion

    #region LogSummary Tests

    /// <summary>
    /// Verifies LogSummary contains all expected components.
    /// </summary>
    [Fact]
    public void LogSummary_ContainsAllComponents()
    {
        // Arrange
        var query = new SearchQuery(
            QueryText: "test query",
            MaxResults: 100,
            IncludeConversations: true,
            IncludeMessages: false,
            MinRank: -5.0);

        // Act
        var summary = query.LogSummary;

        // Assert
        Assert.Contains("Query='test query'", summary);
        Assert.Contains("Max=100", summary);
        Assert.Contains("Conv=True", summary);
        Assert.Contains("Msg=False", summary);
        Assert.Contains("MinRank=-5", summary);
    }

    /// <summary>
    /// Verifies LogSummary uses normalized query text.
    /// </summary>
    [Fact]
    public void LogSummary_UsesNormalizedQueryText()
    {
        // Arrange
        var query = new SearchQuery("  trimmed  ");

        // Act
        var summary = query.LogSummary;

        // Assert
        Assert.Contains("Query='trimmed'", summary);
    }

    #endregion

    #region Record Equality Tests

    /// <summary>
    /// Verifies that two SearchQuery instances with same values are equal.
    /// </summary>
    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var query1 = new SearchQuery("test", 50, true, true, -10.0);
        var query2 = new SearchQuery("test", 50, true, true, -10.0);

        // Act & Assert
        Assert.Equal(query1, query2);
    }

    /// <summary>
    /// Verifies that two SearchQuery instances with different values are not equal.
    /// </summary>
    [Fact]
    public void Equals_DifferentQueryText_ReturnsFalse()
    {
        // Arrange
        var query1 = SearchQuery.Simple("test1");
        var query2 = SearchQuery.Simple("test2");

        // Act & Assert
        Assert.NotEqual(query1, query2);
    }

    /// <summary>
    /// Verifies 'with' expression creates modified copy.
    /// </summary>
    [Fact]
    public void With_ModifiesProperty_CreatesNewInstance()
    {
        // Arrange
        var original = SearchQuery.Simple("original");

        // Act
        var modified = original with { MaxResults = 100 };

        // Assert
        Assert.NotEqual(original, modified);
        Assert.Equal(50, original.MaxResults);
        Assert.Equal(100, modified.MaxResults);
    }

    #endregion
}
