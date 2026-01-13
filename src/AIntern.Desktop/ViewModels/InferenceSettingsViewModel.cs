using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the inference settings panel providing parameter binding,
/// preset management, and computed descriptions.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel bridges the UI parameter sliders with <see cref="IInferenceSettingsService"/>,
/// providing:
/// </para>
/// <list type="bullet">
///   <item><description><b>Parameter Properties:</b> Temperature, TopP, TopK, MaxTokens, ContextSize, RepetitionPenalty with two-way binding</description></item>
///   <item><description><b>Computed Descriptions:</b> Context-aware text explaining current parameter values</description></item>
///   <item><description><b>Debounced Updates:</b> 200ms delay before batching service updates to reduce event spam</description></item>
///   <item><description><b>Preset Management:</b> Commands for loading, saving, updating, and deleting presets</description></item>
///   <item><description><b>Two-Way Sync:</b> Event handlers sync UI when service changes externally</description></item>
/// </list>
/// <para>
/// <b>Debouncing Strategy:</b>
/// </para>
/// <para>
/// When a slider moves, the property setter fires immediately (for UI responsiveness).
/// The debounce timer resets on each change. Only after 200ms of inactivity does
/// <see cref="OnDebounceElapsed"/> fire to batch-update all parameters to the service.
/// This reduces redundant service calls during rapid slider dragging.
/// </para>
/// <para>
/// <b>Feedback Loop Prevention:</b>
/// </para>
/// <para>
/// The <c>_isUpdatingFromService</c> flag prevents infinite loops:
/// </para>
/// <code>
/// User drags slider → Property setter → QueueServiceUpdate() → Service.Update()
///                                                                    ↓
///                                      Service fires SettingsChanged event
///                                                                    ↓
///                         OnSettingsChanged() sets _isUpdatingFromService = true
///                                                                    ↓
///                                 SyncFromService() updates properties
///                         (Property setters see flag, skip QueueServiceUpdate)
///                                                                    ↓
///                                      _isUpdatingFromService = false
/// </code>
/// <para>
/// <b>Thread Safety:</b>
/// </para>
/// <list type="bullet">
///   <item><description>All property changes must occur on the UI thread</description></item>
///   <item><description>Service events may fire from any thread; <see cref="IDispatcher"/> marshals to UI</description></item>
///   <item><description>Debounce timer callback uses Dispatcher.InvokeAsync for thread safety</description></item>
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
/// </list>
/// </remarks>
/// <seealso cref="IInferenceSettingsService"/>
/// <seealso cref="InferencePresetViewModel"/>
/// <seealso cref="InferenceSettings"/>
public partial class InferenceSettingsViewModel : ViewModelBase, IDisposable
{
    #region Constants

    /// <summary>
    /// Debounce delay in milliseconds before batching parameter updates to the service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 200ms provides a good balance between responsiveness (user sees immediate slider movement)
    /// and efficiency (reduces service calls during rapid dragging).
    /// </para>
    /// </remarks>
    private const int DebounceDelayMs = 200;

    #endregion

    #region Fields

    private readonly IInferenceSettingsService _settingsService;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<InferenceSettingsViewModel>? _logger;

    /// <summary>
    /// Debounce timer for batching parameter updates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Timer is reset each time a parameter changes. When it elapses,
    /// all current parameter values are sent to the service.
    /// </para>
    /// </remarks>
    private System.Timers.Timer? _debounceTimer;

    /// <summary>
    /// Flag to prevent feedback loops when syncing from service events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, property setters skip calling <see cref="QueueServiceUpdate"/>,
    /// preventing an infinite loop: ViewModel → Service → Event → ViewModel → Service...
    /// </para>
    /// </remarks>
    private bool _isUpdatingFromService;

    /// <summary>
    /// Flag indicating whether the ViewModel has been disposed.
    /// </summary>
    private bool _isDisposed;

    #endregion

    #region Parameter Properties

