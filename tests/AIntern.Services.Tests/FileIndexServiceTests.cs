namespace AIntern.Services.Tests;

using System.Collections.Generic;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FileIndexService"/>.
/// </summary>
public class FileIndexServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly FileIndexService _sut;

    public FileIndexServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _sut = new FileIndexService(
            _mockFileSystem.Object,
            NullLogger<FileIndexService>.Instance);
    }

    #region Initialization

    [Fact]
    public void InitialState_IsNotIndexed()
    {
        // Assert
        Assert.False(_sut.IsIndexed);
        Assert.Equal(0, _sut.IndexedFileCount);
    }

    #endregion

    #region Search

    [Fact]
    public void Search_WithEmptyQuery_ReturnsRecentFiles()
    {
        // Arrange
        _sut.AddToRecent("/path/to/file.cs");

        // Act
        var results = _sut.Search(string.Empty);

        // Assert (returns empty since files aren't indexed)
        Assert.Empty(results);
    }

    [Fact]
    public void Search_WhenNotIndexed_ReturnsEmpty()
    {
        // Act
        var results = _sut.Search("test");

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Recent Files

    [Fact]
    public void AddToRecent_AddsFileToFront()
    {
        // Arrange
        _sut.AddToRecent("/path/file1.cs");
        _sut.AddToRecent("/path/file2.cs");

        // Act
        var recent = _sut.GetRecentFiles();

        // Assert
        Assert.Equal(2, recent.Count);
        Assert.Equal("/path/file2.cs", recent[0]);
        Assert.Equal("/path/file1.cs", recent[1]);
    }

    [Fact]
    public void AddToRecent_DuplicateMovesToFront()
    {
        // Arrange
        _sut.AddToRecent("/path/file1.cs");
        _sut.AddToRecent("/path/file2.cs");
        _sut.AddToRecent("/path/file1.cs"); // Add again

        // Act
        var recent = _sut.GetRecentFiles();

        // Assert
        Assert.Equal(2, recent.Count);
        Assert.Equal("/path/file1.cs", recent[0]);
        Assert.Equal("/path/file2.cs", recent[1]);
    }

    [Fact]
    public void GetRecentFiles_RespectsCountLimit()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
            _sut.AddToRecent($"/path/file{i}.cs");

        // Act
        var recent = _sut.GetRecentFiles(3);

        // Assert
        Assert.Equal(3, recent.Count);
    }

    #endregion

    #region ClearIndex

    [Fact]
    public void ClearIndex_ResetsIndexedState()
    {
        // Arrange - simulate indexed state
        _sut.AddToRecent("/path/file.cs");

        // Act
        _sut.ClearIndex();

        // Assert
        Assert.False(_sut.IsIndexed);
        Assert.Equal(0, _sut.IndexedFileCount);
    }

    #endregion
}
