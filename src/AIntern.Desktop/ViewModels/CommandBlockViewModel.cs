// -----------------------------------------------------------------------
// <copyright file="CommandBlockViewModel.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Desktop.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ COMMAND BLOCK VIEWMODEL (v0.5.4e)                                       │
// │ MVVM presentation layer for command blocks with user interactions.      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for an individual command block in chat messages.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4e.</para>
/// <para>
/// This ViewModel provides:
/// </para>
/// <list type="bullet">
/// <item>
///     <term>Observable Properties</term>
///     <description>
///     Status, IsExecuting, StatusMessage for UI binding.
///     </description>
/// </item>
/// <item>
///     <term>Computed Properties</term>
///     <description>
///     CommandPreview, LanguageBadge, StatusText, CanExecute for display.
///     </description>
/// </item>
/// <item>
///     <term>Commands</term>
///     <description>
///     Copy, SendToTerminal, Execute, ConfirmDanger, CancelDanger, ToggleExpanded.
///     </description>
/// </item>
/// <item>
///     <term>Dangerous Command Handling</term>
///     <description>
///     Confirmation workflow before executing dangerous commands.
///     </description>
/// </item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> All property changes are raised on the UI thread via
/// CommunityToolkit.Mvvm's source-generated change notifications.
/// </para>
/// </remarks>
public sealed partial class CommandBlockViewModel : ViewModelBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════
    // DEPENDENCIES
    // ═══════════════════════════════════════════════════════════════════════

    private readonly ICommandExecutionService _executionService;
    private readonly ITerminalService _terminalService;
    private readonly ILogger<CommandBlockViewModel> _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The underlying command block model.
    /// </summary>
    [ObservableProperty]
    private CommandBlock _command = null!;

    /// <summary>
    /// Current execution status of the command.
    /// </summary>
    [ObservableProperty]
    private CommandBlockStatus _status = CommandBlockStatus.Pending;

    /// <summary>
    /// Whether the command is currently being executed.
    /// </summary>
    [ObservableProperty]
    private bool _isExecuting;

    /// <summary>
    /// Temporary status message (e.g., "Copied to clipboard").
    /// </summary>
    /// <remarks>
    /// Auto-clears after 2 seconds via <see cref="ClearStatusMessageAfterDelayAsync"/>.
    /// </remarks>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Whether the danger warning overlay is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showDangerWarning;

    /// <summary>
    /// Whether the user has confirmed a dangerous command.
    /// </summary>
    [ObservableProperty]
    private bool _dangerConfirmed;

    /// <summary>
    /// Whether the multi-line command is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED DISPLAY PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Full command text for display.
    /// </summary>
    public string CommandText => Command?.Command ?? string.Empty;

    /// <summary>
    /// Short preview (first line, max 60 chars) for collapsed view.
    /// </summary>
    public string CommandPreview
    {
        get
        {
            if (Command?.Command == null) return string.Empty;
            var firstLine = Command.Command.Split('\n').FirstOrDefault() ?? string.Empty;
            return firstLine.Length > 60 ? firstLine[..57] + "..." : firstLine;
        }
    }

    /// <summary>
    /// Description extracted from preceding text.
    /// </summary>
    public string? Description => Command?.Description;

    /// <summary>
    /// Language badge text (uppercase).
    /// </summary>
    /// <remarks>
    /// Example: "bash" → "BASH", "powershell" → "POWERSHELL"
    /// </remarks>
    public string? LanguageBadge => Command?.Language?.ToUpperInvariant();

    /// <summary>
    /// Whether command spans multiple lines.
    /// </summary>
    public bool IsMultiLine => Command?.IsMultiLine ?? false;

    /// <summary>
    /// Number of lines in the command.
    /// </summary>
    public int LineCount => Command?.LineCount ?? 1;

    /// <summary>
    /// Text for collapsed view showing additional lines.
    /// </summary>
    /// <remarks>
    /// Example: "+3 more lines"
    /// </remarks>
    public string AdditionalLinesText =>
        LineCount > 1 ? $"+{LineCount - 1} more lines" : string.Empty;

    /// <summary>
    /// Whether command is potentially dangerous.
    /// </summary>
    public bool IsDangerous => Command?.IsPotentiallyDangerous ?? false;

    /// <summary>
    /// Danger warning message for display.
    /// </summary>
    public string? DangerWarning => Command?.DangerWarning;

    /// <summary>
    /// Confidence score as percentage text.
    /// </summary>
    /// <remarks>
    /// Example: "85%"
    /// </remarks>
    public string ConfidenceText => $"{(int)((Command?.ConfidenceScore ?? 1.0f) * 100)}%";

    /// <summary>
    /// Whether to show confidence indicator (only if low confidence).
    /// </summary>
    public bool ShowConfidence => (Command?.ConfidenceScore ?? 1.0f) < 0.80f;

    // ═══════════════════════════════════════════════════════════════════════
    // STATUS DISPLAY PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Status text for badge display.
    /// </summary>
    public string StatusText => Status switch
    {
        CommandBlockStatus.Pending => "",
        CommandBlockStatus.Copied => "Copied",
        CommandBlockStatus.SentToTerminal => "Sent",
        CommandBlockStatus.Executing => "Running...",
        CommandBlockStatus.Executed => "Executed",
        CommandBlockStatus.Failed => "Failed",
        CommandBlockStatus.Cancelled => "Cancelled",
        _ => ""
    };

    /// <summary>
    /// Whether to show the status badge.
    /// </summary>
    public bool ShowStatus => Status != CommandBlockStatus.Pending;

    /// <summary>
    /// CSS-like class name for status styling.
    /// </summary>
    /// <remarks>
    /// Used by the View to apply appropriate styling.
    /// Values: "success", "error", "warning", "running", "info", ""
    /// </remarks>
    public string StatusClass => Status switch
    {
        CommandBlockStatus.Executed => "success",
        CommandBlockStatus.Failed => "error",
        CommandBlockStatus.Cancelled => "warning",
        CommandBlockStatus.Executing => "running",
        CommandBlockStatus.Copied => "info",
        CommandBlockStatus.SentToTerminal => "info",
        _ => ""
    };

    /// <summary>
    /// Icon name for status display.
    /// </summary>
    public string StatusIcon => Status.ToIconName();

    // ═══════════════════════════════════════════════════════════════════════
    // TERMINAL STATE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether there's an active terminal session.
    /// </summary>
    public bool HasActiveTerminal => _terminalService.ActiveSession != null;

    /// <summary>
    /// Whether the command can be executed right now.
    /// </summary>
    /// <remarks>
    /// False when:
    /// - Currently executing (IsExecuting)
    /// - Already in a terminal status (Executed, Failed, Cancelled)
    /// </remarks>
    public bool CanExecute => !IsExecuting && !Status.IsTerminal();

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBlockViewModel"/> class.
    /// </summary>
    /// <param name="executionService">Command execution service.</param>
    /// <param name="terminalService">Terminal service for session state.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public CommandBlockViewModel(
        ICommandExecutionService executionService,
        ITerminalService terminalService,
        ILogger<CommandBlockViewModel> logger)
    {
        _executionService = executionService;
        _terminalService = terminalService;
        _logger = logger;

        // Subscribe to status changes
        _executionService.StatusChanged += OnStatusChanged;
        _terminalService.SessionStateChanged += OnSessionStateChanged;

        _logger.LogDebug("CommandBlockViewModel created");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles status change events from the execution service.
    /// </summary>
    private void OnStatusChanged(object? sender, CommandStatusChangedEventArgs e)
    {
        // Only update if this is our command
        if (Command == null || e.CommandId != Command.Id) return;

        _logger.LogDebug("Status changed for command {CommandId}: {Old} → {New}",
            e.CommandId, e.OldStatus, e.NewStatus);

        Status = e.NewStatus;
        IsExecuting = e.NewStatus == CommandBlockStatus.Executing;
    }

    /// <summary>
    /// Handles terminal session state changes.
    /// </summary>
    private void OnSessionStateChanged(object? sender, TerminalSessionStateEventArgs e)
    {
        _logger.LogTrace("Terminal session state changed, refreshing HasActiveTerminal");
        OnPropertyChanged(nameof(HasActiveTerminal));
        SendToTerminalCommand.NotifyCanExecuteChanged();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Copy command text to clipboard.
    /// </summary>
    [RelayCommand]
    private async Task CopyAsync()
    {
        if (Command == null) return;

        _logger.LogDebug("Copying command {CommandId} to clipboard", Command.Id);

        try
        {
            ClearError();
            await _executionService.CopyToClipboardAsync(Command);
            StatusMessage = "Copied to clipboard";
            await ClearStatusMessageAfterDelayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy command {CommandId}", Command.Id);
            StatusMessage = $"Copy failed: {ex.Message}";
            SetError(ex.Message);
        }
    }

    /// <summary>
    /// Send command to terminal (paste) without executing.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasActiveTerminal))]
    private async Task SendToTerminalAsync()
    {
        if (Command == null) return;

        _logger.LogDebug("Sending command {CommandId} to terminal", Command.Id);

        // Check for dangerous command confirmation
        if (IsDangerous && !DangerConfirmed)
        {
            _logger.LogInformation("Dangerous command {CommandId} requires confirmation", Command.Id);
            ShowDangerWarning = true;
            return;
        }

        try
        {
            ClearError();
            await _executionService.SendToTerminalAsync(Command);
            StatusMessage = "Sent to terminal";
            await ClearStatusMessageAfterDelayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command {CommandId} to terminal", Command.Id);
            StatusMessage = $"Send failed: {ex.Message}";
            SetError(ex.Message);
        }
    }

    /// <summary>
    /// Execute command in terminal.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task ExecuteAsync()
    {
        if (Command == null) return;

        _logger.LogDebug("Executing command {CommandId}", Command.Id);

        // Check for dangerous command confirmation
        if (IsDangerous && !DangerConfirmed)
        {
            _logger.LogInformation("Dangerous command {CommandId} requires confirmation before execution", Command.Id);
            ShowDangerWarning = true;
            return;
        }

        try
        {
            ClearError();
            IsExecuting = true;

            var result = await _executionService.ExecuteAsync(
                Command,
                captureOutput: true);

            StatusMessage = result.Status == CommandBlockStatus.Executed
                ? "Executed successfully"
                : $"Execution {result.Status.ToString().ToLowerInvariant()}";

            _logger.LogInformation("Command {CommandId} execution completed: {Status}",
                Command.Id, result.Status);

            await ClearStatusMessageAfterDelayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command {CommandId}", Command.Id);
            StatusMessage = $"Execution failed: {ex.Message}";
            SetError(ex.Message);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// Confirm dangerous command execution.
    /// </summary>
    [RelayCommand]
    private void ConfirmDanger()
    {
        _logger.LogInformation("User confirmed dangerous command {CommandId}", Command?.Id);

        DangerConfirmed = true;
        ShowDangerWarning = false;

        // Note: The user will need to click the button again after confirming.
        // This is intentional for safety - explicit re-action required.
    }

    /// <summary>
    /// Cancel dangerous command confirmation.
    /// </summary>
    [RelayCommand]
    private void CancelDanger()
    {
        _logger.LogDebug("User cancelled dangerous command {CommandId}", Command?.Id);

        ShowDangerWarning = false;
        // DangerConfirmed stays false
    }

    /// <summary>
    /// Toggle multi-line command expansion.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        _logger.LogTrace("Command {CommandId} expanded: {Expanded}", Command?.Id, IsExpanded);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clears the status message after a 2 second delay.
    /// </summary>
    private async Task ClearStatusMessageAfterDelayAsync()
    {
        await Task.Delay(2000);
        StatusMessage = null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PROPERTY CHANGE HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the Command property changes.
    /// </summary>
    partial void OnCommandChanged(CommandBlock value)
    {
        _logger.LogTrace("Command changed to {CommandId}", value?.Id);

        // Sync status from model
        if (value != null)
        {
            Status = value.Status;
        }

        // Notify all computed properties
        OnPropertyChanged(nameof(CommandText));
        OnPropertyChanged(nameof(CommandPreview));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(LanguageBadge));
        OnPropertyChanged(nameof(IsMultiLine));
        OnPropertyChanged(nameof(LineCount));
        OnPropertyChanged(nameof(AdditionalLinesText));
        OnPropertyChanged(nameof(IsDangerous));
        OnPropertyChanged(nameof(DangerWarning));
        OnPropertyChanged(nameof(ConfidenceText));
        OnPropertyChanged(nameof(ShowConfidence));
    }

    /// <summary>
    /// Called when the Status property changes.
    /// </summary>
    partial void OnStatusChanged(CommandBlockStatus value)
    {
        _logger.LogTrace("Status property changed to {Status}", value);

        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ShowStatus));
        OnPropertyChanged(nameof(StatusClass));
        OnPropertyChanged(nameof(StatusIcon));
        OnPropertyChanged(nameof(CanExecute));

        // Notify commands of can-execute changes
        ExecuteCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when the IsExecuting property changes.
    /// </summary>
    partial void OnIsExecutingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanExecute));
        ExecuteCommand.NotifyCanExecuteChanged();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public void Dispose()
    {
        _executionService.StatusChanged -= OnStatusChanged;
        _terminalService.SessionStateChanged -= OnSessionStateChanged;

        _logger.LogTrace("CommandBlockViewModel disposed for command {CommandId}", Command?.Id);
    }
}
