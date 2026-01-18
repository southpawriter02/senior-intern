namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalPanelViewModel (v0.5.2d)                                             │
// │ ViewModel for the terminal panel with session management and commands.       │
// │ Manages terminal tabs, panel visibility, and terminal settings.              │
// └─────────────────────────────────────────────────────────────────────────────┘

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;

#region Type Documentation

/// <summary>
/// ViewModel for the terminal panel with session management and commands.
/// </summary>
/// <remarks>
/// <para>
/// The TerminalPanelViewModel manages:
/// <list type="bullet">
///   <item><description>Session collection (<see cref="Sessions"/>)</description></item>
///   <item><description>Active session tracking (<see cref="ActiveSession"/>)</description></item>
///   <item><description>Panel visibility and maximize state</description></item>
///   <item><description>Terminal settings (font, theme)</description></item>
///   <item><description>Tab navigation (circular next/previous)</description></item>
/// </list>
/// </para>
/// <para>
/// Commands:
/// <code>
/// Session Management:
///   NewSessionCommand          - Create new terminal session
///   CloseSessionCommand(vm)    - Close specific session
///   CloseActiveSessionCommand  - Close the active session
///   ActivateSessionCommand(vm) - Switch to specified session
/// 
/// Panel Visibility:
///   TogglePanelCommand         - Toggle panel visibility
///   ShowPanelCommand           - Show panel (creates session if empty)
///   HidePanelCommand           - Hide panel and reset maximize
///   ToggleMaximizeCommand      - Toggle maximize state
/// 
/// Terminal Operations:
///   ClearTerminalCommand       - Clear screen (sends "clear" command)
///   NextTabCommand             - Navigate to next tab (circular)
///   PreviousTabCommand         - Navigate to previous tab (circular)
/// </code>
/// </para>
/// <para>
/// Event Handling:
/// All <see cref="ITerminalService"/> events are dispatched to the UI thread
/// via <see cref="Dispatcher.UIThread"/>.
/// </para>
/// <para>Added in v0.5.2d.</para>
/// </remarks>

#endregion

public partial class TerminalPanelViewModel : ViewModelBase
{
    #region Private Fields

    /// <summary>
    /// The terminal service for session management.
    /// </summary>
    private readonly ITerminalService _terminalService;

    /// <summary>
    /// Optional workspace service for initial working directory.
    /// </summary>
    private readonly IWorkspaceService? _workspaceService;

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    private readonly ILogger<TerminalPanelViewModel>? _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // Search Fields (v0.5.5c)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel for the terminal search bar.
    /// </summary>
    /// <remarks>Added in v0.5.5c.</remarks>
    private TerminalSearchBarViewModel? _searchBarViewModel;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of all terminal sessions.
    /// </summary>
    /// <remarks>
    /// This collection is bound to the tab bar in the view.
    /// Sessions are added/removed in response to service events.
    /// </remarks>
    public ObservableCollection<TerminalSessionViewModel> Sessions { get; } = new();

    #endregion

    #region Observable Properties - Session State

    /// <summary>
    /// Gets or sets the currently active session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The active session receives keyboard input and is displayed
    /// in the terminal control. Only one session can be active at a time.
    /// </para>
    /// <para>
    /// Changing the active session:
    /// <list type="bullet">
    ///   <item><description>Notifies <see cref="HasActiveSession"/></description></item>
    ///   <item><description>Updates <see cref="CloseActiveSessionCommand"/> CanExecute</description></item>
    ///   <item><description>Updates <see cref="ClearTerminalCommand"/> CanExecute</description></item>
    ///   <item><description>Raises <see cref="ActiveSessionChanged"/> event</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveSession))]
    [NotifyCanExecuteChangedFor(nameof(CloseActiveSessionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearTerminalCommand))]
    private TerminalSessionViewModel? _activeSession;

    /// <summary>
    /// Gets whether there is an active session.
    /// </summary>
    public bool HasActiveSession => ActiveSession != null;

    #endregion

    #region Observable Properties - Panel State

