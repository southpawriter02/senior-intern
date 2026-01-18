// ============================================================================
// File: TerminalSearchService.cs
// Path: src/AIntern.Services/Terminal/TerminalSearchService.cs
// Description: Implementation of terminal buffer search with plain text and regex
//              pattern matching, navigation, and viewport filtering.
// Created: 2026-01-18
// AI Intern v0.5.5b - Terminal Search Service
// ============================================================================

namespace AIntern.Services.Terminal;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalSearchService (v0.5.5b)                                          │
// │ Provides search functionality within terminal buffer content.            │
// │                                                                          │
// │ Features:                                                                │
// │ - Plain text and regex pattern matching                                  │
// │ - Case-sensitive and case-insensitive modes                              │
// │ - Background thread search with cancellation support                     │
// │ - Result navigation with wrap-around                                     │
// │ - Viewport filtering for efficient rendering                             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for searching within terminal buffer content.
/// </summary>
/// <remarks>
/// <para>
/// This service supports:
/// </para>
/// <list type="bullet">
///   <item><description>Plain text search with case sensitivity options</description></item>
///   <item><description>Regex pattern matching with timeout protection</description></item>
///   <item><description>Background thread execution for responsive UI</description></item>
///   <item><description>Cancellation support for long-running searches</description></item>
///   <item><description>Result navigation with wrap-around</description></item>
///   <item><description>Viewport filtering for rendering optimization</description></item>
/// </list>
/// <para>
/// All search operations run on a background thread to avoid blocking the UI.
/// The service is thread-safe and can be used concurrently.
/// </para>
/// <para>Added in v0.5.5b.</para>
/// </remarks>
public sealed class TerminalSearchService : ITerminalSearchService
{
    // ═══════════════════════════════════════════════════════════════════════
    // Private Fields
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Logger for search operations.</summary>
    private readonly ILogger<TerminalSearchService> _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalSearchService"/> class.
    /// </summary>
    /// <param name="logger">Logger for search operation diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public TerminalSearchService(ILogger<TerminalSearchService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogDebug("TerminalSearchService initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Search Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<TerminalSearchState> SearchAsync(
        TerminalBuffer buffer,
        string query,
        TerminalSearchState state,
        TerminalSearchOptions options,
        CancellationToken ct = default)
    {
        // ─────────────────────────────────────────────────────────────────
        // Parameter Validation
        // ─────────────────────────────────────────────────────────────────
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogDebug(
            "SearchAsync started: Query=\"{Query}\", CaseSensitive={CaseSensitive}, " +
            "UseRegex={UseRegex}, IncludeScrollback={IncludeScrollback}",
            query,
            state.CaseSensitive,
            state.UseRegex,
            state.IncludeScrollback);

        // ─────────────────────────────────────────────────────────────────
        // Check Minimum Query Length
        // ─────────────────────────────────────────────────────────────────
        if (string.IsNullOrEmpty(query) || query.Length < options.MinQueryLength)
        {
            _logger.LogDebug(
                "Query too short: Length={Length}, MinLength={MinLength}",
                query?.Length ?? 0,
                options.MinQueryLength);

            // Return empty state with query preserved
            return state with
            {
                Query = query ?? string.Empty,
                Results = Array.Empty<TerminalSearchResult>(),
                CurrentResultIndex = -1,
                IsSearching = false,
                ErrorMessage = null
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // Create Searching State
        // ─────────────────────────────────────────────────────────────────
        var searchingState = state with
        {
            Query = query,
            IsSearching = true,
            ErrorMessage = null
        };

        try
        {
            // ─────────────────────────────────────────────────────────────
            // Validate Regex Pattern (if regex mode)
            // ─────────────────────────────────────────────────────────────
            if (state.UseRegex)
            {
                var error = ValidateRegexPattern(query);
                if (error != null)
                {
                    _logger.LogWarning(
                        "Invalid regex pattern: \"{Pattern}\", Error: {Error}",
                        query,
                        error);

                    return searchingState with
                    {
                        ErrorMessage = error,
                        IsSearching = false,
                        Results = Array.Empty<TerminalSearchResult>(),
                        CurrentResultIndex = -1
                    };
                }
            }

            // ─────────────────────────────────────────────────────────────
            // Execute Search on Background Thread
            // ─────────────────────────────────────────────────────────────
            var results = await Task.Run(
                () => PerformSearch(
                    buffer,
                    query,
                    state.CaseSensitive,
                    state.UseRegex,
                    state.IncludeScrollback,
                    options.MaxResults,
                    options.RegexTimeoutMs,
                    ct),
                ct);

            _logger.LogDebug(
                "Search completed: Query=\"{Query}\", ResultCount={Count}",
                query,
                results.Count);

            // ─────────────────────────────────────────────────────────────
            // Build Result State
            // ─────────────────────────────────────────────────────────────
            return searchingState with
            {
                Results = results,
                CurrentResultIndex = results.Count > 0 ? 0 : -1,
                IsSearching = false
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Search cancelled for query: \"{Query}\"", query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: \"{Query}\"", query);
            
            return searchingState with
            {
                ErrorMessage = ex.Message,
                IsSearching = false,
                Results = Array.Empty<TerminalSearchResult>(),
                CurrentResultIndex = -1
            };
        }
    }

    /// <inheritdoc />
    public async Task<TerminalSearchState> IncrementalSearchAsync(
        TerminalBuffer buffer,
        string query,
        TerminalSearchOptions options,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "IncrementalSearchAsync: Query=\"{Query}\"",
            query);

        // Create fresh state from default options
        var state = options.CreateInitialState() with
        {
            Query = query
        };

        return await SearchAsync(buffer, query, state, options, ct);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Core Search Logic
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Performs the actual search across buffer lines.
    /// </summary>
    /// <param name="buffer">The terminal buffer to search.</param>
    /// <param name="query">The search query.</param>
    /// <param name="caseSensitive">Whether to use case-sensitive matching.</param>
    /// <param name="useRegex">Whether to interpret query as regex.</param>
    /// <param name="includeScrollback">Whether to include scrollback buffer.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="regexTimeoutMs">Timeout for regex operations in milliseconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of search results.</returns>
    private List<TerminalSearchResult> PerformSearch(
        TerminalBuffer buffer,
        string query,
        bool caseSensitive,
        bool useRegex,
        bool includeScrollback,
        int maxResults,
        int regexTimeoutMs,
        CancellationToken ct)
    {
        var results = new List<TerminalSearchResult>();

        // ─────────────────────────────────────────────────────────────────
        // Set up string comparison for plain text search
        // ─────────────────────────────────────────────────────────────────
        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        // ─────────────────────────────────────────────────────────────────
        // Compile regex pattern if needed
        // ─────────────────────────────────────────────────────────────────
        Regex? regex = null;
        if (useRegex)
        {
            var regexOptions = caseSensitive
                ? RegexOptions.None
                : RegexOptions.IgnoreCase;
            
            // Add compilation for performance on large buffers
            regexOptions |= RegexOptions.Compiled;

            regex = new Regex(
                query,
                regexOptions,
                TimeSpan.FromMilliseconds(regexTimeoutMs));
            
            _logger.LogDebug(
                "Compiled regex pattern with timeout: {TimeoutMs}ms",
                regexTimeoutMs);
        }

        // ─────────────────────────────────────────────────────────────────
        // Determine line range to search
        // ─────────────────────────────────────────────────────────────────
        int startLine = includeScrollback ? 0 : buffer.FirstVisibleLine;
        int endLine = buffer.TotalLineCount;

        _logger.LogDebug(
            "Searching lines {Start} to {End} (total: {Total})",
            startLine,
            endLine,
            endLine - startLine);

        // ─────────────────────────────────────────────────────────────────
        // Iterate through buffer lines
        // ─────────────────────────────────────────────────────────────────
        for (int lineIndex = startLine; lineIndex < endLine; lineIndex++)
        {
            // Check cancellation periodically (every 100 lines)
            // This balances responsiveness with performance overhead
            if (lineIndex % 100 == 0)
            {
                ct.ThrowIfCancellationRequested();
            }

            // Stop if we've reached max results limit
            if (results.Count >= maxResults)
            {
                _logger.LogDebug(
                    "Reached max results limit: {MaxResults}",
                    maxResults);
                break;
            }

            // Get line content
            var lineContent = buffer.GetLineText(lineIndex);
            if (string.IsNullOrEmpty(lineContent))
            {
                continue;
            }

            // ─────────────────────────────────────────────────────────────
            // Search the line using appropriate method
            // ─────────────────────────────────────────────────────────────
            if (useRegex && regex != null)
            {
                SearchLineWithRegex(
                    regex,
                    lineContent,
                    lineIndex,
                    results,
                    maxResults);
            }
            else
            {
                SearchLineWithText(
                    query,
                    lineContent,
                    lineIndex,
                    comparison,
                    results,
                    maxResults);
            }
        }

        return results;
    }

    /// <summary>
    /// Searches a single line using regex pattern matching.
    /// </summary>
    /// <param name="regex">The compiled regex pattern.</param>
    /// <param name="lineContent">The line text content.</param>
    /// <param name="lineIndex">The line index in the buffer.</param>
    /// <param name="results">Collection to add results to.</param>
    /// <param name="maxResults">Maximum number of results.</param>
    private void SearchLineWithRegex(
        Regex regex,
        string lineContent,
        int lineIndex,
        List<TerminalSearchResult> results,
        int maxResults)
    {
        try
        {
            // Find all matches on this line
            var matches = regex.Matches(lineContent);

            foreach (Match match in matches)
            {
                // Stop if we've hit max results
                if (results.Count >= maxResults)
                {
                    break;
                }

                // Skip empty matches (can happen with some patterns)
                if (match.Length == 0)
                {
                    continue;
                }

                results.Add(new TerminalSearchResult
                {
                    LineIndex = lineIndex,
                    StartColumn = match.Index,
                    Length = match.Length,
                    MatchedText = match.Value,
                    LineContent = lineContent
                });
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            // Log but continue - catastrophic backtracking protection
            _logger.LogWarning(
                ex,
                "Regex timeout on line {LineIndex}, skipping",
                lineIndex);
        }
    }

    /// <summary>
    /// Searches a single line using plain text matching.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="lineContent">The line text content.</param>
    /// <param name="lineIndex">The line index in the buffer.</param>
    /// <param name="comparison">String comparison mode.</param>
    /// <param name="results">Collection to add results to.</param>
    /// <param name="maxResults">Maximum number of results.</param>
    private void SearchLineWithText(
        string query,
        string lineContent,
        int lineIndex,
        StringComparison comparison,
        List<TerminalSearchResult> results,
        int maxResults)
    {
        int startIndex = 0;
        int index;

        // Find all occurrences on this line
        while ((index = lineContent.IndexOf(query, startIndex, comparison)) >= 0)
        {
            // Stop if we've hit max results
            if (results.Count >= maxResults)
            {
                break;
            }

            results.Add(new TerminalSearchResult
            {
                LineIndex = lineIndex,
                StartColumn = index,
                Length = query.Length,
                MatchedText = lineContent.Substring(index, query.Length),
                LineContent = lineContent
            });

            // Move past this match to find next occurrence
            startIndex = index + 1;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Navigation Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public TerminalSearchState NavigateNext(TerminalSearchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // No results to navigate
        if (!state.HasResults)
        {
            _logger.LogDebug("NavigateNext: No results to navigate");
            return state;
        }

        var newIndex = state.CurrentResultIndex + 1;

        // Handle wrap-around or clamp
        if (newIndex >= state.Results.Count)
        {
            newIndex = state.WrapAround ? 0 : state.Results.Count - 1;
        }

        _logger.LogDebug(
            "NavigateNext: {OldIndex} → {NewIndex} (WrapAround={WrapAround})",
            state.CurrentResultIndex,
            newIndex,
            state.WrapAround);

        return state with { CurrentResultIndex = newIndex };
    }

    /// <inheritdoc />
    public TerminalSearchState NavigatePrevious(TerminalSearchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // No results to navigate
        if (!state.HasResults)
        {
            _logger.LogDebug("NavigatePrevious: No results to navigate");
            return state;
        }

        var newIndex = state.CurrentResultIndex - 1;

        // Handle wrap-around or clamp
        if (newIndex < 0)
        {
            newIndex = state.WrapAround ? state.Results.Count - 1 : 0;
        }

        _logger.LogDebug(
            "NavigatePrevious: {OldIndex} → {NewIndex} (WrapAround={WrapAround})",
            state.CurrentResultIndex,
            newIndex,
            state.WrapAround);

        return state with { CurrentResultIndex = newIndex };
    }

    /// <inheritdoc />
    public TerminalSearchState NavigateToIndex(TerminalSearchState state, int index)
    {
        ArgumentNullException.ThrowIfNull(state);

        // No results to navigate
        if (!state.HasResults)
        {
            _logger.LogDebug("NavigateToIndex: No results to navigate");
            return state;
        }

        // Clamp index to valid range
        var clampedIndex = Math.Clamp(index, 0, state.Results.Count - 1);

        _logger.LogDebug(
            "NavigateToIndex: Requested={Requested}, Clamped={Clamped}",
            index,
            clampedIndex);

        return state with { CurrentResultIndex = clampedIndex };
    }

    /// <inheritdoc />
    public TerminalSearchState NavigateToLine(
        TerminalSearchState state,
        int lineIndex,
        SearchDirection direction = SearchDirection.Forward)
    {
        ArgumentNullException.ThrowIfNull(state);

        // No results to navigate
        if (!state.HasResults)
        {
            _logger.LogDebug("NavigateToLine: No results to navigate");
            return state;
        }

        // Use extension method to find nearest result
        var nearestIndex = state.FindNearestResultIndex(lineIndex, direction);

        if (nearestIndex >= 0)
        {
            _logger.LogDebug(
                "NavigateToLine: Line={Line}, Direction={Direction}, ResultIndex={Index}",
                lineIndex,
                direction,
                nearestIndex);

            return state with { CurrentResultIndex = nearestIndex };
        }

        _logger.LogDebug(
            "NavigateToLine: No result found near line {Line}",
            lineIndex);

        return state;
    }

    /// <inheritdoc />
    public TerminalSearchState ClearSearch()
    {
        _logger.LogDebug("ClearSearch: Resetting to empty state");
        return TerminalSearchState.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Validation Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string? ValidateRegexPattern(string pattern)
    {
        // Empty pattern is valid (matches nothing)
        if (string.IsNullOrEmpty(pattern))
        {
            return null;
        }

        try
        {
            // Try to create a regex with a short timeout
            // This validates the pattern syntax
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            
            _logger.LogDebug(
                "ValidateRegexPattern: Pattern \"{Pattern}\" is valid",
                pattern);
            
            return null;
        }
        catch (ArgumentException ex)
        {
            var error = $"Invalid regex: {ex.Message}";
            
            _logger.LogDebug(
                "ValidateRegexPattern: Pattern \"{Pattern}\" is invalid: {Error}",
                pattern,
                ex.Message);
            
            return error;
        }
        catch (RegexMatchTimeoutException)
        {
            var error = "Regex pattern is too complex";
            
            _logger.LogDebug(
                "ValidateRegexPattern: Pattern \"{Pattern}\" timed out",
                pattern);
            
            return error;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Viewport Filtering
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public IReadOnlyList<TerminalSearchResult> GetVisibleResults(
        TerminalSearchState state,
        int firstVisibleLine,
        int visibleLineCount)
    {
        ArgumentNullException.ThrowIfNull(state);

        // No results means no visible results
        if (!state.HasResults)
        {
            return Array.Empty<TerminalSearchResult>();
        }

        var lastVisibleLine = firstVisibleLine + visibleLineCount;

        // Filter results to only those in the visible viewport
        var visible = state.Results
            .Where(r => r.LineIndex >= firstVisibleLine && r.LineIndex < lastVisibleLine)
            .ToList();

        _logger.LogDebug(
            "GetVisibleResults: Lines {Start}-{End}, Visible={Count}/{Total}",
            firstVisibleLine,
            lastVisibleLine - 1,
            visible.Count,
            state.Results.Count);

        return visible;
    }
}
