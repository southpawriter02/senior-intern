using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ I FILE TREE PROPOSAL SERVICE (v0.4.4c)                                   │
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
public interface IFileTreeProposalService
{
    // ═══════════════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validate a proposal against the current workspace state.
    /// </summary>
    /// <param name="proposal">The proposal to validate.</param>
    /// <param name="workspacePath">Absolute path to the workspace root.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with any errors and warnings.</returns>
    /// <remarks>
    /// Validation checks include: duplicate paths, invalid path chars,
    /// paths outside workspace, path length, file existence, content.
    /// </remarks>
    Task<ProposalValidationResult> ValidateProposalAsync(
        FileTreeProposal proposal,
        string workspacePath,
        CancellationToken ct = default);

    /// <summary>
    /// Validate a single operation.
    /// </summary>
    /// <param name="operation">The operation to validate.</param>
    /// <param name="workspacePath">Absolute path to the workspace root.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of validation issues for this operation.</returns>
    Task<IReadOnlyList<ValidationIssue>> ValidateOperationAsync(
        FileOperation operation,
        string workspacePath,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Apply
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Apply selected operations from a proposal.
    /// </summary>
    /// <param name="proposal">The proposal to apply.</param>
    /// <param name="workspacePath">Absolute path to the workspace root.</param>
    /// <param name="options">Apply options (backup, etc.).</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the batch apply operation.</returns>
    /// <remarks>
    /// The apply process follows these phases:
    /// 1. Validation - Check all operations
    /// 2. Create directories - Ensure parent directories exist
    /// 3. Create backups - Backup existing files (if configured)
    /// 4. Write files - Apply each operation in order
    /// 5. Finalize - Update statuses and build result
    /// Cancellation triggers rollback of completed operations.
    /// </remarks>
    Task<BatchApplyResult> ApplyProposalAsync(
        FileTreeProposal proposal,
        string workspacePath,
        ApplyOptions? options = null,
        IProgress<BatchApplyProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Apply a single operation.
    /// </summary>
    /// <param name="operation">The operation to apply.</param>
    /// <param name="workspacePath">Absolute path to the workspace root.</param>
    /// <param name="options">Apply options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<ApplyResult> ApplyOperationAsync(
        FileOperation operation,
        string workspacePath,
        ApplyOptions? options = null,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Preview
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Preview what changes will be made by a proposal.
    /// </summary>
    /// <param name="proposal">The proposal to preview.</param>
    /// <param name="workspacePath">Absolute path to the workspace root.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of diff results for each operation.</returns>
    /// <remarks>
    /// Generates diffs for Create and Modify operations.
    /// New files show full content as additions.
    /// Modifications show line-by-line differences.
    /// </remarks>
    Task<IReadOnlyList<DiffResult>> PreviewProposalAsync(
        FileTreeProposal proposal,
        string workspacePath,
        CancellationToken ct = default);

    /// <summary>
    /// Preview a single operation.
    /// </summary>
    /// <param name="operation">The operation to preview.</param>
    /// <param name="workspacePath">Absolute path to the workspace root.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Diff result for the operation.</returns>
    Task<DiffResult?> PreviewOperationAsync(
        FileOperation operation,
        string workspacePath,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Undo
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Undo all changes from a batch apply.
    /// </summary>
    /// <param name="result">The batch apply result to undo.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if all operations were undone successfully.</returns>
    /// <remarks>
    /// Undoes operations in reverse order.
    /// Uses backups to restore modified files.
    /// Deletes newly created files.
    /// </remarks>
    Task<bool> UndoBatchApplyAsync(
        BatchApplyResult result,
        CancellationToken ct = default);

    /// <summary>
    /// Undo a single apply result.
    /// </summary>
    /// <param name="result">The apply result to undo.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the operation was undone successfully.</returns>
    Task<bool> UndoApplyResultAsync(
        ApplyResult result,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Utility
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Check which files in a proposal already exist.
    /// </summary>
    /// <param name="proposal">The proposal to check.</param>
    /// <param name="workspacePath">Absolute path to the workspace root.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping relative paths to existence.</returns>
    Task<IReadOnlyDictionary<string, bool>> CheckExistingFilesAsync(
        FileTreeProposal proposal,
        string workspacePath,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a path is within the workspace boundaries.
    /// </summary>
    /// <param name="path">The path to check (relative or absolute).</param>
    /// <param name="workspacePath">Absolute path to the workspace root.</param>
    /// <returns>True if the path is within the workspace.</returns>
    bool IsWithinWorkspace(string path, string workspacePath);

    /// <summary>
    /// Estimate the time required to apply a proposal.
    /// </summary>
    /// <param name="proposal">The proposal to estimate.</param>
    /// <returns>Estimated time span.</returns>
    TimeSpan EstimateApplyTime(FileTreeProposal proposal);

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Event raised when batch apply progress updates.
    /// </summary>
    event EventHandler<BatchApplyProgress>? ProgressChanged;

    /// <summary>
    /// Event raised when an individual operation completes.
    /// </summary>
    event EventHandler<OperationCompletedEventArgs>? OperationCompleted;

    /// <summary>
    /// Event raised when validation completes.
    /// </summary>
    event EventHandler<ProposalValidationResult>? ValidationCompleted;
}
