using AIntern.Core.Events;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Service for managing system prompts with CRUD operations and events.
/// </summary>
public interface ISystemPromptService
{
    /// <summary>
    /// Gets the currently selected system prompt for new conversations.
    /// </summary>
    SystemPrompt? CurrentPrompt { get; }

    // Query methods

    /// <summary>
    /// Gets all user-created (non-built-in) prompts.
    /// </summary>
    Task<IReadOnlyList<SystemPrompt>> GetUserPromptsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all built-in template prompts.
    /// </summary>
    Task<IReadOnlyList<SystemPrompt>> GetTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all prompts (user and built-in).
    /// </summary>
    Task<IReadOnlyList<SystemPrompt>> GetAllPromptsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a prompt by ID.
    /// </summary>
    Task<SystemPrompt?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the default system prompt.
    /// </summary>
    Task<SystemPrompt?> GetDefaultPromptAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the prompt associated with a conversation.
    /// </summary>
    Task<SystemPrompt?> GetPromptForConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Searches prompts by name, description, or content.
    /// </summary>
    Task<IReadOnlyList<SystemPrompt>> SearchPromptsAsync(string query, CancellationToken ct = default);

    // Mutation methods

    /// <summary>
    /// Creates a new custom prompt.
    /// </summary>
    Task<SystemPrompt> CreatePromptAsync(string name, string content, string? description = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a new prompt based on a template.
    /// </summary>
    Task<SystemPrompt> CreateFromTemplateAsync(Guid templateId, string? newName = null, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing prompt. Only non-null parameters are applied.
    /// </summary>
    Task UpdatePromptAsync(Guid id, string? name = null, string? content = null, string? description = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a user-created prompt.
    /// </summary>
    Task DeletePromptAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Duplicates a prompt with a new name.
    /// </summary>
    Task<SystemPrompt> DuplicatePromptAsync(Guid id, string? newName = null, CancellationToken ct = default);

    /// <summary>
    /// Sets a prompt as the default.
    /// </summary>
    Task SetAsDefaultAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets the current prompt for new conversations.
    /// </summary>
    Task SetCurrentPromptAsync(Guid? id, CancellationToken ct = default);

    // Utilities

    /// <summary>
    /// Formats a prompt's content for use in model context.
    /// </summary>
    string FormatPromptForContext(SystemPrompt prompt);

    /// <summary>
    /// Initializes the service, loading saved state and ensuring templates exist.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    // Events

    /// <summary>
    /// Raised when the prompt list changes (create, update, delete).
    /// </summary>
    event EventHandler<PromptListChangedEventArgs>? PromptListChanged;

    /// <summary>
    /// Raised when the current prompt changes.
    /// </summary>
    event EventHandler<CurrentPromptChangedEventArgs>? CurrentPromptChanged;
}
