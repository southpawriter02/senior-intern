// ============================================================================
// File: TerminalSearchBarViewModel.cs
// Path: src/AIntern.Desktop/ViewModels/TerminalSearchBarViewModel.cs
// Description: ViewModel for the terminal search bar with debounced incremental
//              search, navigation commands, and option toggles.
// Created: 2026-01-18
// AI Intern v0.5.5c - Terminal Search UI
// ============================================================================

namespace AIntern.Desktop.ViewModels;

using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalSearchBarViewModel (v0.5.5c)                                     │
// │ Manages terminal search bar state, debounced search, and navigation.    │
// │                                                                          │
// │ Features:                                                                │
// │ - Debounced incremental search (configurable delay)                      │
// │ - Case sensitivity and regex mode toggles                               │
// │ - Result navigation with automatic scroll-to-result                     │
// │ - Cancellation of in-progress searches                                   │
// │ - Error message display for invalid regex patterns                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for the terminal search bar UI component.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel provides:
/// </para>
/// <list type="bullet">
///   <item><description>Debounced search on query input changes</description></item>
///   <item><description>Case sensitivity and regex toggles that re-trigger search</description></item>
///   <item><description>Next/Previous result navigation commands</description></item>
///   <item><description>ScrollToResultRequested event for viewport scrolling</description></item>
///   <item><description>Error display for invalid regex patterns</description></item>
///   <item><description>Loading state during async searches</description></item>
/// </list>
/// <para>
/// Typical usage in XAML:
/// <code>
/// &lt;views:TerminalSearchBar DataContext="{Binding SearchBarViewModel}" /&gt;
/// </code>
/// </para>
/// <para>Added in v0.5.5c.</para>
/// </remarks>
public partial class TerminalSearchBarViewModel : ViewModelBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════
    // Private Fields
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Service for executing searches.</summary>
    private readonly ITerminalSearchService _searchService;

    /// <summary>Search configuration options.</summary>
    private readonly TerminalSearchOptions _options;

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<TerminalSearchBarViewModel>? _logger;

    /// <summary>The terminal buffer to search within.</summary>
    private TerminalBuffer? _buffer;

    /// <summary>Cancellation token source for current search.</summary>
    private CancellationTokenSource? _searchCts;

    /// <summary>Timer for debouncing search input.</summary>
    private readonly System.Timers.Timer _debounceTimer;

    /// <summary>Flag to prevent timer events after disposal.</summary>
    private bool _disposed;

    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The current search query text.
    /// </summary>
    /// <remarks>
    /// Changes to this property trigger a debounced search after
    /// <see cref="TerminalSearchOptions.DebounceDelayMs"/> milliseconds.
    /// </remarks>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Whether the search bar is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isSearchVisible;

    /// <summary>
    /// Whether search is case-sensitive.
    /// </summary>
    /// <remarks>
    /// Changes to this property immediately re-trigger the search.
    /// </remarks>
    [ObservableProperty]
    private bool _caseSensitive;

    /// <summary>
    /// Whether to use regular expressions for search.
    /// </summary>
    /// <remarks>
    /// Changes to this property immediately re-trigger the search.
    /// Invalid regex patterns will surface as <see cref="ErrorMessage"/>.
    /// </remarks>
    [ObservableProperty]
    private bool _useRegex;

    /// <summary>
    /// The current search state containing results.
    /// </summary>
    [ObservableProperty]
    private TerminalSearchState _searchState = TerminalSearchState.Empty;

    /// <summary>
    /// Whether a search operation is currently in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isSearching;

    /// <summary>
    /// Text displaying the current result position (e.g., "3 of 15").
    /// </summary>
    [ObservableProperty]
    private string _searchResultsText = string.Empty;

    /// <summary>
    /// Error message to display (e.g., invalid regex pattern).
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new <see cref="TerminalSearchBarViewModel"/>.
    /// </summary>
    /// <param name="searchService">The terminal search service.</param>
    /// <param name="options">Optional search configuration (defaults to TerminalSearchOptions.Default).</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="searchService"/> is null.</exception>
    public TerminalSearchBarViewModel(
        ITerminalSearchService searchService,
        TerminalSearchOptions? options = null,
        ILogger<TerminalSearchBarViewModel>? logger = null)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _options = options ?? TerminalSearchOptions.Default;
        _logger = logger;

        // ─────────────────────────────────────────────────────────────────
        // Initialize Debounce Timer
        // ─────────────────────────────────────────────────────────────────
        // The debounce timer prevents excessive search operations during
        // rapid typing. It resets on each keystroke and fires once typing
        // pauses for the configured delay.
        _debounceTimer = new System.Timers.Timer(_options.DebounceDelayMs);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += OnDebounceTimerElapsed;

        _logger?.LogDebug(
            "TerminalSearchBarViewModel created with debounce delay: {DelayMs}ms",
            _options.DebounceDelayMs);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Buffer Connection
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets the terminal buffer to search within.
    /// </summary>
    /// <param name="buffer">The terminal buffer.</param>
    /// <remarks>
    /// Call this when the terminal session changes or becomes available.
    /// </remarks>
    public void SetBuffer(TerminalBuffer? buffer)
    {
        _buffer = buffer;
        
        _logger?.LogDebug(
            "Search buffer set: {HasBuffer}, TotalLines={Lines}",
            buffer != null,
            buffer?.TotalLines ?? 0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Property Change Handlers
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when <see cref="SearchQuery"/> changes.
    /// Resets and starts the debounce timer.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        // Stop any pending timer, then restart
        // This ensures we wait for typing to pause before searching
        _debounceTimer.Stop();
        _debounceTimer.Start();

        _logger?.LogDebug("Search query changed: \"{Query}\" - debounce started", value);
    }

    /// <summary>
    /// Called when <see cref="CaseSensitive"/> changes.
    /// Immediately triggers a new search.
    /// </summary>
    partial void OnCaseSensitiveChanged(bool value)
    {
        _logger?.LogDebug("Case sensitivity toggled: {Value}", value);
        TriggerSearchAsync();
    }

    /// <summary>
    /// Called when <see cref="UseRegex"/> changes.
    /// Immediately triggers a new search.
    /// </summary>
    partial void OnUseRegexChanged(bool value)
    {
        _logger?.LogDebug("Regex mode toggled: {Value}", value);
        TriggerSearchAsync();
    }

    /// <summary>
    /// Called when <see cref="IsSearchVisible"/> changes.
    /// Raises open/close events.
    /// </summary>
    partial void OnIsSearchVisibleChanged(bool value)
    {
        if (value)
        {
            _logger?.LogDebug("Search bar opened");
            SearchOpened?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _logger?.LogDebug("Search bar closed");
            SearchClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Debounced Search
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles the debounce timer elapsed event.
    /// Dispatches to UI thread and triggers search.
    /// </summary>
    private void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // Guard against timer firing after disposal
        if (_disposed) return;

        // Dispatch search to UI thread since we'll update bound properties
        Dispatcher.UIThread.Post(() => TriggerSearchAsync());
    }

    /// <summary>
    /// Executes the search with current query and options.
    /// Cancels any in-progress search first.
    /// </summary>
    private async void TriggerSearchAsync()
    {
        // ─────────────────────────────────────────────────────────────────
        // Validation
        // ─────────────────────────────────────────────────────────────────
        if (_buffer == null)
        {
            _logger?.LogDebug("Search skipped: no buffer set");
            return;
        }

        // ─────────────────────────────────────────────────────────────────
        // Cancel Previous Search
        // ─────────────────────────────────────────────────────────────────
        // If a previous search is still running, cancel it before starting new
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        try
        {
            IsSearching = true;
            ErrorMessage = null;

            _logger?.LogDebug(
                "Starting search: Query=\"{Query}\", CaseSensitive={Case}, UseRegex={Regex}",
                SearchQuery,
                CaseSensitive,
                UseRegex);

            // ─────────────────────────────────────────────────────────────
            // Build Search State
            // ─────────────────────────────────────────────────────────────
            var state = TerminalSearchState.ForQuery(SearchQuery) with
            {
                CaseSensitive = CaseSensitive,
                UseRegex = UseRegex,
                WrapAround = true,
                IncludeScrollback = true
            };

            // ─────────────────────────────────────────────────────────────
            // Execute Search
            // ─────────────────────────────────────────────────────────────
            SearchState = await _searchService.SearchAsync(
                _buffer,
                SearchQuery,
                state,
                _options,
                ct);

            // ─────────────────────────────────────────────────────────────
            // Update UI State
            // ─────────────────────────────────────────────────────────────
            UpdateResultsText();
            ErrorMessage = SearchState.ErrorMessage;

            _logger?.LogDebug(
                "Search completed: {Count} results, CurrentIndex={Index}",
                SearchState.Results.Count,
                SearchState.CurrentResultIndex);

            // Request scroll to current result if any
            if (SearchState.CurrentResult != null)
            {
                ScrollToResultRequested?.Invoke(this, SearchState.CurrentResult);
            }
        }
        catch (OperationCanceledException)
        {
            // Search was cancelled by a newer search, this is expected
            _logger?.LogDebug("Search cancelled: Query=\"{Query}\"", SearchQuery);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Search failed: Query=\"{Query}\"", SearchQuery);
            ErrorMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Updates the results text to reflect current state.
    /// </summary>
    private void UpdateResultsText()
    {
        SearchResultsText = SearchState.ResultsSummary;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates to the next search result.
    /// </summary>
    [RelayCommand]
    private void NextSearchResult()
    {
        if (!SearchState.HasResults)
        {
            _logger?.LogDebug("NextSearchResult: No results to navigate");
            return;
        }

        SearchState = _searchService.NavigateNext(SearchState);
        UpdateResultsText();

        _logger?.LogDebug(
            "Navigated to next result: {Index} of {Count}",
            SearchState.CurrentResultIndex + 1,
            SearchState.Results.Count);

        if (SearchState.CurrentResult != null)
        {
            ScrollToResultRequested?.Invoke(this, SearchState.CurrentResult);
        }
    }

    /// <summary>
    /// Navigates to the previous search result.
    /// </summary>
    [RelayCommand]
    private void PreviousSearchResult()
    {
        if (!SearchState.HasResults)
        {
            _logger?.LogDebug("PreviousSearchResult: No results to navigate");
            return;
        }

        SearchState = _searchService.NavigatePrevious(SearchState);
        UpdateResultsText();

        _logger?.LogDebug(
            "Navigated to previous result: {Index} of {Count}",
            SearchState.CurrentResultIndex + 1,
            SearchState.Results.Count);

        if (SearchState.CurrentResult != null)
        {
            ScrollToResultRequested?.Invoke(this, SearchState.CurrentResult);
        }
    }

    /// <summary>
    /// Closes the search bar and clears the search state.
    /// </summary>
    [RelayCommand]
    private void CloseSearch()
    {
        _logger?.LogDebug("Closing search bar");

        IsSearchVisible = false;
        SearchQuery = string.Empty;
        SearchState = _searchService.ClearSearch();
        ErrorMessage = null;
        UpdateResultsText();
    }

    /// <summary>
    /// Toggles case sensitivity and re-searches.
    /// </summary>
    [RelayCommand]
    private void ToggleCaseSensitive()
    {
        CaseSensitive = !CaseSensitive;
    }

    /// <summary>
    /// Toggles regex mode and re-searches.
    /// </summary>
    [RelayCommand]
    private void ToggleRegex()
    {
        UseRegex = !UseRegex;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Opens the search bar.
    /// </summary>
    /// <remarks>
    /// The view should focus the search input when this is called.
    /// </remarks>
    public void OpenSearch()
    {
        _logger?.LogDebug("Opening search bar");
        IsSearchVisible = true;
    }

    /// <summary>
    /// Navigates to the result nearest to a specific line.
    /// </summary>
    /// <param name="lineIndex">The target line index.</param>
    /// <remarks>
    /// Useful for scrollbar marker clicks or other line-based navigation.
    /// </remarks>
    public void NavigateToLine(int lineIndex)
    {
        if (!SearchState.HasResults)
        {
            _logger?.LogDebug("NavigateToLine: No results to navigate");
            return;
        }

        SearchState = _searchService.NavigateToLine(
            SearchState,
            lineIndex,
            SearchDirection.Forward);

        UpdateResultsText();

        _logger?.LogDebug(
            "Navigated to line {Line}: Result {Index}",
            lineIndex,
            SearchState.CurrentResultIndex);

        if (SearchState.CurrentResult != null)
        {
            ScrollToResultRequested?.Invoke(this, SearchState.CurrentResult);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when the terminal should scroll to show a search result.
    /// </summary>
    /// <remarks>
    /// The view should subscribe to this event and scroll the terminal
    /// viewport to make the result visible.
    /// </remarks>
    public event EventHandler<TerminalSearchResult>? ScrollToResultRequested;

    /// <summary>
    /// Raised when the search bar is opened.
    /// </summary>
    /// <remarks>
    /// The view should focus the search input when this event fires.
    /// </remarks>
    public event EventHandler? SearchOpened;

    /// <summary>
    /// Raised when the search bar is closed.
    /// </summary>
    /// <remarks>
    /// The view can use this to return focus to the terminal.
    /// </remarks>
    public event EventHandler? SearchClosed;

    // ═══════════════════════════════════════════════════════════════════════
    // Cleanup
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Disposes of managed resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger?.LogDebug("Disposing TerminalSearchBarViewModel");

        // Stop and dispose the debounce timer
        _debounceTimer.Stop();
        _debounceTimer.Elapsed -= OnDebounceTimerElapsed;
        _debounceTimer.Dispose();

        // Cancel and dispose the search cancellation token
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
