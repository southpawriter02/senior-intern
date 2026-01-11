using CommunityToolkit.Mvvm.ComponentModel;
using SeniorIntern.Core.Events;
using SeniorIntern.Core.Interfaces;

namespace SeniorIntern.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILlmService _llmService;
    private readonly ISettingsService _settingsService;

    public ChatViewModel ChatViewModel { get; }
    public ModelSelectorViewModel ModelSelectorViewModel { get; }

    [ObservableProperty]
    private string _statusMessage = "No model loaded";

    [ObservableProperty]
    private string _tokenInfo = string.Empty;

    public MainWindowViewModel(
        ChatViewModel chatViewModel,
        ModelSelectorViewModel modelSelectorViewModel,
        ILlmService llmService,
        ISettingsService settingsService)
    {
        ChatViewModel = chatViewModel;
        ModelSelectorViewModel = modelSelectorViewModel;
        _llmService = llmService;
        _settingsService = settingsService;

        // Subscribe to service events
        _llmService.ModelStateChanged += OnModelStateChanged;
        _llmService.InferenceProgress += OnInferenceProgress;

        // Load settings on startup
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        await _settingsService.LoadSettingsAsync();
    }

    private void OnModelStateChanged(object? sender, ModelStateChangedEventArgs e)
    {
        StatusMessage = e.IsLoaded
            ? $"Model: {e.ModelName}"
            : "No model loaded";
    }

    private void OnInferenceProgress(object? sender, InferenceProgressEventArgs e)
    {
        TokenInfo = $"Tokens: {e.TokensGenerated} ({e.TokensPerSecond:F1} tok/s)";
    }
}
