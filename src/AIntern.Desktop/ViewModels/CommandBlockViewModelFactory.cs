// -----------------------------------------------------------------------
// <copyright file="CommandBlockViewModelFactory.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Desktop.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ COMMAND BLOCK VIEWMODEL FACTORY (v0.5.4e)                               │
// │ Creates CommandBlockViewModel instances with proper DI.                 │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Factory for creating <see cref="CommandBlockViewModel"/> instances with proper DI.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4e.</para>
/// <para>
/// This factory is registered as a singleton and provides:
/// </para>
/// <list type="bullet">
/// <item>
///     <term>Single creation</term>
///     <description>
///     <see cref="Create(CommandBlock)"/> for individual command blocks.
///     </description>
/// </item>
/// <item>
///     <term>Batch creation</term>
///     <description>
///     <see cref="CreateRange(IEnumerable{CommandBlock})"/> for multiple blocks.
///     </description>
/// </item>
/// <item>
///     <term>Extraction result handling</term>
///     <description>
///     <see cref="CreateFromResult(CommandExtractionResult)"/> for extraction output.
///     </description>
/// </item>
/// </list>
/// <para>
/// <b>Usage:</b> Inject this factory into ViewModels or services that need to
/// create CommandBlockViewModels without directly accessing DI container.
/// </para>
/// </remarks>
public sealed class CommandBlockViewModelFactory
{
    // ═══════════════════════════════════════════════════════════════════════
    // DEPENDENCIES
    // ═══════════════════════════════════════════════════════════════════════

    private readonly ICommandExecutionService _executionService;
    private readonly ITerminalService _terminalService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<CommandBlockViewModelFactory> _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBlockViewModelFactory"/> class.
    /// </summary>
    /// <param name="executionService">Command execution service.</param>
    /// <param name="terminalService">Terminal service for session state.</param>
    /// <param name="loggerFactory">Logger factory for creating ViewModel loggers.</param>
    public CommandBlockViewModelFactory(
        ICommandExecutionService executionService,
        ITerminalService terminalService,
        ILoggerFactory loggerFactory)
    {
        _executionService = executionService;
        _terminalService = terminalService;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<CommandBlockViewModelFactory>();

        _logger.LogDebug("CommandBlockViewModelFactory initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FACTORY METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Create a ViewModel for a single command block.
    /// </summary>
    /// <param name="command">The command block to wrap.</param>
    /// <returns>Configured ViewModel instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="command"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The returned ViewModel subscribes to execution and terminal service events.
    /// Dispose the ViewModel when no longer needed to unsubscribe.
    /// </para>
    /// </remarks>
    public CommandBlockViewModel Create(CommandBlock command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var viewModel = new CommandBlockViewModel(
            _executionService,
            _terminalService,
            _loggerFactory.CreateLogger<CommandBlockViewModel>())
        {
            Command = command
        };

        _logger.LogTrace("Created CommandBlockViewModel for command {CommandId}", command.Id);

        return viewModel;
    }

    /// <summary>
    /// Create ViewModels for multiple command blocks.
    /// </summary>
    /// <param name="commands">Command blocks to wrap.</param>
    /// <returns>Sequence of ViewModels in order.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="commands"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This creates one ViewModel per command block, preserving order.
    /// Each ViewModel subscribes to its own events - dispose when done.
    /// </para>
    /// </remarks>
    public IEnumerable<CommandBlockViewModel> CreateRange(IEnumerable<CommandBlock> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        _logger.LogDebug("Creating CommandBlockViewModels for multiple commands");

        return commands.Select(Create);
    }

    /// <summary>
    /// Create ViewModels from an extraction result.
    /// </summary>
    /// <param name="result">Extraction result containing commands.</param>
    /// <returns>Sequence of ViewModels for all extracted commands.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="result"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is a convenience method for processing output from
    /// <see cref="ICommandExtractorService"/>.
    /// </para>
    /// </remarks>
    public IEnumerable<CommandBlockViewModel> CreateFromResult(CommandExtractionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _logger.LogDebug("Creating CommandBlockViewModels from extraction result with {Count} commands",
            result.Commands.Count);

        return CreateRange(result.Commands);
    }

    /// <summary>
    /// Create ViewModels from a list of command blocks (convenience for List).
    /// </summary>
    /// <param name="commands">List of command blocks.</param>
    /// <returns>List of ViewModels.</returns>
    public IReadOnlyList<CommandBlockViewModel> CreateList(IReadOnlyList<CommandBlock> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        return CreateRange(commands).ToList().AsReadOnly();
    }
}
