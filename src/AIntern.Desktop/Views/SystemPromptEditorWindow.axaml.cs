using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using AIntern.Desktop.Dialogs;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// Code-behind for the SystemPromptEditorWindow providing lifecycle management,
/// keyboard shortcuts, and unsaved changes handling.
/// </summary>
/// <remarks>
/// <para>
/// This window hosts the <see cref="SystemPromptEditorViewModel"/> and provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Lifecycle Management:</b> Initializes ViewModel on open, disposes on close</description></item>
///   <item><description><b>Keyboard Shortcuts:</b> Ctrl+S (save), Ctrl+N (new), Escape (discard/close)</description></item>
///   <item><description><b>Unsaved Changes:</b> Prompts user before closing with dirty state</description></item>
/// </list>
/// <para>
/// <b>Keyboard Shortcuts:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Ctrl+S - Save current prompt (if CanSave)</description></item>
///   <item><description>Ctrl+N - Create new prompt</description></item>
///   <item><description>Escape - Discard changes if dirty, otherwise close window</description></item>
/// </list>
/// <para>
/// <b>Unsaved Changes Flow:</b>
/// </para>
/// <para>
/// When the user attempts to close the window with unsaved changes (IsDirty=true),
/// the <see cref="UnsavedChangesDialog"/> is shown with Save/Don't Save/Cancel options.
/// </para>
/// <para>
/// <b>Logging:</b>
/// </para>
/// <list type="bullet">
///   <item><description>[ENTER] - Method entry with parameters</description></item>
///   <item><description>[INFO] - Significant state changes</description></item>
///   <item><description>[SKIP] - No-op conditions</description></item>
///   <item><description>[EXIT] - Method completion with duration</description></item>
///   <item><description>[ERROR] - Exception handling</description></item>
///   <item><description>[INIT] - Constructor completion</description></item>
///   <item><description>[DISPOSE] - Resource cleanup</description></item>
/// </list>
/// <para>Added in v0.2.4d.</para>
/// </remarks>
/// <seealso cref="SystemPromptEditorViewModel"/>
/// <seealso cref="UnsavedChangesDialog"/>
public partial class SystemPromptEditorWindow : Window, IDisposable
{
    #region Fields

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    private readonly ILogger<SystemPromptEditorWindow>? _logger;

    /// <summary>
    /// Flag indicating whether we're closing via code (after handling unsaved changes).
    /// Used to prevent re-showing the unsaved changes dialog.
    /// </summary>
    private bool _isClosingFromCode;

    /// <summary>
    /// Flag indicating whether the window has been disposed.
    /// </summary>
    private bool _isDisposed;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the typed ViewModel from DataContext.
    /// </summary>
    /// <value>
    /// The <see cref="SystemPromptEditorViewModel"/> bound to this window, or null if not set.
    /// </value>
    private SystemPromptEditorViewModel? ViewModel => DataContext as SystemPromptEditorViewModel;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptEditorWindow"/> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor for XAML designer and DI container.
    /// </remarks>
    public SystemPromptEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptEditorWindow"/> class with logging.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <remarks>
    /// Use this constructor when explicit logging is needed for debugging.
    /// </remarks>
    public SystemPromptEditorWindow(ILogger<SystemPromptEditorWindow>? logger)
    {
        _logger = logger;
        _logger?.LogDebug("[INIT] SystemPromptEditorWindow constructor called");

        InitializeComponent();

        _logger?.LogDebug("[INIT] SystemPromptEditorWindow InitializeComponent completed");
    }

    #endregion

    #region Window Lifecycle

    /// <summary>
    /// Called when the window is opened and visible.
    /// Initiates async initialization of the ViewModel.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    /// <remarks>
    /// <para>
    /// Calls <see cref="SystemPromptEditorViewModel.InitializeAsync"/> to load prompts
    /// from the service. The async void pattern is acceptable here because OnOpened
    /// is an event handler with a void return type requirement.
    /// </para>
    /// </remarks>
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnOpened");

