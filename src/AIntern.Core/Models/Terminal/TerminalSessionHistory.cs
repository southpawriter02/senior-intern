// ============================================================================
// File: TerminalSessionHistory.cs
// Path: src/AIntern.Core/Models/Terminal/TerminalSessionHistory.cs
// Description: Model representing session-level grouping for terminal command
//              history, aggregating commands executed within a single session.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Session-level history grouping for terminal commands.
/// Groups commands by terminal session for organized browsing.
/// </summary>
/// <remarks>
/// <para>
/// A session history record aggregates all commands executed within a single
/// terminal session, providing context about when the session was active and
/// what profile was used.
/// </para>
/// <para>
/// <b>Session Lifecycle:</b>
/// <list type="number">
///   <item>Session starts when a terminal is opened (StartedAt is set)</item>
///   <item>Commands are added as they execute (Commands list grows)</item>
///   <item>Session ends when terminal is closed (EndedAt is set)</item>
/// </list>
/// </para>
/// <para>
/// <b>Typical Usage:</b>
/// <code>
/// var sessions = await historyService.GetSessionRecordsAsync(count: 10);
/// foreach (var session in sessions)
/// {
///     Console.WriteLine($"Session {session.SessionId}: {session.CommandCount} commands, {session.DurationDisplay}");
///     foreach (var cmd in session.Commands)
///     {
///         Console.WriteLine($"  {cmd.RelativeTime}: {cmd.Command}");
///     }
/// }
/// </code>
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class TerminalSessionHistory
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Session Identification
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the session ID this history belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Corresponds to the <c>TerminalSession.Id</c> from the terminal subsystem.
    /// This is typically a GUID string generated when the session starts.
    /// </para>
    /// </remarks>
    public string SessionId { get; init; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    // Timing Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets when the session was started (UTC).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <see cref="DateTime.UtcNow"/>. For sessions loaded from
    /// history, this is derived from the earliest command's ExecutedAt.
    /// </para>
    /// </remarks>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the session was ended (UTC), or null if still active.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set when the terminal session is closed. A null value indicates the
    /// session is still active. For sessions loaded from history, this is
    /// derived from the latest command's ExecutedAt.
    /// </para>
    /// </remarks>
    public DateTime? EndedAt { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    // Profile Information
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the ID of the shell profile used for this session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// References the <c>ShellProfile.Id</c> that was used to start the session.
    /// May be null for sessions where profile tracking was not available.
    /// </para>
    /// </remarks>
    public string? ProfileId { get; init; }

    /// <summary>
    /// Gets the display name of the shell profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Human-readable name for the profile (e.g., "Bash", "Zsh", "PowerShell").
    /// Used for UI display. May be null if profile information is not available.
    /// </para>
    /// </remarks>
    public string? ProfileName { get; init; }

    // ═══════════════════════════════════════════════════════════════════════════
    // Command History
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the commands executed in this session, in chronological order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Commands are ordered from oldest to newest (ascending ExecutedAt).
    /// The list may be empty if no commands were executed in the session.
    /// </para>
    /// </remarks>
    public List<TerminalHistoryEntry> Commands { get; init; } = new();

    // ═══════════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the number of commands executed in this session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shorthand for <c>Commands.Count</c>.
    /// </para>
    /// </remarks>
    public int CommandCount => Commands.Count;

    /// <summary>
    /// Gets the total duration of the session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calculated as <c>EndedAt - StartedAt</c>. For active sessions
    /// (where EndedAt is null), calculates duration up to the current time.
    /// </para>
    /// </remarks>
    public TimeSpan Duration => (EndedAt ?? DateTime.UtcNow) - StartedAt;

    /// <summary>
    /// Gets whether the session is still active.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>true</c> if <see cref="EndedAt"/> is null, indicating the
    /// terminal session has not been closed.
    /// </para>
    /// </remarks>
    public bool IsActive => EndedAt == null;

    /// <summary>
    /// Gets a display-friendly session duration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Format rules:
    /// <list type="bullet">
    ///   <item>&lt; 1 minute: seconds (e.g., "45s")</item>
    ///   <item>&lt; 1 hour: minutes (e.g., "15m")</item>
    ///   <item>Otherwise: hours with decimal (e.g., "2.5h")</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string DurationDisplay
    {
        get
        {
            var d = Duration;

            if (d.TotalMinutes < 1)
                return $"{d.TotalSeconds:F0}s";

            if (d.TotalHours < 1)
                return $"{d.TotalMinutes:F0}m";

            return $"{d.TotalHours:F1}h";
        }
    }

    /// <summary>
    /// Gets a display-friendly start time string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Format: "MMM d, yyyy h:mm tt" (e.g., "Jan 19, 2026 2:30 PM")
    /// </para>
    /// </remarks>
    public string StartedAtDisplay => StartedAt.ToLocalTime().ToString("MMM d, yyyy h:mm tt");

    /// <summary>
    /// Gets the number of successful commands in this session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Counts commands where <see cref="TerminalHistoryEntry.IsSuccess"/> is true.
    /// </para>
    /// </remarks>
    public int SuccessfulCommandCount => Commands.Count(c => c.IsSuccess);

    /// <summary>
    /// Gets the number of failed commands in this session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Counts commands where exit code is non-zero (not null and not 0).
    /// </para>
    /// </remarks>
    public int FailedCommandCount => Commands.Count(c => c.ExitCode.HasValue && c.ExitCode != 0);

    // ═══════════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new session history with the specified session ID.
    /// </summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <param name="profileId">Optional profile ID.</param>
    /// <param name="profileName">Optional profile display name.</param>
    /// <returns>A new <see cref="TerminalSessionHistory"/> for the session.</returns>
    public static TerminalSessionHistory Create(
        string sessionId,
        string? profileId = null,
        string? profileName = null) => new()
    {
        SessionId = sessionId,
        ProfileId = profileId,
        ProfileName = profileName
    };

    /// <summary>
    /// Creates an empty session history.
    /// </summary>
    /// <returns>A new <see cref="TerminalSessionHistory"/> with default values.</returns>
    public static TerminalSessionHistory Empty => new();

    // ═══════════════════════════════════════════════════════════════════════════
    // Object Overrides
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a summary string for this session.
    /// </summary>
    /// <returns>A string in format "Session {id}: {count} commands".</returns>
    public override string ToString() => $"Session {SessionId}: {CommandCount} commands";
}
