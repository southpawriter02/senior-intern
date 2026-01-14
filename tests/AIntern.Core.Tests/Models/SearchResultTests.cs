using AIntern.Core.Enums;
using AIntern.Core.Models;
using Xunit;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="SearchResult"/> record.
/// Verifies record construction, computed properties, and value semantics.
/// </summary>
/// <remarks>
/// <para>
/// These tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>Record construction with all required properties</description></item>
///   <item><description>Computed properties for result type detection</description></item>
///   <item><description>TypeLabel property for display purposes</description></item>
///   <item><description>Formatted properties for UI display</description></item>
///   <item><description>Record value equality semantics</description></item>
/// </list>
/// <para>Added in v0.2.5a.</para>
/// </remarks>
public class SearchResultTests
{
    #region Test Data Helpers

    /// <summary>
    /// Creates a conversation search result for testing.
    /// </summary>
    private static SearchResult CreateConversationResult(
        Guid? id = null,
        string title = "Test Conversation",
        string preview = "Test Preview",
        double rank = -5.0,
        DateTime? timestamp = null)
    {
        var resultId = id ?? Guid.NewGuid();
        return new SearchResult(
            Id: resultId,
            ResultType: SearchResultType.Conversation,
            Title: title,
            Preview: preview,
            Rank: rank,
            Timestamp: timestamp ?? DateTime.UtcNow,
            ConversationId: resultId,
            MessageId: null);
    }

    /// <summary>
    /// Creates a message search result for testing.
    /// </summary>
    private static SearchResult CreateMessageResult(
        Guid? id = null,
        Guid? conversationId = null,
        string title = "Parent Conversation",
        string preview = "...matched <mark>text</mark>...",
        double rank = -3.0,
        DateTime? timestamp = null)
    {
        var resultId = id ?? Guid.NewGuid();
        var convId = conversationId ?? Guid.NewGuid();
        return new SearchResult(
            Id: resultId,
            ResultType: SearchResultType.Message,
            Title: title,
            Preview: preview,
            Rank: rank,
            Timestamp: timestamp ?? DateTime.UtcNow,
            ConversationId: convId,
            MessageId: resultId);
    }

    #endregion

    #region Construction Tests

