using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ UNDO TOAST VIEW MODEL (v0.4.3h)                                          │
// │ ViewModel for the Undo Toast notification with countdown.               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for the Undo Toast notification.
/// Displays after applying changes with an option to undo.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3h.</para>
/// </remarks>
public partial class UndoToastViewModel : ViewModelBase, IDisposable
{
    private readonly IUndoManager? _undoManager;
    private CancellationTokenSource? _autoHideCts;
    private CancellationTokenSource? _countdownCts;
    private Guid? _currentChangeId;
    private bool _disposed;

    /// <summary>Auto-hide delay (10 seconds).</summary>
    public const int AutoHideDelayMs = 10000;

    /// <summary>Countdown update interval (1 second).</summary>
    private const int CountdownUpdateIntervalMs = 1000;

    /// <summary>Threshold for "expiring soon" state (30 seconds).</summary>
    private static readonly TimeSpan ExpiringSoonThreshold = TimeSpan.FromSeconds(30);

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Gets whether the toast is visible.</summary>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>Gets the action message.</summary>
    [ObservableProperty]
    private string _message = string.Empty;

    /// <summary>Gets the full file path.</summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>Gets the file name.</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>Gets time remaining until undo expires.</summary>
    [ObservableProperty]
    private TimeSpan _timeRemaining;

    /// <summary>Gets whether undo is in progress.</summary>
    [ObservableProperty]
    private bool _isUndoing;

    /// <summary>Gets the change type.</summary>
    [ObservableProperty]
    private FileChangeType _changeType;

    /// <summary>Gets the total undo window.</summary>
    [ObservableProperty]
    private TimeSpan _totalUndoWindow;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Gets formatted time remaining (m:ss).</summary>
    public string FormattedTimeRemaining
    {
        get
        {
            if (TimeRemaining <= TimeSpan.Zero)
                return "0:00";

            if (TimeRemaining.TotalHours >= 1)
                return TimeRemaining.ToString(@"h\:mm\:ss");

            return TimeRemaining.ToString(@"m\:ss");
        }
    }

    /// <summary>Gets whether undo is expiring soon (under 30 seconds).</summary>
    public bool IsExpiringSoon => TimeRemaining > TimeSpan.Zero && TimeRemaining <= ExpiringSoonThreshold;

    /// <summary>Gets progress percentage (100% = full time).</summary>
    public double ProgressPercentage
    {
        get
        {
            if (TotalUndoWindow <= TimeSpan.Zero) return 0;
            var percentage = TimeRemaining.TotalMilliseconds / TotalUndoWindow.TotalMilliseconds * 100;
            return Math.Clamp(percentage, 0, 100);
        }
    }

