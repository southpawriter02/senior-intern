// ============================================================================
// File: TerminalHistoryConfiguration.cs
// Path: src/AIntern.Data/Configurations/TerminalHistoryConfiguration.cs
// Description: Entity Framework Core configuration for the TerminalHistory
//              table, defining schema, constraints, and indexes.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

namespace AIntern.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

/// <summary>
/// EF Core configuration for the TerminalHistory table.
/// </summary>
/// <remarks>
/// <para>
/// This configuration defines the schema for terminal command history storage:
/// <list type="bullet">
///   <item><description>Primary key: Id (GUID string)</description></item>
///   <item><description>Required fields: SessionId, Command, ExecutedAt</description></item>
///   <item><description>Optional fields: WorkingDirectory, ExitCode, DurationMs, ProfileId</description></item>
///   <item><description>Indexes: SessionId, ExecutedAt, Command</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Index Strategy:</b>
/// <list type="bullet">
///   <item>
///     <b>SessionId Index:</b> Optimizes session-specific queries like
///     "get all commands for session X".
///   </item>
///   <item>
///     <b>ExecutedAt Index:</b> Optimizes time-based ordering and cleanup
///     operations like "delete entries older than date".
///   </item>
///   <item>
///     <b>Command Index:</b> Optimizes LIKE-based search operations.
///     Note: For very long commands, this may have performance implications.
///   </item>
/// </list>
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class TerminalHistoryConfiguration : IEntityTypeConfiguration<TerminalHistoryEntity>
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Constants
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The database table name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by <see cref="AInternDbContext.OnModelCreating"/> for logging
    /// the configured tables.
    /// </para>
    /// </remarks>
    public const string TableName = "TerminalHistory";

    /// <summary>
    /// Maximum length for GUID string fields (Id, SessionId, ProfileId).
    /// </summary>
    private const int GuidMaxLength = 36;

    /// <summary>
    /// Maximum length for the WorkingDirectory field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 1024 characters should accommodate most path lengths, including
    /// Windows paths (260 chars traditional, 32767 with long path support)
    /// and Unix paths (typically limited by PATH_MAX of 4096).
    /// </para>
    /// </remarks>
    private const int WorkingDirectoryMaxLength = 1024;

    // ═══════════════════════════════════════════════════════════════════════════
    // Configuration
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Configures the entity type for <see cref="TerminalHistoryEntity"/>.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    /// <remarks>
    /// <para>
    /// This method is called by EF Core during model creation via
    /// <c>ApplyConfigurationsFromAssembly</c> in <see cref="AInternDbContext"/>.
    /// </para>
    /// </remarks>
    public void Configure(EntityTypeBuilder<TerminalHistoryEntity> builder)
    {
        // ─────────────────────────────────────────────────────────────────────
        // Table Mapping
        // ─────────────────────────────────────────────────────────────────────

        builder.ToTable(TableName);

        // ─────────────────────────────────────────────────────────────────────
        // Primary Key
        // ─────────────────────────────────────────────────────────────────────

        builder.HasKey(e => e.Id);

        // ─────────────────────────────────────────────────────────────────────
        // Column Configurations
        // ─────────────────────────────────────────────────────────────────────

        // Id: GUID string primary key
        builder.Property(e => e.Id)
            .HasMaxLength(GuidMaxLength)
            .IsRequired();

        // SessionId: Required, indexed for session-specific queries
        builder.Property(e => e.SessionId)
            .HasMaxLength(GuidMaxLength)
            .IsRequired();

        // Command: Required, indexed for search operations
        // No max length constraint - commands can be arbitrarily long
        builder.Property(e => e.Command)
            .IsRequired();

        // ExecutedAt: Required, indexed for time-based operations
        builder.Property(e => e.ExecutedAt)
            .IsRequired();

        // WorkingDirectory: Optional, limited to reasonable path length
        builder.Property(e => e.WorkingDirectory)
            .HasMaxLength(WorkingDirectoryMaxLength);

        // ExitCode: Optional integer
        builder.Property(e => e.ExitCode);

        // DurationMs: Optional floating-point for sub-millisecond precision
        builder.Property(e => e.DurationMs);

        // ProfileId: Optional GUID reference to shell profile
        builder.Property(e => e.ProfileId)
            .HasMaxLength(GuidMaxLength);

        // ─────────────────────────────────────────────────────────────────────
        // Indexes
        // ─────────────────────────────────────────────────────────────────────

        // Index on SessionId for session-specific queries:
        // SELECT * FROM TerminalHistory WHERE SessionId = ?
        builder.HasIndex(e => e.SessionId)
            .HasDatabaseName("IX_TerminalHistory_SessionId");

        // Index on ExecutedAt for time-based ordering and cleanup:
        // SELECT * FROM TerminalHistory ORDER BY ExecutedAt DESC
        // DELETE FROM TerminalHistory WHERE ExecutedAt < ?
        builder.HasIndex(e => e.ExecutedAt)
            .HasDatabaseName("IX_TerminalHistory_ExecutedAt");

        // Index on Command for search operations:
        // SELECT * FROM TerminalHistory WHERE Command LIKE '%query%'
        // Note: SQLite LIKE is case-insensitive by default for ASCII
        builder.HasIndex(e => e.Command)
            .HasDatabaseName("IX_TerminalHistory_Command");
    }
}
