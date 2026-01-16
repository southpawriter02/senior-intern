using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.Models;
using AIntern.Desktop.ViewModels;
using AIntern.Desktop.Views;

namespace AIntern.Desktop.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHAT PROPOSAL COORDINATOR (v0.4.4h)                                     │
// │ Coordinates multi-file proposal workflows with chat context.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Coordinates multi-file proposal workflows within the chat context.
/// </summary>
/// <remarks>
/// <para>
/// Manages:
/// <list type="bullet">
/// <item>Preview dialog display</item>
/// <item>Apply workflow with progress overlay</item>
/// <item>Undo operations</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4h.</para>
/// </remarks>
public class ChatProposalCoordinator : IDisposable
{
    private readonly IFileTreeProposalService _proposalService;
    private readonly IDiffService _diffService;
    private readonly IInlineDiffService _inlineDiffService;
    private readonly ApplyProgressViewModel _progressViewModel;
    private readonly ILogger<ChatProposalCoordinator> _logger;

    private CancellationTokenSource? _cts;
    private BatchApplyResult? _lastApplyResult;

    /// <summary>
    /// Event raised when the undo toast should be shown.
    /// </summary>
    public event EventHandler<UndoToastEventArgs>? UndoToastRequested;

    /// <summary>
    /// Event raised when undo completes.
    /// </summary>
    public event EventHandler<BatchUndoCompletedEventArgs>? UndoCompleted;

    /// <summary>
    /// The progress ViewModel for the overlay.
    /// </summary>
    public ApplyProgressViewModel ProgressViewModel => _progressViewModel;

    /// <summary>
    /// Whether an operation is currently in progress.
    /// </summary>
    public bool IsOperationInProgress => _progressViewModel.IsVisible;

    /// <summary>
    /// Whether undo is available for the last operation.
    /// </summary>
    public bool CanUndo => _lastApplyResult?.CanUndoAll == true;

    /// <summary>
    /// Create a new chat proposal coordinator.
    /// </summary>
    public ChatProposalCoordinator(
        IFileTreeProposalService proposalService,
        IDiffService diffService,
        IInlineDiffService inlineDiffService,
        ApplyProgressViewModel progressViewModel,
        ILogger<ChatProposalCoordinator> logger)
    {
        _proposalService = proposalService ?? throw new ArgumentNullException(nameof(proposalService));
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _inlineDiffService = inlineDiffService ?? throw new ArgumentNullException(nameof(inlineDiffService));
        _progressViewModel = progressViewModel ?? throw new ArgumentNullException(nameof(progressViewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ChatProposalCoordinator initialized");
    }

    #region Preview Workflow

    /// <summary>
    /// Show the batch preview dialog for the selected files.
    /// </summary>
    /// <param name="proposalVm">The proposal ViewModel.</param>
    /// <param name="workspacePath">The workspace root path.</param>
    /// <param name="owner">The owner window for the dialog.</param>
    /// <returns>True if user confirmed, false if cancelled.</returns>
    public async Task<bool> PreviewAsync(
        FileTreeProposalViewModel proposalVm,
        string workspacePath,
        Window owner)
    {
        _logger.LogDebug("Opening batch preview dialog for {Count} files", proposalVm.SelectedCount);

        try
        {
            // Generate diff previews for selected operations
            var previews = await _proposalService.PreviewProposalAsync(
                proposalVm.Proposal,
                workspacePath);

            if (!previews.Any())
            {
                _logger.LogWarning("No previews generated for proposal");
                return false;
            }

            // Create and show the dialog
            var dialog = new BatchPreviewDialog(
                previews.ToList(),
                _diffService,
                _inlineDiffService);

            var result = await dialog.ShowDialog<bool>(owner);

            _logger.LogDebug("Batch preview dialog result: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing batch preview dialog");
            return false;
        }
    }

    #endregion

    #region Apply Workflow

    /// <summary>
    /// Apply the selected files from a proposal.
    /// </summary>
    /// <param name="proposalVm">The proposal ViewModel.</param>
    /// <param name="workspacePath">The workspace root path.</param>
    /// <param name="createBackups">Whether to create backup files.</param>
    /// <returns>The apply result.</returns>
    public async Task<BatchApplyResult> ApplyAsync(
        FileTreeProposalViewModel proposalVm,
        string workspacePath,
        bool createBackups = true)
    {
        if (IsOperationInProgress)
        {
            throw new InvalidOperationException("An operation is already in progress");
        }

        _logger.LogInformation(
            "Applying proposal with {Count} selected files",
            proposalVm.SelectedCount);

        _cts = new CancellationTokenSource();

        try
        {
            // Start progress overlay
            _progressViewModel.Start(_cts, proposalVm.SelectedCount);

            // Create progress adapter
            var progress = new ProgressReporterAdapter(_progressViewModel);

            // Execute apply
            var result = await _proposalService.ApplyProposalAsync(
                proposalVm.Proposal,
                workspacePath,
                new ApplyOptions { CreateBackup = createBackups },
                progress,
                _cts.Token);

            // Handle result
            if (result.AllSucceeded)
            {
                _progressViewModel.Complete();
                _lastApplyResult = result;

                // Request undo toast
                UndoToastRequested?.Invoke(this, new UndoToastEventArgs(result));

                _logger.LogInformation(
                    "Successfully applied {Count} files in {Duration:F1}s",
                    result.SuccessCount, result.Duration.TotalSeconds);
            }
            else if (result.WasCancelled)
            {
                _progressViewModel.RollbackComplete();

                _logger.LogInformation("Apply cancelled by user, rolled back");
            }
            else
            {
                var firstFailed = result.FailedResults.FirstOrDefault();
                _progressViewModel.Error(
                    $"Failed: {firstFailed?.FilePath ?? "Unknown error"}");

                _logger.LogWarning(
                    "Apply completed with {FailedCount} failures",
                    result.FailedCount);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _progressViewModel.RollbackComplete();

            _logger.LogInformation("Apply cancelled");

            return BatchApplyResult.Cancelled(
                Array.Empty<ApplyResult>(),
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _progressViewModel.Error(ex.Message);
            _logger.LogError(ex, "Error during apply");

            return BatchApplyResult.RolledBack(ex.Message, DateTime.UtcNow);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// Cancel the current apply operation.
    /// </summary>
    public void CancelApply()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _logger.LogInformation("Cancelling apply operation");
            _cts.Cancel();
        }
    }

    #endregion

    #region Undo Workflow

    /// <summary>
    /// Undo the last apply operation.
    /// </summary>
    /// <returns>True if undo succeeded.</returns>
    public async Task<bool> UndoLastApplyAsync()
    {
        if (_lastApplyResult == null || !_lastApplyResult.CanUndoAll)
        {
            _logger.LogWarning("No undo available");
            return false;
        }

        _logger.LogInformation("Undoing last apply with {Count} files", _lastApplyResult.SuccessCount);

        try
        {
            await _proposalService.UndoBatchApplyAsync(_lastApplyResult);

            UndoCompleted?.Invoke(this, new BatchUndoCompletedEventArgs(true, null));

            _lastApplyResult = null;

            _logger.LogInformation("Undo completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during undo");
            UndoCompleted?.Invoke(this, new BatchUndoCompletedEventArgs(false, ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Clear the undo state.
    /// </summary>
    public void ClearUndoState()
    {
        _lastApplyResult = null;
        _logger.LogDebug("Undo state cleared");
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _progressViewModel.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}
