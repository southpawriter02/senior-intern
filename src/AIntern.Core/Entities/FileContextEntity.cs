using AIntern.Core.Models;

namespace AIntern.Core.Entities;

/// <summary>
/// Entity class for persisting file context metadata to the database.
/// Note: File content is NOT stored - only metadata for history/reference.
/// </summary>
public sealed class FileContextEntity
{
    public Guid Id { get; set; }

    /// <summary>Foreign key to the conversation.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>Foreign key to the message this context was attached to.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Absolute path to the file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>File name extracted from the path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Detected programming language.</summary>
    public string? Language { get; set; }

    /// <summary>Hash of the content at time of attachment.</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>Total line count of the attached content.</summary>
    public int LineCount { get; set; }

    /// <summary>Estimated token count for the content.</summary>
    public int EstimatedTokens { get; set; }

    /// <summary>Starting line number if partial content (1-indexed). Null if entire file.</summary>
    public int? StartLine { get; set; }

    /// <summary>Ending line number if partial content (1-indexed, inclusive). Null if entire file.</summary>
    public int? EndLine { get; set; }

    /// <summary>When the context was attached.</summary>
    public DateTime AttachedAt { get; set; }

    // Navigation properties
    public ConversationEntity? Conversation { get; set; }
    public MessageEntity? Message { get; set; }

    /// <summary>
    /// Creates a new entity from a FileContext model.
    /// </summary>
    public static FileContextEntity FromFileContext(FileContext context, Guid conversationId, Guid messageId)
    {
        return new FileContextEntity
        {
            Id = context.Id,
            ConversationId = conversationId,
            MessageId = messageId,
            FilePath = context.FilePath,
            FileName = context.FileName,
            Language = context.Language,
            ContentHash = context.ContentHash,
            LineCount = context.LineCount,
            EstimatedTokens = context.EstimatedTokens,
            StartLine = context.StartLine,
            EndLine = context.EndLine,
            AttachedAt = context.AttachedAt
        };
    }

    /// <summary>
    /// Converts this entity to a lightweight FileContextStub (without content).
    /// </summary>
    public FileContextStub ToFileContextStub()
    {
        return new FileContextStub
        {
            Id = Id,
            FilePath = FilePath,
            FileName = FileName,
            Language = Language,
            ContentHash = ContentHash,
            LineCount = LineCount,
            EstimatedTokens = EstimatedTokens,
            StartLine = StartLine,
            EndLine = EndLine,
            AttachedAt = AttachedAt
        };
    }
}

/// <summary>
/// Lightweight representation of file context metadata (without actual content).
/// Used when loading history without needing to reload file contents.
/// </summary>
public sealed class FileContextStub
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string? Language { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public int LineCount { get; init; }
    public int EstimatedTokens { get; init; }
    public int? StartLine { get; init; }
    public int? EndLine { get; init; }
    public DateTime AttachedAt { get; init; }

    /// <summary>Whether this is a partial file (selection) or full file.</summary>
    public bool IsPartialContent => StartLine.HasValue || EndLine.HasValue;

    /// <summary>Display label for UI showing file name and line range if applicable.</summary>
    public string DisplayLabel => IsPartialContent
        ? $"{FileName} (lines {StartLine}-{EndLine})"
        : FileName;
}
