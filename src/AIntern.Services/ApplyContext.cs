using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY CONTEXT (v0.4.4c)                                                  │
// │ Context for tracking state during a batch apply operation.              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Context for tracking state during a batch apply operation.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4c.</para>
/// </remarks>
internal sealed class ApplyContext : IDisposable
{
    /// <summary>
    /// When the apply operation started.
    /// </summary>
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Results of individual operations.
    /// </summary>
    public List<ApplyResult> Results { get; } = new();

    /// <summary>
    /// Paths to backup files created.
    /// </summary>
    public List<string> BackupPaths { get; } = new();

    /// <summary>
    /// Directories created during this operation.
    /// </summary>
    public HashSet<string> CreatedDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Files created during this operation.
    /// </summary>
    public HashSet<string> CreatedFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Files modified during this operation (with backup paths).
    /// </summary>
    public Dictionary<string, string> ModifiedFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The cancellation token for this operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// The rollback manager for this operation.
    /// </summary>
    public RollbackManager RollbackManager { get; }

    /// <summary>
    /// The proposal being applied.
    /// </summary>
    public FileTreeProposal Proposal { get; init; } = null!;

    /// <summary>
    /// The workspace path.
    /// </summary>
    public string WorkspacePath { get; init; } = string.Empty;

    /// <summary>
    /// The apply options.
    /// </summary>
    public ApplyOptions Options { get; init; } = ApplyOptions.Default;

    /// <summary>
    /// Progress reporter.
    /// </summary>
    public IProgress<BatchApplyProgress>? Progress { get; init; }

    /// <summary>
    /// Current phase of the operation.
    /// </summary>
    public BatchApplyPhase CurrentPhase { get; set; }

    /// <summary>
    /// Number of completed operations.
    /// </summary>
    public int CompletedOperations { get; set; }

    /// <summary>
    /// Total operations to apply.
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// Current file being processed.
    /// </summary>
    public string CurrentFile { get; set; } = string.Empty;

    /// <summary>
    /// Whether the operation has been cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Whether rollback has been triggered.
    /// </summary>
    public bool IsRollingBack { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyContext"/> class.
    /// </summary>
    public ApplyContext(IFileSystemService fileSystem, IBackupService backupService)
    {
        RollbackManager = new RollbackManager(fileSystem, backupService);
    }

    /// <summary>
    /// Report progress to the reporter.
    /// </summary>
    public void ReportProgress(Action<BatchApplyProgress>? additionalHandler = null)
    {
        var progress = new BatchApplyProgress
        {
            TotalOperations = TotalOperations,
            CompletedOperations = CompletedOperations,
            Phase = CurrentPhase,
            CurrentFile = CurrentFile,
            CanCancel = !IsRollingBack,
            CancellationRequested = IsCancelled,
            Elapsed = DateTime.UtcNow - StartedAt
        };

        Progress?.Report(progress);
        additionalHandler?.Invoke(progress);
    }

    /// <summary>
    /// Build the final result.
    /// </summary>
    public BatchApplyResult BuildResult()
    {
        var successCount = Results.Count(r => r.Success);
        var failedCount = Results.Count(r => !r.Success);

        return new BatchApplyResult
        {
            AllSucceeded = failedCount == 0 && successCount == TotalOperations,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = TotalOperations - successCount - failedCount,
            Results = Results.ToList(),
            StartedAt = StartedAt,
            CompletedAt = DateTime.UtcNow,
            BackupPaths = BackupPaths.ToList(),
            WasCancelled = IsCancelled,
            WasRolledBack = IsRollingBack
        };
    }

    public void Dispose()
    {
        RollbackManager.Dispose();
    }
}