        try
        {
            if (ViewModel != null)
            {
                _logger?.LogDebug("[INFO] Initializing SystemPromptEditorViewModel via InitializeCommand");
                await ViewModel.InitializeCommand.ExecuteAsync(null);
                _logger?.LogInformation("[INFO] SystemPromptEditorWindow initialized successfully");
            }
            else
            {
                _logger?.LogWarning("[SKIP] DataContext is not SystemPromptEditorViewModel");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnOpened initialization failed: {Message}", ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OnOpened - Duration: {Ms}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Called when the window is about to close.
    /// Checks for unsaved changes and prompts the user if necessary.
    /// </summary>
    /// <param name="e">The closing event arguments.</param>
    /// <remarks>
    /// <para>
    /// If there are unsaved changes (IsDirty=true), this handler:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Cancels the close operation</description></item>
    ///   <item><description>Shows the <see cref="UnsavedChangesDialog"/></description></item>
    ///   <item><description>Handles Save/DontSave/Cancel based on user choice</description></item>
    /// </list>
    /// <para>
    /// The <c>_isClosingFromCode</c> flag prevents re-showing the dialog when
    /// we call Close() programmatically after handling the dialog result.
    /// </para>
    /// </remarks>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnClosing - IsDirty: {IsDirty}, IsClosingFromCode: {IsClosing}",
            ViewModel?.IsDirty, _isClosingFromCode);

        try
        {
            // Skip dialog if already handled via code close.
            if (_isClosingFromCode)
            {
                _logger?.LogDebug("[SKIP] Already closing from code, proceeding with close");
                base.OnClosing(e);
                return;
            }

            // Check for unsaved changes.
            if (ViewModel?.IsDirty == true)
            {
                _logger?.LogDebug("[INFO] Unsaved changes detected, showing dialog");
                e.Cancel = true;

                var result = await UnsavedChangesDialog.ShowAsync(
                    this,
                    ViewModel.PromptName ?? "System Prompt",
                    _logger);

                switch (result)
                {
                    case UnsavedChangesDialog.Result.Save:
                        _logger?.LogDebug("[INFO] User chose Save, saving prompt");
                        if (ViewModel.SavePromptCommand.CanExecute(null))
                        {
                            await ViewModel.SavePromptCommand.ExecuteAsync(null);
                        }
                        _isClosingFromCode = true;
                        Close();
                        break;

                    case UnsavedChangesDialog.Result.DontSave:
                        _logger?.LogDebug("[INFO] User chose Don't Save, closing without saving");
                        _isClosingFromCode = true;
                        Close();
                        break;

                    case UnsavedChangesDialog.Result.Cancel:
                        _logger?.LogDebug("[INFO] User chose Cancel, aborting close");
                        // Do nothing - close was already cancelled.
                        break;
                }
            }
            else
            {
                _logger?.LogDebug("[SKIP] No unsaved changes, allowing close");
                base.OnClosing(e);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnClosing handler failed: {Message}", ex.Message);
            // Allow close on error to prevent user from being stuck.
            base.OnClosing(e);
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OnClosing - Duration: {Ms}ms", sw.ElapsedMilliseconds);
        }
    }

    #endregion

    #region Keyboard Handling

    /// <summary>
    /// Handles key down events for keyboard shortcuts.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <para>
    /// Keyboard shortcuts handled:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Ctrl+S - Save current prompt (if CanSave)</description></item>
    ///   <item><description>Ctrl+N - Create new prompt</description></item>
    ///   <item><description>Escape - Discard changes if dirty, otherwise close window</description></item>
    /// </list>
    /// </remarks>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnKeyDown - Key: {Key}, Modifiers: {Modifiers}",
            e.Key, e.KeyModifiers);

        try
        {
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                switch (e.Key)
                {
                    case Key.S:
                        _logger?.LogDebug("[INFO] Ctrl+S pressed - invoking SavePromptCommand");
                        if (ViewModel?.SavePromptCommand.CanExecute(null) == true)
                        {
                            _ = ViewModel.SavePromptCommand.ExecuteAsync(null);
                        }
                        else
                        {
                            _logger?.LogDebug("[SKIP] SavePromptCommand cannot execute");
                        }
                        e.Handled = true;
                        break;

                    case Key.N:
                        _logger?.LogDebug("[INFO] Ctrl+N pressed - invoking CreateNewPromptCommand");
                        if (ViewModel?.CreateNewPromptCommand.CanExecute(null) == true)
                        {
                            _ = ViewModel.CreateNewPromptCommand.ExecuteAsync(null);
                        }
                        else
                        {
                            _logger?.LogDebug("[SKIP] CreateNewPromptCommand cannot execute");
                        }
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.Escape)
            {
                _logger?.LogDebug("[INFO] Escape pressed - IsDirty: {IsDirty}", ViewModel?.IsDirty);
                if (ViewModel?.IsDirty == true)
                {
                    // Discard changes when dirty.
                    _logger?.LogDebug("[INFO] Discarding changes");
                    ViewModel.DiscardChangesCommand.Execute(null);
                }
                else
                {
                    // Close window when not dirty.
                    _logger?.LogDebug("[INFO] Closing window (no unsaved changes)");
                    Close();
                }
                e.Handled = true;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnKeyDown handler failed: {Message}", ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OnKeyDown - Duration: {Ms}ms", sw.ElapsedMilliseconds);
        }

        base.OnKeyDown(e);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the window and its ViewModel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Disposes the <see cref="SystemPromptEditorViewModel"/> to unsubscribe from
    /// service events and prevent memory leaks.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _logger?.LogDebug("[DISPOSE] SystemPromptEditorWindow - Disposing");

        // Dispose the ViewModel to unsubscribe from events.
        if (ViewModel is IDisposable disposableViewModel)
        {
            disposableViewModel.Dispose();
        }

        _isDisposed = true;

        _logger?.LogDebug("[DISPOSE] SystemPromptEditorWindow - Disposal complete");
    }

    #endregion
}
