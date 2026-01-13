using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;

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
/// </remarks>
public partial class MainWindowViewModel : ViewModelBase
{
    #region Fields

    // Service dependencies for model state and settings
    private readonly ILlmService _llmService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<MainWindowViewModel>? _logger;

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
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <remarks>
    /// <para>
    /// After construction, call <see cref="InitializeAsync"/> to complete initialization.
    /// </para>
    /// <para>
    /// Sets up event subscriptions for model state and inference progress.
    /// </para>
    /// </remarks>
    public MainWindowViewModel(
        ChatViewModel chatViewModel,
        ModelSelectorViewModel modelSelectorViewModel,
        ConversationListViewModel conversationListViewModel,
        InferenceSettingsViewModel inferenceSettingsViewModel,
        ILlmService llmService,
        ISettingsService settingsService,
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

        // Store services for event subscriptions
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        _logger?.LogDebug("[INFO] Child ViewModels assigned");

        // Subscribe to model state changes for status bar updates
        _llmService.ModelStateChanged += OnModelStateChanged;

        // Subscribe to inference progress for token statistics
        _llmService.InferenceProgress += OnInferenceProgress;

        _logger?.LogDebug("[INFO] Subscribed to ILlmService events");
        _logger?.LogDebug("[INIT] MainWindowViewModel construction completed - {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion

    #region Initialization

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

    #endregion
}