    /// <summary>
    /// Gets or sets the temperature parameter controlling output randomness.
    /// </summary>
    /// <value>
    /// Value between 0.0 (deterministic) and 2.0 (maximum randomness).
    /// Default: 0.7 for balanced creativity.
    /// </value>
    /// <remarks>
    /// <para>
    /// Bound to a slider in the UI. When changed:
    /// </para>
    /// <list type="number">
    ///   <item><description><see cref="OnTemperatureChanged"/> fires</description></item>
    ///   <item><description>If not updating from service, <see cref="QueueServiceUpdate"/> resets timer</description></item>
    ///   <item><description><see cref="TemperatureDescription"/> is recomputed via PropertyChanged</description></item>
    /// </list>
    /// </remarks>
    [ObservableProperty]
    private float _temperature = ParameterConstants.Temperature.Default;

    /// <summary>
    /// Gets or sets the Top-P (nucleus sampling) parameter.
    /// </summary>
    /// <value>
    /// Value between 0.0 and 1.0. Default: 0.9.
    /// Higher values consider more tokens (more diverse); 1.0 disables filtering.
    /// </value>
    [ObservableProperty]
    private float _topP = ParameterConstants.TopP.Default;

    /// <summary>
    /// Gets or sets the Top-K parameter limiting token selection.
    /// </summary>
    /// <value>
    /// Value between 0 (disabled) and 100. Default: 40.
    /// Limits selection to the K most probable tokens.
    /// </value>
    [ObservableProperty]
    private int _topK = ParameterConstants.TopK.Default;

    /// <summary>
    /// Gets or sets the maximum tokens to generate in a response.
    /// </summary>
    /// <value>
    /// Value between 64 and 8192. Default: 2048.
    /// Limits response length; higher values use more memory.
    /// </value>
    [ObservableProperty]
    private int _maxTokens = ParameterConstants.MaxTokens.Default;

    /// <summary>
    /// Gets or sets the context window size in tokens.
    /// </summary>
    /// <value>
    /// Value between 512 and 32768. Default: 4096.
    /// Total tokens available for prompt + response.
    /// </value>
    [ObservableProperty]
    private uint _contextSize = ParameterConstants.ContextSize.Default;

    /// <summary>
    /// Gets or sets the repetition penalty parameter.
    /// </summary>
    /// <value>
    /// Value between 1.0 (disabled) and 2.0 (strong penalty). Default: 1.1.
    /// Discourages the model from repeating previous tokens.
    /// </value>
    [ObservableProperty]
    private float _repetitionPenalty = ParameterConstants.RepetitionPenalty.Default;

    /// <summary>
    /// Gets or sets the random seed for reproducible outputs.
    /// </summary>
    /// <value>
    /// -1 for random seed each generation; 0+ for reproducible output.
    /// Default: -1 (random).
    /// </value>
    [ObservableProperty]
    private int _seed = ParameterConstants.Seed.Default;

    #endregion

    #region State Properties

    /// <summary>
    /// Gets the collection of available presets for the dropdown.
    /// </summary>
    /// <value>
    /// An observable collection of <see cref="InferencePresetViewModel"/> instances.
    /// Populated by <see cref="LoadPresetsCommand"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Contains both built-in presets (Balanced, Creative, Precise, etc.)
    /// and user-created presets, sorted by category then name.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<InferencePresetViewModel> _presets = new();

    /// <summary>
    /// Gets or sets the currently selected preset in the dropdown.
    /// </summary>
    /// <value>
    /// The <see cref="InferencePresetViewModel"/> matching the active preset,
    /// or <c>null</c> if using custom (modified) settings.
    /// </value>
    /// <remarks>
    /// <para>
    /// When changed by user, <see cref="OnSelectedPresetChanged"/> triggers
    /// <see cref="ApplyPresetCommand"/> to apply the preset settings.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private InferencePresetViewModel? _selectedPreset;

