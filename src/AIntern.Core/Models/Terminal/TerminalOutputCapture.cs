// -----------------------------------------------------------------------
// <copyright file="TerminalOutputCapture.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

using System.Text;
using System.Text.Json.Serialization;

/// <summary>
/// Captured output from a terminal session for AI context.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4a.</para>
/// <para>
/// This model stores terminal output that can be shared with the AI
/// assistant to provide context for debugging or explanation. Output
/// may be truncated to fit within token budgets while preserving the
/// most relevant information.
/// </para>
/// </remarks>
public sealed class TerminalOutputCapture
{
    // ═══════════════════════════════════════════════════════════════════════
    // IDENTITY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the unique identifier for this capture.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the ID of the terminal session this was captured from.
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Gets the name of the terminal session.
    /// </summary>
    /// <remarks>
    /// Preserved for display purposes even if the session is closed.
    /// </remarks>
    public string? SessionName { get; init; }

    /// <summary>
    /// Gets the ID of the CommandBlock that triggered this capture.
    /// </summary>
    /// <remarks>
    /// Links back to the source command if this capture was triggered
    /// by executing a command block. Null for manual captures.
    /// </remarks>
    public Guid? CommandBlockId { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // CONTENT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the command that was executed (if known).
    /// </summary>
    /// <remarks>
    /// May be null for buffer captures or selection captures where
    /// no specific command is associated.
    /// </remarks>
    public string? Command { get; init; }

    /// <summary>
    /// Gets the captured output text.
    /// </summary>
    /// <remarks>
    /// Contains the raw terminal output with ANSI escape sequences stripped.
    /// May be truncated if the original output exceeded the character limit.
    /// </remarks>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the output was truncated.
    /// </summary>
    public bool IsTruncated { get; init; }

    /// <summary>
    /// Gets the original output length before truncation.
    /// </summary>
    /// <remarks>
    /// Equal to <see cref="Output"/>.Length if not truncated.
    /// </remarks>
    public int OriginalLength { get; init; }

    /// <summary>
    /// Gets the exit code of the command (if available).
    /// </summary>
    /// <remarks>
    /// Null if the exit code could not be determined or if this
    /// capture is not associated with a specific command.
    /// </remarks>
    public int? ExitCode { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // TIMING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the timestamp when the capture started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the capture completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Gets the duration of the capture.
    /// </summary>
    [JsonIgnore]
    public TimeSpan Duration => CompletedAt - StartedAt;

    // ═══════════════════════════════════════════════════════════════════════
    // CONTEXT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the working directory at time of capture.
    /// </summary>
    /// <remarks>
    /// Provides context for relative paths in the output.
    /// </remarks>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the capture mode used.
    /// </summary>
    public OutputCaptureMode CaptureMode { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the estimated token count for LLM context.
    /// </summary>
    /// <remarks>
    /// Uses a rough estimate of ~4 characters per token, plus overhead
    /// for formatting (command, directory, exit code).
    /// </remarks>
    [JsonIgnore]
    public int EstimatedTokens => (Output.Length + (Command?.Length ?? 0) + 50) / 4;

    /// <summary>
    /// Gets a value indicating whether the command succeeded.
    /// </summary>
    /// <remarks>
    /// True if exit code is 0 or unknown (null).
    /// </remarks>
    [JsonIgnore]
    public bool IsSuccess => !ExitCode.HasValue || ExitCode.Value == 0;

    /// <summary>
    /// Gets the number of lines in the output.
    /// </summary>
    [JsonIgnore]
    public int LineCount => Output.Split('\n').Length;

    /// <summary>
    /// Gets a value indicating whether this capture has output content.
    /// </summary>
    [JsonIgnore]
    public bool HasOutput => !string.IsNullOrEmpty(Output);

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Formats the capture for inclusion in AI context.
    /// </summary>
    /// <returns>
    /// A markdown-formatted string containing the command, output,
    /// and metadata suitable for LLM context.
    /// </returns>
    public string ToContextString()
    {
        // Log: Formatting capture {Id} for context ({EstimatedTokens} estimated tokens)
        var sb = new StringBuilder();

        sb.AppendLine("```terminal");

        if (!string.IsNullOrEmpty(WorkingDirectory))
        {
            sb.AppendLine($"# Directory: {WorkingDirectory}");
        }

        if (!string.IsNullOrEmpty(Command))
        {
            sb.AppendLine($"$ {Command}");
        }

        sb.AppendLine(Output);

        if (ExitCode.HasValue)
        {
            sb.AppendLine($"# Exit code: {ExitCode.Value}");
        }

        if (IsTruncated)
        {
            sb.AppendLine($"# (Output truncated from {OriginalLength:N0} characters)");
        }

        sb.AppendLine("```");

        return sb.ToString();
    }

    /// <summary>
    /// Creates a compact summary for display.
    /// </summary>
    /// <returns>
    /// First few lines of output, truncated for UI display.
    /// </returns>
    public string ToSummary()
    {
        // Log: Creating summary for capture {Id}
        var lines = Output.Split('\n');
        var preview = lines.Length > 3
            ? $"{lines[0]}\n{lines[1]}\n... ({lines.Length - 2} more lines)"
            : Output;

        // Truncate long previews
        if (preview.Length > 200)
        {
            preview = preview[..197] + "...";
        }

        return preview;
    }

    /// <summary>
    /// Creates a truncated version of this capture to fit within a character limit.
    /// </summary>
    /// <param name="maxCharacters">Maximum characters to keep.</param>
    /// <returns>
    /// A new <see cref="TerminalOutputCapture"/> with truncated output,
    /// or this instance if no truncation is needed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Truncation preserves the first 20% and last 60% of output, with
    /// an ellipsis message in the middle indicating how much was removed.
    /// This strategy keeps initial context/setup and recent/relevant output.
    /// </para>
    /// </remarks>
    public TerminalOutputCapture Truncate(int maxCharacters)
    {
        // Log: Truncating capture {Id} from {Output.Length} to {maxCharacters} characters
        if (Output.Length <= maxCharacters)
        {
            return this;
        }

        // Keep first 20% and last 60%, with ellipsis in middle
        var firstPart = (int)(maxCharacters * 0.2);
        var lastPart = (int)(maxCharacters * 0.6);
        var ellipsis = $"\n\n[... {Output.Length - firstPart - lastPart:N0} characters truncated ...]\n\n";

        var truncated = Output[..firstPart] + ellipsis + Output[^lastPart..];

        return new TerminalOutputCapture
        {
            Id = Id,
            SessionId = SessionId,
            SessionName = SessionName,
            CommandBlockId = CommandBlockId,
            Command = Command,
            Output = truncated,
            IsTruncated = true,
            OriginalLength = Output.Length,
            ExitCode = ExitCode,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            WorkingDirectory = WorkingDirectory,
            CaptureMode = CaptureMode
        };
    }

    /// <summary>
    /// Returns a string representation of this capture.
    /// </summary>
    public override string ToString() =>
        $"TerminalOutputCapture[{Id.ToString("N")[..8]}]: {LineCount} lines, {EstimatedTokens} tokens (Mode: {CaptureMode})";
}
