namespace AIntern.Desktop.Tests.ViewModels;

using System;
using Xunit;
using AIntern.Desktop.ViewModels;
using AIntern.Core.Models;

/// <summary>
/// Unit tests for <see cref="FileContextViewModel"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4c.</para>
/// </remarks>
public class FileContextViewModelTests
{
    #region FromFile Tests

    /// <summary>
    /// Verifies FromFile creates correct ViewModel from file path.
    /// </summary>
    [Fact]
    public void FromFile_ValidPath_CreatesViewModel()
    {
        // Arrange
        var filePath = "/project/src/UserService.cs";
        var content = "public class UserService { }";
        var tokens = 50;

        // Act
        var vm = FileContextViewModel.FromFile(filePath, content, tokens);

        // Assert
        Assert.Equal("UserService.cs", vm.FileName);
        Assert.Equal(filePath, vm.FilePath);
        Assert.Equal(content, vm.Content);
        Assert.Equal(tokens, vm.EstimatedTokens);
        Assert.Equal(ContextAttachmentType.File, vm.AttachmentType);
    }

    /// <summary>
    /// Verifies FromFile detects language correctly.
    /// </summary>
    [Fact]
    public void FromFile_DetectsLanguage()
    {
        // Act
        var vmCs = FileContextViewModel.FromFile("/test.cs", "code", 10);
        var vmPy = FileContextViewModel.FromFile("/test.py", "code", 10);
        var vmJs = FileContextViewModel.FromFile("/test.js", "code", 10);

        // Assert
        Assert.Equal("csharp", vmCs.Language);
        Assert.Equal("python", vmPy.Language);
        Assert.Equal("javascript", vmJs.Language);
    }

    /// <summary>
    /// Verifies FromFile calculates line count correctly.
    /// </summary>
    [Fact]
    public void FromFile_CalculatesLineCount()
    {
        // Arrange
        var content = "line1\nline2\nline3";

        // Act
        var vm = FileContextViewModel.FromFile("/test.txt", content, 10);

        // Assert
        Assert.Equal(3, vm.LineCount);
    }

    #endregion

    #region FromSelection Tests

    /// <summary>
    /// Verifies FromSelection creates correct ViewModel.
    /// </summary>
    [Fact]
    public void FromSelection_ValidParams_CreatesViewModel()
    {
        // Arrange
        var filePath = "/project/src/Program.cs";
        var content = "selected code";
        var startLine = 10;
        var endLine = 25;

        // Act
        var vm = FileContextViewModel.FromSelection(filePath, content, startLine, endLine, 30);

        // Assert
        Assert.Equal(startLine, vm.StartLine);
        Assert.Equal(endLine, vm.EndLine);
        Assert.Equal(ContextAttachmentType.Selection, vm.AttachmentType);
    }

    /// <summary>
    /// Verifies FromSelection calculates line count from range.
    /// </summary>
    [Fact]
    public void FromSelection_CalculatesLineCountFromRange()
    {
        // Act
        var vm = FileContextViewModel.FromSelection("/test.cs", "code", 10, 25, 10);

        // Assert
        Assert.Equal(16, vm.LineCount); // 25 - 10 + 1
    }

    /// <summary>
    /// Verifies FromSelection sets IsPartialContent to true.
    /// </summary>
    [Fact]
    public void FromSelection_IsPartialContent_True()
    {
        // Act
        var vm = FileContextViewModel.FromSelection("/test.cs", "code", 5, 10, 10);

        // Assert
        Assert.True(vm.IsPartialContent);
    }

    #endregion

    #region FromClipboard Tests

    /// <summary>
    /// Verifies FromClipboard with language.
    /// </summary>
    [Fact]
    public void FromClipboard_WithLanguage_CreatesViewModel()
    {
        // Arrange
        var content = "def hello(): pass";
        var language = "python";

        // Act
        var vm = FileContextViewModel.FromClipboard(content, language, 10);

        // Assert
        Assert.Equal("Clipboard", vm.FileName);
        Assert.Equal(language, vm.Language);
        Assert.Equal(ContextAttachmentType.Clipboard, vm.AttachmentType);
    }

    /// <summary>
    /// Verifies FromClipboard without language.
    /// </summary>
    [Fact]
    public void FromClipboard_WithoutLanguage_Works()
    {
        // Act
        var vm = FileContextViewModel.FromClipboard("some text", null, 5);

        // Assert
        Assert.Null(vm.Language);
    }

    #endregion

    #region DisplayLabel Tests

    /// <summary>
    /// Verifies DisplayLabel for file.
    /// </summary>
    [Fact]
    public void DisplayLabel_File_ShowsFileName()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test/MyClass.cs", "code", 10);

