namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIRECTORY CHANGE SOURCE (v0.5.3e)                                       │
// │ Enumeration for working directory change origins.                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Source of a working directory change.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3e.</para>
/// <para>
/// Used with <see cref="DirectoryChangedEventArgs"/> to indicate
/// what triggered a directory change event.
/// </para>
/// </remarks>
public enum DirectoryChangeSource
{
    /// <summary>
    /// Directory changed via shell command (cd).
    /// </summary>
    Shell,

    /// <summary>
    /// Directory changed via OSC 7 escape sequence.
    /// </summary>
    /// <remarks>
    /// OSC 7 format: ESC ] 7 ; file://hostname/path BEL
    /// </remarks>
    Osc7,

    /// <summary>
    /// Directory changed via explicit API call.
    /// </summary>
    Api,

    /// <summary>
    /// Directory changed via file explorer sync.
    /// </summary>
    ExplorerSync,

    /// <summary>
    /// Directory changed via workspace navigation.
    /// </summary>
    WorkspaceSync
}
