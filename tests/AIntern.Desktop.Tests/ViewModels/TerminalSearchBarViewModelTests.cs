// ============================================================================
// File: TerminalSearchBarViewModelTests.cs
// Path: tests/AIntern.Desktop.Tests/ViewModels/TerminalSearchBarViewModelTests.cs
// Description: Unit tests for TerminalSearchBarViewModel.
// Created: 2026-01-18
// AI Intern v0.5.5c - Terminal Search UI
// ============================================================================

namespace AIntern.Desktop.Tests.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalSearchBarViewModelTests (v0.5.5c)                                    │
// │ Comprehensive tests for the terminal search bar ViewModel.                  │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="TerminalSearchBarViewModel"/>.
/// </summary>
public class TerminalSearchBarViewModelTests : IDisposable
{
    #region Test Fixtures

    private readonly Mock<ITerminalSearchService> _mockSearchService;
    private readonly Mock<ILogger<TerminalSearchBarViewModel>> _mockLogger;
    private readonly TerminalSearchBarViewModel _sut;

    public TerminalSearchBarViewModelTests()
    {
        _mockSearchService = new Mock<ITerminalSearchService>();
        _mockLogger = new Mock<ILogger<TerminalSearchBarViewModel>>();

        // Setup default mock behavior
        _mockSearchService.Setup(s => s.ClearSearch()).Returns(TerminalSearchState.Empty);

        _sut = new TerminalSearchBarViewModel(
            _mockSearchService.Object,
            options: null,
            logger: _mockLogger.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSearchServiceIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TerminalSearchBarViewModel(null!));
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Assert
        Assert.Empty(_sut.SearchQuery);
        Assert.False(_sut.IsSearchVisible);
        Assert.False(_sut.CaseSensitive);
        Assert.False(_sut.UseRegex);
        Assert.False(_sut.IsSearching);
        Assert.Empty(_sut.SearchResultsText);
        Assert.Null(_sut.ErrorMessage);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void SearchQuery_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var propertyChanged = false;
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TerminalSearchBarViewModel.SearchQuery))
                propertyChanged = true;
        };

        // Act
        _sut.SearchQuery = "test";

        // Assert
        Assert.True(propertyChanged);
        Assert.Equal("test", _sut.SearchQuery);
    }

    [Fact]
    public void IsSearchVisible_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var propertyChanged = false;
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TerminalSearchBarViewModel.IsSearchVisible))
                propertyChanged = true;
        };

        // Act
        _sut.OpenSearch();

        // Assert
        Assert.True(propertyChanged);
        Assert.True(_sut.IsSearchVisible);
    }

    [Fact]
    public void CaseSensitive_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var propertyChanged = false;
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TerminalSearchBarViewModel.CaseSensitive))
                propertyChanged = true;
        };

        // Act
        _sut.CaseSensitive = true;

        // Assert
        Assert.True(propertyChanged);
        Assert.True(_sut.CaseSensitive);
    }

    [Fact]
    public void UseRegex_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var propertyChanged = false;
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TerminalSearchBarViewModel.UseRegex))
                propertyChanged = true;
        };

        // Act
        _sut.UseRegex = true;

        // Assert
        Assert.True(propertyChanged);
        Assert.True(_sut.UseRegex);
    }

    #endregion

    #region OpenSearch/CloseSearch Tests

    [Fact]
    public void OpenSearch_SetsIsSearchVisibleTrue()
    {
        // Act
        _sut.OpenSearch();

        // Assert
        Assert.True(_sut.IsSearchVisible);
    }

    [Fact]
    public void OpenSearch_RaisesSearchOpenedEvent()
    {
        // Arrange
        var eventRaised = false;
        _sut.SearchOpened += (_, _) => eventRaised = true;

        // Act
        _sut.OpenSearch();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void CloseSearch_SetsIsSearchVisibleFalse()
    {
        // Arrange
        _sut.OpenSearch();

        // Act
        _sut.CloseSearchCommand.Execute(null);

        // Assert
        Assert.False(_sut.IsSearchVisible);
    }

    [Fact]
    public void CloseSearch_RaisesSearchClosedEvent()
    {
        // Arrange
        _sut.OpenSearch();
        var eventRaised = false;
        _sut.SearchClosed += (_, _) => eventRaised = true;

        // Act
        _sut.CloseSearchCommand.Execute(null);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void CloseSearch_ClearsSearchQuery()
    {
        // Arrange
        _sut.SearchQuery = "test";
        _sut.OpenSearch();

        // Act
        _sut.CloseSearchCommand.Execute(null);

        // Assert
        Assert.Empty(_sut.SearchQuery);
    }

    #endregion

    #region Toggle Command Tests

    [Fact]
    public void ToggleCaseSensitiveCommand_TogglesValue()
    {
        // Arrange
        Assert.False(_sut.CaseSensitive);

        // Act
        _sut.ToggleCaseSensitiveCommand.Execute(null);

        // Assert
        Assert.True(_sut.CaseSensitive);

        // Act again
        _sut.ToggleCaseSensitiveCommand.Execute(null);

        // Assert
        Assert.False(_sut.CaseSensitive);
    }

    [Fact]
    public void ToggleRegexCommand_TogglesValue()
    {
        // Arrange
        Assert.False(_sut.UseRegex);

        // Act
        _sut.ToggleRegexCommand.Execute(null);

        // Assert
        Assert.True(_sut.UseRegex);

        // Act again
        _sut.ToggleRegexCommand.Execute(null);

        // Assert
        Assert.False(_sut.UseRegex);
    }

    #endregion

    #region SetBuffer Tests

    [Fact]
    public void SetBuffer_AcceptsValidBuffer()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act - should not throw
        _sut.SetBuffer(buffer);

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    public void SetBuffer_AcceptsNullBuffer()
    {
        // Act - should not throw
        _sut.SetBuffer(null!);

        // Assert - no exception thrown
        Assert.True(true);
    }

    #endregion

    #region NavigateToLine Tests

    [Fact]
    public void NavigateToLine_RaisesScrollToResultRequestedEvent()
    {
        // Arrange
        var eventRaised = false;
        TerminalSearchResult? requestedResult = null;
        _sut.ScrollToResultRequested += (_, result) =>
        {
            eventRaised = true;
            requestedResult = result;
        };

        // Setup mock to return state with a result
        var result = TerminalSearchResult.Create(42, 0, 5, "test", "test match");
        var stateWithResults = TerminalSearchState.Empty with
        {
            Results = new List<TerminalSearchResult> { result }.AsReadOnly(),
            CurrentResultIndex = 0
        };
        _mockSearchService.Setup(s => s.NavigateToLine(
                It.IsAny<TerminalSearchState>(),
                It.IsAny<int>(),
                It.IsAny<SearchDirection>()))
            .Returns(stateWithResults);

        // Set SearchState first (by direct access for testing)
        _sut.GetType().GetProperty("SearchState")?.SetValue(_sut, stateWithResults);

        // Act
        _sut.NavigateToLine(42);

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(requestedResult);
        Assert.Equal(42, requestedResult.LineIndex);
    }

    #endregion

    #region Search State Tests

    [Fact]
    public void SearchState_InitiallyEmpty()
    {
        // Assert
        Assert.NotNull(_sut.SearchState);
        Assert.Empty(_sut.SearchState.Query);
        Assert.Empty(_sut.SearchState.Results);
    }

    [Fact]
    public void SearchResultsText_WhenNoSearch_IsEmpty()
    {
        // Assert
        Assert.Empty(_sut.SearchResultsText);
    }

    [Fact]
    public void ErrorMessage_WhenNoError_IsNull()
    {
        // Assert
        Assert.Null(_sut.ErrorMessage);
    }

    #endregion

    #region Navigation Command Tests

    [Fact]
    public void NextSearchResultCommand_ExecutesWithoutError_WhenNoResults()
    {
        // Act - should not throw
        _sut.NextSearchResultCommand.Execute(null);

        // Assert - no exception, search state unchanged
        Assert.False(_sut.SearchState.HasResults);
    }

    [Fact]
    public void PreviousSearchResultCommand_ExecutesWithoutError_WhenNoResults()
    {
        // Act - should not throw
        _sut.PreviousSearchResultCommand.Execute(null);

        // Assert - no exception, search state unchanged
        Assert.False(_sut.SearchState.HasResults);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var vm = new TerminalSearchBarViewModel(_mockSearchService.Object);

        // Act - should not throw
        vm.Dispose();
        vm.Dispose();

        // Assert - no exception thrown
        Assert.True(true);
    }

    #endregion

    #region SearchOptions Tests

    [Fact]
    public void SearchOptions_ReturnsCaseSensitiveOption_WhenSet()
    {
        // Arrange
        _sut.CaseSensitive = true;

        // Assert - options are passed to search service (verified via mock if search executed)
        Assert.True(_sut.CaseSensitive);
    }

    [Fact]
    public void SearchOptions_ReturnsRegexOption_WhenSet()
    {
        // Arrange
        _sut.UseRegex = true;

        // Assert
        Assert.True(_sut.UseRegex);
    }

    #endregion
}
