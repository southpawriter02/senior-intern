using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// The chat interface view displaying messages and input controls.
/// Handles keyboard shortcuts for message submission and system prompt editor access.
/// </summary>
/// <remarks>
/// <para>
/// <b>Layout Structure (v0.2.4e):</b>
/// <list type="bullet">
///   <item>Row 0: System prompt selector header with dropdown</item>
///   <item>Row 1: System prompt content expander (collapsible)</item>
///   <item>Row 2: Message list with scrolling</item>
///   <item>Row 3: Input area with send/stop buttons</item>
/// </list>
/// </para>
/// <para>
/// <b>Event Handling:</b>
/// <list type="bullet">
///   <item>Enter key: Submits the current message</item>
///   <item>Edit button click: Opens the SystemPromptEditorWindow</item>
///   <item>Drag-drop: Attaches files to chat context (v0.3.4g)</item>
/// </list>
/// </para>
/// </remarks>
public partial class ChatView : UserControl
{
    #region Fields

    /// <summary>
    /// Logger for exhaustive operation tracking.
    /// </summary>
    private readonly ILogger<ChatView>? _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatView"/> class.
    /// </summary>
    /// <remarks>
    /// Default constructor used by XAML instantiation.
    /// </remarks>
    public ChatView() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatView"/> class
    /// with optional logging support.
    /// </summary>
    /// <param name="logger">Optional logger for operation tracking.</param>
    /// <remarks>
    /// <para>
    /// This constructor allows dependency injection of a logger for detailed
    /// operation tracking during development and debugging.
    /// </para>
    /// <para>
    /// Subscribes to the <see cref="SystemPromptSelector.EditButtonClick"/> event
    /// from the embedded PromptSelector control to handle editor window opening.
    /// </para>
    /// </remarks>
    public ChatView(ILogger<ChatView>? logger)
    {
        var sw = Stopwatch.StartNew();
        _logger = logger;

        _logger?.LogDebug("[INIT] ChatView construction started");

        // Load the XAML-defined UI components
        InitializeComponent();

        // Wire up the SystemPromptSelector's Edit button click handler.
        // The selector is defined in XAML with x:Name="PromptSelector".
        if (this.FindControl<SystemPromptSelector>("PromptSelector") is { } selector)
        {
            selector.EditButtonClick += OnPromptSelectorEditButtonClick;
            _logger?.LogDebug("[INIT] PromptSelector EditButtonClick handler attached");
        }
        else
        {
            _logger?.LogWarning("[INIT] PromptSelector not found in template");
        }

        // Initialize drag-drop handlers (v0.3.4g)
        InitializeDragDrop();

        sw.Stop();
        _logger?.LogDebug("[INIT] ChatView construction completed - {ElapsedMs}ms",
            sw.ElapsedMilliseconds);
    }

    #endregion

    #region Drag-Drop (v0.3.4g)

    /// <summary>
    /// Initializes drag-drop event handlers.
    /// </summary>
    private void InitializeDragDrop()
    {
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);

        _logger?.LogDebug("[INIT] Drag-drop handlers attached");
    }

    /// <summary>
    /// Handles drag enter - shows the drop zone indicator.
    /// </summary>
    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            DropZoneIndicator.IsVisible = true;
            _logger?.LogDebug("[DRAG] Drag enter with files");
        }
    }

    /// <summary>
    /// Handles drag over - sets the drag effect.
    /// </summary>
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Link;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    /// <summary>
    /// Handles drag leave - hides the drop zone indicator.
    /// </summary>
    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        DropZoneIndicator.IsVisible = false;
        _logger?.LogDebug("[DRAG] Drag leave");
    }

    /// <summary>
    /// Handles file drop - attaches files to chat context.
    /// </summary>
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        DropZoneIndicator.IsVisible = false;

        if (!e.Data.Contains(DataFormats.Files))
            return;

        // Access MainWindowViewModel through the visual tree
        var window = TopLevel.GetTopLevel(this) as Window;
        var mainViewModel = window?.DataContext as MainWindowViewModel;
        if (mainViewModel == null)
        {
            _logger?.LogWarning("[DROP] Could not find MainWindowViewModel");
            return;
        }

        var files = e.Data.GetFiles()?.ToList();
        if (files == null || files.Count == 0)
            return;

        _logger?.LogDebug("[DROP] Processing {Count} dropped items", files.Count);

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            // Only attach files, not directories
            if (File.Exists(path))
            {
                _logger?.LogDebug("[DROP] Attaching file: {Path}", path);
                await mainViewModel.AttachFileAsync(path);
            }
            else
            {
                _logger?.LogDebug("[DROP] Skipping directory: {Path}", path);
            }
        }

        e.Handled = true;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the KeyDown event on the input TextBox.
    /// Submits the message when Enter is pressed (without Shift modifier).
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <para>
    /// Enter key behavior:
    /// <list type="bullet">
    ///   <item>Enter alone: Submits the message via <see cref="ChatViewModel.HandleEnterKey"/></item>
    ///   <item>Shift+Enter: Allows multi-line input (default TextBox behavior)</item>
    /// </list>
    /// </para>
    /// </remarks>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        // Check for Enter key without Shift modifier
        // Shift+Enter allows for multi-line input
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _logger?.LogDebug("[INFO] Enter key pressed - attempting message send");

            // Access the ViewModel through DataContext binding
            if (DataContext is ChatViewModel viewModel)
            {
                // Delegate to ViewModel which checks CanSend and executes command
                viewModel.HandleEnterKey();

                // Mark event as handled to prevent default Enter behavior
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Handles the Edit button click from the SystemPromptSelector control.
    /// Opens the SystemPromptEditorWindow as a dialog.
    /// </summary>
    /// <param name="sender">The event source (SystemPromptSelector).</param>
    /// <param name="e">Event arguments.</param>
    /// <remarks>
    /// <para>
    /// This handler delegates to the MainWindowViewModel's OpenSystemPromptEditorCommand
    /// to handle the actual window creation and display.
    /// </para>
    /// <para>
    /// The command is accessed through the visual tree by finding the MainWindow's
    /// DataContext.
    /// </para>
    /// </remarks>
    private void OnPromptSelectorEditButtonClick(object? sender, RoutedEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnPromptSelectorEditButtonClick");

        try
        {
            // Find the MainWindow to access the command for opening the editor.
            // Walk up the visual tree to find the window.
            var window = TopLevel.GetTopLevel(this) as Window;

            if (window?.DataContext is MainWindowViewModel mainViewModel)
            {
                _logger?.LogDebug("[INFO] Found MainWindowViewModel, executing OpenSystemPromptEditorCommand");

                // Execute the command if available.
                if (mainViewModel.OpenSystemPromptEditorCommand.CanExecute(null))
                {
                    mainViewModel.OpenSystemPromptEditorCommand.Execute(null);
                    _logger?.LogDebug("[INFO] OpenSystemPromptEditorCommand executed");
                }
                else
                {
                    _logger?.LogWarning("[WARN] OpenSystemPromptEditorCommand cannot execute");
                }
            }
            else
            {
                _logger?.LogWarning("[WARN] Could not find MainWindowViewModel to open editor");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OnPromptSelectorEditButtonClick failed: {Message}", ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OnPromptSelectorEditButtonClick - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    #endregion
}
