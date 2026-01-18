namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIRECTORY CHANGED EVENT ARGS (v0.5.3e)                                  │
// │ Event arguments for working directory change notifications.             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event arguments for directory change events.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3e.</para>
/// <para>
/// Provides details about directory changes including:
/// <list type="bullet">
///   <item>Session that changed</item>
///   <item>Old and new directory paths</item>
///   <item>Source of the change (OSC 7, API, sync, etc.)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class DirectoryChangedEventArgs : EventArgs
{
    /// <summary>
    /// ID of the terminal session that changed.
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Previous working directory.
    /// </summary>
    /// <remarks>
    /// Empty string if the previous directory is unknown.
    /// </remarks>
    public string OldDirectory { get; init; } = string.Empty;

    /// <summary>
    /// New working directory.
    /// </summary>
    public string NewDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Source of the directory change.
    /// </summary>
    public DirectoryChangeSource Source { get; init; }

    /// <summary>
    /// Returns a string representation of the event.
    /// </summary>
    public override string ToString() =>
        $"DirectoryChanged({Source}: {OldDirectory} → {NewDirectory})";
}
