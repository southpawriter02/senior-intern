// ============================================================================
// File: TerminalHistoryEntity.cs
// Path: src/AIntern.Core/Entities/TerminalHistoryEntity.cs
// Description: Database entity for terminal command history persistence via
//              Entity Framework Core.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

namespace AIntern.Core.Entities;

/// <summary>
/// Database entity for terminal command history.
/// </summary>
/// <remarks>
/// <para>
/// This entity maps to the <c>TerminalHistory</c> table in SQLite.
/// It stores command execution data with metadata for later retrieval,
/// search, and analysis.
/// </para>
/// <para>
/// <b>Table Schema:</b>
/// <list type="table">
///   <listheader>
///     <term>Column</term>
///     <description>Type / Constraints</description>
///   </listheader>
///   <item>
///     <term>Id</term>
///     <description>TEXT(36), PRIMARY KEY</description>
///   </item>
///   <item>
///     <term>SessionId</term>
///     <description>TEXT(36), NOT NULL, INDEXED</description>
///   </item>
///   <item>
///     <term>Command</term>
///     <description>TEXT, NOT NULL, INDEXED</description>
///   </item>
///   <item>
///     <term>ExecutedAt</term>
///     <description>DATETIME, NOT NULL, INDEXED</description>
///   </item>
///   <item>
///     <term>WorkingDirectory</term>
///     <description>TEXT(1024), NULL</description>
///   </item>
///   <item>
///     <term>ExitCode</term>
///     <description>INTEGER, NULL</description>
///   </item>
///   <item>
///     <term>DurationMs</term>
///     <description>REAL, NULL</description>
///   </item>
///   <item>
///     <term>ProfileId</term>
///     <description>TEXT(36), NULL</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Indexes:</b>
/// <list type="bullet">
///   <item>SessionId - for session-specific queries</item>
///   <item>ExecutedAt - for time-based ordering and cleanup</item>
///   <item>Command - for search operations</item>
/// </list>
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class TerminalHistoryEntity
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Primary Key
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets or sets the unique identifier (GUID string).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Primary key for the TerminalHistory table.
    /// Format: GUID string (36 characters including hyphens).
    /// </para>
    /// </remarks>
    public string Id { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    // Session Association
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets or sets the session ID this command belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// References the terminal session that executed this command.
    /// Used to group commands by session for history browsing.
    /// </para>
    /// <para>
    /// Indexed for efficient session-specific queries.
    /// </para>
    /// </remarks>
    public string SessionId { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    // Command Data
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets or sets the executed command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The full command text as entered by the user.
    /// May include arguments, pipes, and redirections.
    /// </para>
    /// <para>
    /// Indexed for search operations. Note that the index is on the
    /// full command text, which may impact performance for very long
    /// commands.
    /// </para>
    /// </remarks>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the command was executed (UTC).
    /// </summary>
    /// <remarks>
    /// <para>
    /// All timestamps are stored in UTC to avoid timezone issues.
    /// Indexed for time-based ordering and cleanup operations.
    /// </para>
    /// </remarks>
    public DateTime ExecutedAt { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    // Execution Context
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets or sets the working directory when executed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The absolute path to the current working directory at the time
    /// of command execution. Maximum length 1024 characters.
    /// </para>
    /// </remarks>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the exit code of the command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Standard convention: 0 for success, non-zero for failure.
    /// May be null if exit code was not captured.
    /// </para>
    /// </remarks>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stored as a floating-point value for sub-millisecond precision.
    /// Convert to TimeSpan using <c>TimeSpan.FromMilliseconds(DurationMs)</c>.
    /// </para>
    /// </remarks>
    public double? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the shell profile ID used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// References the <c>ShellProfile.Id</c> that was active when the
    /// command was executed. Maximum length 36 characters.
    /// </para>
    /// </remarks>
    public string? ProfileId { get; set; }
}
