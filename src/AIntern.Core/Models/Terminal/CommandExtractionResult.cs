// -----------------------------------------------------------------------
// <copyright file="CommandExtractionResult.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

using AIntern.Core.Interfaces;

/// <summary>
/// Result of extracting commands from a message.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4a.</para>
/// <para>
/// This model contains extracted commands along with any warnings
/// generated during extraction. It provides aggregate statistics
/// for quick assessment of the extraction results.
/// </para>
/// </remarks>
public sealed class CommandExtractionResult
{
    // ═══════════════════════════════════════════════════════════════════════
    // COLLECTIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the extracted command blocks, ordered by sequence number.
    /// </summary>
    public IReadOnlyList<CommandBlock> Commands { get; init; } = Array.Empty<CommandBlock>();

    /// <summary>
    /// Gets the warnings generated during extraction.
    /// </summary>
    /// <remarks>
    /// Warnings indicate issues like potentially dangerous commands
    /// or low-confidence detections.
    /// </remarks>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets a value indicating whether any commands were found.
    /// </summary>
    public bool HasCommands => Commands.Count > 0;

    /// <summary>
    /// Gets the total number of commands found.
    /// </summary>
    public int CommandCount => Commands.Count;

    /// <summary>
    /// Gets the number of potentially dangerous commands.
    /// </summary>
    public int DangerousCommandCount => Commands.Count(c => c.IsPotentiallyDangerous);

    /// <summary>
    /// Gets a value indicating whether any dangerous commands were detected.
    /// </summary>
    public bool HasDangerousCommands => DangerousCommandCount > 0;

    /// <summary>
    /// Gets a value indicating whether there are warnings to display.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Gets the number of multi-line commands (scripts).
    /// </summary>
    public int MultiLineCommandCount => Commands.Count(c => c.IsMultiLine);

    /// <summary>
    /// Gets the average confidence score across all commands.
    /// </summary>
    public float AverageConfidence =>
        Commands.Count > 0 ? Commands.Average(c => c.ConfidenceScore) : 0f;

    /// <summary>
    /// Gets all unique shell types detected in commands.
    /// </summary>
    public IEnumerable<ShellType> DetectedShellTypes =>
        Commands
            .Where(c => c.DetectedShellType.HasValue)
            .Select(c => c.DetectedShellType!.Value)
            .Distinct();

    // ═══════════════════════════════════════════════════════════════════════
    // FACTORY METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets an empty result (no commands found).
    /// </summary>
    public static CommandExtractionResult Empty { get; } = new();

    /// <summary>
    /// Creates a result with a single command.
    /// </summary>
    /// <param name="command">The extracted command.</param>
    /// <returns>A result containing only the specified command.</returns>
    public static CommandExtractionResult Single(CommandBlock command) =>
        new() { Commands = new[] { command } };

    /// <summary>
    /// Creates a result from a list of commands and optional warnings.
    /// </summary>
    /// <param name="commands">The extracted commands.</param>
    /// <param name="warnings">Optional extraction warnings.</param>
    /// <returns>A new CommandExtractionResult.</returns>
    public static CommandExtractionResult From(
        IEnumerable<CommandBlock> commands,
        IEnumerable<string>? warnings = null) =>
        new()
        {
            Commands = commands.ToList().AsReadOnly(),
            Warnings = (warnings?.ToList() ?? new List<string>()).AsReadOnly()
        };

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets commands filtered by shell type.
    /// </summary>
    /// <param name="shellType">The shell type to filter by.</param>
    /// <returns>Commands matching the specified shell type.</returns>
    public IEnumerable<CommandBlock> GetCommandsByShellType(ShellType shellType) =>
        Commands.Where(c => c.DetectedShellType == shellType);

    /// <summary>
    /// Gets commands with confidence score at or above the threshold.
    /// </summary>
    /// <param name="minConfidence">Minimum confidence threshold (0.0 to 1.0).</param>
    /// <returns>Commands meeting the confidence threshold.</returns>
    public IEnumerable<CommandBlock> GetHighConfidenceCommands(float minConfidence = 0.8f) =>
        Commands.Where(c => c.ConfidenceScore >= minConfidence);

    /// <summary>
    /// Gets only safe (non-dangerous) commands.
    /// </summary>
    /// <returns>Commands that are not flagged as potentially dangerous.</returns>
    public IEnumerable<CommandBlock> GetSafeCommands() =>
        Commands.Where(c => !c.IsPotentiallyDangerous);

    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    public override string ToString() =>
        HasCommands
            ? $"CommandExtractionResult: {CommandCount} commands ({DangerousCommandCount} dangerous, {Warnings.Count} warnings)"
            : "CommandExtractionResult: No commands found";
}
