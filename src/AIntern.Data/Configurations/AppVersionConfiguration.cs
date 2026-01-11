using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

namespace AIntern.Data.Configurations;

/// <summary>
/// EF Core configuration for AppVersionEntity.
/// </summary>
public class AppVersionConfiguration : IEntityTypeConfiguration<AppVersionEntity>
{
    public void Configure(EntityTypeBuilder<AppVersionEntity> builder)
    {
        builder.ToTable("AppVersion");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Major).IsRequired();
        builder.Property(e => e.Minor).IsRequired();
        builder.Property(e => e.Patch).IsRequired();
        builder.Property(e => e.MigratedAt).IsRequired();
    }
}
