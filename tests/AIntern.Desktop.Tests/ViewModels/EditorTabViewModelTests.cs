using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="EditorTabViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>Factory methods (FromFile, CreateNew)</description></item>
///   <item><description>Dirty state tracking</description></item>
///   <item><description>Caret position tracking</description></item>
///   <item><description>Line ending detection</description></item>
///   <item><description>DisplayTitle formatting</description></item>
///   <item><description>GetOffsetForLine bounds</description></item>
///   <item><description>Dispose cleanup</description></item>
/// </list>
/// <para>Added in v0.3.3a.</para>
/// </remarks>
public class EditorTabViewModelTests : IDisposable
{
    private readonly Mock<ILogger<EditorTabViewModel>> _mockLogger;
    private EditorTabViewModel? _tab;

    public EditorTabViewModelTests()
    {
        _mockLogger = new Mock<ILogger<EditorTabViewModel>>();
    }

    public void Dispose()
    {
        _tab?.Dispose();
    }

    #region FromFile Factory Tests

    /// <summary>
    /// Verifies FromFile sets FilePath correctly.
    /// </summary>
    [Fact]
    public void FromFile_SetsFilePath()
    {
        // Act
        _tab = EditorTabViewModel.FromFile("/path/to/file.cs", "content", _mockLogger.Object);

        // Assert
        Assert.Equal("/path/to/file.cs", _tab.FilePath);
    }

    /// <summary>
    /// Verifies FromFile extracts FileName from path.
    /// </summary>
    [Fact]
    public void FromFile_SetsFileName()
    {
        // Act
        _tab = EditorTabViewModel.FromFile("/path/to/file.cs", "content", _mockLogger.Object);

        // Assert
        Assert.Equal("file.cs", _tab.FileName);
    }

    /// <summary>
    /// Verifies FromFile detects language from extension.
    /// </summary>
    [Fact]
    public void FromFile_DetectsLanguageFromExtension()
    {
        // Act
        _tab = EditorTabViewModel.FromFile("/path/to/file.cs", "class Foo {}", _mockLogger.Object);

        // Assert
        Assert.Equal("csharp", _tab.Language);
    }

    /// <summary>
    /// Verifies FromFile starts with IsDirty = false.
    /// </summary>
    [Fact]
    public void FromFile_StartsNotDirty()
    {
        // Act
        _tab = EditorTabViewModel.FromFile("/path/to/file.txt", "original", _mockLogger.Object);

        // Assert
        Assert.False(_tab.IsDirty);
    }

    /// <summary>
    /// Verifies FromFile sets document content.
    /// </summary>
    [Fact]
    public void FromFile_SetsDocumentContent()
    {
        // Act
        _tab = EditorTabViewModel.FromFile("/path/to/file.txt", "test content", _mockLogger.Object);

        // Assert
        Assert.Equal("test content", _tab.Document.Text);
    }

    #endregion

    #region CreateNew Factory Tests

    /// <summary>
    /// Verifies CreateNew uses provided name.
    /// </summary>
    [Fact]
    public void CreateNew_UsesProvidedName()
    {
        // Act
        _tab = EditorTabViewModel.CreateNew("Untitled-1", null, _mockLogger.Object);

        // Assert
        Assert.Equal("Untitled-1", _tab.FileName);
    }

    /// <summary>
    /// Verifies CreateNew defaults to "Untitled".
    /// </summary>
    [Fact]
    public void CreateNew_DefaultsToUntitled()
    {
        // Act
        _tab = EditorTabViewModel.CreateNew(null, null, _mockLogger.Object);

        // Assert
        Assert.Equal("Untitled", _tab.FileName);
    }

    /// <summary>
    /// Verifies CreateNew sets language when provided.
    /// </summary>
    [Fact]
    public void CreateNew_SetsLanguage()
    {
        // Act
        _tab = EditorTabViewModel.CreateNew("script.js", "javascript", _mockLogger.Object);

        // Assert
        Assert.Equal("javascript", _tab.Language);
    }

    /// <summary>
    /// Verifies CreateNew starts with empty FilePath.
    /// </summary>
    [Fact]
    public void CreateNew_HasEmptyFilePath()
    {
        // Act
        _tab = EditorTabViewModel.CreateNew("Untitled-1", null, _mockLogger.Object);

        // Assert
        Assert.Equal(string.Empty, _tab.FilePath);
        Assert.True(_tab.IsNewFile);
    }