    /// <summary>
    /// Gets or sets whether current settings differ from the active preset.
    /// </summary>
    /// <value>
    /// <c>true</c> if any parameter has been modified since a preset was loaded;
    /// <c>false</c> if settings match the preset or no preset is active.
    /// </value>
    /// <remarks>
    /// <para>
    /// Used to show an "unsaved changes" indicator in the UI, prompting
    /// users to save their customizations as a new preset.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Gets or sets whether the settings panel is expanded.
    /// </summary>
    /// <value>
    /// <c>true</c> if the panel is visible; <c>false</c> if collapsed.
    /// Default: true.
    /// </value>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// Gets or sets whether advanced parameters are shown.
    /// </summary>
    /// <value>
    /// <c>true</c> to show all parameters (including TopK, RepetitionPenalty, Seed);
    /// <c>false</c> to show only basic parameters (Temperature, TopP, MaxTokens).
    /// </value>
    /// <remarks>
    /// <para>
    /// Default is false to reduce cognitive load for casual users.
    /// Power users can toggle this to access all tuning options.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _showAdvanced;

    /// <summary>
    /// Gets or sets whether an async operation is in progress.
    /// </summary>
    /// <value>
    /// <c>true</c> while loading presets or saving; <c>false</c> otherwise.
    /// </value>
    /// <remarks>
    /// <para>
    /// Bound to loading spinners and used to disable controls during operations.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the name for a new preset being saved.
    /// </summary>
    /// <value>
    /// User-entered name for <see cref="SaveAsPresetCommand"/>.
    /// Must be unique among existing presets.
    /// </value>
    [ObservableProperty]
    private string _newPresetName = string.Empty;

    /// <summary>
    /// Gets or sets the description for a new preset being saved.
    /// </summary>
    /// <value>
    /// Optional descriptive text for the new preset.
    /// </value>
    [ObservableProperty]
    private string? _newPresetDescription;

    /// <summary>
    /// Gets or sets the category for a new preset being saved.
    /// </summary>
    /// <value>
    /// Optional category for grouping (e.g., "Code", "Creative").
    /// </value>
    [ObservableProperty]
    private string? _newPresetCategory;

    #endregion

    #region Computed Description Properties

    /// <summary>
    /// Gets a context-aware description of the current temperature setting.
    /// </summary>
    /// <value>
    /// Descriptive text based on temperature value:
    /// <list type="bullet">
    ///   <item><description>&lt; 0.3: "Very focused and deterministic"</description></item>
    ///   <item><description>0.3-0.6: "Consistent with slight variation"</description></item>
    ///   <item><description>0.6-0.8: "Balanced creativity and consistency"</description></item>
    ///   <item><description>0.8-1.0: "More creative and varied"</description></item>
    ///   <item><description>1.0-1.3: "Creative and experimental"</description></item>
    ///   <item><description>&gt; 1.3: "Highly random and experimental"</description></item>
    /// </list>
    /// </value>
    /// <remarks>
    /// <para>
    /// Recomputed when <see cref="Temperature"/> changes via
    /// <see cref="OnTemperatureChanged"/> raising PropertyChanged.
    /// </para>
    /// </remarks>
    public string TemperatureDescription => Temperature switch
    {
        < 0.3f => "Very focused and deterministic",
        < 0.6f => "Consistent with slight variation",
        < 0.8f => "Balanced creativity and consistency",
        < 1.0f => "More creative and varied",
        < 1.3f => "Creative and experimental",
        _ => "Highly random and experimental"
    };

    /// <summary>
    /// Gets a description of the current Top-P setting.
    /// </summary>
    /// <value>
    /// Format: "Considers top X% probability mass" where X is TopP * 100.
    /// </value>
    public string TopPDescription => $"Considers top {TopP * 100:F0}% probability mass";

    /// <summary>
    /// Gets a description of the current Top-K setting.
    /// </summary>
    /// <value>
    /// "Disabled" if TopK is 0; otherwise "Consider top X tokens".
    /// </value>
    public string TopKDescription => TopK == 0 ? "Disabled" : $"Consider top {TopK} tokens";

    /// <summary>
    /// Gets a description of the current Max Tokens setting.
    /// </summary>
    /// <value>
    /// Format: "~X words maximum" where X is approximately MaxTokens * 0.75.
    /// </value>
    /// <remarks>
    /// <para>
    /// Uses rough approximation of 0.75 words per token for English text.
    /// Actual word count varies by content and language.
    /// </para>
    /// </remarks>
    public string MaxTokensDescription => $"~{MaxTokens * 3 / 4} words maximum";

