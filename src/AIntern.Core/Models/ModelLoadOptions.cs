namespace AIntern.Core.Models;

/// <summary>
/// Configuration options for loading a GGUF model into memory.
/// </summary>
/// <param name="ContextSize">
/// The context window size in tokens (default: 4096).
/// Larger values allow longer conversations but use more memory.
/// </param>
/// <param name="GpuLayerCount">
/// Number of layers to offload to GPU.
/// Use -1 for auto-detection (Metal on macOS, CPU elsewhere).
/// Use 0 for CPU-only, or specific count for manual control.
/// </param>
/// <param name="BatchSize">
/// Token processing batch size (default: 512).
/// Larger values may improve throughput at cost of latency.
/// </param>
/// <param name="UseMemoryMapping">
/// Whether to use memory-mapped file loading for faster startup.
/// Recommended for large models to reduce initial load time.
/// </param>
public sealed record ModelLoadOptions(
    uint ContextSize = 4096,
    int GpuLayerCount = -1,
    uint BatchSize = 512,
    bool UseMemoryMapping = true
);

/// <summary>
/// Reports progress during model loading operations.
/// </summary>
/// <param name="Stage">
/// The current loading stage name.
/// Typical values: "Initializing", "Loading", "Context", "Complete".
/// </param>
/// <param name="PercentComplete">
/// Progress percentage from 0 to 100.
/// Can be used to drive a progress bar UI.
/// </param>
/// <param name="Message">
/// Optional detailed status message for display.
/// Falls back to Stage if not provided.
/// </param>
public sealed record ModelLoadProgress(
    string Stage,
    double PercentComplete,
    string? Message = null
);

/// <summary>
/// Configuration options for text generation inference.
/// </summary>
/// <param name="MaxTokens">
/// Maximum number of tokens to generate (default: 2048).
/// Generation stops when this limit is reached.
/// </param>
/// <param name="Temperature">
/// Sampling temperature controlling creativity (default: 0.7).
/// Range: 0.0 (deterministic) to 2.0 (very random).
/// Typical values: 0.1-0.5 (focused), 0.7-1.0 (balanced), 1.0+ (creative).
/// </param>
/// <param name="TopP">
/// Nucleus sampling threshold (default: 0.9).
/// Limits tokens to smallest set with cumulative probability >= TopP.
/// Lower values make output more focused.
/// </param>
/// <param name="StopSequences">
/// Optional sequences that trigger generation to stop.
/// Defaults to user turn indicators like "User:" if not specified.
/// </param>
public sealed record InferenceOptions(
    int MaxTokens = 2048,
    float Temperature = 0.7f,
    float TopP = 0.9f,
    IReadOnlyList<string>? StopSequences = null
);
