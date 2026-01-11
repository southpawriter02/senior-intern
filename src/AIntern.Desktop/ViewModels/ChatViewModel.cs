using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeniorIntern.Core.Exceptions;
using SeniorIntern.Core.Interfaces;
using SeniorIntern.Core.Models;

namespace SeniorIntern.Desktop.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    private readonly ILlmService _llmService;
    private readonly IConversationService _conversationService;
    private readonly ISettingsService _settingsService;
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

    public ChatViewModel(
        ILlmService llmService,
        IConversationService conversationService,
        ISettingsService settingsService)
    {
        _llmService = llmService;
        _conversationService = conversationService;
        _settingsService = settingsService;

        // Update CanSend when dependencies change
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(UserInput) or nameof(IsGenerating))
            {
                UpdateCanSend();
            }
        };

        _llmService.ModelStateChanged += (_, _) => UpdateCanSend();
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
            var settings = _settingsService.CurrentSettings;
            var options = new InferenceOptions(
                MaxTokens: settings.MaxTokens,
                Temperature: settings.Temperature,
                TopP: settings.TopP
            );

            var conversation = _conversationService.GetMessages();

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
}
