using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Sampling;
using AIntern.Core.Events;
using AIntern.Core.Exceptions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

/// <summary>
/// Provides LLM inference capabilities using LLamaSharp.
/// Supports loading GGUF models, streaming text generation, and resource management.
/// </summary>
/// <remarks>
/// <para>
/// This service is thread-safe for concurrent operations. Model loading and inference
/// are protected by separate semaphores to allow status queries during inference.
/// </para>
/// <para>
/// On macOS with Metal support, GPU acceleration is automatically enabled.
/// On other platforms, CPU inference is used by default but can be configured
/// via <see cref="ModelLoadOptions.GpuLayerCount"/>.
/// </para>
/// <para>
/// <b>v0.2.3e:</b> Integrates with <see cref="IInferenceSettingsService"/> to use
/// user-configured inference parameters (Temperature, TopP, TopK, RepetitionPenalty, etc.).
/// </para>
/// </remarks>
public sealed class LlmService : ILlmService
{
    #region Fields

    // LLamaSharp components for model inference
    private LLamaWeights? _model;           // Loaded model weights (VRAM/RAM)
    private LLamaContext? _context;         // Model context for inference
    private InteractiveExecutor? _executor; // Handles the actual text generation

    // Thread synchronization locks
    private readonly SemaphoreSlim _loadLock = new(1, 1);      // Protects load/unload operations
    private readonly SemaphoreSlim _inferenceLock = new(1, 1); // Protects inference operations

    // Allows cancellation of the current inference operation
    private CancellationTokenSource? _currentInferenceCts;

