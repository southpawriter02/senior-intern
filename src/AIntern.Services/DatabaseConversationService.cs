namespace AIntern.Services;

using System.Diagnostics;
using System.Timers;
using AIntern.Core.Entities;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data.Repositories;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

/// <summary>
/// Conversation service backed by SQLite database with auto-save functionality.
/// </summary>
/// <remarks>
/// <para>
/// This service manages in-memory conversation state while providing full
/// database persistence through the repository layer. Key features include:
/// </para>
/// <list type="bullet">
///   <item><description>Auto-save with 500ms debouncing</description></item>
///   <item><description>Title auto-generation from first user message</description></item>
///   <item><description>Thread-safe save operations via SemaphoreSlim</description></item>
///   <item><description>Event-driven updates for UI synchronization</description></item>
///   <item><description>Bidirectional entity ↔ domain model mapping</description></item>
/// </list>
/// </remarks>
public sealed class DatabaseConversationService : IConversationService, IDisposable
{
    #region Constants

    /// <summary>
    /// Auto-save debounce interval in milliseconds.
    /// Prevents excessive database writes during rapid message entry.
    /// </summary>
    private const int AutoSaveDelayMs = 500;

    /// <summary>
    /// Maximum length for auto-generated conversation titles.
    /// </summary>
    private const int TitleMaxLength = 50;

    /// <summary>
    /// Maximum length for conversation preview text.
    /// </summary>
    private const int PreviewMaxLength = 100;

    #endregion

    #region Dependencies

    private readonly IConversationRepository _repository;
    private readonly ISystemPromptRepository _systemPromptRepository;
    private readonly ILogger<DatabaseConversationService> _logger;

    #endregion

    #region State

    /// <summary>
    /// The currently active conversation (in-memory).
    /// </summary>
    private Conversation _currentConversation = new();

    /// <summary>
    /// Timer for auto-save debouncing.
    /// </summary>
    private readonly Timer _autoSaveTimer;

    /// <summary>
    /// Lock for thread-safe save operations.
    /// </summary>
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    /// <summary>
    /// Timestamp of last successful save.
    /// </summary>
    private DateTime? _lastSavedAt;

    /// <summary>
    /// Disposal flag.
    /// </summary>
    private bool _isDisposed;

    #endregion

    #region IConversationService Properties

    /// <inheritdoc />
    public Conversation CurrentConversation => _currentConversation;

    /// <inheritdoc />
    public bool HasUnsavedChanges => _currentConversation.HasUnsavedChanges;

    /// <inheritdoc />
    public bool HasActiveConversation =>
        _currentConversation.IsPersisted || _currentConversation.Messages.Count > 0;

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<ConversationChangedEventArgs>? ConversationChanged;

    /// <inheritdoc />
    public event EventHandler<ConversationListChangedEventArgs>? ConversationListChanged;

