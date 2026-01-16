using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE CHANGE SERVICE (v0.4.3b)                                            │
// │ Service for applying code changes to the filesystem with undo support.   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for applying code changes to the filesystem with undo support.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3b.</para>
/// <para>
/// Provides thread-safe file modification operations with:
/// conflict detection, backup creation, change history tracking,
/// time-windowed undo support, and event notifications.
/// </para>
/// </remarks>
public sealed class FileChangeService : IFileChangeService, IDisposable
{
    private readonly IFileSystemService _fileSystem;
    private readonly IDiffService _diffService;
    private readonly IBackupService _backupService;
    private readonly ILogger<FileChangeService>? _logger;
    private readonly ApplyOptions _defaultOptions;

    // Thread-safe change history storage: file path -> stack of changes
    private readonly ConcurrentDictionary<string, Stack<FileChangeRecord>> _changeHistory = new();

    // Lock for history stack mutations (Push/Pop operations)
    private readonly object _historyLock = new();

    // Semaphore to ensure only one apply operation at a time
    private readonly SemaphoreSlim _applyLock = new(1, 1);

    // Maximum history records per file
    private const int MaxHistoryPerFile = 50;

    private bool _disposed;

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <inheritdoc />
    public event EventHandler<FileChangeFailedEventArgs>? ChangeFailed;

    /// <inheritdoc />
    public event EventHandler<FileChangeUndoneEventArgs>? ChangeUndone;

