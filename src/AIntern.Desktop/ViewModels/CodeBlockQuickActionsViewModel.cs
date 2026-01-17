using System.Collections.ObjectModel;
using AIntern.Core.Models;
using AIntern.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the code block quick actions bar (v0.4.5g).
/// </summary>
public partial class CodeBlockQuickActionsViewModel : ViewModelBase
{
    private readonly IQuickActionService _quickActionService;
    private readonly ILogger<CodeBlockQuickActionsViewModel>? _logger;

    private const int MaxVisibleActions = 4;
    private const int StatusDisplayDurationMs = 2000;
    private CancellationTokenSource? _statusClearCts;

    // ═══════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    [NotifyPropertyChangedFor(nameof(CanShowDiff))]
    [NotifyPropertyChangedFor(nameof(CanOpenFile))]
    [NotifyPropertyChangedFor(nameof(HasTargetFile))]
    [NotifyPropertyChangedFor(nameof(TargetFileName))]
    private CodeBlock? _codeBlock;

    [ObservableProperty]
    private ObservableCollection<QuickActionItemViewModel> _availableActions = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStatus))]
    private bool _isExecuting;

    [ObservableProperty]
    private QuickActionType? _currentAction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStatus))]
    private string? _statusMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStatus))]
    private bool _hasError;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isCopied;

    // ═══════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Actions visible in the main button bar.
    /// </summary>
    public IEnumerable<QuickActionItemViewModel> PrimaryActions =>
        AvailableActions.Take(MaxVisibleActions);

    /// <summary>
    /// Actions shown in the overflow menu.
    /// </summary>
    public IEnumerable<QuickActionItemViewModel> OverflowActions =>
        AvailableActions.Skip(MaxVisibleActions);

    /// <summary>
    /// Whether there are overflow actions to show.
    /// </summary>
    public bool HasOverflow => AvailableActions.Count > MaxVisibleActions;

    /// <summary>
    /// Whether the Apply action is available.
    /// </summary>
    public bool CanApply => CodeBlock?.IsApplicable == true &&
                            CodeBlock?.Status == CodeBlockStatus.Pending &&
                            !string.IsNullOrEmpty(CodeBlock?.TargetFilePath);

    /// <summary>
    /// Whether the Show Diff action is available.
    /// </summary>
    public bool CanShowDiff => CodeBlock?.IsApplicable == true &&
                               !string.IsNullOrEmpty(CodeBlock?.TargetFilePath);

    /// <summary>
    /// Whether the Open File action is available.
    /// </summary>
    public bool CanOpenFile => !string.IsNullOrEmpty(CodeBlock?.TargetFilePath);

    /// <summary>
    /// Whether there is a target file associated.
    /// </summary>
    public bool HasTargetFile => !string.IsNullOrEmpty(CodeBlock?.TargetFilePath);

    /// <summary>
    /// Display name of the target file.
    /// </summary>
    public string TargetFileName => CodeBlock?.TargetFilePath is { } path
        ? Path.GetFileName(path)
        : string.Empty;

    /// <summary>
    /// Whether to show the status message.
    /// </summary>
    public bool ShowStatus => !string.IsNullOrEmpty(StatusMessage) || IsExecuting;

    // ═══════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when a diff view should be shown.
    /// </summary>
    public event EventHandler<CodeBlock>? ShowDiffRequested;

    /// <summary>
    /// Raised when the options popup should be shown.
    /// </summary>
    public event EventHandler<CodeBlock>? ShowOptionsRequested;

    /// <summary>
    /// Raised when apply action starts.
    /// </summary>
    public event EventHandler<CodeBlock>? ApplyRequested;

    /// <summary>
    /// Raised when the file should be opened in editor.
    /// </summary>
    public event EventHandler<string>? OpenFileRequested;

    // ═══════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════

    public CodeBlockQuickActionsViewModel(
        IQuickActionService quickActionService,
        ILogger<CodeBlockQuickActionsViewModel>? logger = null)
    {
        _quickActionService = quickActionService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // Initialization
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes the ViewModel with a code block.
    /// </summary>
    public void Initialize(CodeBlock block)
    {
        CodeBlock = block;
        RefreshAvailability();
        _logger?.LogDebug(
            "[INIT] Initialized with block {BlockId}, {Count} actions available",
            block.Id, AvailableActions.Count);
    }

    /// <summary>
    /// Refreshes the available actions based on current block state.
    /// </summary>
    public void RefreshAvailability()
    {
        if (CodeBlock is null) return;

        AvailableActions.Clear();
        var actions = _quickActionService.GetAvailableActions(CodeBlock);
        foreach (var action in actions)
        {
            AvailableActions.Add(new QuickActionItemViewModel(action, this));
        }

        OnPropertyChanged(nameof(PrimaryActions));
        OnPropertyChanged(nameof(OverflowActions));
        OnPropertyChanged(nameof(HasOverflow));
    }

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Executes a quick action.
    /// </summary>
    [RelayCommand]
    private async Task ExecuteActionAsync(QuickAction action)
    {
        if (CodeBlock is null || IsExecuting) return;

        IsExecuting = true;
        CurrentAction = action.Type;
        HasError = false;
        StatusMessage = null;

        try
        {
            var result = await _quickActionService.ExecuteAsync(action, CodeBlock);

            if (result.IsSuccess)
            {
                StatusMessage = result.DisplayMessage;
                HasError = false;

                // Handle action-specific follow-up
                switch (action.Type)
                {
                    case QuickActionType.Apply:
                        ApplyRequested?.Invoke(this, CodeBlock);
                        break;
                    case QuickActionType.ShowDiff:
                        ShowDiffRequested?.Invoke(this, CodeBlock);
                        break;
                    case QuickActionType.ApplyWithOptions:
                        ShowOptionsRequested?.Invoke(this, CodeBlock);
                        break;
                    case QuickActionType.OpenFile when CodeBlock.TargetFilePath is not null:
                        OpenFileRequested?.Invoke(this, CodeBlock.TargetFilePath);
                        break;
                    case QuickActionType.Copy:
                        IsCopied = true;
                        break;
                }
            }
            else
            {
                StatusMessage = result.Message ?? "Action failed";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[EXEC] Failed to execute action {ActionId}", action.Id);
            StatusMessage = ex.Message;
            HasError = true;
        }
        finally
        {
            IsExecuting = false;
            CurrentAction = null;

            // Refresh availability after action
            RefreshAvailability();

            // Clear status after delay
            await ClearStatusAfterDelayAsync();
        }
    }

    /// <summary>
    /// Applies the code block.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        var action = _quickActionService.GetAction("apply");
        if (action is not null)
        {
            await ExecuteActionAsync(action);
        }
    }

    /// <summary>
    /// Copies the code block to clipboard.
    /// </summary>
    [RelayCommand]
    private async Task CopyAsync()
    {
        var action = _quickActionService.GetAction("copy");
        if (action is not null)
        {
            await ExecuteActionAsync(action);
        }
    }

    /// <summary>
    /// Shows the diff preview.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanShowDiff))]
    private async Task ShowDiffAsync()
    {
        var action = _quickActionService.GetAction("diff");
        if (action is not null)
        {
            await ExecuteActionAsync(action);
        }
    }

    /// <summary>
    /// Opens the target file in editor.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenInEditorAsync()
    {
        var action = _quickActionService.GetAction("open");
        if (action is not null)
        {
            await ExecuteActionAsync(action);
        }
    }

    /// <summary>
    /// Shows the apply options popup.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ShowOptionsAsync()
    {
        var action = _quickActionService.GetAction("options");
        if (action is not null)
        {
            await ExecuteActionAsync(action);
        }
    }

    /// <summary>
    /// Toggles the overflow menu.
    /// </summary>
    [RelayCommand]
    private void ToggleOverflow()
    {
        IsExpanded = !IsExpanded;
    }

    // ═══════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════

    private async Task ClearStatusAfterDelayAsync()
    {
        // Cancel any existing clear operation
        _statusClearCts?.Cancel();
        _statusClearCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(StatusDisplayDurationMs, _statusClearCts.Token);
            StatusMessage = null;
            HasError = false;
            IsCopied = false;
        }
        catch (TaskCanceledException)
        {
            // Expected when new action triggers before delay completes
        }
    }
}

/// <summary>
/// ViewModel wrapper for a single quick action item.
/// </summary>
public sealed class QuickActionItemViewModel
{
    private readonly CodeBlockQuickActionsViewModel _parent;

    public QuickAction Action { get; }
    public string Id => Action.Id;
    public string Label => Action.Label;
    public string Icon => Action.Icon;
    public string Tooltip => Action.Tooltip;
    public QuickActionType Type => Action.Type;

    public QuickActionItemViewModel(QuickAction action, CodeBlockQuickActionsViewModel parent)
    {
        Action = action;
        _parent = parent;
    }

    public IAsyncRelayCommand ExecuteCommand => _parent.ExecuteActionCommand;
}
