namespace AIntern.Services.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using AIntern.Core.Models;

/// <summary>
/// Unit tests for <see cref="ContextFormatter"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4b.</para>
/// </remarks>
public class ContextFormatterTests
{
    private readonly ContextFormatter _formatter = new();

    #region Test Helpers

    private static FileContext CreateTestContext(
        string content = "public class Test { }",
        string? language = "csharp",
        string filePath = "/project/src/Test.cs",
        int? startLine = null,
        int? endLine = null)
    {
        return new FileContext
        {
            FilePath = filePath,
            Content = content,
            Language = language,
            LineCount = content.Split('\n').Length,
            EstimatedTokens = content.Length / 4,
            StartLine = startLine,
            EndLine = endLine,
            ContentHash = "ABCD1234EFGH5678"
        };
    }

    #endregion

    #region FormatForPrompt Tests

    /// <summary>
    /// Verifies FormatForPrompt with empty context list returns empty string.
    /// </summary>
    [Fact]
    public void FormatForPrompt_EmptyContexts_ReturnsEmpty()
    {
        // Arrange
        var contexts = Array.Empty<FileContext>();

        // Act
        var result = _formatter.FormatForPrompt(contexts);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Verifies FormatForPrompt with single context includes introduction and conclusion.
    /// </summary>
    [Fact]
    public void FormatForPrompt_SingleContext_IncludesIntroAndConclusion()
    {
        // Arrange
        var context = CreateTestContext();
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForPrompt(contexts);

        // Assert
        Assert.Contains("code context", result);
        Assert.Contains("consider this context", result);
        Assert.Contains("Test.cs", result);
    }

    /// <summary>
    /// Verifies FormatForPrompt with multiple contexts formats all of them.
    /// </summary>
    [Fact]
    public void FormatForPrompt_MultipleContexts_FormatsAll()
    {
        // Arrange
        var contexts = new[]
        {
            CreateTestContext(filePath: "/project/src/File1.cs"),
            CreateTestContext(filePath: "/project/src/File2.cs")
        };

        // Act
        var result = _formatter.FormatForPrompt(contexts);

        // Assert
        Assert.Contains("File1.cs", result);
        Assert.Contains("File2.cs", result);
    }

    #endregion

    #region FormatSingleContext Tests

    /// <summary>
    /// Verifies FormatSingleContext includes header and code block.
    /// </summary>
    [Fact]
    public void FormatSingleContext_FullFile_IncludesHeaderAndCodeBlock()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        var result = _formatter.FormatSingleContext(context);

        // Assert
        Assert.Contains("### File: `Test.cs`", result);
        Assert.Contains("```csharp", result);
        Assert.Contains("public class Test", result);
    }

    /// <summary>
    /// Verifies FormatSingleContext with selection shows line range.
    /// </summary>
    [Fact]
    public void FormatSingleContext_Selection_ShowsLineRange()
    {
        // Arrange
        var context = CreateTestContext(startLine: 10, endLine: 20);

        // Act
        var result = _formatter.FormatSingleContext(context);

        // Assert
        Assert.Contains("lines 10-20", result);
    }

    /// <summary>
    /// Verifies FormatSingleContext with language shows syntax hint.
    /// </summary>
    [Fact]
    public void FormatSingleContext_WithLanguage_ShowsSyntaxHint()
    {
        // Arrange
        var context = CreateTestContext(language: "typescript");

        // Act
        var result = _formatter.FormatSingleContext(context);

        // Assert
        Assert.Contains("```typescript", result);
    }

    #endregion

    #region FormatForDisplay Tests

