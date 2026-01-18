using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;

namespace AIntern.Services.Tests.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSearchService"/>.
/// </summary>
/// <remarks>Added in v0.5.5b.</remarks>
public sealed class TerminalSearchServiceTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Test Fixtures
    // ═══════════════════════════════════════════════════════════════════════

    private readonly Mock<ILogger<TerminalSearchService>> _loggerMock;
    private readonly TerminalSearchService _service;
    private readonly TerminalSearchOptions _defaultOptions;

    public TerminalSearchServiceTests()
    {
        _loggerMock = new Mock<ILogger<TerminalSearchService>>();
        _service = new TerminalSearchService(_loggerMock.Object);
        _defaultOptions = TerminalSearchOptions.Default;
    }

    /// <summary>Creates a test buffer with predefined content.</summary>
    private static TerminalBuffer CreateTestBuffer(params string[] lines)
    {
        var buffer = new TerminalBuffer(80, lines.Length > 0 ? lines.Length : 10);
        foreach (var line in lines)
        {
            buffer.WriteString(line);
            buffer.LineFeed();
            buffer.CarriageReturn();
        }
        return buffer;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAsync - Query Validation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyState()
    {
        // Arrange
        var buffer = CreateTestBuffer("Hello World", "Test line");
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _service.SearchAsync(buffer, "", state, _defaultOptions);

        // Assert
        Assert.Empty(result.Results);
        Assert.Equal(-1, result.CurrentResultIndex);
        Assert.False(result.IsSearching);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SearchAsync_NullQuery_ReturnsEmptyState()
    {
        // Arrange
        var buffer = CreateTestBuffer("Hello World");
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _service.SearchAsync(buffer, null!, state, _defaultOptions);

        // Assert
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task SearchAsync_QueryTooShort_ReturnsEmptyState()
    {
        // Arrange
        var buffer = CreateTestBuffer("Hello World");
        var state = TerminalSearchState.Empty;
        var options = new TerminalSearchOptions { MinQueryLength = 3 };

        // Act - Query "ab" is shorter than MinQueryLength of 3
        var result = await _service.SearchAsync(buffer, "ab", state, options);

        // Assert
        Assert.Empty(result.Results);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAsync - Plain Text Search Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAsync_PlainText_FindsAllMatches()
    {
        // Arrange
        var buffer = CreateTestBuffer(
            "error: file not found",
            "warning: deprecated",
            "error: permission denied",
            "info: completed"
        );
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _service.SearchAsync(buffer, "error", state, _defaultOptions);

        // Assert
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, r => Assert.Equal("error", r.MatchedText));
    }

    [Fact]
    public async Task SearchAsync_CaseSensitive_MatchesExactCase()
    {
        // Arrange
        var buffer = CreateTestBuffer(
            "Error message",
            "error message",
            "ERROR message"
        );
        var state = TerminalSearchState.Empty with { CaseSensitive = true };

        // Act
        var result = await _service.SearchAsync(buffer, "error", state, _defaultOptions);

        // Assert
        Assert.Single(result.Results);
        Assert.Equal(1, result.Results[0].LineIndex);
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_MatchesAnyCase()
    {
        // Arrange
        var buffer = CreateTestBuffer(
            "Error message",
            "error message",
            "ERROR message"
        );
        var state = TerminalSearchState.Empty with { CaseSensitive = false };

        // Act
        var result = await _service.SearchAsync(buffer, "error", state, _defaultOptions);

        // Assert
        Assert.Equal(3, result.Results.Count);
    }

    [Fact]
    public async Task SearchAsync_MultipleMatchesPerLine_FindsAll()
    {
        // Arrange
        var buffer = CreateTestBuffer("error error error");
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _service.SearchAsync(buffer, "error", state, _defaultOptions);

        // Assert
        Assert.Equal(3, result.Results.Count);
        Assert.Equal(0, result.Results[0].StartColumn);
        Assert.Equal(6, result.Results[1].StartColumn);
        Assert.Equal(12, result.Results[2].StartColumn);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAsync - Regex Search Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAsync_Regex_FindsPatternMatches()
    {
        // Arrange
        var buffer = CreateTestBuffer(
            "error123",
            "error456",
            "warning789"
        );
        var state = TerminalSearchState.Empty with { UseRegex = true };

        // Act
        var result = await _service.SearchAsync(buffer, @"error\d+", state, _defaultOptions);

        // Assert
        Assert.Equal(2, result.Results.Count);
        Assert.Equal("error123", result.Results[0].MatchedText);
        Assert.Equal("error456", result.Results[1].MatchedText);
    }

    [Fact]
    public async Task SearchAsync_InvalidRegex_ReturnsError()
    {
        // Arrange
        var buffer = CreateTestBuffer("Hello World");
        var state = TerminalSearchState.Empty with { UseRegex = true };

        // Act
        var result = await _service.SearchAsync(buffer, "[invalid", state, _defaultOptions);

        // Assert
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Invalid regex", result.ErrorMessage);
        Assert.Empty(result.Results);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAsync - Results Limiting Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAsync_MaxResults_LimitsOutput()
    {
        // Arrange - Create buffer with many matches
        var lines = Enumerable.Range(0, 100).Select(i => $"error on line {i}").ToArray();
        var buffer = CreateTestBuffer(lines);
        var state = TerminalSearchState.Empty;
        var options = new TerminalSearchOptions { MaxResults = 10 };

        // Act
        var result = await _service.SearchAsync(buffer, "error", state, options);

        // Assert
        Assert.Equal(10, result.Results.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAsync - Cancellation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAsync_Cancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var lines = Enumerable.Range(0, 1000).Select(i => $"Line {i}").ToArray();
        var buffer = CreateTestBuffer(lines);
        var state = TerminalSearchState.Empty;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException derives from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.SearchAsync(buffer, "Line", state, _defaultOptions, cts.Token));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAsync - Empty Buffer Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAsync_EmptyBuffer_ReturnsEmptyResults()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _service.SearchAsync(buffer, "test", state, _defaultOptions);

        // Assert
        Assert.Empty(result.Results);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAsync - State Updates Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAsync_WithResults_SetsCurrentIndexToZero()
    {
        // Arrange
        var buffer = CreateTestBuffer("error 1", "error 2", "error 3");
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _service.SearchAsync(buffer, "error", state, _defaultOptions);

        // Assert
        Assert.Equal(0, result.CurrentResultIndex);
    }

    [Fact]
    public async Task SearchAsync_NoResults_SetsCurrentIndexToMinusOne()
    {
        // Arrange
        var buffer = CreateTestBuffer("Hello World");
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _service.SearchAsync(buffer, "notfound", state, _defaultOptions);

        // Assert
        Assert.Equal(-1, result.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IncrementalSearchAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task IncrementalSearchAsync_UsesDefaultOptions()
    {
        // Arrange
        var buffer = CreateTestBuffer("error message");
        var options = new TerminalSearchOptions
        {
            DefaultCaseSensitive = false,
            DefaultUseRegex = false
        };

        // Act
        var result = await _service.IncrementalSearchAsync(buffer, "error", options);

        // Assert
        Assert.Single(result.Results);
        Assert.Equal("error", result.Results[0].MatchedText);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NavigateNext Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigateNext_IncrementsIndex()
    {
        // Arrange
        var state = CreateStateWithResults(5, currentIndex: 0);

        // Act
        var result = _service.NavigateNext(state);

        // Assert
        Assert.Equal(1, result.CurrentResultIndex);
    }

    [Fact]
    public void NavigateNext_WrapsAround_ToFirstResult()
    {
        // Arrange
        var state = CreateStateWithResults(5, currentIndex: 4) with { WrapAround = true };

        // Act
        var result = _service.NavigateNext(state);

        // Assert
        Assert.Equal(0, result.CurrentResultIndex);
    }

    [Fact]
    public void NavigateNext_NoWrap_StaysAtEnd()
    {
        // Arrange
        var state = CreateStateWithResults(5, currentIndex: 4) with { WrapAround = false };

        // Act
        var result = _service.NavigateNext(state);

        // Assert
        Assert.Equal(4, result.CurrentResultIndex);
    }

    [Fact]
    public void NavigateNext_NoResults_ReturnsSameState()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act
        var result = _service.NavigateNext(state);

        // Assert
        Assert.Same(state, result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NavigatePrevious Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigatePrevious_DecrementsIndex()
    {
        // Arrange
        var state = CreateStateWithResults(5, currentIndex: 3);

        // Act
        var result = _service.NavigatePrevious(state);

        // Assert
        Assert.Equal(2, result.CurrentResultIndex);
    }

    [Fact]
    public void NavigatePrevious_WrapsAround_ToLastResult()
    {
        // Arrange
        var state = CreateStateWithResults(5, currentIndex: 0) with { WrapAround = true };

        // Act
        var result = _service.NavigatePrevious(state);

        // Assert
        Assert.Equal(4, result.CurrentResultIndex);
    }

    [Fact]
    public void NavigatePrevious_NoWrap_StaysAtStart()
    {
        // Arrange
        var state = CreateStateWithResults(5, currentIndex: 0) with { WrapAround = false };

        // Act
        var result = _service.NavigatePrevious(state);

        // Assert
        Assert.Equal(0, result.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NavigateToIndex Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigateToIndex_SetsIndex()
    {
        // Arrange
        var state = CreateStateWithResults(10, currentIndex: 0);

        // Act
        var result = _service.NavigateToIndex(state, 5);

        // Assert
        Assert.Equal(5, result.CurrentResultIndex);
    }

    [Fact]
    public void NavigateToIndex_ClampsToValidRange_TooHigh()
    {
        // Arrange
        var state = CreateStateWithResults(5, currentIndex: 0);

        // Act
        var result = _service.NavigateToIndex(state, 100);

        // Assert
        Assert.Equal(4, result.CurrentResultIndex); // Clamped to last index
    }

    [Fact]
    public void NavigateToIndex_ClampsToValidRange_Negative()
    {
        // Arrange
        var state = CreateStateWithResults(5, currentIndex: 2);

        // Act
        var result = _service.NavigateToIndex(state, -10);

        // Assert
        Assert.Equal(0, result.CurrentResultIndex); // Clamped to 0
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NavigateToLine Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigateToLine_FindsNearestForward()
    {
        // Arrange - Results on lines 10, 50, 100
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 50 },
            new TerminalSearchResult { LineIndex = 100 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act - Find nearest to line 40, going forward
        var result = _service.NavigateToLine(state, 40, SearchDirection.Forward);

        // Assert - Should find line 50 (index 1)
        Assert.Equal(1, result.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ClearSearch Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ClearSearch_ReturnsEmptyState()
    {
        // Act
        var result = _service.ClearSearch();

        // Assert
        Assert.Equal(TerminalSearchState.Empty, result);
        Assert.Empty(result.Results);
        Assert.Equal(string.Empty, result.Query);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ValidateRegexPattern Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(@"\d+", null)]
    [InlineData(@"[a-z]+", null)]
    [InlineData(@"^error", null)]
    [InlineData("", null)]
    public void ValidateRegexPattern_ValidPatterns_ReturnsNull(string pattern, string? expected)
    {
        // Act
        var result = _service.ValidateRegexPattern(pattern);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(@"[invalid")]
    [InlineData(@"(unclosed")]
    [InlineData(@"+invalid")]
    public void ValidateRegexPattern_InvalidPatterns_ReturnsError(string pattern)
    {
        // Act
        var result = _service.ValidateRegexPattern(pattern);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid regex", result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetVisibleResults Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetVisibleResults_FiltersToViewport()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },  // Above viewport
            new TerminalSearchResult { LineIndex = 50 },  // In viewport
            new TerminalSearchResult { LineIndex = 60 },  // In viewport
            new TerminalSearchResult { LineIndex = 100 }  // Below viewport
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act - Viewport from line 40, 25 lines visible (40-64)
        var visible = _service.GetVisibleResults(state, 40, 25);

        // Assert
        Assert.Equal(2, visible.Count);
        Assert.Equal(50, visible[0].LineIndex);
        Assert.Equal(60, visible[1].LineIndex);
    }

    [Fact]
    public void GetVisibleResults_NoResults_ReturnsEmpty()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act
        var visible = _service.GetVisibleResults(state, 0, 25);

        // Assert
        Assert.Empty(visible);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private static TerminalSearchState CreateStateWithResults(int count, int currentIndex = 0)
    {
        var results = Enumerable.Range(0, count)
            .Select(i => new TerminalSearchResult
            {
                LineIndex = i * 10,
                StartColumn = 0,
                Length = 5,
                MatchedText = "match"
            })
            .ToArray();

        return TerminalSearchState.ForQuery("test") with
        {
            Results = results,
            CurrentResultIndex = currentIndex
        };
    }
}