    // Service dependencies (v0.2.3e)
    private readonly IInferenceSettingsService? _inferenceSettings;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmService"/> class.
    /// </summary>
    /// <remarks>
    /// Default constructor for backward compatibility and simpler DI scenarios.
    /// </remarks>
    public LlmService()
    {
        _inferenceSettings = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmService"/> class with inference settings.
    /// </summary>
    /// <param name="inferenceSettings">The inference settings service for user-configured parameters.</param>
    /// <remarks>
    /// <para>
    /// Added in v0.2.3e to support user-configured inference parameters from the settings panel.
    /// When provided, the service uses <see cref="IInferenceSettingsService.CurrentSettings"/>
    /// for inference parameters instead of hardcoded defaults.
    /// </para>
    /// </remarks>
    public LlmService(IInferenceSettingsService inferenceSettings)
    {
        _inferenceSettings = inferenceSettings ?? throw new ArgumentNullException(nameof(inferenceSettings));
    }

    #endregion

    #region Properties

    /// <inheritdoc />
    public bool IsModelLoaded => _model is not null;

    /// <inheritdoc />
    public string? CurrentModelPath { get; private set; }

    /// <inheritdoc />
    public string? CurrentModelName => CurrentModelPath is null ? null : Path.GetFileName(CurrentModelPath);

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<ModelStateChangedEventArgs>? ModelStateChanged;

    /// <inheritdoc />
    public event EventHandler<InferenceProgressEventArgs>? InferenceProgress;

    #endregion

    #region Model Management

    /// <inheritdoc />
    /// <exception cref="ModelLoadException">Thrown when the model file is not found or fails to load.</exception>
    public async Task LoadModelAsync(
        string modelPath,
        ModelLoadOptions options,
        IProgress<ModelLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Validate that the model file exists before attempting load
        if (!File.Exists(modelPath))
        {
            throw new ModelLoadException($"Model file not found: {modelPath}", modelPath);
        }

        // Acquire the load lock to prevent concurrent load operations
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Unload any existing model first to free resources
            await UnloadModelCoreAsync();

            progress?.Report(new ModelLoadProgress("Initializing", 0, "Preparing to load model..."));

            // Build model parameters from options
            var modelParams = new ModelParams(modelPath)
            {
                ContextSize = options.ContextSize,  // Token context window size
                GpuLayerCount = options.GpuLayerCount == -1
                    ? DetectOptimalGpuLayers()       // Auto-detect GPU support
                    : options.GpuLayerCount,         // Use specified layer count
                BatchSize = options.BatchSize        // Processing batch size
            };

            progress?.Report(new ModelLoadProgress("Loading", 25, "Loading model weights..."));

            // Load the model on a background thread to avoid blocking UI
            // This is the most time-consuming operation (can take 10-30+ seconds)
            _model = await Task.Run(
                () => LLamaWeights.LoadFromFile(modelParams),
                cancellationToken);

            progress?.Report(new ModelLoadProgress("Context", 75, "Creating context..."));

            // Create the context and executor for inference
            _context = _model.CreateContext(modelParams);
            _executor = new InteractiveExecutor(_context);
            CurrentModelPath = modelPath;

            progress?.Report(new ModelLoadProgress("Complete", 100, "Model loaded successfully"));

            // Notify subscribers that a model is now loaded
            ModelStateChanged?.Invoke(this, new ModelStateChangedEventArgs(true, modelPath));
        }
        catch (OperationCanceledException)
        {
            // User cancelled - clean up partial load and re-throw
            await UnloadModelCoreAsync();
            throw;
        }
        catch (Exception ex) when (ex is not ModelLoadException)
        {
            // Wrap unexpected errors in ModelLoadException
            await UnloadModelCoreAsync();
            throw new ModelLoadException($"Failed to load model: {ex.Message}", modelPath, ex);
        }
        finally
        {
            // Always release the lock
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UnloadModelAsync()
    {
        // Acquire lock for thread-safe unload
        await _loadLock.WaitAsync();
        try
        {
            await UnloadModelCoreAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Internal method to unload the model without acquiring the lock.
    /// Cancels any ongoing inference and releases all resources.
    /// </summary>
    private Task UnloadModelCoreAsync()
    {
        // Signal any ongoing inference to stop
        _currentInferenceCts?.Cancel();

        // Clear executor first (depends on context)
        _executor = null;

        // Dispose context (releases context memory)
        _context?.Dispose();
        _context = null;

        // Dispose model (releases VRAM/RAM for weights)
        _model?.Dispose();
        _model = null;

        // Track previous path for event notification
        var previousPath = CurrentModelPath;
        CurrentModelPath = null;

        // Only fire event if we actually had a model loaded
        if (previousPath is not null)
        {
            ModelStateChanged?.Invoke(this, new ModelStateChangedEventArgs(false, null));
        }

        // Force garbage collection to release VRAM immediately
        // This is important for GPU memory management
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return Task.CompletedTask;
    }

    #endregion

    #region Inference

    /// <inheritdoc />
    /// <exception cref="InferenceException">Thrown when no model is loaded.</exception>
    /// <remarks>
    /// <para>
    /// <b>v0.2.3e Parameter Resolution:</b> Parameters are resolved in the following order:
    /// <list type="number">
    ///   <item>Use <paramref name="options"/> values if explicitly provided</item>
    ///   <item>Fall back to <see cref="IInferenceSettingsService.CurrentSettings"/> if service is available</item>
    ///   <item>Use hardcoded defaults as last resort</item>
    /// </list>
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        IEnumerable<ChatMessage> conversation,
        InferenceOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Guard: require a loaded model
        if (_executor is null)
        {
            throw new InferenceException("No model is loaded. Please load a model first.");
        }

        // Acquire inference lock (only one inference at a time)
        await _inferenceLock.WaitAsync(cancellationToken);

        // Create a linked cancellation token that can be cancelled externally
        _currentInferenceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var inferenceToken = _currentInferenceCts.Token;

        // Track timing for tokens-per-second calculation
        var stopwatch = Stopwatch.StartNew();
        var tokenCount = 0;

        try
        {
            // Format the conversation into a prompt string
            var prompt = FormatConversation(conversation);

            // v0.2.3e: Resolve inference parameters from settings service or options
            // Use inference settings service if available, otherwise fall back to options/defaults
            var settings = _inferenceSettings?.CurrentSettings;

            // Build inference parameters, preferring service settings when available
            var inferenceParams = new InferenceParams
            {
                // MaxTokens: options override > service settings > hardcoded default
                MaxTokens = options.MaxTokens > 0
                    ? options.MaxTokens
                    : settings?.MaxTokens ?? 2048,
                AntiPrompts = options.StopSequences?.ToList()
                    ?? ["User:", "\n\nUser:", "\nUser:"],  // Stop when user turn detected
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    // Temperature: options override > service settings > hardcoded default
                    Temperature = options.Temperature > 0
                        ? options.Temperature
                        : settings?.Temperature ?? 0.7f,
                    // TopP: options override > service settings > hardcoded default
                    TopP = options.TopP > 0
                        ? options.TopP
                        : settings?.TopP ?? 0.9f,
                    // TopK and RepetitionPenalty from settings only (v0.2.3e)
                    TopK = settings?.TopK ?? 40,
                    RepeatPenalty = settings?.RepetitionPenalty ?? 1.1f
                }
            };

            // Stream tokens from the model
            await foreach (var token in _executor.InferAsync(prompt, inferenceParams, inferenceToken))
            {
                tokenCount++;
                yield return token;  // Yield each token to the caller immediately

                // Report progress every 10 tokens to avoid event spam
                if (tokenCount % 10 == 0)
                {
                    InferenceProgress?.Invoke(this, new InferenceProgressEventArgs
                    {
                        TokensGenerated = tokenCount,
                        Elapsed = stopwatch.Elapsed
                    });
                }
            }

            // Final progress report with complete stats
            InferenceProgress?.Invoke(this, new InferenceProgressEventArgs
            {
                TokensGenerated = tokenCount,
                Elapsed = stopwatch.Elapsed
            });
        }
        finally
        {
            // Always clean up resources
            stopwatch.Stop();
            _currentInferenceCts?.Dispose();
            _currentInferenceCts = null;
            _inferenceLock.Release();
        }
    }

    /// <inheritdoc />
    public void CancelCurrentInference()
    {
        // Signal the current inference to stop via cancellation token
        _currentInferenceCts?.Cancel();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Formats a conversation into a prompt string suitable for the model.
    /// Uses a simple "Role: Content" format with newlines between messages.
    /// </summary>
    /// <param name="messages">The conversation messages to format.</param>
    /// <returns>A formatted prompt string ending with "Assistant: ".</returns>
    private static string FormatConversation(IEnumerable<ChatMessage> messages)
    {
        var sb = new StringBuilder();

        // Format each message as "Role: Content\n\n"
        foreach (var msg in messages)
        {
            // Map enum to display name
            var role = msg.Role switch
            {
                MessageRole.System => "System",
                MessageRole.User => "User",
                MessageRole.Assistant => "Assistant",
                _ => "User"  // Default fallback
            };

            sb.AppendLine($"{role}: {msg.Content}");
            sb.AppendLine();  // Blank line between messages
        }

        // End with "Assistant: " to prompt the model to respond
        sb.Append("Assistant: ");
        return sb.ToString();
    }

    /// <summary>
    /// Detects the optimal number of GPU layers based on the current platform.
    /// </summary>
    /// <returns>
    /// 999 on macOS (Metal support), 0 on other platforms (CPU-only default).
    /// </returns>
    private static int DetectOptimalGpuLayers()
    {
        // macOS has built-in Metal support in LLamaSharp
        if (OperatingSystem.IsMacOS())
        {
            return 999; // LLamaSharp automatically caps this to available layers
        }

        // Windows/Linux: default to CPU for safety
        // Users can configure CUDA manually in settings if they have compatible GPU
        return 0;
    }

    #endregion

    #region IAsyncDisposable

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Release model resources
        await UnloadModelAsync();
        
        // Dispose synchronization primitives
        _loadLock.Dispose();
        _inferenceLock.Dispose();
    }

    #endregion
}
