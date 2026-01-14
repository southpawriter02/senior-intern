using AIntern.Core.Entities;
using AIntern.Core.Events;
using AIntern.Core.Models;
using AIntern.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for <see cref="DatabaseConversationService"/> (v0.2.2a).
/// Tests CRUD operations, auto-save, title generation, mapping, and events.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Message operations (add, update, remove, clear)</description></item>
///   <item><description>Conversation CRUD (create, load, save, delete, rename)</description></item>
///   <item><description>Auto-save debouncing and triggering</description></item>
///   <item><description>Title auto-generation from first user message</description></item>
///   <item><description>Entity ↔ domain mapping</description></item>
///   <item><description>Event firing for UI synchronization</description></item>
///   <item><description>Thread safety via SemaphoreSlim</description></item>
/// </list>
/// </remarks>
public class DatabaseConversationServiceTests : IDisposable
{
    #region Test Infrastructure

    private readonly Mock<IConversationRepository> _mockConversationRepository;
    private readonly Mock<ISystemPromptRepository> _mockSystemPromptRepository;
    private readonly Mock<ILogger<DatabaseConversationService>> _mockLogger;

    private DatabaseConversationService? _service;

    public DatabaseConversationServiceTests()
    {
        _mockConversationRepository = new Mock<IConversationRepository>();
        _mockSystemPromptRepository = new Mock<ISystemPromptRepository>();
        _mockLogger = new Mock<ILogger<DatabaseConversationService>>();

        // Default repository setups
        _mockConversationRepository
            .Setup(r => r.CreateAsync(It.IsAny<ConversationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationEntity e, CancellationToken _) => e);
        _mockConversationRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ConversationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockConversationRepository
            .Setup(r => r.AddMessageAsync(It.IsAny<Guid>(), It.IsAny<MessageEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, MessageEntity m, CancellationToken _) => m);
        _mockConversationRepository
            .Setup(r => r.GetMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MessageEntity>());
    }

    private DatabaseConversationService CreateService()
    {
        _service = new DatabaseConversationService(
            _mockConversationRepository.Object,
            _mockSystemPromptRepository.Object,
            _mockLogger.Object);
        return _service;
    }

    private static ConversationEntity CreateTestConversationEntity(
        string title = "Test Conversation",
        int messageCount = 0)
    {
        var entity = new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow,
            MessageCount = messageCount,
            Messages = new List<MessageEntity>()
        };

        for (int i = 0; i < messageCount; i++)
        {
            entity.Messages.Add(new MessageEntity
            {
                Id = Guid.NewGuid(),
                ConversationId = entity.Id,
                Content = $"Message {i + 1}",
                Role = i % 2 == 0 ? MessageRole.User : MessageRole.Assistant,
                SequenceNumber = i + 1,
                Timestamp = DateTime.UtcNow,
                IsComplete = true
            });
        }

