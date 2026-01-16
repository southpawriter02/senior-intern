namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF SERVICE INTERFACE (v0.4.2b)                                         │
// │ Service for computing diffs between text content.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for computing diffs between text content.
/// Used to generate side-by-side diff views for code changes.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2b.</para>
/// <para>
/// Provides synchronous methods for direct content comparison and
/// asynchronous methods for file-based operations that interact with
/// the file system to read original content.
/// </para>
/// </remarks>
public interface IDiffService
{
    /// <summary>
    /// Compute diff between original and proposed content.
    /// </summary>
    /// <param name="originalContent">The original content before changes.</param>
    /// <param name="proposedContent">The proposed content after changes.</param>
    /// <returns>A DiffResult containing hunks and statistics.</returns>
    DiffResult ComputeDiff(string originalContent, string proposedContent);

    /// <summary>
    /// Compute diff with file path context for display.
    /// </summary>
    /// <param name="originalContent">The original content before changes.</param>
    /// <param name="proposedContent">The proposed content after changes.</param>
    /// <param name="filePath">The file path for context (used in DiffResult.OriginalFilePath).</param>
    /// <returns>A DiffResult containing hunks and statistics.</returns>
    DiffResult ComputeDiff(string originalContent, string proposedContent, string filePath);

    /// <summary>
    /// Compute diff with custom options.
    /// </summary>
    /// <param name="originalContent">The original content before changes.</param>
    /// <param name="proposedContent">The proposed content after changes.</param>
    /// <param name="filePath">The file path for context.</param>
    /// <param name="options">Custom diff options.</param>
    /// <returns>A DiffResult containing hunks and statistics.</returns>
    DiffResult ComputeDiff(
        string originalContent,
        string proposedContent,
        string filePath,
        DiffOptions options);

    /// <summary>
    /// Compute diff for a new file (all lines marked as added).
    /// </summary>
    /// <param name="proposedContent">The content of the new file.</param>
    /// <param name="filePath">The target file path.</param>
    /// <returns>A DiffResult with IsNewFile=true and all lines as Added.</returns>
    DiffResult ComputeNewFileDiff(string proposedContent, string filePath);

    /// <summary>
    /// Compute diff for file deletion (all lines marked as removed).
    /// </summary>
    /// <param name="originalContent">The content of the file being deleted.</param>
    /// <param name="filePath">The file path being deleted.</param>
    /// <returns>A DiffResult with IsDeleteFile=true and all lines as Removed.</returns>
    DiffResult ComputeDeleteFileDiff(string originalContent, string filePath);

    /// <summary>
    /// Compute diff for a code block against its target file.
    /// Reads the original file from disk and computes the diff.
    /// </summary>
    /// <param name="block">The code block containing proposed changes.</param>
    /// <param name="workspacePath">The workspace root path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A DiffResult comparing the file's current content to the block's proposed content.</returns>
    /// <exception cref="ArgumentException">If block.TargetFilePath is null or empty.</exception>
    Task<DiffResult> ComputeDiffForBlockAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute merged diff for multiple code blocks targeting the same file.
    /// </summary>
    /// <param name="blocks">The code blocks to merge.</param>
    /// <param name="workspacePath">The workspace root path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A DiffResult representing all merged changes.</returns>
    /// <exception cref="ArgumentException">If blocks is empty or blocks target different files.</exception>
    Task<DiffResult> ComputeMergedDiffAsync(
        IReadOnlyList<CodeBlock> blocks,
        string workspacePath,
        CancellationToken cancellationToken = default);
}
