using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY CHANGES DIALOG VIEW MODEL (v0.4.3f)                                │
// │ ViewModel for the Apply Changes confirmation dialog.                     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for the Apply Changes confirmation dialog.
/// Manages diff preview display and apply operation execution.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3f.</para>
/// </remarks>
public partial class ApplyChangesDialogViewModel : ViewModelBase
{
    private readonly IFileChangeService? _changeService;
    private Action<ApplyResult?>? _closeAction;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Gets the code block being applied.</summary>
    [ObservableProperty]
    private CodeBlock? _codeBlock;

    /// <summary>Gets the file name for display.</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>Gets the full file path for display.</summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>Gets whether this creates a new file.</summary>
    [ObservableProperty]
    private bool _isNewFile;

    /// <summary>Gets or sets whether to create a backup. Default: true.</summary>
    [ObservableProperty]
    private bool _createBackup = true;

    /// <summary>Gets whether an apply is in progress.</summary>
    [ObservableProperty]
    private bool _isApplying;

    /// <summary>Gets the current error message.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Gets the apply result.</summary>
    [ObservableProperty]
    private ApplyResult? _result;

    /// <summary>Gets the workspace path.</summary>
    [ObservableProperty]
    private string _workspacePath = string.Empty;

    /// <summary>Gets the diff summary for display.</summary>
    [ObservableProperty]
    private string _diffSummary = string.Empty;

    /// <summary>Gets the original content.</summary>
    [ObservableProperty]
    private string _originalContent = string.Empty;

    /// <summary>Gets the proposed content.</summary>
    [ObservableProperty]
    private string _proposedContent = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Gets whether Apply can execute.</summary>
    public bool CanApply => !IsApplying;

    /// <summary>Gets whether Cancel can execute.</summary>
    public bool CanCancel => !IsApplying;

    /// <summary>Gets the Apply button text.</summary>
    public string ApplyButtonText => IsApplying ? "Applying..." : "Apply Changes";

    /// <summary>Gets whether to show error.</summary>
    public bool ShowError => !string.IsNullOrEmpty(ErrorMessage);

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance for design-time or testing.
    /// </summary>
    public ApplyChangesDialogViewModel()
    {
        // Wire up property change notifications
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsApplying))
            {
                OnPropertyChanged(nameof(CanApply));
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(ApplyButtonText));
            }
            else if (e.PropertyName == nameof(ErrorMessage))
            {
                OnPropertyChanged(nameof(ShowError));
            }
        };
    }

    /// <summary>
    /// Initializes a new instance with dependencies.
    /// </summary>
    public ApplyChangesDialogViewModel(
        IFileChangeService changeService,
        CodeBlock codeBlock,
        string workspacePath,
        string originalContent,
        string proposedContent,
        bool isNewFile,
        Action<ApplyResult?> closeAction)
        : this()
    {
        _changeService = changeService ?? throw new ArgumentNullException(nameof(changeService));
        _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

        CodeBlock = codeBlock ?? throw new ArgumentNullException(nameof(codeBlock));
        WorkspacePath = workspacePath ?? string.Empty;
        OriginalContent = originalContent ?? string.Empty;
        ProposedContent = proposedContent ?? string.Empty;
        IsNewFile = isNewFile;

        FileName = Path.GetFileName(codeBlock.TargetFilePath ?? "Unknown");
        FilePath = codeBlock.TargetFilePath ?? string.Empty;

        // Calculate diff summary
        var addedLines = proposedContent.Split('\n').Length;
        var removedLines = originalContent.Split('\n').Length;
        DiffSummary = isNewFile 
            ? $"+{addedLines} lines (new file)"
            : $"+{Math.Max(0, addedLines - removedLines)} / -{Math.Max(0, removedLines - addedLines)} lines";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Executes the apply operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync(CancellationToken ct)
    {
        if (IsApplying || CodeBlock == null || _changeService == null) return;

        try
        {
            IsApplying = true;
            ErrorMessage = null;

            var options = ApplyOptions.Default with
            {
                CreateBackup = CreateBackup
            };

            Result = await _changeService.ApplyCodeBlockAsync(
                CodeBlock,
                WorkspacePath,
                options,
                ct);

            if (Result.Success)
            {
                _closeAction?.Invoke(Result);
            }
            else
            {
                ErrorMessage = Result.ErrorMessage ?? "Failed to apply changes";
            }
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Operation was cancelled";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    /// <summary>
    /// Cancels the dialog.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }

    /// <summary>
    /// Sets the close action for the dialog.
    /// </summary>
    public void SetCloseAction(Action<ApplyResult?> closeAction)
    {
        _closeAction = closeAction;
    }
}