    /// <summary>
    /// Gets a description of the current Context Size setting.
    /// </summary>
    /// <value>
    /// Format: "~X words of history" where X is approximately ContextSize * 0.75.
    /// </value>
    public string ContextSizeDescription => $"~{ContextSize * 3 / 4} words of history";

    /// <summary>
    /// Gets a context-aware description of the repetition penalty.
    /// </summary>
    /// <value>
    /// <list type="bullet">
    ///   <item><description>1.0: "No repetition penalty"</description></item>
    ///   <item><description>1.0-1.1: "Light repetition penalty"</description></item>
    ///   <item><description>1.1-1.2: "Moderate repetition penalty"</description></item>
    ///   <item><description>&gt; 1.2: "Strong repetition penalty"</description></item>
    /// </list>
    /// </value>
    public string RepetitionPenaltyDescription => RepetitionPenalty switch
    {
        <= 1.0f => "No repetition penalty",
        < 1.1f => "Light repetition penalty",
        < 1.2f => "Moderate repetition penalty",
        _ => "Strong repetition penalty"
    };

    /// <summary>
    /// Gets a description of the current Seed setting.
    /// </summary>
    /// <value>
    /// "Random each generation" for -1; "Seed: X (reproducible)" for positive values.
    /// </value>
    public string SeedDescription => Seed == ParameterConstants.Seed.Random
        ? "Random each generation"
        : $"Seed: {Seed} (reproducible)";

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The inference settings service for parameter management.</param>
    /// <param name="dispatcher">The dispatcher for UI thread marshalling.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="settingsService"/> or <paramref name="dispatcher"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor:
    /// </para>
    /// <list type="number">
    ///   <item><description>Stores service and dispatcher references</description></item>
    ///   <item><description>Subscribes to service events (SettingsChanged, PresetChanged)</description></item>
    ///   <item><description>Initializes the debounce timer</description></item>
    ///   <item><description>Syncs initial state from the service</description></item>
    /// </list>
    /// <para>
    /// <b>Important:</b> Call <see cref="InitializeAsync"/> after construction to
    /// load presets and fully initialize the ViewModel.
    /// </para>
    /// </remarks>
    public InferenceSettingsViewModel(
        IInferenceSettingsService settingsService,
        IDispatcher dispatcher,
        ILogger<InferenceSettingsViewModel>? logger = null)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger;

        _logger?.LogDebug("[INIT] InferenceSettingsViewModel - Subscribing to service events");

        // Subscribe to service events for two-way sync
        _settingsService.SettingsChanged += OnSettingsChanged;
        _settingsService.PresetChanged += OnPresetChanged;

        // Initialize debounce timer
        _debounceTimer = new System.Timers.Timer(DebounceDelayMs)
        {
            AutoReset = false // One-shot timer, reset manually in QueueServiceUpdate
        };
        _debounceTimer.Elapsed += OnDebounceElapsed;

        // Sync initial state from service
        SyncFromService();

        _logger?.LogDebug("[INIT] InferenceSettingsViewModel - Initialization complete");
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Called when <see cref="Temperature"/> changes.
    /// </summary>
    /// <param name="value">The new temperature value.</param>
    /// <remarks>
    /// <para>
    /// Updates the computed description and queues a service update
    /// (unless updating from service to prevent feedback loop).
    /// </para>
    /// </remarks>
    partial void OnTemperatureChanged(float value)
    {
        if (_isUpdatingFromService)
        {
            _logger?.LogDebug("[SKIP] OnTemperatureChanged - Updating from service");
            return;
        }

        _logger?.LogDebug("[INFO] OnTemperatureChanged - Value: {Value}", value);
        OnPropertyChanged(nameof(TemperatureDescription));
        QueueServiceUpdate();
    }

    /// <summary>
    /// Called when <see cref="TopP"/> changes.
    /// </summary>
    partial void OnTopPChanged(float value)
    {
        if (_isUpdatingFromService)
        {
            _logger?.LogDebug("[SKIP] OnTopPChanged - Updating from service");
            return;
        }

        _logger?.LogDebug("[INFO] OnTopPChanged - Value: {Value}", value);
        OnPropertyChanged(nameof(TopPDescription));
        QueueServiceUpdate();
    }

