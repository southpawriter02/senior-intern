namespace AIntern.Desktop.Tests.Models;

using System;
using Xunit;
using AIntern.Desktop.Models;

/// <summary>
/// Unit tests for <see cref="SelectionInfo"/> and <see cref="SelectionAttachmentEventArgs"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4f.</para>
/// </remarks>
public class SelectionInfoTests
{
    #region SelectionInfo Tests

    /// <summary>
    /// Verifies default property values.
    /// </summary>
    [Fact]
    public void SelectionInfo_DefaultValues()
    {
        // Arrange & Act
        var info = new SelectionInfo();

        // Assert
        Assert.Equal(string.Empty, info.FilePath);
        Assert.Equal(string.Empty, info.FileName);
        Assert.Null(info.Language);
        Assert.Equal(string.Empty, info.Content);
        Assert.Equal(0, info.StartLine);
        Assert.Equal(0, info.EndLine);
        Assert.False(info.IsFullFile);
    }

    /// <summary>
    /// Verifies LineCount for selection.
    /// </summary>
    [Fact]
    public void SelectionInfo_LineCount_Selection_Calculated()
    {
        // Arrange
        var info = new SelectionInfo
        {
            StartLine = 5,
            EndLine = 10
        };

        // Act & Assert
        Assert.Equal(6, info.LineCount);
    }

    /// <summary>
    /// Verifies LineCount for full file.
    /// </summary>
    [Fact]
    public void SelectionInfo_LineCount_FullFile_CountsNewlines()
    {
        // Arrange
        var info = new SelectionInfo
        {
            Content = "line1\nline2\nline3",
            IsFullFile = true
        };

        // Act & Assert
        Assert.Equal(3, info.LineCount);
    }

    /// <summary>
    /// Verifies LineCount for single line selection.
    /// </summary>
    [Fact]
    public void SelectionInfo_LineCount_SingleLine()
    {
        // Arrange
        var info = new SelectionInfo
        {
            StartLine = 7,
            EndLine = 7
        };

        // Act & Assert
        Assert.Equal(1, info.LineCount);
    }

    /// <summary>
    /// Verifies all properties can be set via init.
    /// </summary>
    [Fact]
    public void SelectionInfo_AllProperties_Init()
    {
        // Arrange & Act
        var info = new SelectionInfo
        {
            FilePath = "/path/to/file.cs",
            FileName = "file.cs",
            Language = "csharp",
            Content = "var x = 1;",
            StartLine = 10,
            EndLine = 15,
            StartColumn = 5,
            EndColumn = 20,
            IsFullFile = false
        };

        // Assert
        Assert.Equal("/path/to/file.cs", info.FilePath);
        Assert.Equal("file.cs", info.FileName);
        Assert.Equal("csharp", info.Language);
        Assert.Equal("var x = 1;", info.Content);
        Assert.Equal(10, info.StartLine);
        Assert.Equal(15, info.EndLine);
        Assert.Equal(5, info.StartColumn);
        Assert.Equal(20, info.EndColumn);
        Assert.False(info.IsFullFile);
    }

    #endregion

    #region SelectionAttachmentEventArgs Tests

    /// <summary>
    /// Verifies constructor sets Selection property.
    /// </summary>
    [Fact]
    public void SelectionAttachmentEventArgs_SetsSelection()
    {
        // Arrange
        var info = new SelectionInfo { FileName = "test.cs" };

        // Act
        var args = new SelectionAttachmentEventArgs(info);

        // Assert
        Assert.Same(info, args.Selection);
    }

    /// <summary>
    /// Verifies constructor throws on null.
    /// </summary>
    [Fact]
    public void SelectionAttachmentEventArgs_NullThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SelectionAttachmentEventArgs(null!));
    }

    #endregion
}
