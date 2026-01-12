using System.Text.Json;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

/// <summary>
/// Manages persistent application settings using JSON file storage.
/// Settings are stored in the user's application data directory.
/// </summary>
/// <remarks>
/// <para>
/// Settings are stored at: <c>%APPDATA%/AIntern/settings.json</c> (Windows)
/// or <c>~/.config/AIntern/settings.json</c> (Linux/macOS).
/// </para>
/// <para>
/// If the settings file is corrupted or cannot be read, default settings are used.
/// Save failures are silently ignored to prevent application crashes.
/// </para>
/// </remarks>
public sealed class SettingsService : ISettingsService
{
    // Full path to the settings.json file
    private readonly string _settingsPath;
    
    // In-memory copy of current settings (avoids repeated disk reads)
    private AppSettings _currentSettings = new();

    // JSON serialization options for consistent formatting
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,                       // Pretty-print for human readability
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase  // Use camelCase in JSON
    };

    /// <inheritdoc />
    public AppSettings CurrentSettings => _currentSettings;

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// Creates the application data directory if it doesn't exist.
    /// </summary>
    public SettingsService()
    {
        // Get the platform-appropriate application data folder
        // Windows: C:\Users\{user}\AppData\Roaming
        // macOS: ~/Library/Application Support  
        // Linux: ~/.config
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        // Create AIntern subfolder for our settings
        var appFolder = Path.Combine(appData, "AIntern");
        Directory.CreateDirectory(appFolder); // No-op if already exists
        
        // Build full path to settings file
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            // Only attempt to load if file exists
            if (File.Exists(_settingsPath))
            {
                // Read and deserialize the settings file
                var json = await File.ReadAllTextAsync(_settingsPath);
                _currentSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) 
                    ?? new AppSettings(); // Fallback if deserialize returns null
            }
            // If file doesn't exist, keep default settings (new AppSettings())
        }
        catch (Exception)
        {
            // If settings file is corrupted or unreadable, use defaults
            // This prevents the app from crashing on startup
            _currentSettings = new AppSettings();
        }

        return _currentSettings;
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        // Update in-memory copy first
        _currentSettings = settings;

        try
        {
            // Serialize to JSON and write to disk
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
            
            // Notify listeners that settings have changed
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { Settings = settings });
        }
        catch (Exception)
        {
            // Log error but don't throw - settings save failure shouldn't crash the app
            // In production, this could be logged via ILogger
        }
    }
}
