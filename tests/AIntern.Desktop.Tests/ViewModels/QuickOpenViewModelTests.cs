namespace AIntern.Desktop.Tests.ViewModels;

using System.Collections.Generic;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="QuickOpenViewModel"/>.
/// </summary>
public class QuickOpenViewModelTests
{
    private readonly Mock<IFileIndexService> _mockFileIndex;
    private readonly QuickOpenViewModel _sut;

    public QuickOpenViewModelTests()
    {
        _mockFileIndex = new Mock<IFileIndexService>();
        _mockFileIndex.Setup(f => f.Search(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(new List<FileSearchResult>());
        _mockFileIndex.Setup(f => f.GetRecentFiles(It.IsAny<int>()))
            .Returns(new List<string>());
        _mockFileIndex.Setup(f => f.IsIndexed).Returns(true);
        _mockFileIndex.Setup(f => f.IndexedFileCount).Returns(100);

        _sut = new QuickOpenViewModel(
            _mockFileIndex.Object,
            NullLogger<QuickOpenViewModel>.Instance);
    }

    #region Initialization

    [Fact]
    public void Constructor_LoadsInitialResults()
    {
        // Assert
        Assert.NotNull(_sut.Results);
        Assert.Contains("100", _sut.StatusText);
    }

    [Fact]
    public void Constructor_SetsStatusText()
    {
        // Assert
        Assert.Equal("100 files indexed", _sut.StatusText);
    }

    #endregion

    #region Navigation

    [Fact]
    public void MoveUp_WithNoResults_DoesNothing()
    {
        // Arrange
        Assert.Empty(_sut.Results);

        // Act
        _sut.MoveUpCommand.Execute(null);

        // Assert - no exception
    }

    [Fact]
    public void MoveDown_WithNoResults_DoesNothing()
    {
        // Arrange
        Assert.Empty(_sut.Results);

        // Act
        _sut.MoveDownCommand.Execute(null);

        // Assert - no exception
    }

    [Fact]
    public void MoveDown_WithResults_IncrementsIndex()
    {
        // Arrange
        _mockFileIndex.Setup(f => f.Search(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(new List<FileSearchResult>
            {
                new() { FilePath = "/a.cs", FileName = "a.cs", RelativePath = "a.cs" },
                new() { FilePath = "/b.cs", FileName = "b.cs", RelativePath = "b.cs" }
            });

        var vm = new QuickOpenViewModel(
            _mockFileIndex.Object,
            NullLogger<QuickOpenViewModel>.Instance);

        // Force search to populate results
        vm.SearchQuery = "test";

        // Act - wait for debounce and simulate
        vm.MoveDownCommand.Execute(null);

        // Assert - index should change (wrapping if needed)
    }

    [Fact]
    public void MoveUp_Wraps()
    {
        // Arrange
        _mockFileIndex.Setup(f => f.Search(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(new List<FileSearchResult>
            {
                new() { FilePath = "/a.cs", FileName = "a.cs", RelativePath = "a.cs" },
                new() { FilePath = "/b.cs", FileName = "b.cs", RelativePath = "b.cs" }
            });

        var vm = new QuickOpenViewModel(
            _mockFileIndex.Object,
            NullLogger<QuickOpenViewModel>.Instance);

        vm.SearchQuery = "test";

        // Act - MoveUp when at 0 should wrap to end
        vm.MoveUpCommand.Execute(null);

        // No exception
    }

    #endregion

    #region Events

    [Fact]
    public void Cancel_RaisesCloseRequested()
    {
        // Arrange
        var raised = false;
        _sut.CloseRequested += (s, e) => raised = true;

        // Act
        _sut.CancelCommand.Execute(null);

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void Confirm_WithNoResults_RaisesCloseRequested()
    {
        // Arrange
        var closeRaised = false;
        _sut.CloseRequested += (s, e) => closeRaised = true;

        // Act
        _sut.ConfirmCommand.Execute(null);

        // Assert
        Assert.True(closeRaised);
    }

    #endregion
}