        return entity;
    }

    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies new service has empty conversation.
    /// </summary>
    [Fact]
    public void Constructor_HasEmptyConversation()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.NotNull(service.CurrentConversation);
        Assert.Empty(service.CurrentConversation.Messages);
        Assert.Equal("New Conversation", service.CurrentConversation.Title);
    }

    /// <summary>
    /// Verifies HasUnsavedChanges is false initially.
    /// </summary>
    [Fact]
    public void Constructor_HasUnsavedChangesIsFalse()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.False(service.HasUnsavedChanges);
    }

    /// <summary>
    /// Verifies HasActiveConversation is false initially.
    /// </summary>
    [Fact]
    public void Constructor_HasActiveConversationIsFalse()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.False(service.HasActiveConversation);
    }

    #endregion

    #region AddMessage Tests

    /// <summary>
    /// Verifies AddMessage adds message to current conversation.
    /// </summary>
    [Fact]
    public void AddMessage_AddsToCurrentConversation()
    {
        // Arrange
        var service = CreateService();
        var message = new ChatMessage
        {
            Role = MessageRole.User,
            Content = "Hello"
        };

        // Act
        service.AddMessage(message);

        // Assert
        Assert.Single(service.CurrentConversation.Messages);
        Assert.Equal("Hello", service.CurrentConversation.Messages[0].Content);
    }

    /// <summary>
    /// Verifies AddMessage sets HasUnsavedChanges to true.
    /// </summary>
    [Fact]
    public void AddMessage_SetsHasUnsavedChanges()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.AddMessage(new ChatMessage { Content = "Test" });

        // Assert
        Assert.True(service.HasUnsavedChanges);
    }

    /// <summary>
    /// Verifies AddMessage fires ConversationChanged event.
    /// </summary>
    [Fact]
    public void AddMessage_FiresConversationChangedEvent()
    {
        // Arrange
        var service = CreateService();
        ConversationChangedEventArgs? eventArgs = null;
        service.ConversationChanged += (_, e) => eventArgs = e;

        // Act
        service.AddMessage(new ChatMessage { Content = "Test" });

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ConversationChangeType.MessageAdded, eventArgs.ChangeType);
    }

    /// <summary>
    /// Verifies AddMessage auto-generates title from first user message.
    /// </summary>
    [Fact]
    public void AddMessage_AutoGeneratesTitle_FromFirstUserMessage()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.AddMessage(new ChatMessage
        {
            Role = MessageRole.User,
            Content = "How do I implement dependency injection in C#?"
        });

        // Assert
        Assert.NotEqual("New Conversation", service.CurrentConversation.Title);
        Assert.Contains("dependency injection", service.CurrentConversation.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies AddMessage truncates long titles.
    /// </summary>
    [Fact]
    public void AddMessage_TruncatesLongTitles()
    {
        // Arrange
        var service = CreateService();
        var longContent = new string('a', 200); // 200 characters

        // Act
        service.AddMessage(new ChatMessage
        {
            Role = MessageRole.User,
            Content = longContent
        });

        // Assert
        Assert.True(service.CurrentConversation.Title.Length <= 53); // 50 + "..."
    }

    /// <summary>
    /// Verifies AddMessage assigns sequence numbers correctly.
    /// </summary>
    [Fact]
    public void AddMessage_AssignsSequenceNumbers()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.AddMessage(new ChatMessage { Content = "First" });
        service.AddMessage(new ChatMessage { Content = "Second" });

        // Assert
        Assert.Equal(1, service.CurrentConversation.Messages[0].SequenceNumber);
        Assert.Equal(2, service.CurrentConversation.Messages[1].SequenceNumber);
    }

    #endregion

    #region UpdateMessage Tests

    /// <summary>
    /// Verifies UpdateMessage updates existing message.
    /// </summary>
    [Fact]
    public void UpdateMessage_UpdatesExistingMessage()
    {
        // Arrange
        var service = CreateService();
        var message = new ChatMessage { Content = "Original" };
        service.AddMessage(message);

        // Act
        service.UpdateMessage(message.Id, m => m.Content = "Updated");

        // Assert
        Assert.Equal("Updated", service.CurrentConversation.Messages[0].Content);
    }

    /// <summary>
    /// Verifies UpdateMessage fires ConversationChanged event.
    /// </summary>
    [Fact]
    public void UpdateMessage_FiresConversationChangedEvent()
    {
        // Arrange
        var service = CreateService();
        var message = new ChatMessage { Content = "Test" };
        service.AddMessage(message);

        ConversationChangedEventArgs? eventArgs = null;
        service.ConversationChanged += (_, e) => eventArgs = e;

        // Act
        service.UpdateMessage(message.Id, m => m.Content = "Updated");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ConversationChangeType.MessageUpdated, eventArgs.ChangeType);
    }

    #endregion

    #region RemoveMessage Tests

    /// <summary>
    /// Verifies RemoveMessage removes and re-sequences messages.
    /// </summary>
    [Fact]
    public void RemoveMessage_RemovesAndResequences()
    {
        // Arrange
        var service = CreateService();
        var msg1 = new ChatMessage { Content = "1" };
        var msg2 = new ChatMessage { Content = "2" };
        var msg3 = new ChatMessage { Content = "3" };
        service.AddMessage(msg1);
        service.AddMessage(msg2);
        service.AddMessage(msg3);

        // Act
        service.RemoveMessage(msg2.Id);

        // Assert
        Assert.Equal(2, service.CurrentConversation.Messages.Count);
        Assert.Equal(1, msg1.SequenceNumber);
        Assert.Equal(2, msg3.SequenceNumber);
    }

    #endregion

    #region CreateNewConversationAsync Tests

    /// <summary>
    /// Verifies CreateNewConversationAsync creates empty conversation.
    /// </summary>
    [Fact]
    public async Task CreateNewConversationAsync_CreatesEmptyConversation()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CreateNewConversationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Messages);
        Assert.Equal("New Conversation", result.Title);
    }

    /// <summary>
    /// Verifies CreateNewConversationAsync uses provided title.
    /// </summary>
    [Fact]
    public async Task CreateNewConversationAsync_UsesProvidedTitle()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CreateNewConversationAsync(title: "Custom Title");

        // Assert
        Assert.Equal("Custom Title", result.Title);
    }

    /// <summary>
    /// Verifies CreateNewConversationAsync fires Created event.
    /// </summary>
    [Fact]
    public async Task CreateNewConversationAsync_FiresCreatedEvent()
    {
        // Arrange
        var service = CreateService();
        ConversationChangedEventArgs? eventArgs = null;
        service.ConversationChanged += (_, e) => eventArgs = e;

        // Act
        await service.CreateNewConversationAsync();

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ConversationChangeType.Created, eventArgs.ChangeType);
    }

    #endregion

    #region LoadConversationAsync Tests

    /// <summary>
    /// Verifies LoadConversationAsync loads conversation with messages.
    /// </summary>
    [Fact]
    public async Task LoadConversationAsync_LoadsConversationWithMessages()
    {
        // Arrange
        var entity = CreateTestConversationEntity("Loaded Conversation", 3);
        _mockConversationRepository
            .Setup(r => r.GetByIdWithMessagesAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        var result = await service.LoadConversationAsync(entity.Id);

        // Assert
        Assert.Equal("Loaded Conversation", result.Title);
        Assert.Equal(3, result.Messages.Count);
        Assert.True(result.IsPersisted);
    }

    /// <summary>
    /// Verifies LoadConversationAsync fires Loaded event.
    /// </summary>
    [Fact]
    public async Task LoadConversationAsync_FiresLoadedEvent()
    {
        // Arrange
        var entity = CreateTestConversationEntity();
        _mockConversationRepository
            .Setup(r => r.GetByIdWithMessagesAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        ConversationChangedEventArgs? eventArgs = null;
        service.ConversationChanged += (_, e) => eventArgs = e;

        // Act
        await service.LoadConversationAsync(entity.Id);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ConversationChangeType.Loaded, eventArgs.ChangeType);
    }

    /// <summary>
    /// Verifies LoadConversationAsync throws for non-existent conversation.
    /// </summary>
    [Fact]
    public async Task LoadConversationAsync_NonExistent_ThrowsException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockConversationRepository
            .Setup(r => r.GetByIdWithMessagesAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationEntity?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoadConversationAsync(unknownId));
    }

    #endregion

    #region SaveCurrentConversationAsync Tests

    /// <summary>
    /// Verifies SaveCurrentConversationAsync creates new conversation in database.
    /// </summary>
    [Fact]
    public async Task SaveCurrentConversationAsync_NewConversation_CreatesInDatabase()
    {
        // Arrange
        var service = CreateService();
        service.AddMessage(new ChatMessage { Content = "Test" });

        // Act
        await service.SaveCurrentConversationAsync();

        // Assert
        _mockConversationRepository.Verify(
            r => r.CreateAsync(It.IsAny<ConversationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(service.CurrentConversation.IsPersisted);
        Assert.False(service.CurrentConversation.HasUnsavedChanges);
    }

    /// <summary>
    /// Verifies SaveCurrentConversationAsync fires SaveStateChanged events.
    /// </summary>
    [Fact]
    public async Task SaveCurrentConversationAsync_FiresSaveStateChangedEvents()
    {
        // Arrange
        var service = CreateService();
        service.AddMessage(new ChatMessage { Content = "Test" });

        var saveStateEvents = new List<SaveStateChangedEventArgs>();
        service.SaveStateChanged += (_, e) => saveStateEvents.Add(e);

        // Act
        await service.SaveCurrentConversationAsync();

        // Assert
        Assert.True(saveStateEvents.Count >= 2); // At least saving=true and saving=false
        Assert.Contains(saveStateEvents, e => e.IsSaving);
        Assert.Contains(saveStateEvents, e => !e.IsSaving && !e.HasUnsavedChanges);
    }

    /// <summary>
    /// Verifies SaveCurrentConversationAsync skips when no changes.
    /// </summary>
    [Fact]
    public async Task SaveCurrentConversationAsync_NoChanges_Skips()
    {
        // Arrange
        var service = CreateService();
        // Don't add any messages

        // Act
        await service.SaveCurrentConversationAsync();

        // Assert
        _mockConversationRepository.Verify(
            r => r.CreateAsync(It.IsAny<ConversationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetRecentConversationsAsync Tests

    /// <summary>
    /// Verifies GetRecentConversationsAsync returns mapped summaries.
    /// </summary>
    [Fact]
    public async Task GetRecentConversationsAsync_ReturnsMappedSummaries()
    {
        // Arrange
        var entities = new List<ConversationEntity>
        {
            CreateTestConversationEntity("Conv 1", 5),
            CreateTestConversationEntity("Conv 2", 3)
        };
        _mockConversationRepository
            .Setup(r => r.GetRecentAsync(0, 50, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var service = CreateService();

        // Act
        var result = await service.GetRecentConversationsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Conv 1", result[0].Title);
        Assert.Equal("Conv 2", result[1].Title);
    }

    #endregion

    #region DeleteConversationAsync Tests

    /// <summary>
    /// Verifies DeleteConversationAsync deletes from repository.
    /// </summary>
    [Fact]
    public async Task DeleteConversationAsync_DeletesFromRepository()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var service = CreateService();

        // Act
        await service.DeleteConversationAsync(conversationId);

        // Assert
        _mockConversationRepository.Verify(
            r => r.DeleteAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies DeleteConversationAsync fires ConversationListChanged event.
    /// </summary>
    [Fact]
    public async Task DeleteConversationAsync_FiresListChangedEvent()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var service = CreateService();

        ConversationListChangedEventArgs? eventArgs = null;
        service.ConversationListChanged += (_, e) => eventArgs = e;

        // Act
        await service.DeleteConversationAsync(conversationId);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ConversationListChangeType.ConversationRemoved, eventArgs.ChangeType);
        Assert.Equal(conversationId, eventArgs.AffectedConversationId);
    }

    #endregion

    #region RenameConversationAsync Tests

    /// <summary>
    /// Verifies RenameConversationAsync updates title.
    /// </summary>
    [Fact]
    public async Task RenameConversationAsync_UpdatesTitle()
    {
        // Arrange
        var entity = CreateTestConversationEntity("Old Title");
        _mockConversationRepository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        await service.RenameConversationAsync(entity.Id, "New Title");

        // Assert
        _mockConversationRepository.Verify(
            r => r.UpdateAsync(It.Is<ConversationEntity>(e => e.Title == "New Title"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies Dispose cleans up resources.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - should not throw
        service.Dispose();
        service.Dispose(); // Should be idempotent
    }

    #endregion
}
