using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using AIntern.Desktop.Dialogs;
using AIntern.Desktop.Services;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// The main application window.
/// Hosts the chat panel, model selector, conversation list, and status bar.
/// </summary>
/// <remarks>
/// <para>
/// Layout structure:
/// <list type="bullet">
/// <item>SplitView with sidebar (left) containing ModelSelector and ConversationList</item>
/// <item>Main content area (right) containing ChatView</item>
/// <item>Status bar at bottom with model info and token statistics</item>
/// </list>
/// </para>
/// <para>
/// <b>Initialization Flow:</b>
/// <list type="number">
/// <item>Constructor: InitializeComponent() called</item>
/// <item>OnOpened: ViewModel.InitializeAsync() called to load data</item>
/// </list>
/// </para>
/// <para>
/// <b>Keyboard Shortcuts (defined in XAML):</b>
/// <list type="bullet">
/// <item>Ctrl+N - Create new conversation</item>
/// <item>Ctrl+S - Save current conversation</item>
/// <item>Ctrl+B - Toggle sidebar visibility</item>
/// <item>Ctrl+F - Focus search box</item>
/// </list>
/// </para>
/// <para>
/// <b>Keyboard Shortcuts (defined in code-behind):</b>
/// <list type="bullet">
/// <item>F2 - Rename selected conversation</item>
/// </list>
/// </para>
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// Optional logger instance for diagnostics.
    /// Injected via constructor when using DI container.
    /// </summary>
    private readonly ILogger<MainWindow>? _logger;

    /// <summary>
    /// Keyboard shortcut service for centralized key handling (v0.3.5g).
    /// </summary>
    private IKeyboardShortcutService? _shortcutService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor used by XAML designer and DI container.
    /// </remarks>
    public MainWindow()
    {
        InitializeComponent();
        InitializeDragDrop();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class with logging.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <remarks>
    /// Use this constructor when explicit logging is needed during development.
    /// </remarks>
    public MainWindow(ILogger<MainWindow>? logger)
    {
        _logger = logger;
        _logger?.LogDebug("[INIT] MainWindow constructor called");

        InitializeComponent();
        InitializeDragDrop();

        _logger?.LogDebug("[INIT] MainWindow InitializeComponent completed");
    }

    /// <summary>
    /// Called when the window is opened and visible.
    /// Initiates async initialization of the ViewModel.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    /// <remarks>
    /// <para>
    /// This override performs post-construction initialization that requires
    /// async operations, such as loading conversations from the database.
    /// </para>
    /// <para>
    /// The async void pattern is acceptable here because:
    /// <list type="bullet">
    /// <item>OnOpened is an event handler (void return required)</item>
    /// <item>Errors are caught and logged rather than propagated</item>
    /// <item>The window is already visible - failures degrade gracefully</item>
    /// </list>
    /// </para>
    /// </remarks>
    protected override async void OnOpened(EventArgs e)
    {
        // Call base implementation first (standard pattern)
        base.OnOpened(e);

        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] MainWindow.OnOpened");

        try
        {
            // Get the ViewModel from DataContext (set by DI container)
            if (DataContext is MainWindowViewModel viewModel)
            {
                _logger?.LogDebug("[INFO] Initializing MainWindowViewModel");

                // Set up owner window provider for dialogs (v0.2.2e)
                viewModel.ConversationListViewModel.SetOwnerWindowProvider(() => this);
                _logger?.LogDebug("[INFO] Owner window provider set for ConversationListViewModel");

                // Set up keyboard shortcut service (v0.3.5g)
                _shortcutService = viewModel.KeyboardShortcutService;
                if (_shortcutService != null)
                {
                    _shortcutService.CommandRequested += OnShortcutCommandRequested;
                    _logger?.LogDebug("[INFO] Keyboard shortcut service connected");
                }

                // Perform async initialization (loads settings and conversations)
                await viewModel.InitializeAsync();

                _logger?.LogInformation("[INFO] MainWindow initialized successfully");
            }
            else
            {
                _logger?.LogWarning("[WARN] DataContext is not MainWindowViewModel, skipping initialization");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash - window is already visible
            _logger?.LogError(ex, "[ERROR] MainWindow initialization failed");

            // Optionally show error to user (could use notification service in future)
            // For now, errors are displayed in the status bar via SetError
        }
        finally
        {
            _logger?.LogDebug("[EXIT] MainWindow.OnOpened - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Handles keyboard shortcut command requests (v0.3.5g).
    /// </summary>
    private void OnShortcutCommandRequested(object? sender, string commandId)
    {
        _logger?.LogDebug("[INFO] Shortcut command requested: {CommandId}", commandId);
        if (DataContext is MainWindowViewModel viewModel)
        {
            _ = viewModel.ExecuteCommandAsync(commandId);
        }
    }

    #region Window Close Handler (v0.2.2e)

    /// <summary>
    /// Called when the window is about to close.
    /// Checks for unsaved changes and prompts the user if necessary.
    /// </summary>
    /// <param name="e">The closing event arguments.</param>
    /// <remarks>
    /// <para>
    /// If there are unsaved changes in the current conversation, this handler:
    /// <list type="bullet">
    /// <item>Cancels the close operation</item>
    /// <item>Shows the UnsavedChangesDialog</item>
    /// <item>Handles Save/DontSave/Cancel based on user choice</item>
    /// </list>
    /// </para>
    /// <para>
    /// The async void pattern is acceptable here because OnClosing is an event
    /// handler with a void return type requirement.
    /// </para>
    /// </remarks>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] MainWindow.OnClosing");

        try
        {
            // Check if there are unsaved changes
            if (DataContext is MainWindowViewModel viewModel &&
                viewModel.ChatViewModel.HasUnsavedChanges)
            {
                _logger?.LogDebug("[INFO] Unsaved changes detected, showing dialog");

                // Cancel the close to show dialog
                e.Cancel = true;

                // Get conversation title for dialog
                var conversationTitle = viewModel.ChatViewModel.ConversationTitle
                    ?? "Untitled Conversation";

                // Show unsaved changes dialog
                var result = await UnsavedChangesDialog.ShowAsync(
                    this,
                    conversationTitle,
                    _logger);

                switch (result)
                {
                    case UnsavedChangesDialog.Result.Save:
                        _logger?.LogDebug("[INFO] User chose Save, saving conversation");
                        await viewModel.ChatViewModel.SaveCommand.ExecuteAsync(null);
                        // Close after saving
                        _logger?.LogDebug("[INFO] Save completed, closing window");
                        Close();
                        break;

                    case UnsavedChangesDialog.Result.DontSave:
                        _logger?.LogDebug("[INFO] User chose Don't Save, closing without saving");
                        // Mark as no longer having unsaved changes to allow close
                        viewModel.ChatViewModel.ClearUnsavedChangesFlag();
                        Close();
                        break;

                    case UnsavedChangesDialog.Result.Cancel:
                        _logger?.LogDebug("[INFO] User chose Cancel, aborting close");
                        // Do nothing - close was already cancelled
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
            // Allow close on error to prevent user from being stuck
            base.OnClosing(e);
        }
        finally
        {
            _logger?.LogDebug("[EXIT] MainWindow.OnClosing - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    #endregion

    #region Keyboard Handler (v0.2.2e)

    /// <summary>
    /// Handles key down events for shortcuts not defined in XAML.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <para>
    /// This handler provides F2 support for renaming conversations.
    /// Other shortcuts (Ctrl+N, Ctrl+S, Ctrl+B, Ctrl+F) are defined in XAML.
    /// </para>
    /// <para>
    /// F2 is handled in code-behind because it requires access to the
    /// ConversationListViewModel to invoke BeginRename on the selected item.
    /// </para>
    /// </remarks>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _logger?.LogDebug("[INFO] MainWindow.OnKeyDown - Key: {Key}, Mods: {Mods}", e.Key, e.KeyModifiers);

        try
        {
            // Try centralized shortcut service first (v0.3.5g)
            if (_shortcutService?.HandleKeyPress(e) == true)
            {
                e.Handled = true;
                return;
            }

            // F2: Begin rename of selected conversation (legacy fallback)
            if (e.Key == Key.F2)
            {
                _logger?.LogDebug("[INFO] F2 pressed, attempting to begin rename");

                if (DataContext is MainWindowViewModel viewModel)
                {
                    var selectedConversation = viewModel.ConversationListViewModel.SelectedConversation;
                    if (selectedConversation != null)
                    {
                        _logger?.LogDebug(
                            "[INFO] Beginning rename for conversation: {ConversationId}",
                            selectedConversation.Id);

                        viewModel.ConversationListViewModel.RenameConversationCommand.Execute(selectedConversation);
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        _logger?.LogDebug("[SKIP] No conversation selected for F2 rename");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnKeyDown handler failed: {Message}", ex.Message);
        }

        base.OnKeyDown(e);
    }

    #endregion

    #region Drag-Drop Handler (v0.3.5f)

    /// <summary>
    /// Initializes drag-drop event handlers.
    /// </summary>
    private void InitializeDragDrop()
    {
        _logger?.LogDebug("[INIT] Setting up drag-drop handlers");
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    /// <summary>
    /// Shows drop overlay when files are dragged into the window.
    /// </summary>
    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.FileNames))
        {
            _logger?.LogDebug("[INFO] DragEnter: File(s) detected");
            ShowDropOverlay(true);
        }
    }

    /// <summary>
    /// Hides drop overlay when files are dragged out of the window.
    /// </summary>
    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        _logger?.LogDebug("[INFO] DragLeave");
        ShowDropOverlay(false);
    }

    /// <summary>
    /// Updates overlay message based on dropped content type.
    /// </summary>
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.FileNames))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var files = e.Data.GetFileNames()?.ToList();
        if (files == null || files.Count == 0)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        // Determine content type
        var hasFolder = files.Any(Directory.Exists);
        var hasFiles = files.Any(File.Exists);

        if (hasFolder)
        {
            e.DragEffects = DragDropEffects.Link;
            UpdateDropOverlay("Open as workspace");
        }
        else if (hasFiles)
        {
            var count = files.Count(File.Exists);
            e.DragEffects = DragDropEffects.Copy;
            UpdateDropOverlay(count == 1
                ? $"Open {Path.GetFileName(files.First(File.Exists))}"
                : $"Open {count} files");
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    /// <summary>
    /// Processes dropped files and folders.
    /// </summary>
    private void OnDrop(object? sender, DragEventArgs e)
    {
        ShowDropOverlay(false);

        if (!e.Data.Contains(DataFormats.FileNames)) return;

        var files = e.Data.GetFileNames()?.ToList();
        if (files == null || files.Count == 0) return;

        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel == null) return;

        _logger?.LogDebug("[INFO] OnDrop: Processing {Count} items", files.Count);

        // Process folders (open as workspace via FileExplorer's command)
        var folders = files.Where(Directory.Exists).ToList();
        if (folders.Count > 0)
        {
            _logger?.LogDebug("[INFO] OnDrop: Opening folder as workspace: {Path}", folders[0]);
            // Use FileExplorer to open workspace (it has access to workspace service)
            viewModel.FileExplorer.OpenFolderByPath(folders[0]);
        }

        // Process files (open in editor via FileOpenRequested event)
        var fileList = files.Where(File.Exists).ToList();
        foreach (var file in fileList)
        {
            _logger?.LogDebug("[INFO] OnDrop: Opening file: {Path}", file);
            viewModel.FileExplorer.RequestOpenFile(file);
        }
    }

    /// <summary>
    /// Shows or hides the drop overlay.
    /// </summary>
    private void ShowDropOverlay(bool show)
    {
        DropOverlay.IsVisible = show;
    }

    /// <summary>
    /// Updates the drop overlay message text.
    /// </summary>
    private void UpdateDropOverlay(string message)
    {
        DropOverlayText.Text = message;
    }

    #endregion
}
