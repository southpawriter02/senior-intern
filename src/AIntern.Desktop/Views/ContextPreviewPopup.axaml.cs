namespace AIntern.Desktop.Views;

using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using AIntern.Desktop.Services;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Code-behind for ContextPreviewPopup control.
/// Handles syntax highlighting, clipboard, and keyboard events.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4e.</para>
/// </remarks>
public partial class ContextPreviewPopup : UserControl
{
    private SyntaxHighlightingService? _syntaxService;

    #region Styled Properties

    /// <summary>
    /// Command to close the preview.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<ContextPreviewPopup, ICommand?>(nameof(CloseCommand));

    /// <summary>
    /// Command to open context in editor.
    /// </summary>
    public static readonly StyledProperty<ICommand?> OpenInEditorCommandProperty =
        AvaloniaProperty.Register<ContextPreviewPopup, ICommand?>(nameof(OpenInEditorCommand));

    /// <summary>
    /// Command to remove the context.
    /// </summary>
    public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
        AvaloniaProperty.Register<ContextPreviewPopup, ICommand?>(nameof(RemoveCommand));

    /// <summary>
    /// Gets or sets the close command.
    /// </summary>
    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the open in editor command.
    /// </summary>
    public ICommand? OpenInEditorCommand
    {
        get => GetValue(OpenInEditorCommandProperty);
        set => SetValue(OpenInEditorCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the remove command.
    /// </summary>
    public ICommand? RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    #endregion

    /// <summary>
    /// Command to copy content to clipboard.
    /// </summary>
    public ICommand CopyContentCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ContextPreviewPopup"/>.
    /// </summary>
    public ContextPreviewPopup()
    {
        InitializeComponent();

        CopyContentCommand = new RelayCommand(CopyContent);
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Initializes the preview popup with the syntax highlighting service.
    /// </summary>
    /// <param name="syntaxService">The syntax highlighting service.</param>
    public void Initialize(SyntaxHighlightingService syntaxService)
    {
        _syntaxService = syntaxService;
    }

    /// <summary>
    /// Handles DataContext changes to update the editor content.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not FileContextViewModel context)
        {
            return;
        }

        // Set content
        PreviewEditor.Text = context.Content;

        // Apply syntax highlighting
        if (_syntaxService != null && !string.IsNullOrEmpty(context.Language))
        {
            _syntaxService.ApplyHighlighting(PreviewEditor, context.Language);
        }

        // Reset scroll position
        PreviewEditor.TextArea.Caret.Offset = 0;

        // If selection, scroll to the start line
        if (context.StartLine.HasValue && context.EndLine.HasValue)
        {
            try
            {
                var lineNumber = Math.Min(context.StartLine.Value, PreviewEditor.Document.LineCount);
                if (lineNumber > 0)
                {
                    var line = PreviewEditor.Document.GetLineByNumber(lineNumber);
                    PreviewEditor.TextArea.Caret.Offset = line.Offset;
                    PreviewEditor.TextArea.Caret.BringCaretToView();
                }
            }
            catch
            {
                // Ignore scroll errors
            }
        }
    }

    /// <summary>
    /// Copies the content to clipboard.
    /// </summary>
    private async void CopyContent()
    {
        if (DataContext is not FileContextViewModel context)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(context.Content);
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            CloseCommand?.Execute(null);
            e.Handled = true;
        }
    }
}
