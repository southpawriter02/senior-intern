using System.Diagnostics;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.Views;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// Root ViewModel for the main application window.
/// Coordinates child ViewModels and manages overall application state.
/// </summary>
/// <remarks>
/// <para>
/// Acts as the composition root for all UI ViewModels:
/// <list type="bullet">
/// <item><see cref="ChatViewModel"/> - Chat message panel</item>
/// <item><see cref="ModelSelectorViewModel"/> - Model file selection</item>
/// <item><see cref="ConversationListViewModel"/> - Sidebar conversation list</item>
/// <item><see cref="InferenceSettingsViewModel"/> - Inference parameter sliders (v0.2.3e)</item>
/// <item><see cref="FileExplorerViewModel"/> - File explorer sidebar (v0.3.2g)</item>
/// </list>
/// </para>
/// <para>
/// <b>Initialization:</b>
/// The <see cref="InitializeAsync"/> method must be called after construction
/// to load settings and populate the conversation list. This is typically
/// done in the view's OnOpened event.
/// </para>
/// <para>
/// <b>Event Subscriptions:</b>
/// <list type="bullet">
/// <item><see cref="ILlmService.ModelStateChanged"/> - Updates status bar with model info</item>
/// <item><see cref="ILlmService.InferenceProgress"/> - Updates token statistics display</item>
/// </list>
/// </para>
/// <para>
/// <b>v0.2.4e Additions:</b>
/// <list type="bullet">
/// <item><see cref="OpenSystemPromptEditorCommand"/> - Opens the SystemPromptEditorWindow</item>
/// <item>Requires <see cref="ISystemPromptService"/> dependency for editor ViewModel construction</item>
/// </list>
/// </para>
/// <para>
/// <b>v0.2.5e Additions:</b>
/// <list type="bullet">
/// <item><see cref="OpenSearchCommand"/> - Opens the SearchDialog (Ctrl+K)</item>
/// <item>Requires <see cref="ISearchService"/> dependency for search ViewModel construction</item>
/// </list>
/// </para>
/// <para>
/// <b>v0.2.5f Additions:</b>
/// <list type="bullet">
/// <item><see cref="OpenExportCommand"/> - Opens the ExportDialog (Ctrl+E)</item>
/// <item>Requires <see cref="IExportService"/> dependency for export ViewModel construction</item>
/// </list>
/// </para>
/// <para>
/// <b>v0.2.5g Additions:</b>
/// <list type="bullet">
/// <item><see cref="SaveState"/> - Tracks conversation save state for status bar display</item>
/// <item><see cref="IsModelLoaded"/> - Exposes model load state for status bar color</item>
/// <item><see cref="ToggleSettingsPanelCommand"/> - Expands/collapses inference settings (Ctrl+,)</item>
/// <item>Subscribes to <see cref="IConversationService.SaveStateChanged"/> for save indicators</item>
/// </list>
/// </para>
/// </remarks>
public partial class MainWindowViewModel : ViewModelBase
{
    #region Fields

    // Service dependencies for model state and settings
    private readonly ILlmService _llmService;
    private readonly ISettingsService _settingsService;
    private readonly ISystemPromptService _systemPromptService;
    private readonly ISearchService _searchService;
    private readonly IExportService _exportService;
    private readonly IConversationService _conversationService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<MainWindowViewModel>? _logger;

    // Reference to the main window for dialog display (v0.2.4e)
    private Window? _mainWindow;

    #endregion

    #region Child ViewModels

    /// <summary>
    /// Gets the ViewModel for the chat panel.
    /// Manages user input, message display, and streaming responses.
    /// </summary>
    public ChatViewModel ChatViewModel { get; }

    /// <summary>
    /// Gets the ViewModel for the model selection panel.
    /// Handles model file selection and loading operations.
    /// </summary>
    public ModelSelectorViewModel ModelSelectorViewModel { get; }

    /// <summary>
    /// Gets the ViewModel for the conversation list sidebar.
    /// Manages conversation history, search, and selection.
    /// </summary>
    public ConversationListViewModel ConversationListViewModel { get; }

    /// <summary>
    /// Gets the ViewModel for the inference settings panel.
    /// Manages parameter sliders, presets, and inference configuration.
    /// </summary>
    /// <remarks>
    /// Added in v0.2.3e to provide user control over inference parameters
    /// (Temperature, TopP, MaxTokens, etc.) with preset management.
    /// </remarks>
    public InferenceSettingsViewModel InferenceSettingsViewModel { get; }

