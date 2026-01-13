using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the system prompt editor dialog providing CRUD operations,
/// dirty tracking, and validation for system prompt management.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel manages the system prompt editor window with:
/// </para>
/// <list type="bullet">
///   <item><description><b>Prompt Lists:</b> Separate lists for user prompts and templates</description></item>
///   <item><description><b>Editor State:</b> Name, description, category, and content editing</description></item>
///   <item><description><b>Dirty Tracking:</b> Detects unsaved changes via comparison with original</description></item>
///   <item><description><b>Validation:</b> CanSave computed from content validity and dirty state</description></item>
///   <item><description><b>CRUD Commands:</b> Create, save, delete, duplicate, set as default</description></item>
///   <item><description><b>Event Sync:</b> Refreshes lists when service fires PromptListChanged</description></item>
/// </list>
/// <para>
/// <b>Dirty Tracking Strategy:</b>
/// </para>
/// <para>
/// When a prompt is selected, its state is captured in <c>_originalPrompt</c>.
/// As the user edits, <see cref="UpdateDirtyState"/> compares current values
/// against the original to determine if changes exist. This approach avoids
/// service calls during editing for better responsiveness.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// </para>
/// <list type="bullet">
///   <item><description>All property changes must occur on the UI thread</description></item>
///   <item><description>Service events may fire from any thread; <see cref="IDispatcher"/> marshals to UI</description></item>
///   <item><description>ObservableCollections must be modified on the UI thread</description></item>
/// </list>
/// <para>
/// <b>Logging:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>[ENTER]:</b> Method entry with parameters</description></item>
///   <item><description><b>[INFO]:</b> Significant state changes</description></item>
///   <item><description><b>[SKIP]:</b> No-op conditions (e.g., already updating from service)</description></item>
///   <item><description><b>[EXIT]:</b> Method completion with duration</description></item>
///   <item><description><b>[EVENT]:</b> Event handlers invoked</description></item>
///   <item><description><b>[INIT]:</b> Constructor completion</description></item>
///   <item><description><b>[DISPOSE]:</b> Resource cleanup</description></item>
/// </list>
/// <para>Added in v0.2.4c.</para>
/// </remarks>
/// <seealso cref="ISystemPromptService"/>
/// <seealso cref="SystemPromptViewModel"/>
/// <seealso cref="SystemPrompt"/>
public sealed partial class SystemPromptEditorViewModel : ViewModelBase, IDisposable
{
    #region Constants

    /// <summary>
    /// Approximate characters per token for display estimation.
    /// </summary>
    private const int CharactersPerToken = 4;

    /// <summary>
    /// Default name for new prompts.
    /// </summary>
    private const string DefaultNewPromptName = "New Prompt";

    /// <summary>
    /// Default category for new prompts.
    /// </summary>
    private const string DefaultCategory = "General";

    #endregion

    #region Fields

    private readonly ISystemPromptService _promptService;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<SystemPromptEditorViewModel>? _logger;

    /// <summary>
    /// Stores the original prompt state for dirty tracking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a prompt is loaded into the editor, this captures its state.
    /// <see cref="UpdateDirtyState"/> compares current values against this
    /// to determine if changes exist.
    /// </para>
    /// <para>
    /// Null when creating a new prompt (<see cref="IsNewPrompt"/> = true)
    /// or when no prompt is selected.
    /// </para>
    /// </remarks>
    private SystemPrompt? _originalPrompt;

    /// <summary>
    /// Flag indicating whether the ViewModel has been disposed.
    /// </summary>
    private bool _isDisposed;

    #endregion

    #region Observable Properties - Prompt Lists

    /// <summary>
    /// Gets the collection of user-created (non-built-in) prompts.
    /// </summary>
    /// <value>
    /// An observable collection of <see cref="SystemPromptViewModel"/> instances
    /// representing user prompts. Populated by <see cref="LoadPromptsAsync"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Separated from templates for cleaner UI organization in the sidebar.
    /// User prompts can be edited, duplicated, and deleted.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<SystemPromptViewModel> _userPrompts = [];

