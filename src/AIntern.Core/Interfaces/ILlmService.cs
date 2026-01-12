using AIntern.Core.Events;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Defines the contract for LLM model loading and text generation.
/// Supports loading GGUF models, streaming inference, and resource management.
/// </summary>
/// <remarks>
/// Implementation notes:
/// <list type="bullet">
/// <item>Only one model can be loaded at a time</item>
/// <item>Model loading is async and supports progress reporting</item>
/// <item>Text generation is streaming (token-by-token output)</item>
/// <item>Implements IAsyncDisposable for proper resource cleanup</item>
/// </list>
/// </remarks>
public interface ILlmService : IAsyncDisposable
{
    #region Properties

    /// <summary>
    /// Gets whether a model is currently loaded and ready for inference.
    /// Check this before calling <see cref="GenerateStreamingAsync"/>.
    /// </summary>
    bool IsModelLoaded { get; }

    /// <summary>
    /// Gets the full file path of the currently loaded model, or null if no model is loaded.
    /// </summary>
    string? CurrentModelPath { get; }

    /// <summary>
    /// Gets the filename (without path) of the currently loaded model.
    /// Useful for display in the UI.
    /// </summary>
    string? CurrentModelName { get; }

    #endregion

    #region Model Management

    /// <summary>
    /// Loads a GGUF model from the specified path.
    /// </summary>
    /// <param name="modelPath">Full path to the GGUF model file.</param>
    /// <param name="options">Configuration options (context size, GPU layers, etc.).</param>
    /// <param name="progress">Optional progress reporter for UI updates.</param>
    /// <param name="cancellationToken">Token to cancel the load operation.</param>
    /// <exception cref="Exceptions.ModelLoadException">Thrown if model fails to load.</exception>
    Task LoadModelAsync(
        string modelPath,
        ModelLoadOptions options,
        IProgress<ModelLoadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads the current model and releases all resources (VRAM, context, etc.).
    /// Safe to call even if no model is loaded.
    /// </summary>
    Task UnloadModelAsync();

    #endregion

    #region Inference

    /// <summary>
    /// Generates a streaming response for the given conversation.
    /// Yields tokens one-by-one as they are generated.
    /// </summary>
    /// <param name="conversation">The conversation history to continue.</param>
    /// <param name="options">Inference parameters (temperature, max tokens, etc.).</param>
    /// <param name="cancellationToken">Token to cancel generation.</param>
    /// <returns>An async enumerable of generated tokens.</returns>
    /// <exception cref="Exceptions.InferenceException">Thrown if no model is loaded.</exception>
    IAsyncEnumerable<string> GenerateStreamingAsync(
        IEnumerable<ChatMessage> conversation,
        InferenceOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the current inference operation if one is in progress.
    /// The generation will stop and the partial response will be preserved.
    /// </summary>
    void CancelCurrentInference();

    #endregion

    #region Events

    /// <summary>
    /// Raised when model loading state changes (load or unload).
    /// Subscribe to update UI elements like status bar.
    /// </summary>
    event EventHandler<ModelStateChangedEventArgs>? ModelStateChanged;

    /// <summary>
    /// Raised during inference to report generation progress.
    /// Fired periodically with token count and speed statistics.
    /// </summary>
    event EventHandler<InferenceProgressEventArgs>? InferenceProgress;

    #endregion
}
