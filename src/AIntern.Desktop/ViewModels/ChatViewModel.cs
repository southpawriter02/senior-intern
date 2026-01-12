using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Events;
using AIntern.Core.Exceptions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.Views;

namespace AIntern.Desktop.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    private readonly ILlmService _llmService;
    private readonly IConversationService _conversationService;
    private readonly IInferenceSettingsService _inferenceSettingsService;
    private readonly ISystemPromptService _systemPromptService;
    private CancellationTokenSource? _generationCts;

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages = new();

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private int _tokenCount;

    [ObservableProperty]
    private bool _canSend;

    // System prompt integration properties
    [ObservableProperty]
    private SystemPromptSelectorViewModel? _systemPromptSelectorViewModel;

    [ObservableProperty]
    private bool _hasSystemPrompt;

    [ObservableProperty]
    private string? _currentPromptName;

    [ObservableProperty]
    private string? _currentPromptContent;

    // Context attachment properties (v0.3.4d)
    [ObservableProperty]
    private ObservableCollection<FileContextViewModel> _attachedContexts = new();

    [ObservableProperty]
    private int _totalContextTokens;

    [ObservableProperty]
    private int _maxContextTokens = 8000;

    /// <summary>
    /// Whether any contexts are currently attached.
    /// </summary>
    public bool HasAttachedContexts => AttachedContexts.Count > 0;

    /// <summary>
    /// Whether multiple contexts are attached (shows Clear All button).
    /// </summary>
    public bool HasMultipleContexts => AttachedContexts.Count > 1;

    /// <summary>
    /// Whether token count is approaching the limit (80-100%).
    /// </summary>
    public bool IsNearTokenLimit => TotalContextTokens >= MaxContextTokens * 0.8 && TotalContextTokens < MaxContextTokens;

    /// <summary>
    /// Whether token count exceeds the limit (>100%).
    /// </summary>
    public bool IsOverTokenLimit => TotalContextTokens >= MaxContextTokens;

    public ChatViewModel(
        ILlmService llmService,
        IConversationService conversationService,
        IInferenceSettingsService inferenceSettingsService,
        ISystemPromptService systemPromptService)
    {
        _llmService = llmService;
        _conversationService = conversationService;
        _inferenceSettingsService = inferenceSettingsService;
        _systemPromptService = systemPromptService;

        // Initialize system prompt selector
        _systemPromptSelectorViewModel = new SystemPromptSelectorViewModel(
            _systemPromptService, _conversationService);
        _systemPromptSelectorViewModel.OpenEditorAction = OpenSystemPromptEditor;

        // Update CanSend when dependencies change
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(UserInput) or nameof(IsGenerating))
            {
                UpdateCanSend();
            }
        };

        _llmService.ModelStateChanged += (_, _) => UpdateCanSend();

        // Subscribe to conversation changes to reload messages when switching conversations
        _conversationService.ConversationChanged += OnConversationChanged;

        // Subscribe to system prompt changes
        _systemPromptService.CurrentPromptChanged += OnCurrentPromptChanged;
        UpdateSystemPromptDisplay();
    }

    private void OnCurrentPromptChanged(object? sender, CurrentPromptChangedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(UpdateSystemPromptDisplay);
    }

    private void UpdateSystemPromptDisplay()
    {
        var prompt = _systemPromptService.CurrentPrompt;
        HasSystemPrompt = prompt != null;
        CurrentPromptName = prompt?.Name;
        CurrentPromptContent = prompt?.Content;
    }

    private IEnumerable<ChatMessage> BuildContextWithSystemPrompt()
    {
        var systemPrompt = _systemPromptService.CurrentPrompt;

        // Prepend system message if prompt is selected
        if (systemPrompt != null)
        {
            yield return new ChatMessage
            {
                Role = MessageRole.System,
                Content = _systemPromptService.FormatPromptForContext(systemPrompt),
                Timestamp = DateTime.UtcNow
            };
        }

        // Then yield all conversation messages
        foreach (var message in _conversationService.GetMessages())
        {
            yield return message;
        }
    }

    private void OnConversationChanged(object? sender, EventArgs e)
    {
        // Reload messages from current conversation
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            Messages.Clear();
            foreach (var message in _conversationService.GetMessages())
            {
                Messages.Add(ChatMessageViewModel.FromChatMessage(message));
            }
        });
    }

    private void UpdateCanSend()
    {
        CanSend = !string.IsNullOrWhiteSpace(UserInput)
                  && !IsGenerating
                  && _llmService.IsModelLoaded;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput)) return;
        if (!_llmService.IsModelLoaded)
        {
            SetError("Please load a model first.");
            return;
        }

        ClearError();
        var userMessage = UserInput.Trim();
        UserInput = string.Empty;

        // Add user message
        var userMessageVm = new ChatMessageViewModel
        {
            Role = MessageRole.User,
            Content = userMessage
        };
        Messages.Add(userMessageVm);
        _conversationService.AddMessage(userMessageVm.ToChatMessage());

        // Create assistant message placeholder
        var assistantMessageVm = new ChatMessageViewModel
        {
            Role = MessageRole.Assistant,
            IsStreaming = true
        };
        Messages.Add(assistantMessageVm);

        _generationCts = new CancellationTokenSource();
        IsGenerating = true;
        TokenCount = 0;

        try
        {
            // Get current inference settings from the settings service
            var settings = _inferenceSettingsService.CurrentSettings;
            var options = new InferenceOptions(
                MaxTokens: settings.MaxTokens,
                Temperature: settings.Temperature,
                TopP: settings.TopP
            );

            var conversation = BuildContextWithSystemPrompt();

            await foreach (var token in _llmService.GenerateStreamingAsync(
                conversation, options, _generationCts.Token))
            {
                // Update on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    assistantMessageVm.AppendContent(token);
                    TokenCount++;
                });
            }

            assistantMessageVm.CompleteStreaming();

            // Add completed message to conversation service
            _conversationService.AddMessage(assistantMessageVm.ToChatMessage());
        }
        catch (OperationCanceledException)
        {
            // User cancelled - mark message appropriately
            assistantMessageVm.MarkAsCancelled();
        }
        catch (InferenceException ex)
        {
            SetError($"Generation failed: {ex.Message}");
            assistantMessageVm.CompleteStreaming();
            if (string.IsNullOrEmpty(assistantMessageVm.Content))
            {
                assistantMessageVm.Content = "[Error: Generation failed]";
            }
        }
        catch (Exception ex)
        {
            SetError($"Unexpected error: {ex.Message}");
            assistantMessageVm.CompleteStreaming();
            if (string.IsNullOrEmpty(assistantMessageVm.Content))
            {
                assistantMessageVm.Content = "[Error occurred]";
            }
        }
        finally
        {
            IsGenerating = false;
            _generationCts?.Dispose();
            _generationCts = null;
        }
    }

    [RelayCommand]
    private void CancelGeneration()
    {
        _generationCts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
        _conversationService.ClearConversation();
        ClearError();
    }

    public void HandleEnterKey()
    {
        if (CanSend)
        {
            SendMessageCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void OpenSystemPromptEditor()
    {
        var viewModel = new SystemPromptEditorViewModel(_systemPromptService);
        var window = new SystemPromptEditorWindow
        {
            DataContext = viewModel
        };
        window.Show();
    }

    partial void OnTotalContextTokensChanged(int value)
    {
        OnPropertyChanged(nameof(IsNearTokenLimit));
        OnPropertyChanged(nameof(IsOverTokenLimit));
    }

    partial void OnMaxContextTokensChanged(int value)
    {
        OnPropertyChanged(nameof(IsNearTokenLimit));
        OnPropertyChanged(nameof(IsOverTokenLimit));
    }

    #region Context Management (v0.3.4d)

    /// <summary>
    /// Adds a file context to the attached contexts.
    /// </summary>
    public void AddContext(FileContextViewModel context)
    {
        AttachedContexts.Add(context);
        UpdateContextTokens();
        NotifyContextPropertiesChanged();
    }

    /// <summary>
    /// Removes a specific context from attached contexts.
    /// </summary>
    [RelayCommand]
    private void RemoveContext(FileContextViewModel? context)
    {
        if (context != null && AttachedContexts.Remove(context))
        {
            UpdateContextTokens();
            NotifyContextPropertiesChanged();
        }
    }

    /// <summary>
    /// Clears all attached contexts.
    /// </summary>
    [RelayCommand]
    private void ClearAllContexts()
    {
        AttachedContexts.Clear();
        TotalContextTokens = 0;
        NotifyContextPropertiesChanged();
    }

    /// <summary>
    /// Shows a preview of the selected context (placeholder for future implementation).
    /// </summary>
    [RelayCommand]
    private void ShowPreview(FileContextViewModel? context)
    {
        // TODO: Implement preview functionality in a future version
        // This will open a preview panel or dialog showing the context content
    }

    /// <summary>
    /// Updates the total token count from all attached contexts.
    /// </summary>
    private void UpdateContextTokens()
    {
        TotalContextTokens = AttachedContexts.Sum(c => c.EstimatedTokens);
    }

    /// <summary>
    /// Notifies property changes for computed context properties.
    /// </summary>
    private void NotifyContextPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasAttachedContexts));
        OnPropertyChanged(nameof(HasMultipleContexts));
        OnPropertyChanged(nameof(IsNearTokenLimit));
        OnPropertyChanged(nameof(IsOverTokenLimit));
    }

    #endregion
}
