namespace AIntern.Desktop.Views;

using Avalonia.Controls;
using Avalonia.Input;
using AIntern.Core.Interfaces;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Quick file navigation dialog with fuzzy search.
/// </summary>
/// <remarks>
/// <para>Triggered via Ctrl+P for fast file access.</para>
/// <para>Added in v0.3.5c.</para>
/// </remarks>
public partial class QuickOpenDialog : Window
{
    private QuickOpenViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="QuickOpenDialog"/>.
    /// </summary>
    public QuickOpenDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the dialog with the file index service.
    /// </summary>
    /// <param name="fileIndexService">The file index service.</param>
    /// <param name="logger">Optional logger.</param>
    public void Initialize(IFileIndexService fileIndexService, ILogger<QuickOpenViewModel>? logger = null)
    {
        _viewModel = new QuickOpenViewModel(
            fileIndexService,
            logger ?? NullLogger<QuickOpenViewModel>.Instance);

        // Subscribe to close events
        _viewModel.FileSelected += (s, filePath) => Close(filePath);
        _viewModel.CloseRequested += (s, e) => Close(null);

        DataContext = _viewModel;
    }

    /// <inheritdoc />
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Focus the search input
        var searchInput = this.FindControl<TextBox>("SearchInput");
        searchInput?.Focus();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_viewModel == null)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Up:
                _viewModel.MoveUpCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Down:
                _viewModel.MoveDownCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Enter:
                _viewModel.ConfirmCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                _viewModel.CancelCommand.Execute(null);
                e.Handled = true;
                break;

            default:
                base.OnKeyDown(e);
                break;
        }
    }

    /// <summary>
    /// Shows the Quick Open dialog and returns the selected file path.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <param name="fileIndexService">The file index service.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>Selected file path, or null if cancelled.</returns>
    public static async Task<string?> ShowDialogAsync(
        Window owner,
        IFileIndexService fileIndexService,
        ILogger<QuickOpenViewModel>? logger = null)
    {
        var dialog = new QuickOpenDialog();
        dialog.Initialize(fileIndexService, logger);

        return await dialog.ShowDialog<string?>(owner);
    }
}
