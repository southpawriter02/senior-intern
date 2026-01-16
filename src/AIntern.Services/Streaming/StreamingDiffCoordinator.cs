using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace AIntern.Services.Streaming;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STREAMING DIFF COORDINATOR (v0.4.5b)                                    │
// │ Coordinates diff computation during LLM response streaming.             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Coordinates streaming diff computation during LLM response generation.
/// </summary>
/// <remarks>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item>Debouncing: 250ms default before processing updates</item>
/// <item>Content hashing: Detects actual content changes</item>
/// <item>Concurrent state management: Thread-safe tracking</item>
/// <item>Settings-aware: Respects ShowDiffPreviewDuringStreaming</item>
/// </list>
/// </para>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public sealed class StreamingDiffCoordinator : IStreamingDiffCoordinator, IDisposable
{
    private readonly IDiffService _diffService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<StreamingDiffCoordinator> _logger;
    private readonly ConcurrentDictionary<Guid, DiffComputationState> _states = new();
    private readonly DiffComputationQueue _queue;
    private readonly CancellationTokenSource _disposalCts = new();
    private bool _disposed;

    /// <summary>
    /// Default debounce interval for update coalescing.
    /// </summary>
    public static readonly TimeSpan DefaultDebounceInterval = TimeSpan.FromMilliseconds(250);

    /// <inheritdoc/>
    public event EventHandler<DiffComputedEventArgs>? DiffComputed;

    /// <summary>
    /// Creates a new streaming diff coordinator.
    /// </summary>
    public StreamingDiffCoordinator(
        IDiffService diffService,
        ISettingsService settingsService,
        ILogger<StreamingDiffCoordinator> logger)
    {
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _queue = new DiffComputationQueue(
            DefaultDebounceInterval,
            ProcessComputationAsync,
            _logger);

        // Start processing queue
        _ = _queue.StartProcessingAsync(_disposalCts.Token);

        _logger.LogDebug("StreamingDiffCoordinator initialized");
    }

    /// <inheritdoc/>
    public async Task<DiffComputationState> OnCodeBlockDetectedAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        // Check if streaming diff preview is enabled
        var settings = _settingsService.CurrentSettings;
        if (!settings.ShowDiffPreviewDuringStreaming)
        {
            _logger.LogDebug("Streaming diff preview disabled, skipping block {BlockId}", block.Id);
            return CreateDisabledState(block);
        }

        // Skip blocks without target file paths
        if (string.IsNullOrEmpty(block.TargetFilePath))
        {
            _logger.LogDebug("Block {BlockId} has no target file path, skipping", block.Id);
            return CreateNoTargetState(block);
        }

        // Create or get existing state
        var state = _states.GetOrAdd(block.Id, _ => new DiffComputationState
        {
            BlockId = block.Id,
            TargetFilePath = block.TargetFilePath,
            StartedAt = DateTime.UtcNow
        });

        // Queue computation
        _queue.Enqueue(new DiffComputationRequest(
            block,
            workspacePath,
            DiffComputationPriority.Detect,
            cancellationToken));

        _logger.LogDebug("Queued initial diff computation for block {BlockId}", block.Id);
        return state;
    }

    /// <inheritdoc/>
    public async Task<DiffComputationState> OnCodeBlockUpdatedAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (!_states.TryGetValue(block.Id, out var state))
        {
            // First time seeing this block, treat as detection
            return await OnCodeBlockDetectedAsync(block, workspacePath, cancellationToken);
        }

        // Check if content actually changed
        var newHash = ComputeContentHash(block.Content);
        if (state.ContentHash == newHash)
        {
            // Content unchanged, return cached state
            _logger.LogDebug("Block {BlockId} content unchanged, using cached state", block.Id);
            return state;
        }

        // Mark as needing recompute
        state.NeedsRecompute = true;

        // Queue update (will be debounced/coalesced)
        _queue.Enqueue(new DiffComputationRequest(
            block,
            workspacePath,
            DiffComputationPriority.Update,
            cancellationToken));

        _logger.LogDebug("Queued update diff computation for block {BlockId}", block.Id);
        return state;
    }

    /// <inheritdoc/>
    public async Task<DiffComputationState> FinalizeBlockDiffAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (!_states.TryGetValue(block.Id, out var state))
        {
            // No existing state, compute fresh
            state = new DiffComputationState
            {
                BlockId = block.Id,
                TargetFilePath = block.TargetFilePath,
                StartedAt = DateTime.UtcNow
            };
            _states[block.Id] = state;
        }

        state.IsFinalized = true;

        // Force immediate computation (bypass debounce)
        _logger.LogDebug("Finalizing diff for block {BlockId}", block.Id);
        await ProcessComputationAsync(new DiffComputationRequest(
            block,
            workspacePath,
            DiffComputationPriority.Finalize,
            cancellationToken));

        return state;
    }

    /// <inheritdoc/>
    public DiffComputationState? GetComputationState(Guid blockId) =>
        _states.TryGetValue(blockId, out var state) ? state : null;

    /// <inheritdoc/>
    public IReadOnlyCollection<DiffComputationState> GetAllStates() =>
        _states.Values.ToList().AsReadOnly();

    /// <inheritdoc/>
    public void Cancel(Guid blockId)
    {
        if (_states.TryGetValue(blockId, out var state))
        {
            state.Status = DiffComputationStatus.Cancelled;
            _logger.LogDebug("Cancelled diff computation for block {BlockId}", blockId);
        }
    }

    /// <inheritdoc/>
    public void CancelAll()
    {
        foreach (var state in _states.Values)
        {
            state.Status = DiffComputationStatus.Cancelled;
        }
        _queue.Clear();
        _logger.LogDebug("Cancelled all diff computations");
    }

    /// <inheritdoc/>
    public void Reset()
    {
        CancelAll();
        _states.Clear();
        _logger.LogDebug("Reset streaming diff coordinator");
    }

    private async Task ProcessComputationAsync(DiffComputationRequest request)
    {
        var block = request.Block;

        if (!_states.TryGetValue(block.Id, out var state))
        {
            _logger.LogWarning("No state found for block {BlockId}", block.Id);
            return;
        }

        // Skip if already cancelled
        if (state.Status == DiffComputationStatus.Cancelled)
        {
            _logger.LogDebug("Skipping cancelled block {BlockId}", block.Id);
            return;
        }

        // Skip if no target file path
        if (string.IsNullOrEmpty(block.TargetFilePath))
        {
            _logger.LogDebug("Skipping block {BlockId} with no target path", block.Id);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        state.Status = DiffComputationStatus.Computing;

        try
        {
            var fullPath = Path.Combine(request.WorkspacePath, block.TargetFilePath);
            DiffResult diff;

            if (!File.Exists(fullPath))
            {
                // New file - all content is additions
                state.IsNewFile = true;
                diff = CreateNewFileDiff(block.TargetFilePath, block.Content);
                _logger.LogDebug("Creating new file diff for {FilePath}", block.TargetFilePath);
            }
            else
            {
                // Existing file - compute actual diff
                var originalContent = await File.ReadAllTextAsync(
                    fullPath,
                    request.CancellationToken);

                diff = _diffService.ComputeDiff(
                    originalContent,
                    block.Content,
                    block.TargetFilePath);

                _logger.LogDebug("Computed diff for existing file {FilePath}", block.TargetFilePath);
            }

            stopwatch.Stop();

            // Update state
            state.Result = diff;
            state.ContentHash = ComputeContentHash(block.Content);
            state.LastUpdatedAt = DateTime.UtcNow;
            state.ComputationDuration = stopwatch.Elapsed;
            state.ComputationCount++;
            state.NeedsRecompute = false;
            state.Status = DiffComputationStatus.Completed;

            _logger.LogDebug(
                "Completed diff for block {BlockId}: {Stats} in {Duration}ms",
                block.Id,
                state.StatsDisplay,
                stopwatch.ElapsedMilliseconds);

            // Raise event
            var eventArgs = new DiffComputedEventArgs(
                block.Id,
                state,
                isIntermediate: !state.IsFinalized);

            DiffComputed?.Invoke(this, eventArgs);
        }
        catch (OperationCanceledException)
        {
            state.Status = DiffComputationStatus.Cancelled;
            _logger.LogDebug("Diff computation cancelled for block {BlockId}", block.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            state.Status = DiffComputationStatus.Failed;
            state.ErrorMessage = ex.Message;
            state.ComputationDuration = stopwatch.Elapsed;

            _logger.LogError(ex, "Diff computation failed for block {BlockId}", block.Id);

            // Still raise event for failure
            var eventArgs = new DiffComputedEventArgs(
                block.Id,
                state,
                isIntermediate: !state.IsFinalized);

            DiffComputed?.Invoke(this, eventArgs);
        }
    }

    private DiffResult CreateNewFileDiff(string filePath, string content)
    {
        var lineCount = content.Split('\n').Length;
        return new DiffResult
        {
            OriginalFilePath = filePath,
            IsNewFile = true,
            Stats = new DiffStats
            {
                AddedLines = lineCount,
                RemovedLines = 0,
                ModifiedLines = 0
            },
            Hunks = []
        };
    }

    private static DiffComputationState CreateDisabledState(CodeBlock block) => new()
    {
        BlockId = block.Id,
        Status = DiffComputationStatus.Cancelled,
        ErrorMessage = "Streaming diff preview disabled in settings"
    };

    private static DiffComputationState CreateNoTargetState(CodeBlock block) => new()
    {
        BlockId = block.Id,
        Status = DiffComputationStatus.Completed,
        ErrorMessage = "No target file path specified"
    };

    private static string ComputeContentHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _disposalCts.Cancel();
        _queue.Dispose();
        _disposalCts.Dispose();

        _logger.LogDebug("StreamingDiffCoordinator disposed");
    }
}
