namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE CHANGE SERVICE INTERFACE (v0.4.3b)                                  │
// │ Service for applying code changes to the filesystem with undo support.   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for applying code changes to the filesystem with undo support.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3b.</para>
/// <para>
/// The file change service is the central orchestrator for all file modifications
/// in the apply workflow. It coordinates with diff, backup, and filesystem services
/// to provide a safe, undoable apply operation.
/// </para>
/// </remarks>
public interface IFileChangeService
{
    // ═══════════════════════════════════════════════════════════════════════
    // Apply Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Apply a single code block to the filesystem.
    /// </summary>
    Task<ApplyResult> ApplyCodeBlockAsync(
        CodeBlock block,
        string workspacePath,
        ApplyOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Apply multiple code blocks to the filesystem.
    /// </summary>
    Task<IReadOnlyList<ApplyResult>> ApplyCodeBlocksAsync(
        IEnumerable<CodeBlock> blocks,
        string workspacePath,
        ApplyOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Apply a pre-computed diff to the filesystem.
    /// </summary>
    Task<ApplyResult> ApplyDiffAsync(
        DiffResult diff,
        string workspacePath,
        ApplyOptions? options = null,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Preview Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Preview what would happen when applying a code block.
    /// </summary>
    Task<ApplyPreview> PreviewApplyAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Undo Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Undo the last change made to a specific file.
    /// </summary>
    Task<bool> UndoLastChangeAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Undo a specific change by its unique identifier.
    /// </summary>
    Task<bool> UndoChangeAsync(Guid changeId, CancellationToken ct = default);

    /// <summary>
    /// Check if undo is available for a specific file.
    /// </summary>
    bool CanUndo(string filePath);

    // ═══════════════════════════════════════════════════════════════════════
    // History Operations
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get the change history for a specific file.
    /// </summary>
    IReadOnlyList<FileChangeRecord> GetChangeHistory(string filePath, int maxRecords = 10);

    /// <summary>
    /// Get all pending undos across all files.
    /// </summary>
    IReadOnlyList<FileChangeRecord> GetPendingUndos();

    // ═══════════════════════════════════════════════════════════════════════
    // Conflict Detection
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Check for conflicts before applying a code block.
    /// </summary>
    Task<ConflictCheckResult> CheckForConflictsAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when a file is successfully changed.
    /// </summary>
    event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <summary>
    /// Raised when a file change operation fails.
    /// </summary>
    event EventHandler<FileChangeFailedEventArgs>? ChangeFailed;

    /// <summary>
    /// Raised when a change is successfully undone.
    /// </summary>
    event EventHandler<FileChangeUndoneEventArgs>? ChangeUndone;

    /// <summary>
    /// Raised when a conflict is detected during apply.
    /// </summary>
    event EventHandler<FileConflictDetectedEventArgs>? ConflictDetected;
}
