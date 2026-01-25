// ============================================================================
// File: TerminalHistoryEntry.cs
// Path: src/AIntern.Core/Models/Terminal/TerminalHistoryEntry.cs
// Description: Immutable model representing a single entry in the terminal
//              command history with metadata about command execution.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// A single entry in the terminal command history.
/// Captures metadata about command execution for future reference.
/// </summary>
/// <remarks>
/// <para>
/// History entries are immutable records created when a command completes.
/// They store the command text along with contextual metadata like working
/// directory, exit code, and duration.
/// </para>
/// <para>
/// <b>Typical Usage:</b>
/// <code>
/// var entry = TerminalHistoryEntry.Create(
///     command: "git status",
///     workingDirectory: "/home/user/project",
///     exitCode: 0,
///     duration: TimeSpan.FromSeconds(0.5));
///
/// await historyService.AddCommandAsync(sessionId, entry);
/// </code>
/// </para>
/// <para>
/// <b>Display Helpers:</b>
/// The class provides computed properties for UI display:
/// <list type="bullet">
///   <item><see cref="IsSuccess"/> - True if exit code is 0</item>
///   <item><see cref="RelativeTime"/> - Human-readable time (e.g., "5m ago")</item>
///   <item><see cref="DurationDisplay"/> - Formatted duration (e.g., "1.2s")</item>
/// </list>
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class TerminalHistoryEntry
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Primary Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the unique identifier for this history entry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Automatically generated as a new GUID string when the entry is created.
    /// This ID is used for database persistence and correlation.
    /// </para>
    /// </remarks>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the command that was executed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The full command text as entered by the user or script.
    /// This may include arguments, flags, and redirections.
    /// </para>
    /// </remarks>
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Gets when the command was executed (UTC).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <see cref="DateTime.UtcNow"/> when the entry is created.
    /// All timestamps are stored in UTC to avoid timezone issues.
    /// </para>
    /// </remarks>
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════════
    // Context Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the working directory when the command was executed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The absolute path to the current working directory at the time of
    /// command execution. This is useful for context when reviewing history.
    /// </para>
    /// </remarks>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the exit code of the command, if captured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Standard convention: 0 indicates success, non-zero indicates failure.
    /// The specific meaning of non-zero codes varies by command.
    /// </para>
    /// <para>
    /// May be null if the exit code was not captured (e.g., for commands
    /// that are still running or were interrupted).
    /// </para>
    /// </remarks>
    public int? ExitCode { get; init; }

    /// <summary>
    /// Gets the duration of command execution, if captured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The elapsed time from command start to completion. May be null
    /// for commands where timing was not captured.
    /// </para>
    /// </remarks>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the ID of the shell profile used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// References the <c>ShellProfile.Id</c> that was active when the
    /// command was executed. Useful for filtering history by profile.
    /// </para>
    /// </remarks>
    public string? ProfileId { get; init; }

    // ═══════════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets whether the command executed successfully (exit code 0).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>true</c> only if <see cref="ExitCode"/> is exactly 0.
    /// Returns <c>false</c> for non-zero codes or if exit code is null.
    /// </para>
    /// </remarks>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Gets a display-friendly relative time string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Examples: "just now", "5m ago", "2h ago", "3d ago", "Jan 15, 2026"
    /// </para>
    /// <para>
    /// Time ranges:
    /// <list type="bullet">
    ///   <item>&lt; 60 seconds: "just now"</item>
    ///   <item>&lt; 60 minutes: "{n}m ago"</item>
    ///   <item>&lt; 24 hours: "{n}h ago"</item>
    ///   <item>&lt; 7 days: "{n}d ago"</item>
    ///   <item>&lt; 30 days: "{n}w ago"</item>
    ///   <item>Otherwise: "MMM d, yyyy" format</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string RelativeTime => FormatRelativeTime(ExecutedAt);

    /// <summary>
    /// Gets a display-friendly duration string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Examples: "150ms", "1.5s", "2.3m"
    /// Returns null if <see cref="Duration"/> is null.
    /// </para>
    /// <para>
    /// Format rules:
    /// <list type="bullet">
    ///   <item>&lt; 1 second: milliseconds (e.g., "150ms")</item>
    ///   <item>&lt; 1 minute: seconds with decimal (e.g., "1.5s")</item>
    ///   <item>Otherwise: minutes with decimal (e.g., "2.3m")</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? DurationDisplay => Duration.HasValue
        ? FormatDuration(Duration.Value)
        : null;

    // ═══════════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new history entry with the specified properties.
    /// </summary>
    /// <param name="command">The command that was executed.</param>
    /// <param name="workingDirectory">The working directory when executed.</param>
    /// <param name="exitCode">The exit code of the command.</param>
    /// <param name="duration">The duration of command execution.</param>
    /// <param name="profileId">The shell profile ID that was active.</param>
    /// <returns>A new <see cref="TerminalHistoryEntry"/> with the specified values.</returns>
    /// <remarks>
    /// <para>
    /// Factory method that generates a new unique ID and sets <see cref="ExecutedAt"/>
    /// to the current UTC time.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var entry = TerminalHistoryEntry.Create(
    ///     command: "npm install",
    ///     workingDirectory: "/home/user/app",
    ///     exitCode: 0,
    ///     duration: TimeSpan.FromSeconds(15.7));
    /// </code>
    /// </example>
    public static TerminalHistoryEntry Create(
        string command,
        string? workingDirectory = null,
        int? exitCode = null,
        TimeSpan? duration = null,
        string? profileId = null) => new()
    {
        Command = command,
        WorkingDirectory = workingDirectory,
        ExitCode = exitCode,
        Duration = duration,
        ProfileId = profileId
    };

    /// <summary>
    /// Creates an empty history entry.
    /// </summary>
    /// <returns>A new <see cref="TerminalHistoryEntry"/> with default values.</returns>
    /// <remarks>
    /// <para>
    /// Useful for initializing variables or representing a placeholder entry.
    /// </para>
    /// </remarks>
    public static TerminalHistoryEntry Empty => new();

    // ═══════════════════════════════════════════════════════════════════════════
    // Formatting Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Formats a DateTime as a relative time string.
    /// </summary>
    /// <param name="dateTime">The UTC datetime to format.</param>
    /// <returns>A human-readable relative time string.</returns>
    private static string FormatRelativeTime(DateTime dateTime)
    {
        var diff = DateTime.UtcNow - dateTime;

        // Handle future dates (shouldn't happen, but be defensive)
        if (diff.TotalSeconds < 0)
            return "just now";

        if (diff.TotalSeconds < 60)
            return "just now";

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";

        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)}w ago";

        return dateTime.ToString("MMM d, yyyy");
    }

    /// <summary>
    /// Formats a TimeSpan as a display-friendly duration string.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>A formatted duration string.</returns>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";

        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F1}s";

        return $"{duration.TotalMinutes:F1}m";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Object Overrides
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the command text as the string representation.
    /// </summary>
    /// <returns>The command text.</returns>
    public override string ToString() => Command;
}