    /// <summary>
    /// Gets the ViewModel for the file explorer sidebar.
    /// Manages workspace files, navigation, and file operations.
    /// </summary>
    /// <remarks>
    /// Added in v0.3.2g. Singleton instance persists across navigation.
    /// </remarks>
    public FileExplorerViewModel FileExplorer { get; }

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets the status bar message showing current model state.
    /// Updated automatically when model is loaded/unloaded.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "No model loaded";

    /// <summary>
    /// Gets or sets the token generation statistics display.
    /// Shows token count and generation speed during inference.
    /// </summary>
    /// <remarks>
    /// Format: "Tokens: 42 (15.3 tok/s)"
    /// Empty string when not generating.
    /// </remarks>
    [ObservableProperty]
    private string _tokenInfo = string.Empty;

    /// <summary>
    /// Gets or sets whether the sidebar is visible.
    /// Toggled via <see cref="ToggleSidebarCommand"/> or Ctrl+B keyboard shortcut.
    /// </summary>
    /// <remarks>
    /// Bound to <c>SplitView.IsPaneOpen</c> in the view.
    /// Persists across sessions (future: save to settings).
    /// </remarks>
    [ObservableProperty]
    private bool _isSidebarVisible = true;

    /// <summary>
    /// Gets or sets the sidebar width in pixels.
    /// </summary>
    /// <remarks>
    /// Default: 280px (matches design specification).
    /// Bound to <c>SplitView.OpenPaneLength</c> in the view.
    /// </remarks>
    [ObservableProperty]
    private double _sidebarWidth = 280;

    /// <summary>
    /// Gets or sets the current save state for status bar display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Updated via <see cref="IConversationService.SaveStateChanged"/> event.
    /// Used with <see cref="Converters.SaveStatusTextConverter"/> and
    /// <see cref="Converters.SaveStatusColorConverter"/> for status bar display.
    /// </para>
    /// <para>Added in v0.2.5g.</para>
    /// </remarks>
    [ObservableProperty]
    private SaveStateChangedEventArgs? _saveState;

    /// <summary>
    /// Gets or sets whether a model is currently loaded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Updated via <see cref="ILlmService.ModelStateChanged"/> event.
    /// Used with <see cref="Converters.BoolToAccentColorConverter"/> for
    /// status bar model name color.
    /// </para>
    /// <para>Added in v0.2.5g.</para>
    /// </remarks>
    [ObservableProperty]
    private bool _isModelLoaded;

