namespace AIntern.Core.Models;

/// <summary>
/// Domain model for a system prompt with validation and utility methods.
/// </summary>
public sealed class SystemPrompt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Custom";
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    public bool IsBuiltIn { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int UsageCount { get; set; }

    /// <summary>
    /// Character count of the content.
    /// </summary>
    public int CharacterCount => Content?.Length ?? 0;

    /// <summary>
    /// Estimated token count (~4 characters per token).
    /// </summary>
    public int EstimatedTokenCount => CharacterCount / 4;

    /// <summary>
    /// Creates a duplicate of this prompt with a new ID.
    /// Built-in and default flags are reset.
    /// </summary>
    public SystemPrompt Duplicate(string? newName = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = newName ?? $"{Name} (Copy)",
        Content = Content,
        Description = Description,
        Category = "Custom",
        Tags = Tags.ToList(),
        IsBuiltIn = false,
        IsDefault = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        UsageCount = 0
    };

    /// <summary>
    /// Validates the prompt and returns a result with any errors.
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");
        else if (Name.Length > 100)
            errors.Add("Name must be 100 characters or less");

        if (string.IsNullOrWhiteSpace(Content))
            errors.Add("Content is required");
        else if (Content.Length > 50000)
            errors.Add("Content must be 50,000 characters or less");

        if (Description?.Length > 500)
            errors.Add("Description must be 500 characters or less");

        return new ValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
/// Result of a validation operation.
/// </summary>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ValidationResult Success => new(true, Array.Empty<string>());
}
