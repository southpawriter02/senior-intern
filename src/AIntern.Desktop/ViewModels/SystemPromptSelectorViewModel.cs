using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the quick prompt selector in the chat header.
/// </summary>
public sealed partial class SystemPromptSelectorViewModel : ViewModelBase, IDisposable
{
    private readonly ISystemPromptService _promptService;
    private readonly IConversationService _conversationService;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<SystemPromptViewModel> _availablePrompts = new();

    [ObservableProperty]
    private SystemPromptViewModel? _selectedPrompt;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasPromptSelected;

    public string DisplayText => SelectedPrompt?.Name ?? "No prompt selected";

    /// <summary>
    /// Action to open the system prompt editor window.
    /// Set by the parent view/viewmodel to handle opening the editor.
    /// </summary>
    public Action? OpenEditorAction { get; set; }

    public SystemPromptSelectorViewModel(
        ISystemPromptService promptService,
        IConversationService conversationService)
    {
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));

        _promptService.PromptListChanged += OnPromptListChanged;
        _promptService.CurrentPromptChanged += OnCurrentPromptChanged;
        _conversationService.ConversationChanged += OnConversationChanged;
    }

    public async Task InitializeAsync()
    {
        await LoadPromptsAsync();
        await SyncWithCurrentPromptAsync();
    }

    partial void OnSelectedPromptChanged(SystemPromptViewModel? value)
    {
        HasPromptSelected = value != null && value.Id != Guid.Empty;
        OnPropertyChanged(nameof(DisplayText));
    }

    [RelayCommand]
    private async Task ApplyToConversationAsync()
    {
        if (SelectedPrompt == null)
            return;

        IsLoading = true;
        try
        {
            // Guid.Empty means "None" option
            var promptId = SelectedPrompt.Id == Guid.Empty ? null : (Guid?)SelectedPrompt.Id;
            await _promptService.SetCurrentPromptAsync(promptId);
            ClearError();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadPromptsAsync()
    {
        IsLoading = true;
        try
        {
            var prompts = await _promptService.GetAllPromptsAsync();

            AvailablePrompts.Clear();

            // Add "None" option first
            AvailablePrompts.Add(new SystemPromptViewModel
            {
                Name = "(No system prompt)",
                Category = "None"
            });

            // Add all prompts
            foreach (var prompt in prompts)
            {
                AvailablePrompts.Add(new SystemPromptViewModel(prompt));
            }

            ClearError();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SyncWithCurrentPromptAsync()
    {
        var currentPrompt = _promptService.CurrentPrompt;

        if (currentPrompt == null)
        {
            // Select "None" option
            SelectedPrompt = AvailablePrompts.FirstOrDefault(p => p.Id == Guid.Empty);
        }
        else
        {
            SelectedPrompt = AvailablePrompts.FirstOrDefault(p => p.Id == currentPrompt.Id)
                             ?? AvailablePrompts.FirstOrDefault(p => p.Id == Guid.Empty);
        }

        await Task.CompletedTask;
    }

    private async void OnPromptListChanged(object? sender, PromptListChangedEventArgs e)
    {
        await LoadPromptsAsync();
        await SyncWithCurrentPromptAsync();
    }

    private async void OnCurrentPromptChanged(object? sender, CurrentPromptChangedEventArgs e)
    {
        await SyncWithCurrentPromptAsync();
    }

    private async void OnConversationChanged(object? sender, EventArgs e)
    {
        // When conversation changes, sync with current prompt
        await SyncWithCurrentPromptAsync();
    }

    [RelayCommand]
    private void OpenEditor()
    {
        OpenEditorAction?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _promptService.PromptListChanged -= OnPromptListChanged;
        _promptService.CurrentPromptChanged -= OnCurrentPromptChanged;
        _conversationService.ConversationChanged -= OnConversationChanged;
        _disposed = true;
    }
}
