using System.Text.Json;
using AIntern.Core.Entities;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

/// <summary>
/// Service for managing system prompts with CRUD operations and event notifications.
/// </summary>
public sealed class SystemPromptService : ISystemPromptService
{
    private readonly ISystemPromptRepository _repository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ISettingsService _settingsService;
    private SystemPrompt? _currentPrompt;

    public SystemPrompt? CurrentPrompt => _currentPrompt;

    public event EventHandler<PromptListChangedEventArgs>? PromptListChanged;
    public event EventHandler<CurrentPromptChangedEventArgs>? CurrentPromptChanged;

    public SystemPromptService(
        ISystemPromptRepository repository,
        IConversationRepository conversationRepository,
        ISettingsService settingsService)
    {
        _repository = repository;
        _conversationRepository = conversationRepository;
        _settingsService = settingsService;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        // Ensure built-in prompts exist
        await _repository.SeedBuiltInPromptsAsync(ct);

        // Restore saved current prompt
        var settings = _settingsService.CurrentSettings;
        if (settings.CurrentSystemPromptId.HasValue)
        {
            _currentPrompt = await GetByIdAsync(settings.CurrentSystemPromptId.Value, ct);
        }

        // Fall back to default if not found
        _currentPrompt ??= await GetDefaultPromptAsync(ct);
    }

    #region Query Methods

    public async Task<IReadOnlyList<SystemPrompt>> GetUserPromptsAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetUserPromptsAsync(ct);
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<SystemPrompt>> GetTemplatesAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetBuiltInPromptsAsync(ct);
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<SystemPrompt>> GetAllPromptsAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetAllAsync(ct);
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<SystemPrompt?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<SystemPrompt?> GetDefaultPromptAsync(CancellationToken ct = default)
    {
        var entity = await _repository.GetDefaultAsync(ct);
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<SystemPrompt?> GetPromptForConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(conversationId, ct);
        if (conversation?.SystemPromptId == null)
            return null;

        return await GetByIdAsync(conversation.SystemPromptId.Value, ct);
    }

    public async Task<IReadOnlyList<SystemPrompt>> SearchPromptsAsync(string query, CancellationToken ct = default)
    {
        var entities = await _repository.SearchAsync(query, ct);
        return entities.Select(MapToDomain).ToList();
    }

    #endregion

    #region Mutation Methods

    public async Task<SystemPrompt> CreatePromptAsync(
        string name, string content, string? description = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required.", nameof(content));

        var trimmedName = name.Trim();
        if (await _repository.NameExistsAsync(trimmedName, ct: ct))
            throw new InvalidOperationException($"A prompt named '{trimmedName}' already exists.");

        var now = DateTime.UtcNow;
        var entity = new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Content = content,
            Description = description?.Trim(),
            Category = "Custom",
            IsBuiltIn = false,
            IsDefault = false,
            CreatedAt = now,
            UpdatedAt = now,
            UsageCount = 0
        };

        await _repository.CreateAsync(entity, ct);
        var prompt = MapToDomain(entity);

