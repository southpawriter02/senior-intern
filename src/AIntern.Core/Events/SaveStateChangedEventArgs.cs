namespace AIntern.Core.Events;

/// <summary>
/// Event args for save state changes (for UI indicators).
/// </summary>
/// <remarks>
/// Fired by <c>IConversationService</c> when save operations start,
/// complete, or fail. Used to update status indicators in the UI.
/// </remarks>
public sealed class SaveStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether a save operation is currently in progress.
    /// </summary>
    public required bool IsSaving { get; init; }

    /// <summary>
    /// Gets whether there are unsaved changes pending.
    /// </summary>
    public required bool HasUnsavedChanges { get; init; }

    /// <summary>
    /// Gets when the last successful save occurred.
    /// </summary>
    /// <remarks>
    /// Null if no save has completed yet or after a failure.
    /// </remarks>
    public DateTime? LastSavedAt { get; init; }

    /// <summary>
    /// Gets any error message from a failed save operation.
    /// </summary>
    /// <remarks>
    /// Null on successful save or when save is in progress.
    /// </remarks>
    public string? Error { get; init; }
}
