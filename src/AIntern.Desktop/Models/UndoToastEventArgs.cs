using AIntern.Core.Models;

namespace AIntern.Desktop.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ UNDO TOAST EVENT ARGS (v0.4.4h)                                         │
// │ Event arguments for undo toast.                                         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event arguments when the undo toast should be shown.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4h.</para>
/// </remarks>
public class UndoToastEventArgs : EventArgs
{
    /// <summary>
    /// The apply result that can be undone.
    /// </summary>
    public BatchApplyResult ApplyResult { get; }

    /// <summary>
    /// Number of files that were applied.
    /// </summary>
    public int FileCount => ApplyResult.SuccessCount;

    /// <summary>
    /// Create new undo toast event args.
    /// </summary>
    /// <param name="applyResult">The result to undo.</param>
    public UndoToastEventArgs(BatchApplyResult applyResult)
    {
        ApplyResult = applyResult ?? throw new ArgumentNullException(nameof(applyResult));
    }
}

/// <summary>
/// Event arguments when a batch undo operation completes.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4h.</para>
/// </remarks>
public class BatchUndoCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Whether the undo succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Error message if undo failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Create new batch undo completed event args.
    /// </summary>
    /// <param name="success">Whether undo succeeded.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    public BatchUndoCompletedEventArgs(bool success, string? errorMessage = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }
}
