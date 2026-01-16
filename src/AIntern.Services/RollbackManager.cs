using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ ROLLBACK MANAGER (v0.4.4c)                                               │
// │ Manages rollback of file operations on failure or cancellation.         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Manages rollback of file operations on failure or cancellation.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4c.</para>
/// </remarks>
internal sealed class RollbackManager : IDisposable
{
    private readonly IFileSystemService _fileSystem;
    private readonly IBackupService _backupService;
    private readonly List<RollbackAction> _actions = new();
    private readonly object _lock = new();
    private bool _committed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackManager"/> class.
    /// </summary>
    public RollbackManager(IFileSystemService fileSystem, IBackupService backupService)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Registration Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Register a newly created file for potential deletion.
    /// </summary>
    public void RegisterCreatedFile(string filePath)
    {
        lock (_lock)
        {
            if (_committed) return;

            _actions.Add(new RollbackAction
            {
                Type = RollbackActionType.DeleteCreatedFile,
                FilePath = filePath,
                Order = _actions.Count
            });
        }
    }

    /// <summary>
    /// Register a modified file for potential restoration.
    /// </summary>
    public void RegisterModifiedFile(string filePath, string backupPath)
    {
        lock (_lock)
        {
            if (_committed) return;

            _actions.Add(new RollbackAction
            {
                Type = RollbackActionType.RestoreModifiedFile,
                FilePath = filePath,
                BackupPath = backupPath,
                Order = _actions.Count
            });
        }
    }

    /// <summary>
    /// Register a created directory for potential deletion.
    /// </summary>
    public void RegisterCreatedDirectory(string directoryPath)
    {
        lock (_lock)
        {
            if (_committed) return;

            _actions.Add(new RollbackAction
            {
                Type = RollbackActionType.DeleteCreatedDirectory,
                FilePath = directoryPath,
                Order = _actions.Count
            });
        }
    }

    /// <summary>
    /// Register a deleted file for potential restoration.
    /// </summary>
    public void RegisterDeletedFile(string filePath, string backupPath)
    {
        lock (_lock)
        {
            if (_committed) return;

            _actions.Add(new RollbackAction
            {
                Type = RollbackActionType.RestoreDeletedFile,
                FilePath = filePath,
                BackupPath = backupPath,
                Order = _actions.Count
            });
        }
    }

    /// <summary>
    /// Register a renamed file for potential restoration.
    /// </summary>
    public void RegisterRenamedFile(string originalPath, string newPath)
    {
        lock (_lock)
        {
            if (_committed) return;

            _actions.Add(new RollbackAction
            {
                Type = RollbackActionType.UndoRename,
                FilePath = newPath,
                OriginalPath = originalPath,
                Order = _actions.Count
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Rollback Execution
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Execute rollback of all registered actions.
    /// </summary>
    /// <param name="logger">Optional logger for rollback progress.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if all rollback actions succeeded.</returns>
    public async Task<bool> RollbackAsync(
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        List<RollbackAction> actionsToRollback;

        lock (_lock)
        {
            if (_committed)
            {
                logger?.LogWarning("Cannot rollback: already committed");
                return false;
            }

            // Copy and reverse for LIFO order
            actionsToRollback = _actions.OrderByDescending(a => a.Order).ToList();
        }

        logger?.LogInformation(
            "Starting rollback of {Count} actions",
            actionsToRollback.Count);

        var allSuccess = true;

        foreach (var action in actionsToRollback)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var success = await ExecuteRollbackActionAsync(action, logger, ct);
                if (!success)
                {
                    allSuccess = false;
                    logger?.LogWarning(
                        "Rollback action failed: {Type} for {Path}",
                        action.Type, action.FilePath);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                allSuccess = false;
                logger?.LogError(ex,
                    "Exception during rollback action: {Type} for {Path}",
                    action.Type, action.FilePath);
            }
        }

        logger?.LogInformation("Rollback complete. Success: {Success}", allSuccess);
        return allSuccess;
    }

    private async Task<bool> ExecuteRollbackActionAsync(
        RollbackAction action,
        ILogger? logger,
        CancellationToken ct)
    {
        logger?.LogDebug(
            "Executing rollback: {Type} for {Path}",
            action.Type, action.FilePath);

        switch (action.Type)
        {
            case RollbackActionType.DeleteCreatedFile:
                if (await _fileSystem.FileExistsAsync(action.FilePath))
                {
                    await _fileSystem.DeleteFileAsync(action.FilePath, ct);
                    logger?.LogDebug("Deleted created file: {Path}", action.FilePath);
                }
                return true;

            case RollbackActionType.RestoreModifiedFile:
                if (!string.IsNullOrEmpty(action.BackupPath))
                {
                    await _backupService.RestoreBackupAsync(
                        action.BackupPath, action.FilePath, ct);
                    logger?.LogDebug(
                        "Restored modified file: {Path} from {Backup}",
                        action.FilePath, action.BackupPath);
                }
                return true;

            case RollbackActionType.DeleteCreatedDirectory:
                var dirPath = action.FilePath;
                if (Directory.Exists(dirPath) && IsDirectoryEmpty(dirPath))
                {
                    Directory.Delete(dirPath);
                    logger?.LogDebug("Deleted created directory: {Path}", dirPath);
                }
                return true;

            case RollbackActionType.RestoreDeletedFile:
                if (!string.IsNullOrEmpty(action.BackupPath))
                {
                    await _backupService.RestoreBackupAsync(
                        action.BackupPath, action.FilePath, ct);
                    logger?.LogDebug(
                        "Restored deleted file: {Path} from {Backup}",
                        action.FilePath, action.BackupPath);
                }
                return true;

            case RollbackActionType.UndoRename:
                if (!string.IsNullOrEmpty(action.OriginalPath) &&
                    await _fileSystem.FileExistsAsync(action.FilePath) &&
                    !await _fileSystem.FileExistsAsync(action.OriginalPath))
                {
                    File.Move(action.FilePath, action.OriginalPath);
                    logger?.LogDebug(
                        "Undid rename: {NewPath} → {OriginalPath}",
                        action.FilePath, action.OriginalPath);
                }
                return true;

            default:
                logger?.LogWarning("Unknown rollback action type: {Type}", action.Type);
                return false;
        }
    }

    private static bool IsDirectoryEmpty(string path)
    {
        return !Directory.EnumerateFileSystemEntries(path).Any();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commit and Clear
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Commit the changes (prevent future rollback).
    /// </summary>
    public void Commit()
    {
        lock (_lock)
        {
            _committed = true;
            _actions.Clear();
        }
    }

    /// <summary>
    /// Clear all tracked actions without executing rollback.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _actions.Clear();
        }
    }

    /// <summary>
    /// Number of registered rollback actions.
    /// </summary>
    public int ActionCount
    {
        get
        {
            lock (_lock) { return _actions.Count; }
        }
    }

    /// <summary>
    /// Whether the manager has been committed.
    /// </summary>
    public bool IsCommitted
    {
        get
        {
            lock (_lock) { return _committed; }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _actions.Clear();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Types
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A rollback action to execute.
    /// </summary>
    private sealed class RollbackAction
    {
        public RollbackActionType Type { get; init; }
        public string FilePath { get; init; } = string.Empty;
        public string? BackupPath { get; init; }
        public string? OriginalPath { get; init; }
        public int Order { get; init; }
    }

    /// <summary>
    /// Type of rollback action.
    /// </summary>
    private enum RollbackActionType
    {
        DeleteCreatedFile,
        RestoreModifiedFile,
        DeleteCreatedDirectory,
        RestoreDeletedFile,
        UndoRename
    }
}
