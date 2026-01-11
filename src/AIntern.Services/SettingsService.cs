using System.Text.Json;
using SeniorIntern.Core.Events;
using SeniorIntern.Core.Interfaces;
using SeniorIntern.Core.Models;

namespace SeniorIntern.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private AppSettings _currentSettings = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings CurrentSettings => _currentSettings;

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "SeniorIntern");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                _currentSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // If settings file is corrupted, use defaults
            _currentSettings = new AppSettings();
        }

        return _currentSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _currentSettings = settings;

        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { Settings = settings });
        }
        catch (Exception)
        {
            // Log error but don't throw - settings save failure shouldn't crash the app
        }
    }
}