    /// <summary>
    /// Verifies that a SearchResult can be constructed with all required properties.
    /// </summary>
    [Fact]
    public void Constructor_WithAllProperties_CreatesInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var result = new SearchResult(
            Id: id,
            ResultType: SearchResultType.Message,
            Title: "Test Title",
            Preview: "Test Preview",
            Rank: -5.5,
            Timestamp: timestamp,
            ConversationId: conversationId,
            MessageId: messageId);

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(SearchResultType.Message, result.ResultType);
        Assert.Equal("Test Title", result.Title);
        Assert.Equal("Test Preview", result.Preview);
        Assert.Equal(-5.5, result.Rank);
        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(conversationId, result.ConversationId);
        Assert.Equal(messageId, result.MessageId);
    }

    /// <summary>
    /// Verifies that conversation results can have null MessageId.
    /// </summary>
    [Fact]
    public void Constructor_ConversationResult_MessageIdIsNull()
    {
        // Arrange & Act
        var result = CreateConversationResult();

        // Assert
        Assert.Null(result.MessageId);
    }

    /// <summary>
    /// Verifies that message results have non-null MessageId.
    /// </summary>
    [Fact]
    public void Constructor_MessageResult_MessageIdIsNotNull()
    {
        // Arrange & Act
        var result = CreateMessageResult();

        // Assert
        Assert.NotNull(result.MessageId);
    }

    #endregion

    #region IsConversationResult Tests

    /// <summary>
    /// Verifies IsConversationResult is true for Conversation type.
    /// </summary>
    [Fact]
    public void IsConversationResult_WhenConversationType_ReturnsTrue()
    {
        // Arrange
        var result = CreateConversationResult();

        // Act & Assert
        Assert.True(result.IsConversationResult);
    }

    /// <summary>
    /// Verifies IsConversationResult is false for Message type.
    /// </summary>
    [Fact]
    public void IsConversationResult_WhenMessageType_ReturnsFalse()
    {
        // Arrange
        var result = CreateMessageResult();

        // Act & Assert
        Assert.False(result.IsConversationResult);
    }

    #endregion

    #region IsMessageResult Tests

    /// <summary>
    /// Verifies IsMessageResult is true for Message type.
    /// </summary>
    [Fact]
    public void IsMessageResult_WhenMessageType_ReturnsTrue()
    {
        // Arrange
        var result = CreateMessageResult();

        // Act & Assert
        Assert.True(result.IsMessageResult);
    }

    /// <summary>
    /// Verifies IsMessageResult is false for Conversation type.
    /// </summary>
    [Fact]
    public void IsMessageResult_WhenConversationType_ReturnsFalse()
    {
        // Arrange
        var result = CreateConversationResult();

        // Act & Assert
        Assert.False(result.IsMessageResult);
    }

    #endregion

    #region TypeLabel Tests

    /// <summary>
    /// Verifies TypeLabel returns "Conversation" for Conversation type.
    /// </summary>
    [Fact]
    public void TypeLabel_WhenConversationType_ReturnsConversation()
    {
        // Arrange
        var result = CreateConversationResult();

        // Act & Assert
        Assert.Equal("Conversation", result.TypeLabel);
    }

    /// <summary>
    /// Verifies TypeLabel returns "Message" for Message type.
    /// </summary>
    [Fact]
    public void TypeLabel_WhenMessageType_ReturnsMessage()
    {
        // Arrange
        var result = CreateMessageResult();

        // Act & Assert
        Assert.Equal("Message", result.TypeLabel);
    }

    #endregion

    #region FormattedRank Tests

    /// <summary>
    /// Verifies FormattedRank formats the rank to 2 decimal places.
    /// </summary>
    [Theory]
    [InlineData(-5.0, "-5.00")]
    [InlineData(-3.14159, "-3.14")]
    [InlineData(-10.5, "-10.50")]
    [InlineData(0.0, "0.00")]
    [InlineData(-0.12345, "-0.12")]
    public void FormattedRank_ReturnsCorrectFormat(double rank, string expected)
    {
        // Arrange
        var result = CreateConversationResult(rank: rank);

        // Act & Assert
        Assert.Equal(expected, result.FormattedRank);
    }

    #endregion

    #region FormattedTimestamp Tests

    /// <summary>
    /// Verifies FormattedTimestamp uses the "g" format (short date/time).
    /// </summary>
    [Fact]
    public void FormattedTimestamp_UsesShortDateTimeFormat()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 13, 14, 30, 0);
        var result = CreateConversationResult(timestamp: timestamp);

        // Act
        var formatted = result.FormattedTimestamp;

        // Assert
        // The "g" format produces culture-specific output, so just verify it's not empty
        // and contains date components
        Assert.NotEmpty(formatted);
        Assert.Contains("2026", formatted);
    }

    #endregion

    #region Record Equality Tests

    /// <summary>
    /// Verifies that two SearchResults with same values are equal.
    /// </summary>
    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        var result1 = new SearchResult(
            Id: id,
            ResultType: SearchResultType.Conversation,
            Title: "Test",
            Preview: "Test",
            Rank: -5.0,
            Timestamp: timestamp,
            ConversationId: id,
            MessageId: null);

        var result2 = new SearchResult(
            Id: id,
            ResultType: SearchResultType.Conversation,
            Title: "Test",
            Preview: "Test",
            Rank: -5.0,
            Timestamp: timestamp,
            ConversationId: id,
            MessageId: null);

        // Act & Assert
        Assert.Equal(result1, result2);
    }

    /// <summary>
    /// Verifies that two SearchResults with different IDs are not equal.
    /// </summary>
    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var result1 = CreateConversationResult(id: Guid.NewGuid(), timestamp: timestamp);
        var result2 = CreateConversationResult(id: Guid.NewGuid(), timestamp: timestamp);

        // Act & Assert
        Assert.NotEqual(result1, result2);
    }

    /// <summary>
    /// Verifies that SearchResult with immutable through 'with' creates new instance.
    /// </summary>
    [Fact]
    public void With_ModifiesProperty_CreatesNewInstance()
    {
        // Arrange
        var original = CreateConversationResult(title: "Original");

        // Act
        var modified = original with { Title = "Modified" };

        // Assert
        Assert.NotEqual(original, modified);
        Assert.Equal("Original", original.Title);
        Assert.Equal("Modified", modified.Title);
    }

    #endregion

    #region BM25 Rank Interpretation Tests

    /// <summary>
    /// Verifies understanding of BM25 scores where more negative is better.
    /// </summary>
    [Fact]
    public void Rank_MoreNegativeIsBetterMatch()
    {
        // Arrange
        var excellentMatch = CreateConversationResult(rank: -8.0);
        var goodMatch = CreateConversationResult(rank: -4.0);
        var fairMatch = CreateConversationResult(rank: -1.0);

        // Act - Sort by rank ascending (as done in SearchAsync)
        var sorted = new[] { fairMatch, excellentMatch, goodMatch }
            .OrderBy(r => r.Rank)
            .ToList();

        // Assert - Most negative (best match) should come first
        Assert.Equal(excellentMatch, sorted[0]);
        Assert.Equal(goodMatch, sorted[1]);
        Assert.Equal(fairMatch, sorted[2]);
    }

    #endregion
}
