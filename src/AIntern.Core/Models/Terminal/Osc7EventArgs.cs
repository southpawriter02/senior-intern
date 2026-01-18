namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ OSC 7 EVENT ARGS (v0.5.3e)                                              │
// │ Event arguments for OSC 7 escape sequence handling.                     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event arguments for OSC 7 escape sequence events.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3e.</para>
/// <para>
/// OSC 7 is an escape sequence used by shells to report their
/// current working directory in the format:
/// <c>ESC ] 7 ; file://hostname/path BEL</c>
/// </para>
/// </remarks>
public sealed class Osc7EventArgs : EventArgs
{
    /// <summary>
    /// The file:// URI containing the current directory.
    /// </summary>
    /// <remarks>
    /// Format: file://hostname/path/to/directory
    /// The path component may be URL-encoded.
    /// </remarks>
    public string Uri { get; init; } = string.Empty;

    /// <summary>
    /// Returns a string representation of the event.
    /// </summary>
    public override string ToString() => $"Osc7({Uri})";
}
