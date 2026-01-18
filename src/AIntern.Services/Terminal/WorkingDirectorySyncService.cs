using System.Collections.Concurrent;
using System.Web;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;

namespace AIntern.Services.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ WORKING DIRECTORY SYNC SERVICE (v0.5.3e)                                │
// │ Bi-directional directory sync between terminal and file explorer.       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Manages working directory synchronization between terminal and file explorer.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3e.</para>
/// <para>
/// Features:
/// <list type="bullet">
///   <item>Tracks terminal working directory via OSC 7 escape sequences</item>
///   <item>Bi-directional sync between terminal and file explorer</item>
///   <item>Automatic sync based on user settings (TerminalSyncMode)</item>
///   <item>Workspace linking for grouped terminal sessions</item>
///   <item>WSL path translation on Windows (/mnt/c → C:\)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class WorkingDirectorySyncService : IWorkingDirectorySyncService, IDisposable
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly ITerminalService _terminalService;
    private readonly IShellConfigurationService _shellConfig;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<WorkingDirectorySyncService> _logger;

    /// <summary>
    /// Tracks sync state per session.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, SessionSyncState> _sessionStates = new();

    private bool _disposed;

    // ─────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public event EventHandler<DirectoryChangedEventArgs>? TerminalDirectoryChanged;

    /// <inheritdoc />
    public event EventHandler<DirectoryChangedEventArgs>? ExplorerDirectoryChanged;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new working directory sync service.
    /// </summary>
    /// <param name="terminalService">Terminal service for session management.</param>
    /// <param name="shellConfig">Shell configuration for cd command formatting.</param>
    /// <param name="settingsService">Settings service for sync preferences.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public WorkingDirectorySyncService(
        ITerminalService terminalService,
        IShellConfigurationService shellConfig,
        ISettingsService settingsService,
        ILogger<WorkingDirectorySyncService> logger)
    {
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
        _shellConfig = shellConfig ?? throw new ArgumentNullException(nameof(shellConfig));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to session lifecycle events
        _terminalService.SessionCreated += OnSessionCreated;
        _terminalService.SessionClosed += OnSessionClosed;

        _logger.LogDebug("WorkingDirectorySyncService initialized");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Session Lifecycle Event Handlers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles session creation by initializing sync state.
    /// </summary>
    private void OnSessionCreated(object? sender, TerminalSessionEventArgs e)
    {
        var state = new SessionSyncState
        {
            SessionId = e.Session.Id,
            CurrentDirectory = e.Session.WorkingDirectory,
            AutoSyncEnabled = true  // Default to enabled
        };

        _sessionStates[e.Session.Id] = state;
        _logger.LogDebug("Tracking directory sync for session: {Id}, initial dir: {Dir}",
            e.Session.Id, e.Session.WorkingDirectory);
    }

    /// <summary>
    /// Handles session closure by removing sync state.
    /// </summary>
    private void OnSessionClosed(object? sender, TerminalSessionEventArgs e)
    {
        if (_sessionStates.TryRemove(e.Session.Id, out _))
        {
            _logger.LogDebug("Stopped tracking session: {Id}", e.Session.Id);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Query Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<string?> GetTerminalDirectoryAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Check tracking state first
        if (_sessionStates.TryGetValue(sessionId, out var state) &&
            !string.IsNullOrEmpty(state.CurrentDirectory))
        {
            _logger.LogDebug("GetTerminalDirectoryAsync: {Id} -> {Dir}",
                sessionId, state.CurrentDirectory);
            return Task.FromResult<string?>(state.CurrentDirectory);
        }

        // Fallback to session's WorkingDirectory property
        var session = _terminalService.Sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null && !string.IsNullOrEmpty(session.WorkingDirectory))
        {
            _logger.LogDebug("GetTerminalDirectoryAsync (from session): {Id} -> {Dir}",
                sessionId, session.WorkingDirectory);
            return Task.FromResult<string?>(session.WorkingDirectory);
        }

        _logger.LogDebug("GetTerminalDirectoryAsync: {Id} -> unknown", sessionId);
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public bool IsAutoSyncEnabled(Guid sessionId)
    {
        var enabled = _sessionStates.TryGetValue(sessionId, out var state) && state.AutoSyncEnabled;
        _logger.LogDebug("IsAutoSyncEnabled: {Id} -> {Enabled}", sessionId, enabled);
        return enabled;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Command Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task ChangeTerminalDirectoryAsync(Guid sessionId, string path, CancellationToken ct = default)
    {
        // Find session
        var session = _terminalService.Sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
        {
            _logger.LogWarning("ChangeTerminalDirectoryAsync: session not found: {Id}", sessionId);
            return;
        }

        // Validate directory exists
        if (!Directory.Exists(path))
        {
            _logger.LogWarning("ChangeTerminalDirectoryAsync: directory does not exist: {Path}", path);
            return;
        }

        // Get shell configuration for this shell
        var config = _shellConfig.GetConfiguration(session.ShellPath);
        var command = _shellConfig.FormatChangeDirectoryCommand(config.Type, path);

        _logger.LogDebug("ChangeTerminalDirectoryAsync: session {Id}, command: {Command}",
            sessionId, command);

        // Send cd command to terminal
        await _terminalService.WriteInputAsync(sessionId, command + "\n", ct);

        // Update state and fire event
        if (_sessionStates.TryGetValue(sessionId, out var state))
        {
            var oldDir = state.CurrentDirectory;
            state.CurrentDirectory = path;

            _logger.LogInformation("Terminal directory changed: {Id}, {Old} -> {New}",
                sessionId, oldDir, path);

            TerminalDirectoryChanged?.Invoke(this, new DirectoryChangedEventArgs
            {
                SessionId = sessionId,
                OldDirectory = oldDir ?? string.Empty,
                NewDirectory = path,
                Source = DirectoryChangeSource.Api
            });
        }
    }

    /// <inheritdoc />
    public async Task SyncTerminalToExplorerAsync(Guid sessionId, string explorerPath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(explorerPath))
        {
            _logger.LogWarning("SyncTerminalToExplorerAsync: empty explorer path");
            return;
        }

        if (!Directory.Exists(explorerPath))
        {
            _logger.LogWarning("SyncTerminalToExplorerAsync: explorer path does not exist: {Path}",
                explorerPath);
            return;
        }

        _logger.LogDebug("SyncTerminalToExplorerAsync: {Id} -> {Path}", sessionId, explorerPath);

        // Change terminal directory
        await ChangeTerminalDirectoryAsync(sessionId, explorerPath, ct);

        // Fire explorer sync event
        TerminalDirectoryChanged?.Invoke(this, new DirectoryChangedEventArgs
        {
            SessionId = sessionId,
            NewDirectory = explorerPath,
            Source = DirectoryChangeSource.ExplorerSync
        });
    }

    /// <inheritdoc />
    public Task SyncExplorerToTerminalAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Get current terminal directory
        if (!_sessionStates.TryGetValue(sessionId, out var state) ||
            string.IsNullOrEmpty(state.CurrentDirectory))
        {
            _logger.LogWarning("SyncExplorerToTerminalAsync: no directory for session {Id}", sessionId);
            return Task.CompletedTask;
        }

        _logger.LogDebug("SyncExplorerToTerminalAsync: {Id} -> {Dir}",
            sessionId, state.CurrentDirectory);

        // Fire event to notify file explorer
        ExplorerDirectoryChanged?.Invoke(this, new DirectoryChangedEventArgs
        {
            SessionId = sessionId,
            NewDirectory = state.CurrentDirectory,
            Source = DirectoryChangeSource.Shell
        });

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Configuration Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void SetAutoSync(Guid sessionId, bool enabled)
    {
        if (_sessionStates.TryGetValue(sessionId, out var state))
        {
            state.AutoSyncEnabled = enabled;
            _logger.LogDebug("SetAutoSync: {Id} -> {Enabled}", sessionId, enabled);
        }
        else
        {
            _logger.LogDebug("SetAutoSync: session not found {Id}", sessionId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Workspace Linking Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void LinkToWorkspace(Guid sessionId, Guid workspaceId)
    {
        if (_sessionStates.TryGetValue(sessionId, out var state))
        {
            state.LinkedWorkspaceId = workspaceId;
            _logger.LogDebug("LinkToWorkspace: session {SessionId} -> workspace {WorkspaceId}",
                sessionId, workspaceId);
        }
        else
        {
            _logger.LogDebug("LinkToWorkspace: session not found {Id}", sessionId);
        }
    }

    /// <inheritdoc />
    public void UnlinkFromWorkspace(Guid sessionId)
    {
        if (_sessionStates.TryGetValue(sessionId, out var state))
        {
            var oldWorkspace = state.LinkedWorkspaceId;
            state.LinkedWorkspaceId = null;
            _logger.LogDebug("UnlinkFromWorkspace: session {Id} unlinked from {WorkspaceId}",
                sessionId, oldWorkspace);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // OSC Integration
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void ProcessOsc7(Guid sessionId, string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            _logger.LogDebug("ProcessOsc7: empty URI for session {Id}", sessionId);
            return;
        }

        if (!_sessionStates.TryGetValue(sessionId, out var state))
        {
            _logger.LogDebug("ProcessOsc7: session not tracked {Id}", sessionId);
            return;
        }

        try
        {
            // Parse file:// URI
            var parsedUri = new Uri(uri);
            var path = HttpUtility.UrlDecode(parsedUri.LocalPath);

            _logger.LogDebug("ProcessOsc7: parsed URI {Uri} -> path {Path}", uri, path);

            // Handle WSL paths on Windows (/mnt/c/... -> C:\...)
            path = TranslateWslPath(path);

            // Validate directory exists
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                _logger.LogDebug("ProcessOsc7: path does not exist: {Path}", path);
                return;
            }

            // Update state
            var oldDir = state.CurrentDirectory;
            state.CurrentDirectory = path;

            _logger.LogDebug("ProcessOsc7: directory updated for session {Id}: {Old} -> {New}",
                sessionId, oldDir, path);

            // Fire terminal directory changed event
            TerminalDirectoryChanged?.Invoke(this, new DirectoryChangedEventArgs
            {
                SessionId = sessionId,
                OldDirectory = oldDir ?? string.Empty,
                NewDirectory = path,
                Source = DirectoryChangeSource.Osc7
            });

            // Check auto-sync settings
            var settings = _settingsService.CurrentSettings;
            if (state.AutoSyncEnabled && settings.SyncTerminalWithWorkspace)
            {
                HandleAutoSync(sessionId, path, settings.TerminalSyncMode);
            }
        }
        catch (UriFormatException ex)
        {
            _logger.LogWarning(ex, "ProcessOsc7: invalid URI format: {Uri}", uri);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ProcessOsc7: failed to process URI: {Uri}", uri);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Helper Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles automatic sync based on sync mode settings.
    /// </summary>
    /// <param name="sessionId">Session that triggered the sync.</param>
    /// <param name="path">New directory path.</param>
    /// <param name="mode">Sync mode from settings.</param>
    private void HandleAutoSync(Guid sessionId, string path, DirectorySyncMode mode)
    {
        _logger.LogDebug("HandleAutoSync: session {Id}, path {Path}, mode {Mode}",
            sessionId, path, mode);

        switch (mode)
        {
            case DirectorySyncMode.ActiveTerminalOnly:
                // Only sync if this is the active terminal
                // The caller (TerminalViewModel) should check if this session is active
                ExplorerDirectoryChanged?.Invoke(this, new DirectoryChangedEventArgs
                {
                    SessionId = sessionId,
                    NewDirectory = path,
                    Source = DirectoryChangeSource.Osc7
                });
                break;

            case DirectorySyncMode.AllLinkedTerminals:
                // Sync if this session is linked to a workspace
                if (_sessionStates.TryGetValue(sessionId, out var state) &&
                    state.LinkedWorkspaceId.HasValue)
                {
                    ExplorerDirectoryChanged?.Invoke(this, new DirectoryChangedEventArgs
                    {
                        SessionId = sessionId,
                        NewDirectory = path,
                        Source = DirectoryChangeSource.WorkspaceSync
                    });
                }
                break;

            case DirectorySyncMode.Manual:
                // No auto-sync in manual mode
                _logger.LogDebug("HandleAutoSync: manual mode, skipping sync");
                break;
        }
    }

    /// <summary>
    /// Translates WSL paths to Windows paths on Windows.
    /// </summary>
    /// <param name="path">Input path (potentially WSL format).</param>
    /// <returns>Translated Windows path, or original path if not WSL.</returns>
    /// <remarks>
    /// <para>
    /// Handles paths like:
    /// <list type="bullet">
    ///   <item>/mnt/c/Users/name → C:\Users\name</item>
    ///   <item>/mnt/d/Projects → D:\Projects</item>
    /// </list>
    /// </para>
    /// </remarks>
    internal static string TranslateWslPath(string path)
    {
        // Only relevant on Windows
        if (!OperatingSystem.IsWindows())
        {
            return path;
        }

        // Check for /mnt/X pattern
        if (!path.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        // Split path: ["", "mnt", "c", "Users", "name", ...]
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Validate structure: at least ["mnt", "X"]
        if (parts.Length >= 2 &&
            parts[0].Equals("mnt", StringComparison.OrdinalIgnoreCase) &&
            parts[1].Length == 1)
        {
            // Convert to Windows path
            var driveLetter = parts[1].ToUpperInvariant();
            var windowsPath = string.Join("\\", parts.Skip(2));
            return $"{driveLetter}:\\{windowsPath}";
        }

        return path;
    }

    // ─────────────────────────────────────────────────────────────────────
    // IDisposable
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Disposes of the service and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _terminalService.SessionCreated -= OnSessionCreated;
        _terminalService.SessionClosed -= OnSessionClosed;
        _sessionStates.Clear();
        _disposed = true;

        _logger.LogDebug("WorkingDirectorySyncService disposed");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Internal State Class
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tracks sync state for a terminal session.
    /// </summary>
    private sealed class SessionSyncState
    {
        /// <summary>
        /// Session ID.
        /// </summary>
        public Guid SessionId { get; init; }

        /// <summary>
        /// Current known working directory.
        /// </summary>
        public string? CurrentDirectory { get; set; }

        /// <summary>
        /// Whether auto-sync is enabled for this session.
        /// </summary>
        public bool AutoSyncEnabled { get; set; }

        /// <summary>
        /// Linked workspace ID for grouped syncing.
        /// </summary>
        public Guid? LinkedWorkspaceId { get; set; }
    }
}
