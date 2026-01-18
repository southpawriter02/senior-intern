namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalSessionViewModel (v0.5.2d)                                           │
// │ ViewModel for a single terminal session (tab).                               │
// │ Wraps TerminalSession and provides observable properties for UI binding.     │
// └─────────────────────────────────────────────────────────────────────────────┘

using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;

#region Type Documentation

/// <summary>
/// ViewModel for a single terminal session (tab).
/// Wraps a <see cref="TerminalSession"/> and provides observable properties for UI binding.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel represents a single tab in the terminal panel. It exposes:
/// <list type="bullet">
///   <item><description>Identity: <see cref="Id"/>, <see cref="ShellType"/></description></item>
///   <item><description>Display: <see cref="Name"/>, <see cref="IsActive"/></description></item>
///   <item><description>State: <see cref="State"/>, <see cref="HasExited"/>, <see cref="ExitCode"/></description></item>
///   <item><description>Context: <see cref="WorkingDirectory"/></description></item>
/// </list>
/// </para>
/// <para>
/// Shell Type Detection:
/// <code>
/// Executable Name    → Shell Type
/// ─────────────────────────────────
/// bash               → "bash"
/// zsh                → "zsh"
/// fish               → "fish"
/// pwsh, powershell   → "powershell"
/// cmd                → "cmd"
/// nu                 → "nushell"
/// (any other)        → "terminal"
/// </code>
/// </para>
/// <para>Added in v0.5.2d.</para>
/// </remarks>

#endregion

public partial class TerminalSessionViewModel : ViewModelBase
{
    #region Private Fields

    /// <summary>
    /// The underlying terminal session model.
    /// </summary>
    private readonly TerminalSession _session;

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    private readonly ILogger<TerminalSessionViewModel>? _logger;

    #endregion

    #region Computed Properties (from model)

    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    /// <remarks>
    /// This ID is used to reference the session across service calls.
    /// </remarks>
    public Guid Id => _session.Id;

    /// <summary>
    /// Gets the shell type for icon display and styling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Normalized shell type string computed once from the shell path.
    /// Used by the view to display appropriate shell icons in tabs.
    /// </para>
    /// <para>
    /// Possible values: "bash", "zsh", "fish", "powershell", "cmd", "nushell", "terminal".
    /// </para>
    /// </remarks>
    public string ShellType { get; }

    /// <summary>
    /// Gets whether the session has exited (normally or with error).
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> when <see cref="State"/> is either
    /// <see cref="TerminalSessionState.Exited"/> or <see cref="TerminalSessionState.Error"/>.
    /// </remarks>
    public bool HasExited =>
        State == TerminalSessionState.Exited ||
        State == TerminalSessionState.Error;

    /// <summary>
    /// Gets the exit code if the session has ended, otherwise null.
    /// </summary>
    /// <remarks>
    /// Exit code 0 typically indicates successful completion.
    /// Non-zero values indicate various error conditions.
    /// </remarks>
    public int? ExitCode => _session.ExitCode;

    /// <summary>
    /// Gets the underlying terminal session model.
    /// </summary>
    /// <remarks>
    /// Used by the view layer for direct service operations.
    /// </remarks>
    public TerminalSession Session => _session;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets the display name for the tab.
    /// </summary>
    /// <remarks>
    /// Updated when the session title changes via OSC escape sequences.
    /// Falls back to the session Name if no title is set.
    /// </remarks>
    [ObservableProperty]
    private string _name;

    /// <summary>
    /// Gets or sets whether this session is the currently active/selected tab.
    /// </summary>
    /// <remarks>
    /// Only one session can be active at a time. When activated,
    /// the terminal control attaches to this session for input.
    /// </remarks>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// Gets or sets the current session state.
    /// </summary>
    /// <remarks>
    /// State transitions: Starting → Running → (Exited | Error) → Closing.
    /// Changing state notifies <see cref="HasExited"/> property.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExited))]
    private TerminalSessionState _state;

    /// <summary>
    /// Gets or sets the current working directory.
    /// </summary>
    /// <remarks>
    /// Updated when the shell changes directory (via OSC 7 escape sequence).
    /// Can be used for file explorer synchronization.
    /// </remarks>
    [ObservableProperty]
    private string _workingDirectory;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new <see cref="TerminalSessionViewModel"/> wrapping the specified session.
    /// </summary>
    /// <param name="session">The underlying terminal session.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="session"/> is null.</exception>
    public TerminalSessionViewModel(
        TerminalSession session,
        ILogger<TerminalSessionViewModel>? logger = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger;

        // Initialize observable properties from session
        _name = session.Title ?? session.Name;
        _state = session.State;
        _workingDirectory = session.WorkingDirectory;

        // Compute shell type once (doesn't change during session lifetime)
        ShellType = GetShellType(session.ShellPath);

        _logger?.LogDebug(
            "[TerminalSessionViewModel] Created for session {SessionId}, shell type: {ShellType}",
            session.Id, ShellType);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Updates observable properties from the underlying session model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method when the session state or title changes to sync
    /// the ViewModel with the current model state. This is typically
    /// invoked from service event handlers.
    /// </para>
    /// <para>
    /// Properties updated:
    /// <list type="bullet">
    ///   <item><description><see cref="Name"/> from Title or Name</description></item>
    ///   <item><description><see cref="State"/> from session State</description></item>
    ///   <item><description><see cref="WorkingDirectory"/> from session WorkingDirectory</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public void UpdateFromSession()
    {
        var previousState = State;

        Name = _session.Title ?? _session.Name;
        State = _session.State;
        WorkingDirectory = _session.WorkingDirectory;

        if (previousState != State)
        {
            _logger?.LogDebug(
                "[TerminalSessionViewModel] Session {SessionId} state changed: {OldState} → {NewState}",
                _session.Id, previousState, State);
        }
    }

    #endregion

    #region Shell Type Detection

    /// <summary>
    /// Determines the shell type from the shell executable path.
    /// </summary>
    /// <param name="shellPath">Full path to the shell executable.</param>
    /// <returns>A normalized shell type string for icon/styling.</returns>
    /// <remarks>
    /// <para>
    /// Shell type detection process:
    /// <list type="number">
    ///   <item><description>Extract filename from path (Path.GetFileNameWithoutExtension)</description></item>
    ///   <item><description>Convert to lowercase for case-insensitive matching</description></item>
    ///   <item><description>Match against known shell names</description></item>
    ///   <item><description>Return "terminal" as fallback for unknown shells</description></item>
    /// </list>
    /// </para>
    /// <para>This method is internal for testing purposes.</para>
    /// </remarks>
    internal static string GetShellType(string? shellPath)
    {
        // Handle null/empty path
        if (string.IsNullOrEmpty(shellPath))
            return "terminal";

        // Normalize path separators for cross-platform compatibility
        // Replace Windows backslashes with forward slashes before extracting filename
        var normalizedPath = shellPath.Replace('\\', '/');
        
        // Extract filename without extension and convert to lowercase
        var fileName = Path.GetFileNameWithoutExtension(normalizedPath).ToLowerInvariant();

        // Match against known shells
        return fileName switch
        {
            "bash" => "bash",
            "zsh" => "zsh",
            "fish" => "fish",
            "pwsh" or "powershell" => "powershell",
            "cmd" => "cmd",
            "nu" => "nushell",
            _ => "terminal"
        };
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a string representation of the session ViewModel.
    /// </summary>
    /// <returns>A string in the format "TerminalSessionVM(ID, Name, State)".</returns>
    public override string ToString() =>
        $"TerminalSessionVM({Id.ToString()[..8]}, {Name}, {State})";

    #endregion
}
