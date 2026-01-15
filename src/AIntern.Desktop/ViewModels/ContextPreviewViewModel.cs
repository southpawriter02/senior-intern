namespace AIntern.Desktop.ViewModels;

using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for context preview management.
/// Controls the preview overlay visibility and selected context.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4e.</para>
/// </remarks>
public partial class ContextPreviewViewModel : ViewModelBase
{
    #region Fields

    private readonly ILogger<ContextPreviewViewModel>? _logger;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Whether the preview overlay is currently open.
    /// </summary>
    [ObservableProperty]
    private bool _isPreviewOpen;

    /// <summary>
    /// The context currently being previewed.
    /// </summary>
    [ObservableProperty]
    private FileContextViewModel? _selectedPreviewContext;

    #endregion

    #region Commands

    /// <summary>
    /// Command to show preview for a context.
    /// </summary>
    public ICommand ShowPreviewCommand { get; }

    /// <summary>
    /// Command to hide the preview.
    /// </summary>
    public ICommand HidePreviewCommand { get; }

    /// <summary>
    /// Command to open context file in editor.
    /// </summary>
    public ICommand? OpenContextFileCommand { get; set; }

    /// <summary>
    /// Command to remove selected context and close preview.
    /// </summary>
    public ICommand? RemoveSelectedContextCommand { get; set; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when a context should be opened in the editor.
    /// </summary>
    public event EventHandler<FileContextViewModel>? OpenInEditorRequested;

    /// <summary>
    /// Raised when a context should be removed.
    /// </summary>
    public event EventHandler<FileContextViewModel>? RemoveContextRequested;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="ContextPreviewViewModel"/>.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ContextPreviewViewModel(ILogger<ContextPreviewViewModel>? logger = null)
    {
        _logger = logger;

        ShowPreviewCommand = new RelayCommand<FileContextViewModel>(ShowPreview);
        HidePreviewCommand = new RelayCommand(HidePreview);
        OpenContextFileCommand = new RelayCommand(OpenInEditor);
        RemoveSelectedContextCommand = new RelayCommand(RemoveAndClose);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Shows preview for the specified context.
    /// </summary>
    /// <param name="context">The context to preview.</param>
    public void ShowPreview(FileContextViewModel? context)
    {
        if (context == null) return;

        SelectedPreviewContext = context;
        IsPreviewOpen = true;

        _logger?.LogDebug("[PREVIEW] Showing preview: {FileName} ({Lines} lines)",
            context.FileName, context.LineCount);
    }

    /// <summary>
    /// Hides the preview overlay.
    /// </summary>
    public void HidePreview()
    {
        IsPreviewOpen = false;
        SelectedPreviewContext = null;

        _logger?.LogDebug("[PREVIEW] Preview closed");
    }

    /// <summary>
    /// Opens the selected context in the editor.
    /// </summary>
    private void OpenInEditor()
    {
        if (SelectedPreviewContext == null) return;

        _logger?.LogDebug("[PREVIEW] Opening in editor: {FilePath}",
            SelectedPreviewContext.FilePath);

        OpenInEditorRequested?.Invoke(this, SelectedPreviewContext);
        HidePreview();
    }

    /// <summary>
    /// Removes the selected context and closes the preview.
    /// </summary>
    private void RemoveAndClose()
    {
        if (SelectedPreviewContext == null) return;

        _logger?.LogDebug("[PREVIEW] Removing context: {FileName}",
            SelectedPreviewContext.FileName);

        RemoveContextRequested?.Invoke(this, SelectedPreviewContext);
        HidePreview();
    }

    #endregion
}
