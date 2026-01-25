// ============================================================================
// File: ITerminalHistoryService.cs
// Path: src/AIntern.Core/Interfaces/ITerminalHistoryService.cs
// Description: Service interface for managing terminal command history with
//              SQLite persistence, search, and export capabilities.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

namespace AIntern.Core.Interfaces;

using AIntern.Core.Models.Terminal;

/// <summary>
/// Service for managing terminal command history with persistence.
/// </summary>
/// <remarks>
/// <para>
/// This service provides comprehensive command history management including:
/// <list type="bullet">
///   <item><description>Command persistence with metadata</description></item>
///   <item><description>Session-level history grouping</description></item>
///   <item><description>Pattern-based search</description></item>
///   <item><description>Export to JSON, CSV, and text formats</description></item>
///   <item><description>Automatic cleanup of old history</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations should be thread-safe. Write operations are protected
/// with locks to ensure data consistency.
/// </para>
/// <para>
/// <b>Events:</b>
/// The <see cref="HistoryChanged"/> event fires when history is modified,
/// allowing UI components to refresh their displays.
/// </para>
/// <para>
/// <b>Typical Usage:</b>
/// <code>
/// // Add a command to history
/// var entry = TerminalHistoryEntry.Create(
///     command: "git status",
///     exitCode: 0,
///     duration: TimeSpan.FromSeconds(0.5));
/// await historyService.AddCommandAsync(sessionId, entry);
///
/// // Search history
/// var results = await historyService.SearchHistoryAsync("git", maxResults: 20);
///
/// // Export history
/// await historyService.ExportHistoryAsync("history.json", HistoryExportFormat.Json);
/// </code>
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public interface ITerminalHistoryService
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Command Management
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a command to the history.
    /// </summary>
    /// <param name="sessionId">The session ID this command belongs to.</param>
    /// <param name="entry">The history entry to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Empty or whitespace-only commands are silently skipped.
    /// The <see cref="HistoryChanged"/> event is raised on successful add.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="sessionId"/> is null.
    /// </exception>
    Task AddCommandAsync(string sessionId, TerminalHistoryEntry entry);

    /// <summary>
    /// Gets recent commands across all sessions.
    /// </summary>
    /// <param name="count">Maximum number of entries to return. Default is 100.</param>
    /// <returns>List of recent history entries, newest first.</returns>
    /// <remarks>
    /// <para>
    /// Results are ordered by <see cref="TerminalHistoryEntry.ExecutedAt"/>
    /// in descending order (most recent first).
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<TerminalHistoryEntry>> GetRecentCommandsAsync(int count = 100);

    /// <summary>
    /// Gets all commands for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to retrieve history for.</param>
    /// <returns>List of history entries in chronological order.</returns>
    /// <remarks>
    /// <para>
    /// Results are ordered by <see cref="TerminalHistoryEntry.ExecutedAt"/>
    /// in ascending order (oldest first) to match execution order.
    /// </para>
    /// <para>
    /// Returns an empty list if no commands exist for the session.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<TerminalHistoryEntry>> GetSessionHistoryAsync(string sessionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Search Operations
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Searches command history using a query string.
    /// </summary>
    /// <param name="query">The search query (substring match).</param>
    /// <param name="maxResults">Maximum results to return. Default is 50.</param>
    /// <returns>Matching history entries, newest first.</returns>
    /// <remarks>
    /// <para>
    /// Search is case-insensitive and matches partial strings.
    /// For example, "git" matches "git status", "git pull", etc.
    /// </para>
    /// <para>
    /// Returns an empty list if query is null or whitespace.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<TerminalHistoryEntry>> SearchHistoryAsync(string query, int maxResults = 50);

    /// <summary>
    /// Gets unique commands (deduplicated).
    /// </summary>
    /// <param name="count">Maximum number of unique commands. Default is 100.</param>
    /// <returns>List of unique command strings, most recent first.</returns>
    /// <remarks>
    /// <para>
    /// Returns distinct command strings, useful for autocomplete suggestions.
    /// Only the command text is returned, not the full entry metadata.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<string>> GetUniqueCommandsAsync(int count = 100);

    // ═══════════════════════════════════════════════════════════════════════════
    // History Cleanup
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clears all command history.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Permanently deletes all history entries from the database.
    /// The <see cref="HistoryChanged"/> event is raised with
    /// <see cref="HistoryChangeType.Cleared"/>.
    /// </para>
    /// <para>
    /// <b>Warning:</b> This operation cannot be undone.
    /// </para>
    /// </remarks>
    Task ClearAllHistoryAsync();

    /// <summary>
    /// Clears history entries older than the specified date.
    /// </summary>
    /// <param name="cutoffDate">Entries before this date will be deleted.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Useful for automatic cleanup of old history to manage database size.
    /// Only entries with <see cref="TerminalHistoryEntry.ExecutedAt"/>
    /// before the cutoff date are deleted.
    /// </para>
    /// <para>
    /// The <see cref="HistoryChanged"/> event is raised if any entries
    /// were deleted.
    /// </para>
    /// </remarks>
    Task ClearHistoryOlderThanAsync(DateTime cutoffDate);

    // ═══════════════════════════════════════════════════════════════════════════
    // Session Records
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets session history records for browsing.
    /// </summary>
    /// <param name="count">Maximum number of sessions to return. Default is 20.</param>
    /// <returns>List of session history records, newest first.</returns>
    /// <remarks>
    /// <para>
    /// Returns session-level aggregations with all commands for each session.
    /// Sessions are ordered by start time (most recent first).
    /// </para>
    /// <para>
    /// Each <see cref="TerminalSessionHistory"/> includes the full list of
    /// commands executed in that session.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<TerminalSessionHistory>> GetSessionRecordsAsync(int count = 20);

    // ═══════════════════════════════════════════════════════════════════════════
    // Export Operations
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Exports history to a file in the specified format.
    /// </summary>
    /// <param name="filePath">Path to write the export file.</param>
    /// <param name="format">Export format to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Exports up to 10,000 most recent history entries.
    /// </para>
    /// <para>
    /// <b>Format Details:</b>
    /// <list type="bullet">
    ///   <item><see cref="HistoryExportFormat.Json"/>: Full metadata as JSON array</item>
    ///   <item><see cref="HistoryExportFormat.Csv"/>: CSV with header row</item>
    ///   <item><see cref="HistoryExportFormat.Text"/>: One command per line</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="filePath"/> is null or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="format"/> is not a valid enum value.
    /// </exception>
    Task ExportHistoryAsync(string filePath, HistoryExportFormat format);

    // ═══════════════════════════════════════════════════════════════════════════
    // Statistics
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the total number of commands in history.
    /// </summary>
    /// <returns>Total command count.</returns>
    Task<int> GetTotalCommandCountAsync();

    // ═══════════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when history changes (add, clear).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this event to receive notifications when history is modified.
    /// The <see cref="HistoryChangedEventArgs"/> contains the change type and,
    /// for add operations, the entry that was added.
    /// </para>
    /// </remarks>
    event EventHandler<HistoryChangedEventArgs>? HistoryChanged;
}

