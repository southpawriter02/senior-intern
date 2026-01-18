// ============================================================================
// File: ITerminalSearchService.cs
// Path: src/AIntern.Core/Interfaces/ITerminalSearchService.cs
// Description: Interface for terminal buffer search functionality with plain text
//              and regex pattern support, result navigation, and viewport filtering.
// Created: 2026-01-18
// AI Intern v0.5.5b - Terminal Search Service
// ============================================================================

namespace AIntern.Core.Interfaces;

using AIntern.Core.Models.Terminal;

/// <summary>
/// Service for searching within terminal buffer content.
/// Supports plain text and regex patterns with result navigation.
/// </summary>
/// <remarks>
/// <para>
/// This service provides:
/// </para>
/// <list type="bullet">
///   <item><description>Async search with cancellation support</description></item>
///   <item><description>Plain text and regex pattern matching</description></item>
///   <item><description>Case-sensitive and case-insensitive modes</description></item>
///   <item><description>Result navigation with wrap-around</description></item>
///   <item><description>Viewport filtering for efficient rendering</description></item>
///   <item><description>Regex pattern validation</description></item>
/// </list>
/// <para>
/// Typical usage:
/// <code>
/// // Perform a search
/// var state = await searchService.SearchAsync(buffer, "error", options);
/// 
/// // Navigate through results
/// state = searchService.NavigateNext(state);
/// state = searchService.NavigatePrevious(state);
/// 
/// // Get visible results for rendering
/// var visible = searchService.GetVisibleResults(state, firstLine, lineCount);
/// </code>
/// </para>
/// <para>Added in v0.5.5b.</para>
/// </remarks>
public interface ITerminalSearchService
{
    /// <summary>
    /// Search for a pattern in the terminal buffer.
    /// </summary>
    /// <param name="buffer">The terminal buffer to search.</param>
    /// <param name="query">The search query (plain text or regex).</param>
    /// <param name="state">Current search state with options.</param>
    /// <param name="options">Search configuration options.</param>
    /// <param name="ct">Cancellation token for long-running searches.</param>
    /// <returns>Updated search state with results.</returns>
    /// <remarks>
    /// <para>
    /// The search runs on a background thread to avoid blocking the UI.
    /// Check <see cref="TerminalSearchState.IsSearching"/> for in-progress status.
    /// </para>
    /// <para>
    /// If the query is shorter than <see cref="TerminalSearchOptions.MinQueryLength"/>,
    /// an empty result set is returned immediately.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="buffer"/>, <paramref name="state"/>, or 
    /// <paramref name="options"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the cancellation token is triggered.
    /// </exception>
    Task<TerminalSearchState> SearchAsync(
        TerminalBuffer buffer,
        string query,
        TerminalSearchState state,
        TerminalSearchOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Perform incremental search with debouncing support.
    /// </summary>
    /// <param name="buffer">The terminal buffer to search.</param>
    /// <param name="query">The search query.</param>
    /// <param name="options">Search configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>New search state with results.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that creates a fresh search state using
    /// the default options from <paramref name="options"/> and performs the search.
    /// </para>
    /// <para>
    /// Use this for incremental search where the previous state is not needed.
    /// </para>
    /// </remarks>
    Task<TerminalSearchState> IncrementalSearchAsync(
        TerminalBuffer buffer,
        string query,
        TerminalSearchOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Navigate to the next search result.
    /// </summary>
    /// <param name="state">Current search state.</param>
    /// <returns>Updated state with new current index.</returns>
    /// <remarks>
    /// <para>
    /// Respects the <see cref="TerminalSearchState.WrapAround"/> setting.
    /// If wrap-around is enabled and at the last result, navigation wraps to the first.
    /// If disabled, stays at the last result.
    /// </para>
    /// <para>
    /// If there are no results, returns the same state unchanged.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="state"/> is null.
    /// </exception>
    TerminalSearchState NavigateNext(TerminalSearchState state);

    /// <summary>
    /// Navigate to the previous search result.
    /// </summary>
    /// <param name="state">Current search state.</param>
    /// <returns>Updated state with new current index.</returns>
    /// <remarks>
    /// <para>
    /// Respects the <see cref="TerminalSearchState.WrapAround"/> setting.
    /// If wrap-around is enabled and at the first result, navigation wraps to the last.
    /// If disabled, stays at the first result.
    /// </para>
    /// <para>
    /// If there are no results, returns the same state unchanged.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="state"/> is null.
    /// </exception>
    TerminalSearchState NavigatePrevious(TerminalSearchState state);

    /// <summary>
    /// Navigate to a specific result by index.
    /// </summary>
    /// <param name="state">Current search state.</param>
    /// <param name="index">Target result index (will be clamped to valid range).</param>
    /// <returns>Updated state with new current index.</returns>
    /// <remarks>
    /// The index is clamped to [0, ResultCount - 1]. Negative values become 0,
    /// values greater than or equal to ResultCount become the last valid index.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="state"/> is null.
    /// </exception>
    TerminalSearchState NavigateToIndex(TerminalSearchState state, int index);

    /// <summary>
    /// Navigate to the result nearest to a specific line.
    /// </summary>
    /// <param name="state">Current search state.</param>
    /// <param name="lineIndex">Target line index.</param>
    /// <param name="direction">Search direction for nearest result.</param>
    /// <returns>Updated state with current index set to nearest result.</returns>
    /// <remarks>
    /// <para>
    /// Finds the result closest to <paramref name="lineIndex"/>, preferring
    /// results in the specified <paramref name="direction"/>.
    /// </para>
    /// <para>
    /// Useful for navigating to results near the current scroll position.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="state"/> is null.
    /// </exception>
    TerminalSearchState NavigateToLine(
        TerminalSearchState state, 
        int lineIndex,
        SearchDirection direction = SearchDirection.Forward);

    /// <summary>
    /// Clear the current search state.
    /// </summary>
    /// <returns>Empty search state.</returns>
    /// <remarks>
    /// Returns <see cref="TerminalSearchState.Empty"/> to reset the search.
    /// </remarks>
    TerminalSearchState ClearSearch();

    /// <summary>
    /// Validate a regular expression pattern.
    /// </summary>
    /// <param name="pattern">The regex pattern to validate.</param>
    /// <returns>Error message if invalid, null if valid.</returns>
    /// <remarks>
    /// <para>
    /// Use this to validate patterns before attempting search to provide
    /// immediate feedback to the user.
    /// </para>
    /// <para>
    /// Returns null for empty or null patterns (considered valid - will match nothing).
    /// </para>
    /// </remarks>
    string? ValidateRegexPattern(string pattern);

    /// <summary>
    /// Get highlight positions for visible results (for rendering).
    /// </summary>
    /// <param name="state">Current search state.</param>
    /// <param name="firstVisibleLine">First visible line index.</param>
    /// <param name="visibleLineCount">Number of visible lines.</param>
    /// <returns>Results within the visible viewport.</returns>
    /// <remarks>
    /// <para>
    /// Filters results to only those visible in the current viewport,
    /// optimizing rendering performance.
    /// </para>
    /// <para>
    /// Results are returned in their original order (by position in buffer).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="state"/> is null.
    /// </exception>
    IReadOnlyList<TerminalSearchResult> GetVisibleResults(
        TerminalSearchState state,
        int firstVisibleLine,
        int visibleLineCount);
}
