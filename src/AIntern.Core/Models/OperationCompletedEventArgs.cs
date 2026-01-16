namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ OPERATION COMPLETED EVENT ARGS (v0.4.4c)                                 │
// │ Event args for operation completion.                                     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event args for operation completion.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4c.</para>
/// </remarks>
public sealed class OperationCompletedEventArgs : EventArgs
{
    /// <summary>
    /// The operation that completed.
    /// </summary>
    public FileOperation Operation { get; init; } = null!;

    /// <summary>
    /// The result of the operation.
    /// </summary>
    public ApplyResult Result { get; init; } = null!;

    /// <summary>
    /// Index of this operation in the batch.
    /// </summary>
    public int OperationIndex { get; init; }

    /// <summary>
    /// Total operations in the batch.
    /// </summary>
    public int TotalOperations { get; init; }

    /// <summary>
    /// Whether this was the last operation.
    /// </summary>
    public bool IsLastOperation => OperationIndex == TotalOperations - 1;

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double ProgressPercent => TotalOperations > 0
        ? (double)(OperationIndex + 1) / TotalOperations * 100
        : 0;
}
