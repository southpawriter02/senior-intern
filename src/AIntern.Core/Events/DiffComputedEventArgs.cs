using AIntern.Core.Models;

namespace AIntern.Core.Events;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF COMPUTED EVENT ARGS (v0.4.5b)                                      │
// │ Event arguments when a streaming diff computation completes.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event arguments for when a streaming diff computation completes.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public sealed class DiffComputedEventArgs : EventArgs
{
    /// <summary>
    /// Creates new diff computed event args.
    /// </summary>
    public DiffComputedEventArgs(
        Guid blockId,
        DiffComputationState state,
        bool isIntermediate)
    {
        BlockId = blockId;
        State = state ?? throw new ArgumentNullException(nameof(state));
        IsIntermediate = isIntermediate;
        ComputedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The code block ID this diff is for.
    /// </summary>
    public Guid BlockId { get; }

    /// <summary>
    /// The full computation state.
    /// </summary>
    public DiffComputationState State { get; }

    /// <summary>
    /// True if this is an intermediate result (streaming still in progress).
    /// </summary>
    public bool IsIntermediate { get; }

    /// <summary>
    /// True if this is the final result (streaming complete).
    /// </summary>
    public bool IsFinal => !IsIntermediate;

    /// <summary>
    /// When the diff was computed.
    /// </summary>
    public DateTime ComputedAt { get; }

    /// <summary>
    /// The diff result (convenience accessor).
    /// </summary>
    public DiffResult? Result => State.Result;

    /// <summary>
    /// Whether computation was successful.
    /// </summary>
    public bool IsSuccess => State.IsCompleted && State.Result is not null;
}
