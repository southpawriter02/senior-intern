// -----------------------------------------------------------------------
// <copyright file="CommandBlockStatus.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Execution status of a command block.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4a.</para>
/// <para>
/// This enum tracks the lifecycle of a command from extraction through execution.
/// State transitions follow the pattern: Pending → (Copied|SentToTerminal) → Executing → (Executed|Failed|Cancelled).
/// </para>
/// </remarks>
public enum CommandBlockStatus
{
    /// <summary>
    /// Command has not been interacted with.
    /// </summary>
    /// <remarks>Initial state for all extracted commands.</remarks>
    Pending,

    /// <summary>
    /// Command was copied to clipboard.
    /// </summary>
    /// <remarks>User clicked the "Copy" button.</remarks>
    Copied,

    /// <summary>
    /// Command was sent to terminal input (awaiting Enter).
    /// </summary>
    /// <remarks>
    /// Command text has been inserted into the terminal but not yet executed.
    /// User must press Enter to run it.
    /// </remarks>
    SentToTerminal,

    /// <summary>
    /// Command is currently executing in the terminal.
    /// </summary>
    /// <remarks>
    /// The command has been submitted and is running. This state persists
    /// until the shell prompt returns or the command is cancelled.
    /// </remarks>
    Executing,

    /// <summary>
    /// Command executed successfully (exit code 0).
    /// </summary>
    /// <remarks>Terminal state - no further transitions expected.</remarks>
    Executed,

    /// <summary>
    /// Command execution failed (non-zero exit code).
    /// </summary>
    /// <remarks>Terminal state - no further transitions expected.</remarks>
    Failed,

    /// <summary>
    /// Command execution was cancelled by user.
    /// </summary>
    /// <remarks>
    /// User sent interrupt signal (Ctrl+C) or closed the terminal.
    /// Terminal state - no further transitions expected.
    /// </remarks>
    Cancelled
}

// ═══════════════════════════════════════════════════════════════════════════
// EXTENSION METHODS
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Extension methods for <see cref="CommandBlockStatus"/>.
/// </summary>
/// <remarks>Added in v0.5.4a.</remarks>
public static class CommandBlockStatusExtensions
{
    /// <summary>
    /// Checks if the status represents a terminal state (no further transitions expected).
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>
    /// True if status is <see cref="CommandBlockStatus.Executed"/>,
    /// <see cref="CommandBlockStatus.Failed"/>, or <see cref="CommandBlockStatus.Cancelled"/>.
    /// </returns>
    public static bool IsTerminal(this CommandBlockStatus status) =>
        status is CommandBlockStatus.Executed
               or CommandBlockStatus.Failed
               or CommandBlockStatus.Cancelled;

    /// <summary>
    /// Checks if the command has been executed or attempted to execute.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>
    /// True if the command entered the Executing state at some point.
    /// </returns>
    public static bool WasAttempted(this CommandBlockStatus status) =>
        status is CommandBlockStatus.Executing
               or CommandBlockStatus.Executed
               or CommandBlockStatus.Failed
               or CommandBlockStatus.Cancelled;

    /// <summary>
    /// Checks if the command is currently in progress.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if status is <see cref="CommandBlockStatus.Executing"/>.</returns>
    public static bool IsRunning(this CommandBlockStatus status) =>
        status == CommandBlockStatus.Executing;

    /// <summary>
    /// Checks if the command can still be executed.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the command has not yet entered a terminal state or started executing.</returns>
    public static bool CanExecute(this CommandBlockStatus status) =>
        status is CommandBlockStatus.Pending
               or CommandBlockStatus.Copied
               or CommandBlockStatus.SentToTerminal;

    /// <summary>
    /// Gets a user-friendly display string.
    /// </summary>
    /// <param name="status">The status to convert.</param>
    /// <returns>A human-readable status label.</returns>
    public static string ToDisplayString(this CommandBlockStatus status) => status switch
    {
        CommandBlockStatus.Pending => "Ready",
        CommandBlockStatus.Copied => "Copied",
        CommandBlockStatus.SentToTerminal => "In Terminal",
        CommandBlockStatus.Executing => "Running...",
        CommandBlockStatus.Executed => "Completed",
        CommandBlockStatus.Failed => "Failed",
        CommandBlockStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the appropriate icon name for this status.
    /// </summary>
    /// <param name="status">The status to get an icon for.</param>
    /// <returns>Name of the icon resource to display.</returns>
    /// <remarks>
    /// Icon names correspond to resources defined in IconPaths.axaml.
    /// </remarks>
    public static string ToIconName(this CommandBlockStatus status) => status switch
    {
        CommandBlockStatus.Pending => "PlayIcon",
        CommandBlockStatus.Copied => "ClipboardIcon",
        CommandBlockStatus.SentToTerminal => "TerminalIcon",
        CommandBlockStatus.Executing => "SpinnerIcon",
        CommandBlockStatus.Executed => "CheckIcon",
        CommandBlockStatus.Failed => "ErrorIcon",
        CommandBlockStatus.Cancelled => "CancelIcon",
        _ => "PlayIcon"
    };

    /// <summary>
    /// Gets a color key for this status.
    /// </summary>
    /// <param name="status">The status to get a color for.</param>
    /// <returns>
    /// A semantic color key: "success", "error", "warning", "info", or "neutral".
    /// </returns>
    public static string ToColorKey(this CommandBlockStatus status) => status switch
    {
        CommandBlockStatus.Executed => "success",
        CommandBlockStatus.Failed => "error",
        CommandBlockStatus.Cancelled => "warning",
        CommandBlockStatus.Executing => "info",
        _ => "neutral"
    };
}
