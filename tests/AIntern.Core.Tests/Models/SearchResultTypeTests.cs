using AIntern.Core.Enums;
using Xunit;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="SearchResultType"/> enum.
/// Verifies that all expected enum values exist and have correct underlying values.
/// </summary>
/// <remarks>
/// <para>
/// These tests ensure backward compatibility of enum values, which is important for:
/// </para>
/// <list type="bullet">
///   <item><description>Database persistence (if enum values are stored as integers)</description></item>
///   <item><description>API serialization (ensuring consistent JSON representation)</description></item>
///   <item><description>Switch statement completeness in consuming code</description></item>
/// </list>
/// <para>Added in v0.2.5a.</para>
/// </remarks>
public class SearchResultTypeTests
{
    #region Enum Value Tests

    /// <summary>
    /// Verifies that the Conversation enum value exists.
    /// </summary>
    [Fact]
    public void Conversation_EnumValueExists()
    {
        // Arrange & Act
        var value = SearchResultType.Conversation;

        // Assert
        Assert.Equal(SearchResultType.Conversation, value);
    }

    /// <summary>
    /// Verifies that the Message enum value exists.
    /// </summary>
    [Fact]
    public void Message_EnumValueExists()
    {
        // Arrange & Act
        var value = SearchResultType.Message;

        // Assert
        Assert.Equal(SearchResultType.Message, value);
    }

    /// <summary>
    /// Verifies that the enum has exactly 2 values.
    /// </summary>
    /// <remarks>
    /// This test ensures no unexpected values are added without updating tests.
    /// </remarks>
    [Fact]
    public void SearchResultType_HasExactlyTwoValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<SearchResultType>();

        // Assert
        Assert.Equal(2, values.Length);
    }

    /// <summary>
    /// Verifies that Conversation and Message have distinct integer values.
    /// </summary>
    [Fact]
    public void EnumValues_AreDistinct()
    {
        // Arrange & Act
        var conversationValue = (int)SearchResultType.Conversation;
        var messageValue = (int)SearchResultType.Message;

        // Assert
        Assert.NotEqual(conversationValue, messageValue);
    }

    #endregion

    #region Enum String Representation Tests

    /// <summary>
    /// Verifies that Conversation converts to the expected string.
    /// </summary>
    [Fact]
    public void Conversation_ToStringReturnsExpectedValue()
    {
        // Arrange
        var value = SearchResultType.Conversation;

        // Act
        var result = value.ToString();

        // Assert
        Assert.Equal("Conversation", result);
    }

    /// <summary>
    /// Verifies that Message converts to the expected string.
    /// </summary>
    [Fact]
    public void Message_ToStringReturnsExpectedValue()
    {
        // Arrange
        var value = SearchResultType.Message;

        // Act
        var result = value.ToString();

        // Assert
        Assert.Equal("Message", result);
    }

    #endregion

    #region Enum Parsing Tests

    /// <summary>
    /// Verifies that "Conversation" can be parsed to the enum value.
    /// </summary>
    [Fact]
    public void Parse_Conversation_ReturnsConversationValue()
    {
        // Arrange & Act
        var success = Enum.TryParse<SearchResultType>("Conversation", out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(SearchResultType.Conversation, result);
    }

    /// <summary>
    /// Verifies that "Message" can be parsed to the enum value.
    /// </summary>
    [Fact]
    public void Parse_Message_ReturnsMessageValue()
    {
        // Arrange & Act
        var success = Enum.TryParse<SearchResultType>("Message", out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(SearchResultType.Message, result);
    }

    /// <summary>
    /// Verifies that invalid strings fail to parse.
    /// </summary>
    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    [InlineData("conversation")]  // Case-sensitive by default
    public void Parse_InvalidValue_ReturnsFalse(string input)
    {
        // Arrange & Act
        var success = Enum.TryParse<SearchResultType>(input, ignoreCase: false, out _);

        // Assert
        Assert.False(success);
    }

    /// <summary>
    /// Verifies case-insensitive parsing works correctly.
    /// </summary>
    [Theory]
    [InlineData("conversation", SearchResultType.Conversation)]
    [InlineData("CONVERSATION", SearchResultType.Conversation)]
    [InlineData("message", SearchResultType.Message)]
    [InlineData("MESSAGE", SearchResultType.Message)]
    public void Parse_CaseInsensitive_ReturnsCorrectValue(string input, SearchResultType expected)
    {
        // Arrange & Act
        var success = Enum.TryParse<SearchResultType>(input, ignoreCase: true, out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(expected, result);
    }

    #endregion
}
