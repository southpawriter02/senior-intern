using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ EDITOR REFRESH SERVICE (v0.4.3i)                                         │
// │ Coordinates editor refresh when files change.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Coordinates editor refresh when files change.
/// Listens to file change events and notifies the editor to refresh.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3i.</para>
/// </remarks>
public sealed class EditorRefreshService : IEditorRefreshService
{
    private readonly IFileChangeService? _changeService;
    private readonly ILogger<EditorRefreshService>? _logger;

    private readonly ConcurrentQueue<EditorRefreshEventArgs> _pendingRefreshes = new();
    private int _suspendCount;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<EditorRefreshEventArgs>? RefreshRequested;

    /// <inheritdoc/>
    public bool IsSuspended => _suspendCount > 0;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructors
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance for testing without dependencies.
    /// </summary>
    public EditorRefreshService()
    {
    }

    /// <summary>
    /// Initializes a new instance with optional logger.
    /// </summary>
    public EditorRefreshService(ILogger<EditorRefreshService>? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance with file change service integration.
    /// </summary>
    public EditorRefreshService(
        IFileChangeService changeService,
        ILogger<EditorRefreshService>? logger = null)
    {
        _changeService = changeService ?? throw new ArgumentNullException(nameof(changeService));
        _logger = logger;

        // Subscribe to file change events
        _changeService.FileChanged += OnFileChanged;
        _changeService.ChangeUndone += OnChangeUndone;

        _logger?.LogInformation("EditorRefreshService initialized with FileChangeService integration");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════════════════

    private void OnFileChanged(object? sender, FileChangedEventArgs e)
    {
        var result = e.Result;
        _logger?.LogDebug(
            "File changed, requesting editor refresh: {FilePath} (Type: {ResultType})",
            result.FilePath,
            result.ResultType);

        var args = new EditorRefreshEventArgs
        {
            FilePath = result.FilePath,
            RelativePath = result.RelativePath,
            Reason = MapResultTypeToReason(result.ResultType),
            ChangeId = result.ChangeRecordId,
            IsUserInitiated = true
        };

        RaiseOrQueueRefresh(args);
    }

    private void OnChangeUndone(object? sender, FileChangeUndoneEventArgs e)
    {
        var record = e.ChangeRecord;
        _logger?.LogDebug(
            "Change undone, requesting editor refresh: {FilePath}",
            record.FilePath);

        var args = new EditorRefreshEventArgs
        {
            FilePath = record.FilePath,
            RelativePath = record.RelativePath,
            Reason = RefreshReason.Undone,
            ChangeId = record.Id,
            IsUserInitiated = true
        };

        RaiseOrQueueRefresh(args);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public void RequestRefresh(string filePath, RefreshReason reason, string? newContent = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path required", nameof(filePath));

        var args = new EditorRefreshEventArgs
        {
            FilePath = filePath,
            Reason = reason,
            NewContent = newContent,
            IsUserInitiated = false
        };

        _logger?.LogDebug(
            "Manual refresh requested: {FilePath} (Reason: {Reason})",
            filePath,
            reason);

        RaiseOrQueueRefresh(args);
    }

    /// <inheritdoc/>
    public IDisposable SuspendNotifications()
    {
        Interlocked.Increment(ref _suspendCount);
        _logger?.LogDebug("Notifications suspended (count: {Count})", _suspendCount);

        return new SuspensionScope(this);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════

    private void RaiseOrQueueRefresh(EditorRefreshEventArgs args)
    {
        if (IsSuspended)
        {
            _pendingRefreshes.Enqueue(args);
            _logger?.LogDebug("Refresh queued (suspended): {FilePath}", args.FilePath);
            return;
        }

        RefreshRequested?.Invoke(this, args);
    }

    private void ResumeNotifications()
    {
        var newCount = Interlocked.Decrement(ref _suspendCount);
        _logger?.LogDebug("Notifications resumed (count: {Count})", newCount);

        if (newCount > 0) return;

        // Process pending refreshes, coalescing duplicates
        var processed = new Dictionary<string, EditorRefreshEventArgs>(StringComparer.OrdinalIgnoreCase);

        while (_pendingRefreshes.TryDequeue(out var args))
        {
            // Keep last refresh for each file
            processed[args.FilePath] = args;
        }

        // Raise events for unique files
        foreach (var args in processed.Values)
        {
            RefreshRequested?.Invoke(this, args);
        }

        _logger?.LogDebug("Processed {Count} pending refresh requests", processed.Count);
    }

    private static RefreshReason MapResultTypeToReason(ApplyResultType resultType) => resultType switch
    {
        ApplyResultType.Created => RefreshReason.FileCreated,
        ApplyResultType.Modified => RefreshReason.FileModified,
        ApplyResultType.Success => RefreshReason.FileModified,
        _ => RefreshReason.FileModified
    };

    // ═══════════════════════════════════════════════════════════════════════
    // IDisposable
    // ═══════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;

        if (_changeService != null)
        {
            _changeService.FileChanged -= OnFileChanged;
            _changeService.ChangeUndone -= OnChangeUndone;
        }

        _disposed = true;
        _logger?.LogInformation("EditorRefreshService disposed");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Suspension Scope
    // ═══════════════════════════════════════════════════════════════════════

    private sealed class SuspensionScope : IDisposable
    {
        private readonly EditorRefreshService _service;
        private bool _disposed;

        public SuspensionScope(EditorRefreshService service) => _service = service;

        public void Dispose()
        {
            if (_disposed) return;
            _service.ResumeNotifications();
            _disposed = true;
        }
    }
}

