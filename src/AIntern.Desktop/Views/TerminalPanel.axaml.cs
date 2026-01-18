namespace AIntern.Desktop.Views;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalPanel (v0.5.2e)                                                      │
// │ Terminal panel view with session tabs and content area.                      │
// │ Manages session attachment to the TerminalControl based on active session.   │
// └─────────────────────────────────────────────────────────────────────────────┘

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AIntern.Core.Interfaces;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

#region Type Documentation

/// <summary>
/// Terminal panel view with tabs and session management.
/// </summary>
/// <remarks>
/// <para>
/// The TerminalPanel is a composite view that integrates:
/// <list type="bullet">
///   <item><description>Tab bar with scrollable session tabs (from v0.5.2d ViewModels)</description></item>
///   <item><description>Panel action buttons (maximize, hide)</description></item>
///   <item><description>TerminalControl for rendering (from v0.5.2c)</description></item>
///   <item><description>Empty state display when no sessions exist</description></item>
/// </list>
/// </para>
/// <para>
/// Session Attachment Flow:
/// <code>
/// 1. Parent window calls Initialize(ITerminalService)
///    └── Stores service reference
/// 
/// 2. OnLoaded() fires
///    ├── Gets ViewModel from DataContext
///    └── Subscribes to ActiveSessionChanged event
/// 
/// 3. When ActiveSessionChanged fires:
///    ├── session != null → AttachToSessionAsync(session)
///    │   ├── TerminalView.AttachSessionAsync(service, id)
///    │   └── TerminalView.Focus()
///    └── session == null → DetachFromSession()
///        └── TerminalView.DetachSession()
/// 
/// 4. OnUnloaded() fires
///    └── Unsubscribes from ActiveSessionChanged
/// </code>
/// </para>
/// <para>Added in v0.5.2e.</para>
/// </remarks>

#endregion

public partial class TerminalPanel : UserControl
{
    #region Private Fields

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    private readonly ILogger<TerminalPanel>? _logger;

    /// <summary>
    /// The terminal service for session management.
    /// Set via <see cref="Initialize(ITerminalService)"/>.
    /// </summary>
    private ITerminalService? _terminalService;

    /// <summary>
    /// The TerminalPanelViewModel from DataContext.
    /// Captured in <see cref="OnLoaded"/> for event subscription.
    /// </summary>
    private TerminalPanelViewModel? _viewModel;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new TerminalPanel instance.
    /// </summary>
    /// <remarks>
    /// Default constructor required for XAML instantiation.
    /// Call <see cref="Initialize(ITerminalService)"/> after construction
    /// to enable session attachment.
    /// </remarks>
    public TerminalPanel()
    {
        InitializeComponent();
        _logger?.LogDebug("[TerminalPanel] Instance created");
    }

    /// <summary>
    /// Creates a new TerminalPanel instance with logging.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <remarks>
    /// Constructor for dependency injection scenarios.
    /// </remarks>
    public TerminalPanel(ILogger<TerminalPanel>? logger) : this()
    {
        _logger = logger;
        _logger?.LogDebug("[TerminalPanel] Instance created with logger");
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Called when the control is loaded into the visual tree.
    /// </summary>
    /// <param name="e">The routed event arguments.</param>
    /// <remarks>
    /// <para>
    /// Initialization steps:
    /// <list type="number">
    ///   <item><description>Capture ViewModel from DataContext</description></item>
    ///   <item><description>Subscribe to ActiveSessionChanged event</description></item>
    ///   <item><description>Attach to current active session (if any)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _logger?.LogDebug("[TerminalPanel] OnLoaded - capturing ViewModel and subscribing to events");

        // Capture ViewModel from DataContext
        _viewModel = DataContext as TerminalPanelViewModel;

        if (_viewModel != null)
        {
            // Subscribe to session changes
            _viewModel.ActiveSessionChanged += OnActiveSessionChanged;
            _logger?.LogDebug("[TerminalPanel] Subscribed to ActiveSessionChanged");

            // Attach to current active session if one exists and service is ready
            if (_viewModel.ActiveSession != null && _terminalService != null)
            {
                _logger?.LogDebug(
                    "[TerminalPanel] Found existing active session {SessionId}, attaching",
                    _viewModel.ActiveSession.Id);
                _ = AttachToSessionAsync(_viewModel.ActiveSession);
            }
        }
        else
        {
            _logger?.LogWarning("[TerminalPanel] DataContext is not TerminalPanelViewModel");
        }
    }

