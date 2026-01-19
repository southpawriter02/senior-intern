// ============================================================================
// File: ShellProfileEditorViewModel.cs
// Path: src/AIntern.Desktop/ViewModels/ShellProfileEditorViewModel.cs
// Description: ViewModel for the shell profile editor dialog.
// Created: 2026-01-19
// AI Intern v0.5.5g - Shell Profile Editor
// ============================================================================

namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ ShellProfileEditorViewModel (v0.5.5g)                                        │
// │ ViewModel for the shell profile editor dialog with validation,              │
// │ shell detection, version display, and environment variable management.      │
// │                                                                              │
// │ Features:                                                                    │
// │   - Real-time validation with error messages                                │
// │   - Shell type auto-detection from path                                     │
// │   - Async shell version detection                                           │
// │   - Environment variable parsing (multiline KEY=VALUE format)               │
// │   - File/folder browser integration via Avalonia StorageProvider            │
// │   - Support for both new profile creation and existing profile editing      │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for the shell profile editor dialog.
/// </summary>
/// <remarks>
/// <para>
/// Provides two-way binding for all shell profile properties with:
/// </para>
/// <list type="bullet">
///   <item><description>Real-time validation with error messages for name and path</description></item>
///   <item><description>Shell type auto-detection from the executable path</description></item>
///   <item><description>Asynchronous shell version detection</description></item>
///   <item><description>Environment variable parsing in multiline KEY=VALUE format</description></item>
///   <item><description>File picker for shell executable selection</description></item>
///   <item><description>Folder picker for working directory selection</description></item>
/// </list>
/// <para>Added in v0.5.5g.</para>
/// </remarks>
public partial class ShellProfileEditorViewModel : ViewModelBase
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Dependencies
    // ═══════════════════════════════════════════════════════════════════════════

    private readonly ILogger<ShellProfileEditorViewModel>? _logger;
    private readonly IShellDetectionService _shellDetectionService;

    /// <summary>Original profile for preserving identity during edits.</summary>
    private ShellProfile _originalProfile;

    /// <summary>Cancellation token source for version detection.</summary>
    private CancellationTokenSource? _versionDetectionCts;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties - Identity
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Display name for the profile (required).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the name shown in the shell profile list and tab titles.
    /// Must be non-empty. Validation error is set in <see cref="NameError"/>
    /// if empty.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _profileName = string.Empty;

    /// <summary>
    /// Validation error message for profile name, if any.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set to "Profile name is required" when <see cref="ProfileName"/> is empty.
    /// Null when validation passes.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string? _nameError;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties - Shell Configuration
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Full path to the shell executable (required).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Must be a valid, existing executable file. Changes trigger:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Validation via <see cref="Validate"/></description></item>
    ///   <item><description>Shell type detection via <see cref="DetectShellTypeFromPath"/></description></item>
    ///   <item><description>Version detection via <see cref="DetectVersionAsync"/></description></item>
    /// </list>
    /// </remarks>
    [ObservableProperty]
    private string _shellPath = string.Empty;

    /// <summary>
    /// Validation error message for shell path, if any.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set to an error message when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"Shell path is required" - when path is empty</description></item>
    ///   <item><description>"Shell executable not found or not accessible" - when path doesn't exist</description></item>
    /// </list>
    /// <para>Null when validation passes.</para>
    /// </remarks>
    [ObservableProperty]
    private string? _pathError;

    /// <summary>
    /// Detected or manually selected shell type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Auto-detected when <see cref="ShellPath"/> changes. Can be manually
    /// overridden via the dropdown if detection is incorrect.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private ShellType _shellType = ShellType.Unknown;

    /// <summary>
    /// Detected shell version string, if available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated asynchronously by running the shell with --version.
    /// Examples: "bash 5.1.16", "zsh 5.9", "PowerShell 7.4.0".
    /// Null if version cannot be detected.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string? _detectedVersion;

    /// <summary>
    /// Indicates whether version detection is currently in progress.
    /// </summary>
    /// <remarks>
    /// <para>
    /// True while the async version detection is running.
    /// Used to show a loading indicator in the UI.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isDetectingVersion;

    /// <summary>
    /// Command-line arguments passed to the shell on startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional. Common examples:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"-l" or "--login" for login shell behavior</description></item>
    ///   <item><description>"--norc" to skip profile scripts</description></item>
    ///   <item><description>"-i" for interactive mode</description></item>
    /// </list>
    /// </remarks>
    [ObservableProperty]
    private string _arguments = string.Empty;

    /// <summary>
    /// Starting directory for new terminal sessions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional. When empty, the terminal inherits the workspace directory.
    /// Supports ~ expansion for home directory on Unix systems.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _workingDirectory = string.Empty;

    /// <summary>
    /// Environment variables as multiline text in KEY=VALUE format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each line should be in the format KEY=VALUE. Lines without an equals
    /// sign are ignored. Empty lines are skipped.
    /// </para>
    /// <para>Example:</para>
    /// <code>
    /// EDITOR=vim
    /// PAGER=less
    /// MY_VAR=custom_value
    /// </code>
    /// </remarks>
    [ObservableProperty]
    private string _environmentVariables = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties - Options
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether to run the shell as a login shell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, adds the appropriate login flag to shell invocation
    /// (typically -l or --login). This causes the shell to source login
    /// profile scripts like .bash_profile, .zprofile, etc.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isLoginShell;

    /// <summary>
    /// Whether to use Windows ConPTY for terminal emulation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows only. ConPTY (Console Pseudoterminal) provides improved
    /// terminal emulation on Windows 10 1809+. Should generally be enabled
    /// for modern Windows systems.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _useConPty = true;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties - State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether the current form configuration is valid for saving.
    /// </summary>
    /// <remarks>
    /// <para>
    /// True when both <see cref="NameError"/> and <see cref="PathError"/> are null.
    /// Used to enable/disable the Save button.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isValid;

    /// <summary>
    /// Whether we are editing an existing profile (vs. creating a new one).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set to true when <see cref="LoadProfile"/> is called.
    /// Affects the window title display.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isEditMode;

    /// <summary>
    /// Whether the profile being edited is a built-in system profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built-in profiles are detected shells that cannot be deleted.
    /// When editing a built-in profile, a copy is created instead.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isBuiltIn;

    // ═══════════════════════════════════════════════════════════════════════════
    // Static/Computed Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Available shell types for the dropdown selector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns all values from the <see cref="ShellType"/> enum.
    /// </para>
    /// </remarks>
    public IReadOnlyList<ShellType> ShellTypes { get; } = Enum.GetValues<ShellType>();

    /// <summary>
    /// Whether the current platform is Windows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used to conditionally show Windows-specific options like ConPTY.
    /// </para>
    /// </remarks>
    public bool IsWindows => OperatingSystem.IsWindows();

    /// <summary>
    /// Window title based on edit mode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns "New Shell Profile" for new profiles or
    /// "Edit Shell Profile" when editing an existing profile.
    /// </para>
    /// </remarks>
    public string WindowTitle => IsEditMode ? "Edit Shell Profile" : "New Shell Profile";

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new <see cref="ShellProfileEditorViewModel"/>.
    /// </summary>
    /// <param name="shellDetectionService">Service for shell type detection and validation.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="shellDetectionService"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// After construction, call either <see cref="LoadProfile"/> to edit an existing
    /// profile or <see cref="InitializeNewProfileAsync"/> to set up for creating a new profile.
    /// </para>
    /// </remarks>
    public ShellProfileEditorViewModel(
        IShellDetectionService shellDetectionService,
        ILogger<ShellProfileEditorViewModel>? logger = null)
    {
        _shellDetectionService = shellDetectionService ?? throw new ArgumentNullException(nameof(shellDetectionService));
        _logger = logger;

        // ─────────────────────────────────────────────────────────────────────
        // Initialize with empty profile
        // ─────────────────────────────────────────────────────────────────────
        _originalProfile = new ShellProfile
        {
            Id = Guid.NewGuid(),
            Name = "New Profile",
            IsBuiltIn = false
        };

        _logger?.LogDebug("ShellProfileEditorViewModel created");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Profile Loading
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads an existing profile for editing.
    /// </summary>
    /// <param name="profile">The profile to edit.</param>
    /// <remarks>
    /// <para>
    /// Populates all form fields from the profile properties. Sets
    /// <see cref="IsEditMode"/> to true and preserves the original profile
    /// ID for updates.
    /// </para>
    /// <para>
    /// Environment variables are formatted as multiline KEY=VALUE text.
    /// </para>
    /// </remarks>
    public void LoadProfile(ShellProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _logger?.LogDebug("Loading profile for editing: {Name} ({Id})", profile.Name, profile.Id);

        _originalProfile = profile;
        IsEditMode = true;
        IsBuiltIn = profile.IsBuiltIn;

        // ─────────────────────────────────────────────────────────────────────
        // Load identity
        // ─────────────────────────────────────────────────────────────────────
        ProfileName = profile.Name;

        // ─────────────────────────────────────────────────────────────────────
        // Load shell configuration
        // ─────────────────────────────────────────────────────────────────────
        ShellPath = profile.ShellPath;
        ShellType = profile.ShellType;
        Arguments = profile.Arguments ?? string.Empty;
        WorkingDirectory = profile.StartingDirectory ?? string.Empty;
        EnvironmentVariables = FormatEnvironmentVariables(profile.Environment);

        // ─────────────────────────────────────────────────────────────────────
        // Load options
        // ─────────────────────────────────────────────────────────────────────
        IsLoginShell = profile.Arguments?.Contains("-l") == true ||
                       profile.Arguments?.Contains("--login") == true;
        // Note: UseConPty would be loaded from profile if we had that property

        // ─────────────────────────────────────────────────────────────────────
        // Validate and detect version
        // ─────────────────────────────────────────────────────────────────────
        Validate();

        // Notify title change
        OnPropertyChanged(nameof(WindowTitle));

        _logger?.LogInformation("Loaded profile: {Name}", profile.Name);
    }

    /// <summary>
    /// Initializes the ViewModel for creating a new profile.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Sets up default values including detecting the system's default shell
    /// to pre-populate the shell path.
    /// </para>
    /// </remarks>
    public async Task InitializeNewProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Initializing new profile");

        IsEditMode = false;
        IsBuiltIn = false;

        _originalProfile = new ShellProfile
        {
            Id = Guid.NewGuid(),
            Name = "New Profile",
            IsBuiltIn = false
        };

        ProfileName = "New Profile";

        // ─────────────────────────────────────────────────────────────────────
        // Try to pre-populate with default shell
        // ─────────────────────────────────────────────────────────────────────
        try
        {
            var defaultShell = await _shellDetectionService.GetDefaultShellAsync(cancellationToken);
            if (!string.IsNullOrEmpty(defaultShell))
            {
                ShellPath = defaultShell;
                _logger?.LogDebug("Pre-populated shell path with default: {Path}", defaultShell);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Could not detect default shell for pre-population");
        }

        Validate();
        OnPropertyChanged(nameof(WindowTitle));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Property Change Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when <see cref="ProfileName"/> changes.
    /// </summary>
    /// <param name="value">The new profile name value.</param>
    partial void OnProfileNameChanged(string value)
    {
        _logger?.LogTrace("ProfileName changed to: {Value}", value);
        Validate();
        OnPropertyChanged(nameof(WindowTitle));
    }

    /// <summary>
    /// Called when <see cref="ShellPath"/> changes.
    /// </summary>
    /// <param name="value">The new shell path value.</param>
    partial void OnShellPathChanged(string value)
    {
        _logger?.LogTrace("ShellPath changed to: {Value}", value);
        Validate();
        DetectShellTypeFromPath();
        _ = DetectVersionAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates all form fields and updates error properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Checks:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Profile name is not empty</description></item>
    ///   <item><description>Shell path is not empty</description></item>
    ///   <item><description>Shell path points to a valid file</description></item>
    /// </list>
    /// <para>
    /// Updates <see cref="NameError"/>, <see cref="PathError"/>, and
    /// <see cref="IsValid"/> accordingly.
    /// </para>
    /// </remarks>
    private void Validate()
    {
        // ─────────────────────────────────────────────────────────────────────
        // Validate name
        // ─────────────────────────────────────────────────────────────────────
        NameError = string.IsNullOrWhiteSpace(ProfileName)
            ? "Profile name is required"
            : null;

        // ─────────────────────────────────────────────────────────────────────
        // Validate path
        // ─────────────────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(ShellPath))
        {
            PathError = "Shell path is required";
        }
        else if (!_shellDetectionService.ValidateShellPath(ShellPath))
        {
            PathError = "Shell executable not found or not accessible";
        }
        else
        {
            PathError = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update overall validity
        // ─────────────────────────────────────────────────────────────────────
        IsValid = NameError == null && PathError == null;

        _logger?.LogTrace("Validation result: IsValid={IsValid}, NameError={NameError}, PathError={PathError}",
            IsValid, NameError, PathError);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Shell Detection
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detects the shell type from the current shell path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="IShellDetectionService.DetectShellType"/> to determine
    /// the shell type based on the executable name. Updates <see cref="ShellType"/>.
    /// </para>
    /// </remarks>
    private void DetectShellTypeFromPath()
    {
        if (string.IsNullOrWhiteSpace(ShellPath))
        {
            ShellType = ShellType.Unknown;
            return;
        }

        var detectedType = _shellDetectionService.DetectShellType(ShellPath);
        ShellType = detectedType;

        _logger?.LogDebug("Detected shell type: {Type} from path: {Path}", detectedType, ShellPath);
    }

    /// <summary>
    /// Asynchronously detects the shell version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs the shell with --version to extract version information.
    /// Updates <see cref="DetectedVersion"/> and manages
    /// <see cref="IsDetectingVersion"/> state.
    /// </para>
    /// <para>
    /// Previous detection operations are cancelled when a new one starts.
    /// </para>
    /// </remarks>
    private async Task DetectVersionAsync()
    {
        if (string.IsNullOrWhiteSpace(ShellPath))
        {
            DetectedVersion = null;
            return;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cancel any previous detection
        // ─────────────────────────────────────────────────────────────────────
        _versionDetectionCts?.Cancel();
        _versionDetectionCts = new CancellationTokenSource();
        var token = _versionDetectionCts.Token;

        IsDetectingVersion = true;
        DetectedVersion = null;

        try
        {
            var version = await _shellDetectionService.GetShellVersionAsync(ShellPath, token);

            if (!token.IsCancellationRequested)
            {
                DetectedVersion = version;
                _logger?.LogDebug("Detected version: {Version} for {Path}", version, ShellPath);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogTrace("Version detection cancelled for: {Path}", ShellPath);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to detect version for: {Path}", ShellPath);
            DetectedVersion = null;
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsDetectingVersion = false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Opens a file picker dialog to select a shell executable.
    /// </summary>
    /// <param name="parentWindow">The parent window for the dialog.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Uses Avalonia's <see cref="IStorageProvider"/> for cross-platform file
    /// selection. On Windows, filters to executable file types.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task BrowseShellPathAsync(Window parentWindow)
    {
        _logger?.LogDebug("Opening file picker for shell path");

        var storageProvider = parentWindow.StorageProvider;

        var options = new FilePickerOpenOptions
        {
            Title = "Select Shell Executable",
            AllowMultiple = false
        };

        // ─────────────────────────────────────────────────────────────────────
        // Add Windows-specific file filters
        // ─────────────────────────────────────────────────────────────────────
        if (OperatingSystem.IsWindows())
        {
            options.FileTypeFilter = new[]
            {
                new FilePickerFileType("Executables")
                {
                    Patterns = new[] { "*.exe", "*.cmd", "*.bat" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*" }
                }
            };
        }

        var result = await storageProvider.OpenFilePickerAsync(options);

        if (result.Count > 0)
        {
            ShellPath = result[0].Path.LocalPath;
            _logger?.LogInformation("Selected shell path: {Path}", ShellPath);
        }
    }

    /// <summary>
    /// Opens a folder picker dialog to select a working directory.
    /// </summary>
    /// <param name="parentWindow">The parent window for the dialog.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Uses Avalonia's <see cref="IStorageProvider"/> for cross-platform folder
    /// selection.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task BrowseWorkingDirectoryAsync(Window parentWindow)
    {
        _logger?.LogDebug("Opening folder picker for working directory");

        var storageProvider = parentWindow.StorageProvider;

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Working Directory",
            AllowMultiple = false
        };

        var result = await storageProvider.OpenFolderPickerAsync(options);

        if (result.Count > 0)
        {
            WorkingDirectory = result[0].Path.LocalPath;
            _logger?.LogInformation("Selected working directory: {Path}", WorkingDirectory);
        }
    }

    /// <summary>
    /// Attempts to find the shell in PATH based on the executable name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Extracts the filename from <see cref="ShellPath"/> and searches the
    /// system PATH for a matching executable. Useful when a shell name is
    /// entered but the full path is needed.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void DetectFromPath()
    {
        if (string.IsNullOrWhiteSpace(ShellPath))
        {
            _logger?.LogDebug("Cannot detect from empty path");
            return;
        }

        var shellName = Path.GetFileNameWithoutExtension(ShellPath);
        var detected = _shellDetectionService.FindInPath(shellName);

        if (!string.IsNullOrEmpty(detected))
        {
            ShellPath = detected;
            _logger?.LogInformation("Found shell in PATH: {Path}", detected);
        }
        else
        {
            _logger?.LogDebug("Shell not found in PATH: {Name}", shellName);
        }
    }

    /// <summary>
    /// Clears the working directory field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When cleared, terminal sessions will inherit the workspace directory.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void ClearWorkingDirectory()
    {
        WorkingDirectory = string.Empty;
        _logger?.LogDebug("Cleared working directory");
    }

    /// <summary>
    /// Clears the environment variables field.
    /// </summary>
    [RelayCommand]
    private void ClearEnvironmentVariables()
    {
        EnvironmentVariables = string.Empty;
        _logger?.LogDebug("Cleared environment variables");
    }

    /// <summary>
    /// Adds a template environment variable line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Appends "KEY=value" as a template for the user to edit.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void AddEnvironmentVariable()
    {
        if (string.IsNullOrEmpty(EnvironmentVariables))
        {
            EnvironmentVariables = "KEY=value";
        }
        else
        {
            EnvironmentVariables += Environment.NewLine + "KEY=value";
        }

        _logger?.LogDebug("Added environment variable template");
    }

    /// <summary>
    /// Validates and saves the profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Builds a <see cref="ShellProfile"/> from the current form values and
    /// raises the <see cref="SaveRequested"/> event. The dialog code-behind
    /// handles closing the window.
    /// </para>
    /// <para>
    /// Does nothing if validation fails (<see cref="IsValid"/> is false).
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void Save()
    {
        if (!IsValid)
        {
            _logger?.LogWarning("Cannot save: validation failed");
            return;
        }

        var profile = GetProfile();
        _logger?.LogInformation("Saving profile: {Name} ({Id})", profile.Name, profile.Id);

        SaveRequested?.Invoke(this, profile);
    }

    /// <summary>
    /// Cancels editing and requests dialog closure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Raises the <see cref="CancelRequested"/> event. The dialog code-behind
    /// handles closing the window without returning a profile.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void Cancel()
    {
        _logger?.LogDebug("Cancelling profile editor");
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Profile Building
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds a <see cref="ShellProfile"/> from the current form values.
    /// </summary>
    /// <returns>A configured <see cref="ShellProfile"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// Preserves the original profile ID when editing. Trims whitespace from
    /// text fields. Parses environment variables from multiline format.
    /// </para>
    /// <para>
    /// The returned profile is never marked as built-in, as user-edited
    /// profiles are always custom profiles.
    /// </para>
    /// </remarks>
    public ShellProfile GetProfile()
    {
        var arguments = Arguments.Trim();

        // ─────────────────────────────────────────────────────────────────────
        // Handle login shell flag
        // ─────────────────────────────────────────────────────────────────────
        if (IsLoginShell && !arguments.Contains("-l") && !arguments.Contains("--login"))
        {
            arguments = string.IsNullOrEmpty(arguments) ? "-l" : $"-l {arguments}";
        }

        return new ShellProfile
        {
            Id = _originalProfile.Id,
            Name = ProfileName.Trim(),
            ShellPath = ShellPath.Trim(),
            ShellType = ShellType,
            Arguments = string.IsNullOrWhiteSpace(arguments) ? null : arguments,
            StartingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory) ? null : WorkingDirectory.Trim(),
            Environment = ParseEnvironmentVariables(EnvironmentVariables),
            IsBuiltIn = false,
            IsDefault = _originalProfile.IsDefault,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Checks if there are unsaved changes compared to the original profile.
    /// </summary>
    /// <returns>True if any field has been modified; otherwise false.</returns>
    /// <remarks>
    /// <para>
    /// Compares current form values against the original profile loaded via
    /// <see cref="LoadProfile"/>. For new profiles, always returns true if
    /// any required field is populated.
    /// </para>
    /// </remarks>
    public bool HasChanges()
    {
        if (!IsEditMode)
        {
            return !string.IsNullOrWhiteSpace(ProfileName) ||
                   !string.IsNullOrWhiteSpace(ShellPath);
        }

        return ProfileName != _originalProfile.Name ||
               ShellPath != _originalProfile.ShellPath ||
               Arguments != (_originalProfile.Arguments ?? string.Empty) ||
               WorkingDirectory != (_originalProfile.StartingDirectory ?? string.Empty);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Environment Variable Parsing
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses environment variables from multiline KEY=VALUE text.
    /// </summary>
    /// <param name="text">Multiline text with one KEY=VALUE per line.</param>
    /// <returns>
    /// A dictionary of environment variables, or an empty dictionary if the
    /// input is empty or contains no valid entries.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Parsing rules:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Empty lines are skipped</description></item>
    ///   <item><description>Lines without '=' are skipped</description></item>
    ///   <item><description>Keys are trimmed and must be non-empty</description></item>
    ///   <item><description>Values are trimmed</description></item>
    ///   <item><description>Duplicate keys use the last value</description></item>
    /// </list>
    /// </remarks>
    public static Dictionary<string, string> ParseEnvironmentVariables(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Skip comment lines
            if (line.StartsWith('#'))
            {
                continue;
            }

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex > 0)
            {
                var key = line[..equalsIndex].Trim();
                var value = line[(equalsIndex + 1)..].Trim();

                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Formats a dictionary of environment variables as multiline KEY=VALUE text.
    /// </summary>
    /// <param name="variables">Dictionary of environment variables.</param>
    /// <returns>Multiline text with one KEY=VALUE per line.</returns>
    /// <remarks>
    /// <para>
    /// Returns an empty string if the dictionary is null or empty.
    /// </para>
    /// </remarks>
    public static string FormatEnvironmentVariables(Dictionary<string, string>? variables)
    {
        if (variables == null || variables.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(Environment.NewLine, variables.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when the user clicks Save with valid data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The event argument contains the configured <see cref="ShellProfile"/>.
    /// The dialog code-behind should close the window and return the profile.
    /// </para>
    /// </remarks>
    public event EventHandler<ShellProfile>? SaveRequested;

    /// <summary>
    /// Raised when the user clicks Cancel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The dialog code-behind should close the window without returning a profile.
    /// </para>
    /// </remarks>
    public event EventHandler? CancelRequested;
}
