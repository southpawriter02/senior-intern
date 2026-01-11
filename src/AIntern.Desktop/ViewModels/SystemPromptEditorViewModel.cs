using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the system prompt editor window.
/// Handles CRUD operations, dirty tracking, and validation.
/// </summary>
public sealed partial class SystemPromptEditorViewModel : ViewModelBase, IDisposable
{
    private readonly ISystemPromptService _promptService;
    private SystemPrompt? _originalPrompt;
    private bool _disposed;

    // Prompt Lists
    [ObservableProperty]
    private ObservableCollection<SystemPromptViewModel> _userPrompts = new();

    [ObservableProperty]
    private ObservableCollection<SystemPromptViewModel> _templates = new();

    [ObservableProperty]
    private SystemPromptViewModel? _selectedPrompt;

    // Editor State
    [ObservableProperty]
    private string _promptName = string.Empty;

    [ObservableProperty]
    private string _promptDescription = string.Empty;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isNewPrompt;

    [ObservableProperty]
    private bool _canEdit;

    [ObservableProperty]
    private bool _canDelete;

    [ObservableProperty]
    private bool _canSetDefault;

    // UI State
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _validationError;

    // Computed Properties
    public int CharacterCount => EditorContent?.Length ?? 0;
    public int EstimatedTokenCount => CharacterCount / 4;
    public string CharacterCountText => $"{CharacterCount:N0} characters";
    public string TokenCountText => $"~{EstimatedTokenCount:N0} tokens";
    public bool HasContent => !string.IsNullOrWhiteSpace(EditorContent);
    public bool HasValidName => !string.IsNullOrWhiteSpace(PromptName);
    public bool CanSave => HasValidName && HasContent && IsDirty && string.IsNullOrEmpty(ValidationError);

    public SystemPromptEditorViewModel(ISystemPromptService promptService)
    {
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _promptService.PromptListChanged += OnPromptListChanged;
    }

    #region Property Change Handlers

    partial void OnSelectedPromptChanged(SystemPromptViewModel? value)
    {
        if (value != null)
        {
            LoadPromptIntoEditor(value);
        }
        else
        {
            ClearEditor();
        }
    }

    partial void OnPromptNameChanged(string value)
    {
        UpdateDirtyState();
        ValidatePrompt();
        OnPropertyChanged(nameof(HasValidName));
        OnPropertyChanged(nameof(CanSave));
        SavePromptCommand.NotifyCanExecuteChanged();
    }

