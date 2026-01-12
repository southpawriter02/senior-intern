using AIntern.Core.Events;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Defines the contract for persisting and retrieving application settings.
/// </summary>
/// <remarks>
/// Implementation notes:
/// <list type="bullet">
/// <item>Settings are persisted to JSON in the user's app data directory</item>
/// <item>Load failures fall back to default settings (no crash)</item>
/// <item>Save failures are silently ignored to prevent app crashes</item>
/// </list>
/// Storage locations by platform:
/// <list type="bullet">
/// <item>Windows: %APPDATA%\AIntern\settings.json</item>
/// <item>macOS: ~/Library/Application Support/AIntern/settings.json</item>
/// <item>Linux: ~/.config/AIntern/settings.json</item>
/// </list>
/// </remarks>
public interface ISettingsService
{
    #region Properties

    /// <summary>
    /// Gets the current in-memory application settings.
    /// Call <see cref="LoadSettingsAsync"/> on startup to populate from disk.
    /// </summary>
    AppSettings CurrentSettings { get; }

    #endregion

    #region Persistence

    /// <summary>
    /// Loads settings from persistent storage.
    /// Returns default settings if file doesn't exist or is corrupted.
    /// </summary>
    /// <returns>The loaded (or default) settings.</returns>
    Task<AppSettings> LoadSettingsAsync();

    /// <summary>
    /// Saves settings to persistent storage.
    /// Updates <see cref="CurrentSettings"/> and fires <see cref="SettingsChanged"/>.
    /// </summary>
    /// <param name="settings">The settings to persist.</param>
    Task SaveSettingsAsync(AppSettings settings);

    #endregion

    #region Events

    /// <summary>
    /// Raised when settings are saved successfully.
    /// Subscribe to react to settings changes (e.g., theme updates).
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    #endregion
}
