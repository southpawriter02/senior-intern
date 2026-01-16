namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF RESULT (v0.4.2a)                                                    │
// │ Top-level container for diff computation results.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents the complete result of a diff computation between two texts.
/// This is the root model returned by IDiffService.ComputeDiff().
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2a.</para>
/// <para>
/// Contains the original and proposed content, organized hunks of changes,
/// and summary statistics. Supports new file creation, file deletion, and
/// binary file detection.
/// </para>
/// </remarks>
public sealed class DiffResult
{
    /// <summary>
    /// Unique identifier for this diff result.
    /// Used for caching and tracking diffs across UI interactions.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Path to the original file being modified.
    /// Empty string for in-memory comparisons or new file creation.
    /// </summary>
    /// <example>"src/Services/MyService.cs"</example>
    public string OriginalFilePath { get; init; } = string.Empty;

    /// <summary>
    /// The original content before changes.
    /// Empty string if IsNewFile is true.
    /// </summary>
    public string OriginalContent { get; init; } = string.Empty;

    /// <summary>
    /// The proposed content after changes.
    /// Empty string if IsDeleteFile is true.
    /// </summary>
    public string ProposedContent { get; init; } = string.Empty;

    /// <summary>
    /// Individual diff hunks (contiguous groups of changes).
    /// Each hunk contains context lines plus the actual changes.
    /// </summary>
    public IReadOnlyList<DiffHunk> Hunks { get; init; } = [];

    /// <summary>
    /// Summary statistics for the diff.
    /// </summary>
    public DiffStats Stats { get; init; } = DiffStats.Empty;

    /// <summary>
    /// Whether this represents a new file creation (original didn't exist).
    /// When true, OriginalContent is empty.
    /// </summary>
    public bool IsNewFile { get; init; }

    /// <summary>
    /// Whether this represents a file deletion (proposed is empty).
    /// When true, ProposedContent is empty.
    /// </summary>
    public bool IsDeleteFile { get; init; }

    /// <summary>
    /// Whether this represents a binary file that cannot be diffed.
    /// When true, Hunks will be empty.
    /// </summary>
    public bool IsBinaryFile { get; init; }

    /// <summary>
    /// The code block that generated this diff (if applicable).
    /// Links back to the v0.4.1 CodeBlock model.
    /// </summary>
    public Guid? SourceBlockId { get; init; }

    /// <summary>
    /// When this diff was computed.
    /// Useful for cache invalidation and debugging.
    /// </summary>
    public DateTime ComputedAt { get; init; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether there are any actual changes between original and proposed.
    /// Returns false if the files are identical or if this is a binary file.
    /// </summary>
    public bool HasChanges => Hunks.Count > 0 &&
        (Stats.AddedLines > 0 || Stats.RemovedLines > 0 || Stats.ModifiedLines > 0);

    /// <summary>
    /// Total number of hunks in this diff.
    /// </summary>
    public int HunkCount => Hunks.Count;

    /// <summary>
    /// Whether the diff can be applied (not binary, has changes or is new/delete).
    /// </summary>
    public bool IsApplicable => !IsBinaryFile && (HasChanges || IsNewFile || IsDeleteFile);

    /// <summary>
    /// File name extracted from OriginalFilePath.
    /// </summary>
    public string FileName => string.IsNullOrEmpty(OriginalFilePath)
        ? "(untitled)"
        : Path.GetFileName(OriginalFilePath);

    // ═══════════════════════════════════════════════════════════════════════
    // Static Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an empty diff result for identical content.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="content">The identical content in both versions.</param>
    /// <returns>A <see cref="DiffResult"/> representing no changes.</returns>
    public static DiffResult NoChanges(string filePath, string content) => new()
    {
        OriginalFilePath = filePath,
        OriginalContent = content,
        ProposedContent = content,
        Hunks = [],
        Stats = DiffStats.Empty
    };

    /// <summary>
    /// Creates a diff result for a new file.
    /// </summary>
    /// <param name="filePath">The file path for the new file.</param>
    /// <param name="content">The content of the new file.</param>
    /// <param name="hunks">The hunks representing the additions.</param>
    /// <param name="stats">Statistics for the additions.</param>
    /// <returns>A <see cref="DiffResult"/> representing a new file creation.</returns>
    public static DiffResult NewFile(
        string filePath,
        string content,
        IReadOnlyList<DiffHunk> hunks,
        DiffStats stats) => new()
    {
        OriginalFilePath = filePath,
        OriginalContent = string.Empty,
        ProposedContent = content,
        Hunks = hunks,
        Stats = stats,
        IsNewFile = true
    };

    /// <summary>
    /// Creates a diff result for file deletion.
    /// </summary>
    /// <param name="filePath">The file path of the deleted file.</param>
    /// <param name="originalContent">The content of the deleted file.</param>
    /// <param name="hunks">The hunks representing the removals.</param>
    /// <param name="stats">Statistics for the removals.</param>
    /// <returns>A <see cref="DiffResult"/> representing a file deletion.</returns>
    public static DiffResult DeleteFile(
        string filePath,
        string originalContent,
        IReadOnlyList<DiffHunk> hunks,
        DiffStats stats) => new()
    {
        OriginalFilePath = filePath,
        OriginalContent = originalContent,
        ProposedContent = string.Empty,
        Hunks = hunks,
        Stats = stats,
        IsDeleteFile = true
    };

    /// <summary>
    /// Creates a diff result for binary files.
    /// </summary>
    /// <param name="filePath">The file path of the binary file.</param>
    /// <returns>A <see cref="DiffResult"/> indicating a binary file.</returns>
    public static DiffResult BinaryFile(string filePath) => new()
    {
        OriginalFilePath = filePath,
        IsBinaryFile = true,
        Hunks = [],
        Stats = DiffStats.Empty
    };
}
