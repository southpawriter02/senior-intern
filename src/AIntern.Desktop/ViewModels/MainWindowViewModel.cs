using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// Root ViewModel for the main application window.
/// Coordinates child ViewModels and manages overall application state.
/// </summary>
/// <remarks>
/// <para>
/// Subscribes to <see cref="ILlmService"/> events to update status bar information.
/// </para>
/// <para>
/// Loads application settings on startup via <see cref="ISettingsService"/>.
/// </para>
/// </remarks>
public partial class MainWindowViewModel : ViewModelBase
{
    // Service dependencies for model state and settings
    private readonly ILlmService _llmService;
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Gets the ViewModel for the chat panel.
    /// Manages user input and message display.
    /// </summary>
    public ChatViewModel ChatViewModel { get; }

    /// <summary>
    /// Gets the ViewModel for the model selection panel.
    /// Handles model file selection and loading.
    /// </summary>
    public ModelSelectorViewModel ModelSelectorViewModel { get; }

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
    [ObservableProperty]
    private string _tokenInfo = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="chatViewModel">The chat panel ViewModel.</param>
    /// <param name="modelSelectorViewModel">The model selector ViewModel.</param>
    /// <param name="llmService">The LLM service for model state events.</param>
    /// <param name="settingsService">The settings service for loading configuration.</param>
    public MainWindowViewModel(
        ChatViewModel chatViewModel,
        ModelSelectorViewModel modelSelectorViewModel,
        ILlmService llmService,
        ISettingsService settingsService)
    {
        // Store child ViewModels for binding
        ChatViewModel = chatViewModel;
        ModelSelectorViewModel = modelSelectorViewModel;
        
        // Store services for event subscriptions
        _llmService = llmService;
        _settingsService = settingsService;

        // Subscribe to model state changes for status bar updates
        _llmService.ModelStateChanged += OnModelStateChanged;
        
        // Subscribe to inference progress for token statistics
        _llmService.InferenceProgress += OnInferenceProgress;

        // Load settings asynchronously on startup (fire-and-forget)
        _ = LoadSettingsAsync();
    }

    /// <summary>
    /// Loads application settings from persistent storage.
    /// Called automatically on ViewModel construction.
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        // Load settings from disk (or use defaults if file not found)
        await _settingsService.LoadSettingsAsync();
    }

    /// <summary>
    /// Handles model state change events to update the status bar.
    /// </summary>
    private void OnModelStateChanged(object? sender, ModelStateChangedEventArgs e)
    {
        // Update status message based on whether model is loaded
        StatusMessage = e.IsLoaded
            ? $"Model: {e.ModelName}"
            : "No model loaded";
    }

    /// <summary>
    /// Handles inference progress events to update token statistics.
    /// </summary>
    private void OnInferenceProgress(object? sender, InferenceProgressEventArgs e)
    {
        // Format: "Tokens: 42 (15.3 tok/s)"
        TokenInfo = $"Tokens: {e.TokensGenerated} ({e.TokensPerSecond:F1} tok/s)";
    }
}
