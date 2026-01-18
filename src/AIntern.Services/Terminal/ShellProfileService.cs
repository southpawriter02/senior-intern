using System.Text.Json;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;

namespace AIntern.Services.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL PROFILE SERVICE (v0.5.3d)                                         │
// │ CRUD operations, persistence, and built-in profile generation.          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Manages shell profiles with JSON persistence.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3d.</para>
/// <para>
/// Features:
/// <list type="bullet">
///   <item>CRUD operations with validation</item>
///   <item>JSON persistence to app data directory</item>
///   <item>Automatic built-in profile generation from detected shells</item>
///   <item>Thread-safe operations using SemaphoreSlim</item>
///   <item>Default profile resolution with fallback chain</item>
///   <item>Import/export functionality</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ShellProfileService : IShellProfileService
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly IShellDetectionService _shellDetection;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ShellProfileService> _logger;
    private readonly string _profilesPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<ShellProfile>? _profiles;
    private bool _initialized;

    // ─────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public event EventHandler<ProfilesChangedEventArgs>? ProfilesChanged;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new shell profile service.
    /// </summary>
    /// <param name="shellDetection">Shell detection service for validation.</param>
    /// <param name="settingsService">Settings service for app settings.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public ShellProfileService(
        IShellDetectionService shellDetection,
        ISettingsService settingsService,
        ILogger<ShellProfileService> logger)
    {
        _shellDetection = shellDetection ?? throw new ArgumentNullException(nameof(shellDetection));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Determine profile storage path based on platform
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AIntern");
        Directory.CreateDirectory(appDataPath);
        _profilesPath = Path.Combine(appDataPath, "shell-profiles.json");

        _logger.LogDebug("ShellProfileService initialized, profiles path: {Path}", _profilesPath);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Read Operations
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShellProfile>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        _logger.LogDebug("GetAllProfilesAsync: returning {Count} profiles", _profiles!.Count);
        return _profiles.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShellProfile>> GetVisibleProfilesAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        var visible = _profiles!
            .Where(p => !p.IsHidden)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToList()
            .AsReadOnly();

        _logger.LogDebug("GetVisibleProfilesAsync: returning {Count} visible profiles", visible.Count);
        return visible;
    }

    /// <inheritdoc />
    public async Task<ShellProfile?> GetProfileAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        var profile = _profiles!.FirstOrDefault(p => p.Id == id);

        _logger.LogDebug("GetProfileAsync: {Id} -> {Found}", id,
            profile != null ? profile.Name : "not found");

        return profile;
    }

    /// <inheritdoc />
    public async Task<ShellProfile> GetDefaultProfileAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);


        var settings = _settingsService.CurrentSettings;

        // Priority 1: AppSettings.DefaultShellProfileId
        if (settings.DefaultShellProfileId.HasValue)
        {
            var configured = _profiles!.FirstOrDefault(p => p.Id == settings.DefaultShellProfileId.Value);
            if (configured != null)
            {
                _logger.LogDebug("GetDefaultProfileAsync: using configured default {Name}", configured.Name);
                return configured;
            }
        }

        // Priority 2: Profile with IsDefault = true
        var defaultProfile = _profiles!.FirstOrDefault(p => p.IsDefault);
        if (defaultProfile != null)
        {
            _logger.LogDebug("GetDefaultProfileAsync: using IsDefault profile {Name}", defaultProfile.Name);
            return defaultProfile;
        }

        // Priority 3: First available
        _logger.LogDebug("GetDefaultProfileAsync: using first profile {Name}", _profiles![0].Name);
        return _profiles[0];
    }

    // ─────────────────────────────────────────────────────────────────────
    // Write Operations
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ShellProfile> CreateProfileAsync(ShellProfile profile, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await _lock.WaitAsync(ct);

        try
        {
            // Validate shell path
            if (string.IsNullOrWhiteSpace(profile.ShellPath))
            {
                _logger.LogWarning("CreateProfileAsync: empty shell path");
                throw new ArgumentException("Shell path is required", nameof(profile));
            }

            if (!_shellDetection.ValidateShellPath(profile.ShellPath))
            {
                _logger.LogWarning("CreateProfileAsync: invalid shell path: {Path}", profile.ShellPath);
                throw new ArgumentException($"Invalid shell path: {profile.ShellPath}", nameof(profile));
            }

            // Auto-detect shell type if unknown
            if (profile.ShellType == ShellType.Unknown)
            {
                profile.ShellType = _shellDetection.DetectShellType(profile.ShellPath);
                _logger.LogDebug("CreateProfileAsync: auto-detected type {Type}", profile.ShellType);
            }

            // Update timestamps and add
            profile.ModifiedAt = DateTime.UtcNow;
            _profiles!.Add(profile);
            await SaveProfilesAsync(ct);

            _logger.LogInformation("Created profile: {Name} ({Id})", profile.Name, profile.Id);

            // Raise event
            ProfilesChanged?.Invoke(this, new ProfilesChangedEventArgs
            {
                ChangeType = ProfileChangeType.Added,
                ProfileId = profile.Id,
                Profile = profile
            });

            return profile;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateProfileAsync(ShellProfile profile, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await _lock.WaitAsync(ct);

        try
        {
            // Find existing profile
            var existing = _profiles!.FirstOrDefault(p => p.Id == profile.Id);
            if (existing == null)
            {
                _logger.LogWarning("UpdateProfileAsync: profile not found: {Id}", profile.Id);
                throw new InvalidOperationException($"Profile not found: {profile.Id}");
            }

            // Prevent modification of built-in profiles
            if (existing.IsBuiltIn)
            {
                _logger.LogWarning("UpdateProfileAsync: cannot modify built-in profile: {Name}", existing.Name);
                throw new InvalidOperationException("Cannot modify built-in profile");
            }

            // Replace profile
            var index = _profiles.IndexOf(existing);
            profile.ModifiedAt = DateTime.UtcNow;
            _profiles[index] = profile;
            await SaveProfilesAsync(ct);

            _logger.LogInformation("Updated profile: {Name} ({Id})", profile.Name, profile.Id);

            // Raise event
            ProfilesChanged?.Invoke(this, new ProfilesChangedEventArgs
            {
                ChangeType = ProfileChangeType.Updated,
                ProfileId = profile.Id,
                Profile = profile
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteProfileAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await _lock.WaitAsync(ct);

        try
        {
            var profile = _profiles!.FirstOrDefault(p => p.Id == id);
            if (profile == null)
            {
                _logger.LogDebug("DeleteProfileAsync: profile not found, ignoring: {Id}", id);
                return;
            }

            // Prevent deletion of built-in profiles
            if (profile.IsBuiltIn)
            {
                _logger.LogWarning("DeleteProfileAsync: cannot delete built-in profile: {Name}", profile.Name);
                throw new InvalidOperationException("Cannot delete built-in profile");
            }

            _profiles.Remove(profile);

            // Clear default if this was the default profile
            var settings = _settingsService.CurrentSettings;
            if (settings.DefaultShellProfileId == id)
            {
                settings.DefaultShellProfileId = null;
                await _settingsService.SaveSettingsAsync(settings);
                _logger.LogDebug("DeleteProfileAsync: cleared DefaultShellProfileId");
            }

            await SaveProfilesAsync(ct);

            _logger.LogInformation("Deleted profile: {Id}", id);

            // Raise event
            ProfilesChanged?.Invoke(this, new ProfilesChangedEventArgs
            {
                ChangeType = ProfileChangeType.Deleted,
                ProfileId = id
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetDefaultProfileAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await _lock.WaitAsync(ct);

        try
        {
            var profile = _profiles!.FirstOrDefault(p => p.Id == id);
            if (profile == null)
            {
                _logger.LogWarning("SetDefaultProfileAsync: profile not found: {Id}", id);
                throw new InvalidOperationException($"Profile not found: {id}");
            }

            // Clear IsDefault on all profiles
            foreach (var p in _profiles.Where(p => p.IsDefault))
            {
                p.IsDefault = false;
            }

            // Mark new default
            profile.IsDefault = true;

            // Update AppSettings
            var settings = _settingsService.CurrentSettings;
            settings.DefaultShellProfileId = id;
            await _settingsService.SaveSettingsAsync(settings);

            await SaveProfilesAsync(ct);

            _logger.LogInformation("Set default profile: {Name} ({Id})", profile.Name, id);

            // Raise event
            ProfilesChanged?.Invoke(this, new ProfilesChangedEventArgs
            {
                ChangeType = ProfileChangeType.DefaultChanged,
                ProfileId = id,
                Profile = profile
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ShellProfile> DuplicateProfileAsync(Guid id, CancellationToken ct = default)
    {
        var source = await GetProfileAsync(id, ct);
        if (source == null)
        {
            _logger.LogWarning("DuplicateProfileAsync: profile not found: {Id}", id);
            throw new InvalidOperationException($"Profile not found: {id}");
        }

        _logger.LogDebug("DuplicateProfileAsync: cloning {Name}", source.Name);
        return await CreateProfileAsync(source.Clone(), ct);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Bulk Operations
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);

        try
        {
            _logger.LogInformation("Resetting profiles to defaults");

            _profiles = new List<ShellProfile>();
            await GenerateBuiltInProfilesAsync(ct);
            await SaveProfilesAsync(ct);

            _logger.LogInformation("Reset complete, {Count} profiles generated", _profiles.Count);

            // Raise event
            ProfilesChanged?.Invoke(this, new ProfilesChangedEventArgs
            {
                ChangeType = ProfileChangeType.Reset
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> ImportProfilesAsync(string json, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogDebug("ImportProfilesAsync: empty JSON, returning 0");
            return 0;
        }

        List<ShellProfile>? imported;
        try
        {
            imported = JsonSerializer.Deserialize<List<ShellProfile>>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "ImportProfilesAsync: failed to parse JSON");
            return 0;
        }

        if (imported == null || imported.Count == 0)
        {
            _logger.LogDebug("ImportProfilesAsync: no profiles in JSON");
            return 0;
        }

        await EnsureInitializedAsync(ct);
        await _lock.WaitAsync(ct);

        try
        {
            var count = 0;

            foreach (var profile in imported)
            {
                // Create new profile with new ID (avoid conflicts)
                var newProfile = new ShellProfile
                {
                    Name = profile.Name,
                    ShellPath = profile.ShellPath,
                    ShellType = profile.ShellType,
                    Arguments = profile.Arguments,
                    StartingDirectory = profile.StartingDirectory,
                    StartupCommand = profile.StartupCommand,
                    Environment = new Dictionary<string, string>(profile.Environment),
                    FontFamily = profile.FontFamily,
                    FontSize = profile.FontSize,
                    ThemeName = profile.ThemeName,
                    CursorStyle = profile.CursorStyle,
                    CursorBlink = profile.CursorBlink,
                    ScrollbackLines = profile.ScrollbackLines,
                    CloseOnExit = profile.CloseOnExit,
                    BellStyle = profile.BellStyle,
                    TabTitleFormat = profile.TabTitleFormat,
                    IconPath = profile.IconPath,
                    AccentColor = profile.AccentColor,
                    IsBuiltIn = false  // Imported profiles are never built-in
                };

                // Validate and add
                if (_shellDetection.ValidateShellPath(newProfile.ShellPath))
                {
                    _profiles!.Add(newProfile);
                    count++;
                    _logger.LogDebug("ImportProfilesAsync: imported {Name}", newProfile.Name);
                }
                else
                {
                    _logger.LogDebug("ImportProfilesAsync: skipped {Name} (invalid path)", profile.Name);
                }
            }

            await SaveProfilesAsync(ct);
            _logger.LogInformation("Imported {Count} profiles", count);

            return count;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportProfilesAsync(IEnumerable<Guid>? profileIds = null, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        var toExport = profileIds == null
            ? _profiles!.Where(p => !p.IsBuiltIn).ToList()
            : _profiles!.Where(p => profileIds.Contains(p.Id)).ToList();

        _logger.LogDebug("ExportProfilesAsync: exporting {Count} profiles", toExport.Count);

        return JsonSerializer.Serialize(toExport, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ShellProfileDefaults GetEffectiveSettings(ShellProfile profile)
    {
        var settings = _settingsService.CurrentSettings;

        var effective = new ShellProfileDefaults
        {
            FontFamily = profile.FontFamily ?? settings.TerminalFontFamily,
            FontSize = profile.FontSize ?? settings.TerminalFontSize,
            ThemeName = profile.ThemeName ?? settings.TerminalTheme,
            CursorStyle = profile.CursorStyle ?? settings.TerminalCursorStyle,
            CursorBlink = profile.CursorBlink ?? settings.TerminalCursorBlink,
            ScrollbackLines = profile.ScrollbackLines ?? settings.TerminalScrollbackLines,
            BellStyle = profile.BellStyle,
            CloseOnExit = profile.CloseOnExit
        };

        _logger.LogDebug("GetEffectiveSettings: resolved for {Name}", profile.Name);
        return effective;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Initialization
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures profiles are loaded from disk.
    /// </summary>
    /// <remarks>
    /// Uses double-checked locking for thread safety.
    /// </remarks>
    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;

        await _lock.WaitAsync(ct);
        try
        {
            if (_initialized) return;

            await LoadProfilesAsync(ct);
            _initialized = true;

            _logger.LogDebug("EnsureInitializedAsync: initialization complete");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads profiles from disk and generates built-in profiles.
    /// </summary>
    private async Task LoadProfilesAsync(CancellationToken ct)
    {
        if (File.Exists(_profilesPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_profilesPath, ct);
                _profiles = JsonSerializer.Deserialize<List<ShellProfile>>(json) ?? new();
                _logger.LogDebug("LoadProfilesAsync: loaded {Count} profiles from disk", _profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load profiles from {Path}, regenerating", _profilesPath);
                _profiles = new List<ShellProfile>();
            }
        }
        else
        {
            _logger.LogDebug("LoadProfilesAsync: no profiles file found, creating new");
            _profiles = new List<ShellProfile>();
        }

        // Always ensure built-in profiles exist
        await GenerateBuiltInProfilesAsync(ct);
    }

    /// <summary>
    /// Generates built-in profiles from detected shells.
    /// </summary>
    private async Task GenerateBuiltInProfilesAsync(CancellationToken ct)
    {
        var shells = await _shellDetection.GetAvailableShellsAsync(ct);

        // Get existing built-in paths to avoid duplicates
        var existingPaths = _profiles!
            .Where(p => p.IsBuiltIn)
            .Select(p => p.ShellPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Add new built-in profiles for detected shells
        foreach (var shell in shells.Where(s => !existingPaths.Contains(s.Path)))
        {
            var profile = new ShellProfile
            {
                Name = shell.Name,
                ShellPath = shell.Path,
                ShellType = shell.ShellType,
                IsDefault = shell.IsDefault,
                IsBuiltIn = true,
                Arguments = shell.DefaultArguments != null ? string.Join(" ", shell.DefaultArguments) : null,
                IconPath = shell.IconPath,
                SortOrder = shell.IsDefault ? 0 : 100
            };

            _profiles.Add(profile);
            _logger.LogDebug("GenerateBuiltInProfilesAsync: added {Name}", shell.Name);
        }

        // Ensure at least one profile is default
        if (!_profiles.Any(p => p.IsDefault) && _profiles.Count > 0)
        {
            _profiles[0].IsDefault = true;
            _logger.LogDebug("GenerateBuiltInProfilesAsync: marked first profile as default");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Persistence
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves profiles to disk.
    /// </summary>
    private async Task SaveProfilesAsync(CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_profilesPath, json, ct);
        _logger.LogDebug("SaveProfilesAsync: saved {Count} profiles to disk", _profiles!.Count);
    }
}