    /// <summary>
    /// Verifies FormatForDisplay with empty list returns empty string.
    /// </summary>
    [Fact]
    public void FormatForDisplay_EmptyContexts_ReturnsEmpty()
    {
        // Arrange
        var contexts = Array.Empty<FileContext>();

        // Act
        var result = _formatter.FormatForDisplay(contexts);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Verifies FormatForDisplay collapsed mode shows preview.
    /// </summary>
    [Fact]
    public void FormatForDisplay_Collapsed_ShowsPreview()
    {
        // Arrange
        var multiLineContent = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line {i}"));
        var context = CreateTestContext(content: multiLineContent);

        // Act
        var result = _formatter.FormatForDisplay(new[] { context }, expanded: false);

        // Assert
        Assert.Contains("more lines", result);
    }

    /// <summary>
    /// Verifies FormatForDisplay expanded mode shows full content.
    /// </summary>
    [Fact]
    public void FormatForDisplay_Expanded_ShowsFullContent()
    {
        // Arrange
        var multiLineContent = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line {i}"));
        var context = CreateTestContext(content: multiLineContent);

        // Act
        var result = _formatter.FormatForDisplay(new[] { context }, expanded: true);

        // Assert
        Assert.Contains("line 20", result);
        Assert.DoesNotContain("more lines", result);
    }

    /// <summary>
    /// Verifies FormatForDisplay with selection shows line info.
    /// </summary>
    [Fact]
    public void FormatForDisplay_Selection_ShowsLineInfo()
    {
        // Arrange
        var context = CreateTestContext(startLine: 5, endLine: 15);

        // Act
        var result = _formatter.FormatForDisplay(new[] { context });

        // Assert
        Assert.Contains("_Lines 5-15_", result);
    }

    #endregion

    #region FormatCodeBlock Tests

    /// <summary>
    /// Verifies FormatCodeBlock with language includes syntax hint.
    /// </summary>
    [Fact]
    public void FormatCodeBlock_WithLanguage_IncludesSyntaxHint()
    {
        // Arrange
        var content = "console.log('hello');";
        var language = "javascript";

        // Act
        var result = _formatter.FormatCodeBlock(content, language);

        // Assert
        Assert.StartsWith("```javascript\n", result);
        Assert.EndsWith("```\n", result);
    }

    /// <summary>
    /// Verifies FormatCodeBlock without language uses plain block.
    /// </summary>
    [Fact]
    public void FormatCodeBlock_NoLanguage_UsesPlainBlock()
    {
        // Arrange
        var content = "some text";

        // Act
        var result = _formatter.FormatCodeBlock(content, null);

        // Assert
        Assert.StartsWith("```\n", result);
    }

    /// <summary>
    /// Verifies FormatCodeBlock handles special characters.
    /// </summary>
    [Fact]
    public void FormatCodeBlock_SpecialChars_PreservesContent()
    {
        // Arrange
        var content = "<html>&amp;\"'</html>";

        // Act
        var result = _formatter.FormatCodeBlock(content, "html");

        // Assert
        Assert.Contains("<html>&amp;\"'</html>", result);
    }

    #endregion

    #region FormatContextHeader Tests

    /// <summary>
    /// Verifies FormatContextHeader for full file.
    /// </summary>
    [Fact]
    public void FormatContextHeader_FullFile_ShowsFileName()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        var result = _formatter.FormatContextHeader(context);

        // Assert
        Assert.Contains("### File: `Test.cs`", result);
        Assert.Contains("(csharp)", result);
    }

    /// <summary>
    /// Verifies FormatContextHeader with line range.
    /// </summary>
    [Fact]
    public void FormatContextHeader_WithLineRange_ShowsLines()
    {
        // Arrange
        var context = CreateTestContext(startLine: 10, endLine: 25);

        // Act
        var result = _formatter.FormatContextHeader(context);

        // Assert
        Assert.Contains("lines 10-25", result);
    }

    /// <summary>
    /// Verifies FormatContextHeader with path different from filename.
    /// </summary>
    [Fact]
    public void FormatContextHeader_WithPath_ShowsRelativePath()
    {
        // Arrange
        var context = CreateTestContext(filePath: "/Users/dev/project/src/services/UserService.cs");

        // Act
        var result = _formatter.FormatContextHeader(context);

        // Assert
        Assert.Contains("_Path:", result);
    }

    /// <summary>
    /// Verifies FormatContextHeader with selection uses selection template.
    /// </summary>
    [Fact]
    public void FormatContextHeader_Selection_UsesSelectionTemplate()
    {
        // Arrange
        var context = CreateTestContext(startLine: 5, endLine: 10);

        // Act
        var result = _formatter.FormatContextHeader(context);

        // Assert
        Assert.Contains("Selected Code", result);
    }

    #endregion

    #region FormatForStorage Tests

    /// <summary>
    /// Verifies FormatForStorage creates valid JSON.
    /// </summary>
    [Fact]
    public void FormatForStorage_SingleContext_CreatesValidJson()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        var result = _formatter.FormatForStorage(new[] { context });

        // Assert
        Assert.StartsWith("[", result);
        Assert.EndsWith("]", result);
        Assert.Contains("\"FilePath\":", result);
        Assert.Contains("\"ContentHash\":", result);
    }

    /// <summary>
    /// Verifies FormatForStorage does not include full content.
    /// </summary>
    [Fact]
    public void FormatForStorage_ExcludesContent()
    {
        // Arrange
        var context = CreateTestContext(content: "SECRET_CONTENT_NOT_STORED");

        // Act
        var result = _formatter.FormatForStorage(new[] { context });

        // Assert
        Assert.DoesNotContain("SECRET_CONTENT_NOT_STORED", result);
        Assert.Contains("ContentLength", result);
    }

    #endregion

    #region ContextPromptTemplates Tests

    /// <summary>
    /// Verifies templates have expected placeholders.
    /// </summary>
    [Fact]
    public void ContextPromptTemplates_HasExpectedTemplates()
    {
        // Assert
        Assert.Contains("{FileName}", ContextPromptTemplates.FileHeaderTemplate);
        Assert.Contains("{Language}", ContextPromptTemplates.FileHeaderTemplate);
        Assert.Contains("{StartLine}", ContextPromptTemplates.SelectionHeaderTemplate);
        Assert.Contains("{RemainingLines}", ContextPromptTemplates.PreviewTruncation);
    }

    /// <summary>
    /// Verifies PromptIntroduction and PromptConclusion are not empty.
    /// </summary>
    [Fact]
    public void ContextPromptTemplates_IntroAndConclusionNotEmpty()
    {
        // Assert
        Assert.NotEmpty(ContextPromptTemplates.PromptIntroduction);
        Assert.NotEmpty(ContextPromptTemplates.PromptConclusion);
    }

    #endregion
}