    /// <summary>
    /// Called when the control is removed from the visual tree.
    /// </summary>
    /// <param name="e">The routed event arguments.</param>
    /// <remarks>
    /// Unsubscribes from ViewModel events to prevent memory leaks.
    /// </remarks>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        _logger?.LogDebug("[TerminalPanel] OnUnloaded - unsubscribing from events");

        if (_viewModel != null)
        {
            _viewModel.ActiveSessionChanged -= OnActiveSessionChanged;
            _logger?.LogDebug("[TerminalPanel] Unsubscribed from ActiveSessionChanged");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the panel with the terminal service.
    /// </summary>
    /// <param name="terminalService">The terminal service for session management.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="terminalService"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Must be called by the parent window before sessions can be attached.
    /// Typically called in the parent's OnLoaded handler:
    /// <code>
    /// protected override void OnLoaded(RoutedEventArgs e)
    /// {
    ///     base.OnLoaded(e);
    ///     var service = App.Services.GetRequiredService&lt;ITerminalService&gt;();
    ///     TerminalPanel.Initialize(service);
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// If an active session already exists when Initialize is called,
    /// the panel will automatically attach to it.
    /// </para>
    /// </remarks>
    public void Initialize(ITerminalService terminalService)
    {
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
        _logger?.LogInformation("[TerminalPanel] Initialized with terminal service");

        // If we already have a ViewModel with an active session, attach now
        if (_viewModel?.ActiveSession != null)
        {
            _logger?.LogDebug(
                "[TerminalPanel] ViewModel has active session {SessionId}, attaching",
                _viewModel.ActiveSession.Id);
            _ = AttachToSessionAsync(_viewModel.ActiveSession);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the ActiveSessionChanged event from the ViewModel.
    /// </summary>
    /// <param name="sender">The event sender (TerminalPanelViewModel).</param>
    /// <param name="session">The new active session, or null if all sessions are closed.</param>
    /// <remarks>
    /// <para>
    /// This handler is the key bridge between the ViewModel layer and the View layer.
    /// When the active session changes:
    /// <list type="bullet">
    ///   <item><description>If session is not null: Attach to the new session and focus terminal</description></item>
    ///   <item><description>If session is null: Detach from current session (shows empty state)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private async void OnActiveSessionChanged(object? sender, TerminalSessionViewModel? session)
    {
        _logger?.LogDebug(
            "[TerminalPanel] ActiveSessionChanged event - session: {SessionId}",
            session?.Id.ToString() ?? "null");

        // Guard: Service must be initialized
        if (_terminalService == null)
        {
            _logger?.LogWarning(
                "[TerminalPanel] Terminal service not initialized, cannot attach session");
            return;
        }

        if (session != null)
        {
            await AttachToSessionAsync(session);
        }
        else
        {
            DetachFromSession();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Attaches to a terminal session.
    /// </summary>
    /// <param name="session">The session to attach to.</param>
    /// <remarks>
    /// <para>
    /// Attachment process:
    /// <list type="number">
    ///   <item><description>Call TerminalControl.AttachSessionAsync with service and session ID</description></item>
    ///   <item><description>Move keyboard focus to the terminal</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Errors are logged but not propagated - the UI continues to function
    /// and the user can retry by clicking the tab again.
    /// </para>
    /// </remarks>
    private async Task AttachToSessionAsync(TerminalSessionViewModel session)
    {
        try
        {
            _logger?.LogDebug(
                "[TerminalPanel] Attaching to session {SessionId} ({SessionName})",
                session.Id, session.Name);

            // Attach the terminal control to the session
            await TerminalView.AttachSessionAsync(_terminalService!, session.Id);

            // Focus the terminal to receive keyboard input
            TerminalView.Focus();

            _logger?.LogInformation(
                "[TerminalPanel] Session {SessionId} attached and focused",
                session.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[TerminalPanel] Failed to attach to session {SessionId}",
                session.Id);
        }
    }

    /// <summary>
    /// Detaches from the current session.
    /// </summary>
    /// <remarks>
    /// Called when all sessions are closed (ActiveSession becomes null).
    /// The TerminalControl will display the last buffer state until hidden
    /// by the empty state overlay.
    /// </remarks>
    private void DetachFromSession()
    {
        _logger?.LogDebug("[TerminalPanel] Detaching from current session");
        TerminalView.DetachSession();
        _logger?.LogInformation("[TerminalPanel] Session detached, empty state visible");
    }

    #endregion
}
