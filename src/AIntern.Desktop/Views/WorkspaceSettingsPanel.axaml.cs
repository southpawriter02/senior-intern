namespace AIntern.Desktop.Views;

using Avalonia.Controls;
using AIntern.Core.Interfaces;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Settings dialog window for workspace-related configuration.
/// </summary>
/// <remarks>
/// <para>
/// Organizes settings into File Explorer, Editor, and Context Attachment sections
/// with appropriate controls for each setting type.
/// </para>
/// <para>Added in v0.3.5b.</para>
/// </remarks>
public partial class WorkspaceSettingsPanel : Window
{
    /// <summary>
    /// Initializes a new instance of <see cref="WorkspaceSettingsPanel"/>.
    /// </summary>
    public WorkspaceSettingsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the dialog with the settings service.
    /// </summary>
    /// <param name="settingsService">The settings service for loading and saving settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public void Initialize(ISettingsService settingsService, ILogger<WorkspaceSettingsViewModel>? logger = null)
    {
        var viewModel = new WorkspaceSettingsViewModel(
            settingsService,
            logger ?? NullLogger<WorkspaceSettingsViewModel>.Instance);

        // Subscribe to close events
        viewModel.SaveCompleted += (s, e) => Close(true);
        viewModel.CancelRequested += (s, e) => Close(false);

        DataContext = viewModel;
    }

    /// <summary>
    /// Shows the dialog and returns whether save was clicked.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <param name="settingsService">The settings service.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>True if Save was clicked, false if Cancel/closed.</returns>
    public static async Task<bool> ShowDialogAsync(
        Window owner,
        ISettingsService settingsService,
        ILogger<WorkspaceSettingsViewModel>? logger = null)
    {
        var dialog = new WorkspaceSettingsPanel();
        dialog.Initialize(settingsService, logger);

        var result = await dialog.ShowDialog<bool?>(owner);
        return result == true;
    }
}
