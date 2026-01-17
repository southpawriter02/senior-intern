using System.Collections.Concurrent;
using System.Diagnostics;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Services;

/// <summary>
/// Implements quick action management and execution (v0.4.5g).
/// </summary>
public sealed class QuickActionService : IQuickActionService
{
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<QuickActionService>? _logger;

    private readonly ConcurrentDictionary<string, QuickAction> _actions = new();

    /// <inheritdoc />
    public event EventHandler<QuickActionExecutingEventArgs>? ActionExecuting;

    /// <inheritdoc />
    public event EventHandler<QuickActionExecutedEventArgs>? ActionExecuted;

    /// <summary>
    /// Initializes the quick action service with default actions.
    /// </summary>
    public QuickActionService(
        IClipboardService clipboardService,
        ILogger<QuickActionService>? logger = null)
    {
        _clipboardService = clipboardService;
        _logger = logger;

        _logger?.LogDebug("[INIT] QuickActionService v0.4.5g initializing");
        RegisterDefaultActions();
        _logger?.LogInformation(
            "[INIT] QuickActionService registered {Count} default actions",
            _actions.Count);
    }

    /// <summary>
    /// Registers all built-in quick actions.
    /// </summary>
    private void RegisterDefaultActions()
    {
        RegisterAction(QuickAction.Apply());
        RegisterAction(QuickAction.Copy());
        RegisterAction(QuickAction.ShowDiff());
        RegisterAction(QuickAction.OpenInEditor());
        RegisterAction(QuickAction.ApplyWithOptions());
        RegisterAction(QuickAction.Reject());
        RegisterAction(QuickAction.RunCommand());
        RegisterAction(QuickAction.InsertAtCursor());
    }

    /// <inheritdoc />
    public IEnumerable<QuickAction> GetAvailableActions(CodeBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        return _actions.Values
            .Where(action => action.IsEnabled(block))
            .OrderBy(action => action.Priority);
    }

    /// <inheritdoc />
    public IEnumerable<QuickAction> GetAllActions() =>
        _actions.Values.OrderBy(a => a.Priority);

    /// <inheritdoc />
    public async Task<QuickActionResult> ExecuteAsync(
        QuickAction action,
        CodeBlock block,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(block);

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug(
            "[EXEC] Executing action {ActionId} on block {BlockId}",
            action.Id, block.Id);

        // Raise executing event
        var executingArgs = new QuickActionExecutingEventArgs
        {
            Action = action,
            Block = block
        };
        ActionExecuting?.Invoke(this, executingArgs);

        if (executingArgs.Cancel)
        {
            _logger?.LogDebug("[EXEC] Action {ActionId} was cancelled", action.Id);
            return QuickActionResult.Failure(
                action.Type,
                "Action was cancelled",
                stopwatch.Elapsed);
        }

        QuickActionResult result;
        try
        {
            result = action.Type switch
            {
                QuickActionType.Apply => ExecuteApply(block),
                QuickActionType.Copy => await ExecuteCopyAsync(block, cancellationToken),
                QuickActionType.ShowDiff => ExecuteShowDiff(block),
                QuickActionType.OpenFile => ExecuteOpenFile(block),
                QuickActionType.ApplyWithOptions => ExecuteApplyWithOptions(block),
                QuickActionType.Reject => ExecuteReject(block),
                QuickActionType.RunCommand => ExecuteRunCommand(block),
                QuickActionType.InsertAtCursor => ExecuteInsertAtCursor(block),
                _ => QuickActionResult.Failure(action.Type, $"Unknown action type: {action.Type}")
            };
        }
        catch (OperationCanceledException)
        {
            result = QuickActionResult.Failure(
                action.Type,
                "Operation was cancelled",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[EXEC] Action {ActionId} failed", action.Id);
            result = QuickActionResult.Failure(
                action.Type,
                ex.Message,
                stopwatch.Elapsed);
        }

        stopwatch.Stop();
        result = result with { Duration = stopwatch.Elapsed };

        _logger?.LogDebug(
            "[EXEC] Action {ActionId} completed: Success={Success}, Duration={Duration}ms",
            action.Id, result.IsSuccess, result.Duration.TotalMilliseconds);

        // Raise executed event
        ActionExecuted?.Invoke(this, new QuickActionExecutedEventArgs
        {
            Action = action,
            Block = block,
            Result = result
        });

        return result;
    }

    /// <inheritdoc />
    public async Task<QuickActionResult> ExecuteByIdAsync(
        string actionId,
        CodeBlock block,
        CancellationToken cancellationToken = default)
    {
        var action = GetAction(actionId);
        if (action is null)
        {
            _logger?.LogWarning("[EXEC] Unknown action ID: {ActionId}", actionId);
            return QuickActionResult.Failure(
                QuickActionType.Apply,
                $"Unknown action: {actionId}");
        }

        return await ExecuteAsync(action, block, cancellationToken);
    }

    /// <inheritdoc />
    public void RegisterAction(QuickAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions[action.Id] = action;
        _logger?.LogDebug("[REG] Registered action: {ActionId}", action.Id);
    }

    /// <inheritdoc />
    public bool UnregisterAction(string actionId)
    {
        var removed = _actions.TryRemove(actionId, out _);
        if (removed)
        {
            _logger?.LogDebug("[REG] Unregistered action: {ActionId}", actionId);
        }
        return removed;
    }

    /// <inheritdoc />
    public QuickAction? GetAction(string actionId) =>
        _actions.TryGetValue(actionId, out var action) ? action : null;

    // ═══════════════════════════════════════════════════════════════
    // Action Implementations
    // ═══════════════════════════════════════════════════════════════

    private QuickActionResult ExecuteApply(CodeBlock block)
    {
        // Full apply is handled by the ViewModel which calls IFileChangeService
        // Here we just validate and signal that apply should proceed
        if (string.IsNullOrEmpty(block.TargetFilePath))
        {
            return QuickActionResult.Failure(
                QuickActionType.Apply,
                "No target file path specified");
        }

        // Return success with the block as data for ViewModel to handle
        return QuickActionResult.Success(
            QuickActionType.Apply,
            data: block);
    }

    private async Task<QuickActionResult> ExecuteCopyAsync(
        CodeBlock block,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(block.Content))
        {
            return QuickActionResult.Failure(
                QuickActionType.Copy,
                "Code block is empty");
        }

        await _clipboardService.SetTextAsync(block.Content);
        _logger?.LogDebug("[COPY] Copied {Length} chars to clipboard", block.Content.Length);

        return QuickActionResult.Success(QuickActionType.Copy);
    }

