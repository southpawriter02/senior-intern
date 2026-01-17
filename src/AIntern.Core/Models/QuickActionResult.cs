namespace AIntern.Core.Models;

/// <summary>
/// Result of executing a quick action (v0.4.5g).
/// </summary>
/// <param name="IsSuccess">Whether the action completed successfully</param>
/// <param name="ActionType">The type of action that was executed</param>
/// <param name="Message">Success message or error description</param>
/// <param name="Data">Action-specific result data</param>
/// <param name="Duration">Time taken to execute the action</param>
public sealed record QuickActionResult(
    bool IsSuccess,
    QuickActionType ActionType,
    string? Message = null,
    object? Data = null,
    TimeSpan Duration = default)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static QuickActionResult Success(
        QuickActionType actionType,
        string? message = null,
        object? data = null,
        TimeSpan duration = default) => new(
            IsSuccess: true,
            ActionType: actionType,
            Message: message,
            Data: data,
            Duration: duration);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static QuickActionResult Failure(
        QuickActionType actionType,
        string message,
        TimeSpan duration = default) => new(
            IsSuccess: false,
            ActionType: actionType,
            Message: message,
            Duration: duration);

    /// <summary>
    /// User-friendly message based on action type and result.
    /// </summary>
    public string DisplayMessage => IsSuccess
        ? ActionType switch
        {
            QuickActionType.Apply => "Applied!",
            QuickActionType.Copy => "Copied!",
            QuickActionType.ShowDiff => "Showing diff...",
            QuickActionType.OpenFile => "Opened!",
            QuickActionType.ApplyWithOptions => "Options opened",
            QuickActionType.Reject => "Rejected",
            QuickActionType.RunCommand => "Running...",
            QuickActionType.InsertAtCursor => "Inserted!",
            _ => Message ?? "Done!"
        }
        : Message ?? "Action failed";
}
