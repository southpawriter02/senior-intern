// -----------------------------------------------------------------------
// <copyright file="CommandStatusChangedEventArgs.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Event arguments for command execution status change notifications.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4c.</para>
/// <para>
/// This event is raised by <see cref="Core.Interfaces.ICommandExecutionService"/>
/// whenever a command's execution status changes. Subscribers can use this
/// to update UI elements or trigger follow-up actions.
/// </para>
/// <para>
/// <b>Common status transitions:</b>
/// <list type="bullet">
/// <item><description>Pending → Copied (clipboard copy)</description></item>
/// <item><description>Pending → SentToTerminal (send without execute)</description></item>
/// <item><description>Pending → Executing → Executed (successful run)</description></item>
/// <item><description>Pending → Executing → Failed (error during run)</description></item>
/// <item><description>Executing → Cancelled (user interrupt)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class CommandStatusChangedEventArgs : EventArgs
{
    // ═══════════════════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the ID of the command whose status changed.
    /// </summary>
    /// <remarks>
    /// This corresponds to <see cref="CommandBlock.Id"/> and can be used
    /// to look up the full command details.
    /// </remarks>
    public Guid CommandId { get; init; }

    /// <summary>
    /// Gets the previous status of the command before this change.
    /// </summary>
    public CommandBlockStatus OldStatus { get; init; }

    /// <summary>
    /// Gets the new status of the command after this change.
    /// </summary>
    public CommandBlockStatus NewStatus { get; init; }

    /// <summary>
    /// Gets the ID of the terminal session associated with this status change.
    /// </summary>
    /// <remarks>
    /// May be null for status changes that don't involve a terminal session,
    /// such as copying to clipboard.
    /// </remarks>
    public Guid? SessionId { get; init; }

    /// <summary>
    /// Gets the timestamp when this status change occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets whether this status change represents the start of execution.
    /// </summary>
    public bool IsExecutionStarted => NewStatus == CommandBlockStatus.Executing;

    /// <summary>
    /// Gets whether this status change represents completion (success, failure, or cancel).
    /// </summary>
    public bool IsExecutionCompleted => NewStatus.IsTerminal();

    /// <summary>
    /// Gets whether this status change represents a successful completion.
    /// </summary>
    public bool IsSuccess => NewStatus == CommandBlockStatus.Executed;

    /// <summary>
    /// Gets whether this status change represents a failure.
    /// </summary>
    public bool IsFailure => NewStatus == CommandBlockStatus.Failed;

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Command {CommandId.ToString("N")[..8]}: {OldStatus} → {NewStatus}";
    }
}