    /// <summary>
    /// Called when <see cref="TopK"/> changes.
    /// </summary>
    partial void OnTopKChanged(int value)
    {
        if (_isUpdatingFromService)
        {
            _logger?.LogDebug("[SKIP] OnTopKChanged - Updating from service");
            return;
        }

        _logger?.LogDebug("[INFO] OnTopKChanged - Value: {Value}", value);
        OnPropertyChanged(nameof(TopKDescription));
        QueueServiceUpdate();
    }

    /// <summary>
    /// Called when <see cref="MaxTokens"/> changes.
    /// </summary>
    partial void OnMaxTokensChanged(int value)
    {
        if (_isUpdatingFromService)
        {
            _logger?.LogDebug("[SKIP] OnMaxTokensChanged - Updating from service");
            return;
        }

        _logger?.LogDebug("[INFO] OnMaxTokensChanged - Value: {Value}", value);
        OnPropertyChanged(nameof(MaxTokensDescription));
        QueueServiceUpdate();
    }

    /// <summary>
    /// Called when <see cref="ContextSize"/> changes.
    /// </summary>
    partial void OnContextSizeChanged(uint value)
    {
        if (_isUpdatingFromService)
        {
            _logger?.LogDebug("[SKIP] OnContextSizeChanged - Updating from service");
            return;
        }

        _logger?.LogDebug("[INFO] OnContextSizeChanged - Value: {Value}", value);
        OnPropertyChanged(nameof(ContextSizeDescription));
        QueueServiceUpdate();
    }

    /// <summary>
    /// Called when <see cref="RepetitionPenalty"/> changes.
    /// </summary>
    partial void OnRepetitionPenaltyChanged(float value)
    {
        if (_isUpdatingFromService)
        {
            _logger?.LogDebug("[SKIP] OnRepetitionPenaltyChanged - Updating from service");
            return;
        }

        _logger?.LogDebug("[INFO] OnRepetitionPenaltyChanged - Value: {Value}", value);
        OnPropertyChanged(nameof(RepetitionPenaltyDescription));
        QueueServiceUpdate();
    }

    /// <summary>
    /// Called when <see cref="Seed"/> changes.
    /// </summary>
    partial void OnSeedChanged(int value)
    {
        if (_isUpdatingFromService)
        {
            _logger?.LogDebug("[SKIP] OnSeedChanged - Updating from service");
            return;
        }

        _logger?.LogDebug("[INFO] OnSeedChanged - Value: {Value}", value);
        OnPropertyChanged(nameof(SeedDescription));
        QueueServiceUpdate();
    }

    /// <summary>
    /// Called when <see cref="SelectedPreset"/> changes.
    /// </summary>
    /// <param name="value">The newly selected preset, or null.</param>
    /// <remarks>
    /// <para>
    /// When the user selects a preset from the dropdown, this triggers
    /// <see cref="ApplyPresetCommand"/> to apply the preset settings.
    /// </para>
    /// </remarks>
    partial void OnSelectedPresetChanged(InferencePresetViewModel? value)
    {
        if (_isUpdatingFromService)
        {
            _logger?.LogDebug("[SKIP] OnSelectedPresetChanged - Updating from service");
            return;
        }

        if (value == null)
        {
            _logger?.LogDebug("[INFO] OnSelectedPresetChanged - Selection cleared");
            return;
        }

        _logger?.LogDebug("[INFO] OnSelectedPresetChanged - Selected: {Name} ({Id})", value.Name, value.Id);

        // Apply the selected preset
        ApplyPresetCommand.Execute(value);
    }

    #endregion

    #region Debounce Logic