/// <summary>
/// Event arguments for history changes.
/// </summary>
/// <remarks>
/// <para>
/// Provides context about what changed in the history:
/// <list type="bullet">
///   <item>For <see cref="HistoryChangeType.Added"/>: includes the added entry</item>
///   <item>For <see cref="HistoryChangeType.Cleared"/>: Entry is null</item>
/// </list>
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class HistoryChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public HistoryChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the entry that was added (for Add operations).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is only set when <see cref="ChangeType"/> is
    /// <see cref="HistoryChangeType.Added"/>. For clear operations,
    /// this will be null.
    /// </para>
    /// </remarks>
    public TerminalHistoryEntry? Entry { get; init; }

    /// <summary>
    /// Creates event args for an added entry.
    /// </summary>
    /// <param name="entry">The entry that was added.</param>
    /// <returns>Event args for the add operation.</returns>
    public static HistoryChangedEventArgs ForAdded(TerminalHistoryEntry entry) => new()
    {
        ChangeType = HistoryChangeType.Added,
        Entry = entry
    };

    /// <summary>
    /// Creates event args for a clear operation.
    /// </summary>
    /// <returns>Event args for the clear operation.</returns>
    public static HistoryChangedEventArgs ForCleared() => new()
    {
        ChangeType = HistoryChangeType.Cleared,
        Entry = null
    };
}

/// <summary>
/// Type of history change.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public enum HistoryChangeType
{
    /// <summary>
    /// A command was added to history.
    /// </summary>
    Added,

    /// <summary>
    /// History was cleared (fully or partially).
    /// </summary>
    Cleared
}