    /// <summary>Gets whether undo can execute.</summary>
    public bool CanUndo => !IsUndoing && TimeRemaining > TimeSpan.Zero;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance for design-time or testing.
    /// </summary>
    public UndoToastViewModel()
    {
        // Wire up property change notifications
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TimeRemaining))
            {
                OnPropertyChanged(nameof(FormattedTimeRemaining));
                OnPropertyChanged(nameof(IsExpiringSoon));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(CanUndo));
            }
            else if (e.PropertyName == nameof(IsUndoing))
            {
                OnPropertyChanged(nameof(CanUndo));
            }
        };
    }

    /// <summary>
    /// Initializes a new instance with UndoManager.
    /// </summary>
    public UndoToastViewModel(IUndoManager undoManager) : this()
    {
        _undoManager = undoManager ?? throw new ArgumentNullException(nameof(undoManager));

        // Subscribe to undo manager events
        _undoManager.UndoAvailable += OnUndoAvailable;
        _undoManager.UndoExpired += OnUndoExpired;
        _undoManager.UndoCompleted += OnUndoCompleted;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════════════════

    private void OnUndoAvailable(object? sender, UndoAvailableEventArgs e)
    {
        _currentChangeId = e.ChangeId;
        FilePath = e.FilePath;
        FileName = Path.GetFileName(e.FilePath);
        ChangeType = e.ChangeType;
        TotalUndoWindow = e.UndoState.ExpiresAt - e.UndoState.CreatedAt;
        TimeRemaining = e.ExpiresAt - DateTime.UtcNow;

        Message = FormatMessage(e.ChangeType, FileName);
        Show();
    }

    private void OnUndoExpired(object? sender, UndoExpiredEventArgs e)
    {
        if (e.ChangeId == _currentChangeId)
            Hide();
    }

    private void OnUndoCompleted(object? sender, UndoCompletedEventArgs e)
    {
        if (e.ChangeId == _currentChangeId && e.Success)
        {
            Message = $"Restored {FileName}";
            _ = DelayedHideAsync(2000);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Shows the toast.</summary>
    public void Show()
    {
        CancelTimers();

        _autoHideCts = new CancellationTokenSource();
        _countdownCts = new CancellationTokenSource();

        IsVisible = true;

        StartAutoHide(_autoHideCts.Token);
        StartCountdownUpdates(_countdownCts.Token);
    }

    /// <summary>Shows the toast with specific values (for testing/manual use).</summary>
    public void Show(string filePath, FileChangeType changeType, TimeSpan undoWindow)
    {
        _currentChangeId = Guid.NewGuid();
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        ChangeType = changeType;
        TotalUndoWindow = undoWindow;
        TimeRemaining = undoWindow;
        Message = FormatMessage(changeType, FileName);

        Show();
    }

    /// <summary>Hides the toast.</summary>
    public void Hide()
    {
        CancelTimers();
        IsVisible = false;
        _currentChangeId = null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Executes the undo operation.</summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync()
    {
        if (IsUndoing || _currentChangeId == null || _undoManager == null) return;

        try
        {
            IsUndoing = true;
            UndoCommand.NotifyCanExecuteChanged();

            var success = await _undoManager.UndoByIdAsync(_currentChangeId.Value);

            if (!success)
            {
                Message = "Undo failed";
                await DelayedHideAsync(3000);
            }
        }
        finally
        {
            IsUndoing = false;
            UndoCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Dismisses the toast.</summary>
    [RelayCommand]
    private void Dismiss()
    {
        Hide();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════

    private async void StartAutoHide(CancellationToken ct)
    {
        try
        {
            await Task.Delay(AutoHideDelayMs, ct);
            Hide();
        }
        catch (OperationCanceledException) { }
    }

    private async void StartCountdownUpdates(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && IsVisible)
            {
                if (_currentChangeId.HasValue && _undoManager != null)
                {
                    var remaining = _undoManager.GetTimeRemainingById(_currentChangeId.Value);
                    TimeRemaining = remaining;

                    if (TimeRemaining <= TimeSpan.Zero)
                    {
                        Hide();
                        break;
                    }
                }
                else
                {
                    // Manual mode - decrement locally
                    TimeRemaining -= TimeSpan.FromMilliseconds(CountdownUpdateIntervalMs);
                    if (TimeRemaining <= TimeSpan.Zero)
                    {
                        Hide();
                        break;
                    }
                }

                await Task.Delay(CountdownUpdateIntervalMs, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task DelayedHideAsync(int delayMs)
    {
        try
        {
            await Task.Delay(delayMs);
            Hide();
        }
        catch (OperationCanceledException) { }
    }

    private static string FormatMessage(FileChangeType changeType, string fileName) => changeType switch
    {
        FileChangeType.Created => $"Created {fileName}",
        FileChangeType.Modified => $"Modified {fileName}",
        FileChangeType.Deleted => $"Deleted {fileName}",
        _ => $"Changed {fileName}"
    };

    private void CancelTimers()
    {
        _autoHideCts?.Cancel();
        _autoHideCts?.Dispose();
        _autoHideCts = null;

        _countdownCts?.Cancel();
        _countdownCts?.Dispose();
        _countdownCts = null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IDisposable
    // ═══════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;

        CancelTimers();

        if (_undoManager != null)
        {
            _undoManager.UndoAvailable -= OnUndoAvailable;
            _undoManager.UndoExpired -= OnUndoExpired;
            _undoManager.UndoCompleted -= OnUndoCompleted;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
