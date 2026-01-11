using SeniorIntern.Core.Events;
using SeniorIntern.Core.Models;

namespace SeniorIntern.Core.Interfaces;

public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings CurrentSettings { get; }

    /// <summary>
    /// Loads settings from persistent storage.
    /// </summary>
    Task<AppSettings> LoadSettingsAsync();

    /// <summary>
    /// Saves settings to persistent storage.
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Raised when settings are changed.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}
