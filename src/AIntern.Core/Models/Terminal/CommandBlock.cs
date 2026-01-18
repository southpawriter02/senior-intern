// -----------------------------------------------------------------------
// <copyright file="CommandBlock.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

using System.Text.Json.Serialization;
using AIntern.Core.Interfaces;

/// <summary>
/// Represents an executable command extracted from an AI response.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4a.</para>
/// <para>
/// Command blocks are extracted from fenced code blocks in AI responses
/// and can be executed in the integrated terminal with one click.
/// This class tracks the command text, its source location, detection
/// metadata, and execution status.
/// </para>
/// </remarks>
public sealed class CommandBlock
{
    // ═══════════════════════════════════════════════════════════════════════
    // IDENTITY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the unique identifier for this command block.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the ID of the message containing this command.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// Gets the position of this command within the message (0-based).
    /// </summary>
    /// <remarks>
    /// When a message contains multiple commands, this index indicates
    /// the order in which they appear.
    /// </remarks>
    public int SequenceNumber { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // CONTENT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the command text to execute.
    /// </summary>
    /// <remarks>
    /// May contain multiple lines for multi-line commands or scripts.
    /// </remarks>
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Gets the language/shell identifier from the code fence (e.g., "bash", "powershell").
    /// </summary>
    /// <remarks>
    /// Null if the code fence had no language identifier or if the command
    /// was extracted heuristically from inline code.
    /// </remarks>
    public string? Language { get; init; }

    /// <summary>
    /// Gets a description of what the command does.
    /// </summary>
    /// <remarks>
    /// Extracted from surrounding text in the message. May be null if
    /// no descriptive context was found.
    /// </remarks>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the suggested working directory for this command.
    /// </summary>
    /// <remarks>
    /// Extracted from context if the message mentions a specific directory.
    /// May be null if no directory was mentioned.
    /// </remarks>
    public string? WorkingDirectory { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // DETECTION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the detected shell type based on language identifier.
    /// </summary>
    /// <remarks>
    /// Used to determine shell-specific behavior and formatting.
    /// Null if the shell type could not be determined.
    /// </remarks>
    public ShellType? DetectedShellType { get; init; }

    /// <summary>
    /// Gets the confidence score for command detection (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// Higher values indicate more confidence that this is an executable command:
    /// <list type="bullet">
    /// <item><description>0.95: Fenced code block with shell language identifier</description></item>
    /// <item><description>0.70: Fenced code block without language identifier</description></item>
    /// <item><description>0.60: Inline code following command indicator phrase</description></item>
    /// </list>
    /// </remarks>
    public float ConfidenceScore { get; init; } = 1.0f;

    /// <summary>
    /// Gets a value indicating whether this command appears to be dangerous.
    /// </summary>
    /// <remarks>
    /// True for commands matching dangerous patterns like "rm -rf /",
    /// "format C:", "DROP DATABASE", etc.
    /// </remarks>
    public bool IsPotentiallyDangerous { get; init; }

    /// <summary>
    /// Gets the warning message if command is potentially dangerous.
    /// </summary>
    /// <remarks>
    /// Describes why the command was flagged and what caution to take.
    /// </remarks>
    public string? DangerWarning { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // EXECUTION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets or sets the current execution status.
    /// </summary>
    public CommandBlockStatus Status { get; set; } = CommandBlockStatus.Pending;

    /// <summary>
    /// Gets or sets the timestamp when this command was last executed.
    /// </summary>
    /// <remarks>Null if the command has never been executed.</remarks>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the terminal session where this was executed.
    /// </summary>
    /// <remarks>Null if the command has not been sent to a terminal.</remarks>
    public Guid? ExecutedInSessionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the associated output capture.
    /// </summary>
    /// <remarks>
    /// References a <see cref="TerminalOutputCapture"/> if output was captured.
    /// </remarks>
    public Guid? OutputCaptureId { get; set; }

    // ═══════════════════════════════════════════════════════════════════════
    // SOURCE MAPPING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the character range in the original message content.
    /// </summary>
    /// <remarks>
    /// Used for highlighting the command in the message view.
    /// </remarks>
    public TextRange SourceRange { get; init; }

    /// <summary>
    /// Gets the timestamp when this command was extracted.
    /// </summary>
    public DateTime ExtractedAt { get; init; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets a value indicating whether this command spans multiple lines.
    /// </summary>
    [JsonIgnore]
    public bool IsMultiLine => Command.Contains('\n');

    /// <summary>
    /// Gets the number of lines in the command.
    /// </summary>
    [JsonIgnore]
    public int LineCount => Command.Split('\n').Length;

    /// <summary>
    /// Gets just the first line for display in compact views.
    /// </summary>
    [JsonIgnore]
    public string FirstLine => Command.Split('\n')[0];

    /// <summary>
    /// Gets a value indicating whether the command has completed.
    /// </summary>
    /// <remarks>
    /// True if status is Executed, Failed, or Cancelled.
    /// </remarks>
    [JsonIgnore]
    public bool IsCompleted => Status.IsTerminal();

    /// <summary>
    /// Gets a value indicating whether the command can be run.
    /// </summary>
    /// <remarks>
    /// True if the command is not currently running or already completed.
    /// </remarks>
    [JsonIgnore]
    public bool CanRun => Status is CommandBlockStatus.Pending
                               or CommandBlockStatus.Copied
                               or CommandBlockStatus.SentToTerminal;

    /// <summary>
    /// Gets a value indicating whether the command is currently executing.
    /// </summary>
    [JsonIgnore]
    public bool IsRunning => Status == CommandBlockStatus.Executing;

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a display-friendly summary of this command.
    /// </summary>
    /// <returns>
    /// Truncated command text suitable for display in lists or tooltips.
    /// </returns>
    public string ToDisplaySummary()
    {
        // Log: Creating display summary for command {Id}
        var summary = LineCount > 1
            ? $"{FirstLine} (+{LineCount - 1} more lines)"
            : Command;

        // Truncate long commands
        if (summary.Length > 60)
        {
            summary = summary[..57] + "...";
        }

        return summary;
    }

    /// <summary>
    /// Marks this command as copied to clipboard.
    /// </summary>
    /// <remarks>
    /// Only transitions from Pending state to preserve execution history.
    /// </remarks>
    public void MarkCopied()
    {
        // Log: Marking command {Id} as copied
        if (Status == CommandBlockStatus.Pending)
        {
            Status = CommandBlockStatus.Copied;
        }
    }

    /// <summary>
    /// Marks this command as sent to terminal.
    /// </summary>
    /// <param name="sessionId">ID of the terminal session.</param>
    public void MarkSentToTerminal(Guid sessionId)
    {
        // Log: Marking command {Id} as sent to terminal {sessionId}
        Status = CommandBlockStatus.SentToTerminal;
        ExecutedInSessionId = sessionId;
    }

    /// <summary>
    /// Marks this command as currently executing.
    /// </summary>
    /// <param name="sessionId">ID of the terminal session.</param>
    public void MarkExecuting(Guid sessionId)
    {
        // Log: Marking command {Id} as executing in session {sessionId}
        Status = CommandBlockStatus.Executing;
        ExecutedInSessionId = sessionId;
        ExecutedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this command as completed with the given exit code.
    /// </summary>
    /// <param name="exitCode">The command's exit code (0 = success).</param>
    /// <param name="outputCaptureId">Optional ID of the captured output.</param>
    public void MarkCompleted(int exitCode, Guid? outputCaptureId = null)
    {
        // Log: Marking command {Id} as completed with exit code {exitCode}
        Status = exitCode == 0
            ? CommandBlockStatus.Executed
            : CommandBlockStatus.Failed;
        OutputCaptureId = outputCaptureId;
    }

    /// <summary>
    /// Marks this command as cancelled.
    /// </summary>
    public void MarkCancelled()
    {
        // Log: Marking command {Id} as cancelled
        Status = CommandBlockStatus.Cancelled;
    }

    /// <summary>
    /// Returns a string representation of this command block.
    /// </summary>
    public override string ToString() =>
        $"CommandBlock[{Id.ToString("N")[..8]}]: {ToDisplaySummary()} ({Status})";
}
