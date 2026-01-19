// ============================================================================
// File: TerminalSettingsViewModel.cs
// Path: src/AIntern.Desktop/ViewModels/TerminalSettingsViewModel.cs
// Description: ViewModel for the terminal settings panel.
// Created: 2026-01-19
// AI Intern v0.5.5f - Terminal Settings Panel
// ============================================================================

namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalSettingsViewModel (v0.5.5f)                                          │
// │ ViewModel for the terminal settings panel with live preview support.        │
// │                                                                              │
// │ Features:                                                                    │
// │   - Observable properties for all terminal settings                         │
// │   - Live preview via PreviewSettingsChanged event                           │
// │   - HasUnsavedChanges tracking with warning banner                         │
// │   - Shell profile management (Add, Edit, Delete)                           │
// │   - Settings validation with error display                                  │
// │   - Save, Reset, Discard commands                                          │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for the terminal settings panel.
/// </summary>
/// <remarks>
/// <para>
/// Provides two-way binding for all terminal settings with live preview support.
/// Changes trigger the <see cref="PreviewSettingsChanged"/> event for immediate
/// visual feedback in the terminal.
/// </para>
/// <para>Added in v0.5.5f.</para>
/// </remarks>
public partial class TerminalSettingsViewModel : ViewModelBase
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Dependencies
    // ═══════════════════════════════════════════════════════════════════════════

    private readonly ILogger<TerminalSettingsViewModel> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IFontService _fontService;
    private readonly IShellDetectionService _shellDetectionService;
    private readonly IShellProfileService _profileService;

    /// <summary>Original settings for comparison and discard.</summary>
    private TerminalSettings _originalSettings;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties - Collections
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Available monospace fonts on the system.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _availableFonts = new();

    /// <summary>Available terminal color themes.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _availableThemes = new();

    /// <summary>Shell profiles (detected + custom).</summary>
    [ObservableProperty]
    private ObservableCollection<ShellProfile> _shellProfiles = new();

    /// <summary>Currently selected shell profile.</summary>
    [ObservableProperty]
    private ShellProfile? _selectedProfile;

    /// <summary>Default shell profile for new terminals.</summary>
    [ObservableProperty]
    private ShellProfile? _defaultProfile;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties - Appearance
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Selected font family.</summary>
    [ObservableProperty]
    private string _selectedFontFamily = string.Empty;

    /// <summary>Font size in points (8-32).</summary>
    [ObservableProperty]
    private double _selectedFontSize = 14;

    /// <summary>Line height multiplier (1.0-2.0).</summary>
    [ObservableProperty]
    private double _lineHeight = 1.2;

    /// <summary>Selected color theme name.</summary>
    [ObservableProperty]
    private string _selectedThemeName = "Dark";

    /// <summary>Cursor display style.</summary>
    [ObservableProperty]
    private TerminalCursorStyle _selectedCursorStyle = TerminalCursorStyle.Block;

    /// <summary>Whether cursor blinks.</summary>
    [ObservableProperty]
    private bool _cursorBlink = true;

    /// <summary>Cursor blink rate in milliseconds.</summary>
    [ObservableProperty]
    private int _cursorBlinkRate = 530;

    /// <summary>Whether bold text uses bright colors.</summary>
    [ObservableProperty]
    private bool _boldIsBright = true;

    /// <summary>Whether font ligatures are enabled.</summary>
    [ObservableProperty]
    private bool _enableLigatures = true;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties - Behavior
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Scrollback buffer size (1000-100000).</summary>
    [ObservableProperty]
    private int _scrollbackLines = 10000;

    /// <summary>Whether terminal bell is enabled.</summary>
    [ObservableProperty]
    private bool _bellEnabled;

    /// <summary>Bell notification style.</summary>
    [ObservableProperty]
    private TerminalBellStyle _bellStyle = TerminalBellStyle.Visual;

    /// <summary>Copy text automatically on selection.</summary>
    [ObservableProperty]
    private bool _copyOnSelect;

    /// <summary>Scroll to bottom on keyboard input.</summary>
    [ObservableProperty]
    private bool _scrollOnInput = true;

    /// <summary>Scroll to bottom on new output.</summary>
    [ObservableProperty]
    private bool _scrollOnOutput;

    /// <summary>Sync terminal directory with workspace.</summary>
    [ObservableProperty]
    private bool _syncWithWorkspace = true;

    /// <summary>Confirm before closing terminal with running process.</summary>
    [ObservableProperty]
    private bool _confirmOnClose = true;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties - State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Whether there are unsaved changes.</summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>Current validation error message, if any.</summary>
    [ObservableProperty]
    private string? _validationError;

    /// <summary>Whether settings are currently loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    // ═══════════════════════════════════════════════════════════════════════════
    // Static Lists
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Available cursor styles for dropdown.</summary>
    public IReadOnlyList<TerminalCursorStyle> CursorStyles { get; } =
        Enum.GetValues<TerminalCursorStyle>();

    /// <summary>Available bell styles for dropdown.</summary>
    public IReadOnlyList<TerminalBellStyle> BellStyles { get; } =
        Enum.GetValues<TerminalBellStyle>();

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new <see cref="TerminalSettingsViewModel"/>.
    /// </summary>
    public TerminalSettingsViewModel(
        ILogger<TerminalSettingsViewModel> logger,
        ISettingsService settingsService,
        IFontService fontService,
        IShellDetectionService shellDetectionService,
        IShellProfileService profileService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _fontService = fontService ?? throw new ArgumentNullException(nameof(fontService));
        _shellDetectionService = shellDetectionService ?? throw new ArgumentNullException(nameof(shellDetectionService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

        // ─────────────────────────────────────────────────────────────────────
        // Initialize from current settings
        // ─────────────────────────────────────────────────────────────────────
        _originalSettings = CreateSettingsFromAppSettings();

        _logger.LogDebug("TerminalSettingsViewModel created");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Initialization
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes the ViewModel by loading settings and available options.
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogDebug("Initializing terminal settings");
        IsLoading = true;

        try
        {
            // ─────────────────────────────────────────────────────────────────
            // Load available fonts
            // ─────────────────────────────────────────────────────────────────
            var fonts = _fontService.GetMonospaceFonts();
            AvailableFonts = new ObservableCollection<string>(fonts);
            _logger.LogDebug("Loaded {Count} available fonts", fonts.Count);

            // ─────────────────────────────────────────────────────────────────
            // Load available themes
            // ─────────────────────────────────────────────────────────────────
            AvailableThemes = new ObservableCollection<string>(new[]
            {
                "Dark", "Light", "Solarized Dark"
            });

            // ─────────────────────────────────────────────────────────────────
            // Load shell profiles
            // ─────────────────────────────────────────────────────────────────
            await LoadShellProfilesAsync();

            // ─────────────────────────────────────────────────────────────────
            // Apply current values from app settings
            // ─────────────────────────────────────────────────────────────────
            LoadCurrentSettings();

            HasUnsavedChanges = false;
            ValidationError = null;

            _logger.LogInformation("Terminal settings initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize terminal settings");
            ValidationError = "Failed to load settings";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a TerminalSettings object from current AppSettings values.
    /// </summary>
    private TerminalSettings CreateSettingsFromAppSettings()
    {
        var appSettings = _settingsService.CurrentSettings;
        return new TerminalSettings
        {
            FontFamily = appSettings.TerminalFontFamily,
            FontSize = appSettings.TerminalFontSize,
            ThemeName = appSettings.TerminalTheme,
            CursorStyle = appSettings.TerminalCursorStyle,
            CursorBlink = appSettings.TerminalCursorBlink,
            ScrollbackLines = appSettings.TerminalScrollbackLines,
            BellStyle = appSettings.TerminalBellStyle,
            CopyOnSelect = appSettings.TerminalCopyOnSelect,
            SyncWithWorkspace = appSettings.SyncTerminalWithWorkspace,
            WordSeparators = appSettings.TerminalWordSeparators
        };
    }

    /// <summary>
    /// Loads current settings values into observable properties.
    /// </summary>
    private void LoadCurrentSettings()
    {
        var appSettings = _settingsService.CurrentSettings;

        // ─────────────────────────────────────────────────────────────────────
        // Appearance
        // ─────────────────────────────────────────────────────────────────────
        SelectedFontFamily = _fontService.GetBestAvailableFont(appSettings.TerminalFontFamily);
        SelectedFontSize = appSettings.TerminalFontSize;
        SelectedThemeName = appSettings.TerminalTheme;
        SelectedCursorStyle = appSettings.TerminalCursorStyle;
        CursorBlink = appSettings.TerminalCursorBlink;

        // ─────────────────────────────────────────────────────────────────────
        // Behavior
        // ─────────────────────────────────────────────────────────────────────
        ScrollbackLines = appSettings.TerminalScrollbackLines;
        BellEnabled = appSettings.TerminalBellStyle != TerminalBellStyle.None;
        BellStyle = appSettings.TerminalBellStyle;
        CopyOnSelect = appSettings.TerminalCopyOnSelect;
        SyncWithWorkspace = appSettings.SyncTerminalWithWorkspace;

        // ─────────────────────────────────────────────────────────────────────
        // Shell - set default profile
        // ─────────────────────────────────────────────────────────────────────
        DefaultProfile = ShellProfiles.FirstOrDefault();

        _logger.LogDebug("Loaded current settings: Font={Font}, Theme={Theme}",
            SelectedFontFamily, SelectedThemeName);
    }

    /// <summary>
    /// Loads shell profiles from detection service.
    /// </summary>
    private async Task LoadShellProfilesAsync()
    {
        try
        {
            _logger.LogDebug("Loading shell profiles");

            var detectedShells = await _shellDetectionService.GetAvailableShellsAsync();
            var profiles = detectedShells
                .Select(s => new ShellProfile
                {
                    Id = Guid.NewGuid(),
                    Name = s.Name,
                    ShellPath = s.Path,
                    ShellType = s.ShellType,
                    IsBuiltIn = true
                })
                .ToList();

            ShellProfiles = new ObservableCollection<ShellProfile>(profiles);
            DefaultProfile = ShellProfiles.FirstOrDefault();

            _logger.LogDebug("Loaded {Count} shell profiles", profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load shell profiles");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Property Change Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    partial void OnSelectedFontFamilyChanged(string value) => MarkDirtyAndPreview();
    partial void OnSelectedFontSizeChanged(double value) => MarkDirtyAndPreview();
    partial void OnLineHeightChanged(double value) => MarkDirtyAndPreview();
    partial void OnSelectedThemeNameChanged(string value) => MarkDirtyAndPreview();
    partial void OnSelectedCursorStyleChanged(TerminalCursorStyle value) => MarkDirtyAndPreview();
    partial void OnCursorBlinkChanged(bool value) => MarkDirtyAndPreview();
    partial void OnCursorBlinkRateChanged(int value) => MarkDirtyAndPreview();
    partial void OnBoldIsBrightChanged(bool value) => MarkDirtyAndPreview();
    partial void OnEnableLigaturesChanged(bool value) => MarkDirtyAndPreview();
    partial void OnScrollbackLinesChanged(int value) => MarkDirty();
    partial void OnBellEnabledChanged(bool value) => MarkDirty();
    partial void OnBellStyleChanged(TerminalBellStyle value) => MarkDirty();
    partial void OnCopyOnSelectChanged(bool value) => MarkDirty();
    partial void OnScrollOnInputChanged(bool value) => MarkDirty();
    partial void OnScrollOnOutputChanged(bool value) => MarkDirty();
    partial void OnSyncWithWorkspaceChanged(bool value) => MarkDirty();
    partial void OnConfirmOnCloseChanged(bool value) => MarkDirty();
    partial void OnDefaultProfileChanged(ShellProfile? value) => MarkDirty();

    /// <summary>
    /// Marks settings as dirty (unsaved changes).
    /// </summary>
    private void MarkDirty()
    {
        HasUnsavedChanges = true;
        Validate();
    }

    /// <summary>
    /// Marks settings as dirty and raises preview event.
    /// </summary>
    private void MarkDirtyAndPreview()
    {
        MarkDirty();
        PreviewSettingsChanged?.Invoke(this, BuildPreviewSettings());
    }

    /// <summary>
    /// Builds a TerminalSettings object from current UI values.
    /// </summary>
    private TerminalSettings BuildPreviewSettings()
    {
        return new TerminalSettings
        {
            FontFamily = SelectedFontFamily,
            FontSize = SelectedFontSize,
            LineHeight = LineHeight,
            ThemeName = SelectedThemeName,
            CursorStyle = SelectedCursorStyle,
            CursorBlink = CursorBlink,
            CursorBlinkRate = CursorBlinkRate,
            BoldIsBright = BoldIsBright,
            EnableLigatures = EnableLigatures,
            ScrollbackLines = ScrollbackLines,
            BellEnabled = BellEnabled,
            BellStyle = BellStyle,
            CopyOnSelect = CopyOnSelect,
            ScrollOnInput = ScrollOnInput,
            ScrollOnOutput = ScrollOnOutput,
            SyncWithWorkspace = SyncWithWorkspace,
            ConfirmOnClose = ConfirmOnClose,
            DefaultProfileId = DefaultProfile?.Id.ToString()
        };
    }

    /// <summary>
    /// Validates current settings.
    /// </summary>
    private void Validate()
    {
        var preview = BuildPreviewSettings();
        var errors = preview.Validate();
        ValidationError = errors.Count > 0 ? string.Join("; ", errors) : null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saves current settings to persistence.
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (ValidationError != null)
        {
            _logger.LogWarning("Cannot save invalid settings: {Error}", ValidationError);
            return;
        }

        _logger.LogInformation("Saving terminal settings");

        try
        {
            // ─────────────────────────────────────────────────────────────────
            // Apply to AppSettings
            // ─────────────────────────────────────────────────────────────────
            var appSettings = _settingsService.CurrentSettings;
            appSettings.TerminalFontFamily = SelectedFontFamily;
            appSettings.TerminalFontSize = SelectedFontSize;
            appSettings.TerminalTheme = SelectedThemeName;
            appSettings.TerminalCursorStyle = SelectedCursorStyle;
            appSettings.TerminalCursorBlink = CursorBlink;
            appSettings.TerminalScrollbackLines = ScrollbackLines;
            appSettings.TerminalBellStyle = BellEnabled ? BellStyle : TerminalBellStyle.None;
            appSettings.TerminalCopyOnSelect = CopyOnSelect;
            appSettings.SyncTerminalWithWorkspace = SyncWithWorkspace;

            // ─────────────────────────────────────────────────────────────────
            // Persist
            // ─────────────────────────────────────────────────────────────────
            await _settingsService.SaveSettingsAsync(appSettings);

            _originalSettings = BuildPreviewSettings();
            HasUnsavedChanges = false;

            SettingsSaved?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("Terminal settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save terminal settings");
            ValidationError = "Failed to save settings";
        }
    }

    /// <summary>
    /// Resets all settings to defaults.
    /// </summary>
    [RelayCommand]
    private void ResetToDefaults()
    {
        _logger.LogInformation("Resetting terminal settings to defaults");

        // ─────────────────────────────────────────────────────────────────────
        // Apply default values
        // ─────────────────────────────────────────────────────────────────────
        SelectedFontFamily = _fontService.GetBestAvailableFont("Cascadia Mono, Consolas, monospace");
        SelectedFontSize = 14;
        LineHeight = 1.2;
        SelectedThemeName = "Dark";
        SelectedCursorStyle = TerminalCursorStyle.Block;
        CursorBlink = true;
        CursorBlinkRate = 530;
        BoldIsBright = true;
        EnableLigatures = true;
        ScrollbackLines = 10000;
        BellEnabled = false;
        BellStyle = TerminalBellStyle.Visual;
        CopyOnSelect = false;
        ScrollOnInput = true;
        ScrollOnOutput = false;
        SyncWithWorkspace = true;
        ConfirmOnClose = true;

        MarkDirtyAndPreview();
    }

    /// <summary>
    /// Discards unsaved changes and reloads from saved settings.
    /// </summary>
    [RelayCommand]
    private void DiscardChanges()
    {
        _logger.LogDebug("Discarding terminal settings changes");

        LoadCurrentSettings();
        HasUnsavedChanges = false;
        ValidationError = null;

        PreviewSettingsChanged?.Invoke(this, _originalSettings);
    }

    /// <summary>
    /// Adds a new custom shell profile.
    /// </summary>
    [RelayCommand]
    private async Task AddProfileAsync()
    {
        _logger.LogDebug("Adding new shell profile");

        var defaultShellPath = await _shellDetectionService.GetDefaultShellAsync();
        var shellType = _shellDetectionService.DetectShellType(defaultShellPath);

        var profile = new ShellProfile
        {
            Id = Guid.NewGuid(),
            Name = "New Profile",
            ShellPath = defaultShellPath ?? "/bin/bash",
            ShellType = shellType,
            IsBuiltIn = false
        };

        ShellProfiles.Add(profile);
        SelectedProfile = profile;
        MarkDirty();

        ProfileEditRequested?.Invoke(this, profile);
    }

    /// <summary>
    /// Opens profile for editing.
    /// </summary>
    [RelayCommand]
    private void EditProfile(ShellProfile? profile)
    {
        if (profile == null) return;

        _logger.LogDebug("Editing profile: {Name}", profile.Name);
        SelectedProfile = profile;
        ProfileEditRequested?.Invoke(this, profile);
    }

    /// <summary>
    /// Deletes a custom profile.
    /// </summary>
    [RelayCommand]
    private void DeleteProfile(ShellProfile? profile)
    {
        if (profile == null) return;

        if (profile.IsBuiltIn)
        {
            _logger.LogWarning("Cannot delete built-in profile: {Name}", profile.Name);
            return;
        }

        _logger.LogInformation("Deleting profile: {Name}", profile.Name);

        ShellProfiles.Remove(profile);

        if (DefaultProfile == profile)
        {
            DefaultProfile = ShellProfiles.FirstOrDefault();
        }

        MarkDirty();
    }

    /// <summary>
    /// Sets a profile as the default.
    /// </summary>
    [RelayCommand]
    private void SetDefaultProfile(ShellProfile? profile)
    {
        if (profile == null) return;

        DefaultProfile = profile;
        _logger.LogDebug("Set default profile: {Name}", profile.Name);
    }

    /// <summary>
    /// Updates a profile after editing.
    /// </summary>
    public void UpdateProfile(ShellProfile updatedProfile)
    {
        var existing = ShellProfiles.FirstOrDefault(p => p.Id == updatedProfile.Id);
        if (existing != null)
        {
            var index = ShellProfiles.IndexOf(existing);
            ShellProfiles[index] = updatedProfile;
            MarkDirty();
            _logger.LogDebug("Updated profile: {Name}", updatedProfile.Name);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when appearance settings change for live preview.
    /// </summary>
    public event EventHandler<TerminalSettings>? PreviewSettingsChanged;

    /// <summary>
    /// Raised when settings are successfully saved.
    /// </summary>
    public event EventHandler? SettingsSaved;

    /// <summary>
    /// Raised when profile editing is requested.
    /// </summary>
    public event EventHandler<ShellProfile>? ProfileEditRequested;
}