        OnPromptListChanged(PromptListChangeType.PromptCreated, prompt.Id, prompt.Name);
        return prompt;
    }

    public async Task<SystemPrompt> CreateFromTemplateAsync(
        Guid templateId, string? newName = null, CancellationToken ct = default)
    {
        var template = await _repository.GetByIdAsync(templateId, ct)
            ?? throw new InvalidOperationException($"Template with ID '{templateId}' not found.");

        // Generate unique name
        var baseName = newName?.Trim() ?? $"{template.Name} (Copy)";
        var finalName = await GenerateUniqueName(baseName, ct);

        return await CreatePromptAsync(finalName, template.Content, template.Description, ct);
    }

    public async Task UpdatePromptAsync(
        Guid id, string? name = null, string? content = null, string? description = null, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Prompt with ID '{id}' not found.");

        if (entity.IsBuiltIn)
            throw new InvalidOperationException("Cannot modify built-in prompts.");

        if (name != null)
        {
            var trimmedName = name.Trim();
            if (await _repository.NameExistsAsync(trimmedName, id, ct))
                throw new InvalidOperationException($"A prompt named '{trimmedName}' already exists.");
            entity.Name = trimmedName;
        }

        if (content != null)
            entity.Content = content;

        if (description != null)
            entity.Description = description.Trim();

        await _repository.UpdateAsync(entity, ct);
        OnPromptListChanged(PromptListChangeType.PromptUpdated, entity.Id, entity.Name);

        // Update current prompt reference if this is the current one
        if (_currentPrompt?.Id == id)
        {
            _currentPrompt = MapToDomain(entity);
        }
    }

    public async Task DeletePromptAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity == null)
            return;

        if (entity.IsBuiltIn)
            throw new InvalidOperationException("Cannot delete built-in prompts.");

        var name = entity.Name;
        await _repository.DeleteAsync(id, ct);
        OnPromptListChanged(PromptListChangeType.PromptDeleted, id, name);

        // Reset to default if deleted was current
        if (_currentPrompt?.Id == id)
        {
            var previous = _currentPrompt;
            _currentPrompt = await GetDefaultPromptAsync(ct);
            await PersistCurrentPromptId(_currentPrompt?.Id);
            OnCurrentPromptChanged(_currentPrompt, previous);
        }
    }

    public async Task<SystemPrompt> DuplicatePromptAsync(Guid id, string? newName = null, CancellationToken ct = default)
    {
        var source = await _repository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Prompt with ID '{id}' not found.");

        var baseName = newName?.Trim() ?? $"{source.Name} (Copy)";
        var finalName = await GenerateUniqueName(baseName, ct);

        return await CreatePromptAsync(finalName, source.Content, source.Description, ct);
    }

    public async Task SetAsDefaultAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Prompt with ID '{id}' not found.");

        await _repository.SetDefaultAsync(id, ct);
        OnPromptListChanged(PromptListChangeType.DefaultChanged, id, entity.Name);
    }

    public async Task SetCurrentPromptAsync(Guid? id, CancellationToken ct = default)
    {
        var previous = _currentPrompt;

        if (id.HasValue)
        {
            _currentPrompt = await GetByIdAsync(id.Value, ct);
            if (_currentPrompt != null)
            {
                await _repository.IncrementUsageCountAsync(id.Value, ct);
            }
        }
        else
        {
            _currentPrompt = null;
        }

        await PersistCurrentPromptId(id);
        OnCurrentPromptChanged(_currentPrompt, previous);
    }

    #endregion

    #region Utilities

    public string FormatPromptForContext(SystemPrompt prompt)
    {
        return prompt.Content.Trim();
    }

    #endregion

    #region Private Helpers

    private static SystemPrompt MapToDomain(SystemPromptEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Content = entity.Content,
        Description = entity.Description,
        Category = entity.Category,
        Tags = ParseTags(entity.TagsJson),
        IsBuiltIn = entity.IsBuiltIn,
        IsDefault = entity.IsDefault,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        UsageCount = entity.UsageCount
    };

    private static IReadOnlyList<string> ParseTags(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private async Task<string> GenerateUniqueName(string baseName, CancellationToken ct)
    {
        if (!await _repository.NameExistsAsync(baseName, ct: ct))
            return baseName;

        for (int i = 1; i < 100; i++)
        {
            var candidateName = $"{baseName} ({i})";
            if (!await _repository.NameExistsAsync(candidateName, ct: ct))
                return candidateName;
        }

        // Fallback with GUID suffix
        return $"{baseName} ({Guid.NewGuid():N})"[..Math.Min(100, baseName.Length + 40)];
    }

    private async Task PersistCurrentPromptId(Guid? id)
    {
        var settings = _settingsService.CurrentSettings;
        settings.CurrentSystemPromptId = id;
        await _settingsService.SaveSettingsAsync(settings);
    }

    private void OnPromptListChanged(PromptListChangeType type, Guid? id = null, string? name = null)
    {
        PromptListChanged?.Invoke(this, new PromptListChangedEventArgs
        {
            ChangeType = type,
            AffectedPromptId = id,
            AffectedPromptName = name
        });
    }

    private void OnCurrentPromptChanged(SystemPrompt? newPrompt, SystemPrompt? previousPrompt)
    {
        CurrentPromptChanged?.Invoke(this, new CurrentPromptChangedEventArgs
        {
            NewPrompt = newPrompt,
            PreviousPrompt = previousPrompt
        });
    }

    #endregion
}
