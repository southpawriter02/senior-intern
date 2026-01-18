// -----------------------------------------------------------------------
// <copyright file="CommandExecutionResult.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Result of executing a command in a terminal session.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4c.</para>
/// <para>
/// This model captures the outcome of a command execution attempt, including:
/// </para>
/// <list type="bullet">
/// <item><description>Final status (Executed, Failed, Cancelled)</description></item>
/// <item><description>Timing information (when, how long)</description></item>
/// <item><description>Session context (which terminal was used)</description></item>
/// <item><description>Optional captured output (if capture was requested)</description></item>
/// <item><description>Error details (if execution failed)</description></item>
/// </list>
/// <para>
/// This is returned by <see cref="Core.Interfaces.ICommandExecutionService.ExecuteAsync"/>
/// and related methods.
/// </para>
/// </remarks>
public sealed class CommandExecutionResult
{
    // ═══════════════════════════════════════════════════════════════════════
    // IDENTITY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the ID of the command that was executed.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="CommandBlock.Id"/>.
    /// </remarks>
    public Guid CommandId { get; init; }

    /// <summary>
    /// Gets the final execution status of the command.
    /// </summary>
    /// <remarks>
    /// Terminal states: Executed (success), Failed (error), Cancelled (interrupted).
    /// </remarks>
    public CommandBlockStatus Status { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // SESSION CONTEXT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the ID of the terminal session where the command was executed.
    /// </summary>
    /// <remarks>
    /// Can be used to reuse the same session for subsequent commands.
    /// </remarks>
    public Guid SessionId { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // TIMING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the timestamp when execution started.
    /// </summary>
    public DateTime ExecutedAt { get; init; }

    /// <summary>
    /// Gets the duration of execution.
    /// </summary>
    /// <remarks>
    /// May be null if execution was interrupted before completion tracking.
    /// </remarks>
    public TimeSpan? Duration { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // OUTPUT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the captured terminal output, if capture was requested.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Will be null if:
    /// </para>
    /// <list type="bullet">
    /// <item><description>captureOutput was false in ExecuteAsync</description></item>
    /// <item><description>Output capture service is not available</description></item>
    /// <item><description>Capture failed for some reason</description></item>
    /// </list>
    /// <para>
    /// Use <see cref="HasOutput"/> to check before accessing.
    /// </para>
    /// </remarks>
    public TerminalOutputCapture? OutputCapture { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // ERROR INFORMATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the error message if execution failed.
    /// </summary>
    /// <remarks>
    /// Populated when <see cref="Status"/> is <see cref="CommandBlockStatus.Failed"/>.
    /// Contains exception message or other error details.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets whether execution completed successfully.
    /// </summary>
    public bool IsSuccess => Status == CommandBlockStatus.Executed;

    /// <summary>
    /// Gets whether execution failed.
    /// </summary>
    public bool IsFailed => Status == CommandBlockStatus.Failed;

    /// <summary>
    /// Gets whether execution was cancelled.
    /// </summary>
    public bool IsCancelled => Status == CommandBlockStatus.Cancelled;

    /// <summary>
    /// Gets whether output was captured.
    /// </summary>
    public bool HasOutput => OutputCapture != null;

    /// <summary>
    /// Gets whether there is an error message.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public override string ToString()
    {
        var durationStr = Duration.HasValue
            ? $", {Duration.Value.TotalMilliseconds:F0}ms"
            : "";

        return $"CommandExecutionResult({CommandId.ToString("N")[..8]}: {Status}{durationStr})";
    }
}
