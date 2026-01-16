using AIntern.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AIntern.Services.Streaming;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF COMPUTATION QUEUE (v0.4.5b)                                        │
// │ Manages debounced, prioritized diff computation requests.               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Priority for diff computation requests.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public enum DiffComputationPriority
{
    /// <summary>
    /// Initial detection (lowest priority).
    /// </summary>
    Detect = 1,

    /// <summary>
    /// Content update (medium priority).
    /// </summary>
    Update = 2,

    /// <summary>
    /// Finalization (highest priority, bypasses debounce).
    /// </summary>
    Finalize = 3
}

/// <summary>
/// Request to compute a diff for a code block.
/// </summary>
/// <param name="Block">The code block to diff.</param>
/// <param name="WorkspacePath">Path to the workspace root.</param>
/// <param name="Priority">Computation priority.</param>
/// <param name="CancellationToken">Cancellation token.</param>
/// <remarks>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public sealed record DiffComputationRequest(
    CodeBlock Block,
    string WorkspacePath,
    DiffComputationPriority Priority,
    CancellationToken CancellationToken);

/// <summary>
/// Manages debounced, prioritized diff computation requests.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5b.</para>
/// </remarks>
public sealed class DiffComputationQueue : IDisposable
{
    private readonly TimeSpan _debounceInterval;
    private readonly Func<DiffComputationRequest, Task> _processor;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Guid, PendingComputation> _pending = new();
    private readonly Channel<DiffComputationRequest> _channel;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new diff computation queue.
    /// </summary>
    /// <param name="debounceInterval">Time to wait before processing updates.</param>
    /// <param name="processor">Function to process requests.</param>
    /// <param name="logger">Logger instance.</param>
    public DiffComputationQueue(
        TimeSpan debounceInterval,
        Func<DiffComputationRequest, Task> processor,
        ILogger logger)
    {
        _debounceInterval = debounceInterval;
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = Channel.CreateUnbounded<DiffComputationRequest>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    }

    /// <summary>
    /// Enqueue a computation request.
    /// Requests for the same block are coalesced with debouncing.
    /// </summary>
    public void Enqueue(DiffComputationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blockId = request.Block.Id;

        _logger.LogDebug(
            "Enqueueing diff request for block {BlockId} with priority {Priority}",
            blockId, request.Priority);

        // Finalize requests bypass debouncing
        if (request.Priority == DiffComputationPriority.Finalize)
        {
            // Cancel any pending debounced request
            if (_pending.TryRemove(blockId, out var pending))
            {
                pending.Cancel();
            }

            // Process immediately
            _channel.Writer.TryWrite(request);
            return;
        }

        // Debounce detection and update requests
        _pending.AddOrUpdate(
            blockId,
            _ => new PendingComputation(request, _debounceInterval, () => FlushPending(blockId)),
            (_, existing) =>
            {
                existing.Update(request);
                return existing;
            });
    }

    /// <summary>
    /// Start processing the queue.
    /// </summary>
    public async Task StartProcessingAsync(CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cts.Token);

        _logger.LogDebug("Starting diff computation queue processing");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(linked.Token))
            {
                try
                {
                    await _processor(request);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing diff computation for block {BlockId}",
                        request.Block.Id);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Diff computation queue processing cancelled");
        }
    }

    /// <summary>
    /// Clear all pending computations.
    /// </summary>
    public void Clear()
    {
        foreach (var pending in _pending.Values)
        {
            pending.Cancel();
        }
        _pending.Clear();
        _logger.LogDebug("Cleared all pending diff computations");
    }

    private void FlushPending(Guid blockId)
    {
        if (_pending.TryRemove(blockId, out var pending))
        {
            var request = pending.GetLatestRequest();
            if (request is not null)
            {
                _channel.Writer.TryWrite(request);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        Clear();
        _channel.Writer.Complete();
        _cts.Dispose();
    }

    /// <summary>
    /// Tracks a pending debounced computation.
    /// </summary>
    private sealed class PendingComputation : IDisposable
    {
        private DiffComputationRequest _latestRequest;
        private readonly TimeSpan _debounceInterval;
        private readonly Action _onDebounceExpired;
        private CancellationTokenSource? _debounceCts;
        private readonly object _lock = new();

        public PendingComputation(
            DiffComputationRequest request,
            TimeSpan debounceInterval,
            Action onDebounceExpired)
        {
            _latestRequest = request;
            _debounceInterval = debounceInterval;
            _onDebounceExpired = onDebounceExpired;
            StartDebounceTimer();
        }

        public void Update(DiffComputationRequest request)
        {
            lock (_lock)
            {
                // Keep higher priority request
                if (request.Priority >= _latestRequest.Priority)
                {
                    _latestRequest = request;
                }
                // Reset debounce timer
                StartDebounceTimer();
            }
        }

        public DiffComputationRequest? GetLatestRequest()
        {
            lock (_lock)
            {
                return _latestRequest;
            }
        }

        public void Cancel()
        {
            lock (_lock)
            {
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();
                _debounceCts = null;
            }
        }

        private void StartDebounceTimer()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();

            var token = _debounceCts.Token;
            _ = Task.Delay(_debounceInterval, token).ContinueWith(
                _ => _onDebounceExpired(),
                token,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public void Dispose()
        {
            Cancel();
        }
    }
}
