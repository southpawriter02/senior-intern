using SeniorIntern.Core.Events;
using SeniorIntern.Core.Models;

namespace SeniorIntern.Core.Interfaces;

public interface ILlmService : IAsyncDisposable
{
    /// <summary>
    /// Gets whether a model is currently loaded.
    /// </summary>
    bool IsModelLoaded { get; }

    /// <summary>
    /// Gets the currently loaded model path, or null if no model is loaded.
    /// </summary>
    string? CurrentModelPath { get; }

    /// <summary>
    /// Gets the filename of the currently loaded model.
    /// </summary>
    string? CurrentModelName { get; }

    /// <summary>
    /// Loads a GGUF model from the specified path.
    /// </summary>
    Task LoadModelAsync(
        string modelPath,
        ModelLoadOptions options,
        IProgress<ModelLoadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads the current model and releases resources.
    /// </summary>
    Task UnloadModelAsync();

    /// <summary>
    /// Generates a streaming response for the given conversation.
    /// </summary>
    IAsyncEnumerable<string> GenerateStreamingAsync(
        IEnumerable<ChatMessage> conversation,
        InferenceOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the current inference operation.
    /// </summary>
    void CancelCurrentInference();

    /// <summary>
    /// Raised when model loading state changes.
    /// </summary>
    event EventHandler<ModelStateChangedEventArgs>? ModelStateChanged;

    /// <summary>
    /// Raised during inference to report progress.
    /// </summary>
    event EventHandler<InferenceProgressEventArgs>? InferenceProgress;
}
