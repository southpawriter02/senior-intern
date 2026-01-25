// ============================================================================
// File: TerminalSearchIntegrationTests.cs
// Path: tests/AIntern.Tests.Integration/Terminal/TerminalSearchIntegrationTests.cs
// Description: Integration tests for terminal search functionality.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Integration.Terminal;

using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;
using AIntern.Tests.Integration.Mocks;

/// <summary>
/// Integration tests for terminal search functionality.
/// Tests search service with mock buffer across various scenarios.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
public sealed class TerminalSearchIntegrationTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Test Fixtures
    // ═══════════════════════════════════════════════════════════════════════

    private readonly Mock<ILogger<TerminalSearchService>> _loggerMock;
    private readonly TerminalSearchService _searchService;
    private readonly TerminalSearchOptions _defaultOptions;

    public TerminalSearchIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<TerminalSearchService>>();
        _searchService = new TerminalSearchService(_loggerMock.Object);
        _defaultOptions = TerminalSearchOptions.Default;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Plain Text Search Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Search_PlainText_FindsAllMatches()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLines(
            "Hello World",
            "hello again",
            "HELLO there"
        );
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty with { CaseSensitive = false };

        // Act
        var result = await _searchService.SearchAsync(buffer, "hello", state, _defaultOptions);

        // Assert
        Assert.Equal(3, result.Results.Count);
        Assert.Equal(3, result.ResultCount);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Search_PlainText_MultipleMatchesPerLine()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLine("hello hello hello");
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _searchService.SearchAsync(buffer, "hello", state, _defaultOptions);

        // Assert
        Assert.Equal(3, result.Results.Count);
        Assert.All(result.Results, r => Assert.Equal(0, r.LineIndex));
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLine("some content");
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _searchService.SearchAsync(buffer, "", state, _defaultOptions);

        // Assert
        Assert.Empty(result.Results);
        Assert.Equal(string.Empty, result.Query);
    }

    [Fact]
    public async Task Search_NoMatches_ReturnsEmptyResults()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLines("apple", "banana", "cherry");
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty;

        // Act
        var result = await _searchService.SearchAsync(buffer, "grape", state, _defaultOptions);

        // Assert
        Assert.Empty(result.Results);
        Assert.Equal(0, result.ResultCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Case Sensitivity Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Search_CaseSensitive_OnlyMatchesExact()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLines("Hello", "hello", "HELLO");
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty with { CaseSensitive = true };

        // Act
        var result = await _searchService.SearchAsync(buffer, "hello", state, _defaultOptions);

        // Assert
        Assert.Single(result.Results);
        Assert.Equal(1, result.Results[0].LineIndex);
    }

    [Fact]
    public async Task Search_CaseInsensitive_MatchesAll()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLines("Error", "ERROR", "error", "ErRoR");
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty with { CaseSensitive = false };

        // Act
        var result = await _searchService.SearchAsync(buffer, "error", state, _defaultOptions);

        // Assert
        Assert.Equal(4, result.Results.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Regex Search Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Search_Regex_MatchesPattern()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLines(
            "Error: file not found",
            "Warning: deprecated API",
            "Error: connection failed",
            "Info: processing complete"
        );
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty with { UseRegex = true };

        // Act
        var result = await _searchService.SearchAsync(buffer, @"Error:.*", state, _defaultOptions);

        // Assert
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public async Task Search_Regex_CapturesGroups()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLines(
            "[2024-01-01] Error 404",
            "[2024-01-02] Error 500"
        );
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty with { UseRegex = true };

        // Act
        var result = await _searchService.SearchAsync(buffer, @"Error \d+", state, _defaultOptions);

        // Assert
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public async Task Search_InvalidRegex_ReturnsError()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        mockBuffer.AddLine("test");
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty with { UseRegex = true };

        // Act
        var result = await _searchService.SearchAsync(buffer, "[invalid", state, _defaultOptions);

        // Assert
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Invalid", result.ErrorMessage);
        Assert.Empty(result.Results);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Navigation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigateNext_WrapsAround()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 0 },
            new TerminalSearchResult { LineIndex = 1 },
            new TerminalSearchResult { LineIndex = 2 }
        };
        var state = TerminalSearchState.Empty.WithResults(results) with
        {
            CurrentResultIndex = 2,
            WrapAround = true
        };

        // Act
        var newState = _searchService.NavigateNext(state);

        // Assert
        Assert.Equal(0, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigatePrevious_WrapsAround()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 0 },
            new TerminalSearchResult { LineIndex = 1 },
            new TerminalSearchResult { LineIndex = 2 }
        };
        var state = TerminalSearchState.Empty.WithResults(results) with
        {
            CurrentResultIndex = 0,
            WrapAround = true
        };

        // Act
        var newState = _searchService.NavigatePrevious(state);

        // Assert
        Assert.Equal(2, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigateNext_NoWrap_StaysAtEnd()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 0 },
            new TerminalSearchResult { LineIndex = 1 }
        };
        var state = TerminalSearchState.Empty.WithResults(results) with
        {
            CurrentResultIndex = 1,
            WrapAround = false
        };

        // Act
        var newState = _searchService.NavigateNext(state);

        // Assert
        Assert.Equal(1, newState.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Large Buffer Performance Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Search_LargeBuffer_CompletesInReasonableTime()
    {
        // Arrange
        var mockBuffer = new MockTerminalBuffer();
        for (int i = 0; i < 50000; i++)
            mockBuffer.AddLine($"Log entry {i}: Some content here");
        var buffer = mockBuffer.ToTerminalBuffer();
        var state = TerminalSearchState.Empty;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _searchService.SearchAsync(buffer, "entry 25000", state, _defaultOptions);

        sw.Stop();

        // Assert
        Assert.Single(result.Results);
        Assert.True(sw.ElapsedMilliseconds < 5000, "Search took too long");
    }
}
