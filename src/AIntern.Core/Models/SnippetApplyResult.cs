namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET APPLY RESULT (v0.4.5d)                                          │
// │ Result of applying a code snippet to a file.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Result of applying a code snippet to a file.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5d.</para>
/// </remarks>
public sealed record SnippetApplyResult
{
    /// <summary>
    /// Whether the snippet was successfully applied.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Target file path.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Options used for the apply operation.
    /// </summary>
    public SnippetApplyOptions? Options { get; init; }

    /// <summary>
    /// Path to backup file (if created).
    /// </summary>
    public string? BackupPath { get; init; }

    /// <summary>
    /// Number of lines modified (affected range size).
    /// </summary>
    public int LinesModified { get; init; }

    /// <summary>
    /// Number of lines added.
    /// </summary>
    public int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines removed.
    /// </summary>
    public int LinesRemoved { get; init; }

    /// <summary>
    /// The computed diff (if available).
    /// </summary>
    public DiffResult? Diff { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When the apply operation completed.
    /// </summary>
    public DateTime AppliedAt { get; init; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static SnippetApplyResult Succeeded(
        string filePath,
        SnippetApplyOptions options,
        string? backupPath,
        int linesModified,
        int linesAdded,
        int linesRemoved,
        DiffResult? diff = null) => new()
    {
        IsSuccess = true,
        FilePath = filePath,
        Options = options,
        BackupPath = backupPath,
        LinesModified = linesModified,
        LinesAdded = linesAdded,
        LinesRemoved = linesRemoved,
        Diff = diff
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static SnippetApplyResult Failed(string filePath, string errorMessage) => new()
    {
        IsSuccess = false,
        FilePath = filePath,
        ErrorMessage = errorMessage
    };
}
