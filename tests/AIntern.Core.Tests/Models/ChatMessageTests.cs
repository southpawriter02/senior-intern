using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="ChatMessage"/> class.
/// Verifies default values, property initialization, and mutability.
/// </summary>
public class ChatMessageTests
{
    /// <summary>
    /// Verifies that a new ChatMessage has correct default values.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var message = new ChatMessage();

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(string.Empty, message.Content);
        Assert.True(message.IsComplete);
        Assert.Null(message.TokenCount);
        Assert.Null(message.GenerationTime);
    }

    /// <summary>
    /// Verifies that the Timestamp is set to the current UTC time on construction.
    /// </summary>
    [Fact]
    public void Constructor_SetsTimestampToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var message = new ChatMessage();

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(message.Timestamp, before, after);
    }

    /// <summary>
    /// Verifies that the Role property can be set to any MessageRole value.
    /// </summary>
    [Theory]
    [InlineData(MessageRole.System)]
    [InlineData(MessageRole.User)]
    [InlineData(MessageRole.Assistant)]
    public void Role_CanBeSetToAnyValue(MessageRole role)
    {
        // Arrange & Act
        var message = new ChatMessage { Role = role };

        // Assert
        Assert.Equal(role, message.Role);
    }

    /// <summary>
    /// Verifies that the Content property is mutable.
    /// </summary>
    [Fact]
    public void Content_CanBeModified()
    {
        // Arrange
        var message = new ChatMessage { Content = "Initial" };

        // Act
        message.Content = "Updated";

        // Assert
        Assert.Equal("Updated", message.Content);
    }
}
