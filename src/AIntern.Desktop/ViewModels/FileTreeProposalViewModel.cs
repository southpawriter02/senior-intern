namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PROPOSAL VIEWMODEL (v0.4.4d)                                   │
// │ Main ViewModel for displaying and managing a multi-file proposal.        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for displaying and managing a multi-file creation proposal.
/// Coordinates between the FileTreeProposal model, proposal service, and UI.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel provides:
/// <list type="bullet">
/// <item>Hierarchical tree display of proposed files</item>
/// <item>Selection management with bulk operations</item>
/// <item>Validation display with issue mapping to tree items</item>
/// <item>Apply operation with progress and cancellation</item>
/// <item>Preview functionality for viewing diffs</item>
/// </list>
/// </para>
/// </remarks>
public partial class FileTreeProposalViewModel : ViewModelBase, IDisposable
{
    private readonly IFileTreeProposalService _proposalService;
    private readonly IDiffService _diffService;
    private readonly ITreeBuildingService _treeBuildingService;
    private readonly ILogger<FileTreeProposalViewModel> _logger;
    private readonly string _workspacePath;
    private CancellationTokenSource? _applyCancellation;
    private bool _disposed;

    #region Observable Properties

    /// <summary>
    /// The underlying proposal model.
    /// </summary>
    [ObservableProperty]
    private FileTreeProposal _proposal;

    /// <summary>
    /// Hierarchical tree structure built from the proposal's operations.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FileOperationItemViewModel> _treeItems = [];

    /// <summary>
    /// Currently selected item in the tree.
    /// </summary>
    [ObservableProperty]
    private FileOperationItemViewModel? _selectedItem;

    /// <summary>
    /// Human-readable description of the proposal.
    /// </summary>
    [ObservableProperty]
    private string? _description;

    /// <summary>
    /// Total number of files in the proposal.
    /// </summary>
    [ObservableProperty]
    private int _fileCount;

    /// <summary>
    /// Total number of directories in the proposal.
    /// </summary>
    [ObservableProperty]
    private int _directoryCount;

    /// <summary>
    /// Number of currently selected files.
    /// </summary>
    [ObservableProperty]
    private int _selectedCount;

    /// <summary>
    /// Whether any files are selected for creation.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviewCommand))]
    private bool _hasSelectedFiles;

    /// <summary>
    /// Whether to create backups of existing files before overwriting.
    /// </summary>
    [ObservableProperty]
    private bool _createBackups = true;

    /// <summary>
    /// Whether an apply operation is currently in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviewCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeselectAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isApplying;

    /// <summary>
    /// Progress percentage of the current apply operation (0-100).
    /// </summary>
    [ObservableProperty]
    private double _applyProgress;

    /// <summary>
    /// Path of the file currently being processed.
    /// </summary>
    [ObservableProperty]
    private string _currentFile = string.Empty;

    /// <summary>
    /// Result of the most recent validation.
    /// </summary>
    [ObservableProperty]
    private ProposalValidationResult? _validationResult;

    /// <summary>
    /// Whether the validation found any warnings.
    /// </summary>
    [ObservableProperty]
    private bool _hasWarnings;

    /// <summary>
    /// Whether the validation found any errors.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private bool _hasErrors;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether the apply operation can be executed.
    /// </summary>
    public bool CanApply => HasSelectedFiles && !IsApplying && !HasErrors;

    /// <summary>
    /// Summary text for the selection state.
    /// </summary>
    public string SelectionSummary => $"{SelectedCount} of {FileCount} files selected";

    /// <summary>
    /// Whether all files are currently selected.
    /// </summary>
    public bool AllFilesSelected => SelectedCount == FileCount && FileCount > 0;

