namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ WORKING DIRECTORY SYNC SERVICE INTERFACE (v0.5.3e)                      │
// │ Bi-directional sync between terminal and file explorer.                 │
// └─────────────────────────────────────────────────────────────────────────┘

using AIntern.Core.Models.Terminal;

/// <summary>
/// Service for synchronizing working directories between terminal and file explorer.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3e.</para>
/// <para>
/// Provides:
/// <list type="bullet">
///   <item>Tracking terminal working directory via OSC 7</item>
///   <item>Bi-directional sync between terminal and file explorer</item>
///   <item>Automatic sync based on user settings</item>
///   <item>Workspace linking for terminal sessions</item>
///   <item>WSL path translation on Windows</item>
/// </list>
/// </para>
/// </remarks>
public interface IWorkingDirectorySyncService
{
    // ─────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when a terminal's working directory changes.
    /// </summary>
    /// <remarks>
    /// Fired for all directory changes regardless of source (OSC 7, API, sync).
    /// </remarks>
    event EventHandler<DirectoryChangedEventArgs>? TerminalDirectoryChanged;

    /// <summary>
    /// Raised when the file explorer should navigate to a directory.
    /// </summary>
    /// <remarks>
    /// Only fired when auto-sync is enabled and conditions are met.
    /// </remarks>
    event EventHandler<DirectoryChangedEventArgs>? ExplorerDirectoryChanged;

    // ─────────────────────────────────────────────────────────────────────
    // Query
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the current working directory for a terminal session.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current directory path, or null if unknown.</returns>
    Task<string?> GetTerminalDirectoryAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Checks if auto-sync is enabled for a session.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <returns>True if auto-sync is enabled.</returns>
    bool IsAutoSyncEnabled(Guid sessionId);

    // ─────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Changes the terminal's working directory.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="path">Directory path to change to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Sends the appropriate cd command to the shell.
    /// </remarks>
    Task ChangeTerminalDirectoryAsync(Guid sessionId, string path, CancellationToken ct = default);

    /// <summary>
    /// Syncs terminal directory to match file explorer.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="explorerPath">Current file explorer path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SyncTerminalToExplorerAsync(Guid sessionId, string explorerPath, CancellationToken ct = default);

    /// <summary>
    /// Syncs file explorer to match terminal directory.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Fires <see cref="ExplorerDirectoryChanged"/> event.
    /// </remarks>
    Task SyncExplorerToTerminalAsync(Guid sessionId, CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────────────
    // Configuration
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enables or disables automatic sync for a session.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="enabled">True to enable auto-sync.</param>
    void SetAutoSync(Guid sessionId, bool enabled);

    // ─────────────────────────────────────────────────────────────────────
    // Workspace Linking
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Links a terminal session to a workspace.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="workspaceId">Workspace ID to link to.</param>
    /// <remarks>
    /// Linked sessions can sync together when TerminalSyncMode is AllLinkedTerminals.
    /// </remarks>
    void LinkToWorkspace(Guid sessionId, Guid workspaceId);

    /// <summary>
    /// Unlinks a terminal session from its workspace.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    void UnlinkFromWorkspace(Guid sessionId);

    // ─────────────────────────────────────────────────────────────────────
    // OSC Integration
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Processes an OSC 7 directory report from the terminal.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="uri">The file:// URI from OSC 7.</param>
    /// <remarks>
    /// <para>
    /// OSC 7 format: ESC ] 7 ; file://hostname/path BEL
    /// </para>
    /// <para>
    /// Handles URL decoding and WSL path translation.
    /// </para>
    /// </remarks>
    void ProcessOsc7(Guid sessionId, string uri);
}
