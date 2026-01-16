namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF COMPUTATION STATUS (v0.4.5b)                                       │
// │ Status of a streaming diff computation.                                 │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Status of a streaming diff computation.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public enum DiffComputationStatus
{
    /// <summary>
    /// Computation is queued but not yet started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Computation is actively running.
    /// </summary>
    Computing = 1,

    /// <summary>
    /// Computation completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Computation failed with an error.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Computation was cancelled (e.g., user stopped streaming).
    /// </summary>
    Cancelled = 4
}
