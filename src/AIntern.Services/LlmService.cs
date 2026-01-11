using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Sampling;
using SeniorIntern.Core.Events;
using SeniorIntern.Core.Exceptions;
using SeniorIntern.Core.Interfaces;
using SeniorIntern.Core.Models;

namespace SeniorIntern.Services;

public sealed class LlmService : ILlmService
{
    private LLamaWeights? _model;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly SemaphoreSlim _inferenceLock = new(1, 1);
    private CancellationTokenSource? _currentInferenceCts;

    public bool IsModelLoaded => _model is not null;
    public string? CurrentModelPath { get; private set; }
    public string? CurrentModelName => CurrentModelPath is null ? null : Path.GetFileName(CurrentModelPath);

    public event EventHandler<ModelStateChangedEventArgs>? ModelStateChanged;
    public event EventHandler<InferenceProgressEventArgs>? InferenceProgress;

    public async Task LoadModelAsync(
        string modelPath,
        ModelLoadOptions options,
        IProgress<ModelLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(modelPath))
        {
            throw new ModelLoadException($"Model file not found: {modelPath}", modelPath);
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Unload existing model first
            await UnloadModelCoreAsync();

            progress?.Report(new ModelLoadProgress("Initializing", 0, "Preparing to load model..."));

            var modelParams = new ModelParams(modelPath)
            {
                ContextSize = options.ContextSize,
                GpuLayerCount = options.GpuLayerCount == -1
                    ? DetectOptimalGpuLayers()
                    : options.GpuLayerCount,
                BatchSize = options.BatchSize
            };

            progress?.Report(new ModelLoadProgress("Loading", 25, "Loading model weights..."));

            // Load on background thread to avoid blocking UI
            _model = await Task.Run(
                () => LLamaWeights.LoadFromFile(modelParams),
                cancellationToken);

            progress?.Report(new ModelLoadProgress("Context", 75, "Creating context..."));

            _context = _model.CreateContext(modelParams);
            _executor = new InteractiveExecutor(_context);
            CurrentModelPath = modelPath;

            progress?.Report(new ModelLoadProgress("Complete", 100, "Model loaded successfully"));

            ModelStateChanged?.Invoke(this, new ModelStateChangedEventArgs(true, modelPath));
        }
        catch (OperationCanceledException)
        {
            await UnloadModelCoreAsync();
            throw;
        }
        catch (Exception ex) when (ex is not ModelLoadException)
        {
            await UnloadModelCoreAsync();
            throw new ModelLoadException($"Failed to load model: {ex.Message}", modelPath, ex);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task UnloadModelAsync()
    {
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

    private Task UnloadModelCoreAsync()
    {
        // Cancel any ongoing inference
        _currentInferenceCts?.Cancel();

        _executor = null;

        _context?.Dispose();
        _context = null;

        _model?.Dispose();
        _model = null;

        var previousPath = CurrentModelPath;
        CurrentModelPath = null;

        if (previousPath is not null)
        {
            ModelStateChanged?.Invoke(this, new ModelStateChangedEventArgs(false, null));
        }

        // Force garbage collection to free VRAM
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        IEnumerable<ChatMessage> conversation,
        InferenceOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_executor is null)
        {
            throw new InferenceException("No model is loaded. Please load a model first.");
        }

        await _inferenceLock.WaitAsync(cancellationToken);

        // Create linked cancellation token for this inference
        _currentInferenceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var inferenceToken = _currentInferenceCts.Token;

        var stopwatch = Stopwatch.StartNew();
        var tokenCount = 0;

        try
        {
            var prompt = FormatConversation(conversation);

            var inferenceParams = new InferenceParams
            {
                MaxTokens = options.MaxTokens,
                AntiPrompts = options.StopSequences?.ToList() ?? ["User:", "\n\nUser:", "\nUser:"],
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = options.Temperature,
                    TopP = options.TopP
                }
            };

            await foreach (var token in _executor.InferAsync(prompt, inferenceParams, inferenceToken))
            {
                tokenCount++;
                yield return token;

                // Report progress periodically
                if (tokenCount % 10 == 0)
                {
                    InferenceProgress?.Invoke(this, new InferenceProgressEventArgs
                    {
                        TokensGenerated = tokenCount,
                        Elapsed = stopwatch.Elapsed
                    });
                }
            }

            // Final progress report
            InferenceProgress?.Invoke(this, new InferenceProgressEventArgs
            {
                TokensGenerated = tokenCount,
                Elapsed = stopwatch.Elapsed
            });
        }
        finally
        {
            stopwatch.Stop();
            _currentInferenceCts?.Dispose();
            _currentInferenceCts = null;
            _inferenceLock.Release();
        }
    }

    public void CancelCurrentInference()
    {
        _currentInferenceCts?.Cancel();
    }

    private static string FormatConversation(IEnumerable<ChatMessage> messages)
    {
        var sb = new StringBuilder();

        foreach (var msg in messages)
        {
            var role = msg.Role switch
            {
                MessageRole.System => "System",
                MessageRole.User => "User",
                MessageRole.Assistant => "Assistant",
                _ => "User"
            };

            sb.AppendLine($"{role}: {msg.Content}");
            sb.AppendLine();
        }

        sb.Append("Assistant: ");
        return sb.ToString();
    }

    private static int DetectOptimalGpuLayers()
    {
        // On macOS with Metal, use all layers
        if (OperatingSystem.IsMacOS())
        {
            return 999; // LLamaSharp caps this automatically
        }

        // On Windows/Linux, start conservative (CPU-focused for now)
        // Users can adjust this in settings for CUDA support
        return 0;
    }

    public async ValueTask DisposeAsync()
    {
        await UnloadModelAsync();
        _loadLock.Dispose();
        _inferenceLock.Dispose();
    }
}
