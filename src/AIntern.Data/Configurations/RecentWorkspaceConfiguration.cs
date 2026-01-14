namespace AIntern.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

/// <summary>
/// EF Core configuration for the RecentWorkspaces table.
/// </summary>
/// <remarks>
/// <para>
/// Table: RecentWorkspaces
/// </para>
/// <para>
/// Indexes:
/// </para>
/// <list type="bullet">
///   <item><description>RootPath: UNIQUE (prevent duplicate workspaces)</description></item>
///   <item><description>LastAccessedAt: DESC (for recent sorting)</description></item>
/// </list>
/// </remarks>
public sealed class RecentWorkspaceConfiguration : IEntityTypeConfiguration<RecentWorkspaceEntity>
{
    /// <summary>
    /// The database table name.
    /// </summary>
    public const string TableName = "RecentWorkspaces";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RecentWorkspaceEntity> builder)
    {
        builder.ToTable(TableName);

        builder.HasKey(e => e.Id);

        // RootPath must be unique - can't have same workspace twice
        builder.Property(e => e.RootPath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.HasIndex(e => e.RootPath)
            .IsUnique();

        // Index for sorting by most recently accessed
        builder.HasIndex(e => e.LastAccessedAt)
            .IsDescending();

        builder.Property(e => e.Name)
            .HasMaxLength(256);

        builder.Property(e => e.ActiveFilePath)
            .HasMaxLength(1024);

        // JSON columns stored as TEXT
        builder.Property(e => e.OpenFilesJson)
            .HasColumnType("TEXT");

        builder.Property(e => e.ExpandedFoldersJson)
            .HasColumnType("TEXT");

        builder.Property(e => e.IsPinned)
            .HasDefaultValue(false);
    }
}
