using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

namespace AIntern.Data.Configurations;

/// <summary>
/// EF Core configuration for RecentWorkspaceEntity.
/// </summary>
public class RecentWorkspaceConfiguration : IEntityTypeConfiguration<RecentWorkspaceEntity>
{
    public void Configure(EntityTypeBuilder<RecentWorkspaceEntity> builder)
    {
        builder.ToTable("RecentWorkspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(256);

        builder.Property(w => w.RootPath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(w => w.LastAccessedAt)
            .IsRequired();

        builder.Property(w => w.ActiveFilePath)
            .HasMaxLength(1024);

        builder.Property(w => w.IsPinned)
            .HasDefaultValue(false);

        // Unique index on RootPath - only one entry per workspace
        builder.HasIndex(w => w.RootPath)
            .IsUnique();

        // Index on LastAccessedAt for sorting recent workspaces
        builder.HasIndex(w => w.LastAccessedAt)
            .IsDescending();
    }
}
