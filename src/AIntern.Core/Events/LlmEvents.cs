namespace SeniorIntern.Core.Events;

public sealed class ModelStateChangedEventArgs : EventArgs
{
    public bool IsLoaded { get; }
    public string? ModelPath { get; }
    public string? ModelName => ModelPath is null ? null : Path.GetFileName(ModelPath);

    public ModelStateChangedEventArgs(bool isLoaded, string? modelPath)
    {
        IsLoaded = isLoaded;
        ModelPath = modelPath;
    }
}

public sealed class SettingsChangedEventArgs : EventArgs
{
    public required Models.AppSettings Settings { get; init; }
}

public sealed class InferenceProgressEventArgs : EventArgs
{
    public int TokensGenerated { get; init; }
    public TimeSpan Elapsed { get; init; }
    public double TokensPerSecond => Elapsed.TotalSeconds > 0
        ? TokensGenerated / Elapsed.TotalSeconds
        : 0;
}
