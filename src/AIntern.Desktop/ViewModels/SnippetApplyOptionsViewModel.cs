// -----------------------------------------------------------------------
// <copyright file="SnippetApplyOptionsViewModel.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     ViewModel for the snippet apply options popup.
//     Manages user selections and coordinates with services for preview and apply.
//     Added in v0.4.5e.
// </summary>
// -----------------------------------------------------------------------

using System.Diagnostics;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the snippet apply options popup.
/// Manages insert mode selection, line range inputs, preview computation, and apply actions.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel coordinates with <see cref="ISnippetApplyService"/> to:
/// <list type="bullet">
///   <item><description>Get AI-suggested insertion locations</description></item>
///   <item><description>Detect indentation style of target files</description></item>
///   <item><description>Preview changes before applying</description></item>
///   <item><description>Apply snippets to files with configured options</description></item>
/// </list>
/// </para>
/// <para>Added in v0.4.5e.</para>
/// </remarks>
public partial class SnippetApplyOptionsViewModel : ViewModelBase
{
    // ═══════════════════════════════════════════════════════════════════
    // Dependencies
    // ═══════════════════════════════════════════════════════════════════

    private readonly ISnippetApplyService _snippetApplyService;
    private readonly IDiffService _diffService;
    private readonly ILogger<SnippetApplyOptionsViewModel>? _logger;

    // ═══════════════════════════════════════════════════════════════════
    // Preview Debounce State
    // ═══════════════════════════════════════════════════════════════════

    private CancellationTokenSource? _previewCts;
    private readonly Stopwatch _debounceTimer = new();
    private const int DebounceDelayMs = 300;

    // ═══════════════════════════════════════════════════════════════════
    // Observable Properties - File Information
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Target file path.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    private string _filePath = string.Empty;

    /// <summary>
    /// The snippet content to apply.
    /// </summary>
    [ObservableProperty]
    private string _snippetContent = string.Empty;

    /// <summary>
    /// Total lines in the target file.
    /// </summary>
    [ObservableProperty]
    private int _totalLines;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [ObservableProperty]
    private long _fileSizeBytes;

    /// <summary>
    /// Raw file content (for preview computation).
    /// </summary>
    [ObservableProperty]
    private string? _fileContent;

