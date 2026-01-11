using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

namespace AIntern.Data.Configurations;

/// <summary>
/// EF Core configuration for SystemPromptEntity.
/// </summary>
public class SystemPromptConfiguration : IEntityTypeConfiguration<SystemPromptEntity>
{
    public void Configure(EntityTypeBuilder<SystemPromptEntity> builder)
    {
        builder.ToTable("SystemPrompts");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sp => sp.Content)
            .IsRequired();

        builder.Property(sp => sp.Description)
            .HasMaxLength(500);

        builder.Property(sp => sp.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Custom");

        builder.Property(sp => sp.TagsJson)
            .HasMaxLength(1000);

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.Property(sp => sp.UpdatedAt)
            .IsRequired();

        builder.Property(sp => sp.IsDefault)
            .HasDefaultValue(false);

        builder.Property(sp => sp.IsBuiltIn)
            .HasDefaultValue(false);

        builder.Property(sp => sp.UsageCount)
            .HasDefaultValue(0);

        // Unique index on Name
        builder.HasIndex(sp => sp.Name)
            .IsUnique();

        // Index on Category for filtering
        builder.HasIndex(sp => sp.Category);

        // Index on IsDefault for quick lookup
        builder.HasIndex(sp => sp.IsDefault);
    }
}
