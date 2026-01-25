// ============================================================================
// File: TerminalHistoryService.cs
// Path: src/AIntern.Services/Terminal/TerminalHistoryService.cs
// Description: Service implementation for managing terminal command history
//              with SQLite persistence via Entity Framework Core.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

namespace AIntern.Services.Terminal;

using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Data;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL HISTORY SERVICE (v0.5.5i)                                      │
// │ Manages terminal command history with SQLite persistence.               │
// │                                                                         │
// │ Features:                                                               │
// │   • Command persistence with metadata (exit code, duration)             │
// │   • Session-level history grouping                                      │
// │   • Pattern-based search with LIKE queries                              │
// │   • Export to JSON, CSV, Text formats                                   │
// │   • Thread-safe write operations via SemaphoreSlim                      │
// │   • Automatic cleanup of old history                                    │
// │                                                                         │
// │ Dependencies:                                                           │
// │   • IDbContextFactory<AInternDbContext> - for scoped DbContext access   │
// │   • ILogger<TerminalHistoryService> - for diagnostic logging            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for managing terminal command history with SQLite persistence.
/// Thread-safe implementation with async operations.
/// </summary>
/// <remarks>
/// <para>
/// This service provides persistent storage for terminal command history using
/// SQLite via Entity Framework Core. It supports:
/// <list type="bullet">
///   <item><description>Add, retrieve, and search commands</description></item>
///   <item><description>Session-level history grouping</description></item>
///   <item><description>Export to JSON, CSV, and text formats</description></item>
///   <item><description>Automatic cleanup of old entries</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Write operations are protected with a <see cref="SemaphoreSlim"/> to ensure
/// database consistency. Read operations are not locked for performance.
/// </para>
/// <para>
/// <b>DbContext Lifecycle:</b>
/// Uses <see cref="IDbContextFactory{TContext}"/> to create scoped DbContext
/// instances for each operation, as the service is registered as a singleton.
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class TerminalHistoryService : ITerminalHistoryService, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Fields
    // ═══════════════════════════════════════════════════════════════════════════

    private readonly IDbContextFactory<AInternDbContext> _dbContextFactory;
    private readonly ILogger<TerminalHistoryService> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// JSON serializer options for export.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Maximum number of entries to export.
    /// </summary>
    private const int MaxExportCount = 10000;

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of <see cref="TerminalHistoryService"/>.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating DbContext instances.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="dbContextFactory"/> or <paramref name="logger"/> is null.
    /// </exception>
    public TerminalHistoryService(
        IDbContextFactory<AInternDbContext> dbContextFactory,
        ILogger<TerminalHistoryService> logger)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[INIT] TerminalHistoryService initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ITerminalHistoryService Implementation - Command Management
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task AddCommandAsync(string sessionId, TerminalHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(sessionId, nameof(sessionId));
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));

        // Skip empty commands
        if (string.IsNullOrWhiteSpace(entry.Command))
        {
            _logger.LogDebug("[SKIP] AddCommandAsync - Empty command skipped");
            return;
        }

        _logger.LogDebug(
            "[ENTER] AddCommandAsync - SessionId={SessionId}, Command={Command}",
            sessionId,
            TruncateForLog(entry.Command));

        await _writeLock.WaitAsync();
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var entity = new TerminalHistoryEntity
            {
                Id = entry.Id,
                SessionId = sessionId,
                Command = entry.Command,
                ExecutedAt = entry.ExecutedAt,
                WorkingDirectory = entry.WorkingDirectory,
                ExitCode = entry.ExitCode,
                DurationMs = entry.Duration?.TotalMilliseconds,
                ProfileId = entry.ProfileId
            };

            dbContext.TerminalHistory.Add(entity);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "[SUCCESS] AddCommandAsync - Added command to history: {Command} (Session: {SessionId})",
                TruncateForLog(entry.Command),
                sessionId);

            // Raise event
            HistoryChanged?.Invoke(this, HistoryChangedEventArgs.ForAdded(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ERROR] AddCommandAsync - Failed to add command: {Command}",
                TruncateForLog(entry.Command));
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TerminalHistoryEntry>> GetRecentCommandsAsync(int count = 100)
    {
        _logger.LogDebug("[ENTER] GetRecentCommandsAsync - Count={Count}", count);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var entities = await dbContext.TerminalHistory
                .OrderByDescending(h => h.ExecutedAt)
                .Take(count)
                .ToListAsync();

            var results = entities.Select(ToEntry).ToList();

            _logger.LogDebug(
                "[EXIT] GetRecentCommandsAsync - Returned {ResultCount} entries",
                results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] GetRecentCommandsAsync - Failed to retrieve recent commands");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TerminalHistoryEntry>> GetSessionHistoryAsync(string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId, nameof(sessionId));

        _logger.LogDebug("[ENTER] GetSessionHistoryAsync - SessionId={SessionId}", sessionId);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var entities = await dbContext.TerminalHistory
                .Where(h => h.SessionId == sessionId)
                .OrderBy(h => h.ExecutedAt)
                .ToListAsync();

            var results = entities.Select(ToEntry).ToList();

            _logger.LogDebug(
                "[EXIT] GetSessionHistoryAsync - Returned {ResultCount} entries for session {SessionId}",
                results.Count,
                sessionId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ERROR] GetSessionHistoryAsync - Failed to retrieve history for session {SessionId}",
                sessionId);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ITerminalHistoryService Implementation - Search Operations
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<IReadOnlyList<TerminalHistoryEntry>> SearchHistoryAsync(
        string query,
        int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("[SKIP] SearchHistoryAsync - Empty query, returning empty results");
            return Array.Empty<TerminalHistoryEntry>();
        }

        _logger.LogDebug(
            "[ENTER] SearchHistoryAsync - Query={Query}, MaxResults={MaxResults}",
            TruncateForLog(query),
            maxResults);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Use EF.Functions.Like for case-insensitive search
            var entities = await dbContext.TerminalHistory
                .Where(h => EF.Functions.Like(h.Command, $"%{query}%"))
                .OrderByDescending(h => h.ExecutedAt)
                .Take(maxResults)
                .ToListAsync();

            var results = entities.Select(ToEntry).ToList();

            _logger.LogDebug(
                "[EXIT] SearchHistoryAsync - Found {ResultCount} matches for query '{Query}'",
                results.Count,
                TruncateForLog(query));

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ERROR] SearchHistoryAsync - Failed to search for query '{Query}'",
                TruncateForLog(query));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetUniqueCommandsAsync(int count = 100)
    {
        _logger.LogDebug("[ENTER] GetUniqueCommandsAsync - Count={Count}", count);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Get distinct commands, most recently used first
            // Group by command, take the max ExecutedAt for ordering
            var commands = await dbContext.TerminalHistory
                .GroupBy(h => h.Command)
                .Select(g => new
                {
                    Command = g.Key,
                    LastUsed = g.Max(h => h.ExecutedAt)
                })
                .OrderByDescending(x => x.LastUsed)
                .Take(count)
                .Select(x => x.Command)
                .ToListAsync();

            _logger.LogDebug(
                "[EXIT] GetUniqueCommandsAsync - Returned {ResultCount} unique commands",
                commands.Count);

            return commands;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] GetUniqueCommandsAsync - Failed to retrieve unique commands");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ITerminalHistoryService Implementation - History Cleanup
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task ClearAllHistoryAsync()
    {
        _logger.LogInformation("[ENTER] ClearAllHistoryAsync - Clearing all terminal history");

        await _writeLock.WaitAsync();
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var deleted = await dbContext.TerminalHistory.ExecuteDeleteAsync();

            _logger.LogInformation(
                "[EXIT] ClearAllHistoryAsync - Deleted {Count} history entries",
                deleted);

            HistoryChanged?.Invoke(this, HistoryChangedEventArgs.ForCleared());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] ClearAllHistoryAsync - Failed to clear history");
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearHistoryOlderThanAsync(DateTime cutoffDate)
    {
        _logger.LogInformation(
            "[ENTER] ClearHistoryOlderThanAsync - Clearing history older than {CutoffDate:O}",
            cutoffDate);

        await _writeLock.WaitAsync();
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var deleted = await dbContext.TerminalHistory
                .Where(h => h.ExecutedAt < cutoffDate)
                .ExecuteDeleteAsync();

            _logger.LogInformation(
                "[EXIT] ClearHistoryOlderThanAsync - Deleted {Count} old history entries",
                deleted);

            if (deleted > 0)
            {
                HistoryChanged?.Invoke(this, HistoryChangedEventArgs.ForCleared());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ERROR] ClearHistoryOlderThanAsync - Failed to clear old history (cutoff: {CutoffDate:O})",
                cutoffDate);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ITerminalHistoryService Implementation - Session Records
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<IReadOnlyList<TerminalSessionHistory>> GetSessionRecordsAsync(int count = 20)
    {
        _logger.LogDebug("[ENTER] GetSessionRecordsAsync - Count={Count}", count);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Get distinct sessions with their command counts and time range
            var sessionGroups = await dbContext.TerminalHistory
                .GroupBy(h => h.SessionId)
                .Select(g => new
                {
                    SessionId = g.Key,
                    StartedAt = g.Min(h => h.ExecutedAt),
                    EndedAt = g.Max(h => h.ExecutedAt),
                    CommandCount = g.Count(),
                    ProfileId = g.First().ProfileId
                })
                .OrderByDescending(s => s.StartedAt)
                .Take(count)
                .ToListAsync();

            var result = new List<TerminalSessionHistory>();

            foreach (var session in sessionGroups)
            {
                // Get all commands for this session
                var commands = await dbContext.TerminalHistory
                    .Where(h => h.SessionId == session.SessionId)
                    .OrderBy(h => h.ExecutedAt)
                    .ToListAsync();

                result.Add(new TerminalSessionHistory
                {
                    SessionId = session.SessionId,
                    StartedAt = session.StartedAt,
                    EndedAt = session.EndedAt,
                    ProfileId = session.ProfileId,
                    Commands = commands.Select(ToEntry).ToList()
                });
            }

            _logger.LogDebug(
                "[EXIT] GetSessionRecordsAsync - Returned {SessionCount} sessions",
                result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] GetSessionRecordsAsync - Failed to retrieve session records");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ITerminalHistoryService Implementation - Export Operations
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task ExportHistoryAsync(string filePath, HistoryExportFormat format)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath, nameof(filePath));

        _logger.LogInformation(
            "[ENTER] ExportHistoryAsync - Path={FilePath}, Format={Format}",
            filePath,
            format);

        try
        {
            var history = await GetRecentCommandsAsync(MaxExportCount);

            switch (format)
            {
                case HistoryExportFormat.Json:
                    await ExportAsJsonAsync(filePath, history);
                    break;

                case HistoryExportFormat.Csv:
                    await ExportAsCsvAsync(filePath, history);
                    break;

                case HistoryExportFormat.Text:
                    await ExportAsTextAsync(filePath, history);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Invalid export format");
            }

            _logger.LogInformation(
                "[EXIT] ExportHistoryAsync - Exported {Count} entries to {FilePath}",
                history.Count,
                filePath);
        }
        catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
        {
            _logger.LogError(ex,
                "[ERROR] ExportHistoryAsync - Failed to export history to {FilePath}",
                filePath);
            throw;
        }
    }

    /// <summary>
    /// Exports history as JSON.
    /// </summary>
    private static async Task ExportAsJsonAsync(
        string filePath,
        IReadOnlyList<TerminalHistoryEntry> history)
    {
        // Create export DTOs with consistent property names
        var exportData = history.Select(h => new
        {
            id = h.Id,
            command = h.Command,
            executedAt = h.ExecutedAt,
            workingDirectory = h.WorkingDirectory,
            exitCode = h.ExitCode,
            durationMs = h.Duration?.TotalMilliseconds,
            profileId = h.ProfileId
        });

        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }

    /// <summary>
    /// Exports history as CSV.
    /// </summary>
    private static async Task ExportAsCsvAsync(
        string filePath,
        IReadOnlyList<TerminalHistoryEntry> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Command,ExecutedAt,WorkingDirectory,ExitCode,DurationMs");

        foreach (var entry in history)
        {
            // Escape quotes and wrap fields containing special characters
            var command = EscapeCsvField(entry.Command);
            var workDir = EscapeCsvField(entry.WorkingDirectory ?? "");
            var durationMs = entry.Duration?.TotalMilliseconds.ToString("F0") ?? "";

            sb.AppendLine($"{command},{entry.ExecutedAt:O},{workDir},{entry.ExitCode},{durationMs}");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Exports history as plain text (one command per line).
    /// </summary>
    private static async Task ExportAsTextAsync(
        string filePath,
        IReadOnlyList<TerminalHistoryEntry> history)
    {
        var commands = history.Select(h => h.Command);
        await File.WriteAllLinesAsync(filePath, commands, Encoding.UTF8);
    }

    /// <summary>
    /// Escapes a field for CSV output.
    /// </summary>
    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        // If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return $"\"{value}\"";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ITerminalHistoryService Implementation - Statistics
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<int> GetTotalCommandCountAsync()
    {
        _logger.LogDebug("[ENTER] GetTotalCommandCountAsync");

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var count = await dbContext.TerminalHistory.CountAsync();

            _logger.LogDebug("[EXIT] GetTotalCommandCountAsync - Count={Count}", count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] GetTotalCommandCountAsync - Failed to get count");
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public event EventHandler<HistoryChangedEventArgs>? HistoryChanged;

    // ═══════════════════════════════════════════════════════════════════════════
    // Mapping Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts a database entity to a domain model.
    /// </summary>
    private static TerminalHistoryEntry ToEntry(TerminalHistoryEntity entity) => new()
    {
        Id = entity.Id,
        Command = entity.Command,
        ExecutedAt = entity.ExecutedAt,
        WorkingDirectory = entity.WorkingDirectory,
        ExitCode = entity.ExitCode,
        Duration = entity.DurationMs.HasValue
            ? TimeSpan.FromMilliseconds(entity.DurationMs.Value)
            : null,
        ProfileId = entity.ProfileId
    };

    /// <summary>
    /// Truncates text for logging to avoid huge log entries.
    /// </summary>
    private static string TruncateForLog(string text, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(text))
            return "(empty)";

        if (text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IDisposable Implementation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Releases resources used by the service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _writeLock.Dispose();
        _disposed = true;

        _logger.LogDebug("[DISPOSE] TerminalHistoryService disposed");
    }
}
