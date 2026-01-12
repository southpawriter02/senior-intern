using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="Conversation"/> class.
/// Verifies default values, message collection behavior, and timestamp handling.
/// </summary>
public class ConversationTests
{
    /// <summary>
    /// Verifies that a new Conversation has correct default values.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var conversation = new Conversation();

        // Assert
        Assert.NotEqual(Guid.Empty, conversation.Id);
        Assert.Equal("New Conversation", conversation.Title);
        Assert.NotNull(conversation.Messages);
        Assert.Empty(conversation.Messages);
        Assert.Null(conversation.SystemPrompt);
        Assert.Null(conversation.ModelPath);
    }

    /// <summary>
    /// Verifies that messages can be added to and retrieved from the collection.
    /// </summary>
    [Fact]
    public void Messages_CanBeAddedAndRetrieved()
    {
        // Arrange
        var conversation = new Conversation();
        var message = new ChatMessage
        {
            Role = MessageRole.User,
            Content = "Hello"
        };

        // Act
        conversation.Messages.Add(message);

        // Assert
        Assert.Single(conversation.Messages);
        Assert.Equal(message, conversation.Messages[0]);
    }

    /// <summary>
    /// Verifies that the UpdatedAt property is mutable.
    /// </summary>
    [Fact]
    public void UpdatedAt_CanBeModified()
    {
        // Arrange
        var conversation = new Conversation();
        var newTime = DateTime.UtcNow.AddHours(1);

        // Act
        conversation.UpdatedAt = newTime;

        // Assert
        Assert.Equal(newTime, conversation.UpdatedAt);
    }
}