    partial void OnPromptDescriptionChanged(string value)
    {
        UpdateDirtyState();
        OnPropertyChanged(nameof(CanSave));
        SavePromptCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditorContentChanged(string value)
    {
        UpdateDirtyState();
        ValidatePrompt();
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(EstimatedTokenCount));
        OnPropertyChanged(nameof(CharacterCountText));
        OnPropertyChanged(nameof(TokenCountText));
        OnPropertyChanged(nameof(HasContent));
        OnPropertyChanged(nameof(CanSave));
        SavePromptCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsDirtyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SavePromptCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanDeleteChanged(bool value)
    {
        DeletePromptCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanSetDefaultChanged(bool value)
    {
        SetAsDefaultCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadPromptsAsync()
    {
        IsLoading = true;
        try
        {
            var userPrompts = await _promptService.GetUserPromptsAsync();
            var templates = await _promptService.GetTemplatesAsync();

            UserPrompts.Clear();
            foreach (var prompt in userPrompts)
            {
                UserPrompts.Add(new SystemPromptViewModel(prompt));
            }

            Templates.Clear();
            foreach (var prompt in templates)
            {
                Templates.Add(new SystemPromptViewModel(prompt));
            }
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
    private void CreateNewPrompt()
    {
        // Deselect current
        SelectedPrompt = null;

        // Set up new prompt state
        IsNewPrompt = true;
        IsEditing = true;
        CanEdit = true;
        CanDelete = false;
        CanSetDefault = false;

        // Initialize editor
        PromptName = "New Prompt";
        PromptDescription = string.Empty;
        EditorContent = string.Empty;
        _originalPrompt = null;
        IsDirty = false;

        ClearError();
        ValidationError = null;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SavePromptAsync()
    {
        IsLoading = true;
        try
        {
            if (IsNewPrompt)
            {
                var prompt = await _promptService.CreatePromptAsync(
                    PromptName.Trim(),
                    EditorContent,
                    string.IsNullOrWhiteSpace(PromptDescription) ? null : PromptDescription.Trim());

                await LoadPromptsAsync();
                SelectPromptById(prompt.Id);
                IsNewPrompt = false;
            }
            else if (_originalPrompt != null)
            {
                await _promptService.UpdatePromptAsync(
                    _originalPrompt.Id,
                    PromptName.Trim(),
                    EditorContent,
                    string.IsNullOrWhiteSpace(PromptDescription) ? null : PromptDescription.Trim());

                _originalPrompt = await _promptService.GetByIdAsync(_originalPrompt.Id);
            }

            IsDirty = false;
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

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeletePromptAsync()
    {
        if (_originalPrompt == null || _originalPrompt.IsBuiltIn)
            return;

        IsLoading = true;
        try
        {
            await _promptService.DeletePromptAsync(_originalPrompt.Id);
            await LoadPromptsAsync();

            // Select first user prompt or clear
            SelectedPrompt = UserPrompts.FirstOrDefault();
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
    private async Task DuplicatePromptAsync()
    {
        if (_originalPrompt == null)
            return;

        IsLoading = true;
        try
        {
            var duplicated = await _promptService.DuplicatePromptAsync(_originalPrompt.Id);
            await LoadPromptsAsync();
            SelectPromptById(duplicated.Id);
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

    [RelayCommand(CanExecute = nameof(CanSetDefault))]
    private async Task SetAsDefaultAsync()
    {
        if (_originalPrompt == null)
            return;

        IsLoading = true;
        try
        {
            await _promptService.SetAsDefaultAsync(_originalPrompt.Id);
            await LoadPromptsAsync();

            // Update selected prompt's IsDefault
            if (SelectedPrompt != null)
            {
                SelectedPrompt.IsDefault = true;
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

    [RelayCommand]
    private async Task CreateFromTemplateAsync(SystemPromptViewModel? template)
    {
        if (template == null)
            return;

        IsLoading = true;
        try
        {
            var created = await _promptService.CreateFromTemplateAsync(template.Id);
            await LoadPromptsAsync();
            SelectPromptById(created.Id);
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
    private void LoadTemplate(SystemPromptViewModel? template)
    {
        if (template == null)
            return;

        // Load template content into editor without selecting it
        // This allows viewing template content
        _originalPrompt = null;
        PromptName = template.Name;
        PromptDescription = template.Description ?? string.Empty;
        EditorContent = template.Content;

        IsNewPrompt = false;
        IsEditing = false;
        CanEdit = false; // Can't edit built-in templates
        CanDelete = false;
        CanSetDefault = !template.IsDefault;
        IsDirty = false;
    }

    [RelayCommand]
    private void DiscardChanges()
    {
        if (_originalPrompt != null)
        {
            // Reload from original
            PromptName = _originalPrompt.Name;
            PromptDescription = _originalPrompt.Description ?? string.Empty;
            EditorContent = _originalPrompt.Content;
        }
        else if (IsNewPrompt)
        {
            // Clear new prompt state
            PromptName = "New Prompt";
            PromptDescription = string.Empty;
            EditorContent = string.Empty;
        }

        IsDirty = false;
        ValidationError = null;
        ClearError();
    }

    [RelayCommand]
    private void StartEditing()
    {
        if (CanEdit)
        {
            IsEditing = true;
        }
    }

    [RelayCommand]
    private void SelectPrompt(SystemPromptViewModel? prompt)
    {
        SelectedPrompt = prompt;
    }

    [RelayCommand]
    private void ClearValidationError()
    {
        ValidationError = null;
    }

    #endregion

    #region Private Helpers

    private void LoadPromptIntoEditor(SystemPromptViewModel viewModel)
    {
        // Load the full prompt from service to get fresh data
        _ = LoadPromptDetailsAsync(viewModel.Id);
    }

    private async Task LoadPromptDetailsAsync(Guid promptId)
    {
        IsLoading = true;
        try
        {
            var prompt = await _promptService.GetByIdAsync(promptId);
            if (prompt == null)
            {
                SetError("Prompt not found.");
                return;
            }

            _originalPrompt = prompt;
            PromptName = prompt.Name;
            PromptDescription = prompt.Description ?? string.Empty;
            EditorContent = prompt.Content;

            IsNewPrompt = false;
            IsEditing = false;
            CanEdit = !prompt.IsBuiltIn;
            CanDelete = !prompt.IsBuiltIn;
            CanSetDefault = !prompt.IsDefault;
            IsDirty = false;

            ClearError();
            ValidationError = null;
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

    private void ClearEditor()
    {
        _originalPrompt = null;
        PromptName = string.Empty;
        PromptDescription = string.Empty;
        EditorContent = string.Empty;

        IsNewPrompt = false;
        IsEditing = false;
        CanEdit = false;
        CanDelete = false;
        CanSetDefault = false;
        IsDirty = false;

        ValidationError = null;
    }

    private void SelectPromptById(Guid id)
    {
        var prompt = UserPrompts.FirstOrDefault(p => p.Id == id)
                     ?? Templates.FirstOrDefault(p => p.Id == id);

        if (prompt != null)
        {
            SelectedPrompt = prompt;
        }
    }

    private void UpdateDirtyState()
    {
        if (_originalPrompt == null && !IsNewPrompt)
        {
            IsDirty = false;
            return;
        }

        if (IsNewPrompt)
        {
            IsDirty = !string.IsNullOrWhiteSpace(PromptName) && PromptName != "New Prompt"
                      || !string.IsNullOrWhiteSpace(EditorContent);
            return;
        }

        IsDirty = PromptName != _originalPrompt!.Name
                  || EditorContent != _originalPrompt.Content
                  || (PromptDescription ?? "") != (_originalPrompt.Description ?? "");
    }

    private void ValidatePrompt()
    {
        if (string.IsNullOrWhiteSpace(PromptName))
        {
            ValidationError = "Name is required.";
        }
        else if (PromptName.Length > 100)
        {
            ValidationError = "Name must be 100 characters or less.";
        }
        else if (!string.IsNullOrWhiteSpace(EditorContent) && EditorContent.Length > 50000)
        {
            ValidationError = "Content must be 50,000 characters or less.";
        }
        else
        {
            ValidationError = null;
        }
    }

    private async void OnPromptListChanged(object? sender, PromptListChangedEventArgs e)
    {
        await LoadPromptsAsync();
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;

        _promptService.PromptListChanged -= OnPromptListChanged;
        _disposed = true;
    }
}
