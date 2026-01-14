namespace AIntern.Core.Entities;

using AIntern.Core.Models;

/// <summary>
/// Database entity for file context history attached to messages.
/// </summary>
/// <remarks>
/// <para>
/// This entity stores metadata about files that were attached to chat messages
/// as context. The actual file content is NOT stored - only enough information
/// to identify and re-load the file if needed.
/// </para>
/// <para>
/// When a conversation or message is deleted, associated file contexts are
/// automatically removed via cascade delete.
/// </para>
/// </remarks>
public sealed class FileContextEntity
{
    #region Primary Key

    /// <summary>
    /// Gets or sets the unique identifier for this context.
    /// </summary>
    public Guid Id { get; set; }

    #endregion

    #region Foreign Keys

    /// <summary>
    /// Gets or sets the conversation this context belongs to.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the message this context was attached to.
    /// </summary>
    public Guid MessageId { get; set; }

    #endregion

    #region File Information

    /// <summary>
    /// Gets or sets the absolute file path at time of attachment.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the file name (preserved for display if file is moved/deleted).
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Gets or sets the detected programming language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the hash of content at time of attachment.
    /// </summary>
    /// <remarks>
    /// Used to detect if the file has changed since it was attached.
    /// </remarks>
    public required string ContentHash { get; set; }

    #endregion

    #region Content Metadata

    /// <summary>
    /// Gets or sets the line count of attached content.
    /// </summary>
    public int LineCount { get; set; }

    /// <summary>
    /// Gets or sets the estimated token count.
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// Gets or sets the starting line if partial content (1-indexed).
    /// </summary>
    /// <remarks>
    /// Null if the entire file was attached.
    /// </remarks>
    public int? StartLine { get; set; }

    /// <summary>
    /// Gets or sets the ending line if partial content (1-indexed, inclusive).
    /// </summary>
    /// <remarks>
    /// Null if the entire file was attached.
    /// </remarks>
    public int? EndLine { get; set; }

    /// <summary>
    /// Gets or sets when the context was attached.
    /// </summary>
    public DateTime AttachedAt { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the parent conversation.
    /// </summary>
    public ConversationEntity? Conversation { get; set; }

    /// <summary>
    /// Gets or sets the parent message.
    /// </summary>
    public MessageEntity? Message { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether this was a partial file (selection) rather than full file.
    /// </summary>
    public bool IsPartialContent => StartLine.HasValue || EndLine.HasValue;

    /// <summary>
    /// Gets the display label for UI.
    /// </summary>
    public string DisplayLabel => IsPartialContent
        ? $"{FileName} (lines {StartLine}-{EndLine})"
        : FileName;

    #endregion

    #region Mapping Methods

    /// <summary>
    /// Creates an entity from a FileContext domain model.
    /// </summary>
    /// <param name="context">The file context to convert.</param>
    /// <param name="conversationId">The parent conversation ID.</param>
    /// <param name="messageId">The parent message ID.</param>
    /// <returns>A new entity with all properties mapped.</returns>
    public static FileContextEntity FromFileContext(
        FileContext context,
        Guid conversationId,
        Guid messageId)
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
    /// Creates a lightweight stub without content for history queries.
    /// </summary>
    /// <returns>A FileContextStub with all metadata.</returns>
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

    #endregion
}

/// <summary>
/// Lightweight representation of FileContext without content.
/// </summary>
/// <remarks>
/// <para>
/// Used when loading file context history where the actual content
/// must be re-read from disk. This avoids storing potentially large
/// file contents in the database.
/// </para>
/// <para>
/// Check <see cref="FileExists"/> before attempting to reload content.
/// </para>
/// </remarks>
public sealed class FileContextStub
{
    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the absolute file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the detected programming language.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the content hash at time of attachment.
    /// </summary>
    public required string ContentHash { get; init; }

    /// <summary>
    /// Gets the line count.
    /// </summary>
    public int LineCount { get; init; }

    /// <summary>
    /// Gets the estimated token count.
    /// </summary>
    public int EstimatedTokens { get; init; }

    /// <summary>
    /// Gets the starting line if partial content.
    /// </summary>
    public int? StartLine { get; init; }

    /// <summary>
    /// Gets the ending line if partial content.
    /// </summary>
    public int? EndLine { get; init; }

    /// <summary>
    /// Gets when the context was attached.
    /// </summary>
    public DateTime AttachedAt { get; init; }

    /// <summary>
    /// Gets whether this was a partial file (selection).
    /// </summary>
    public bool IsPartialContent => StartLine.HasValue || EndLine.HasValue;

    /// <summary>
    /// Gets the display label for UI.
    /// </summary>
    public string DisplayLabel => IsPartialContent
        ? $"{FileName} (lines {StartLine}-{EndLine})"
        : FileName;

    /// <summary>
    /// Gets whether the file still exists on disk.
    /// </summary>
    public bool FileExists => File.Exists(FilePath);
}
