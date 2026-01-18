namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalSession (v0.5.1b)                                                │
// │ Represents an active terminal session with a PTY process.                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents an active terminal session with a PTY (pseudo-terminal) process.
/// </summary>
/// <remarks>
/// <para>
/// This class manages the lifecycle and metadata of a terminal session.
/// The actual PTY process management is handled by <c>ITerminalService</c> (v0.5.1d),
/// which sets the <see cref="OnDisposeAsync"/> callback.
/// </para>
/// <para>
/// Implements <see cref="IAsyncDisposable"/> for proper async cleanup of resources.
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
public sealed class TerminalSession : IAsyncDisposable
{
    #region Identity Properties

    /// <summary>
    /// Gets the unique identifier for this session.
    /// </summary>
    /// <remarks>
    /// Used to reference the session across service calls and for tab identification.
    /// </remarks>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name for the session.
    /// </summary>
    /// <remarks>
    /// Shown in the terminal tab. Can be updated via OSC escape sequences.
    /// Defaults to "Terminal".
    /// </remarks>
    public string Name { get; set; } = "Terminal";

    #endregion

    #region Process Properties

    /// <summary>
    /// Gets the path to the shell executable.
    /// </summary>
    /// <remarks>
    /// Examples: "/bin/zsh", "/bin/bash", "C:\Windows\System32\cmd.exe", "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
    /// </remarks>
    public string ShellPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the current working directory.
    /// </summary>
    /// <remarks>
    /// Updated when the shell changes directory (via OSC 7 escape sequence or polling).
    /// Used for file explorer synchronization.
    /// </remarks>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets the environment variables for this session.
    /// </summary>
    /// <remarks>
    /// Merged with system environment when spawning the PTY process.
    /// Useful for setting TERM, COLORTERM, custom paths, etc.
    /// </remarks>
    public Dictionary<string, string> Environment { get; init; } = new();

    #endregion

    #region State Properties

    /// <summary>
    /// Gets or sets the current session state.
    /// </summary>
    /// <remarks>
    /// See <see cref="TerminalSessionState"/> for state transition rules.
    /// </remarks>
    public TerminalSessionState State { get; set; } = TerminalSessionState.Starting;

    /// <summary>
    /// Gets the UTC timestamp when the session was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the session was closed.
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the session is still running or starting.
    /// </remarks>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Gets or sets the process exit code.
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the process is running. Set when the process exits.
    /// Exit code 0 typically indicates successful completion.
    /// </remarks>
    public int? ExitCode { get; set; }

    #endregion

    #region Terminal Properties

    /// <summary>
    /// Gets or sets the current terminal dimensions.
    /// </summary>
    /// <remarks>
    /// Updated when the terminal is resized. The PTY process receives a SIGWINCH signal.
    /// </remarks>
    public TerminalSize Size { get; set; } = TerminalSize.Default;

    /// <summary>
    /// Gets or sets the terminal title (from OSC escape sequences).
    /// </summary>
    /// <remarks>
    /// Set by the shell or applications via OSC 0/2 escape sequences.
    /// If set, may be displayed instead of <see cref="Name"/> in the tab.
    /// </remarks>
    public string? Title { get; set; }

    /// <summary>
    /// Gets a value indicating whether the session is interactive.
    /// </summary>
    /// <remarks>
    /// <c>true</c> for PTY-based sessions with full terminal capabilities.
    /// <c>false</c> for non-interactive sessions (e.g., command execution).
    /// </remarks>
    public bool IsInteractive { get; init; } = true;

    #endregion

    #region Workspace Integration

    /// <summary>
    /// Gets or sets the associated workspace ID.
    /// </summary>
    /// <remarks>
    /// When set, enables bidirectional synchronization:
    /// <list type="bullet">
    ///   <item><description>File explorer navigation updates terminal CWD</description></item>
    ///   <item><description>Terminal CWD changes update file explorer</description></item>
    /// </list>
    /// </remarks>
    public Guid? WorkspaceId { get; set; }

    #endregion

    #region Internal

    /// <summary>
    /// Gets or sets the async disposal callback.
    /// </summary>
    /// <remarks>
    /// Set by <c>ITerminalService</c> to handle PTY process cleanup.
    /// This follows the dependency inversion principle - the session doesn't
    /// know about the service, but the service can inject cleanup behavior.
    /// </remarks>
    internal Func<ValueTask>? OnDisposeAsync { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the effective display title for UI.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="Title"/> if set; otherwise returns <see cref="Name"/>.
    /// </remarks>
    public string DisplayTitle => Title ?? Name;

    /// <summary>
    /// Gets a value indicating whether the session is in a terminal state.
    /// </summary>
    /// <remarks>
    /// <c>true</c> if the session is <see cref="TerminalSessionState.Exited"/>,
    /// <see cref="TerminalSessionState.Error"/>, or <see cref="TerminalSessionState.Closing"/>.
    /// </remarks>
    public bool IsTerminated => 
        State is TerminalSessionState.Exited or 
                 TerminalSessionState.Error or 
                 TerminalSessionState.Closing;

    /// <summary>
    /// Gets the session duration.
    /// </summary>
    /// <remarks>
    /// Returns the time from creation to close, or to now if still running.
    /// </remarks>
    public TimeSpan Duration => (ClosedAt ?? DateTime.UtcNow) - CreatedAt;

    #endregion

    #region IAsyncDisposable

    /// <summary>
    /// Asynchronously releases resources associated with this session.
    /// </summary>
    /// <returns>A task representing the asynchronous disposal operation.</returns>
    /// <remarks>
    /// Invokes the <see cref="OnDisposeAsync"/> callback if set by the terminal service.
    /// This cleans up the PTY process and associated resources.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (OnDisposeAsync != null)
        {
            await OnDisposeAsync();
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Returns a string representation of the session.
    /// </summary>
    /// <returns>A string in the format "TerminalSession(ID, Name, State)".</returns>
    public override string ToString() =>
        $"TerminalSession({Id.ToString()[..8]}, {Name}, {State})";

    #endregion
}