    /// <inheritdoc />
    public event EventHandler<FileConflictDetectedEventArgs>? ConflictDetected;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the FileChangeService.
    /// </summary>
    public FileChangeService(
        IFileSystemService fileSystem,
        IDiffService diffService,
        IBackupService backupService,
        ILogger<FileChangeService>? logger = null,
        ApplyOptions? defaultOptions = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger;
        _defaultOptions = defaultOptions ?? ApplyOptions.Default;

        _logger?.LogDebug("FileChangeService initialized with default options: CreateBackup={CreateBackup}, UndoWindow={UndoWindow}",
            _defaultOptions.CreateBackup, _defaultOptions.UndoWindow);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Apply Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ApplyResult> ApplyCodeBlockAsync(
        CodeBlock block,
        string workspacePath,
        ApplyOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= _defaultOptions;

        // Validate inputs
        if (block == null)
        {
            _logger?.LogWarning("ApplyCodeBlockAsync called with null block");
            return ApplyResult.Failed(string.Empty, ApplyResultType.ValidationFailed, "Code block cannot be null");
        }

        if (string.IsNullOrEmpty(block.TargetFilePath))
        {
            _logger?.LogWarning("ApplyCodeBlockAsync called with block without target path: {BlockId}", block.Id);
            return ApplyResult.Failed(string.Empty, ApplyResultType.ValidationFailed, "Code block does not have a target file path");
        }

        if (string.IsNullOrEmpty(workspacePath))
        {
            _logger?.LogWarning("ApplyCodeBlockAsync called with empty workspace path");
            return ApplyResult.Failed(block.TargetFilePath, ApplyResultType.ValidationFailed, "Workspace path cannot be empty");
        }

        var fullPath = Path.Combine(workspacePath, block.TargetFilePath);
        var relativePath = block.TargetFilePath;

        _logger?.LogDebug("Applying code block {BlockId} to {FilePath}", block.Id, relativePath);

        // Acquire apply lock
        await _applyLock.WaitAsync(ct);
        try
        {
            return await ApplyInternalAsync(block, fullPath, relativePath, workspacePath, options, ct);
        }
        finally
        {
            _applyLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplyResult>> ApplyCodeBlocksAsync(
        IEnumerable<CodeBlock> blocks,
        string workspacePath,
        ApplyOptions? options = null,
        CancellationToken ct = default)
    {
        var results = new List<ApplyResult>();
        var blockList = blocks?.ToList() ?? new List<CodeBlock>();

        if (blockList.Count == 0)
        {
            _logger?.LogDebug("ApplyCodeBlocksAsync called with empty block list");
            return results;
        }

        _logger?.LogInformation("Applying {Count} code blocks to workspace {Workspace}", blockList.Count, workspacePath);

        // Group by target file
        var groupedByFile = blockList
            .Where(b => !string.IsNullOrEmpty(b.TargetFilePath))
            .GroupBy(b => b.TargetFilePath!)
            .ToList();

        foreach (var group in groupedByFile)
        {
            ct.ThrowIfCancellationRequested();

            var blocksForFile = group.ToList();

            if (blocksForFile.Count == 1)
            {
                var result = await ApplyCodeBlockAsync(blocksForFile[0], workspacePath, options, ct);
                results.Add(result);
            }
            else
            {
                _logger?.LogDebug("Merging {Count} blocks for file {FilePath}", blocksForFile.Count, group.Key);
                var mergedDiff = await _diffService.ComputeMergedDiffAsync(blocksForFile, workspacePath, ct);
                var result = await ApplyDiffAsync(mergedDiff, workspacePath, options, ct);
                results.Add(result);
            }
        }

        var successCount = results.Count(r => r.Success);
        _logger?.LogInformation("Applied {Success}/{Total} code blocks successfully", successCount, results.Count);

        return results;
    }

    /// <inheritdoc />
    public async Task<ApplyResult> ApplyDiffAsync(
        DiffResult diff,
        string workspacePath,
        ApplyOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= _defaultOptions;

        if (diff == null)
        {
            _logger?.LogWarning("ApplyDiffAsync called with null diff");
            return ApplyResult.Failed(string.Empty, ApplyResultType.ValidationFailed, "Diff cannot be null");
        }

        var fullPath = Path.Combine(workspacePath, diff.OriginalFilePath);
        var relativePath = diff.OriginalFilePath;

        _logger?.LogDebug("Applying diff to {FilePath}", relativePath);

        await _applyLock.WaitAsync(ct);
        try
        {
            return await ApplyDiffInternalAsync(diff, fullPath, relativePath, options, ct);
        }
        finally
        {
            _applyLock.Release();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Preview Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ApplyPreview> PreviewApplyAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken ct = default)
    {
        if (block == null || string.IsNullOrEmpty(block.TargetFilePath))
        {
            return new ApplyPreview
            {
                CanWrite = false,
                WriteBlockedReason = "Invalid code block"
            };
        }

        var fullPath = Path.Combine(workspacePath, block.TargetFilePath);
        var relativePath = block.TargetFilePath;

        _logger?.LogDebug("Generating preview for {FilePath}", relativePath);

        try
        {
            var targetExists = await _fileSystem.FileExistsAsync(fullPath);
            var diff = await _diffService.ComputeDiffForBlockAsync(block, workspacePath, ct);
            var conflictCheck = await CheckForConflictsAsync(block, workspacePath, ct);

            var canWrite = true;
            string? writeBlockedReason = null;

            // Check write permissions
            if (targetExists)
            {
                try
                {
                    var fileInfo = new FileInfo(fullPath);
                    if (fileInfo.IsReadOnly)
                    {
                        canWrite = false;
                        writeBlockedReason = "File is read-only";
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error checking file permissions for {FilePath}", fullPath);
                    canWrite = false;
                    writeBlockedReason = ex.Message;
                }
            }

            // Detect line endings
            var lineEndings = LineEndingStyle.Unknown;
            if (targetExists)
            {
                var content = await _fileSystem.ReadFileAsync(fullPath, ct);
                lineEndings = LineEndingStyleExtensions.DetectLineEndings(content);
            }

            return new ApplyPreview
            {
                Diff = diff,
                FilePath = fullPath,
                RelativePath = relativePath,
                TargetExists = targetExists,
                HasConflict = conflictCheck.HasConflict,
                ConflictResult = conflictCheck,
                CanWrite = canWrite,
                WriteBlockedReason = writeBlockedReason,
                DetectedLineEndings = lineEndings,
                CurrentSizeBytes = targetExists ? new FileInfo(fullPath).Length : null,
                EstimatedNewSizeBytes = Encoding.UTF8.GetByteCount(diff.ProposedContent ?? string.Empty)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error generating preview for {FilePath}", fullPath);
            return new ApplyPreview
            {
                FilePath = fullPath,
                RelativePath = relativePath,
                CanWrite = false,
                WriteBlockedReason = ex.Message
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Undo Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> UndoLastChangeAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger?.LogWarning("UndoLastChangeAsync called with empty file path");
            return false;
        }

        var normalizedPath = Path.GetFullPath(filePath);

        if (!_changeHistory.TryGetValue(normalizedPath, out var stack) || stack.Count == 0)
        {
            _logger?.LogWarning("No change history for {FilePath}", normalizedPath);
            return false;
        }

        FileChangeRecord? record;
        lock (_historyLock)
        {
            if (stack.Count == 0)
                return false;
            record = stack.Peek();
        }

        if (!record.CanUndo(_defaultOptions.UndoWindow))
        {
            _logger?.LogWarning("Undo expired or unavailable for {FilePath}", normalizedPath);
            return false;
        }

        return await PerformUndoAsync(record, ct);
    }

    /// <inheritdoc />
    public async Task<bool> UndoChangeAsync(Guid changeId, CancellationToken ct = default)
    {
        _logger?.LogDebug("Searching for change {ChangeId} to undo", changeId);

        foreach (var kvp in _changeHistory)
        {
            lock (_historyLock)
            {
                var record = kvp.Value.FirstOrDefault(r => r.Id == changeId);
                if (record != null)
                {
                    if (!record.CanUndo(_defaultOptions.UndoWindow))
                    {
                        _logger?.LogWarning("Change {ChangeId} cannot be undone", changeId);
                        return false;
                    }

                    return PerformUndoAsync(record, ct).GetAwaiter().GetResult();
                }
            }
        }

        _logger?.LogWarning("Change {ChangeId} not found in history", changeId);
        return false;
    }

    /// <inheritdoc />
    public bool CanUndo(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var normalizedPath = Path.GetFullPath(filePath);

        if (!_changeHistory.TryGetValue(normalizedPath, out var stack))
            return false;

        lock (_historyLock)
        {
            if (stack.Count == 0)
                return false;

            var record = stack.Peek();
            return record.CanUndo(_defaultOptions.UndoWindow);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // History Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public IReadOnlyList<FileChangeRecord> GetChangeHistory(string filePath, int maxRecords = 10)
    {
        if (string.IsNullOrEmpty(filePath))
            return Array.Empty<FileChangeRecord>();

        var normalizedPath = Path.GetFullPath(filePath);

        if (!_changeHistory.TryGetValue(normalizedPath, out var stack))
            return Array.Empty<FileChangeRecord>();

        lock (_historyLock)
        {
            return stack.Take(maxRecords).ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<FileChangeRecord> GetPendingUndos()
    {
        var pending = new List<FileChangeRecord>();

        foreach (var kvp in _changeHistory)
        {
            lock (_historyLock)
            {
                foreach (var record in kvp.Value)
                {
                    if (record.CanUndo(_defaultOptions.UndoWindow))
                    {
                        pending.Add(record);
                    }
                }
            }
        }

        return pending.OrderByDescending(r => r.ChangedAt).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Conflict Detection
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ConflictCheckResult> CheckForConflictsAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken ct = default)
    {
        if (block == null || string.IsNullOrEmpty(block.TargetFilePath))
        {
            return ConflictCheckResult.NoConflict();
        }

        var fullPath = Path.Combine(workspacePath, block.TargetFilePath);

        if (!await _fileSystem.FileExistsAsync(fullPath))
        {
            return ConflictCheckResult.NoConflict();
        }

        // Check if file was modified since last recorded change
        if (_changeHistory.TryGetValue(fullPath, out var stack))
        {
            lock (_historyLock)
            {
                if (stack.Count > 0)
                {
                    var lastChange = stack.Peek();
                    var currentContent = _fileSystem.ReadFileAsync(fullPath, ct).GetAwaiter().GetResult();
                    var currentHash = ComputeHash(currentContent);

                    if (!string.IsNullOrEmpty(lastChange.NewContentHash) &&
                        currentHash != lastChange.NewContentHash)
                    {
                        _logger?.LogDebug("Conflict detected for {FilePath}: expected {Expected}, got {Actual}",
                            fullPath, lastChange.NewContentHash[..8], currentHash[..8]);

                        return ConflictCheckResult.Detected(
                            lastChange.NewContentHash,
                            currentHash,
                            "File was modified since last apply");
                    }
                }
            }
        }

        return ConflictCheckResult.NoConflict();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Internal Apply Methods
    // ═══════════════════════════════════════════════════════════════════════

    private async Task<ApplyResult> ApplyInternalAsync(
        CodeBlock block,
        string fullPath,
        string relativePath,
        string workspacePath,
        ApplyOptions options,
        CancellationToken ct)
    {
        var fileExists = await _fileSystem.FileExistsAsync(fullPath);

        try
        {
            // Check for conflicts
            if (fileExists && options.CheckForConflicts && !options.AllowConflictOverwrite)
            {
                var conflictCheck = await CheckForConflictsAsync(block, workspacePath, ct);
                if (conflictCheck.HasConflict)
                {
                    _logger?.LogWarning("Conflict detected for {FilePath}: {Description}",
                        relativePath, conflictCheck.Description);

                    ConflictDetected?.Invoke(this, new FileConflictDetectedEventArgs(fullPath, conflictCheck));
                    return ApplyResult.Conflict(fullPath, relativePath,
                        conflictCheck.ExpectedHash ?? "", conflictCheck.ActualHash ?? "",
                        conflictCheck.Description ?? "File modified externally");
                }
            }

            // Create backup
            string? backupPath = null;
            string? originalHash = null;
            if (fileExists && options.CreateBackup)
            {
                var originalContent = await _fileSystem.ReadFileAsync(fullPath, ct);
                originalHash = ComputeHash(originalContent);
                backupPath = await _backupService.CreateBackupAsync(fullPath, ct);
                _logger?.LogDebug("Created backup at {BackupPath}", backupPath);
            }

            // Compute diff
            var diff = await _diffService.ComputeDiffForBlockAsync(block, workspacePath, ct);

            // Create parent directories
            if (options.CreateParentDirectories)
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger?.LogDebug("Created directory {Directory}", directory);
                }
            }

            // Prepare content
            var contentToWrite = diff.ProposedContent ?? string.Empty;

            // Preserve line endings
            if (options.PreserveLineEndings && fileExists)
            {
                var originalContent = await _fileSystem.ReadFileAsync(fullPath, ct);
                contentToWrite = NormalizeLineEndings(contentToWrite, originalContent);
            }

            // Write file
            await _fileSystem.WriteFileAsync(fullPath, contentToWrite, ct);

            var newHash = ComputeHash(contentToWrite);
            var changeType = fileExists ? FileChangeType.Modified : FileChangeType.Created;

            // Record change
            var record = new FileChangeRecord
            {
                FilePath = fullPath,
                RelativePath = relativePath,
                BackupPath = backupPath,
                ChangeType = changeType,
                CodeBlockId = block.Id,
                MessageId = block.MessageId,
                OriginalContentHash = originalHash,
                NewContentHash = newHash,
                LinesAdded = diff.Stats?.AddedLines ?? 0,
                LinesRemoved = diff.Stats?.RemovedLines ?? 0,
                LinesModified = diff.Stats?.ModifiedLines ?? 0,
                Description = $"Applied code block to {Path.GetFileName(fullPath)}"
            };

            RecordChange(fullPath, record);

            // Raise event
            var result = ApplyResult.Modified(fullPath, relativePath, diff, backupPath, block.Id);
            FileChanged?.Invoke(this, new FileChangedEventArgs(result) { ChangeRecord = record, CodeBlockId = block.Id });

            _logger?.LogInformation("Applied code block {BlockId} to {FilePath} ({ChangeType})",
                block.Id, relativePath, changeType);

            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogWarning(ex, "Permission denied for {FilePath}", fullPath);
            RaiseChangeFailed(fullPath, "Permission denied", ApplyResultType.PermissionDenied, ex);
            return ApplyResult.Failed(fullPath, ApplyResultType.PermissionDenied, ex.Message, ex);
        }
        catch (IOException ex) when (IsFileLocked(ex))
        {
            _logger?.LogWarning(ex, "File locked: {FilePath}", fullPath);
            RaiseChangeFailed(fullPath, "File is locked", ApplyResultType.FileLocked, ex);
            return ApplyResult.Failed(fullPath, ApplyResultType.FileLocked, ex.Message, ex);
        }
        catch (IOException ex) when (IsDiskFull(ex))
        {
            _logger?.LogError(ex, "Disk full: {FilePath}", fullPath);
            RaiseChangeFailed(fullPath, "Disk is full", ApplyResultType.DiskFull, ex);
            return ApplyResult.Failed(fullPath, ApplyResultType.DiskFull, ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying to {FilePath}", fullPath);
            RaiseChangeFailed(fullPath, ex.Message, ApplyResultType.Error, ex);
            return ApplyResult.Failed(fullPath, ApplyResultType.Error, ex.Message, ex);
        }
    }

    private async Task<ApplyResult> ApplyDiffInternalAsync(
        DiffResult diff,
        string fullPath,
        string relativePath,
        ApplyOptions options,
        CancellationToken ct)
    {
        var fileExists = File.Exists(fullPath);

        try
        {
            // Create backup
            string? backupPath = null;
            string? originalHash = null;
            if (fileExists && options.CreateBackup)
            {
                var originalContent = await _fileSystem.ReadFileAsync(fullPath, ct);
                originalHash = ComputeHash(originalContent);
                backupPath = await _backupService.CreateBackupAsync(fullPath, ct);
                _logger?.LogDebug("Created backup at {BackupPath}", backupPath);
            }

            // Create parent directories
            if (options.CreateParentDirectories)
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            // Write file
            var contentToWrite = diff.ProposedContent ?? string.Empty;
            await _fileSystem.WriteFileAsync(fullPath, contentToWrite, ct);

            var newHash = ComputeHash(contentToWrite);
            var changeType = fileExists ? FileChangeType.Modified : FileChangeType.Created;

            // Record change
            var record = new FileChangeRecord
            {
                FilePath = fullPath,
                RelativePath = relativePath,
                BackupPath = backupPath,
                ChangeType = changeType,
                OriginalContentHash = originalHash,
                NewContentHash = newHash,
                LinesAdded = diff.Stats?.AddedLines ?? 0,
                LinesRemoved = diff.Stats?.RemovedLines ?? 0,
                LinesModified = diff.Stats?.ModifiedLines ?? 0,
                Description = $"Applied diff to {Path.GetFileName(fullPath)}"
            };

            RecordChange(fullPath, record);

            var result = changeType == FileChangeType.Created
                ? ApplyResult.Created(fullPath, relativePath, backupPath)
                : ApplyResult.Modified(fullPath, relativePath, diff, backupPath);

            FileChanged?.Invoke(this, new FileChangedEventArgs(result) { ChangeRecord = record });

            _logger?.LogInformation("Applied diff to {FilePath} ({ChangeType})", relativePath, changeType);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying diff to {FilePath}", fullPath);
            RaiseChangeFailed(fullPath, ex.Message, ApplyResultType.Error, ex);
            return ApplyResult.Failed(fullPath, ApplyResultType.Error, ex.Message, ex);
        }
    }

    private async Task<bool> PerformUndoAsync(FileChangeRecord record, CancellationToken ct)
    {
        _logger?.LogInformation("Undoing change {ChangeId} for {FilePath}", record.Id, record.FilePath);

        try
        {
            await _applyLock.WaitAsync(ct);

            try
            {
                if (record.ChangeType == FileChangeType.Created)
                {
                    // Undo create = delete file
                    if (File.Exists(record.FilePath))
                    {
                        File.Delete(record.FilePath);
                        _logger?.LogDebug("Deleted created file {FilePath}", record.FilePath);
                    }
                }
                else if (!string.IsNullOrEmpty(record.BackupPath))
                {
                    // Restore from backup
                    var success = await _backupService.RestoreBackupAsync(record.BackupPath, record.FilePath, ct);
                    if (!success)
                    {
                        _logger?.LogWarning("Failed to restore backup for {FilePath}", record.FilePath);
                        return false;
                    }
                    _logger?.LogDebug("Restored {FilePath} from backup", record.FilePath);
                }
                else
                {
                    _logger?.LogWarning("No backup available for undo of {FilePath}", record.FilePath);
                    return false;
                }

                // Mark as undone
                record.IsUndone = true;
                record.UndoneAt = DateTime.UtcNow;

                ChangeUndone?.Invoke(this, new FileChangeUndoneEventArgs(record) { Success = true });
                return true;
            }
            finally
            {
                _applyLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error undoing change for {FilePath}", record.FilePath);
            ChangeUndone?.Invoke(this, new FileChangeUndoneEventArgs(record)
            {
                Success = false,
                ErrorMessage = ex.Message
            });
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Utility Methods
    // ═══════════════════════════════════════════════════════════════════════

    private void RecordChange(string filePath, FileChangeRecord record)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        var stack = _changeHistory.GetOrAdd(normalizedPath, _ => new Stack<FileChangeRecord>());

        lock (_historyLock)
        {
            stack.Push(record);

            // Prune if over limit
            while (stack.Count > MaxHistoryPerFile)
            {
                var removed = stack.ToList();
                stack.Clear();
                foreach (var item in removed.Take(MaxHistoryPerFile).Reverse())
                {
                    stack.Push(item);
                }
            }
        }
    }

    private void RaiseChangeFailed(string filePath, string message, ApplyResultType type, Exception? ex)
    {
        ChangeFailed?.Invoke(this, new FileChangeFailedEventArgs(filePath, type, message) { Exception = ex });
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeLineEndings(string content, string originalContent)
    {
        var style = LineEndingStyleExtensions.DetectLineEndings(originalContent);
        var targetEnding = style.ToLineEnding();

        // Normalize to LF first, then convert to target
        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
        if (targetEnding != "\n")
        {
            normalized = normalized.Replace("\n", targetEnding);
        }
        return normalized;
    }

    private static bool IsFileLocked(IOException ex)
    {
        var hResult = ex.HResult;
        return hResult == unchecked((int)0x80070020) || hResult == unchecked((int)0x80070021);
    }

    private static bool IsDiskFull(IOException ex)
    {
        return ex.HResult == unchecked((int)0x80070070);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose
    // ═══════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;

        _applyLock.Dispose();
        _disposed = true;
    }
}
