namespace AIntern.Core.Events;

/// <summary>
/// Event args for changes to the system prompt list.
/// </summary>
public sealed class PromptListChangedEventArgs : EventArgs
{
    public required PromptListChangeType ChangeType { get; init; }
    public Guid? AffectedPromptId { get; init; }
    public string? AffectedPromptName { get; init; }
}

/// <summary>
/// Type of change to the prompt list.
/// </summary>
public enum PromptListChangeType
{
    PromptCreated,
    PromptUpdated,
    PromptDeleted,
    DefaultChanged,
    ListRefreshed
}
