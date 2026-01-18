// -----------------------------------------------------------------------
// <copyright file="CommandExecutionService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Services.Terminal;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

/// <summary>
/// Handles command execution in terminal sessions with status tracking.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4c.</para>
/// <para>
/// This service provides the execution layer for commands extracted by
/// <see cref="ICommandExtractorService"/>. It manages:
/// </para>
/// <list type="bullet">
/// <item><description>Clipboard integration for copy operations</description></item>
/// <item><description>Terminal I/O for send and execute operations</description></item>
/// <item><description>Session lifecycle with shell type preferences</description></item>
/// <item><description>Status tracking with event notifications</description></item>
/// <item><description>Error handling with proper status transitions</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b>
/// Status tracking uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for
/// thread-safe access. Events are raised synchronously on the calling thread.
/// </para>
/// </remarks>
public sealed class CommandExecutionService : ICommandExecutionService
{
    // ═══════════════════════════════════════════════════════════════════════
    // DEPENDENCIES
    // ═══════════════════════════════════════════════════════════════════════

    private readonly ITerminalService _terminalService;
    private readonly IShellProfileService _profileService;
    private readonly IClipboardService? _clipboardService;
    private readonly ILogger<CommandExecutionService> _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Thread-safe dictionary tracking command statuses.
    /// </summary>
    /// <remarks>
    /// Key: Command ID, Value: Current status.
    /// Commands not in this dictionary are assumed to be Pending.
    /// </remarks>
    private readonly ConcurrentDictionary<Guid, CommandBlockStatus> _commandStatuses = new();

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public event EventHandler<CommandStatusChangedEventArgs>? StatusChanged;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutionService"/> class.
    /// </summary>
    /// <param name="terminalService">Terminal session management service.</param>
    /// <param name="profileService">Shell profile lookup service.</param>
    /// <param name="clipboardService">
    /// Clipboard service for copy operations. May be null in headless environments.
    /// </param>
    /// <param name="logger">Logger for diagnostics.</param>
    public CommandExecutionService(
        ITerminalService terminalService,
        IShellProfileService profileService,
        IClipboardService? clipboardService,
        ILogger<CommandExecutionService> logger)
    {
        _terminalService = terminalService;
        _profileService = profileService;
        _clipboardService = clipboardService;
        _logger = logger;

        _logger.LogDebug("CommandExecutionService initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CLIPBOARD OPERATIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task CopyToClipboardAsync(CommandBlock command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug("Copying command {Id} to clipboard: {Preview}",
            command.Id, command.ToDisplaySummary());

        // Verify clipboard is available
        if (_clipboardService == null)
        {
            _logger.LogWarning("Clipboard not available, cannot copy command {Id}", command.Id);
            throw new InvalidOperationException("Clipboard is not available in this environment.");
        }

        // Copy to clipboard
        await _clipboardService.SetTextAsync(command.Command);

        // Update command state
        command.MarkCopied();

        // Track and raise status change
        UpdateStatus(command.Id, CommandBlockStatus.Copied, sessionId: null);

        _logger.LogInformation("Command {Id} copied to clipboard", command.Id);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TERMINAL OPERATIONS - SEND
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task SendToTerminalAsync(
        CommandBlock command,
        Guid? targetSessionId = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Sending command {Id} to terminal (no execute): {Preview}",
            command.Id, command.ToDisplaySummary());

        // Ensure we have a terminal session
        var sessionId = targetSessionId ?? await EnsureTerminalSessionAsync(
            command.DetectedShellType,
            command.WorkingDirectory,
            ct);

        // Send command text WITHOUT newline - user must press Enter
        // This allows user to review/modify before executing
        var success = await _terminalService.WriteInputAsync(sessionId, command.Command, ct);

        if (!success)
        {
            _logger.LogWarning("Failed to write to terminal session {Session}", sessionId);
            throw new InvalidOperationException($"Terminal session {sessionId} not found or not writable.");
        }

        // Update command state
        command.MarkSentToTerminal(sessionId);

        // Track and raise status change
        UpdateStatus(command.Id, CommandBlockStatus.SentToTerminal, sessionId);

        _logger.LogInformation("Command {Id} sent to terminal session {Session}",
            command.Id, sessionId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TERMINAL OPERATIONS - EXECUTE
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<CommandExecutionResult> ExecuteAsync(
        CommandBlock command,
        Guid? targetSessionId = null,
        bool captureOutput = false,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Executing command {Id}: {Preview}",
            command.Id, command.ToDisplaySummary());

        // ─────────────────────────────────────────────────────────────────
        // Step 1: Ensure terminal session
        // ─────────────────────────────────────────────────────────────────
        var sessionId = targetSessionId ?? await EnsureTerminalSessionAsync(
            command.DetectedShellType,
            command.WorkingDirectory,
            ct);

        var startTime = DateTime.UtcNow;
        TerminalOutputCapture? capture = null;

        try
        {
            // ─────────────────────────────────────────────────────────────
            // Step 2: Update status to Executing
            // ─────────────────────────────────────────────────────────────
            command.MarkExecuting(sessionId);
            UpdateStatus(command.Id, CommandBlockStatus.Executing, sessionId);

            _logger.LogTrace("Command {Id} status: Executing in session {Session}",
                command.Id, sessionId);

            // ─────────────────────────────────────────────────────────────
            // Step 3: Output capture (placeholder for v0.5.4d)
            // ─────────────────────────────────────────────────────────────
            // NOTE: Full output capture will be implemented in v0.5.4d.
            // For now, we just log that capture was requested.
            if (captureOutput)
            {
                _logger.LogDebug("Output capture requested for command {Id} (deferred to v0.5.4d)",
                    command.Id);
            }

            // ─────────────────────────────────────────────────────────────
            // Step 4: Send command WITH newline (Enter key)
            // ─────────────────────────────────────────────────────────────
            var commandWithNewline = command.Command.TrimEnd() + GetNewlineForSession(sessionId);
            var success = await _terminalService.WriteInputAsync(sessionId, commandWithNewline, ct);

            if (!success)
            {
                throw new InvalidOperationException($"Failed to write to terminal session {sessionId}");
            }

            _logger.LogDebug("Command {Id} sent to terminal with Enter key", command.Id);

            // ─────────────────────────────────────────────────────────────
            // Step 5: Mark as executed
            // ─────────────────────────────────────────────────────────────
            // NOTE: Without shell integration (OSC 133), we can't reliably detect
            // command completion. For now, we mark as executed immediately.
            // Future versions may use shell integration or output parsing.

            command.MarkCompleted(exitCode: 0, capture?.Id);
            UpdateStatus(command.Id, CommandBlockStatus.Executed, sessionId);

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Command {Id} executed successfully in session {Session} ({Duration}ms)",
                command.Id, sessionId, duration.TotalMilliseconds);

            return new CommandExecutionResult
            {
                CommandId = command.Id,
                Status = CommandBlockStatus.Executed,
                SessionId = sessionId,
                ExecutedAt = startTime,
                Duration = duration,
                OutputCapture = capture
            };
        }
        catch (OperationCanceledException)
        {
            // ─────────────────────────────────────────────────────────────
            // Handle cancellation
            // ─────────────────────────────────────────────────────────────
            _logger.LogInformation("Command {Id} execution was cancelled", command.Id);

            command.MarkCancelled();
            UpdateStatus(command.Id, CommandBlockStatus.Cancelled, sessionId);

            return new CommandExecutionResult
            {
                CommandId = command.Id,
                Status = CommandBlockStatus.Cancelled,
                SessionId = sessionId,
                ExecutedAt = startTime,
                Duration = DateTime.UtcNow - startTime,
                OutputCapture = capture
            };
        }
        catch (Exception ex)
        {
            // ─────────────────────────────────────────────────────────────
            // Handle errors
            // ─────────────────────────────────────────────────────────────
            _logger.LogError(ex, "Failed to execute command {Id}", command.Id);

            command.MarkCompleted(exitCode: -1);
            UpdateStatus(command.Id, CommandBlockStatus.Failed, sessionId);

            return new CommandExecutionResult
            {
                CommandId = command.Id,
                Status = CommandBlockStatus.Failed,
                SessionId = sessionId,
                ExecutedAt = startTime,
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = ex.Message,
                OutputCapture = capture
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TERMINAL OPERATIONS - EXECUTE ALL
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CommandExecutionResult>> ExecuteAllAsync(
        IEnumerable<CommandBlock> commands,
        Guid? targetSessionId = null,
        bool stopOnError = true,
        bool captureOutput = false,
        CancellationToken ct = default)
    {
        var commandList = commands.ToList();

        _logger.LogDebug("Executing {Count} commands sequentially (stopOnError={StopOnError})",
            commandList.Count, stopOnError);

        var results = new List<CommandExecutionResult>();
        Guid? sessionId = targetSessionId;

        foreach (var command in commandList)
        {
            // Check for cancellation before each command
            ct.ThrowIfCancellationRequested();

            // Execute the command
            var result = await ExecuteAsync(command, sessionId, captureOutput, ct);
            results.Add(result);

            // Reuse the same session for subsequent commands
            // This maintains environment variables and working directory
            sessionId = result.SessionId;

            // Stop on error if requested
            if (stopOnError && result.IsFailed)
            {
                _logger.LogWarning(
                    "Stopping execution due to failed command {Id} ({Executed}/{Total} completed)",
                    command.Id, results.Count, commandList.Count);
                break;
            }

            // Small delay between commands to allow terminal to stabilize
            await Task.Delay(100, ct);
        }

        _logger.LogInformation("Executed {Executed}/{Total} commands",
            results.Count, commandList.Count);

        return results.AsReadOnly();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TERMINAL OPERATIONS - CANCEL
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task CancelExecutionAsync(Guid sessionId, CancellationToken ct = default)
    {
        _logger.LogDebug("Cancelling execution in session {Session}", sessionId);

        // Send interrupt signal (Ctrl+C / SIGINT)
        await _terminalService.SendSignalAsync(sessionId, TerminalSignal.Interrupt, ct);

        _logger.LogInformation("Sent interrupt signal (Ctrl+C) to session {Session}", sessionId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // STATUS OPERATIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public CommandBlockStatus GetStatus(Guid commandId)
    {
        // Return tracked status, or Pending if not tracked
        return _commandStatuses.GetValueOrDefault(commandId, CommandBlockStatus.Pending);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SESSION MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Guid> EnsureTerminalSessionAsync(
        ShellType? preferredShell = null,
        string? workingDirectory = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Ensuring terminal session (preferredShell={Shell}, workingDir={Dir})",
            preferredShell, workingDirectory);

        // ─────────────────────────────────────────────────────────────────
        // Try to reuse active session if no specific shell is required
        // ─────────────────────────────────────────────────────────────────
        // NOTE: TerminalSession doesn't track ShellType, so we can only
        // reuse active session when no specific shell type is requested.
        // If a specific shell type is needed, we always create a new session
        // with the appropriate profile.
        var activeSession = _terminalService.ActiveSession;
        if (activeSession != null && preferredShell == null)
        {
            _logger.LogDebug("Reusing active session {Session} (no shell preference)",
                activeSession.Id);
            return activeSession.Id;
        }

        // ─────────────────────────────────────────────────────────────────
        // Find appropriate profile
        // ─────────────────────────────────────────────────────────────────
        var profile = preferredShell.HasValue
            ? await FindProfileForShellTypeAsync(preferredShell.Value, ct)
            : await _profileService.GetDefaultProfileAsync(ct);

        _logger.LogDebug("Using profile '{Profile}' for shell type {Shell}",
            profile.Name, profile.ShellType);

        // ─────────────────────────────────────────────────────────────────
        // Create new session with the profile
        // ─────────────────────────────────────────────────────────────────
        // Parse Arguments: ShellProfile.Arguments is a string, but
        // TerminalSessionOptions.Arguments expects string[]?
        string[]? argumentsArray = null;
        if (!string.IsNullOrWhiteSpace(profile.Arguments))
        {
            // Split by whitespace, preserving quoted sections would require
            // more complex parsing - for now, simple split is sufficient
            argumentsArray = profile.Arguments
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        var options = new TerminalSessionOptions
        {
            ShellPath = profile.ShellPath,
            Arguments = argumentsArray,
            WorkingDirectory = workingDirectory ?? profile.StartingDirectory,
            Environment = profile.Environment,
            Name = profile.Name
        };

        var session = await _terminalService.CreateSessionAsync(options, ct);

        _logger.LogInformation(
            "Created new terminal session {Session} with profile '{Profile}' (shell={Shell})",
            session.Id, profile.Name, profile.ShellType);

        return session.Id;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Find a shell profile matching the specified shell type.
    /// </summary>
    /// <param name="shellType">The shell type to find.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching profile, or default profile if no match found.</returns>
    private async Task<ShellProfile> FindProfileForShellTypeAsync(
        ShellType shellType,
        CancellationToken ct)
    {
        var profiles = await _profileService.GetAllProfilesAsync(ct);
        var matchingProfile = profiles.FirstOrDefault(p => p.ShellType == shellType);

        if (matchingProfile != null)
        {
            _logger.LogTrace("Found profile '{Name}' for shell type {Shell}",
                matchingProfile.Name, shellType);
            return matchingProfile;
        }

        _logger.LogDebug("No profile found for {ShellType}, using default", shellType);
        return await _profileService.GetDefaultProfileAsync(ct);
    }

    /// <summary>
    /// Get the appropriate newline character for a session.
    /// </summary>
    /// <param name="sessionId">The session ID (for future shell-specific handling).</param>
    /// <returns>Newline character sequence to send as Enter key.</returns>
    /// <remarks>
    /// Most terminal emulators expect carriage return (\r) for the Enter key.
    /// This could be made shell-specific in the future if needed.
    /// </remarks>
    private string GetNewlineForSession(Guid sessionId)
    {
        // Most terminals expect carriage return for Enter key
        // Could be made session/shell-specific if needed
        return "\r";
    }

    /// <summary>
    /// Update command status and raise status changed event.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="sessionId">Associated session ID (may be null).</param>
    private void UpdateStatus(Guid commandId, CommandBlockStatus newStatus, Guid? sessionId)
    {
        // Get old status (default to Pending if not tracked)
        var oldStatus = _commandStatuses.GetValueOrDefault(commandId, CommandBlockStatus.Pending);

        // Update tracking dictionary
        _commandStatuses[commandId] = newStatus;

        // Create event args
        var eventArgs = new CommandStatusChangedEventArgs
        {
            CommandId = commandId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            SessionId = sessionId
        };

        // Raise event
        StatusChanged?.Invoke(this, eventArgs);

        _logger.LogTrace("Command {Id} status: {Old} → {New}",
            commandId, oldStatus, newStatus);
    }
}
