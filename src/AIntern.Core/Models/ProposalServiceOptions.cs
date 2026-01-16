namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROPOSAL SERVICE OPTIONS (v0.4.4c)                                       │
// │ Configuration options for the proposal service.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration options for the proposal service.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4c.</para>
/// </remarks>
public sealed record ProposalServiceOptions
{
    /// <summary>
    /// Whether to create backups before modifying files.
    /// </summary>
    public bool EnableBackups { get; init; } = true;

    /// <summary>
    /// Whether to enable automatic rollback on failure.
    /// </summary>
    public bool EnableRollback { get; init; } = true;

    /// <summary>
    /// Whether to rollback on partial failure (some ops fail).
    /// If false, partial success is allowed.
    /// </summary>
    public bool RollbackOnPartialFailure { get; init; } = false;

    /// <summary>
    /// Maximum number of parallel file operations.
    /// </summary>
    /// <remarks>
    /// Set to 1 for strictly sequential operations.
    /// Higher values may improve performance for many small files.
    /// </remarks>
    public int MaxParallelOperations { get; init; } = 1;

    /// <summary>
    /// Maximum path length (characters).
    /// </summary>
    public int PathLengthLimit { get; init; } = 260;

    /// <summary>
    /// Whether to validate before applying.
    /// </summary>
    public bool ValidateBeforeApply { get; init; } = true;

    /// <summary>
    /// Whether to continue applying after individual failures.
    /// </summary>
    public bool ContinueOnFailure { get; init; } = true;

    /// <summary>
    /// Whether to create parent directories automatically.
    /// </summary>
    public bool CreateParentDirectories { get; init; } = true;

    /// <summary>
    /// Whether to preserve file timestamps on modify.
    /// </summary>
    public bool PreserveTimestamps { get; init; } = false;

    /// <summary>
    /// File encoding to use for writing.
    /// </summary>
    public string FileEncoding { get; init; } = "utf-8";

    /// <summary>
    /// Whether to use BOM for UTF-8 files.
    /// </summary>
    public bool UseUtf8Bom { get; init; } = false;

    /// <summary>
    /// Progress update interval during batch operations.
    /// </summary>
    public TimeSpan ProgressUpdateInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    // ═══════════════════════════════════════════════════════════════════════
    // Static Instances
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Default options.
    /// </summary>
    public static ProposalServiceOptions Default { get; } = new();

    /// <summary>
    /// Safe options with rollback and validation.
    /// </summary>
    public static ProposalServiceOptions Safe { get; } = new()
    {
        EnableBackups = true,
        EnableRollback = true,
        ValidateBeforeApply = true,
        ContinueOnFailure = false,
        RollbackOnPartialFailure = true
    };

    /// <summary>
    /// Fast options for trusted operations.
    /// </summary>
    public static ProposalServiceOptions Fast { get; } = new()
    {
        EnableBackups = false,
        EnableRollback = false,
        ValidateBeforeApply = false,
        MaxParallelOperations = 4
    };
}