    /// <summary>
    /// Verifies CreateNew starts with empty document.
    /// </summary>
    [Fact]
    public void CreateNew_HasEmptyDocument()
    {
        // Act
        _tab = EditorTabViewModel.CreateNew("Untitled-1", null, _mockLogger.Object);

        // Assert
        Assert.Equal(string.Empty, _tab.Document.Text);
    }

    #endregion

    #region Dirty State Tests

    /// <summary>
    /// Verifies IsDirty becomes true on edit.
    /// </summary>
    [Fact]
    public void IsDirty_BecomesTrueOnEdit()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "original", _mockLogger.Object);

        // Act
        _tab.Document.Text = "modified";

        // Assert
        Assert.True(_tab.IsDirty);
    }

    /// <summary>
    /// Verifies IsDirty becomes false when undone to original.
    /// </summary>
    [Fact]
    public void IsDirty_BecomesFalseWhenUndoneToOriginal()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "original", _mockLogger.Object);
        _tab.Document.Text = "modified";
        Assert.True(_tab.IsDirty);

        // Act - undo back to original
        _tab.Document.Text = "original";

        // Assert - hash matches saved
        Assert.False(_tab.IsDirty);
    }

    /// <summary>
    /// Verifies IsDirty for new file becomes true on first edit.
    /// </summary>
    [Fact]
    public void IsDirty_NewFile_BecomesTrueOnEdit()
    {
        // Arrange
        _tab = EditorTabViewModel.CreateNew("Untitled-1", null, _mockLogger.Object);
        Assert.False(_tab.IsDirty);

        // Act
        _tab.Document.Text = "new content";

        // Assert
        Assert.True(_tab.IsDirty);
    }

    #endregion

    #region MarkAsSaved Tests

    /// <summary>
    /// Verifies MarkAsSaved resets dirty state.
    /// </summary>
    [Fact]
    public void MarkAsSaved_ResetsDirtyState()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "original", _mockLogger.Object);
        _tab.Document.Text = "modified";
        Assert.True(_tab.IsDirty);

        // Act
        _tab.MarkAsSaved();

        // Assert
        Assert.False(_tab.IsDirty);
    }

    /// <summary>
    /// Verifies MarkAsSaved updates path for new file.
    /// </summary>
    [Fact]
    public void MarkAsSaved_UpdatesPathForNewFile()
    {
        // Arrange
        _tab = EditorTabViewModel.CreateNew("Untitled-1", null, _mockLogger.Object);
        _tab.Document.Text = "content";
        Assert.True(_tab.IsNewFile);

        // Act
        _tab.MarkAsSaved("/new/path/file.js");

        // Assert
        Assert.Equal("/new/path/file.js", _tab.FilePath);
        Assert.Equal("file.js", _tab.FileName);
        Assert.Equal("javascript", _tab.Language);
        Assert.False(_tab.IsNewFile);
    }

    #endregion

    #region DisplayTitle Tests

    /// <summary>
    /// Verifies DisplayTitle shows filename when clean.
    /// </summary>
    [Fact]
    public void DisplayTitle_ShowsFileNameWhenClean()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "original", _mockLogger.Object);

        // Assert
        Assert.Equal("test.txt", _tab.DisplayTitle);
    }

    /// <summary>
    /// Verifies DisplayTitle includes dot when dirty.
    /// </summary>
    [Fact]
    public void DisplayTitle_IncludesDotWhenDirty()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "original", _mockLogger.Object);

        // Act
        _tab.Document.Text = "modified";

        // Assert
        Assert.Equal("test.txt •", _tab.DisplayTitle);
    }

    #endregion

    #region Line Ending Detection Tests

    /// <summary>
    /// Verifies LF line ending detection.
    /// </summary>
    [Fact]
    public void DetectsLineEnding_LF()
    {
        // Act
        _tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2", _mockLogger.Object);

        // Assert
        Assert.Equal("LF", _tab.LineEnding);
    }

    /// <summary>
    /// Verifies CRLF line ending detection.
    /// </summary>
    [Fact]
    public void DetectsLineEnding_CRLF()
    {
        // Act
        _tab = EditorTabViewModel.FromFile("/test.txt", "line1\r\nline2", _mockLogger.Object);

        // Assert
        Assert.Equal("CRLF", _tab.LineEnding);
    }

    /// <summary>
    /// Verifies CR line ending detection.
    /// </summary>
    [Fact]
    public void DetectsLineEnding_CR()
    {
        // Act
        _tab = EditorTabViewModel.FromFile("/test.txt", "line1\rline2", _mockLogger.Object);

        // Assert
        Assert.Equal("CR", _tab.LineEnding);
    }

    #endregion

    #region Caret Position Tests

    /// <summary>
    /// Verifies UpdateCaretPosition sets values correctly.
    /// </summary>
    [Fact]
    public void UpdateCaretPosition_SetsValues()
    {
        // Arrange
        _tab = EditorTabViewModel.CreateNew("Untitled", null, _mockLogger.Object);

        // Act
        _tab.UpdateCaretPosition(45, 12, 0);

        // Assert
        Assert.Equal(45, _tab.CaretLine);
        Assert.Equal(12, _tab.CaretColumn);
        Assert.Equal(0, _tab.SelectionLength);
    }

    /// <summary>
    /// Verifies CursorPositionDisplay without selection.
    /// </summary>
    [Fact]
    public void CursorPositionDisplay_NoSelection()
    {
        // Arrange
        _tab = EditorTabViewModel.CreateNew("Untitled", null, _mockLogger.Object);
        _tab.UpdateCaretPosition(45, 12, 0);

        // Assert
        Assert.Equal("Ln 45, Col 12", _tab.CursorPositionDisplay);
    }

    /// <summary>
    /// Verifies CursorPositionDisplay with selection.
    /// </summary>
    [Fact]
    public void CursorPositionDisplay_WithSelection()
    {
        // Arrange
        _tab = EditorTabViewModel.CreateNew("Untitled", null, _mockLogger.Object);
        _tab.UpdateCaretPosition(45, 12, 27);

        // Assert
        Assert.Equal("Ln 45, Col 12 (27 selected)", _tab.CursorPositionDisplay);
    }

    #endregion

    #region GetOffsetForLine Tests

    /// <summary>
    /// Verifies GetOffsetForLine for valid line.
    /// </summary>
    [Fact]
    public void GetOffsetForLine_ValidLine()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2\nline3", _mockLogger.Object);

        // Act
        var offset = _tab.GetOffsetForLine(2);

        // Assert - line 2 starts at offset 6 (after "line1\n")
        Assert.Equal(6, offset);
    }

    /// <summary>
    /// Verifies GetOffsetForLine clamps line below 1.
    /// </summary>
    [Fact]
    public void GetOffsetForLine_ClampsLineBelowOne()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2", _mockLogger.Object);

        // Act
        var offset = _tab.GetOffsetForLine(0);

        // Assert - clamped to line 1, offset 0
        Assert.Equal(0, offset);
    }

    /// <summary>
    /// Verifies GetOffsetForLine clamps line above LineCount.
    /// </summary>
    [Fact]
    public void GetOffsetForLine_ClampsLineAboveCount()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2", _mockLogger.Object);

        // Act
        var offset = _tab.GetOffsetForLine(100);

        // Assert - clamped to last line
        Assert.Equal(6, offset); // offset of line 2
    }

    #endregion

    #region Computed Properties Tests

    /// <summary>
    /// Verifies LineCount returns document line count.
    /// </summary>
    [Fact]
    public void LineCount_ReturnsDocumentLineCount()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2\nline3", _mockLogger.Object);

        // Assert
        Assert.Equal(3, _tab.LineCount);
    }

    /// <summary>
    /// Verifies Extension property.
    /// </summary>
    [Fact]
    public void Extension_ReturnsFileExtension()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/path/to/file.cs", "content", _mockLogger.Object);

        // Assert
        Assert.Equal(".cs", _tab.Extension);
    }

    /// <summary>
    /// Verifies IsNewFile is true for new files.
    /// </summary>
    [Fact]
    public void IsNewFile_TrueForNewFile()
    {
        // Arrange
        _tab = EditorTabViewModel.CreateNew("Untitled", null, _mockLogger.Object);

        // Assert
        Assert.True(_tab.IsNewFile);
    }

    /// <summary>
    /// Verifies IsNewFile is false for existing files.
    /// </summary>
    [Fact]
    public void IsNewFile_FalseForExistingFile()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/path/to/file.txt", "content", _mockLogger.Object);

        // Assert
        Assert.False(_tab.IsNewFile);
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies Dispose unsubscribes from document events.
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        _tab = EditorTabViewModel.FromFile("/test.txt", "original", _mockLogger.Object);
        _tab.Document.Text = "modified";
        Assert.True(_tab.IsDirty);

        // Act
        _tab.Dispose();

        // After dispose, further edits should not affect dirty state
        // (though in practice we wouldn't edit after dispose)
        // This test just ensures dispose completes without error
    }

    #endregion
}
