namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalSessionState (v0.5.1b)                                           │
// │ Represents the lifecycle state of a terminal session.                    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents the lifecycle state of a terminal session.
/// </summary>
/// <remarks>
/// <para>
/// State transitions follow this pattern:
/// <code>
///     Starting → Running → Exited
///                  ↓
///                Error → Closing
///                  ↓
///               Closing
/// </code>
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
public enum TerminalSessionState
{
    /// <summary>
    /// The session is being created and initialized.
    /// </summary>
    /// <remarks>
    /// This is the initial state when a new terminal session is requested.
    /// The PTY process is being spawned during this state.
    /// </remarks>
    Starting,

    /// <summary>
    /// The session is active and accepting input.
    /// </summary>
    /// <remarks>
    /// The PTY process is running and the terminal is ready for user interaction.
    /// </remarks>
    Running,

    /// <summary>
    /// The session's process has exited.
    /// </summary>
    /// <remarks>
    /// The process may have exited normally (exit code 0) or abnormally.
    /// Check <see cref="TerminalSession.ExitCode"/> for the exit status.
    /// </remarks>
    Exited,

    /// <summary>
    /// The session encountered an error.
    /// </summary>
    /// <remarks>
    /// This may occur during startup (e.g., shell not found) or during
    /// operation (e.g., I/O error). Recovery may be possible in some cases.
    /// </remarks>
    Error,

    /// <summary>
    /// The session is being closed and disposed.
    /// </summary>
    /// <remarks>
    /// Resources are being released. This state prevents race conditions
    /// during disposal. The session cannot transition to any other state from here.
    /// </remarks>
    Closing
}
