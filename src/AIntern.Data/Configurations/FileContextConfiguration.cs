namespace AIntern.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

/// <summary>
/// EF Core configuration for the FileContextHistory table.
/// </summary>
/// <remarks>
/// <para>
/// Table: FileContextHistory
/// </para>
/// <para>
/// Foreign Keys (CASCADE delete):
/// </para>
/// <list type="bullet">
///   <item><description>ConversationId → Conversations</description></item>
///   <item><description>MessageId → Messages</description></item>
/// </list>
/// <para>
/// Indexes: ConversationId, MessageId, AttachedAt
/// </para>
/// </remarks>
public sealed class FileContextConfiguration : IEntityTypeConfiguration<FileContextEntity>
{
    /// <summary>
    /// The database table name.
    /// </summary>
    public const string TableName = "FileContextHistory";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FileContextEntity> builder)
    {
        builder.ToTable(TableName);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Language)
            .HasMaxLength(50);

        builder.Property(e => e.ContentHash)
            .IsRequired()
            .HasMaxLength(64);

        // Indexes for querying file contexts
        builder.HasIndex(e => e.ConversationId);
        builder.HasIndex(e => e.MessageId);
        builder.HasIndex(e => e.AttachedAt);

        // Foreign key relationships with cascade delete
        // When conversation is deleted, all file contexts are removed
        builder.HasOne(e => e.Conversation)
            .WithMany()
            .HasForeignKey(e => e.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // When message is deleted, all file contexts are removed
        builder.HasOne(e => e.Message)
            .WithMany()
            .HasForeignKey(e => e.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