    /// <summary>
    /// Gets or sets whether the panel is visible.
    /// </summary>
    /// <remarks>
    /// When false, the terminal panel is collapsed/hidden.
    /// </remarks>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Gets or sets whether the panel is maximized.
    /// </summary>
    /// <remarks>
    /// When true, the panel fills the available space instead of using
    /// <see cref="PanelHeight"/>.
    /// </remarks>
    [ObservableProperty]
    private bool _isMaximized;

    /// <summary>
    /// Gets or sets the panel height when not maximized.
    /// </summary>
    /// <remarks>
    /// Default is 300 pixels. User-resizable via drag handle in view layer.
    /// </remarks>
    [ObservableProperty]
    private double _panelHeight = 300;

    #endregion

    #region Observable Properties - Terminal Settings

    /// <summary>
    /// Gets or sets the terminal font family.
    /// </summary>
    /// <remarks>
    /// Default is "Cascadia Mono". Monospace fonts recommended.
    /// </remarks>
    [ObservableProperty]
    private string _fontFamily = "Cascadia Mono";

    /// <summary>
    /// Gets or sets the terminal font size in points.
    /// </summary>
    /// <remarks>
    /// Default is 14.0 points. Valid range typically 8-24.
    /// </remarks>
    [ObservableProperty]
    private double _fontSize = 14;

    /// <summary>
    /// Gets or sets the current terminal theme.
    /// </summary>
    /// <remarks>
    /// Provides color scheme for terminal rendering.
    /// Default is <see cref="TerminalTheme.Dark"/>.
    /// </remarks>
    [ObservableProperty]
    private TerminalTheme _theme = TerminalTheme.Dark;

    #endregion

    #region Observable Properties - Search (v0.5.5c)

    /// <summary>
    /// Gets the search bar ViewModel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initialized lazily when the search service is available.
    /// The search bar is shared across all terminal sessions.
    /// </para>
    /// <para>Added in v0.5.5c.</para>
    /// </remarks>
    public TerminalSearchBarViewModel? SearchBarViewModel => _searchBarViewModel;

    #endregion

    #region Events

    /// <summary>
    /// Raised when the active session changes.
    /// </summary>
    /// <remarks>
    /// Used by the view to attach the terminal control to the new session.
    /// The event argument is the new active session, or null if none.
    /// </remarks>
    public event EventHandler<TerminalSessionViewModel?>? ActiveSessionChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new <see cref="TerminalPanelViewModel"/>.
    /// </summary>
    /// <param name="terminalService">The terminal service for session management.</param>
    /// <param name="searchService">Optional terminal search service for search functionality (v0.5.5c).</param>
    /// <param name="workspaceService">Optional workspace service for initial directory.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="terminalService"/> is null.</exception>
    public TerminalPanelViewModel(
        ITerminalService terminalService,
        ITerminalSearchService? searchService = null,
        IWorkspaceService? workspaceService = null,
        ILogger<TerminalPanelViewModel>? logger = null)
    {
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
        _workspaceService = workspaceService;
        _logger = logger;

        // Subscribe to service events
        _terminalService.SessionCreated += OnSessionCreated;
        _terminalService.SessionClosed += OnSessionClosed;
        _terminalService.SessionStateChanged += OnSessionStateChanged;
        _terminalService.TitleChanged += OnTitleChanged;

        // ─────────────────────────────────────────────────────────────────
        // Initialize Search ViewModel (v0.5.5c)
        // ─────────────────────────────────────────────────────────────────
        if (searchService != null)
        {
            _searchBarViewModel = new TerminalSearchBarViewModel(searchService);
            _logger?.LogDebug("[TerminalPanelViewModel] SearchBarViewModel initialized");
        }

        _logger?.LogDebug("[TerminalPanelViewModel] Instance created and subscribed to service events");
    }

    #endregion

    #region Commands - Session Management

    /// <summary>
    /// Creates a new terminal session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This command:
    /// <list type="number">
    ///   <item><description>Sets IsBusy during operation</description></item>
    ///   <item><description>Creates session with workspace directory if available</description></item>
    ///   <item><description>Shows the panel after creation</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Session is auto-activated via the SessionCreated event handler.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task NewSessionAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            _logger?.LogInformation("[TerminalPanelViewModel] Creating new terminal session");

            // Build options with workspace directory if available
            var options = new TerminalSessionOptions
            {
                WorkingDirectory = _workspaceService?.CurrentWorkspace?.RootPath,
                Columns = 80,
                Rows = 24
            };