        // Assert
        Assert.Equal("MyClass.cs", vm.DisplayLabel);
    }

    /// <summary>
    /// Verifies DisplayLabel for selection.
    /// </summary>
    [Fact]
    public void DisplayLabel_Selection_ShowsLineRange()
    {
        // Arrange
        var vm = FileContextViewModel.FromSelection("/test/MyClass.cs", "code", 10, 25, 15);

        // Assert
        Assert.Equal("MyClass.cs:10-25", vm.DisplayLabel);
    }

    /// <summary>
    /// Verifies DisplayLabel for clipboard.
    /// </summary>
    [Fact]
    public void DisplayLabel_Clipboard_ShowsClipboard()
    {
        // Arrange
        var vm = FileContextViewModel.FromClipboard("code", null, 5);

        // Assert
        Assert.Equal("Clipboard", vm.DisplayLabel);
    }

    #endregion

    #region ShortLabel Tests

    /// <summary>
    /// Verifies ShortLabel for normal length name.
    /// </summary>
    [Fact]
    public void ShortLabel_NormalName_NotTruncated()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test/Short.cs", "code", 10);

        // Assert
        Assert.Equal("Short.cs", vm.ShortLabel);
    }

    /// <summary>
    /// Verifies ShortLabel truncates long names.
    /// </summary>
    [Fact]
    public void ShortLabel_LongName_Truncated()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test/VeryLongServiceImplementationFile.cs", "code", 10);

        // Assert
        Assert.Contains("...", vm.ShortLabel);
        Assert.True(vm.ShortLabel.Length <= 20 + 10); // Allow for line range
    }

    #endregion

    #region PreviewContent Tests

    /// <summary>
    /// Verifies PreviewContent for empty content.
    /// </summary>
    [Fact]
    public void PreviewContent_Empty_ReturnsEmptyIndicator()
    {
        // Arrange
        var vm = new FileContextViewModel { Content = string.Empty };

        // Assert
        Assert.Equal("(empty)", vm.PreviewContent);
    }

    /// <summary>
    /// Verifies PreviewContent for short content.
    /// </summary>
    [Fact]
    public void PreviewContent_ShortContent_ReturnsFullContent()
    {
        // Arrange
        var content = "short content";
        var vm = new FileContextViewModel { Content = content };

        // Assert
        Assert.Equal(content, vm.PreviewContent);
    }

    /// <summary>
    /// Verifies PreviewContent truncates long content.
    /// </summary>
    [Fact]
    public void PreviewContent_LongContent_Truncated()
    {
        // Arrange
        var lines = string.Join("\n", System.Linq.Enumerable.Range(1, 25).Select(i => $"line {i}"));
        var vm = new FileContextViewModel { Content = lines };

        // Assert
        Assert.Contains("more lines", vm.PreviewContent);
    }

    /// <summary>
    /// Verifies PreviewContent includes remaining line count.
    /// </summary>
    [Fact]
    public void PreviewContent_TruncationIndicator_ShowsRemainingLineCount()
    {
        // Arrange
        var lines = string.Join("\n", System.Linq.Enumerable.Range(1, 20).Select(i => $"line {i}"));
        var vm = new FileContextViewModel { Content = lines };

        // Assert - 20 lines, preview shows 15, so 5 remaining
        Assert.Contains("5 more lines", vm.PreviewContent);
    }

    #endregion

    #region IsPartialContent Tests

    /// <summary>
    /// Verifies IsPartialContent for selection.
    /// </summary>
    [Fact]
    public void IsPartialContent_Selection_True()
    {
        // Arrange
        var vm = new FileContextViewModel { StartLine = 10, EndLine = 20 };

        // Assert
        Assert.True(vm.IsPartialContent);
    }

    /// <summary>
    /// Verifies IsPartialContent for file.
    /// </summary>
    [Fact]
    public void IsPartialContent_File_False()
    {
        // Arrange
        var vm = new FileContextViewModel();

        // Assert
        Assert.False(vm.IsPartialContent);
    }

    #endregion

    #region Tooltip Tests

    /// <summary>
    /// Verifies Tooltip with language.
    /// </summary>
    [Fact]
    public void Tooltip_WithLanguage_IncludesAllParts()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.cs", "code", 100);
        vm.LineCount = 50;

        // Assert
        Assert.Contains("test.cs", vm.Tooltip);
        Assert.Contains("csharp", vm.Tooltip);
        Assert.Contains("50 lines", vm.Tooltip);
        Assert.Contains("~100 tokens", vm.Tooltip);
    }

    /// <summary>
    /// Verifies Tooltip without language.
    /// </summary>
    [Fact]
    public void Tooltip_WithoutLanguage_ExcludesLanguage()
    {
        // Arrange
        var vm = new FileContextViewModel
        {
            FileName = "file.txt",
            LineCount = 10,
            EstimatedTokens = 20
        };

        // Assert
        Assert.Contains("10 lines", vm.Tooltip);
        Assert.Contains("~20 tokens", vm.Tooltip);
    }

    #endregion

    #region IconKey Tests

    /// <summary>
    /// Verifies IconKey for clipboard.
    /// </summary>
    [Fact]
    public void IconKey_Clipboard_ReturnsClipboardIcon()
    {
        // Arrange
        var vm = new FileContextViewModel { AttachmentType = ContextAttachmentType.Clipboard };

        // Assert
        Assert.Equal("ClipboardIcon", vm.IconKey);
    }

    /// <summary>
    /// Verifies IconKey for selection.
    /// </summary>
    [Fact]
    public void IconKey_Selection_ReturnsSelectionIcon()
    {
        // Arrange
        var vm = new FileContextViewModel { AttachmentType = ContextAttachmentType.Selection };

        // Assert
        Assert.Equal("SelectionIcon", vm.IconKey);
    }

    /// <summary>
    /// Verifies IconKey for C# file.
    /// </summary>
    [Fact]
    public void IconKey_CSharp_ReturnsCSharpIcon()
    {
        // Arrange
        var vm = new FileContextViewModel { Language = "csharp" };

        // Assert
        Assert.Equal("CSharpIcon", vm.IconKey);
    }

    /// <summary>
    /// Verifies IconKey for unknown language.
    /// </summary>
    [Fact]
    public void IconKey_Unknown_ReturnsFileCodeIcon()
    {
        // Arrange
        var vm = new FileContextViewModel { Language = "unknown" };

        // Assert
        Assert.Equal("FileCodeIcon", vm.IconKey);
    }

    #endregion

    #region Badge Tests

    /// <summary>
    /// Verifies Badge for selection.
    /// </summary>
    [Fact]
    public void Badge_Selection_ReturnsSel()
    {
        // Arrange
        var vm = new FileContextViewModel { AttachmentType = ContextAttachmentType.Selection };

        // Assert
        Assert.Equal("SEL", vm.Badge);
    }

    /// <summary>
    /// Verifies Badge for clipboard.
    /// </summary>
    [Fact]
    public void Badge_Clipboard_ReturnsClip()
    {
        // Arrange
        var vm = new FileContextViewModel { AttachmentType = ContextAttachmentType.Clipboard };

        // Assert
        Assert.Equal("CLIP", vm.Badge);
    }

    /// <summary>
    /// Verifies Badge for C# file.
    /// </summary>
    [Fact]
    public void Badge_CSharp_ReturnsCSharp()
    {
        // Arrange
        var vm = new FileContextViewModel { Language = "csharp" };

        // Assert
        Assert.Equal("C#", vm.Badge);
    }

    /// <summary>
    /// Verifies Badge for JavaScript file.
    /// </summary>
    [Fact]
    public void Badge_JavaScript_ReturnsJs()
    {
        // Arrange
        var vm = new FileContextViewModel { Language = "javascript" };

        // Assert
        Assert.Equal("JS", vm.Badge);
    }

    #endregion

    #region ToFileContext Tests

    /// <summary>
    /// Verifies ToFileContext converts correctly.
    /// </summary>
    [Fact]
    public void ToFileContext_ConvertsFully()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.cs", "code", 100);
        vm.LineCount = 10;

        // Act
        var context = vm.ToFileContext();

        // Assert
        Assert.Equal(vm.Id, context.Id);
        Assert.Equal(vm.FilePath, context.FilePath);
        Assert.Equal(vm.Content, context.Content);
        Assert.Equal(vm.Language, context.Language);
        Assert.Equal(vm.LineCount, context.LineCount);
        Assert.Equal(vm.EstimatedTokens, context.EstimatedTokens);
    }

    /// <summary>
    /// Verifies ToFileContext preserves selection info.
    /// </summary>
    [Fact]
    public void ToFileContext_Selection_PreservesLineRange()
    {
        // Arrange
        var vm = FileContextViewModel.FromSelection("/test.cs", "code", 10, 25, 50);

        // Act
        var context = vm.ToFileContext();

        // Assert
        Assert.Equal(10, context.StartLine);
        Assert.Equal(25, context.EndLine);
        Assert.True(context.IsPartialContent);
    }

    #endregion

    #region FromFileContext Tests

    /// <summary>
    /// Verifies FromFileContext creates correct ViewModel.
    /// </summary>
    [Fact]
    public void FromFileContext_CreatesViewModel()
    {
        // Arrange
        var context = new FileContext
        {
            FilePath = "/test/Service.cs",
            Content = "public class Service { }",
            Language = "csharp",
            LineCount = 5,
            EstimatedTokens = 25
        };

        // Act
        var vm = FileContextViewModel.FromFileContext(context);

        // Assert
        Assert.Equal(context.Id, vm.Id);
        Assert.Equal("Service.cs", vm.FileName);
        Assert.Equal(context.Language, vm.Language);
        Assert.Equal(ContextAttachmentType.File, vm.AttachmentType);
    }

    #endregion

    #region ContextAttachmentType Tests

    /// <summary>
    /// Verifies ContextAttachmentType enum values.
    /// </summary>
    [Fact]
    public void ContextAttachmentType_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ContextAttachmentType.File);
        Assert.Equal(1, (int)ContextAttachmentType.Selection);
        Assert.Equal(2, (int)ContextAttachmentType.Clipboard);
    }

    #endregion
}
