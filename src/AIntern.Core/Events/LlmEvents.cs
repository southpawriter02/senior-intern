namespace AIntern.Core.Events;

/// <summary>
/// Event arguments raised when the LLM model loading state changes.
/// </summary>
/// <remarks>
/// Fired when a model is loaded or unloaded via <see cref="Interfaces.ILlmService"/>.
/// </remarks>
public sealed class ModelStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether a model is currently loaded.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    /// Gets the file path of the loaded model, or null if no model is loaded.
    /// </summary>
    public string? ModelPath { get; }

    /// <summary>
    /// Gets the filename of the loaded model, or null if no model is loaded.
    /// Extracts just the filename from the full path for display purposes.
    /// </summary>
    public string? ModelName => ModelPath is null ? null : Path.GetFileName(ModelPath);

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="isLoaded">Whether a model is loaded.</param>
    /// <param name="modelPath">The path to the loaded model, or null.</param>
    public ModelStateChangedEventArgs(bool isLoaded, string? modelPath)
    {
        IsLoaded = isLoaded;
        ModelPath = modelPath;
    }
}

/// <summary>
/// Event arguments raised when application settings are changed.
/// </summary>
/// <remarks>
/// Fired after settings are saved via <see cref="Interfaces.ISettingsService.SaveSettingsAsync"/>.
/// Subscribers can react to settings changes (e.g., theme updates).
/// </remarks>
public sealed class SettingsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the updated application settings.
    /// Contains the complete settings state after modification.
    /// </summary>
    public required Models.AppSettings Settings { get; init; }
}

/// <summary>
/// Event arguments raised during inference to report generation progress.
/// </summary>
/// <remarks>
/// Fired periodically (every ~10 tokens) during text generation
/// to update UI with token count and speed statistics.
/// </remarks>
public sealed class InferenceProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets the total number of tokens generated so far.
    /// Increments as each token is produced during streaming.
    /// </summary>
    public int TokensGenerated { get; init; }

    /// <summary>
    /// Gets the elapsed time since inference started.
    /// Used with TokensGenerated to calculate generation speed.
    /// </summary>
    public TimeSpan Elapsed { get; init; }

    /// <summary>
    /// Gets the current token generation rate in tokens per second.
    /// Calculated from TokensGenerated / Elapsed. Returns 0 if no time has elapsed.
    /// </summary>
    public double TokensPerSecond => Elapsed.TotalSeconds > 0
        ? TokensGenerated / Elapsed.TotalSeconds
        : 0;
}
