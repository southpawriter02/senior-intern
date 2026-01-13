using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="Conversation"/> class.
/// Verifies default values, message management, and persistence flags.
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
        Assert.False(conversation.IsPersisted);
        Assert.False(conversation.HasUnsavedChanges);
    }

    /// <summary>
    /// Verifies that AddMessage adds to collection and assigns sequence number.
    /// </summary>
    [Fact]
    public void AddMessage_AddsToCollectionAndAssignsSequenceNumber()
    {
        // Arrange
        var conversation = new Conversation();
        var message = new ChatMessage
        {
            Role = MessageRole.User,
            Content = "Hello"
        };

        // Act
        conversation.AddMessage(message);

        // Assert
        Assert.Single(conversation.Messages);
        Assert.Equal(message, conversation.Messages[0]);
        Assert.Equal(1, message.SequenceNumber);
        Assert.True(conversation.HasUnsavedChanges);
    }

    /// <summary>
    /// Verifies that UpdatedAt property is mutable.
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

    /// <summary>
    /// Verifies that RemoveMessage removes and re-sequences remaining messages.
    /// </summary>
    [Fact]
    public void RemoveMessage_ResequencesRemainingMessages()
    {
        // Arrange
        var conversation = new Conversation();
        var msg1 = new ChatMessage { Id = Guid.NewGuid(), Content = "1" };
        var msg2 = new ChatMessage { Id = Guid.NewGuid(), Content = "2" };
        var msg3 = new ChatMessage { Id = Guid.NewGuid(), Content = "3" };
        conversation.AddMessage(msg1);
        conversation.AddMessage(msg2);
        conversation.AddMessage(msg3);

        // Act
        conversation.RemoveMessage(msg2.Id);

        // Assert
        Assert.Equal(2, conversation.Messages.Count);
        Assert.Equal(1, msg1.SequenceNumber);
        Assert.Equal(2, msg3.SequenceNumber);
    }

    /// <summary>
    /// Verifies MarkAsSaved clears dirty flag and sets persisted.
    /// </summary>
    [Fact]
    public void MarkAsSaved_ClearsDirtyFlagAndSetsPersisted()
    {
        // Arrange
        var conversation = new Conversation();
        conversation.AddMessage(new ChatMessage { Content = "Test" });

        // Act
        conversation.MarkAsSaved();

        // Assert
        Assert.False(conversation.HasUnsavedChanges);
        Assert.True(conversation.IsPersisted);
    }

    /// <summary>
    /// Verifies LoadMessages populates messages ordered by SequenceNumber.
    /// </summary>
    [Fact]
    public void LoadMessages_PopulatesMessagesOrderedBySequence()
    {
        // Arrange
        var conversation = new Conversation();
        var messages = new[]
        {
            new ChatMessage { Content = "Third", SequenceNumber = 3 },
            new ChatMessage { Content = "First", SequenceNumber = 1 },
            new ChatMessage { Content = "Second", SequenceNumber = 2 }
        };

        // Act
        conversation.LoadMessages(messages);

        // Assert
        Assert.Equal(3, conversation.Messages.Count);
        Assert.Equal("First", conversation.Messages[0].Content);
        Assert.Equal("Second", conversation.Messages[1].Content);
        Assert.Equal("Third", conversation.Messages[2].Content);
    }
}
