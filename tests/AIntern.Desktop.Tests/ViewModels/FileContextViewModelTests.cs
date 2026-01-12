using Xunit;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for FileContextViewModel (v0.3.4c).
/// </summary>
public class FileContextViewModelTests
{
    #region FromFile Tests

    [Fact]
    public void FromFile_SetsCorrectProperties()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromFile(
            "/src/Services/UserService.cs",
            "public class UserService { }",
            50);

        // Assert
        Assert.Equal("/src/Services/UserService.cs", vm.FilePath);
        Assert.Equal("UserService.cs", vm.FileName);
        Assert.Equal("csharp", vm.Language);
        Assert.Equal("public class UserService { }", vm.Content);
        Assert.Equal(50, vm.EstimatedTokens);
        Assert.Equal(ContextAttachmentType.File, vm.AttachmentType);
        Assert.NotEqual(Guid.Empty, vm.Id);
    }

    [Fact]
    public void FromFile_DetectsLanguageFromExtension()
    {
        // Arrange & Act
        var csFile = FileContextViewModel.FromFile("/test.cs", "content", 10);
        var jsFile = FileContextViewModel.FromFile("/test.js", "content", 10);
        var pyFile = FileContextViewModel.FromFile("/test.py", "content", 10);

        // Assert
        Assert.Equal("csharp", csFile.Language);
        Assert.Equal("javascript", jsFile.Language);
        Assert.Equal("python", pyFile.Language);
    }

    [Fact]
    public void FromFile_CalculatesLineCount()
    {
        // Arrange
        var content = "line1\nline2\nline3\nline4";

        // Act
        var vm = FileContextViewModel.FromFile("/test.txt", content, 20);

        // Assert
        Assert.Equal(4, vm.LineCount);
    }

    [Fact]
    public void FromFile_SingleLine_HasLineCountOne()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromFile("/test.txt", "single line", 5);

        // Assert
        Assert.Equal(1, vm.LineCount);
    }

    #endregion

    #region FromSelection Tests

    [Fact]
    public void FromSelection_SetsCorrectProperties()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromSelection(
            "/src/Services/UserService.cs",
            "public void DoSomething() { }",
            10,
            15,
            30);

        // Assert
        Assert.Equal("/src/Services/UserService.cs", vm.FilePath);
        Assert.Equal("UserService.cs", vm.FileName);
        Assert.Equal(10, vm.StartLine);
        Assert.Equal(15, vm.EndLine);
        Assert.Equal(ContextAttachmentType.Selection, vm.AttachmentType);
    }

    [Fact]
    public void FromSelection_CalculatesLineCountFromRange()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromSelection(
            "/test.cs",
            "content",
            10,
            25,
            40);

        // Assert
        Assert.Equal(16, vm.LineCount); // 25 - 10 + 1 = 16
    }

    [Fact]
    public void FromSelection_SingleLine_HasLineCountOne()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromSelection("/test.cs", "x = 1;", 5, 5, 5);

        // Assert
        Assert.Equal(1, vm.LineCount);
    }

    #endregion

    #region FromClipboard Tests

    [Fact]
    public void FromClipboard_WithLanguage_SetsProperties()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromClipboard(
            "function test() { }",
            "javascript",
            15);

        // Assert
        Assert.Equal("Clipboard", vm.FileName);
        Assert.Equal("javascript", vm.Language);
        Assert.Equal(string.Empty, vm.FilePath);
        Assert.Equal(ContextAttachmentType.Clipboard, vm.AttachmentType);
        Assert.Equal(15, vm.EstimatedTokens);
    }

    [Fact]
    public void FromClipboard_WithoutLanguage_SetsNullLanguage()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromClipboard("some text", null, 5);

        // Assert
        Assert.Null(vm.Language);
        Assert.Equal("Clipboard", vm.FileName);
    }

    [Fact]
    public void FromClipboard_CalculatesLineCount()
    {
        // Arrange
        var content = "line1\nline2\nline3";

        // Act
        var vm = FileContextViewModel.FromClipboard(content, null, 10);

        // Assert
        Assert.Equal(3, vm.LineCount);
    }

    #endregion

    #region DisplayLabel Tests

    [Fact]
    public void DisplayLabel_File_ShowsFileName()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/path/to/MyService.cs", "content", 10);

        // Act & Assert
        Assert.Equal("MyService.cs", vm.DisplayLabel);
    }

    [Fact]
    public void DisplayLabel_Selection_ShowsFileNameAndLineRange()
    {
        // Arrange
        var vm = FileContextViewModel.FromSelection("/path/to/MyService.cs", "content", 10, 25, 20);

        // Act & Assert
        Assert.Equal("MyService.cs:10-25", vm.DisplayLabel);
    }

    [Fact]
    public void DisplayLabel_Clipboard_ShowsClipboard()
    {
        // Arrange
        var vm = FileContextViewModel.FromClipboard("content", "csharp", 10);

        // Act & Assert
        Assert.Equal("Clipboard", vm.DisplayLabel);
    }

    #endregion

    #region ShortLabel Tests

    [Fact]
    public void ShortLabel_ShortName_ReturnsUnchanged()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/path/File.cs", "content", 10);

        // Act & Assert
        Assert.Equal("File.cs", vm.ShortLabel);
    }

    [Fact]
    public void ShortLabel_ExactlyMaxLength_ReturnsUnchanged()
    {
        // Arrange - "12345678901234567890" is exactly 20 chars
        var vm = FileContextViewModel.FromFile("/path/12345678901234567890", "content", 10);

        // Act & Assert
        Assert.Equal("12345678901234567890", vm.ShortLabel);
    }

    [Fact]
    public void ShortLabel_LongName_TruncatesWithEllipsis()
    {
        // Arrange - Name longer than 20 chars
        // "VeryLongFileNameThatExceedsLimit.cs" = 35 chars
        var vm = FileContextViewModel.FromFile("/path/VeryLongFileNameThatExceedsLimit.cs", "content", 10);

        // Act
        var result = vm.ShortLabel;

        // Assert
        Assert.Equal(20, result.Length);
        Assert.EndsWith("...", result);
        // First 17 chars + "..." = 20 chars
        Assert.Equal("VeryLongFileNameT...", result);
    }

    [Fact]
    public void ShortLabel_Selection_TruncatesFullLabel()
    {
        // Arrange - Selection label with line range exceeds 20 chars
        var vm = FileContextViewModel.FromSelection("/path/LongFileName.cs", "content", 100, 200, 10);

        // Act
        var result = vm.ShortLabel;

        // Assert
        Assert.Equal(20, result.Length);
        Assert.EndsWith("...", result);
    }

    #endregion

    #region PreviewContent Tests

    [Fact]
    public void PreviewContent_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.txt", string.Empty, 0);

        // Act & Assert
        Assert.Equal(string.Empty, vm.PreviewContent);
    }

    [Fact]
    public void PreviewContent_ShortContent_ReturnsUnchanged()
    {
        // Arrange
        var content = "line1\nline2\nline3";
        var vm = FileContextViewModel.FromFile("/test.txt", content, 10);

        // Act & Assert
        Assert.Equal(content, vm.PreviewContent);
    }

    [Fact]
    public void PreviewContent_ManyLines_TruncatesWithIndicator()
    {
        // Arrange - Create 20 lines
        var lines = Enumerable.Range(1, 20).Select(i => $"line {i}");
        var content = string.Join("\n", lines);
        var vm = FileContextViewModel.FromFile("/test.txt", content, 50);

        // Act
        var preview = vm.PreviewContent;

        // Assert
        Assert.Contains("line 1", preview);
        Assert.Contains("line 15", preview);
        Assert.DoesNotContain("line 16", preview.Split("//")[0]); // Not in main content
        Assert.Contains("// ... (5 more lines)", preview);
    }

    [Fact]
    public void PreviewContent_ExactlyMaxLines_NoTruncation()
    {
        // Arrange - Create exactly 15 lines
        var lines = Enumerable.Range(1, 15).Select(i => $"line {i}");
        var content = string.Join("\n", lines);
        var vm = FileContextViewModel.FromFile("/test.txt", content, 40);

        // Act
        var preview = vm.PreviewContent;

        // Assert
        Assert.Contains("line 15", preview);
        Assert.DoesNotContain("more lines", preview);
    }

    [Fact]
    public void PreviewContent_LongSingleLine_TruncatesToMaxLength()
    {
        // Arrange - Create content longer than 500 chars
        var longLine = new string('x', 600);
        var vm = FileContextViewModel.FromFile("/test.txt", longLine, 100);

        // Act
        var preview = vm.PreviewContent;

        // Assert
        Assert.Contains("content truncated", preview);
    }

    #endregion

    #region IsPartialContent Tests

    [Fact]
    public void IsPartialContent_Selection_ReturnsTrue()
    {
        // Arrange
        var vm = FileContextViewModel.FromSelection("/test.cs", "content", 10, 20, 15);

        // Act & Assert
        Assert.True(vm.IsPartialContent);
    }

    [Fact]
    public void IsPartialContent_FullFile_ReturnsFalse()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.cs", "content", 10);

        // Act & Assert
        Assert.False(vm.IsPartialContent);
    }

    [Fact]
    public void IsPartialContent_Clipboard_ReturnsFalse()
    {
        // Arrange
        var vm = FileContextViewModel.FromClipboard("content", "csharp", 10);

        // Act & Assert
        Assert.False(vm.IsPartialContent);
    }

    #endregion

    #region Tooltip Tests

    [Fact]
    public void Tooltip_WithLanguage_ContainsAllInfo()
    {
        // Arrange
        var content = "line1\nline2\nline3";
        var vm = FileContextViewModel.FromFile("/path/Service.cs", content, 25);

        // Act
        var tooltip = vm.Tooltip;

        // Assert
        Assert.Contains("Service.cs", tooltip);
        Assert.Contains("csharp", tooltip);
        Assert.Contains("3 lines", tooltip);
        Assert.Contains("~25 tokens", tooltip);
        Assert.Contains(" • ", tooltip);
    }

    [Fact]
    public void Tooltip_WithoutLanguage_ExcludesLanguage()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/path/unknown.xyz", "content", 10);

        // Act
        var tooltip = vm.Tooltip;

        // Assert
        Assert.Contains("unknown.xyz", tooltip);
        Assert.Contains("1 lines", tooltip);
        Assert.Contains("~10 tokens", tooltip);
        // Should not have double separator from missing language
        Assert.DoesNotContain(" •  • ", tooltip);
    }

    #endregion

    #region IconKey Tests

    [Fact]
    public void IconKey_Clipboard_ReturnsClipboardIcon()
    {
        // Arrange
        var vm = FileContextViewModel.FromClipboard("content", "csharp", 10);

        // Act & Assert
        Assert.Equal("clipboard", vm.IconKey);
    }

    [Fact]
    public void IconKey_Selection_ReturnsSelectionIcon()
    {
        // Arrange
        var vm = FileContextViewModel.FromSelection("/test.cs", "content", 1, 10, 10);

        // Act & Assert
        Assert.Equal("selection", vm.IconKey);
    }

    [Fact]
    public void IconKey_CSharpFile_ReturnsDotnetIcon()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.cs", "content", 10);

        // Act & Assert
        Assert.Equal("dotnet", vm.IconKey);
    }

    [Fact]
    public void IconKey_JavaScriptFile_ReturnsJavaScriptIcon()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.js", "content", 10);

        // Act & Assert
        Assert.Equal("javascript", vm.IconKey);
    }

    [Fact]
    public void IconKey_UnknownFile_ReturnsFileIcon()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.unknown", "content", 10);

        // Act & Assert
        Assert.Equal("file", vm.IconKey);
    }

    #endregion

    #region Badge Tests

    [Fact]
    public void Badge_Selection_ReturnsSEL()
    {
        // Arrange
        var vm = FileContextViewModel.FromSelection("/test.cs", "content", 1, 10, 10);

        // Act & Assert
        Assert.Equal("SEL", vm.Badge);
    }

    [Fact]
    public void Badge_Clipboard_ReturnsCLIP()
    {
        // Arrange
        var vm = FileContextViewModel.FromClipboard("content", "csharp", 10);

        // Act & Assert
        Assert.Equal("CLIP", vm.Badge);
    }

    [Fact]
    public void Badge_CSharp_ReturnsCSharp()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.cs", "content", 10);

        // Act & Assert
        Assert.Equal("C#", vm.Badge);
    }

    [Fact]
    public void Badge_JavaScript_ReturnsJS()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.js", "content", 10);

        // Act & Assert
        Assert.Equal("JS", vm.Badge);
    }

    [Fact]
    public void Badge_TypeScript_ReturnsTS()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.ts", "content", 10);

        // Act & Assert
        Assert.Equal("TS", vm.Badge);
    }

    [Fact]
    public void Badge_Python_ReturnsPY()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.py", "content", 10);

        // Act & Assert
        Assert.Equal("PY", vm.Badge);
    }

    [Fact]
    public void Badge_UnknownLanguage_ReturnsTXT()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.unknown", "content", 10);

        // Act & Assert
        Assert.Equal("TXT", vm.Badge);
    }

    [Fact]
    public void Badge_Go_ReturnsGO()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.go", "content", 10);

        // Act & Assert
        Assert.Equal("GO", vm.Badge);
    }

    #endregion

    #region ToFileContext Tests

    [Fact]
    public void ToFileContext_FullFile_CreatesCorrectContext()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile(
            "/src/Services/UserService.cs",
            "public class UserService { }",
            50);

        // Act
        var context = vm.ToFileContext();

        // Assert
        Assert.Equal("/src/Services/UserService.cs", context.FilePath);
        Assert.Equal("UserService.cs", context.FileName);
        Assert.Equal("public class UserService { }", context.Content);
        Assert.Equal("csharp", context.Language);
        Assert.False(context.IsPartialContent);
    }

    [Fact]
    public void ToFileContext_Selection_CreatesPartialContext()
    {
        // Arrange
        var vm = FileContextViewModel.FromSelection(
            "/src/Services/UserService.cs",
            "public void DoWork() { }",
            10,
            15,
            30);

        // Act
        var context = vm.ToFileContext();

        // Assert
        Assert.Equal("/src/Services/UserService.cs", context.FilePath);
        Assert.Equal("public void DoWork() { }", context.Content);
        Assert.Equal(10, context.StartLine);
        Assert.Equal(15, context.EndLine);
        Assert.True(context.IsPartialContent);
    }

    [Fact]
    public void ToFileContext_Clipboard_UsesPlaceholderPath()
    {
        // Arrange
        var vm = FileContextViewModel.FromClipboard("some code", "csharp", 15);

        // Act
        var context = vm.ToFileContext();

        // Assert
        Assert.Equal("clipboard://content", context.FilePath);
        Assert.Equal("some code", context.Content);
    }

    #endregion

    #region AttachedAt Tests

    [Fact]
    public void FromFile_SetsAttachedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var vm = FileContextViewModel.FromFile("/test.cs", "content", 10);

        // Assert
        var after = DateTime.UtcNow;
        Assert.True(vm.AttachedAt >= before);
        Assert.True(vm.AttachedAt <= after);
    }

    #endregion

    #region Observable Property Tests

    [Fact]
    public void IsExpanded_DefaultsFalse()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromFile("/test.cs", "content", 10);

        // Assert
        Assert.False(vm.IsExpanded);
    }

    [Fact]
    public void IsExpanded_CanBeSet()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.cs", "content", 10);

        // Act
        vm.IsExpanded = true;

        // Assert
        Assert.True(vm.IsExpanded);
    }

    [Fact]
    public void IsHovered_DefaultsFalse()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromFile("/test.cs", "content", 10);

        // Assert
        Assert.False(vm.IsHovered);
    }

    [Fact]
    public void IsHovered_CanBeSet()
    {
        // Arrange
        var vm = FileContextViewModel.FromFile("/test.cs", "content", 10);

        // Act
        vm.IsHovered = true;

        // Assert
        Assert.True(vm.IsHovered);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void FromFile_EmptyContent_HandlesGracefully()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromFile("/test.cs", string.Empty, 0);

        // Assert
        Assert.Equal(0, vm.LineCount);
        Assert.Equal(string.Empty, vm.Content);
        Assert.Equal(string.Empty, vm.PreviewContent);
    }

    [Fact]
    public void FromFile_NullLanguage_ForUnknownExtension()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromFile("/test.xyz123", "content", 10);

        // Assert
        Assert.Null(vm.Language);
    }

    [Fact]
    public void FromSelection_SingleCharRange_Works()
    {
        // Arrange & Act
        var vm = FileContextViewModel.FromSelection("/test.cs", "x", 1, 1, 1);

        // Assert
        Assert.Equal(1, vm.LineCount);
        Assert.Equal(1, vm.StartLine);
        Assert.Equal(1, vm.EndLine);
    }

    #endregion
}
