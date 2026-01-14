using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="FileContext"/> class.
/// Verifies factory methods, computed properties, and hash generation.
/// </summary>
public class FileContextTests
{
    #region FromFile Tests

    /// <summary>
    /// Verifies that FromFile calculates line count correctly.
    /// </summary>
    [Fact]
    public void FromFile_CalculatesLineCount()
    {
        // Arrange
        var content = "line1\nline2\nline3";

        // Act
        var context = FileContext.FromFile("/test/file.cs", content);

        // Assert
        Assert.Equal(3, context.LineCount);
    }

    /// <summary>
    /// Verifies that FromFile detects language from file extension.
    /// </summary>
    [Fact]
    public void FromFile_DetectsLanguage()
    {
        // Arrange & Act
        var csContext = FileContext.FromFile("/test/file.cs", "class Test {}");
        var pyContext = FileContext.FromFile("/test/script.py", "print('hello')");
        var txtContext = FileContext.FromFile("/test/readme.txt", "hello");

        // Assert
        Assert.Equal("csharp", csContext.Language);
        Assert.Equal("python", pyContext.Language);
        Assert.Null(txtContext.Language); // .txt is not mapped
    }

    /// <summary>
    /// Verifies that FromFile sets IsPartialContent to false.
    /// </summary>
    [Fact]
    public void FromFile_IsNotPartialContent()
    {
        // Arrange & Act
        var context = FileContext.FromFile("/test/file.cs", "content");

        // Assert
        Assert.False(context.IsPartialContent);
        Assert.Null(context.StartLine);
        Assert.Null(context.EndLine);
    }

    #endregion

    #region FromSelection Tests

    /// <summary>
    /// Verifies that FromSelection sets line range correctly.
    /// </summary>
    [Fact]
    public void FromSelection_SetsLineRange()
    {
        // Arrange & Act
        var context = FileContext.FromSelection("/test/file.cs", "selected content", 10, 25);

        // Assert
        Assert.True(context.IsPartialContent);
        Assert.Equal(10, context.StartLine);
        Assert.Equal(25, context.EndLine);
        Assert.Equal(16, context.LineCount); // 25 - 10 + 1
    }

    /// <summary>
    /// Verifies that FromSelection generates correct display label.
    /// </summary>
    [Fact]
    public void FromSelection_DisplayLabel_ShowsLineRange()
    {
        // Arrange & Act
        var context = FileContext.FromSelection("/path/to/test.cs", "content", 5, 15);

        // Assert
        Assert.Equal("test.cs (lines 5-15)", context.DisplayLabel);
    }

    #endregion

    #region ContentHash Tests

    /// <summary>
    /// Verifies that ContentHash is deterministic for same content.
    /// </summary>
    [Fact]
    public void ContentHash_IsDeterministic()
    {
        // Arrange
        var content = "same content for both";

        // Act
        var context1 = FileContext.FromFile("/test.cs", content);
        var context2 = FileContext.FromFile("/test.cs", content);

        // Assert
        Assert.Equal(context1.ContentHash, context2.ContentHash);
        Assert.Equal(16, context1.ContentHash.Length); // 16 hex chars
    }

    /// <summary>
    /// Verifies that ContentHash differs for different content.
    /// </summary>
    [Fact]
    public void ContentHash_DiffersForDifferentContent()
    {
        // Arrange & Act
        var context1 = FileContext.FromFile("/test.cs", "content A");
        var context2 = FileContext.FromFile("/test.cs", "content B");

        // Assert
        Assert.NotEqual(context1.ContentHash, context2.ContentHash);
    }

    #endregion

    #region FormatForLlmContext Tests

    /// <summary>
    /// Verifies that FormatForLlmContext includes file header for full files.
    /// </summary>
    [Fact]
    public void FormatForLlmContext_FullFile_IncludesFileHeader()
    {
        // Arrange
        var context = FileContext.FromFile("/path/to/example.cs", "public class Test {}");

        // Act
        var formatted = context.FormatForLlmContext();

        // Assert
        Assert.StartsWith("// File: example.cs [csharp]", formatted);
        Assert.Contains("public class Test {}", formatted);
    }

    /// <summary>
    /// Verifies that FormatForLlmContext includes line range for selections.
    /// </summary>
    [Fact]
    public void FormatForLlmContext_Selection_IncludesLineRange()
    {
        // Arrange
        var context = FileContext.FromSelection("/path/to/example.cs", "selected code", 10, 20);

        // Act
        var formatted = context.FormatForLlmContext();

        // Assert
        Assert.StartsWith("// File: example.cs (lines 10-20) [csharp]", formatted);
        Assert.Contains("selected code", formatted);
    }

    /// <summary>
    /// Verifies that FormatForLlmContext works without language detection.
    /// </summary>
    [Fact]
    public void FormatForLlmContext_UnknownLanguage_OmitsLanguageTag()
    {
        // Arrange
        var context = FileContext.FromFile("/path/to/readme.txt", "plain text");

        // Act
        var formatted = context.FormatForLlmContext();

        // Assert
        Assert.StartsWith("// File: readme.txt\n", formatted);
        Assert.DoesNotContain("[", formatted.Split('\n')[0]);
    }

    #endregion

    #region Computed Properties Tests

    /// <summary>
    /// Verifies that FileName is correctly extracted from path.
    /// </summary>
    [Fact]
    public void FileName_ExtractsFromPath()
    {
        // Arrange
        var context = FileContext.FromFile("/deeply/nested/path/file.cs", "content");

        // Assert
        Assert.Equal("file.cs", context.FileName);
    }

    /// <summary>
    /// Verifies that EstimatedTokens is calculated.
    /// </summary>
    [Fact]
    public void EstimatedTokens_IsCalculated()
    {
        // Arrange - 350 characters at ~3.5 chars/token = 100 tokens (with multiplier)
        var content = new string('a', 350);

        // Act
        var context = FileContext.FromFile("/test.txt", content);

        // Assert
        Assert.True(context.EstimatedTokens > 0);
    }

    /// <summary>
    /// Verifies that ContentSizeBytes returns UTF-8 byte count.
    /// </summary>
    [Fact]
    public void ContentSizeBytes_ReturnsUtf8ByteCount()
    {
        // Arrange
        var context = FileContext.FromFile("/test.txt", "hello"); // 5 ASCII chars = 5 bytes

        // Assert
        Assert.Equal(5, context.ContentSizeBytes);
    }

    #endregion
}
