// -----------------------------------------------------------------------
// <copyright file="ICommandExecutionService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Interfaces;

using AIntern.Core.Models.Terminal;

/// <summary>
/// Service for executing commands in terminal sessions with status tracking.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4c.</para>
/// <para>
/// This service provides the bridge between extracted commands (from
/// <see cref="ICommandExtractorService"/>) and terminal execution. It supports:
/// </para>
/// <list type="bullet">
/// <item><description>Copying commands to system clipboard</description></item>
/// <item><description>Sending commands to terminal without executing</description></item>
/// <item><description>Executing commands with Enter key</description></item>
/// <item><description>Sequential multi-command execution</description></item>
/// <item><description>Execution cancellation via SIGINT</description></item>
/// <item><description>Status tracking with event notifications</description></item>
/// <item><description>Session management with shell type preferences</description></item>
/// </list>
/// <para>
/// <b>Dependencies:</b>
/// <list type="bullet">
/// <item><description><see cref="ITerminalService"/> - Terminal session I/O</description></item>
/// <item><description><see cref="IShellProfileService"/> - Profile lookup</description></item>
/// <item><description>IClipboard (Avalonia) - System clipboard access</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ICommandExecutionService
{
    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Event raised when a command's execution status changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this event to receive notifications about command
    /// lifecycle changes. Common uses include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Updating UI to reflect current command state</description></item>
    /// <item><description>Triggering follow-up actions on completion</description></item>
    /// <item><description>Logging command execution flow</description></item>
    /// </list>
    /// <para>
    /// Event handlers should be lightweight to avoid blocking execution.
    /// </para>
    /// </remarks>
    event EventHandler<CommandStatusChangedEventArgs>? StatusChanged;

    // ═══════════════════════════════════════════════════════════════════════
    // CLIPBOARD OPERATIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Copy command text to the system clipboard.
    /// </summary>
    /// <param name="command">The command to copy.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// After copying, the command's status is updated to
    /// <see cref="CommandBlockStatus.Copied"/> and a <see cref="StatusChanged"/>
    /// event is raised.
    /// </para>
    /// <para>
    /// The user can then paste the command into any application (terminal,
    /// text editor, etc.).
    /// </para>
    /// </remarks>
    Task CopyToClipboardAsync(CommandBlock command, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // TERMINAL OPERATIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Send command text to terminal without executing (user must press Enter).
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="targetSessionId">
    /// Specific session to target. If null, uses active session or creates new one.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// The command text is written to the terminal input stream, but no
    /// newline/Enter key is sent. This allows the user to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Review the command before executing</description></item>
    /// <item><description>Modify the command if needed</description></item>
    /// <item><description>Decide not to run it at all</description></item>
    /// </list>
    /// <para>
    /// Status is updated to <see cref="CommandBlockStatus.SentToTerminal"/>.
    /// </para>
    /// </remarks>
    Task SendToTerminalAsync(
        CommandBlock command,
        Guid? targetSessionId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Execute command in terminal (sends command + Enter key).
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="targetSessionId">
    /// Specific session to target. If null, uses active session or creates new one.
    /// </param>
    /// <param name="captureOutput">
    /// Whether to capture command output. Requires output capture service (v0.5.4d).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Execution result with status and optional captured output.</returns>
    /// <remarks>
    /// <para>
    /// This is the primary execution method. It:
    /// </para>
    /// <list type="number">
    /// <item><description>Ensures a terminal session exists</description></item>
    /// <item><description>Updates status to Executing</description></item>
    /// <item><description>Sends command with newline to terminal</description></item>
    /// <item><description>Optionally captures output</description></item>
    /// <item><description>Updates status to Executed/Failed/Cancelled</description></item>
    /// </list>
    /// <para>
    /// The method returns after the command has been sent. For long-running
    /// commands, use <see cref="CancelExecutionAsync"/> to interrupt.
    /// </para>
    /// </remarks>
    Task<CommandExecutionResult> ExecuteAsync(
        CommandBlock command,
        Guid? targetSessionId = null,
        bool captureOutput = false,
        CancellationToken ct = default);

    /// <summary>
    /// Execute multiple commands sequentially in the same terminal session.
    /// </summary>
    /// <param name="commands">Commands to execute in order.</param>
    /// <param name="targetSessionId">
    /// Specific session to target. If null, creates/uses appropriate session.
    /// </param>
    /// <param name="stopOnError">
    /// If true, stop executing remaining commands when one fails.
    /// If false, continue with next command even after failure.
    /// </param>
    /// <param name="captureOutput">Whether to capture output from each command.</param>
    /// <param name="ct">Cancellation token to cancel all remaining commands.</param>
    /// <returns>Results for each command that was executed (may be fewer than input if stopped).</returns>
    /// <remarks>
    /// <para>
    /// Commands are executed sequentially with a small delay (100ms) between
    /// them to allow the terminal to stabilize. All commands use the same
    /// session to maintain context (environment variables, working directory).
    /// </para>
    /// <para>
    /// If <paramref name="stopOnError"/> is true and a command fails, the
    /// returned list will contain results only for commands up to and including
    /// the failed one. Remaining commands are not executed.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<CommandExecutionResult>> ExecuteAllAsync(
        IEnumerable<CommandBlock> commands,
        Guid? targetSessionId = null,
        bool stopOnError = true,
        bool captureOutput = false,
        CancellationToken ct = default);

    /// <summary>
    /// Cancel a running command by sending Ctrl+C (SIGINT) to the terminal.
    /// </summary>
    /// <param name="sessionId">The session to send the interrupt signal to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// This sends an interrupt signal (SIGINT on Unix, Ctrl+C on Windows)
    /// to the terminal session. Most shell commands will terminate when
    /// receiving this signal, but some may require additional handling.
    /// </para>
    /// <para>
    /// The command's status will be updated to
    /// <see cref="CommandBlockStatus.Cancelled"/> if it was executing.
    /// </para>
    /// </remarks>
    Task CancelExecutionAsync(Guid sessionId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // STATUS OPERATIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get the current execution status of a command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <returns>
    /// Current status, or <see cref="CommandBlockStatus.Pending"/> if unknown.
    /// </returns>
    /// <remarks>
    /// Status is tracked in memory and not persisted across application restarts.
    /// </remarks>
    CommandBlockStatus GetStatus(Guid commandId);

    // ═══════════════════════════════════════════════════════════════════════
    // SESSION MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ensure a terminal session exists, creating one if needed.
    /// </summary>
    /// <param name="preferredShell">
    /// Preferred shell type. If null, any shell is acceptable.
    /// </param>
    /// <param name="workingDirectory">
    /// Working directory for new sessions. If null, uses profile default.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Session ID of existing or newly created session.</returns>
    /// <remarks>
    /// <para>
    /// Session reuse logic:
    /// </para>
    /// <list type="number">
    /// <item><description>If active session exists and shell type matches (or no preference), reuse it</description></item>
    /// <item><description>Otherwise, find a profile for the preferred shell type</description></item>
    /// <item><description>If no matching profile, use default profile</description></item>
    /// <item><description>Create new session with the selected profile</description></item>
    /// </list>
    /// </remarks>
    Task<Guid> EnsureTerminalSessionAsync(
        ShellType? preferredShell = null,
        string? workingDirectory = null,
        CancellationToken ct = default);
}
