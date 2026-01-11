using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using AIntern.Core.Entities;

namespace AIntern.Data;

/// <summary>
/// Entity Framework Core database context for the AIntern application.
/// </summary>
public class AInternDbContext : DbContext
{
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<SystemPromptEntity> SystemPrompts => Set<SystemPromptEntity>();
    public DbSet<InferencePresetEntity> InferencePresets => Set<InferencePresetEntity>();
    public DbSet<AppVersionEntity> AppVersions => Set<AppVersionEntity>();

    public AInternDbContext(DbContextOptions<AInternDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AInternDbContext).Assembly);
    }

    /// <summary>Gets the database connection for raw SQL queries.</summary>
    public DbConnection GetConnection() => Database.GetDbConnection();

    /// <summary>Opens the database connection if not already open.</summary>
    public async Task OpenConnectionAsync(CancellationToken ct = default)
        => await Database.OpenConnectionAsync(ct);

    /// <summary>Closes the database connection.</summary>
    public async Task CloseConnectionAsync()
        => await Database.CloseConnectionAsync();
}
