using AIntern.Core.Models;

namespace AIntern.Desktop.Services;

/// <summary>
/// Service for managing and executing quick actions on code blocks (v0.4.5g).
/// </summary>
public interface IQuickActionService
{
    /// <summary>
    /// Gets all actions available for the specified code block.
    /// </summary>
    /// <param name="block">The code block to get actions for</param>
    /// <returns>Available actions sorted by priority</returns>
    IEnumerable<QuickAction> GetAvailableActions(CodeBlock block);

    /// <summary>
    /// Gets all registered actions regardless of availability.
    /// </summary>
    IEnumerable<QuickAction> GetAllActions();

    /// <summary>
    /// Executes the specified action on a code block.
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <param name="block">The target code block</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the action execution</returns>
    Task<QuickActionResult> ExecuteAsync(
        QuickAction action,
        CodeBlock block,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action by its ID.
    /// </summary>
    /// <param name="actionId">The action identifier</param>
    /// <param name="block">The target code block</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the action execution</returns>
    Task<QuickActionResult> ExecuteByIdAsync(
        string actionId,
        CodeBlock block,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a custom action.
    /// </summary>
    /// <param name="action">The action to register</param>
    void RegisterAction(QuickAction action);

    /// <summary>
    /// Unregisters an action by its ID.
    /// </summary>
    /// <param name="actionId">The action ID to remove</param>
    /// <returns>True if the action was removed</returns>
    bool UnregisterAction(string actionId);

    /// <summary>
    /// Gets an action by its ID.
    /// </summary>
    /// <param name="actionId">The action identifier</param>
    /// <returns>The action or null if not found</returns>
    QuickAction? GetAction(string actionId);

    /// <summary>
    /// Raised before an action is executed.
    /// </summary>
    event EventHandler<QuickActionExecutingEventArgs>? ActionExecuting;

    /// <summary>
    /// Raised after an action is executed.
    /// </summary>
    event EventHandler<QuickActionExecutedEventArgs>? ActionExecuted;
}

/// <summary>
/// Event args for ActionExecuting event.
/// </summary>
public sealed class QuickActionExecutingEventArgs : EventArgs
{
    public required QuickAction Action { get; init; }
    public required CodeBlock Block { get; init; }
    public bool Cancel { get; set; }
}

/// <summary>
/// Event args for ActionExecuted event.
/// </summary>
public sealed class QuickActionExecutedEventArgs : EventArgs
{
    public required QuickAction Action { get; init; }
    public required CodeBlock Block { get; init; }
    public required QuickActionResult Result { get; init; }
}