    /// <summary>
    /// Gets the collection of built-in template prompts.
    /// </summary>
    /// <value>
    /// An observable collection of <see cref="SystemPromptViewModel"/> instances
    /// representing templates. Populated by <see cref="LoadPromptsAsync"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Templates are read-only and can be cloned via <see cref="CreateFromTemplateCommand"/>.
    /// They cannot be edited or deleted directly.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<SystemPromptViewModel> _templates = [];

    /// <summary>
    /// Gets or sets the currently selected prompt in the list.
    /// </summary>
    /// <value>
    /// The <see cref="SystemPromptViewModel"/> currently selected, or null if none.
    /// </value>
    /// <remarks>
    /// <para>
    /// When changed, <see cref="OnSelectedPromptChanged"/> loads the prompt
    /// into the editor and updates capability flags.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private SystemPromptViewModel? _selectedPrompt;

    #endregion

    #region Observable Properties - Editor State

    /// <summary>
    /// Gets or sets the name of the prompt being edited.
    /// </summary>
    /// <value>
    /// The display name for the prompt. Must be unique and non-empty.
    /// </value>
    [ObservableProperty]
    private string _promptName = string.Empty;

    /// <summary>
    /// Gets or sets the description of the prompt being edited.
    /// </summary>
    /// <value>
    /// An optional description explaining the prompt's purpose.
    /// </value>
    [ObservableProperty]
    private string _promptDescription = string.Empty;

    /// <summary>
    /// Gets or sets the category of the prompt being edited.
    /// </summary>
    /// <value>
    /// The category for organization (e.g., "General", "Code", "Creative").
    /// </value>
    [ObservableProperty]
    private string _promptCategory = DefaultCategory;

    /// <summary>
    /// Gets or sets the content of the prompt being edited.
    /// </summary>
    /// <value>
    /// The actual system prompt text sent to the model.
    /// </value>
    /// <remarks>
    /// <para>
    /// When changed, <see cref="OnEditorContentChanged"/> updates computed properties
    /// (CharacterCount, TokenCount) and recalculates dirty state.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _editorContent = string.Empty;

    /// <summary>
    /// Gets or sets whether the editor has unsaved changes.
    /// </summary>
    /// <value>
    /// True if any editable property differs from <c>_originalPrompt</c>;
    /// false if no changes or no prompt is selected.
    /// </value>
    /// <remarks>
    /// <para>
    /// Computed by <see cref="UpdateDirtyState"/> whenever an editable property changes.
    /// Used to enable/disable the Save button and show unsaved indicators.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>
    /// Gets or sets whether the editor is in editing mode.
    /// </summary>
    /// <value>
    /// True if the user can edit the content; false if read-only.
    /// </value>
    /// <remarks>
    /// <para>
    /// Set to false when viewing built-in templates (they must be cloned to edit).
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// Gets or sets whether a new prompt is being created.
    /// </summary>
    /// <value>
    /// True if <see cref="CreateNewPromptCommand"/> was invoked and the prompt
    /// has not yet been saved; false for existing prompts.
    /// </value>
    /// <remarks>
    /// <para>
    /// When true, <see cref="SavePromptCommand"/> creates a new prompt via
    /// <see cref="ISystemPromptService.CreatePromptAsync"/>. When false, it
    /// updates the existing prompt via <see cref="ISystemPromptService.UpdatePromptAsync"/>.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isNewPrompt;

    #endregion

    #region Observable Properties - UI State

    /// <summary>
    /// Gets or sets whether an async operation is in progress.
    /// </summary>
    /// <value>
    /// True while loading prompts or performing CRUD operations; false otherwise.
    /// </value>
    /// <remarks>
    /// <para>
    /// Bound to loading spinners and used to disable controls during operations.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the current validation error message.
    /// </summary>
    /// <value>
    /// An error message if validation fails; null if valid.
    /// </value>
    /// <remarks>
    /// <para>
    /// Displayed inline in the editor to provide immediate feedback.
    /// Separate from <see cref="ViewModelBase.ErrorMessage"/> which is for operation errors.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string? _validationError;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the character count of the editor content.
    /// </summary>
    /// <value>
    /// The length of <see cref="EditorContent"/>, or 0 if null.
    /// </value>
    public int CharacterCount => EditorContent?.Length ?? 0;

