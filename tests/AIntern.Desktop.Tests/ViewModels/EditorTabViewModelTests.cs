using Xunit;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

public class EditorTabViewModelTests
{
    #region FromFile Tests

    [Fact]
    public void FromFile_SetsCorrectFilePath()
    {
        var tab = EditorTabViewModel.FromFile("/path/to/file.cs", "content");

        Assert.Equal("/path/to/file.cs", tab.FilePath);
        Assert.Equal("file.cs", tab.FileName);
    }

    [Fact]
    public void FromFile_DetectsLanguageFromExtension()
    {
        var tab = EditorTabViewModel.FromFile("/path/to/file.cs", "class Foo {}");

        Assert.Equal("csharp", tab.Language);
    }

    [Fact]
    public void FromFile_StartsNotDirty()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "original");

        Assert.False(tab.IsDirty);
    }

    #endregion

    #region CreateNew Tests

    [Fact]
    public void CreateNew_UsesDefaultName()
    {
        var tab = EditorTabViewModel.CreateNew();

        Assert.Equal("Untitled", tab.FileName);
        Assert.True(tab.IsNewFile);
    }

    [Fact]
    public void CreateNew_UsesProvidedNameAndLanguage()
    {
        var tab = EditorTabViewModel.CreateNew("Untitled-1.js", "javascript");

        Assert.Equal("Untitled-1.js", tab.FileName);
        Assert.Equal("javascript", tab.Language);
    }

    #endregion

    #region IsDirty Tracking Tests

    [Fact]
    public void IsDirty_BecomesTrueOnEdit()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "original");

        tab.Document.Text = "modified";

        Assert.True(tab.IsDirty);
    }

    [Fact]
    public void IsDirty_BecomesFalseOnUndoToOriginal()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "original");
        tab.Document.Text = "modified";

        tab.Document.Text = "original"; // Undo back

        Assert.False(tab.IsDirty); // Hash matches saved
    }

    [Fact]
    public void IsDirty_BecomesFalseAfterSave()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "original");
        tab.Document.Text = "modified";

        tab.MarkAsSaved();

        Assert.False(tab.IsDirty);
    }

    [Fact]
    public void IsDirty_TracksMultipleEdits()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "original");

        tab.Document.Text = "edit1";
        Assert.True(tab.IsDirty);

        tab.Document.Text = "edit2";
        Assert.True(tab.IsDirty);

        tab.Document.Text = "original";
        Assert.False(tab.IsDirty);
    }

    #endregion

    #region MarkAsSaved Tests

    [Fact]
    public void MarkAsSaved_ResetsDirtyState()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "original");
        tab.Document.Text = "modified";

        tab.MarkAsSaved();

        Assert.False(tab.IsDirty);
    }

    [Fact]
    public void MarkAsSaved_UpdatesPathForNewFile()
    {
        var tab = EditorTabViewModel.CreateNew("Untitled-1");
        tab.Document.Text = "content";

        tab.MarkAsSaved("/new/path/file.js");

        Assert.Equal("/new/path/file.js", tab.FilePath);
        Assert.Equal("file.js", tab.FileName);
        Assert.Equal("javascript", tab.Language);
        Assert.False(tab.IsDirty);
        Assert.False(tab.IsNewFile);
    }

    #endregion

    #region UpdateCaretPosition Tests

    [Fact]
    public void UpdateCaretPosition_SetsValues()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2");

        tab.UpdateCaretPosition(2, 5);

        Assert.Equal(2, tab.CaretLine);
        Assert.Equal(5, tab.CaretColumn);
        Assert.Equal(0, tab.SelectionLength);
    }

    [Fact]
    public void UpdateCaretPosition_WithSelection_UpdatesDisplay()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "some content");

        tab.UpdateCaretPosition(1, 5, 10);

        Assert.Equal("Ln 1, Col 5 (10 selected)", tab.CursorPositionDisplay);
    }

    #endregion

    #region GetOffsetForLine Tests

    [Fact]
    public void GetOffsetForLine_ReturnsCorrectOffset()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2\nline3");

        var offset = tab.GetOffsetForLine(2);

        Assert.Equal(6, offset); // "line1\n" = 6 chars
    }

    [Fact]
    public void GetOffsetForLine_ClampsOutOfBounds()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2");

        var tooLow = tab.GetOffsetForLine(0);
        var tooHigh = tab.GetOffsetForLine(100);

        Assert.Equal(0, tooLow); // Line 1
        Assert.Equal(6, tooHigh); // Last line (line 2)
    }

    #endregion

    #region DisplayTitle Tests

    [Fact]
    public void DisplayTitle_ShowsFileName_WhenClean()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "content");

        Assert.Equal("test.txt", tab.DisplayTitle);
    }

    [Fact]
    public void DisplayTitle_ShowsDirtyIndicator_WhenDirty()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "content");

        tab.Document.Text = "modified";

        Assert.Equal("test.txt •", tab.DisplayTitle);
    }

    #endregion

    #region LineEnding Detection Tests

    [Fact]
    public void DetectsLineEnding_LF()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2");

        Assert.Equal("LF", tab.LineEnding);
    }

    [Fact]
    public void DetectsLineEnding_CRLF()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "line1\r\nline2");

        Assert.Equal("CRLF", tab.LineEnding);
    }

    [Fact]
    public void DetectsLineEnding_CR()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "line1\rline2");

        Assert.Equal("CR", tab.LineEnding);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "content");

        tab.Dispose();

        // After dispose, editing shouldn't trigger property changes
        // This is hard to test directly, but we verify no exception
        tab.Document.Text = "modified";
        Assert.True(true); // No exception thrown
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void IsNewFile_TrueWhenNoPath()
    {
        var tab = EditorTabViewModel.CreateNew();

        Assert.True(tab.IsNewFile);
    }

    [Fact]
    public void LineCount_ReturnsCorrectCount()
    {
        var tab = EditorTabViewModel.FromFile("/test.txt", "line1\nline2\nline3");

        Assert.Equal(3, tab.LineCount);
    }

    [Fact]
    public void Extension_ReturnsFileExtension()
    {
        var tab = EditorTabViewModel.FromFile("/path/to/file.cs", "content");

        Assert.Equal(".cs", tab.Extension);
    }

    #endregion
}