            var session = await _terminalService.CreateSessionAsync(options);

            _logger?.LogInformation(
                "[TerminalPanelViewModel] Created terminal session {SessionId}, shell: {ShellPath}",
                session.Id, session.ShellPath);

            // Show panel when creating a session
            IsVisible = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[TerminalPanelViewModel] Failed to create terminal session");
            SetError($"Failed to create terminal: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Closes a specific session.
    /// </summary>
    /// <param name="session">The session to close.</param>
    /// <remarks>
    /// If this was the active session, an adjacent session is activated.
    /// </remarks>
    [RelayCommand]
    private async Task CloseSessionAsync(TerminalSessionViewModel? session)
    {
        if (session == null)
        {
            _logger?.LogDebug("[TerminalPanelViewModel] CloseSessionAsync called with null session");
            return;
        }

        try
        {
            _logger?.LogInformation(
                "[TerminalPanelViewModel] Closing terminal session {SessionId}",
                session.Id);

            await _terminalService.CloseSessionAsync(session.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[TerminalPanelViewModel] Failed to close terminal session {SessionId}",
                session.Id);
            SetError($"Failed to close terminal: {ex.Message}");
        }
    }

    /// <summary>
    /// Closes the active session.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasActiveSession))]
    private async Task CloseActiveSessionAsync()
    {
        if (ActiveSession != null)
        {
            _logger?.LogDebug(
                "[TerminalPanelViewModel] Closing active session {SessionId}",
                ActiveSession.Id);
            await CloseSessionAsync(ActiveSession);
        }
    }

    /// <summary>
    /// Activates a session (switches tabs).
    /// </summary>
    /// <param name="session">The session to activate.</param>
    /// <remarks>
    /// <para>
    /// Activation process:
    /// <list type="number">
    ///   <item><description>Deactivate current session (sets IsActive = false)</description></item>
    ///   <item><description>Activate new session (sets IsActive = true)</description></item>
    ///   <item><description>Update ActiveSession property</description></item>
    ///   <item><description>Sync with terminal service</description></item>
    ///   <item><description>Raise ActiveSessionChanged event</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void ActivateSession(TerminalSessionViewModel? session)
    {
        if (session == null || session == ActiveSession)
        {
            _logger?.LogDebug(
                "[TerminalPanelViewModel] ActivateSession skipped: session is null or already active");
            return;
        }

        _logger?.LogDebug(
            "[TerminalPanelViewModel] Activating session {SessionId} ({SessionName})",
            session.Id, session.Name);

        // Deactivate current session
        if (ActiveSession != null)
        {
            ActiveSession.IsActive = false;
            _logger?.LogDebug(
                "[TerminalPanelViewModel] Deactivated previous session {SessionId}",
                ActiveSession.Id);
        }

        // Activate new session
        session.IsActive = true;
        ActiveSession = session;

        // Sync with service (set the active session on the service)
        _terminalService.SetActiveSession(session.Id);

        // Raise event for view binding
        ActiveSessionChanged?.Invoke(this, session);

        _logger?.LogInformation(
            "[TerminalPanelViewModel] Session {SessionId} is now active",
            session.Id);
    }

    #endregion

    #region Commands - Panel Visibility

    /// <summary>
    /// Toggles panel visibility.
    /// </summary>
    [RelayCommand]
    private void TogglePanel()
    {
        if (IsVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    /// <summary>
    /// Shows the terminal panel.
    /// </summary>
    /// <remarks>
    /// Creates a new session if none exist.
    /// </remarks>
    [RelayCommand]
    private void ShowPanel()
    {
        _logger?.LogDebug("[TerminalPanelViewModel] Showing terminal panel");
        IsVisible = true;

        // Create a session if none exist
        if (Sessions.Count == 0)
        {
            _logger?.LogDebug("[TerminalPanelViewModel] No sessions exist, creating one");
            _ = NewSessionAsync();
        }
    }

    /// <summary>
    /// Hides the terminal panel.
    /// </summary>
    /// <remarks>
    /// Also resets maximize state when hiding.
    /// </remarks>
    [RelayCommand]
    private void HidePanel()
    {
        _logger?.LogDebug("[TerminalPanelViewModel] Hiding terminal panel");
        IsVisible = false;
        IsMaximized = false;
    }

    /// <summary>
    /// Toggles maximized state.
    /// </summary>
    [RelayCommand]
    private void ToggleMaximize()
    {
        IsMaximized = !IsMaximized;
        _logger?.LogDebug(
            "[TerminalPanelViewModel] Terminal panel maximized: {IsMaximized}",
            IsMaximized);
    }

    #endregion

    #region Commands - Terminal Operations

    /// <summary>
    /// Clears the terminal screen.
    /// </summary>
    /// <remarks>
    /// Sends the "clear" command to the active session.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(HasActiveSession))]
    private async Task ClearTerminalAsync()
    {
        if (ActiveSession == null)
            return;

        try
        {
            _logger?.LogDebug(
                "[TerminalPanelViewModel] Clearing terminal {SessionId}",
                ActiveSession.Id);

            await _terminalService.ExecuteCommandAsync(
                ActiveSession.Id,
                "clear");
        }
        catch (Exception ex)
        {
            // Ignore clear failures - not critical
            _logger?.LogWarning(
                ex,
                "[TerminalPanelViewModel] Failed to clear terminal {SessionId}",
                ActiveSession.Id);
        }
    }

    #endregion

    #region Commands - Search (v0.5.5c)

    /// <summary>
    /// Opens the terminal search bar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bound to Ctrl+F in the terminal panel.
    /// The search bar is positioned as a floating overlay.
    /// </para>
    /// <para>Added in v0.5.5c.</para>
    /// </remarks>
    [RelayCommand]
    private void OpenSearch()
    {
        if (_searchBarViewModel == null)
        {
            _logger?.LogDebug("[TerminalPanelViewModel] SearchBarViewModel not initialized");
            return;
        }

        // Update the search buffer to the current session's buffer
        if (ActiveSession != null)
        {
            var buffer = _terminalService.GetBuffer(ActiveSession.Id);
            if (buffer != null)
            {
                _searchBarViewModel.SetBuffer(buffer);
            }
        }

        _searchBarViewModel.OpenSearch();
        _logger?.LogDebug("[TerminalPanelViewModel] Opened search bar");
    }

    #endregion

    #region Commands - Tab Navigation

    /// <summary>
    /// Navigates to the next tab (circular).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Circular navigation: wraps from last tab to first.
    /// Formula: nextIndex = (currentIndex + 1) % Sessions.Count
    /// </para>
    /// <para>
    /// Early exit when Sessions.Count ≤ 1 (no tab to switch to).
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void NextTab()
    {
        if (Sessions.Count <= 1)
        {
            _logger?.LogDebug("[TerminalPanelViewModel] NextTab skipped: only {Count} session(s)", Sessions.Count);
            return;
        }

        var currentIndex = ActiveSession != null
            ? Sessions.IndexOf(ActiveSession)
            : -1;

        var nextIndex = (currentIndex + 1) % Sessions.Count;

        _logger?.LogDebug(
            "[TerminalPanelViewModel] NextTab: {CurrentIndex} → {NextIndex}",
            currentIndex, nextIndex);

        ActivateSession(Sessions[nextIndex]);
    }

    /// <summary>
    /// Navigates to the previous tab (circular).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Circular navigation: wraps from first tab to last.
    /// Formula: prevIndex = currentIndex > 0 ? currentIndex - 1 : Sessions.Count - 1
    /// </para>
    /// <para>
    /// Early exit when Sessions.Count ≤ 1 (no tab to switch to).
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void PreviousTab()
    {
        if (Sessions.Count <= 1)
        {
            _logger?.LogDebug("[TerminalPanelViewModel] PreviousTab skipped: only {Count} session(s)", Sessions.Count);
            return;
        }

        var currentIndex = ActiveSession != null
            ? Sessions.IndexOf(ActiveSession)
            : 0;

        var prevIndex = currentIndex > 0
            ? currentIndex - 1
            : Sessions.Count - 1;

        _logger?.LogDebug(
            "[TerminalPanelViewModel] PreviousTab: {CurrentIndex} → {PrevIndex}",
            currentIndex, prevIndex);

        ActivateSession(Sessions[prevIndex]);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the SessionCreated event from the terminal service.
    /// </summary>
    /// <remarks>
    /// Creates a ViewModel, adds to collection, and auto-activates.
    /// </remarks>
    private void OnSessionCreated(object? sender, TerminalSessionEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger?.LogDebug(
                "[TerminalPanelViewModel] SessionCreated event: {SessionId}",
                e.Session.Id);

            var viewModel = new TerminalSessionViewModel(e.Session);
            Sessions.Add(viewModel);

            // Auto-activate new sessions
            ActivateSession(viewModel);

            _logger?.LogInformation(
                "[TerminalPanelViewModel] Added and activated session {SessionId}, total sessions: {Count}",
                e.Session.Id, Sessions.Count);
        });
    }

    /// <summary>
    /// Handles the SessionClosed event from the terminal service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Close handling:
    /// <list type="number">
    ///   <item><description>Find session ViewModel by ID</description></item>
    ///   <item><description>Track if was active and index</description></item>
    ///   <item><description>Remove from collection</description></item>
    ///   <item><description>Activate adjacent session if was active</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private void OnSessionClosed(object? sender, TerminalSessionEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger?.LogDebug(
                "[TerminalPanelViewModel] SessionClosed event: {SessionId}",
                e.Session.Id);

            var session = Sessions.FirstOrDefault(s => s.Id == e.Session.Id);
            if (session == null)
            {
                _logger?.LogWarning(
                    "[TerminalPanelViewModel] Session {SessionId} not found in collection",
                    e.Session.Id);
                return;
            }

            // Track state before removal
            var wasActive = session.IsActive;
            var index = Sessions.IndexOf(session);

            // Remove from collection
            Sessions.Remove(session);

            _logger?.LogInformation(
                "[TerminalPanelViewModel] Removed session {SessionId}, wasActive: {WasActive}, remaining: {Count}",
                e.Session.Id, wasActive, Sessions.Count);

            // Handle activation of adjacent session
            if (wasActive && Sessions.Count > 0)
            {
                // Activate the session at the same index, or the last one
                var newIndex = Math.Min(index, Sessions.Count - 1);

                _logger?.LogDebug(
                    "[TerminalPanelViewModel] Activating adjacent session at index {Index}",
                    newIndex);

                ActivateSession(Sessions[newIndex]);
            }
            else if (Sessions.Count == 0)
            {
                _logger?.LogDebug("[TerminalPanelViewModel] No sessions remaining, clearing active session");
                ActiveSession = null;
                ActiveSessionChanged?.Invoke(this, null);
            }
        });
    }

    /// <summary>
    /// Handles the SessionStateChanged event from the terminal service.
    /// </summary>
    private void OnSessionStateChanged(object? sender, TerminalSessionStateEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger?.LogDebug(
                "[TerminalPanelViewModel] SessionStateChanged event: {SessionId}, {OldState} → {NewState}",
                e.SessionId, e.OldState, e.NewState);

            var session = Sessions.FirstOrDefault(s => s.Id == e.SessionId);
            session?.UpdateFromSession();
        });
    }

    /// <summary>
    /// Handles the TitleChanged event from the terminal service.
    /// </summary>
    private void OnTitleChanged(object? sender, TerminalTitleEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger?.LogDebug(
                "[TerminalPanelViewModel] TitleChanged event: {SessionId}, title: \"{Title}\"",
                e.SessionId, e.Title);

            var session = Sessions.FirstOrDefault(s => s.Id == e.SessionId);
            if (session != null)
            {
                session.Name = e.Title;
            }
        });
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Unsubscribes from service events.
    /// </summary>
    /// <remarks>
    /// Call this when disposing the ViewModel to prevent memory leaks.
    /// </remarks>
    public void Dispose()
    {
        _terminalService.SessionCreated -= OnSessionCreated;
        _terminalService.SessionClosed -= OnSessionClosed;
        _terminalService.SessionStateChanged -= OnSessionStateChanged;
        _terminalService.TitleChanged -= OnTitleChanged;

        _logger?.LogDebug("[TerminalPanelViewModel] Disposed and unsubscribed from service events");
    }

    #endregion
}