    /// <summary>
    /// Gets an estimated token count based on character length.
    /// </summary>
    /// <value>
    /// Approximately CharacterCount / 4 (rough estimate).
    /// </value>
    /// <remarks>
    /// <para>
    /// This is a rough approximation. Actual token counts depend on
    /// the specific tokenizer and content type.
    /// </para>
    /// </remarks>
    public int EstimatedTokenCount => CharacterCount / CharactersPerToken;

    /// <summary>
    /// Gets a formatted string showing the character count.
    /// </summary>
    /// <value>
    /// Format: "X characters" or "X,XXX characters".
    /// </value>
    public string CharacterCountText => $"{CharacterCount:N0} characters";

    /// <summary>
    /// Gets a formatted string showing the estimated token count.
    /// </summary>
    /// <value>
    /// Format: "~X tokens" or "~X,XXX tokens".
    /// </value>
    public string TokenCountText => $"~{EstimatedTokenCount:N0} tokens";

    /// <summary>
    /// Gets whether the editor has non-empty content.
    /// </summary>
    /// <value>
    /// True if <see cref="EditorContent"/> is not null, empty, or whitespace.
    /// </value>
    public bool HasContent => !string.IsNullOrWhiteSpace(EditorContent);

    /// <summary>
    /// Gets whether the prompt has a valid (non-empty) name.
    /// </summary>
    /// <value>
    /// True if <see cref="PromptName"/> is not null, empty, or whitespace.
    /// </value>
    public bool HasValidName => !string.IsNullOrWhiteSpace(PromptName);

    /// <summary>
    /// Gets whether the current state can be saved.
    /// </summary>
    /// <value>
    /// True if the prompt has a valid name, has content, is dirty,
    /// and has no validation errors.
    /// </value>
    /// <remarks>
    /// <para>
    /// Used as CanExecute for <see cref="SavePromptCommand"/>.
    /// </para>
    /// </remarks>
    public bool CanSave => HasValidName && HasContent && IsDirty && string.IsNullOrEmpty(ValidationError);

    /// <summary>
    /// Gets whether the current prompt can be edited.
    /// </summary>
    /// <value>
    /// True if a non-built-in prompt is selected, or if creating a new prompt.
    /// </value>
    /// <remarks>
    /// <para>
    /// Built-in templates cannot be edited directly; they must be cloned first.
    /// </para>
    /// </remarks>
    public bool CanEdit => IsNewPrompt || (_originalPrompt != null && !_originalPrompt.IsBuiltIn);

    /// <summary>
    /// Gets whether the current prompt can be deleted.
    /// </summary>
    /// <value>
    /// True if a non-built-in prompt is selected and we're not creating a new prompt.
    /// </value>
    /// <remarks>
    /// <para>
    /// Built-in prompts cannot be deleted.
    /// </para>
    /// </remarks>
    public bool CanDelete => !IsNewPrompt && _originalPrompt != null && !_originalPrompt.IsBuiltIn;

    /// <summary>
    /// Gets whether the current prompt can be set as default.
    /// </summary>
    /// <value>
    /// True if a prompt is selected, we're not creating new, and it's not already default.
    /// </value>
    public bool CanSetDefault => !IsNewPrompt && _originalPrompt != null && !_originalPrompt.IsDefault;

