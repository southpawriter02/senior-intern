namespace AIntern.Core.Entities;

/// <summary>
/// Entity class for persisting system prompts to the database.
/// </summary>
public sealed class SystemPromptEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Custom";
    public string? TagsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDefault { get; set; }
    public bool IsBuiltIn { get; set; }
    public int UsageCount { get; set; }

    // Navigation property
    public ICollection<ConversationEntity> Conversations { get; set; } = new List<ConversationEntity>();
}