    // ═══════════════════════════════════════════════════════════════════
    // Observable Properties - Insert Mode Selection
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Selected insert mode.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReplaceMode))]
    [NotifyPropertyChangedFor(nameof(IsInsertMode))]
    [NotifyPropertyChangedFor(nameof(ShowTargetLine))]
    [NotifyPropertyChangedFor(nameof(ShowRangeInputs))]
    [NotifyPropertyChangedFor(nameof(InsertModeDescription))]
    [NotifyPropertyChangedFor(nameof(IsReplaceFileMode))]
    [NotifyPropertyChangedFor(nameof(IsReplaceRangeMode))]
    [NotifyPropertyChangedFor(nameof(IsInsertBeforeMode))]
    [NotifyPropertyChangedFor(nameof(IsInsertAfterMode))]
    [NotifyPropertyChangedFor(nameof(IsAppendMode))]
    [NotifyPropertyChangedFor(nameof(IsPrependMode))]
    private SnippetInsertMode _insertMode = SnippetInsertMode.ReplaceFile;

    /// <summary>
    /// Target line for InsertBefore/InsertAfter modes.
    /// </summary>
    [ObservableProperty]
    private int _targetLine = 1;

    /// <summary>
    /// Start line for Replace mode.
    /// </summary>
    [ObservableProperty]
    private int _startLine = 1;

    /// <summary>
    /// End line for Replace mode.
    /// </summary>
    [ObservableProperty]
    private int _endLine = 1;

    // ═══════════════════════════════════════════════════════════════════
    // Observable Properties - Formatting Options
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether to preserve the target file's indentation style.
    /// </summary>
    [ObservableProperty]
    private bool _preserveIndentation = true;

    /// <summary>
    /// Whether to add a blank line before the snippet.
    /// </summary>
    [ObservableProperty]
    private bool _addBlankLineBefore;

    /// <summary>
    /// Whether to add a blank line after the snippet.
    /// </summary>
    [ObservableProperty]
    private bool _addBlankLineAfter;

    /// <summary>
    /// Detected indentation style of the target file.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IndentationDescription))]
    private IndentationStyle? _detectedIndentation;

    // ═══════════════════════════════════════════════════════════════════
    // Observable Properties - Preview State
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Current preview result.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    private SnippetApplyPreview? _preview;

    /// <summary>
    /// Whether a preview is currently being computed.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private bool _isPreviewLoading;

    /// <summary>
    /// Whether the ViewModel is initializing.
    /// </summary>
    [ObservableProperty]
    private bool _isInitializing;

    // ═══════════════════════════════════════════════════════════════════
    // Observable Properties - Suggestion
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// AI-suggested insertion location.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuggestion))]
    [NotifyPropertyChangedFor(nameof(SuggestionConfidenceLevel))]
    [NotifyPropertyChangedFor(nameof(SuggestionConfidenceColor))]
    private SnippetLocationSuggestion? _suggestion;

    // ═══════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// File name extracted from path.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// True if the current mode is Replace (specific line range).
    /// </summary>
    public bool IsReplaceMode => InsertMode == SnippetInsertMode.Replace;

    /// <summary>
    /// True if the current mode requires a target line (InsertBefore/InsertAfter).
    /// </summary>
    public bool IsInsertMode =>
        InsertMode is SnippetInsertMode.InsertBefore or SnippetInsertMode.InsertAfter;

    /// <summary>
    /// Show target line input for InsertBefore/InsertAfter modes.
    /// </summary>
    public bool ShowTargetLine => IsInsertMode;

    /// <summary>
    /// Show line range inputs for Replace mode.
    /// </summary>
    public bool ShowRangeInputs => IsReplaceMode;

    // ═══════════════════════════════════════════════════════════════════
    // Mode Selection Properties (Two-Way Bindable)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets or sets whether ReplaceFile mode is selected.
    /// </summary>
    public bool IsReplaceFileMode
    {
        get => InsertMode == SnippetInsertMode.ReplaceFile;
        set { if (value) InsertMode = SnippetInsertMode.ReplaceFile; }
    }

    /// <summary>
    /// Gets or sets whether Replace (specific lines) mode is selected.
    /// </summary>
    public bool IsReplaceRangeMode
    {
        get => InsertMode == SnippetInsertMode.Replace;
        set { if (value) InsertMode = SnippetInsertMode.Replace; }
    }

    /// <summary>
    /// Gets or sets whether InsertBefore mode is selected.
    /// </summary>
    public bool IsInsertBeforeMode
    {
        get => InsertMode == SnippetInsertMode.InsertBefore;
        set { if (value) InsertMode = SnippetInsertMode.InsertBefore; }
    }

    /// <summary>
    /// Gets or sets whether InsertAfter mode is selected.
    /// </summary>
    public bool IsInsertAfterMode
    {
        get => InsertMode == SnippetInsertMode.InsertAfter;
        set { if (value) InsertMode = SnippetInsertMode.InsertAfter; }
    }

    /// <summary>
    /// Gets or sets whether Append mode is selected.
    /// </summary>
    public bool IsAppendMode
    {
        get => InsertMode == SnippetInsertMode.Append;
        set { if (value) InsertMode = SnippetInsertMode.Append; }
    }

    /// <summary>
    /// Gets or sets whether Prepend mode is selected.
    /// </summary>
    public bool IsPrependMode
    {
        get => InsertMode == SnippetInsertMode.Prepend;
        set { if (value) InsertMode = SnippetInsertMode.Prepend; }
    }

    /// <summary>
    /// True if we can apply (have preview and not loading and no error).
    /// </summary>
    public bool CanApply => Preview is not null && !IsPreviewLoading && !HasError;

    /// <summary>
    /// True if a preview is available.
    /// </summary>
    public bool HasPreview => Preview is not null;

    /// <summary>
    /// True if there's an error message.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// True if a location suggestion is available.
    /// </summary>
    public bool HasSuggestion => Suggestion is not null;

    /// <summary>
    /// Human-readable confidence level for the suggestion.
    /// </summary>
    public string SuggestionConfidenceLevel => Suggestion?.Confidence switch
    {
        >= 0.8 => "High",
        >= 0.5 => "Medium",
        _ => "Low"
    };

    /// <summary>
    /// Resource key for the suggestion confidence color.
    /// </summary>
    public string SuggestionConfidenceColor => Suggestion?.Confidence switch
    {
        >= 0.8 => "SuccessBrush",
        >= 0.5 => "WarningBrush",
        _ => "TextMuted"
    };

    /// <summary>
    /// Human-readable description of detected indentation.
    /// </summary>
    public string IndentationDescription => DetectedIndentation?.ToString() ?? "Unknown";

    /// <summary>
    /// Human-readable description of the current insert mode.
    /// </summary>
    public string InsertModeDescription => InsertMode switch
    {
        SnippetInsertMode.ReplaceFile => "Replace entire file content",
        SnippetInsertMode.Replace => $"Replace lines {StartLine}-{EndLine}",
        SnippetInsertMode.InsertBefore => $"Insert before line {TargetLine}",
        SnippetInsertMode.InsertAfter => $"Insert after line {TargetLine}",
        SnippetInsertMode.Append => "Add to end of file",
        SnippetInsertMode.Prepend => "Add to beginning of file",
        _ => "Unknown mode"
    };

    /// <summary>
    /// Available insert modes with descriptions for the UI.
    /// </summary>
    public IReadOnlyList<InsertModeDescriptor> AvailableInsertModes { get; } =
    [
        new InsertModeDescriptor(
            SnippetInsertMode.ReplaceFile,
            "Replace entire file",
            "Replace all content with the snippet"),
        new InsertModeDescriptor(
            SnippetInsertMode.Replace,
            "Replace specific lines",
            "Replace a range of lines with the snippet"),
        new InsertModeDescriptor(
            SnippetInsertMode.InsertAfter,
            "Insert after line",
            "Insert snippet after a specific line"),
        new InsertModeDescriptor(
            SnippetInsertMode.InsertBefore,
            "Insert before line",
            "Insert snippet before a specific line"),
        new InsertModeDescriptor(
            SnippetInsertMode.Append,
            "Append to end",
            "Add snippet at the end of the file"),
        new InsertModeDescriptor(
            SnippetInsertMode.Prepend,
            "Prepend to beginning",
            "Add snippet at the beginning of the file")
    ];

    // ═══════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when the popup should be closed.
    /// </summary>
    public event EventHandler<SnippetApplyResult?>? RequestClose;

    /// <summary>
    /// Raised when the user wants to see a full diff preview.
    /// </summary>
    public event EventHandler<DiffResult?>? RequestDiffPreview;

    // ═══════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new instance of <see cref="SnippetApplyOptionsViewModel"/>.
    /// </summary>
    /// <param name="snippetApplyService">Service for applying snippets.</param>
    /// <param name="diffService">Service for computing diffs.</param>
    /// <param name="logger">Optional logger.</param>
    public SnippetApplyOptionsViewModel(
        ISnippetApplyService snippetApplyService,
        IDiffService diffService,
        ILogger<SnippetApplyOptionsViewModel>? logger = null)
    {
        _snippetApplyService = snippetApplyService ??
            throw new ArgumentNullException(nameof(snippetApplyService));
        _diffService = diffService ??
            throw new ArgumentNullException(nameof(diffService));
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Initialization
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes the ViewModel with file and snippet information.
    /// Loads file metadata, gets AI suggestion, and computes initial preview.
    /// </summary>
    /// <param name="filePath">Path to the target file.</param>
    /// <param name="snippetContent">The snippet content to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(
        string filePath,
        string snippetContent,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug(
            "Initializing SnippetApplyOptionsViewModel for file: {FilePath}",
            filePath);

        IsInitializing = true;
        ClearError();

        try
        {
            FilePath = filePath;
            SnippetContent = snippetContent;

            // Load file info in parallel with getting suggestion and indentation
            var fileInfoTask = LoadFileInfoAsync(filePath, cancellationToken);
            var suggestionTask = _snippetApplyService.SuggestLocationAsync(
                filePath, snippetContent, cancellationToken);
            var indentationTask = _snippetApplyService.DetectIndentationAsync(
                filePath, cancellationToken);

            await Task.WhenAll(fileInfoTask, suggestionTask, indentationTask);

            Suggestion = await suggestionTask;
            DetectedIndentation = await indentationTask;

            _logger?.LogDebug(
                "Suggestion: {Mode}, Confidence: {Confidence:P0}",
                Suggestion?.SuggestedMode,
                Suggestion?.Confidence ?? 0);

            // Auto-apply high-confidence suggestions
            if (Suggestion is { Confidence: >= 0.7 })
            {
                ApplySuggestion();
            }

            // Compute initial preview
            await RefreshPreviewAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize snippet options");
            SetError($"Failed to initialize: {ex.Message}");
        }
        finally
        {
            IsInitializing = false;
        }
    }

    /// <summary>
    /// Loads file information (size, line count, content).
    /// </summary>
    private async Task LoadFileInfoAsync(string filePath, CancellationToken cancellationToken)
    {
        if (File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            FileSizeBytes = fileInfo.Length;

            FileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            TotalLines = FileContent.Split('\n').Length;
            EndLine = TotalLines;

            _logger?.LogDebug(
                "Loaded file info: {Lines} lines, {Size} bytes",
                TotalLines,
                FileSizeBytes);
        }
        else
        {
            // New file
            TotalLines = 0;
            EndLine = 0;
            FileContent = null;
            FileSizeBytes = 0;

            _logger?.LogDebug("File does not exist, treating as new file");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Applies the current suggestion to the form inputs.
    /// </summary>
    [RelayCommand]
    private void ApplySuggestion()
    {
        if (Suggestion is null) return;

        _logger?.LogDebug("Applying suggestion: {Mode}", Suggestion.SuggestedMode);

        InsertMode = Suggestion.SuggestedMode;

        if (Suggestion.SuggestedLine.HasValue)
        {
            TargetLine = Suggestion.SuggestedLine.Value;
        }

        if (Suggestion.SuggestedRange.HasValue)
        {
            StartLine = Suggestion.SuggestedRange.Value.StartLine;
            EndLine = Suggestion.SuggestedRange.Value.EndLine;
        }
    }

    /// <summary>
    /// Refreshes the preview with current options.
    /// </summary>
    [RelayCommand]
    private async Task RefreshPreviewAsync()
    {
        // Cancel any pending preview computation
        await CancelPreviousPreviewAsync();
        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        IsPreviewLoading = true;
        ClearError();

        try
        {
            var options = BuildOptions();
            var (isValid, error) = options.Validate();

            if (!isValid)
            {
                SetError(error ?? "Invalid options configuration");
                Preview = null;
                return;
            }

            _logger?.LogDebug("Computing preview with mode: {Mode}", options.InsertMode);

            Preview = await _snippetApplyService.PreviewSnippetAsync(
                FilePath, SnippetContent, options, token);

            _logger?.LogDebug(
                "Preview computed: +{Added} -{Removed} lines",
                Preview.LinesAdded,
                Preview.LinesRemoved);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
            _logger?.LogDebug("Preview computation cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to compute preview");
            SetError(ex.Message);
            Preview = null;
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsPreviewLoading = false;
            }
        }
    }

    /// <summary>
    /// Applies the snippet with current options.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        var options = BuildOptions();
        var (isValid, error) = options.Validate();

        if (!isValid)
        {
            SetError(error ?? "Invalid options configuration");
            return;
        }

        _logger?.LogInformation(
            "Applying snippet to {FilePath} with mode {Mode}",
            FilePath,
            options.InsertMode);

        try
        {
            IsPreviewLoading = true;
            var result = await _snippetApplyService.ApplySnippetAsync(
                FilePath, SnippetContent, options);

            if (result.IsSuccess)
            {
                _logger?.LogInformation(
                    "Snippet applied successfully: +{Added} -{Removed} lines",
                    result.LinesAdded,
                    result.LinesRemoved);
                RequestClose?.Invoke(this, result);
            }
            else
            {
                _logger?.LogWarning("Apply failed: {Error}", result.ErrorMessage);
                SetError(result.ErrorMessage ?? "Apply failed");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception during apply");
            SetError(ex.Message);
        }
        finally
        {
            IsPreviewLoading = false;
        }
    }

    /// <summary>
    /// Cancels and closes the popup.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _logger?.LogDebug("User cancelled snippet apply options");
        _ = CancelPreviousPreviewAsync();
        RequestClose?.Invoke(this, null);
    }

    /// <summary>
    /// Opens a full diff preview dialog.
    /// </summary>
    [RelayCommand]
    private void ShowDiffPreview()
    {
        if (Preview?.Diff is not null)
        {
            _logger?.LogDebug("Showing diff preview dialog");
            RequestDiffPreview?.Invoke(this, Preview.Diff);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Property Change Handlers
    // ═══════════════════════════════════════════════════════════════════

    partial void OnInsertModeChanged(SnippetInsertMode value)
    {
        SchedulePreviewRefresh();
    }

    partial void OnTargetLineChanged(int value)
    {
        SchedulePreviewRefresh();
    }

    partial void OnStartLineChanged(int value)
    {
        // Ensure EndLine >= StartLine
        if (EndLine < value)
        {
            EndLine = value;
        }
        SchedulePreviewRefresh();
    }

    partial void OnEndLineChanged(int value)
    {
        // Ensure StartLine <= EndLine
        if (StartLine > value)
        {
            StartLine = value;
        }
        SchedulePreviewRefresh();
    }

    partial void OnPreserveIndentationChanged(bool value)
    {
        SchedulePreviewRefresh();
    }

    partial void OnAddBlankLineBeforeChanged(bool value)
    {
        SchedulePreviewRefresh();
    }

    partial void OnAddBlankLineAfterChanged(bool value)
    {
        SchedulePreviewRefresh();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Schedules a debounced preview refresh.
    /// </summary>
    private void SchedulePreviewRefresh()
    {
        // Don't refresh during initialization
        if (IsInitializing) return;

        // Simple debouncing using Task.Delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(DebounceDelayMs);
            await RefreshPreviewAsync();
        });
    }

    /// <summary>
    /// Cancels any pending preview computation.
    /// </summary>
    private async Task CancelPreviousPreviewAsync()
    {
        if (_previewCts is not null)
        {
            await _previewCts.CancelAsync();
            _previewCts.Dispose();
            _previewCts = null;
        }
    }

    /// <summary>
    /// Builds SnippetApplyOptions from current property values.
    /// </summary>
    private SnippetApplyOptions BuildOptions() => InsertMode switch
    {
        SnippetInsertMode.ReplaceFile => SnippetApplyOptions.FullReplace() with
        {
            PreserveIndentation = PreserveIndentation,
            AddBlankLineBefore = AddBlankLineBefore,
            AddBlankLineAfter = AddBlankLineAfter
        },

        SnippetInsertMode.Replace => SnippetApplyOptions.ReplaceLines(StartLine, EndLine) with
        {
            PreserveIndentation = PreserveIndentation,
            AddBlankLineBefore = AddBlankLineBefore,
            AddBlankLineAfter = AddBlankLineAfter
        },

        SnippetInsertMode.InsertBefore => SnippetApplyOptions.InsertBeforeLine(TargetLine) with
        {
            PreserveIndentation = PreserveIndentation,
            AddBlankLineBefore = AddBlankLineBefore,
            AddBlankLineAfter = AddBlankLineAfter
        },

        SnippetInsertMode.InsertAfter => SnippetApplyOptions.InsertAfterLine(TargetLine) with
        {
            PreserveIndentation = PreserveIndentation,
            AddBlankLineBefore = AddBlankLineBefore,
            AddBlankLineAfter = AddBlankLineAfter
        },

        SnippetInsertMode.Append => SnippetApplyOptions.AppendToFile(AddBlankLineBefore) with
        {
            PreserveIndentation = PreserveIndentation,
            AddBlankLineAfter = AddBlankLineAfter
        },

        SnippetInsertMode.Prepend => SnippetApplyOptions.PrependToFile(AddBlankLineAfter) with
        {
            PreserveIndentation = PreserveIndentation,
            AddBlankLineBefore = AddBlankLineBefore
        },

        _ => throw new InvalidOperationException($"Unknown insert mode: {InsertMode}")
    };
}