    /// <summary>
    /// Gets whether a prompt can be duplicated.
    /// </summary>
    /// <value>
    /// True if a prompt is selected and we're not creating new.
    /// </value>
    public bool CanDuplicate => !IsNewPrompt && _originalPrompt != null;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptEditorViewModel"/> class.
    /// </summary>
    /// <param name="promptService">The system prompt service for CRUD operations.</param>
    /// <param name="dispatcher">The dispatcher for UI thread marshalling.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="promptService"/> or <paramref name="dispatcher"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor:
    /// </para>
    /// <list type="number">
    ///   <item><description>Stores service and dispatcher references</description></item>
    ///   <item><description>Subscribes to service events (PromptListChanged)</description></item>
    /// </list>
    /// <para>
    /// <b>Important:</b> Call <see cref="InitializeAsync"/> after construction to
    /// load prompts and fully initialize the ViewModel.
    /// </para>
    /// </remarks>
    public SystemPromptEditorViewModel(
        ISystemPromptService promptService,
        IDispatcher dispatcher,
        ILogger<SystemPromptEditorViewModel>? logger = null)
    {
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger;

        _logger?.LogDebug("[INIT] SystemPromptEditorViewModel - Subscribing to service events");

        // Subscribe to service events for list synchronization.
        _promptService.PromptListChanged += OnPromptListChanged;

        _logger?.LogDebug("[INIT] SystemPromptEditorViewModel - Initialization complete");
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Called when <see cref="SelectedPrompt"/> changes.
    /// </summary>
    /// <param name="value">The newly selected prompt ViewModel.</param>
    /// <remarks>
    /// <para>
    /// Loads the selected prompt into the editor and updates capability flags.
    /// If the user has unsaved changes, this will overwrite them (the UI should
    /// prompt for confirmation before selection changes).
    /// </para>
    /// </remarks>
    partial void OnSelectedPromptChanged(SystemPromptViewModel? value)
    {
        _logger?.LogDebug("[INFO] OnSelectedPromptChanged - Prompt: {Name}", value?.Name ?? "(null)");

        if (value == null)
        {
            ClearEditor();
            return;
        }

        LoadPromptIntoEditor(value);
    }

    /// <summary>
    /// Called when <see cref="PromptName"/> changes.
    /// </summary>
    /// <param name="value">The new name value.</param>
    partial void OnPromptNameChanged(string value)
    {
        _logger?.LogDebug("[INFO] OnPromptNameChanged - Value: {Value}", value);

        UpdateDirtyState();
        ValidatePrompt();

        OnPropertyChanged(nameof(HasValidName));
        OnPropertyChanged(nameof(CanSave));

        SavePromptCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when <see cref="PromptDescription"/> changes.
    /// </summary>
    /// <param name="value">The new description value.</param>
    partial void OnPromptDescriptionChanged(string value)
    {
        _logger?.LogDebug("[INFO] OnPromptDescriptionChanged - Length: {Length}", value?.Length ?? 0);

        UpdateDirtyState();
        SavePromptCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when <see cref="PromptCategory"/> changes.
    /// </summary>
    /// <param name="value">The new category value.</param>
    partial void OnPromptCategoryChanged(string value)
    {
        _logger?.LogDebug("[INFO] OnPromptCategoryChanged - Value: {Value}", value);

        UpdateDirtyState();
        SavePromptCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when <see cref="EditorContent"/> changes.
    /// </summary>
    /// <param name="value">The new content value.</param>
    /// <remarks>
    /// <para>
    /// Updates computed properties (character/token counts) and recalculates dirty state.
    /// </para>
    /// </remarks>
    partial void OnEditorContentChanged(string value)
    {
        _logger?.LogDebug("[INFO] OnEditorContentChanged - Length: {Length}", value?.Length ?? 0);

        UpdateDirtyState();
        ValidatePrompt();

        // Notify computed properties.
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(EstimatedTokenCount));
        OnPropertyChanged(nameof(CharacterCountText));
        OnPropertyChanged(nameof(TokenCountText));
        OnPropertyChanged(nameof(HasContent));
        OnPropertyChanged(nameof(CanSave));

        SavePromptCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when <see cref="IsDirty"/> changes.
    /// </summary>
    /// <param name="value">The new dirty state.</param>
    partial void OnIsDirtyChanged(bool value)
    {
        _logger?.LogDebug("[INFO] OnIsDirtyChanged - Value: {Value}", value);

        OnPropertyChanged(nameof(CanSave));
        SavePromptCommand.NotifyCanExecuteChanged();
        DiscardChangesCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when <see cref="IsNewPrompt"/> changes.
    /// </summary>
    /// <param name="value">The new IsNewPrompt state.</param>
    partial void OnIsNewPromptChanged(bool value)
    {
        _logger?.LogDebug("[INFO] OnIsNewPromptChanged - Value: {Value}", value);

        // Update capability properties.
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanSetDefault));
        OnPropertyChanged(nameof(CanDuplicate));

        // Update command states.
        DeletePromptCommand.NotifyCanExecuteChanged();
        SetAsDefaultCommand.NotifyCanExecuteChanged();
        DuplicatePromptCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the PromptListChanged event from the service.
    /// </summary>
    /// <param name="sender">The service that raised the event.</param>
    /// <param name="e">The event arguments containing change details.</param>
    /// <remarks>
    /// <para>
    /// Reloads the prompt lists to reflect the change. Does not discard
    /// unsaved changes in the editor.
    /// </para>
    /// </remarks>
    private async void OnPromptListChanged(object? sender, PromptListChangedEventArgs e)
    {
        _logger?.LogDebug(
            "[EVENT] OnPromptListChanged - Type: {Type}, AffectedId: {Id}, AffectedName: {Name}",
            e.ChangeType, e.AffectedPromptId, e.AffectedPromptName);

        try
        {
            await LoadPromptsAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[EVENT] OnPromptListChanged - Failed to reload prompts");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Loads a prompt into the editor from its ViewModel.
    /// </summary>
    /// <param name="promptVm">The prompt ViewModel to load.</param>
    private async void LoadPromptIntoEditor(SystemPromptViewModel promptVm)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] LoadPromptIntoEditor - Id: {Id}, Name: {Name}", promptVm.Id, promptVm.Name);

        try
        {
            // Fetch the full prompt from the service.
            var prompt = await _promptService.GetByIdAsync(promptVm.Id);

            if (prompt == null)
            {
                _logger?.LogWarning("[EXIT] LoadPromptIntoEditor - Prompt not found: {Id}", promptVm.Id);
                ClearEditor();
                return;
            }

            // Store original for dirty tracking.
            _originalPrompt = prompt;

            // Populate editor fields.
            PromptName = prompt.Name;
            PromptDescription = prompt.Description ?? string.Empty;
            PromptCategory = prompt.Category;
            EditorContent = prompt.Content;

            // Update state flags.
            IsNewPrompt = false;
            IsEditing = !prompt.IsBuiltIn;
            IsDirty = false;
            ValidationError = null;

            // Update computed properties.
            NotifyCapabilityPropertiesChanged();

            stopwatch.Stop();
            _logger?.LogDebug(
                "[EXIT] LoadPromptIntoEditor - Name: {Name}, IsBuiltIn: {IsBuiltIn}, Duration: {Ms}ms",
                prompt.Name, prompt.IsBuiltIn, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] LoadPromptIntoEditor - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to load prompt: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all editor fields to an empty state.
    /// </summary>
    private void ClearEditor()
    {
        _logger?.LogDebug("[INFO] ClearEditor - Resetting editor state");

        _originalPrompt = null;
        PromptName = string.Empty;
        PromptDescription = string.Empty;
        PromptCategory = DefaultCategory;
        EditorContent = string.Empty;
        IsNewPrompt = false;
        IsEditing = false;
        IsDirty = false;
        ValidationError = null;

        NotifyCapabilityPropertiesChanged();
    }

    /// <summary>
    /// Updates the <see cref="IsDirty"/> flag based on current vs original values.
    /// </summary>
    private void UpdateDirtyState()
    {
        // No prompt selected and not creating new.
        if (_originalPrompt == null && !IsNewPrompt)
        {
            IsDirty = false;
            return;
        }

        // Creating new prompt: dirty if any content entered.
        if (IsNewPrompt)
        {
            IsDirty = !string.IsNullOrWhiteSpace(PromptName) ||
                      !string.IsNullOrWhiteSpace(EditorContent);
            return;
        }

        // Editing existing prompt: compare against original.
        IsDirty = PromptName != _originalPrompt!.Name ||
                  EditorContent != _originalPrompt.Content ||
                  (PromptDescription ?? string.Empty) != (_originalPrompt.Description ?? string.Empty) ||
                  PromptCategory != _originalPrompt.Category;
    }

    /// <summary>
    /// Validates the current prompt state and sets <see cref="ValidationError"/>.
    /// </summary>
    private void ValidatePrompt()
    {
        if (string.IsNullOrWhiteSpace(PromptName))
        {
            ValidationError = "Name is required.";
            return;
        }

        if (PromptName.Length > SystemPrompt.NameMaxLength)
        {
            ValidationError = $"Name must not exceed {SystemPrompt.NameMaxLength} characters.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditorContent))
        {
            ValidationError = "Content is required.";
            return;
        }

        if (EditorContent.Length > SystemPrompt.ContentMaxLength)
        {
            ValidationError = $"Content must not exceed {SystemPrompt.ContentMaxLength:N0} characters.";
            return;
        }

        ValidationError = null;
    }

    /// <summary>
    /// Notifies that all capability computed properties may have changed.
    /// </summary>
    private void NotifyCapabilityPropertiesChanged()
    {
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanSetDefault));
        OnPropertyChanged(nameof(CanDuplicate));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasValidName));
        OnPropertyChanged(nameof(HasContent));

        // Update command states.
        SavePromptCommand.NotifyCanExecuteChanged();
        DeletePromptCommand.NotifyCanExecuteChanged();
        SetAsDefaultCommand.NotifyCanExecuteChanged();
        DuplicatePromptCommand.NotifyCanExecuteChanged();
        DiscardChangesCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Selects a prompt by its ID in the appropriate list.
    /// </summary>
    /// <param name="id">The ID of the prompt to select.</param>
    private void SelectPromptById(Guid id)
    {
        _logger?.LogDebug("[INFO] SelectPromptById - Id: {Id}", id);

        // Search in user prompts first.
        var match = UserPrompts.FirstOrDefault(p => p.Id == id);
        if (match != null)
        {
            SelectedPrompt = match;
            return;
        }

        // Then search in templates.
        match = Templates.FirstOrDefault(p => p.Id == id);
        if (match != null)
        {
            SelectedPrompt = match;
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Initializes the ViewModel by loading prompts.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this after construction to fully initialize the ViewModel.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] InitializeAsync");

        try
        {
            IsLoading = true;
            ClearError();

            await LoadPromptsAsync();

            // Select the first user prompt, or first template if no user prompts.
            if (UserPrompts.Count > 0)
            {
                SelectedPrompt = UserPrompts[0];
            }
            else if (Templates.Count > 0)
            {
                SelectedPrompt = Templates[0];
            }

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] InitializeAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] InitializeAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to initialize: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads all prompts from the service and populates the lists.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task LoadPromptsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] LoadPromptsAsync");

        try
        {
            IsLoading = true;

            var userPrompts = await _promptService.GetUserPromptsAsync();
            var templates = await _promptService.GetTemplatesAsync();

            await _dispatcher.InvokeAsync(() =>
            {
                UserPrompts.Clear();
                foreach (var p in userPrompts)
                {
                    UserPrompts.Add(new SystemPromptViewModel(p));
                }

                Templates.Clear();
                foreach (var t in templates)
                {
                    Templates.Add(new SystemPromptViewModel(t));
                }
            });

            stopwatch.Stop();
            _logger?.LogDebug(
                "[EXIT] LoadPromptsAsync - UserCount: {UserCount}, TemplateCount: {TemplateCount}, Duration: {Ms}ms",
                userPrompts.Count, templates.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] LoadPromptsAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new prompt, initializing the editor with empty fields.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private Task CreateNewPromptAsync()
    {
        _logger?.LogDebug("[ENTER] CreateNewPromptAsync");

        // Clear selection.
        SelectedPrompt = null;
        _originalPrompt = null;

        // Initialize editor for new prompt.
        PromptName = DefaultNewPromptName;
        PromptDescription = string.Empty;
        PromptCategory = DefaultCategory;
        EditorContent = string.Empty;

        IsNewPrompt = true;
        IsEditing = true;
        IsDirty = false;
        ValidationError = null;

        NotifyCapabilityPropertiesChanged();

        _logger?.LogDebug("[EXIT] CreateNewPromptAsync - Editor initialized for new prompt");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the current prompt (creates new or updates existing).
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SavePromptAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug(
            "[ENTER] SavePromptAsync - IsNewPrompt: {IsNew}, Name: {Name}",
            IsNewPrompt, PromptName);

        try
        {
            IsLoading = true;
            ClearError();

            SystemPrompt savedPrompt;

            if (IsNewPrompt)
            {
                // Create new prompt.
                savedPrompt = await _promptService.CreatePromptAsync(
                    PromptName.Trim(),
                    EditorContent,
                    string.IsNullOrWhiteSpace(PromptDescription) ? null : PromptDescription.Trim(),
                    PromptCategory);

                _logger?.LogInformation(
                    "[INFO] SavePromptAsync - Created new prompt: {Name} ({Id})",
                    savedPrompt.Name, savedPrompt.Id);
            }
            else if (_originalPrompt != null)
            {
                // Update existing prompt.
                savedPrompt = await _promptService.UpdatePromptAsync(
                    _originalPrompt.Id,
                    PromptName.Trim(),
                    EditorContent,
                    string.IsNullOrWhiteSpace(PromptDescription) ? null : PromptDescription.Trim(),
                    PromptCategory);

                _logger?.LogInformation(
                    "[INFO] SavePromptAsync - Updated prompt: {Name} ({Id})",
                    savedPrompt.Name, savedPrompt.Id);
            }
            else
            {
                _logger?.LogWarning("[EXIT] SavePromptAsync - No prompt to save");
                return;
            }

            // Update state.
            _originalPrompt = savedPrompt;
            IsNewPrompt = false;
            IsDirty = false;

            // Reload lists and reselect the saved prompt.
            await LoadPromptsAsync();
            SelectPromptById(savedPrompt.Id);

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] SavePromptAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            stopwatch.Stop();
            _logger?.LogWarning("[EXIT] SavePromptAsync - Name conflict: {Message}", ex.Message);
            ValidationError = "A prompt with this name already exists.";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] SavePromptAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to save prompt: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Deletes the current prompt.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeletePromptAsync()
    {
        if (_originalPrompt == null || _originalPrompt.IsBuiltIn)
        {
            _logger?.LogDebug("[SKIP] DeletePromptAsync - Cannot delete: null or built-in");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] DeletePromptAsync - Name: {Name}, Id: {Id}",
            _originalPrompt.Name, _originalPrompt.Id);

        try
        {
            IsLoading = true;
            ClearError();

            await _promptService.DeletePromptAsync(_originalPrompt.Id);

            _logger?.LogInformation("[INFO] DeletePromptAsync - Deleted: {Name}", _originalPrompt.Name);

            // Reload lists and select first available.
            await LoadPromptsAsync();

            if (UserPrompts.Count > 0)
            {
                SelectedPrompt = UserPrompts[0];
            }
            else if (Templates.Count > 0)
            {
                SelectedPrompt = Templates[0];
            }
            else
            {
                ClearEditor();
            }

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] DeletePromptAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] DeletePromptAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to delete prompt: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Duplicates the current prompt.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand(CanExecute = nameof(CanDuplicate))]
    private async Task DuplicatePromptAsync()
    {
        if (_originalPrompt == null)
        {
            _logger?.LogDebug("[SKIP] DuplicatePromptAsync - No prompt selected");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] DuplicatePromptAsync - Source: {Name}", _originalPrompt.Name);

        try
        {
            IsLoading = true;
            ClearError();

            var duplicate = await _promptService.DuplicatePromptAsync(_originalPrompt.Id);

            _logger?.LogInformation(
                "[INFO] DuplicatePromptAsync - Created duplicate: {Name} ({Id})",
                duplicate.Name, duplicate.Id);

            // Reload lists and select the new duplicate.
            await LoadPromptsAsync();
            SelectPromptById(duplicate.Id);

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] DuplicatePromptAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] DuplicatePromptAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to duplicate prompt: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Sets the current prompt as the default.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand(CanExecute = nameof(CanSetDefault))]
    private async Task SetAsDefaultAsync()
    {
        if (_originalPrompt == null)
        {
            _logger?.LogDebug("[SKIP] SetAsDefaultAsync - No prompt selected");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] SetAsDefaultAsync - Name: {Name}", _originalPrompt.Name);

        try
        {
            IsLoading = true;
            ClearError();

            await _promptService.SetAsDefaultAsync(_originalPrompt.Id);

            _logger?.LogInformation("[INFO] SetAsDefaultAsync - Set default: {Name}", _originalPrompt.Name);

            // Reload to update IsDefault flags.
            await LoadPromptsAsync();

            // Refresh the original prompt to get updated IsDefault.
            _originalPrompt = await _promptService.GetByIdAsync(_originalPrompt.Id);
            NotifyCapabilityPropertiesChanged();

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] SetAsDefaultAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] SetAsDefaultAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to set as default: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new prompt from a template.
    /// </summary>
    /// <param name="template">The template to clone.</param>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task CreateFromTemplateAsync(SystemPromptViewModel? template)
    {
        if (template == null)
        {
            _logger?.LogDebug("[SKIP] CreateFromTemplateAsync - Template is null");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] CreateFromTemplateAsync - Template: {Name}", template.Name);

        try
        {
            IsLoading = true;
            ClearError();

            var newPrompt = await _promptService.CreateFromTemplateAsync(template.Id);

            _logger?.LogInformation(
                "[INFO] CreateFromTemplateAsync - Created from template: {Name} ({Id})",
                newPrompt.Name, newPrompt.Id);

            // Reload lists and select the new prompt.
            await LoadPromptsAsync();
            SelectPromptById(newPrompt.Id);

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] CreateFromTemplateAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] CreateFromTemplateAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to create from template: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Discards unsaved changes and reverts to the original prompt state.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    [RelayCommand(CanExecute = nameof(IsDirty))]
    private Task DiscardChangesAsync()
    {
        _logger?.LogDebug("[ENTER] DiscardChangesAsync");

        if (IsNewPrompt)
        {
            // Discard new prompt creation.
            ClearEditor();

            // Select first available prompt.
            if (UserPrompts.Count > 0)
            {
                SelectedPrompt = UserPrompts[0];
            }
            else if (Templates.Count > 0)
            {
                SelectedPrompt = Templates[0];
            }
        }
        else if (_originalPrompt != null)
        {
            // Revert to original values.
            PromptName = _originalPrompt.Name;
            PromptDescription = _originalPrompt.Description ?? string.Empty;
            PromptCategory = _originalPrompt.Category;
            EditorContent = _originalPrompt.Content;
            IsDirty = false;
            ValidationError = null;
        }

        _logger?.LogDebug("[EXIT] DiscardChangesAsync");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears the current error message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by the error banner dismiss button in the UI.
    /// Added in v0.2.4d.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void ClearErrorMessage()
    {
        _logger?.LogDebug("[INFO] ClearErrorMessage - Clearing error from UI");
        ClearError();
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the ViewModel, unsubscribing from events and releasing resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Must be called when the ViewModel is no longer needed to prevent memory leaks
    /// from event subscriptions.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _logger?.LogDebug("[DISPOSE] SystemPromptEditorViewModel - Unsubscribing from events");

        // Unsubscribe from service events.
        _promptService.PromptListChanged -= OnPromptListChanged;

        _isDisposed = true;

        _logger?.LogDebug("[DISPOSE] SystemPromptEditorViewModel - Disposal complete");
    }

    #endregion
}
