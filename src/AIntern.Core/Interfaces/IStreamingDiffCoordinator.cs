using AIntern.Core.Events;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STREAMING DIFF COORDINATOR INTERFACE (v0.4.5b)                          │
// │ Coordinates diff computation during LLM response streaming.             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Coordinates diff computation during LLM response streaming.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public interface IStreamingDiffCoordinator
{
    /// <summary>
    /// Event raised when a diff computation completes (intermediate or final).
    /// </summary>
    event EventHandler<DiffComputedEventArgs>? DiffComputed;

    /// <summary>
    /// Called when a code block is first detected during streaming.
    /// Queues an initial diff computation.
    /// </summary>
    /// <param name="block">The detected code block.</param>
    /// <param name="workspacePath">Path to the workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initial computation state.</returns>
    Task<DiffComputationState> OnCodeBlockDetectedAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a code block's content is updated during streaming.
    /// May trigger a debounced recomputation.
    /// </summary>
    /// <param name="block">The updated code block.</param>
    /// <param name="workspacePath">Path to the workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current computation state (may be cached).</returns>
    Task<DiffComputationState> OnCodeBlockUpdatedAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when streaming completes to finalize diff computation.
    /// Forces immediate recomputation with final content.
    /// </summary>
    /// <param name="block">The finalized code block.</param>
    /// <param name="workspacePath">Path to the workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final computation state.</returns>
    Task<DiffComputationState> FinalizeBlockDiffAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current computation state for a code block.
    /// </summary>
    /// <param name="blockId">The code block ID.</param>
    /// <returns>The computation state, or null if not found.</returns>
    DiffComputationState? GetComputationState(Guid blockId);

    /// <summary>
    /// Get all active computation states.
    /// </summary>
    /// <returns>All tracked computation states.</returns>
    IReadOnlyCollection<DiffComputationState> GetAllStates();

    /// <summary>
    /// Cancel computation for a specific block.
    /// </summary>
    /// <param name="blockId">The code block ID to cancel.</param>
    void Cancel(Guid blockId);

    /// <summary>
    /// Cancel all active computations.
    /// </summary>
    void CancelAll();

    /// <summary>
    /// Clear all tracked states (e.g., when starting new conversation).
    /// </summary>
    void Reset();
}
