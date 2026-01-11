namespace SeniorIntern.Core.Models;

public sealed record ModelLoadOptions(
    uint ContextSize = 4096,
    int GpuLayerCount = -1,
    uint BatchSize = 512,
    bool UseMemoryMapping = true
);

public sealed record ModelLoadProgress(
    string Stage,
    double PercentComplete,
    string? Message = null
);

public sealed record InferenceOptions(
    int MaxTokens = 2048,
    float Temperature = 0.7f,
    float TopP = 0.9f,
    IReadOnlyList<string>? StopSequences = null
);
