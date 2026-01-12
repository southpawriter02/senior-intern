using CommunityToolkit.Mvvm.ComponentModel;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// Base class for all ViewModels in the application.
/// Provides common functionality for busy state tracking and error handling.
/// </summary>
/// <remarks>
/// <para>
/// Uses CommunityToolkit.Mvvm source generators for observable properties.
/// The [ObservableProperty] attribute generates property change notifications automatically.
/// </para>
/// <para>
/// All derived ViewModels inherit:
/// <list type="bullet">
/// <item><see cref="IsBusy"/> - For loading indicators and UI disabling</item>
/// <item><see cref="ErrorMessage"/> - For displaying operation errors</item>
/// </list>
/// </para>
/// </remarks>
public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Gets or sets whether the ViewModel is currently performing a busy operation.
    /// Bind to this to show loading spinners or disable controls.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Gets or sets the current error message, or null if no error.
    /// Bind to this for error display in the UI.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Clears the current error message.
    /// Call this when starting a new operation that might error.
    /// </summary>
    protected void ClearError() => ErrorMessage = null;

    /// <summary>
    /// Sets an error message to display to the user.
    /// Call this when an operation fails to show feedback.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    protected void SetError(string message) => ErrorMessage = message;
}
