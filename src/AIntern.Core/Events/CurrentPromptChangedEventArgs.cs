using AIntern.Core.Models;

namespace AIntern.Core.Events;

/// <summary>
/// Event args for changes to the currently selected system prompt.
/// </summary>
public sealed class CurrentPromptChangedEventArgs : EventArgs
{
    public SystemPrompt? NewPrompt { get; init; }
    public SystemPrompt? PreviousPrompt { get; init; }
}