    /// <summary>
    /// Queues a service update to be sent after the debounce delay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resets the debounce timer. Each subsequent call resets the timer again.
    /// Only when the timer elapses (no changes for 200ms) does the actual
    /// service update occur via <see cref="OnDebounceElapsed"/>.
    /// </para>
    /// </remarks>
    private void QueueServiceUpdate()
    {
        _logger?.LogDebug("[INFO] QueueServiceUpdate - Resetting debounce timer");

        // Stop and restart the timer to reset the delay
        _debounceTimer?.Stop();
        _debounceTimer?.Start();

        // Immediately mark as having unsaved changes
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Called when the debounce timer elapses, sending all parameter updates to the service.
    /// </summary>
    /// <param name="sender">The timer that elapsed.</param>
    /// <param name="e">The elapsed event arguments.</param>
    /// <remarks>
    /// <para>
    /// This fires on a timer thread, not the UI thread. Individual service Update*
    /// methods are synchronous and thread-safe. The service will fire events that
    /// are handled on the UI thread via the dispatcher.
    /// </para>
    /// </remarks>
    private void OnDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] OnDebounceElapsed - Sending batch update to service");

        try
        {
            // Update all parameters to service
            // Service will clamp values and fire events only if changed
            _settingsService.UpdateTemperature(Temperature);
            _settingsService.UpdateTopP(TopP);
            _settingsService.UpdateTopK(TopK);
            _settingsService.UpdateMaxTokens(MaxTokens);
            _settingsService.UpdateContextSize(ContextSize);
            _settingsService.UpdateRepetitionPenalty(RepetitionPenalty);
            _settingsService.UpdateSeed(Seed);

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] OnDebounceElapsed - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] OnDebounceElapsed - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);

            // Update error message on UI thread
            _dispatcher.InvokeAsync(() => SetError($"Failed to update settings: {ex.Message}"));
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the SettingsChanged event from the service.
    /// </summary>
    /// <param name="sender">The service that raised the event.</param>
    /// <param name="e">The event arguments containing new settings.</param>
    /// <remarks>
    /// <para>
    /// Syncs all parameter properties from the service. Uses the
    /// <c>_isUpdatingFromService</c> flag to prevent triggering
    /// service updates in response to this sync.
    /// </para>
    /// </remarks>
    private void OnSettingsChanged(object? sender, InferenceSettingsChangedEventArgs e)
    {
        _logger?.LogDebug(
            "[EVENT] OnSettingsChanged - Type: {Type}, Parameter: {Param}",
            e.ChangeType, e.ChangedParameter ?? "(all)");

        // Marshal to UI thread
        _dispatcher.InvokeAsync(() =>
        {
            SyncFromService();
        });
    }

    /// <summary>
    /// Handles the PresetChanged event from the service.
    /// </summary>
    /// <param name="sender">The service that raised the event.</param>
    /// <param name="e">The event arguments containing preset information.</param>
    /// <remarks>
    /// <para>
    /// Reloads the preset list and updates the selected preset to match
    /// the service's active preset.
    /// </para>
    /// </remarks>
    private void OnPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        _logger?.LogDebug(
            "[EVENT] OnPresetChanged - Type: {Type}, Preset: {Name}",
            e.ChangeType, e.NewPreset?.Name ?? "(null)");

        // Marshal to UI thread and reload presets
        _dispatcher.InvokeAsync(async () =>
        {
            await LoadPresetsAsync();
            UpdatePresetSelection();
        });
    }

    #endregion

    #region Sync Methods

    /// <summary>
    /// Synchronizes all properties from the service's current settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>ViewModel is constructed (initial state)</description></item>
    ///   <item><description>Service fires SettingsChanged event</description></item>
    /// </list>
    /// <para>
    /// Sets <c>_isUpdatingFromService = true</c> to prevent property setters
    /// from queuing service updates, which would create a feedback loop.
    /// </para>
    /// </remarks>
    private void SyncFromService()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] SyncFromService");

        _isUpdatingFromService = true;
        try
        {
            var settings = _settingsService.CurrentSettings;

            // Update all parameter properties
            Temperature = settings.Temperature;
            TopP = settings.TopP;
            TopK = settings.TopK;
            MaxTokens = settings.MaxTokens;
            ContextSize = settings.ContextSize;
            RepetitionPenalty = settings.RepetitionPenalty;
            Seed = settings.Seed;

            // Update state properties
            HasUnsavedChanges = _settingsService.HasUnsavedChanges;

            // Update computed descriptions
            OnPropertyChanged(nameof(TemperatureDescription));
            OnPropertyChanged(nameof(TopPDescription));
            OnPropertyChanged(nameof(TopKDescription));
            OnPropertyChanged(nameof(MaxTokensDescription));
            OnPropertyChanged(nameof(ContextSizeDescription));
            OnPropertyChanged(nameof(RepetitionPenaltyDescription));
            OnPropertyChanged(nameof(SeedDescription));

            // Update preset selection
            UpdatePresetSelection();

            stopwatch.Stop();
            _logger?.LogDebug(
                "[EXIT] SyncFromService - Temp={Temp}, TopP={TopP}, MaxTok={MaxTok}, Duration: {Ms}ms",
                settings.Temperature, settings.TopP, settings.MaxTokens, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _isUpdatingFromService = false;
        }
    }

    /// <summary>
    /// Updates the SelectedPreset to match the service's active preset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called after syncing from service or loading presets.
    /// Finds the preset in <see cref="Presets"/> matching the service's
    /// <see cref="IInferenceSettingsService.ActivePreset"/>.
    /// </para>
    /// </remarks>
    private void UpdatePresetSelection()
    {
        _logger?.LogDebug("[ENTER] UpdatePresetSelection");

        _isUpdatingFromService = true;
        try
        {
            var activePreset = _settingsService.ActivePreset;

            if (activePreset == null)
            {
                _logger?.LogDebug("[INFO] UpdatePresetSelection - No active preset");
                SelectedPreset = null;
                return;
            }

            // Find matching preset in collection
            var matchingPreset = Presets.FirstOrDefault(p => p.Id == activePreset.Id);

            if (matchingPreset != null)
            {
                // Update IsSelected flags
                foreach (var preset in Presets)
                {
                    preset.IsSelected = preset.Id == activePreset.Id;
                }

                SelectedPreset = matchingPreset;
                _logger?.LogDebug("[EXIT] UpdatePresetSelection - Selected: {Name}", matchingPreset.Name);
            }
            else
            {
                _logger?.LogDebug("[EXIT] UpdatePresetSelection - Active preset not found in collection");
                SelectedPreset = null;
            }
        }
        finally
        {
            _isUpdatingFromService = false;
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Initializes the ViewModel by loading presets and syncing state.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this after construction to fully initialize the ViewModel.
    /// This is async to allow preset loading from the database.
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

            await LoadPresetsAsync();
            UpdatePresetSelection();

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] InitializeAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] InitializeAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to initialize settings: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads all presets from the service and populates the <see cref="Presets"/> collection.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task LoadPresetsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] LoadPresetsAsync");

        try
        {
            IsLoading = true;
            ClearError();

            var presets = await _settingsService.GetPresetsAsync();

            Presets.Clear();
            foreach (var preset in presets)
            {
                Presets.Add(new InferencePresetViewModel(preset));
            }

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] LoadPresetsAsync - Count: {Count}, Duration: {Ms}ms",
                Presets.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] LoadPresetsAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to load presets: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Applies a preset, loading its settings into the current parameters.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task ApplyPresetAsync(InferencePresetViewModel? preset)
    {
        if (preset == null)
        {
            _logger?.LogDebug("[SKIP] ApplyPresetAsync - Preset is null");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] ApplyPresetAsync - Preset: {Name} ({Id})", preset.Name, preset.Id);

        try
        {
            IsLoading = true;
            ClearError();

            await _settingsService.ApplyPresetAsync(preset.Id);

            // Service will fire SettingsChanged, which triggers SyncFromService
            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] ApplyPresetAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] ApplyPresetAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to apply preset: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Saves current settings as a new preset.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// Uses <see cref="NewPresetName"/>, <see cref="NewPresetDescription"/>,
    /// and <see cref="NewPresetCategory"/> for the new preset properties.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task SaveAsPresetAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPresetName))
        {
            _logger?.LogDebug("[SKIP] SaveAsPresetAsync - Name is empty");
            SetError("Please enter a name for the new preset.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] SaveAsPresetAsync - Name: {Name}", NewPresetName);

        try
        {
            IsLoading = true;
            ClearError();

            var preset = await _settingsService.SaveAsPresetAsync(
                NewPresetName.Trim(),
                NewPresetDescription?.Trim(),
                NewPresetCategory?.Trim());

            _logger?.LogInformation("[INFO] SaveAsPresetAsync - Created preset: {Name} ({Id})",
                preset.Name, preset.Id);

            // Clear input fields
            NewPresetName = string.Empty;
            NewPresetDescription = null;
            NewPresetCategory = null;

            // Reload presets to include the new one
            await LoadPresetsAsync();

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] SaveAsPresetAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            stopwatch.Stop();
            _logger?.LogWarning("[EXIT] SaveAsPresetAsync - Name conflict: {Message}", ex.Message);
            SetError("A preset with this name already exists. Please choose a different name.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] SaveAsPresetAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to save preset: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Updates the currently selected preset with current settings.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// Only available for user-created (non-built-in) presets.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task UpdateCurrentPresetAsync()
    {
        if (SelectedPreset == null)
        {
            _logger?.LogDebug("[SKIP] UpdateCurrentPresetAsync - No preset selected");
            SetError("No preset is currently selected.");
            return;
        }

        if (SelectedPreset.IsBuiltIn)
        {
            _logger?.LogDebug("[SKIP] UpdateCurrentPresetAsync - Cannot update built-in preset");
            SetError("Built-in presets cannot be modified. Save as a new preset instead.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] UpdateCurrentPresetAsync - Preset: {Name}", SelectedPreset.Name);

        try
        {
            IsLoading = true;
            ClearError();

            await _settingsService.UpdatePresetAsync(SelectedPreset.Id);

            HasUnsavedChanges = false;

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] UpdateCurrentPresetAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] UpdateCurrentPresetAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to update preset: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Deletes a custom preset.
    /// </summary>
    /// <param name="preset">The preset to delete.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// Only available for user-created (non-built-in) presets.
    /// If the deleted preset was selected, selection is cleared.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task DeletePresetAsync(InferencePresetViewModel? preset)
    {
        if (preset == null)
        {
            _logger?.LogDebug("[SKIP] DeletePresetAsync - Preset is null");
            return;
        }

        if (preset.IsBuiltIn)
        {
            _logger?.LogDebug("[SKIP] DeletePresetAsync - Cannot delete built-in preset: {Name}", preset.Name);
            SetError("Built-in presets cannot be deleted.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] DeletePresetAsync - Preset: {Name} ({Id})", preset.Name, preset.Id);

        try
        {
            IsLoading = true;
            ClearError();

            await _settingsService.DeletePresetAsync(preset.Id);

            _logger?.LogInformation("[INFO] DeletePresetAsync - Deleted preset: {Name}", preset.Name);

            // Reload presets to reflect deletion
            await LoadPresetsAsync();

            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] DeletePresetAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] DeletePresetAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to delete preset: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Resets all settings to the default preset.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger?.LogDebug("[ENTER] ResetToDefaultsAsync");

        try
        {
            IsLoading = true;
            ClearError();

            await _settingsService.ResetToDefaultsAsync();

            // Service will fire SettingsChanged, which triggers SyncFromService
            stopwatch.Stop();
            _logger?.LogDebug("[EXIT] ResetToDefaultsAsync - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[EXIT] ResetToDefaultsAsync - Error: {Message}, Duration: {Ms}ms",
                ex.Message, stopwatch.ElapsedMilliseconds);
            SetError($"Failed to reset to defaults: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles the visibility of advanced parameters.
    /// </summary>
    [RelayCommand]
    private void ToggleAdvanced()
    {
        ShowAdvanced = !ShowAdvanced;
        _logger?.LogDebug("[INFO] ToggleAdvanced - ShowAdvanced: {Show}", ShowAdvanced);
    }

    /// <summary>
    /// Toggles the expanded/collapsed state of the settings panel.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        _logger?.LogDebug("[INFO] ToggleExpanded - IsExpanded: {Expanded}", IsExpanded);
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

        _logger?.LogDebug("[DISPOSE] InferenceSettingsViewModel - Unsubscribing from events");

        // Unsubscribe from service events
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _settingsService.PresetChanged -= OnPresetChanged;

        // Dispose debounce timer
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = null;

        _isDisposed = true;

        _logger?.LogDebug("[DISPOSE] InferenceSettingsViewModel - Disposal complete");
    }

    #endregion
}
