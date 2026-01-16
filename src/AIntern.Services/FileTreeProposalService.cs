using System.Text;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PROPOSAL SERVICE (v0.4.4c)                                     │
// │ Service for validating and applying multi-file proposals.               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for validating and applying multi-file proposals.
/// </summary>
/// <remarks>
/// <para>
/// Coordinates the entire apply workflow including validation,
/// backup creation, file operations, and rollback on failure.
/// </para>
/// <para>Added in v0.4.4c.</para>
/// </remarks>
public sealed class FileTreeProposalService : IFileTreeProposalService
{
    private readonly IFileSystemService _fileSystem;
    private readonly IFileChangeService _changeService;
    private readonly IDiffService _diffService;
    private readonly IBackupService _backupService;
    private readonly ProposalServiceOptions _options;
    private readonly ILogger<FileTreeProposalService>? _logger;

    public event EventHandler<BatchApplyProgress>? ProgressChanged;
    public event EventHandler<OperationCompletedEventArgs>? OperationCompleted;
    public event EventHandler<ProposalValidationResult>? ValidationCompleted;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTreeProposalService"/> class.
    /// </summary>
    public FileTreeProposalService(
        IFileSystemService fileSystem,
        IFileChangeService changeService,
        IDiffService diffService,
        IBackupService backupService,
        ProposalServiceOptions? options = null,
        ILogger<FileTreeProposalService>? logger = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _changeService = changeService ?? throw new ArgumentNullException(nameof(changeService));
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _options = options ?? ProposalServiceOptions.Default;
        _logger = logger;

        _logger?.LogDebug(
            "FileTreeProposalService initialized with Backups={Backups}, Rollback={Rollback}",
            _options.EnableBackups,
            _options.EnableRollback);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<ProposalValidationResult> ValidateProposalAsync(
        FileTreeProposal proposal,
        string workspacePath,
        CancellationToken ct = default)
    {
        _logger?.LogDebug(
            "Validating proposal {ProposalId} with {Count} operations",
            proposal.Id, proposal.Operations.Count);

        var issues = new List<ValidationIssue>();

        // Check for duplicate paths
        var duplicates = proposal.Operations
            .GroupBy(o => o.Path.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var dup in duplicates)
        {
            issues.Add(ValidationIssue.Error(
                dup,
                ValidationIssueType.DuplicatePath,
                $"Duplicate path: {dup}"));
        }

        // Validate each operation
        foreach (var operation in proposal.Operations)
        {
            ct.ThrowIfCancellationRequested();
            var opIssues = await ValidateOperationAsync(operation, workspacePath, ct);
            issues.AddRange(opIssues);
        }

        var result = issues.Any(i => i.Severity == ValidationSeverity.Error)
            ? ProposalValidationResult.Invalid(issues.ToArray())
            : new ProposalValidationResult { IsValid = true, Issues = issues };

        _logger?.LogInformation(
            "Validation complete: IsValid={IsValid}, ErrorCount={ErrorCount}, WarningCount={WarningCount}",
            new object[] { result.IsValid, result.ErrorCount, result.WarningCount });

        ValidationCompleted?.Invoke(this, result);
        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ValidationIssue>> ValidateOperationAsync(
        FileOperation operation,
        string workspacePath,
        CancellationToken ct = default)
    {
        var issues = new List<ValidationIssue>();
        var fullPath = Path.Combine(workspacePath, operation.Path);

        // Check path validity
        if (string.IsNullOrWhiteSpace(operation.Path))
        {
            issues.Add(ValidationIssue.Error(
                operation.Path,
                ValidationIssueType.InvalidPath,
                "Path is empty"));
            return issues;
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidPathChars();
        if (operation.Path.Any(c => invalidChars.Contains(c)))
        {
            issues.Add(ValidationIssue.Error(
                operation.Path,
                ValidationIssueType.InvalidCharacters,
                "Path contains invalid characters"));
        }

        // Check path length
        if (fullPath.Length > _options.PathLengthLimit)
        {
            issues.Add(ValidationIssue.Error(
                operation.Path,
                ValidationIssueType.PathTooLong,
                $"Path exceeds {_options.PathLengthLimit} characters"));
        }

        // Check workspace boundaries
        if (!IsWithinWorkspace(operation.Path, workspacePath))
        {
            issues.Add(ValidationIssue.Error(
                operation.Path,
                ValidationIssueType.OutsideWorkspace,
                "Path is outside workspace boundaries"));
        }

        // Check file existence based on operation type
        var exists = await _fileSystem.FileExistsAsync(fullPath);

        if (operation.Type == FileOperationType.Create && exists)
        {
            issues.Add(ValidationIssue.Warning(
                operation.Path,
                ValidationIssueType.FileExists,
                "File already exists and will be overwritten"));
        }

        if (operation.Type == FileOperationType.Modify && !exists)
        {
            issues.Add(ValidationIssue.Warning(
                operation.Path,
                ValidationIssueType.ParentNotExists,
                "File does not exist; will be created"));
        }

        if (operation.Type == FileOperationType.Delete && !exists)
        {
            issues.Add(ValidationIssue.Warning(
                operation.Path,
                ValidationIssueType.ParentNotExists,
                "File does not exist; delete will be skipped"));
        }

        // Check empty content
        if ((operation.Type == FileOperationType.Create ||
             operation.Type == FileOperationType.Modify) &&
            string.IsNullOrEmpty(operation.Content))
        {
            issues.Add(ValidationIssue.Warning(
                operation.Path,
                ValidationIssueType.EmptyContent,
                "File content is empty"));
        }

        // Check parent directory
        var parentDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parentDir) &&
            !Directory.Exists(parentDir) &&
            !_options.CreateParentDirectories)
        {
            issues.Add(ValidationIssue.Error(
                operation.Path,
                ValidationIssueType.ParentNotExists,
                "Parent directory does not exist"));
        }

        return issues;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Apply
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<BatchApplyResult> ApplyProposalAsync(
        FileTreeProposal proposal,
        string workspacePath,
        ApplyOptions? options = null,
        IProgress<BatchApplyProgress>? progress = null,
        CancellationToken ct = default)
    {
        options ??= ApplyOptions.Default;

        _logger?.LogInformation(
            "Applying proposal {ProposalId} with {Count} operations to {Workspace}",
            proposal.Id, proposal.Operations.Count, workspacePath);

        using var context = new ApplyContext(_fileSystem, _backupService)
        {
            Proposal = proposal,
            WorkspacePath = workspacePath,
            Options = options,
            Progress = progress,
            CancellationToken = ct,
            TotalOperations = proposal.SelectedOperations.Count()
        };

        try
        {
            // Phase 1: Validation
            context.CurrentPhase = BatchApplyPhase.Validating;
            context.ReportProgress(OnProgressChanged);

            if (_options.ValidateBeforeApply)
            {
                var validation = await ValidateProposalAsync(proposal, workspacePath, ct);
                if (!validation.IsValid && !_options.ContinueOnFailure)
                {
                    _logger?.LogWarning("Validation failed, aborting apply");
                    return BatchApplyResult.RolledBack(
                        string.Join("; ", validation.Errors.Select(e => e.Message)),
                        context.StartedAt);
                }
            }

            // Phase 2: Create directories
            context.CurrentPhase = BatchApplyPhase.CreatingDirectories;
            context.ReportProgress(OnProgressChanged);

            if (_options.CreateParentDirectories)
            {
                await CreateDirectoriesAsync(context, ct);
            }

            // Phase 3: Create backups
            if (_options.EnableBackups && options.CreateBackup)
            {
                context.CurrentPhase = BatchApplyPhase.CreatingBackups;
                context.ReportProgress(OnProgressChanged);
                await CreateBackupsAsync(context, ct);
            }

            // Phase 4: Write files
            context.CurrentPhase = BatchApplyPhase.WritingFiles;
            context.ReportProgress(OnProgressChanged);

            foreach (var operation in proposal.SelectedOperations.OrderBy(o => o.Order))
            {
                ct.ThrowIfCancellationRequested();

                context.CurrentFile = operation.Path;
                context.ReportProgress(OnProgressChanged);

                var result = await ApplyOperationInternalAsync(context, operation);
                context.Results.Add(result);
                context.CompletedOperations++;

                // Raise operation completed event
                OperationCompleted?.Invoke(this, new OperationCompletedEventArgs
                {
                    Operation = operation,
                    Result = result,
                    OperationIndex = context.CompletedOperations - 1,
                    TotalOperations = context.TotalOperations
                });

                // Handle failure
                if (!result.Success)
                {
                    _logger?.LogWarning(
                        "Operation failed for {Path}: {Error}",
                        operation.Path, result.ErrorMessage);

                    if (!_options.ContinueOnFailure)
                    {
                        break;
                    }
                }
            }

            // Phase 5: Finalize
            context.CurrentPhase = BatchApplyPhase.Finalizing;
            context.ReportProgress(OnProgressChanged);

            // Check if rollback is needed
            var failedCount = context.Results.Count(r => !r.Success);
            if (failedCount > 0 && _options.RollbackOnPartialFailure && _options.EnableRollback)
            {
                context.IsRollingBack = true;
                context.CurrentPhase = BatchApplyPhase.RollingBack;
                context.ReportProgress(OnProgressChanged);

                await context.RollbackManager.RollbackAsync(_logger, ct);
            }
            else
            {
                // Commit - no rollback will occur
                context.RollbackManager.Commit();
            }

            context.CurrentPhase = BatchApplyPhase.Completed;
            context.ReportProgress(OnProgressChanged);

            var batchResult = context.BuildResult();

            _logger?.LogInformation(
                "Apply complete: Success={Success}, Failed={Failed}, Duration={Duration}ms",
                batchResult.SuccessCount,
                batchResult.FailedCount,
                batchResult.Duration.TotalMilliseconds);

            return batchResult;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Apply cancelled, initiating rollback");
            context.IsCancelled = true;
            context.IsRollingBack = true;
            context.CurrentPhase = BatchApplyPhase.RollingBack;
            context.ReportProgress(OnProgressChanged);

            if (_options.EnableRollback)
            {
                await context.RollbackManager.RollbackAsync(_logger, CancellationToken.None);
            }

            return context.BuildResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during apply");

            if (_options.EnableRollback)
            {
                context.IsRollingBack = true;
                context.CurrentPhase = BatchApplyPhase.RollingBack;
                context.ReportProgress(OnProgressChanged);
                await context.RollbackManager.RollbackAsync(_logger, CancellationToken.None);
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ApplyResult> ApplyOperationAsync(
        FileOperation operation,
        string workspacePath,
        ApplyOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= ApplyOptions.Default;

        _logger?.LogDebug(
            "Applying single operation: {Type} {Path}",
            operation.Type, operation.Path);

        using var context = new ApplyContext(_fileSystem, _backupService)
        {
            WorkspacePath = workspacePath,
            Options = options,
            CancellationToken = ct,
            TotalOperations = 1
        };

        var result = await ApplyOperationInternalAsync(context, operation);

        if (result.Success)
        {
            context.RollbackManager.Commit();
        }

        return result;
    }

    private async Task<ApplyResult> ApplyOperationInternalAsync(
        ApplyContext context,
        FileOperation operation)
    {
        var fullPath = Path.Combine(context.WorkspacePath, operation.Path);
        var startTime = DateTime.UtcNow;

        try
        {
            switch (operation.Type)
            {
                case FileOperationType.Create:
                case FileOperationType.Modify:
                    return await WriteFileAsync(context, operation, fullPath);

                case FileOperationType.Delete:
                    return await DeleteFileAsync(context, operation, fullPath);

                case FileOperationType.Rename:
                    return await RenameFileAsync(context, operation, fullPath);

                case FileOperationType.Move:
                    return await MoveFileAsync(context, operation, fullPath);

                case FileOperationType.CreateDirectory:
                    return CreateDirectorySync(context, operation, fullPath);

                default:
                    return new ApplyResult
                    {
                        Success = false,
                        ErrorMessage = $"Unsupported operation type: {operation.Type}"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying operation {Path}", operation.Path);
            return new ApplyResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                AppliedAt = startTime
            };
        }
    }

    private async Task<ApplyResult> WriteFileAsync(
        ApplyContext context,
        FileOperation operation,
        string fullPath)
    {
        var exists = await _fileSystem.FileExistsAsync(fullPath);
        var startTime = DateTime.UtcNow;

        // Create backup if file exists and backups are enabled
        if (exists && _options.EnableBackups && context.Options.CreateBackup)
        {
            var backupPath = await _backupService.CreateBackupAsync(fullPath);
            context.BackupPaths.Add(backupPath);
            context.ModifiedFiles[fullPath] = backupPath;
            context.RollbackManager.RegisterModifiedFile(fullPath, backupPath);
        }

        // Write file
        await _fileSystem.WriteFileAsync(fullPath, operation.Content ?? "");

        var contentSize = operation.Content?.Length ?? 0;
        _logger?.LogDebug("Wrote file: {Path} ({Size} bytes)",
            fullPath, contentSize);

        // Register for rollback
        if (!exists)
        {
            context.CreatedFiles.Add(fullPath);
            context.RollbackManager.RegisterCreatedFile(fullPath);
        }

        return new ApplyResult
        {
            Success = true,
            FilePath = fullPath,
            RelativePath = operation.Path,
            AppliedAt = startTime,
            BackupPath = context.ModifiedFiles.GetValueOrDefault(fullPath)
        };
    }

    private async Task<ApplyResult> DeleteFileAsync(
        ApplyContext context,
        FileOperation operation,
        string fullPath)
    {
        var exists = await _fileSystem.FileExistsAsync(fullPath);
        var startTime = DateTime.UtcNow;

        if (!exists)
        {
            return new ApplyResult
            {
                Success = true,
                FilePath = fullPath,
                RelativePath = operation.Path,
                AppliedAt = startTime
            };
        }

        // Create backup before deletion
        if (_options.EnableBackups && context.Options.CreateBackup)
        {
            var backupPath = await _backupService.CreateBackupAsync(fullPath);
            context.BackupPaths.Add(backupPath);
            context.RollbackManager.RegisterDeletedFile(fullPath, backupPath);
        }

        await _fileSystem.DeleteFileAsync(fullPath);

        _logger?.LogDebug("Deleted file: {Path}", fullPath);

        return new ApplyResult
        {
            Success = true,
            FilePath = fullPath,
            RelativePath = operation.Path,
            AppliedAt = startTime
        };
    }

    private async Task<ApplyResult> RenameFileAsync(
        ApplyContext context,
        FileOperation operation,
        string fullPath)
    {
        var startTime = DateTime.UtcNow;

        // For rename, NewPath contains the new path
        if (string.IsNullOrEmpty(operation.NewPath))
        {
            return new ApplyResult
            {
                Success = false,
                ErrorMessage = "New path not specified for rename"
            };
        }

        var originalFullPath = fullPath;
        var newFullPath = Path.Combine(context.WorkspacePath, operation.NewPath);

        if (!await _fileSystem.FileExistsAsync(originalFullPath))
        {
            return new ApplyResult
            {
                Success = false,
                ErrorMessage = $"Original file does not exist: {operation.Path}"
            };
        }

        File.Move(originalFullPath, newFullPath);
        context.RollbackManager.RegisterRenamedFile(originalFullPath, newFullPath);

        _logger?.LogDebug("Renamed file: {Original} → {New}",
            originalFullPath, newFullPath);

        return new ApplyResult
        {
            Success = true,
            FilePath = newFullPath,
            RelativePath = operation.NewPath,
            AppliedAt = startTime
        };
    }

    private async Task<ApplyResult> MoveFileAsync(
        ApplyContext context,
        FileOperation operation,
        string fullPath)
    {
        // Move is essentially the same as rename but across directories
        return await RenameFileAsync(context, operation, fullPath);
    }

    private ApplyResult CreateDirectorySync(
        ApplyContext context,
        FileOperation operation,
        string fullPath)
    {
        var startTime = DateTime.UtcNow;

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            context.CreatedDirectories.Add(fullPath);
            context.RollbackManager.RegisterCreatedDirectory(fullPath);

            _logger?.LogDebug("Created directory: {Path}", fullPath);
        }

        return new ApplyResult
        {
            Success = true,
            FilePath = fullPath,
            RelativePath = operation.Path,
            AppliedAt = startTime
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Preview
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DiffResult>> PreviewProposalAsync(
        FileTreeProposal proposal,
        string workspacePath,
        CancellationToken ct = default)
    {
        _logger?.LogDebug(
            "Previewing proposal {ProposalId} with {Count} operations",
            proposal.Id, proposal.Operations.Count);

        var results = new List<DiffResult>();

        foreach (var operation in proposal.SelectedOperations)
        {
            ct.ThrowIfCancellationRequested();
            var diff = await PreviewOperationAsync(operation, workspacePath, ct);
            if (diff != null)
            {
                results.Add(diff);
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<DiffResult?> PreviewOperationAsync(
        FileOperation operation,
        string workspacePath,
        CancellationToken ct = default)
    {
        var fullPath = Path.Combine(workspacePath, operation.Path);

        if (operation.Type == FileOperationType.Delete)
        {
            // For delete, show file content as removal
            if (await _fileSystem.FileExistsAsync(fullPath))
            {
                var existingContent = await _fileSystem.ReadFileAsync(fullPath);
                return _diffService.ComputeDeleteFileDiff(existingContent, operation.Path);
            }
            return null;
        }

        if (operation.Type == FileOperationType.Create ||
            operation.Type == FileOperationType.Modify)
        {
            if (await _fileSystem.FileExistsAsync(fullPath))
            {
                var existingContent = await _fileSystem.ReadFileAsync(fullPath);
                return _diffService.ComputeDiff(
                    existingContent,
                    operation.Content ?? "",
                    operation.Path);
            }
            else
            {
                return _diffService.ComputeNewFileDiff(
                    operation.Content ?? "",
                    operation.Path);
            }
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Undo
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<bool> UndoBatchApplyAsync(
        BatchApplyResult result,
        CancellationToken ct = default)
    {
        _logger?.LogInformation(
            "Undoing batch apply with {Count} results",
            result.Results.Count);

        var allSuccess = true;

        // Undo in reverse order
        foreach (var applyResult in result.Results.OrderByDescending(r => r.AppliedAt))
        {
            ct.ThrowIfCancellationRequested();
            var success = await UndoApplyResultAsync(applyResult, ct);
            if (!success)
            {
                allSuccess = false;
            }
        }

        return allSuccess;
    }

    /// <inheritdoc/>
    public async Task<bool> UndoApplyResultAsync(
        ApplyResult result,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(result.FilePath))
        {
            return true;
        }

        try
        {
            // If there's a backup, restore it
            if (!string.IsNullOrEmpty(result.BackupPath))
            {
                await _backupService.RestoreBackupAsync(
                    result.BackupPath,
                    result.FilePath);

                _logger?.LogDebug(
                    "Restored {Path} from backup",
                    result.FilePath);
                return true;
            }

            // If it was a new file, delete it
            if (await _fileSystem.FileExistsAsync(result.FilePath))
            {
                await _fileSystem.DeleteFileAsync(result.FilePath);
                _logger?.LogDebug("Deleted created file: {Path}", result.FilePath);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to undo: {Path}", result.FilePath);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Utility
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, bool>> CheckExistingFilesAsync(
        FileTreeProposal proposal,
        string workspacePath,
        CancellationToken ct = default)
    {
        var results = new Dictionary<string, bool>();

        foreach (var operation in proposal.Operations)
        {
            ct.ThrowIfCancellationRequested();
            var fullPath = Path.Combine(workspacePath, operation.Path);
            results[operation.Path] = await _fileSystem.FileExistsAsync(fullPath);
        }

        return results;
    }

    /// <inheritdoc/>
    public bool IsWithinWorkspace(string path, string workspacePath)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(workspacePath))
            return false;

        // Normalize paths
        var fullPath = Path.GetFullPath(Path.Combine(workspacePath, path));
        var normalizedWorkspace = Path.GetFullPath(workspacePath);

        // Ensure workspace path ends with separator for accurate comparison
        if (!normalizedWorkspace.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            normalizedWorkspace += Path.DirectorySeparatorChar;
        }

        return fullPath.StartsWith(normalizedWorkspace, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public TimeSpan EstimateApplyTime(FileTreeProposal proposal)
    {
        // Rough estimate: 10ms per small file, 50ms per large file
        var smallFileTime = TimeSpan.FromMilliseconds(10);
        var largeFileTime = TimeSpan.FromMilliseconds(50);
        const int largeFileThreshold = 10000; // 10KB

        var totalMs = proposal.Operations.Sum(op =>
        {
            var contentSize = op.Content?.Length ?? 0;
            return contentSize > largeFileThreshold
                ? largeFileTime.TotalMilliseconds
                : smallFileTime.TotalMilliseconds;
        });

        // Add overhead for validation and backups
        totalMs *= _options.EnableBackups ? 1.5 : 1.0;
        totalMs *= _options.ValidateBeforeApply ? 1.2 : 1.0;

        return TimeSpan.FromMilliseconds(totalMs);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private async Task CreateDirectoriesAsync(ApplyContext context, CancellationToken ct)
    {
        var directories = context.Proposal.Operations
            .Where(o => o.Type == FileOperationType.Create ||
                       o.Type == FileOperationType.Modify)
            .Select(o => Path.GetDirectoryName(
                Path.Combine(context.WorkspacePath, o.Path)))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d!.Length) // Shortest paths first (parents before children)
            .ToList();

        foreach (var dir in directories)
        {
            ct.ThrowIfCancellationRequested();

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
                context.CreatedDirectories.Add(dir!);
                context.RollbackManager.RegisterCreatedDirectory(dir!);

                _logger?.LogDebug("Created directory: {Path}", dir);
            }
        }
    }

    private async Task CreateBackupsAsync(ApplyContext context, CancellationToken ct)
    {
        var filesToBackup = context.Proposal.SelectedOperations
            .Where(o => o.Type == FileOperationType.Modify ||
                       o.Type == FileOperationType.Delete)
            .Select(o => Path.Combine(context.WorkspacePath, o.Path))
            .ToList();

        foreach (var file in filesToBackup)
        {
            ct.ThrowIfCancellationRequested();

            if (await _fileSystem.FileExistsAsync(file))
            {
                var backupPath = await _backupService.CreateBackupAsync(file);
                context.BackupPaths.Add(backupPath);
                context.ModifiedFiles[file] = backupPath;

                _logger?.LogDebug("Created backup: {File} → {Backup}", file, backupPath);
            }
        }
    }

    private Encoding GetEncoding()
    {
        return _options.FileEncoding.ToLowerInvariant() switch
        {
            "utf-8" => _options.UseUtf8Bom ? Encoding.UTF8 : new UTF8Encoding(false),
            "utf-16" => Encoding.Unicode,
            "ascii" => Encoding.ASCII,
            _ => new UTF8Encoding(false)
        };
    }

    private void OnProgressChanged(BatchApplyProgress progress)
    {
        ProgressChanged?.Invoke(this, progress);
    }
}
