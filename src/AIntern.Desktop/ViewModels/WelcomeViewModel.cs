namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;

/// <summary>
/// ViewModel for the welcome screen shown when no workspace is open (v0.3.5h).
/// </summary>
public partial class WelcomeViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<WelcomeViewModel>? _logger;

    /// <summary>
    /// Collection of recent workspaces for display.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RecentWorkspaceInfo> _recentWorkspaces = new();

    /// <summary>
    /// Whether there are recent workspaces to display.
    /// </summary>
    [ObservableProperty]
    private bool _hasRecentWorkspaces;

    /// <summary>
    /// Raised when user wants to open a workspace.
    /// </summary>
    public event EventHandler<string>? WorkspaceOpenRequested;

    /// <summary>
    /// Raised when user wants to create a new file.
    /// </summary>
    public event EventHandler? NewFileRequested;

    /// <summary>
    /// Initializes the WelcomeViewModel.
    /// </summary>
    public WelcomeViewModel(
        IWorkspaceService workspaceService,
        ILogger<WelcomeViewModel>? logger = null)
    {
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _logger = logger;
        _logger?.LogDebug("[INIT] WelcomeViewModel created");
    }

    /// <summary>
    /// Loads recent workspaces for display.
    /// </summary>
    public async Task LoadAsync()
    {
        _logger?.LogDebug("[ENTER] WelcomeViewModel.LoadAsync");

        try
        {
            var recent = await _workspaceService.GetRecentWorkspacesAsync(5);

            RecentWorkspaces.Clear();
            foreach (var workspace in recent)
            {
                RecentWorkspaces.Add(new RecentWorkspaceInfo
                {
                    DisplayName = workspace.DisplayName,
                    RootPath = workspace.RootPath
                });
            }

            HasRecentWorkspaces = RecentWorkspaces.Count > 0;
            _logger?.LogDebug("[INFO] Loaded {Count} recent workspaces", RecentWorkspaces.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ERROR] Failed to load recent workspaces");
        }
    }

    /// <summary>
    /// Opens the folder picker dialog.
    /// </summary>
    [RelayCommand]
    private void OpenFolder()
    {
        _logger?.LogDebug("[CMD] OpenFolder");
        WorkspaceOpenRequested?.Invoke(this, string.Empty);
    }

    /// <summary>
    /// Creates a new untitled file.
    /// </summary>
    [RelayCommand]
    private void NewFile()
    {
        _logger?.LogDebug("[CMD] NewFile");
        NewFileRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens a recent workspace by path.
    /// </summary>
    [RelayCommand]
    private void OpenRecent(string rootPath)
    {
        _logger?.LogDebug("[CMD] OpenRecent: {Path}", rootPath);
        WorkspaceOpenRequested?.Invoke(this, rootPath);
    }
}

/// <summary>
/// Display info for a recent workspace.
/// </summary>
public sealed class RecentWorkspaceInfo
{
    /// <summary>
    /// Display name of the workspace.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Root path of the workspace.
    /// </summary>
    public string RootPath { get; init; } = string.Empty;
}