    private QuickActionResult ExecuteShowDiff(CodeBlock block)
    {
        if (string.IsNullOrEmpty(block.TargetFilePath))
        {
            return QuickActionResult.Failure(
                QuickActionType.ShowDiff,
                "No target file to compare against");
        }

        // Return success - ViewModel handles showing the diff
        return QuickActionResult.Success(
            QuickActionType.ShowDiff,
            data: block);
    }

    private QuickActionResult ExecuteOpenFile(CodeBlock block)
    {
        if (string.IsNullOrEmpty(block.TargetFilePath))
        {
            return QuickActionResult.Failure(
                QuickActionType.OpenFile,
                "No target file specified");
        }

        // Return success - ViewModel handles opening the file
        return QuickActionResult.Success(
            QuickActionType.OpenFile,
            data: block);
    }

    private QuickActionResult ExecuteApplyWithOptions(CodeBlock block)
    {
        if (!block.IsApplicable)
        {
            return QuickActionResult.Failure(
                QuickActionType.ApplyWithOptions,
                "Block is not applicable");
        }

        // Return success - ViewModel handles showing the options popup
        return QuickActionResult.Success(
            QuickActionType.ApplyWithOptions,
            data: block);
    }

    private QuickActionResult ExecuteReject(CodeBlock block)
    {
        if (block.Status != CodeBlockStatus.Pending)
        {
            return QuickActionResult.Failure(
                QuickActionType.Reject,
                "Block is not pending");
        }

        block.Status = CodeBlockStatus.Rejected;
        _logger?.LogDebug("[REJECT] Marked block {BlockId} as rejected", block.Id);

        return QuickActionResult.Success(QuickActionType.Reject);
    }

    private QuickActionResult ExecuteRunCommand(CodeBlock block)
    {
        if (block.BlockType != CodeBlockType.Command)
        {
            return QuickActionResult.Failure(
                QuickActionType.RunCommand,
                "Block is not a command");
        }

        // Return success - ViewModel handles terminal execution
        return QuickActionResult.Success(
            QuickActionType.RunCommand,
            data: block);
    }

    private QuickActionResult ExecuteInsertAtCursor(CodeBlock block)
    {
        if (string.IsNullOrEmpty(block.Content))
        {
            return QuickActionResult.Failure(
                QuickActionType.InsertAtCursor,
                "Code block is empty");
        }

        // Return success - ViewModel handles editor insertion
        return QuickActionResult.Success(
            QuickActionType.InsertAtCursor,
            data: block);
    }
}