    /// <summary>
    /// Whether no files are currently selected.
    /// </summary>
    public bool NoFilesSelected => SelectedCount == 0;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new FileTreeProposalViewModel.
    /// </summary>
    /// <param name="proposal">The proposal to display.</param>
    /// <param name="proposalService">Service for validating and applying proposals.</param>
    /// <param name="diffService">Service for generating diffs.</param>
    /// <param name="treeBuildingService">Service for building tree structures.</param>
    /// <param name="workspacePath">The workspace root path.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public FileTreeProposalViewModel(
        FileTreeProposal proposal,
        IFileTreeProposalService proposalService,
        IDiffService diffService,
        ITreeBuildingService treeBuildingService,
        string workspacePath,
        ILogger<FileTreeProposalViewModel> logger)
    {
        _proposal = proposal;
        _proposalService = proposalService;
        _diffService = diffService;
        _treeBuildingService = treeBuildingService;
        _workspacePath = workspacePath;
        _logger = logger;

        Description = proposal.Description;
        FileCount = proposal.FileCount;
        DirectoryCount = proposal.DirectoryCount;

        _logger.LogDebug(
            "Creating proposal ViewModel with {FileCount} files and {DirCount} directories",
            FileCount,
            DirectoryCount);

        BuildTree();
        UpdateSelectedCount();

        // Trigger async validation without blocking constructor
        _ = ValidateAsync();
    }

    #endregion

    #region Tree Building

    /// <summary>
    /// Builds the hierarchical tree structure from the proposal's flat operations list.
    /// </summary>
    private void BuildTree()
    {
        _logger.LogDebug("Building tree structure");

        // Unsubscribe from existing items
        foreach (var item in GetAllItemsRecursive(TreeItems))
        {
            item.PropertyChanged -= OnFileItemPropertyChanged;
        }

        TreeItems.Clear();

        // Use the tree building service to create the hierarchy
        var treeNodes = _treeBuildingService.BuildTree(Proposal);

        foreach (var node in treeNodes)
        {
            var vm = FileOperationItemViewModel.FromTreeNode(node);
            TreeItems.Add(vm);
        }

        // Subscribe to property changes for selection sync
        foreach (var item in GetAllItemsRecursive(TreeItems))
        {
            item.PropertyChanged += OnFileItemPropertyChanged;
        }

        _logger.LogInformation(
            "Built tree with {RootCount} root items",
            TreeItems.Count);
    }

    /// <summary>
    /// Rebuilds the tree structure, preserving expansion state where possible.
    /// </summary>
    public void RebuildTree()
    {
        _logger.LogDebug("Rebuilding tree structure");

        // Capture current expansion state
        var expandedPaths = GetAllItemsRecursive(TreeItems)
            .Where(i => i.IsDirectory && i.IsExpanded)
            .Select(i => i.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        BuildTree();

        // Restore expansion state
        foreach (var item in GetAllItemsRecursive(TreeItems).Where(i => i.IsDirectory))
        {
            item.IsExpanded = expandedPaths.Contains(item.Path);
        }
    }

    /// <summary>
    /// Finds a tree item by its path.
    /// </summary>
    /// <param name="path">The relative path to search for.</param>
    /// <returns>The matching item, or null if not found.</returns>
    public FileOperationItemViewModel? FindItemByPath(string path)
    {
        return GetAllItemsRecursive(TreeItems)
            .FirstOrDefault(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Recursively enumerates all items in the tree.
    /// </summary>
    private IEnumerable<FileOperationItemViewModel> GetAllItemsRecursive(
        IEnumerable<FileOperationItemViewModel> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var child in GetAllItemsRecursive(item.Children))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Gets all file items (non-directory) in the tree.
    /// </summary>
    private IEnumerable<FileOperationItemViewModel> GetAllFileItems()
    {
        return GetAllItemsRecursive(TreeItems).Where(i => !i.IsDirectory);
    }

    #endregion

    #region Selection Management

    /// <summary>
    /// Handles property changes on tree items, syncing selection state.
    /// </summary>
    private void OnFileItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileOperationItemViewModel.IsSelected))
        {
            var item = (FileOperationItemViewModel)sender!;
            _logger.LogTrace("Selection changed for {Path}: {Selected}", item.Path, item.IsSelected);

            UpdateSelectedCount();
            UpdateDirectorySelectionStates();
        }
    }

    /// <summary>
    /// Updates the selected count and related properties.
    /// </summary>
    private void UpdateSelectedCount()
    {
        SelectedCount = Proposal.SelectedCount;
        HasSelectedFiles = SelectedCount > 0;
        OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(AllFilesSelected));
        OnPropertyChanged(nameof(NoFilesSelected));
    }

    /// <summary>
    /// Updates all directory selection states based on their children.
    /// </summary>
    private void UpdateDirectorySelectionStates()
    {
        foreach (var dir in GetAllItemsRecursive(TreeItems).Where(i => i.IsDirectory))
        {
            UpdateDirectorySelectionState(dir);
        }
    }

    /// <summary>
    /// Updates a directory's selection state based on its children.
    /// </summary>
    /// <param name="directory">The directory item to update.</param>
    public void UpdateDirectorySelectionState(FileOperationItemViewModel directory)
    {
        if (!directory.IsDirectory)
            return;

        var fileChildren = GetAllItemsRecursive(directory.Children)
            .Where(i => !i.IsDirectory)
            .ToList();

        if (fileChildren.Count == 0)
        {
            directory.SelectionState = SelectionState.None;
        }
        else if (fileChildren.All(f => f.IsSelected))
        {
            directory.SelectionState = SelectionState.All;
        }
        else if (fileChildren.Any(f => f.IsSelected))
        {
            directory.SelectionState = SelectionState.Some;
        }
        else
        {
            directory.SelectionState = SelectionState.None;
        }
    }

    /// <summary>
    /// Sets the selection state of all file items.
    /// </summary>
    /// <param name="selected">Whether items should be selected.</param>
    private void SetAllSelected(bool selected)
    {
        _logger.LogDebug("Setting all files selected: {Selected}", selected);

        foreach (var item in GetAllFileItems())
        {
            item.IsSelected = selected;
        }
    }

    /// <summary>
    /// Propagates selection state from a parent to all its children.
    /// </summary>
    /// <param name="parent">The parent item.</param>
    /// <param name="selected">The selection state to propagate.</param>
    public void PropagateSelectionToChildren(FileOperationItemViewModel parent, bool selected)
    {
        foreach (var child in parent.Children)
        {
            if (!child.IsDirectory)
            {
                child.IsSelected = selected;
            }
            else
            {
                PropagateSelectionToChildren(child, selected);
            }
        }
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates the proposal asynchronously.
    /// </summary>
    private async Task ValidateAsync()
    {
        try
        {
            _logger.LogDebug("Starting validation for proposal");

            ValidationResult = await _proposalService.ValidateProposalAsync(
                Proposal, _workspacePath);

            HasWarnings = ValidationResult.HasWarnings;
            HasErrors = ValidationResult.HasErrors;

            _logger.LogInformation(
                "Validation complete: Valid={Valid}, Errors={Errors}, Warnings={Warnings}",
                ValidationResult.IsValid,
                ValidationResult.ErrorCount,
                ValidationResult.WarningCount);

            UpdateValidationDisplay();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed with exception");

            // Create an error result if validation itself fails
            ValidationResult = ProposalValidationResult.Invalid(
                new ValidationIssue
                {
                    Type = ValidationIssueType.InvalidPath, // Using InvalidPath as fallback for unknown errors
                    Severity = ValidationSeverity.Error,
                    Message = $"Validation failed: {ex.Message}"
                });
            HasErrors = true;
        }
    }

    /// <summary>
    /// Updates tree items with validation issue information.
    /// </summary>
    private void UpdateValidationDisplay()
    {
        if (ValidationResult == null)
            return;

        // Clear existing validation issues
        foreach (var item in GetAllItemsRecursive(TreeItems))
        {
            item.ValidationIssue = null;
        }

        // Map issues to tree items
        foreach (var issue in ValidationResult.Issues)
        {
            var item = FindItemByPath(issue.Path);
            if (item != null)
            {
                item.ValidationIssue = issue;
                _logger.LogTrace(
                    "Mapped validation issue to {Path}: {Message}",
                    issue.Path,
                    issue.Message);
            }
        }

        // Update file exists flags
        foreach (var item in GetAllFileItems())
        {
            item.FileExists = ValidationResult.Issues.Any(
                i => i.Path == item.Path && i.Type == ValidationIssueType.FileExists);
        }
    }

    /// <summary>
    /// Clears all validation issues from the tree.
    /// </summary>
    public void ClearValidationIssues()
    {
        _logger.LogDebug("Clearing validation issues");

        foreach (var item in GetAllItemsRecursive(TreeItems))
        {
            item.ValidationIssue = null;
            item.FileExists = false;
        }
        ValidationResult = null;
        HasWarnings = false;
        HasErrors = false;
    }

    /// <summary>
    /// Re-validates the proposal.
    /// </summary>
    [RelayCommand]
    private async Task RevalidateAsync()
    {
        _logger.LogDebug("Re-validating proposal");
        ClearValidationIssues();
        await ValidateAsync();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Selects all files in the proposal.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectOrDeselect))]
    private void SelectAll()
    {
        _logger.LogDebug("SelectAll command executed");
        SetAllSelected(true);
    }

    /// <summary>
    /// Deselects all files in the proposal.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectOrDeselect))]
    private void DeselectAll()
    {
        _logger.LogDebug("DeselectAll command executed");
        SetAllSelected(false);
    }

    private bool CanSelectOrDeselect() => !IsApplying;

    /// <summary>
    /// Toggles the selection state of a specific item.
    /// </summary>
    /// <param name="item">The item to toggle.</param>
    [RelayCommand]
    private void ToggleItemSelection(FileOperationItemViewModel? item)
    {
        if (item == null || IsApplying)
            return;

        _logger.LogDebug("Toggling selection for {Path}", item.Path);

        if (item.IsDirectory)
        {
            // Toggle all children
            var newState = item.SelectionState != SelectionState.All;
            PropagateSelectionToChildren(item, newState);
            UpdateDirectorySelectionState(item);
        }
        else
        {
            item.IsSelected = !item.IsSelected;
        }
    }

    /// <summary>
    /// Expands all directory nodes in the tree.
    /// </summary>
    [RelayCommand]
    private void ExpandAll()
    {
        _logger.LogDebug("Expanding all directories");
        foreach (var item in GetAllItemsRecursive(TreeItems).Where(i => i.IsDirectory))
        {
            item.IsExpanded = true;
        }
    }

    /// <summary>
    /// Collapses all directory nodes in the tree.
    /// </summary>
    [RelayCommand]
    private void CollapseAll()
    {
        _logger.LogDebug("Collapsing all directories");
        foreach (var item in GetAllItemsRecursive(TreeItems).Where(i => i.IsDirectory))
        {
            item.IsExpanded = false;
        }
    }

    /// <summary>
    /// Opens the batch preview dialog with diffs for all selected files.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task PreviewAsync()
    {
        _logger.LogDebug("Preview command executed");

        try
        {
            var diffs = await _proposalService.PreviewProposalAsync(Proposal, _workspacePath);
            _logger.LogInformation("Generated {Count} preview diffs", diffs.Count);
            PreviewRequested?.Invoke(this, diffs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview failed");
            SetError($"Preview failed: {ex.Message}");
        }
    }

    private bool CanPreview() => HasSelectedFiles && !IsApplying;

    /// <summary>
    /// Applies the proposal, creating all selected files.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteApply))]
    private async Task ApplyAsync()
    {
        if (!HasSelectedFiles || IsApplying)
            return;

        _logger.LogInformation(
            "Applying proposal with {Count} selected files",
            SelectedCount);

        try
        {
            IsApplying = true;
            ApplyProgress = 0;
            CurrentFile = string.Empty;

            _applyCancellation = new CancellationTokenSource();

            var options = new ApplyOptions { CreateBackup = CreateBackups };
            var progress = new Progress<BatchApplyProgress>(OnProgressUpdate);

            var result = await _proposalService.ApplyProposalAsync(
                Proposal,
                _workspacePath,
                options,
                progress,
                _applyCancellation.Token);

            // Update tree items with operation results
            UpdateTreeWithResults(result);

            _logger.LogInformation(
                "Apply completed: Success={Success}, Failed={Failed}",
                result.SuccessCount,
                result.FailedCount);

            ApplyCompleted?.Invoke(this, result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Apply operation was cancelled");
            ApplyCancelled?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apply failed with exception");
            SetError($"Apply failed: {ex.Message}");
        }
        finally
        {
            IsApplying = false;
            _applyCancellation?.Dispose();
            _applyCancellation = null;
        }
    }

    private bool CanExecuteApply() => CanApply;

    /// <summary>
    /// Cancels the in-progress apply operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _logger.LogDebug("Cancel command executed");
        _applyCancellation?.Cancel();
    }

    private bool CanCancel() => IsApplying;

    /// <summary>
    /// Handles progress updates during apply.
    /// </summary>
    private void OnProgressUpdate(BatchApplyProgress progress)
    {
        ApplyProgress = progress.ProgressPercent;
        CurrentFile = progress.CurrentFile;

        // Update the current item's status
        var item = FindItemByPath(progress.CurrentFile);
        if (item != null)
        {
            item.OperationStatus = FileOperationStatus.InProgress;
        }
    }

    /// <summary>
    /// Updates tree items with the results of the apply operation.
    /// </summary>
    private void UpdateTreeWithResults(BatchApplyResult result)
    {
        foreach (var opResult in result.Results)
        {
            var item = FindItemByPath(opResult.RelativePath);
            if (item != null)
            {
                // Map ApplyResult to FileOperationStatus
                var status = opResult.Success 
                    ? FileOperationStatus.Applied 
                    : FileOperationStatus.Failed;
                item.OperationStatus = status;
                
                if (!opResult.Success)
                {
                    item.ValidationIssue = new ValidationIssue
                    {
                        Path = opResult.RelativePath,
                        Type = ValidationIssueType.InvalidPath, // Using InvalidPath as fallback for errors
                        Severity = ValidationSeverity.Error,
                        Message = opResult.ErrorMessage ?? "Operation failed"
                    };
                }
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the user requests to preview the changes.
    /// </summary>
    public event EventHandler<IReadOnlyList<DiffResult>>? PreviewRequested;

    /// <summary>
    /// Raised when the apply operation completes.
    /// </summary>
    public event EventHandler<BatchApplyResult>? ApplyCompleted;

    /// <summary>
    /// Raised when the apply operation is cancelled.
    /// </summary>
    public event EventHandler? ApplyCancelled;

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleans up resources and event handlers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogDebug("Disposing FileTreeProposalViewModel");

        foreach (var item in GetAllItemsRecursive(TreeItems))
        {
            item.PropertyChanged -= OnFileItemPropertyChanged;
        }

        _applyCancellation?.Dispose();
        _disposed = true;
    }

    #endregion
}