    /// <summary>
    /// Gets or sets whether a workspace is currently open.
    /// Controls EditorPanel visibility in main content area.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Updated via <see cref="IWorkspaceService.WorkspaceChanged"/> event.
    /// When true, the editor panel placeholder is visible.
    /// </para>
    /// <para>Added in v0.3.2g.</para>
    /// </remarks>
    [ObservableProperty]
    private bool _hasOpenWorkspace;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="chatViewModel">The chat panel ViewModel.</param>
    /// <param name="modelSelectorViewModel">The model selector ViewModel.</param>
    /// <param name="conversationListViewModel">The conversation list ViewModel.</param>
    /// <param name="inferenceSettingsViewModel">The inference settings panel ViewModel (v0.2.3e).</param>
    /// <param name="llmService">The LLM service for model state events.</param>
    /// <param name="settingsService">The settings service for loading configuration.</param>
    /// <param name="systemPromptService">The system prompt service for editor ViewModel (v0.2.4e).</param>
    /// <param name="searchService">The search service for search dialog ViewModel (v0.2.5e).</param>
    /// <param name="conversationService">The conversation service for save state events (v0.2.5g).</param>
    /// <param name="dispatcher">The dispatcher for UI thread operations (v0.2.4e).</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <remarks>
    /// <para>
    /// After construction, call <see cref="InitializeAsync"/> to complete initialization.
    /// </para>
    /// <para>
    /// Sets up event subscriptions for model state and inference progress.
    /// </para>
    /// <para>
    /// <b>v0.2.4e Changes:</b>
    /// <list type="bullet">
    ///   <item>Added <paramref name="systemPromptService"/> for editor window construction</item>
    ///   <item>Added <paramref name="dispatcher"/> for thread-safe operations</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>v0.2.5e Changes:</b>
    /// <list type="bullet">
    ///   <item>Added <paramref name="searchService"/> for search dialog construction</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>v0.2.5f Changes:</b>
    /// <list type="bullet">
    ///   <item>Added <paramref name="exportService"/> for export dialog construction</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>v0.2.5g Changes:</b>
    /// <list type="bullet">
    ///   <item>Added <paramref name="conversationService"/> for save state events</item>
    /// </list>
    /// </para>
    /// </remarks>
    public MainWindowViewModel(
        ChatViewModel chatViewModel,
        ModelSelectorViewModel modelSelectorViewModel,
        ConversationListViewModel conversationListViewModel,
        InferenceSettingsViewModel inferenceSettingsViewModel,
        ILlmService llmService,
        ISettingsService settingsService,
        ISystemPromptService systemPromptService,
        ISearchService searchService,
        IExportService exportService,
        IConversationService conversationService,
        IWorkspaceService workspaceService,
        FileExplorerViewModel fileExplorer,
        IDispatcher dispatcher,
        ILogger<MainWindowViewModel>? logger = null)
    {
        var sw = Stopwatch.StartNew();
        _logger = logger;
        _logger?.LogDebug("[INIT] MainWindowViewModel construction started");

        // Store child ViewModels for binding
        ChatViewModel = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));
        ModelSelectorViewModel = modelSelectorViewModel ?? throw new ArgumentNullException(nameof(modelSelectorViewModel));
        ConversationListViewModel = conversationListViewModel ?? throw new ArgumentNullException(nameof(conversationListViewModel));
        InferenceSettingsViewModel = inferenceSettingsViewModel ?? throw new ArgumentNullException(nameof(inferenceSettingsViewModel));

        // Store services for event subscriptions and operations
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _systemPromptService = systemPromptService ?? throw new ArgumentNullException(nameof(systemPromptService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        FileExplorer = fileExplorer ?? throw new ArgumentNullException(nameof(fileExplorer));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        _logger?.LogDebug("[INFO] Child ViewModels and services assigned");

        // Subscribe to model state changes for status bar updates
        _llmService.ModelStateChanged += OnModelStateChanged;

        // Subscribe to inference progress for token statistics
        _llmService.InferenceProgress += OnInferenceProgress;

        // v0.2.5g: Subscribe to save state changes for status bar indicator
        _conversationService.SaveStateChanged += OnSaveStateChanged;

        // v0.2.5g: Initialize model loaded state
        IsModelLoaded = _llmService.IsModelLoaded;

        // v0.3.2g: Subscribe to file explorer and workspace events
        FileExplorer.FileOpenRequested += OnFileOpenRequested;
        FileExplorer.FileAttachRequested += OnFileAttachRequested;
        _workspaceService.WorkspaceChanged += OnWorkspaceChanged;
        HasOpenWorkspace = _workspaceService.CurrentWorkspace != null;

        _logger?.LogDebug("[INFO] Subscribed to ILlmService and IConversationService events");
        _logger?.LogDebug("[INIT] MainWindowViewModel construction completed - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Sets the main window reference for dialog display.
    /// </summary>
    /// <param name="window">The main application window.</param>
    /// <remarks>
    /// <para>
    /// This method should be called from the MainWindow's OnOpened event after
    /// setting the DataContext. The window reference is needed for showing
    /// modal dialogs like the SystemPromptEditorWindow.
    /// </para>
    /// <para>Added in v0.2.4e.</para>
    /// </remarks>
    public void SetMainWindow(Window window)
    {
        _mainWindow = window ?? throw new ArgumentNullException(nameof(window));
        _logger?.LogDebug("[INFO] Main window reference set");
    }

    /// <summary>
    /// Initializes async operations after window loads.
    /// </summary>
    /// <returns>A task representing the async initialization.</returns>
    /// <remarks>
    /// <para>
    /// Called from <c>MainWindow.OnOpened</c> to perform post-construction initialization:
    /// <list type="bullet">
    /// <item>Loads application settings from persistent storage</item>
    /// <item>Populates the conversation list in the sidebar</item>
    /// <item>Initializes the system prompt selector (v0.2.4e)</item>
    /// <item>Updates status bar with current model state</item>
    /// </list>
    /// </para>
    /// <para>
    /// Errors during initialization are caught and displayed in the status bar
    /// rather than propagating to crash the application.
    /// </para>
    /// </remarks>
    public async Task InitializeAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] InitializeAsync");

        try
        {
            // Show loading state in status bar
            StatusMessage = "Loading...";

            // Load application settings first
            _logger?.LogDebug("[INFO] Loading settings");
            await _settingsService.LoadSettingsAsync();
            _logger?.LogDebug("[INFO] Settings loaded successfully");

            // Initialize the conversation list (loads from database)
            _logger?.LogDebug("[INFO] Initializing ConversationListViewModel");
            await ConversationListViewModel.InitializeAsync();
            _logger?.LogInformation("[INFO] ConversationListViewModel initialized with {GroupCount} groups",
                ConversationListViewModel.Groups.Count);

            // Initialize inference settings (loads presets from database) - v0.2.3e
            _logger?.LogDebug("[INFO] Initializing InferenceSettingsViewModel");
            await InferenceSettingsViewModel.InitializeCommand.ExecuteAsync(null);
            _logger?.LogInformation("[INFO] InferenceSettingsViewModel initialized with {PresetCount} presets",
                InferenceSettingsViewModel.Presets.Count);

            // v0.2.4e: Initialize system prompt selector (loads prompts from database)
            _logger?.LogDebug("[INFO] Initializing SystemPromptSelectorViewModel");
            await ChatViewModel.SystemPromptSelectorViewModel.InitializeAsync();
            _logger?.LogInformation("[INFO] SystemPromptSelectorViewModel initialized with {PromptCount} prompts",
                ChatViewModel.SystemPromptSelectorViewModel.AvailablePrompts.Count);

            // Update status bar with model state
            StatusMessage = _llmService.IsModelLoaded
                ? $"Model: {_llmService.CurrentModelName}"
                : "No model loaded";

            _logger?.LogInformation("[INFO] Initialization complete");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] InitializeAsync failed");
            StatusMessage = $"Error: {ex.Message}";
            SetError($"Initialization failed: {ex.Message}");
        }
        finally
        {
            _logger?.LogDebug("[EXIT] InitializeAsync - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Toggles the sidebar visibility.
    /// </summary>
    /// <remarks>
    /// Bound to Ctrl+B keyboard shortcut and sidebar toggle button.
    /// </remarks>
    [RelayCommand]
    private void ToggleSidebar()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] ToggleSidebar - Current: {IsVisible}", IsSidebarVisible);

        IsSidebarVisible = !IsSidebarVisible;

        _logger?.LogInformation("[INFO] Sidebar visibility toggled to: {IsVisible}", IsSidebarVisible);
        _logger?.LogDebug("[EXIT] ToggleSidebar - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// Bound to Ctrl+N keyboard shortcut and sidebar [+] button.
    /// </para>
    /// <para>
    /// Delegates to <see cref="ConversationListViewModel.CreateNewConversationCommand"/>.
    /// The chat view will be updated via event subscriptions when the new
    /// conversation is created.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task NewConversationAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] NewConversationAsync");

        try
        {
            ClearError();
            await ConversationListViewModel.CreateNewConversationCommand.ExecuteAsync(null);
            _logger?.LogInformation("[INFO] New conversation created via MainWindowViewModel");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] NewConversationAsync failed");
            SetError($"Failed to create conversation: {ex.Message}");
        }
        finally
        {
            _logger?.LogDebug("[EXIT] NewConversationAsync - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Focuses the search box in the conversation list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bound to Ctrl+F keyboard shortcut.
    /// </para>
    /// <para>
    /// This command sets a flag that the view monitors to focus the search TextBox.
    /// Future enhancement: Use a messenger pattern for cleaner view/viewmodel separation.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void FocusSearch()
    {
        _logger?.LogDebug("[ENTER] FocusSearch");

        // For v0.2.2d, this is a placeholder. The view will need to handle
        // focus management. A future version could use a messenger pattern.
        // Currently, the view can subscribe to a property change or event.

        _logger?.LogDebug("[INFO] FocusSearch command executed - view should handle focus");
        _logger?.LogDebug("[EXIT] FocusSearch");
    }

    /// <summary>
    /// Opens the System Prompt Editor window as a modal dialog.
    /// </summary>
    /// <returns>A task representing the async dialog operation.</returns>
    /// <remarks>
    /// <para>
    /// This command is invoked from the chat header's Edit button in the
    /// <see cref="SystemPromptSelector"/> control. The editor window allows
    /// full CRUD operations on system prompts including:
    /// <list type="bullet">
    ///   <item>Creating new prompts</item>
    ///   <item>Editing existing user prompts</item>
    ///   <item>Duplicating templates as user prompts</item>
    ///   <item>Setting a prompt as default</item>
    ///   <item>Deleting user prompts</item>
    /// </list>
    /// </para>
    /// <para>
    /// The window is shown as a modal dialog, blocking interaction with the
    /// main window until closed. Any changes made are automatically reflected
    /// in the chat header selector through service events.
    /// </para>
    /// <para>Added in v0.2.4e.</para>
    /// </remarks>
    [RelayCommand]
    private async Task OpenSystemPromptEditorAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OpenSystemPromptEditorAsync");

        try
        {
            if (_mainWindow == null)
            {
                _logger?.LogWarning("[WARN] Main window reference not set - cannot show editor dialog");
                return;
            }

            // Create a new editor ViewModel for this window instance.
            // The ViewModel is transient - each editor window gets its own instance.
            var editorViewModel = new SystemPromptEditorViewModel(
                _systemPromptService,
                _dispatcher,
                _logger != null
                    ? Microsoft.Extensions.Logging.LoggerFactory.Create(b => { }).CreateLogger<SystemPromptEditorViewModel>()
                    : null);

            // Create and show the editor window as a modal dialog.
            var editorWindow = new SystemPromptEditorWindow
            {
                DataContext = editorViewModel
            };

            _logger?.LogDebug("[INFO] Showing SystemPromptEditorWindow as modal dialog");
            await editorWindow.ShowDialog(_mainWindow);

            _logger?.LogDebug("[INFO] SystemPromptEditorWindow closed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OpenSystemPromptEditorAsync failed: {Message}", ex.Message);
            SetError($"Failed to open editor: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OpenSystemPromptEditorAsync - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Opens the Search dialog as a modal dialog.
    /// </summary>
    /// <returns>A task representing the async dialog operation.</returns>
    /// <remarks>
    /// <para>
    /// This command is invoked via Ctrl+K keyboard shortcut from the main window.
    /// The search dialog provides:
    /// <list type="bullet">
    ///   <item>Full-text search across conversations and messages</item>
    ///   <item>Debounced search input (300ms)</item>
    ///   <item>Filter tabs for All, Conversations, Messages</item>
    ///   <item>Keyboard navigation (Up/Down, Enter, Escape)</item>
    ///   <item>Grouped results by type</item>
    /// </list>
    /// </para>
    /// <para>
    /// When a result is selected, the dialog returns the <see cref="SearchResult"/>
    /// which can be used to navigate to the selected conversation or message.
    /// </para>
    /// <para>Added in v0.2.5e.</para>
    /// </remarks>
    [RelayCommand]
    private async Task OpenSearchAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OpenSearchAsync");

        try
        {
            if (_mainWindow == null)
            {
                _logger?.LogWarning("[WARN] Main window reference not set - cannot show search dialog");
                return;
            }

            // Create a new search ViewModel for this dialog instance.
            // The ViewModel is transient - each dialog gets its own instance.
            var searchViewModel = new SearchViewModel(
                _searchService,
                _logger != null
                    ? Microsoft.Extensions.Logging.LoggerFactory.Create(b => { }).CreateLogger<SearchViewModel>()
                    : null);

            // Create and show the search dialog as a modal.
            var searchDialog = new SearchDialog
            {
                DataContext = searchViewModel
            };

            _logger?.LogDebug("[INFO] Showing SearchDialog as modal dialog");
            var result = await searchDialog.ShowDialog<SearchResult?>(_mainWindow);

            if (result != null)
            {
                _logger?.LogInformation("[INFO] Search result selected: {Type} - {Title} (ConversationId: {ConvId})",
                    result.TypeLabel, result.Title, result.ConversationId);

                // Navigate to the selected conversation.
                // Find the conversation in the list and select it.
                var conversationToSelect = ConversationListViewModel.Groups
                    .SelectMany(g => g.Conversations)
                    .FirstOrDefault(c => c.Id == result.ConversationId);

                if (conversationToSelect != null)
                {
                    await ConversationListViewModel.SelectConversationCommand.ExecuteAsync(conversationToSelect);
                    _logger?.LogDebug("[INFO] Navigated to conversation: {Id}", result.ConversationId);
                }
                else
                {
                    _logger?.LogWarning("[WARN] Conversation not found in list: {Id}", result.ConversationId);
                }
            }
            else
            {
                _logger?.LogDebug("[INFO] SearchDialog closed without selection");
            }

            // Dispose the ViewModel to clean up the debounce timer.
            searchViewModel.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OpenSearchAsync failed: {Message}", ex.Message);
            SetError($"Failed to open search: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OpenSearchAsync - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Opens the Export dialog as a modal dialog.
    /// </summary>
    /// <returns>A task representing the async dialog operation.</returns>
    /// <remarks>
    /// <para>
    /// This command is invoked via Ctrl+E keyboard shortcut from the main window.
    /// The export dialog provides:
    /// <list type="bullet">
    ///   <item>Format selection (Markdown, JSON, PlainText, HTML)</item>
    ///   <item>Option toggles (timestamps, system prompt, metadata, token counts)</item>
    ///   <item>Live preview of export output</item>
    ///   <item>File save dialog integration</item>
    /// </list>
    /// </para>
    /// <para>
    /// The command is only enabled when a conversation is selected. When the export
    /// is complete, the dialog closes automatically.
    /// </para>
    /// <para>Added in v0.2.5f.</para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(HasActiveConversation))]
    private async Task OpenExportAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OpenExportAsync");

        try
        {
            if (_mainWindow == null)
            {
                _logger?.LogWarning("[WARN] Main window reference not set - cannot show export dialog");
                return;
            }

            var conversationId = ConversationListViewModel.SelectedConversation?.Id;
            if (conversationId is null)
            {
                _logger?.LogDebug("[SKIP] No conversation selected");
                return;
            }

            // Create a new export ViewModel for this dialog instance.
            // The ViewModel is transient - each dialog gets its own instance.
            var exportViewModel = new ExportViewModel(
                _exportService,
                _mainWindow.StorageProvider,
                conversationId.Value,
                _logger != null
                    ? Microsoft.Extensions.Logging.LoggerFactory.Create(b => { }).CreateLogger<ExportViewModel>()
                    : null);

            // Create and show the export dialog as a modal.
            var exportDialog = new ExportDialog
            {
                DataContext = exportViewModel
            };

            _logger?.LogDebug("[INFO] Showing ExportDialog as modal dialog for conversation: {Id}", conversationId.Value);
            await exportDialog.ShowDialog(_mainWindow);

            _logger?.LogDebug("[INFO] ExportDialog closed");

            // Dispose the ViewModel to clean up resources.
            exportViewModel.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] OpenExportAsync failed: {Message}", ex.Message);
            SetError($"Failed to open export: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            _logger?.LogDebug("[EXIT] OpenExportAsync - {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Gets a value indicating whether a conversation is currently selected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used as the CanExecute condition for <see cref="OpenExportCommand"/>.
    /// </para>
    /// <para>Added in v0.2.5f.</para>
    /// </remarks>
    private bool HasActiveConversation => ConversationListViewModel.SelectedConversation is not null;

    /// <summary>
    /// Toggles the expansion state of the inference settings panel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bound to Ctrl+, keyboard shortcut and clickable temperature in status bar.
    /// Expands the settings panel in the sidebar to show inference parameters.
    /// </para>
    /// <para>Added in v0.2.5g.</para>
    /// </remarks>
    [RelayCommand]
    private void ToggleSettingsPanel()
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] ToggleSettingsPanel - Current: {IsExpanded}",
            InferenceSettingsViewModel.IsExpanded);

        InferenceSettingsViewModel.IsExpanded = !InferenceSettingsViewModel.IsExpanded;

        // Ensure sidebar is visible when expanding settings
        if (InferenceSettingsViewModel.IsExpanded && !IsSidebarVisible)
        {
            IsSidebarVisible = true;
            _logger?.LogDebug("[INFO] Sidebar opened to show settings panel");
        }

        _logger?.LogInformation("[INFO] Settings panel toggled to: {IsExpanded}",
            InferenceSettingsViewModel.IsExpanded);
        _logger?.LogDebug("[EXIT] ToggleSettingsPanel - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles model state change events to update the status bar.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments containing model state details.</param>
    private void OnModelStateChanged(object? sender, ModelStateChangedEventArgs e)
    {
        _logger?.LogDebug("[EVENT] OnModelStateChanged - IsLoaded: {IsLoaded}, ModelName: {ModelName}",
            e.IsLoaded, e.ModelName);

        // v0.2.5g: Update model loaded state for status bar color binding
        IsModelLoaded = e.IsLoaded;

        // Update status message based on whether model is loaded
        StatusMessage = e.IsLoaded
            ? $"Model: {e.ModelName}"
            : "No model loaded";

        // Clear token info when model is unloaded
        if (!e.IsLoaded)
        {
            TokenInfo = string.Empty;
        }

        _logger?.LogInformation("[INFO] Status updated: {StatusMessage}", StatusMessage);
    }

    /// <summary>
    /// Handles save state change events to update the status bar indicator.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments containing save state details.</param>
    /// <remarks>
    /// <para>Added in v0.2.5g.</para>
    /// </remarks>
    private void OnSaveStateChanged(object? sender, SaveStateChangedEventArgs e)
    {
        _logger?.LogDebug("[EVENT] OnSaveStateChanged - IsSaving: {IsSaving}, HasUnsavedChanges: {HasUnsavedChanges}",
            e.IsSaving, e.HasUnsavedChanges);

        SaveState = e;

        _logger?.LogDebug("[INFO] Save state updated");
    }

    /// <summary>
    /// Handles inference progress events to update token statistics.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments containing inference progress details.</param>
    private void OnInferenceProgress(object? sender, InferenceProgressEventArgs e)
    {
        // Format: "Tokens: 42 (15.3 tok/s)"
        TokenInfo = $"Tokens: {e.TokensGenerated} ({e.TokensPerSecond:F1} tok/s)";

        // Log every 10 tokens to avoid log spam
        if (e.TokensGenerated % 10 == 0)
        {
            _logger?.LogDebug("[INFO] Inference progress: {TokenInfo}", TokenInfo);
        }
    }

    /// <summary>
    /// Handles file open requests from the file explorer.
    /// Forwards to editor panel (v0.3.3 when implemented).
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments containing file path.</param>
    /// <remarks>
    /// <para>Added in v0.3.2g.</para>
    /// </remarks>
    private void OnFileOpenRequested(object? sender, FileOpenRequestedEventArgs e)
    {
        _logger?.LogDebug("[EVENT] OnFileOpenRequested - Path: {Path}", e.FilePath);

        // TODO: Forward to editor panel (v0.3.3)
        // EditorPanel?.OpenFileCommand.Execute(e.FilePath);

        _logger?.LogInformation("[INFO] File open requested: {Path}", e.FilePath);
    }

    /// <summary>
    /// Handles file attach requests from the file explorer.
    /// Forwards to chat view model (v0.3.4 when implemented).
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments containing file path.</param>
    /// <remarks>
    /// <para>Added in v0.3.2g.</para>
    /// </remarks>
    private void OnFileAttachRequested(object? sender, FileAttachRequestedEventArgs e)
    {
        _logger?.LogDebug("[EVENT] OnFileAttachRequested - Path: {Path}", e.FilePath);

        // TODO: Forward to chat view model (v0.3.4)
        // Chat?.AttachFileCommand.Execute(e.FilePath);

        _logger?.LogInformation("[INFO] File attach requested: {Path}", e.FilePath);
    }

    /// <summary>
    /// Handles workspace change events to update HasOpenWorkspace.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments containing workspace details.</param>
    /// <remarks>
    /// <para>Added in v0.3.2g.</para>
    /// </remarks>
    private void OnWorkspaceChanged(object? sender, WorkspaceChangedEventArgs e)
    {
        _logger?.LogDebug("[EVENT] OnWorkspaceChanged - CurrentWorkspace: {Workspace}",
            e.CurrentWorkspace?.Name ?? "(null)");

        HasOpenWorkspace = e.CurrentWorkspace != null;

        _logger?.LogInformation("[INFO] Workspace state updated: HasOpenWorkspace={HasOpen}", HasOpenWorkspace);
    }

    #endregion
}