    /// <inheritdoc />
    public event EventHandler<SaveStateChangedEventArgs>? SaveStateChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConversationService"/> class.
    /// </summary>
    /// <param name="repository">Repository for conversation persistence.</param>
    /// <param name="systemPromptRepository">Repository for system prompt lookup.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DatabaseConversationService(
        IConversationRepository repository,
        ISystemPromptRepository systemPromptRepository,
        ILogger<DatabaseConversationService> logger)
    {
        _repository = repository;
        _systemPromptRepository = systemPromptRepository;
        _logger = logger;

        // Configure auto-save timer with one-shot behavior.
        // Timer is reset on each message change to implement debouncing.
        _autoSaveTimer = new Timer(AutoSaveDelayMs)
        {
            AutoReset = false
        };
        _autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;

        _logger.LogDebug("[INIT] DatabaseConversationService created");
    }

    #endregion

    #region Message Operations

    /// <inheritdoc />
    public void AddMessage(ChatMessage message)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] AddMessage - Role: {Role}, ContentLength: {Length}",
            message.Role, message.Content.Length);

        _currentConversation.AddMessage(message);

        // Auto-generate title from first user message.
        if (_currentConversation.Messages.Count == 1 &&
            message.Role == MessageRole.User &&
            _currentConversation.Title == "New Conversation")
        {
            _currentConversation.Title = GenerateTitle(message.Content);
            _logger.LogDebug("Auto-generated title: {Title}", _currentConversation.Title);
        }

        OnConversationChanged(ConversationChangeType.MessageAdded);
        ScheduleAutoSave();

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] AddMessage - Complete in {ElapsedMs}ms",
            stopwatch.ElapsedMilliseconds);
    }

    /// <inheritdoc />
    public void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction)
    {
        _currentConversation.UpdateMessage(messageId, updateAction);
        OnConversationChanged(ConversationChangeType.MessageUpdated);
        ScheduleAutoSave();
    }

    /// <inheritdoc />
    public void RemoveMessage(Guid messageId)
    {
        _currentConversation.RemoveMessage(messageId);
        OnConversationChanged(ConversationChangeType.MessageRemoved);
        ScheduleAutoSave();
    }

    /// <inheritdoc />
    public IReadOnlyList<ChatMessage> GetMessages() => _currentConversation.Messages;

    /// <inheritdoc />
    public void ClearConversation()
    {
        _logger.LogDebug("[ENTER] ClearConversation - Id: {ConversationId}", _currentConversation.Id);

        _currentConversation.ClearMessages();

        OnConversationChanged(ConversationChangeType.Cleared);
        ScheduleAutoSave();

        _logger.LogDebug("[EXIT] ClearConversation - Messages cleared");
    }

    #endregion

    #region Conversation CRUD

    /// <inheritdoc />
    public async Task<Conversation> CreateNewConversationAsync(
        string? title = null,
        Guid? systemPromptId = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] CreateNewConversationAsync - Title: {Title}", title ?? "(auto)");

        // Save current conversation if it has unsaved changes.
        if (_currentConversation.HasUnsavedChanges)
        {
            await SaveCurrentConversationAsync(ct);
        }

        var previousId = _currentConversation.IsPersisted ? _currentConversation.Id : (Guid?)null;

        // Create new in-memory conversation.
        _currentConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = title ?? "New Conversation",
            SystemPromptId = systemPromptId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Load system prompt name for display if specified.
        if (systemPromptId.HasValue)
        {
            var prompt = await _systemPromptRepository.GetByIdAsync(systemPromptId.Value, ct);
            _currentConversation.SystemPromptName = prompt?.Name;
            _currentConversation.SystemPrompt = prompt?.Content;
        }

        OnConversationChanged(ConversationChangeType.Created, previousId);

        stopwatch.Stop();
        _logger.LogInformation(
            "[EXIT] CreateNewConversationAsync - Created {ConversationId} in {ElapsedMs}ms",
            _currentConversation.Id, stopwatch.ElapsedMilliseconds);

        return _currentConversation;
    }

    /// <inheritdoc />
    public async Task<Conversation> LoadConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] LoadConversationAsync - Id: {ConversationId}", conversationId);

        // Save current conversation if it has unsaved changes.
        if (_currentConversation.HasUnsavedChanges)
        {
            await SaveCurrentConversationAsync(ct);
        }

        var previousId = _currentConversation.IsPersisted ? _currentConversation.Id : (Guid?)null;

        // Load from database with messages.
        var entity = await _repository.GetByIdWithMessagesAsync(conversationId, ct);
        if (entity is null)
        {
            _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        // Map entity to domain model.
        _currentConversation = MapToDomain(entity);

        OnConversationChanged(ConversationChangeType.Loaded, previousId);

        stopwatch.Stop();
        _logger.LogInformation(
            "[EXIT] LoadConversationAsync - Loaded {ConversationId}: {Title} ({MessageCount} messages) in {ElapsedMs}ms",
            _currentConversation.Id,
            _currentConversation.Title,
            _currentConversation.Messages.Count,
            stopwatch.ElapsedMilliseconds);

        return _currentConversation;
    }

    /// <inheritdoc />
    public async Task SaveCurrentConversationAsync(CancellationToken ct = default)
    {
        // Skip if no changes to save.
        if (!_currentConversation.HasUnsavedChanges && _currentConversation.IsPersisted)
        {
            _logger.LogDebug("SaveCurrentConversationAsync - No changes to save");
            return;
        }

        // Skip if no messages (don't persist empty conversations).
        if (_currentConversation.Messages.Count == 0)
        {
            _logger.LogDebug("SaveCurrentConversationAsync - No messages to save");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SaveCurrentConversationAsync - Id: {ConversationId}", _currentConversation.Id);

        await _saveLock.WaitAsync(ct);
        try
        {
            OnSaveStateChanged(isSaving: true);

            var entity = MapToEntity(_currentConversation);

            if (_currentConversation.IsPersisted)
            {
                // Update existing conversation.
                await _repository.UpdateAsync(entity, ct);

                // Sync messages (add new, update existing).
                await SyncMessagesAsync(entity.Id, ct);

                OnConversationListChanged(ConversationListChangeType.ConversationUpdated, entity.Id);
            }
            else
            {
                // Create new conversation.
                await _repository.CreateAsync(entity, ct);

                // Add all messages.
                foreach (var message in _currentConversation.Messages)
                {
                    var messageEntity = MapMessageToEntity(message, entity.Id);
                    await _repository.AddMessageAsync(entity.Id, messageEntity, ct);
                }

                _currentConversation.IsPersisted = true;
                OnConversationListChanged(ConversationListChangeType.ConversationAdded, entity.Id);
            }

            _currentConversation.MarkAsSaved();
            _lastSavedAt = DateTime.UtcNow;

            OnSaveStateChanged(isSaving: false);
            OnConversationChanged(ConversationChangeType.Saved);

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] SaveCurrentConversationAsync - Saved {ConversationId} ({MessageCount} messages) in {ElapsedMs}ms",
                _currentConversation.Id,
                _currentConversation.Messages.Count,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[EXIT] SaveCurrentConversationAsync - Failed after {ElapsedMs}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            OnSaveStateChanged(isSaving: false, error: ex.Message);
            throw;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        _logger.LogDebug("[ENTER] DeleteConversationAsync - Id: {ConversationId}", conversationId);

        await _repository.DeleteAsync(conversationId, ct);

        // If deleting current conversation, create a new one.
        if (_currentConversation.Id == conversationId)
        {
            await CreateNewConversationAsync(ct: ct);
        }

        OnConversationListChanged(ConversationListChangeType.ConversationRemoved, conversationId);

        _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);
    }

    /// <inheritdoc />
    public async Task RenameConversationAsync(Guid conversationId, string newTitle, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(conversationId, ct);
        if (entity is null)
        {
            return;
        }

        entity.Title = newTitle.Trim();
        await _repository.UpdateAsync(entity, ct);

        // Update in-memory if current.
        if (_currentConversation.Id == conversationId)
        {
            _currentConversation.Title = newTitle.Trim();
            OnConversationChanged(ConversationChangeType.TitleChanged);
        }

        OnConversationListChanged(ConversationListChangeType.ConversationUpdated, conversationId);

        _logger.LogDebug("Renamed conversation {ConversationId} to {Title}", conversationId, newTitle);
    }

    #endregion

    #region Conversation Flags

    /// <inheritdoc />
    public async Task ArchiveConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        await _repository.ArchiveAsync(conversationId, ct);

        if (_currentConversation.Id == conversationId)
        {
            _currentConversation.IsArchived = true;
        }

        OnConversationListChanged(ConversationListChangeType.ConversationRemoved, conversationId);
        _logger.LogDebug("Archived conversation {ConversationId}", conversationId);
    }

    /// <inheritdoc />
    public async Task UnarchiveConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        await _repository.UnarchiveAsync(conversationId, ct);
        OnConversationListChanged(ConversationListChangeType.ConversationAdded, conversationId);
        _logger.LogDebug("Unarchived conversation {ConversationId}", conversationId);
    }

    /// <inheritdoc />
    public async Task PinConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        await _repository.PinAsync(conversationId, ct);

        if (_currentConversation.Id == conversationId)
        {
            _currentConversation.IsPinned = true;
        }

        OnConversationListChanged(ConversationListChangeType.ConversationUpdated, conversationId);
        _logger.LogDebug("Pinned conversation {ConversationId}", conversationId);
    }

    /// <inheritdoc />
    public async Task UnpinConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        await _repository.UnpinAsync(conversationId, ct);

        if (_currentConversation.Id == conversationId)
        {
            _currentConversation.IsPinned = false;
        }

        OnConversationListChanged(ConversationListChangeType.ConversationUpdated, conversationId);
        _logger.LogDebug("Unpinned conversation {ConversationId}", conversationId);
    }

    #endregion

    #region List Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationSummary>> GetRecentConversationsAsync(
        int count = 50,
        bool includeArchived = false,
        CancellationToken ct = default)
    {
        var entities = await _repository.GetRecentAsync(0, count, includeArchived, ct);
        return entities.Select(MapToSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationSummary>> SearchConversationsAsync(
        string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetRecentConversationsAsync(ct: ct);
        }

        var entities = await _repository.SearchAsync(query, cancellationToken: ct);
        return entities.Select(MapToSummary).ToList();
    }

    #endregion

    #region Auto-Save Timer

    /// <summary>
    /// Schedules an auto-save with debouncing.
    /// </summary>
    private void ScheduleAutoSave()
    {
        // Stop any existing timer to implement debouncing.
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    /// <summary>
    /// Handles auto-save timer elapsed event.
    /// </summary>
    private async void OnAutoSaveTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // Only save if there are changes and at least one message.
        if (!_currentConversation.HasUnsavedChanges || _currentConversation.Messages.Count == 0)
        {
            return;
        }

        try
        {
            await SaveCurrentConversationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-save failed: {Message}", ex.Message);
            // Don't rethrow - auto-save failures are non-fatal.
        }
    }

    #endregion

    #region Mapping Methods

    /// <summary>
    /// Maps a conversation entity to a domain model.
    /// </summary>
    private Conversation MapToDomain(ConversationEntity entity)
    {
        var conversation = new Conversation
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ModelPath = entity.ModelPath,
            ModelName = entity.ModelName,
            SystemPromptId = entity.SystemPromptId,
            SystemPromptName = entity.SystemPrompt?.Name,
            SystemPrompt = entity.SystemPrompt?.Content,
            IsArchived = entity.IsArchived,
            IsPinned = entity.IsPinned,
            IsPersisted = true,
            HasUnsavedChanges = false
        };

        // Load messages from entity.
        var messages = entity.Messages.Select(MapMessageToDomain);
        conversation.LoadMessages(messages);

        return conversation;
    }

    /// <summary>
    /// Maps a domain conversation to an entity for persistence.
    /// </summary>
    private ConversationEntity MapToEntity(Conversation conversation)
    {
        return new ConversationEntity
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            ModelPath = conversation.ModelPath,
            ModelName = conversation.ModelName,
            SystemPromptId = conversation.SystemPromptId,
            IsArchived = conversation.IsArchived,
            IsPinned = conversation.IsPinned,
            MessageCount = conversation.Messages.Count
        };
    }

    /// <summary>
    /// Maps a message entity to a domain ChatMessage.
    /// </summary>
    private static ChatMessage MapMessageToDomain(MessageEntity entity)
    {
        return new ChatMessage
        {
            Id = entity.Id,
            Role = entity.Role,
            Content = entity.Content,
            Timestamp = entity.Timestamp,
            IsComplete = entity.IsComplete,
            TokenCount = entity.TokenCount,
            GenerationTime = entity.GenerationTimeMs.HasValue
                ? TimeSpan.FromMilliseconds(entity.GenerationTimeMs.Value)
                : null,
            SequenceNumber = entity.SequenceNumber
        };
    }

    /// <summary>
    /// Maps a domain ChatMessage to an entity for persistence.
    /// </summary>
    private static MessageEntity MapMessageToEntity(ChatMessage message, Guid conversationId)
    {
        return new MessageEntity
        {
            Id = message.Id,
            ConversationId = conversationId,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            IsComplete = message.IsComplete,
            TokenCount = message.TokenCount,
            GenerationTimeMs = message.GenerationTime.HasValue
                ? (int)message.GenerationTime.Value.TotalMilliseconds
                : null,
            SequenceNumber = message.SequenceNumber
        };
    }

    /// <summary>
    /// Maps a conversation entity to a summary for list display.
    /// </summary>
    private ConversationSummary MapToSummary(ConversationEntity entity)
    {
        // Generate preview from first user message.
        var firstUserMessage = entity.Messages
            .Where(m => m.Role == MessageRole.User)
            .OrderBy(m => m.SequenceNumber)
            .FirstOrDefault();

        var preview = firstUserMessage?.Content;
        if (preview is not null && preview.Length > PreviewMaxLength)
        {
            preview = preview[..PreviewMaxLength] + "...";
        }

        return new ConversationSummary
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            MessageCount = entity.MessageCount,
            Preview = preview,
            IsArchived = entity.IsArchived,
            IsPinned = entity.IsPinned,
            ModelName = entity.ModelName
        };
    }

    #endregion

    #region Message Sync

    /// <summary>
    /// Synchronizes in-memory messages with database.
    /// </summary>
    private async Task SyncMessagesAsync(Guid conversationId, CancellationToken ct)
    {
        // Get existing message IDs from database.
        var existingMessages = await _repository.GetMessagesAsync(conversationId, 0, 1000, ct);
        var existingIds = existingMessages.Select(m => m.Id).ToHashSet();

        // Add or update messages.
        foreach (var message in _currentConversation.Messages)
        {
            var messageEntity = MapMessageToEntity(message, conversationId);

            if (!existingIds.Contains(message.Id))
            {
                // New message - add it.
                await _repository.AddMessageAsync(conversationId, messageEntity, ct);
            }
            else
            {
                // Existing message - update if content changed.
                var existing = existingMessages.First(m => m.Id == message.Id);
                if (existing.Content != message.Content || existing.IsComplete != message.IsComplete)
                {
                    await _repository.UpdateMessageAsync(messageEntity, ct);
                }
            }
        }

        // Delete removed messages.
        var currentIds = _currentConversation.Messages.Select(m => m.Id).ToHashSet();
        foreach (var existingId in existingIds.Where(id => !currentIds.Contains(id)))
        {
            await _repository.DeleteMessageAsync(existingId, ct);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a conversation title from message content.
    /// </summary>
    private static string GenerateTitle(string content)
    {
        // Trim and take first N characters.
        var title = content.Trim();

        // Remove newlines.
        title = title.Replace('\n', ' ').Replace('\r', ' ');

        // Collapse multiple spaces.
        while (title.Contains("  "))
        {
            title = title.Replace("  ", " ");
        }

        // Truncate to max length.
        if (title.Length > TitleMaxLength)
        {
            title = title[..TitleMaxLength].TrimEnd() + "...";
        }

        return title;
    }

    #endregion

    #region Event Helpers

    private void OnConversationChanged(ConversationChangeType changeType, Guid? previousId = null)
    {
        ConversationChanged?.Invoke(this, new ConversationChangedEventArgs
        {
            Conversation = _currentConversation,
            ChangeType = changeType,
            PreviousConversationId = previousId
        });
    }

    private void OnConversationListChanged(ConversationListChangeType changeType, Guid? affectedId = null)
    {
        ConversationListChanged?.Invoke(this, new ConversationListChangedEventArgs
        {
            ChangeType = changeType,
            AffectedConversationId = affectedId
        });
    }

    private void OnSaveStateChanged(bool isSaving, string? error = null)
    {
        SaveStateChanged?.Invoke(this, new SaveStateChangedEventArgs
        {
            IsSaving = isSaving,
            HasUnsavedChanges = _currentConversation.HasUnsavedChanges,
            LastSavedAt = _lastSavedAt,
            Error = error
        });
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;

        _autoSaveTimer.Stop();
        _autoSaveTimer.Elapsed -= OnAutoSaveTimerElapsed;
        _autoSaveTimer.Dispose();

        _saveLock.Dispose();

        _isDisposed = true;
        _logger.LogDebug("[DISPOSE] DatabaseConversationService disposed");
    }

    #endregion
}
