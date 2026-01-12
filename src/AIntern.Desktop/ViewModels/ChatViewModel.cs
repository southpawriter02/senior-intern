using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Exceptions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the chat interface panel.
/// Manages user input, message display, and streaming response generation.
/// </summary>
/// <remarks>
/// <para>
/// Supports streaming text generation with real-time UI updates via <see cref="Dispatcher.UIThread"/>.
/// </para>
/// <para>
/// Integrates with:
/// <list type="bullet">
/// <item><see cref="ILlmService"/> for text generation</item>
/// <item><see cref="IConversationService"/> for message history</item>
/// <item><see cref="ISettingsService"/> for inference parameters</item>
/// </list>
/// </para>
/// </remarks>
public partial class ChatViewModel : ViewModelBase
{
    // Service dependencies for LLM operations, conversation state, and settings
    private readonly ILlmService _llmService;
    private readonly IConversationService _conversationService;
    private readonly ISettingsService _settingsService;
    
    // Cancellation token source for the current generation (allows cancellation)
    private CancellationTokenSource? _generationCts;

    /// <summary>
    /// Gets or sets the current user input text.
    /// </summary>
    [ObservableProperty]
    private string _userInput = string.Empty;

    /// <summary>
    /// Gets or sets the collection of chat messages displayed in the UI.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages = new();

    /// <summary>
    /// Gets or sets whether a response is currently being generated.
    /// </summary>
    [ObservableProperty]
    private bool _isGenerating;

    /// <summary>
    /// Gets or sets the number of tokens generated in the current response.
    /// </summary>
    [ObservableProperty]
    private int _tokenCount;

    /// <summary>
    /// Gets or sets whether the send command can be executed.
    /// </summary>
    [ObservableProperty]
    private bool _canSend;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatViewModel"/> class.
    /// </summary>
    /// <param name="llmService">The LLM service for text generation.</param>
    /// <param name="conversationService">The conversation service for message history.</param>
    /// <param name="settingsService">The settings service for inference parameters.</param>
    public ChatViewModel(
        ILlmService llmService,
        IConversationService conversationService,
        ISettingsService settingsService)
    {
        _llmService = llmService;
        _conversationService = conversationService;
        _settingsService = settingsService;

        // Re-evaluate CanSend whenever input or generation state changes
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(UserInput) or nameof(IsGenerating))
            {
                UpdateCanSend();
            }
        };

        // Also re-evaluate when model state changes (load/unload)
        _llmService.ModelStateChanged += (_, _) => UpdateCanSend();
    }

    /// <summary>
    /// Updates the <see cref="CanSend"/> property based on current state.
    /// </summary>
    private void UpdateCanSend()
    {
        // Can only send if: has input, not generating, and model is loaded
        CanSend = !string.IsNullOrWhiteSpace(UserInput)
                  && !IsGenerating
                  && _llmService.IsModelLoaded;
    }

    /// <summary>
    /// Sends the current user input and generates a streaming response.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        // Guard: ensure we have valid input
        if (string.IsNullOrWhiteSpace(UserInput)) return;
        
        // Guard: ensure model is loaded
        if (!_llmService.IsModelLoaded)
        {
            SetError("Please load a model first.");
            return;
        }

        // Clear any previous errors and capture the user message
        ClearError();
        var userMessage = UserInput.Trim();
        UserInput = string.Empty; // Clear input field immediately for UX

        // Create and display the user message
        var userMessageVm = new ChatMessageViewModel
        {
            Role = MessageRole.User,
            Content = userMessage
        };
        Messages.Add(userMessageVm);
        
        // Also add to the conversation service for context tracking
        _conversationService.AddMessage(userMessageVm.ToChatMessage());

        // Create a placeholder for the assistant response (will be filled by streaming)
        var assistantMessageVm = new ChatMessageViewModel
        {
            Role = MessageRole.Assistant,
            IsStreaming = true // Shows typing indicator in UI
        };
        Messages.Add(assistantMessageVm);

        // Set up cancellation token for this generation
        _generationCts = new CancellationTokenSource();
        IsGenerating = true;
        TokenCount = 0;

        try
        {
            // Build inference options from current settings
            var settings = _settingsService.CurrentSettings;
            var options = new InferenceOptions(
                MaxTokens: settings.MaxTokens,       // Maximum response length
                Temperature: settings.Temperature,   // Randomness (0 = deterministic)
                TopP: settings.TopP                  // Nucleus sampling threshold
            );

            // Get the full conversation context for the model
            var conversation = _conversationService.GetMessages();

            // Stream tokens from the LLM and update UI in real-time
            await foreach (var token in _llmService.GenerateStreamingAsync(
                conversation, options, _generationCts.Token))
            {
                // Must update UI on the UI thread (Avalonia requirement)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    assistantMessageVm.AppendContent(token);
                    TokenCount++;
                });
            }

            // Mark streaming as complete (removes typing indicator)
            assistantMessageVm.CompleteStreaming();

            // Save the completed response to conversation history
            _conversationService.AddMessage(assistantMessageVm.ToChatMessage());
        }
        catch (OperationCanceledException)
        {
            // User clicked cancel - mark message accordingly
            assistantMessageVm.MarkAsCancelled();
        }
        catch (InferenceException ex)
        {
            // Known inference error - display to user
            SetError($"Generation failed: {ex.Message}");
            assistantMessageVm.CompleteStreaming();
            
            // Show error in message if no content was generated
            if (string.IsNullOrEmpty(assistantMessageVm.Content))
            {
                assistantMessageVm.Content = "[Error: Generation failed]";
            }
        }
        catch (Exception ex)
        {
            // Unexpected error - log and display generic message
            SetError($"Unexpected error: {ex.Message}");
            assistantMessageVm.CompleteStreaming();
            
            if (string.IsNullOrEmpty(assistantMessageVm.Content))
            {
                assistantMessageVm.Content = "[Error occurred]";
            }
        }
        finally
        {
            // Always clean up generation state
            IsGenerating = false;
            _generationCts?.Dispose();
            _generationCts = null;
        }
    }

    /// <summary>
    /// Cancels the current text generation operation.
    /// </summary>
    [RelayCommand]
    private void CancelGeneration()
    {
        // Signal the generation task to stop
        _generationCts?.Cancel();
    }

    /// <summary>
    /// Clears all messages from the chat and resets the conversation.
    /// </summary>
    [RelayCommand]
    private void ClearChat()
    {
        // Clear UI message list
        Messages.Clear();
        
        // Clear conversation service history
        _conversationService.ClearConversation();
        
        // Clear any displayed errors
        ClearError();
    }

    /// <summary>
    /// Handles the Enter key press to send a message.
    /// Called from the view's key binding.
    /// </summary>
    public void HandleEnterKey()
    {
        // Only send if conditions are met (has input, not generating, model loaded)
        if (CanSend)
        {
            SendMessageCommand.Execute(null);
        }
    }
}
