namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for workspace settings panel.
/// Manages all configurable options for File Explorer, Editor, and Context Attachment.
/// </summary>
/// <remarks>
/// <para>
/// Supports validation, change tracking, and persistence to settings file.
/// </para>
/// <para>Added in v0.3.5a.</para>
/// </remarks>
public partial class WorkspaceSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<WorkspaceSettingsViewModel> _logger;
    private readonly AppSettings _originalSettings;
    private bool _hasChanges;

    #region File Explorer Settings

    /// <summary>
    /// Whether to restore the last workspace on startup.
    /// </summary>
    [ObservableProperty]
    private bool _restoreLastWorkspace = true;

    /// <summary>
    /// Whether to show hidden files and folders.
    /// </summary>
    [ObservableProperty]
    private bool _showHiddenFiles;

    /// <summary>
    /// Whether to respect .gitignore patterns.
    /// </summary>
    [ObservableProperty]
    private bool _useGitIgnore = true;

    /// <summary>
    /// Maximum number of recent workspaces to remember.
    /// </summary>
    [ObservableProperty]
    private int _maxRecentWorkspaces = 10;

    /// <summary>
    /// Custom ignore patterns (one per line).
    /// </summary>
    [ObservableProperty]
    private string _customIgnorePatterns = string.Empty;

    /// <summary>
    /// Whether to auto-refresh on external file changes.
    /// </summary>
    [ObservableProperty]
    private bool _autoRefreshOnExternalChanges = true;

    #endregion

    #region Editor Settings

    /// <summary>
    /// Editor font family.
    /// </summary>
    [ObservableProperty]
    private string _editorFontFamily = "Cascadia Code, Consolas, monospace";

    /// <summary>
    /// Editor font size.
    /// </summary>
    [ObservableProperty]
    private int _editorFontSize = 14;

    /// <summary>
    /// Tab size in spaces.
    /// </summary>
    [ObservableProperty]
    private int _tabSize = 4;

    /// <summary>
    /// Whether to convert tabs to spaces.
    /// </summary>
    [ObservableProperty]
    private bool _convertTabsToSpaces = true;

    /// <summary>
    /// Whether to show line numbers.
    /// </summary>
    [ObservableProperty]
    private bool _showLineNumbers = true;

    /// <summary>
    /// Whether to highlight the current line.
    /// </summary>
    [ObservableProperty]
    private bool _highlightCurrentLine = true;

    /// <summary>
    /// Whether to enable word wrap.
    /// </summary>
    [ObservableProperty]
    private bool _wordWrap;

    /// <summary>
    /// Column ruler position (0 = disabled).
    /// </summary>
    [ObservableProperty]
    private int _rulerColumn;

    /// <summary>
    /// Editor theme (for syntax highlighting).
    /// </summary>
    [ObservableProperty]
    private string _editorTheme = "DarkPlus";

    #endregion

    #region Context Attachment Settings

    /// <summary>
    /// Maximum files that can be attached at once.
    /// </summary>
    [ObservableProperty]
    private int _maxFilesAttached = 10;

    /// <summary>
    /// Maximum tokens per individual file.
    /// </summary>
    [ObservableProperty]
    private int _maxTokensPerFile = 4000;

    /// <summary>
    /// Maximum total tokens across all attached files.
    /// </summary>
    [ObservableProperty]
    private int _maxTotalContextTokens = 8000;

    /// <summary>
    /// Maximum file size in KB.
    /// </summary>
    [ObservableProperty]
    private int _maxFileSizeKb = 500;

    /// <summary>
    /// Whether to show token count in context bar.
    /// </summary>
    [ObservableProperty]
    private bool _showTokenCount = true;

    /// <summary>
    /// Whether to warn when approaching token limit.
    /// </summary>
    [ObservableProperty]
    private bool _warnOnTokenLimit = true;

    #endregion

    #region Selection Options

    /// <summary>
    /// Available font families for selection.
    /// </summary>
    public ObservableCollection<string> AvailableFonts { get; } = new()
    {
        "Cascadia Code",
        "Cascadia Mono",
        "Consolas",
        "Fira Code",
        "JetBrains Mono",
        "Source Code Pro",
        "Monaco",
        "Menlo",
        "Ubuntu Mono",
        "monospace"
    };

    /// <summary>
    /// Available editor themes.
    /// </summary>
    public ObservableCollection<string> AvailableThemes { get; } = new()
    {
        "DarkPlus",
        "LightPlus",
        "Monokai",
        "SolarizedDark",
        "SolarizedLight",
        "HighContrastDark"
    };

    /// <summary>
    /// Predefined tab size options.
    /// </summary>
    public int[] TabSizeOptions { get; } = [2, 4, 8];

    /// <summary>
    /// Predefined font size options.
    /// </summary>
    public int[] FontSizeOptions { get; } = [10, 11, 12, 13, 14, 15, 16, 18, 20, 24];

    /// <summary>
    /// Predefined ruler column options.
    /// </summary>
    public int[] RulerColumnOptions { get; } = [0, 80, 100, 120];

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether there are unsaved changes.
    /// </summary>
    public bool HasChanges
    {
        get => _hasChanges;
        private set => SetProperty(ref _hasChanges, value);
    }

    /// <summary>
    /// Validation errors for display.
    /// </summary>
    [ObservableProperty]
    private string? _validationError;

    /// <summary>
    /// Whether save is possible (has changes and no errors).
    /// </summary>
    public bool CanSave => HasChanges && string.IsNullOrEmpty(ValidationError);

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="WorkspaceSettingsViewModel"/>.
    /// </summary>
    /// <param name="settingsService">The settings service for loading and saving.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public WorkspaceSettingsViewModel(
        ISettingsService settingsService,
        ILogger<WorkspaceSettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        _originalSettings = settingsService.CurrentSettings;

        _logger.LogDebug("[INIT] WorkspaceSettingsViewModel created");
        LoadFromSettings(_originalSettings);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads values from AppSettings.
    /// </summary>
    private void LoadFromSettings(AppSettings settings)
    {
        _logger.LogDebug("[ENTER] LoadFromSettings");

        // File Explorer
        RestoreLastWorkspace = settings.RestoreLastWorkspace;
        ShowHiddenFiles = settings.ShowHiddenFiles;
        UseGitIgnore = settings.UseGitIgnore;
        MaxRecentWorkspaces = settings.MaxRecentWorkspaces;
        CustomIgnorePatterns = string.Join("\n", settings.CustomIgnorePatterns);
        AutoRefreshOnExternalChanges = settings.AutoRefreshOnExternalChanges;

        // Editor
        EditorFontFamily = settings.EditorFontFamily;
        EditorFontSize = settings.EditorFontSize;
        TabSize = settings.TabSize;
        ConvertTabsToSpaces = settings.ConvertTabsToSpaces;
        ShowLineNumbers = settings.ShowLineNumbers;
        HighlightCurrentLine = settings.HighlightCurrentLine;
        WordWrap = settings.WordWrap;
        RulerColumn = settings.RulerColumn;
        EditorTheme = settings.EditorTheme;

        // Context
        MaxFilesAttached = settings.ContextLimits.MaxFilesAttached;
        MaxTokensPerFile = settings.ContextLimits.MaxTokensPerFile;
        MaxTotalContextTokens = settings.ContextLimits.MaxTotalContextTokens;
        MaxFileSizeKb = settings.ContextLimits.MaxFileSizeBytes / 1024;
        ShowTokenCount = settings.ShowTokenCount;
        WarnOnTokenLimit = settings.WarnOnTokenLimit;

        HasChanges = false;
        ValidationError = null;

        _logger.LogDebug("[EXIT] LoadFromSettings");
    }

    /// <summary>
    /// Creates AppSettings from current values.
    /// </summary>
    private AppSettings ToSettings()
    {
        var updatedSettings = new AppSettings
        {
            // Preserve existing non-workspace settings
            LastModelPath = _originalSettings.LastModelPath,
            DefaultContextSize = _originalSettings.DefaultContextSize,
            DefaultGpuLayers = _originalSettings.DefaultGpuLayers,
            DefaultBatchSize = _originalSettings.DefaultBatchSize,
            Temperature = _originalSettings.Temperature,
            TopP = _originalSettings.TopP,
            MaxTokens = _originalSettings.MaxTokens,
            ActivePresetId = _originalSettings.ActivePresetId,
            CurrentSystemPromptId = _originalSettings.CurrentSystemPromptId,
            Theme = _originalSettings.Theme,
            SidebarWidth = _originalSettings.SidebarWidth,
            WindowWidth = _originalSettings.WindowWidth,
            WindowHeight = _originalSettings.WindowHeight,
            WindowX = _originalSettings.WindowX,
            WindowY = _originalSettings.WindowY,

            // File Explorer settings (updated)
            RestoreLastWorkspace = RestoreLastWorkspace,
            ShowHiddenFiles = ShowHiddenFiles,
            UseGitIgnore = UseGitIgnore,
            MaxRecentWorkspaces = MaxRecentWorkspaces,
            CustomIgnorePatterns = CustomIgnorePatterns
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList(),
            AutoRefreshOnExternalChanges = AutoRefreshOnExternalChanges,

            // Editor settings (updated)
            EditorFontFamily = EditorFontFamily,
            EditorFontSize = EditorFontSize,
            TabSize = TabSize,
            ConvertTabsToSpaces = ConvertTabsToSpaces,
            ShowLineNumbers = ShowLineNumbers,
            HighlightCurrentLine = HighlightCurrentLine,
            WordWrap = WordWrap,
            RulerColumn = RulerColumn,
            EditorTheme = EditorTheme,

            // Context settings (updated)
            ContextLimits = new ContextLimitsConfig
            {
                MaxFilesAttached = MaxFilesAttached,
                MaxTokensPerFile = MaxTokensPerFile,
                MaxTotalContextTokens = MaxTotalContextTokens,
                MaxFileSizeBytes = MaxFileSizeKb * 1024
            },
            ShowTokenCount = ShowTokenCount,
            WarnOnTokenLimit = WarnOnTokenLimit
        };

        return updatedSettings;
    }

    /// <summary>
    /// Validates current settings.
    /// </summary>
    /// <returns>True if validation passes.</returns>
    private bool Validate()
    {
        ValidationError = null;

        // Cross-field validation
        if (MaxTokensPerFile > MaxTotalContextTokens)
        {
            ValidationError = "Max tokens per file cannot exceed max total tokens.";
            return false;
        }

        // Range validation
        if (EditorFontSize < 8 || EditorFontSize > 72)
        {
            ValidationError = "Font size must be between 8 and 72.";
            return false;
        }

        if (MaxRecentWorkspaces < 1 || MaxRecentWorkspaces > 50)
        {
            ValidationError = "Recent workspaces must be between 1 and 50.";
            return false;
        }

        if (TabSize < 1 || TabSize > 16)
        {
            ValidationError = "Tab size must be between 1 and 16.";
            return false;
        }

        if (MaxFileSizeKb < 1 || MaxFileSizeKb > 10000)
        {
            ValidationError = "Max file size must be between 1 and 10000 KB.";
            return false;
        }

        _logger.LogDebug("[VALIDATE] All validations passed");
        return true;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Saves settings and raises SaveCompleted event.
    /// </summary>
    [RelayCommand]
    public async Task SaveAsync()
    {
        _logger.LogDebug("[ENTER] SaveAsync");

        if (!Validate())
        {
            _logger.LogWarning("[SAVE] Validation failed: {Error}", ValidationError);
            return;
        }

        var settings = ToSettings();
        await _settingsService.SaveSettingsAsync(settings);
        HasChanges = false;

        _logger.LogInformation("[SAVE] Settings saved successfully");
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resets all settings to default values.
    /// </summary>
    [RelayCommand]
    public void ResetToDefaults()
    {
        _logger.LogDebug("[ENTER] ResetToDefaults");

        LoadFromSettings(new AppSettings());
        HasChanges = true;

        _logger.LogInformation("[RESET] Settings reset to defaults");
    }

    /// <summary>
    /// Cancels changes and reverts to original settings.
    /// </summary>
    [RelayCommand]
    public void Cancel()
    {
        _logger.LogDebug("[ENTER] Cancel");

        LoadFromSettings(_originalSettings);
        CancelRequested?.Invoke(this, EventArgs.Empty);

        _logger.LogDebug("[CANCEL] Changes cancelled, reverted to original");
    }

    #endregion

    #region Property Changed Tracking

    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Track changes on any property except meta properties
        if (e.PropertyName != nameof(HasChanges) &&
            e.PropertyName != nameof(ValidationError) &&
            e.PropertyName != nameof(CanSave))
        {
            HasChanges = true;
            OnPropertyChanged(nameof(CanSave));
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when save is complete.
    /// </summary>
    public event EventHandler? SaveCompleted;

    /// <summary>
    /// Event raised when cancel is requested.
    /// </summary>
    public event EventHandler? CancelRequested;

    #endregion
}
