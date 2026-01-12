using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

namespace AIntern.Data.Configurations;

/// <summary>
/// EF Core configuration for FileContextEntity.
/// </summary>
public class FileContextConfiguration : IEntityTypeConfiguration<FileContextEntity>
{
    public void Configure(EntityTypeBuilder<FileContextEntity> builder)
    {
        builder.ToTable("FileContextHistory");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FilePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(f => f.Language)
            .HasMaxLength(50);

        builder.Property(f => f.ContentHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(f => f.LineCount)
            .IsRequired();

        builder.Property(f => f.EstimatedTokens)
            .IsRequired();

        builder.Property(f => f.AttachedAt)
            .IsRequired();

        // Foreign key to Conversation (cascade delete)
        builder.HasOne(f => f.Conversation)
            .WithMany()
            .HasForeignKey(f => f.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Message (cascade delete)
        builder.HasOne(f => f.Message)
            .WithMany()
            .HasForeignKey(f => f.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on ConversationId for querying context by conversation
        builder.HasIndex(f => f.ConversationId);

        // Index on MessageId for querying context by message
        builder.HasIndex(f => f.MessageId);

        // Index on AttachedAt for chronological queries
        builder.HasIndex(f => f.AttachedAt)
            .IsDescending();
    }
}
