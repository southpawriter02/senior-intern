namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for the Quick Open dialog.
/// </summary>
/// <remarks>Added in v0.3.5c.</remarks>
public partial class QuickOpenViewModel : ViewModelBase
{
    private readonly IFileIndexService _fileIndexService;
    private readonly ILogger<QuickOpenViewModel> _logger;
    private CancellationTokenSource? _searchCts;

    /// <summary>
    /// Current search query.
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Search results collection.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<QuickOpenItemViewModel> _results = new();

    /// <summary>
    /// Currently selected item.
    /// </summary>
    [ObservableProperty]
    private QuickOpenItemViewModel? _selectedItem;

    /// <summary>
    /// Index of selected item.
    /// </summary>
    [ObservableProperty]
    private int _selectedIndex;

    /// <summary>
    /// Whether search is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Status text for the footer.
    /// </summary>
    [ObservableProperty]
    private string? _statusText;

    /// <summary>
    /// Raised when a file is selected.
    /// </summary>
    public event EventHandler<string>? FileSelected;

    /// <summary>
    /// Raised when close is requested.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Creates a new QuickOpenViewModel.
    /// </summary>
    /// <param name="fileIndexService">File index service.</param>
    /// <param name="logger">Logger instance.</param>
    public QuickOpenViewModel(IFileIndexService fileIndexService, ILogger<QuickOpenViewModel> logger)
    {
        _fileIndexService = fileIndexService;
        _logger = logger;

        _logger.LogDebug("[INIT] QuickOpenViewModel created");
        LoadInitialResults();
    }

    private void LoadInitialResults()
    {
        _logger.LogDebug("[ENTER] LoadInitialResults");

        Results.Clear();

        // Get recent files
        var recentFiles = _fileIndexService.GetRecentFiles(10);

        foreach (var filePath in recentFiles)
        {
            var searchResults = _fileIndexService.Search(string.Empty, 100);
            var match = searchResults.FirstOrDefault(r => r.FilePath == filePath);

            if (match != null)
            {
                Results.Add(new QuickOpenItemViewModel(match, isRecent: true));
            }
        }

        // If no recent files, show some indexed files
        if (Results.Count == 0)
        {
            var allFiles = _fileIndexService.Search(string.Empty, 10);
            foreach (var result in allFiles)
            {
                Results.Add(new QuickOpenItemViewModel(result));
            }
        }

        StatusText = _fileIndexService.IsIndexed
            ? $"{_fileIndexService.IndexedFileCount:N0} files indexed"
            : "Indexing...";

        if (Results.Count > 0)
            SelectedIndex = 0;
    }

    partial void OnSearchQueryChanged(string value)
    {
        _logger.LogDebug("[SEARCH] Query changed to: '{Query}'", value);

        // Cancel previous search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        // Debounce search
        _ = SearchAsync(value, _searchCts.Token);
    }

    private async Task SearchAsync(string query, CancellationToken ct)
    {
        // Short debounce
        await Task.Delay(50, ct);
        if (ct.IsCancellationRequested) return;

        IsLoading = true;

        try
        {
            var results = _fileIndexService.Search(query, 20);

            if (ct.IsCancellationRequested) return;

            Results.Clear();
            foreach (var result in results)
            {
                Results.Add(new QuickOpenItemViewModel(result));
            }

            if (Results.Count > 0)
                SelectedIndex = 0;

            StatusText = Results.Count > 0
                ? $"{Results.Count} results"
                : "No matching files";
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Moves selection up.
    /// </summary>
    [RelayCommand]
    public void MoveUp()
    {
        if (Results.Count == 0) return;
        SelectedIndex = (SelectedIndex - 1 + Results.Count) % Results.Count;
    }

    /// <summary>
    /// Moves selection down.
    /// </summary>
    [RelayCommand]
    public void MoveDown()
    {
        if (Results.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Results.Count;
    }

    /// <summary>
    /// Confirms selection and opens file.
    /// </summary>
    [RelayCommand]
    public void Confirm()
    {
        var selected = SelectedIndex >= 0 && SelectedIndex < Results.Count
            ? Results[SelectedIndex]
            : null;

        if (selected != null)
        {
            _logger.LogInformation("[OPEN] File selected: {Path}", selected.FilePath);
            _fileIndexService.AddToRecent(selected.FilePath);
            FileSelected?.Invoke(this, selected.FilePath);
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Cancels and closes the dialog.
    /// </summary>
    [RelayCommand]
    public void Cancel()
    {
        _logger.LogDebug("[CANCEL] Dialog cancelled");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
