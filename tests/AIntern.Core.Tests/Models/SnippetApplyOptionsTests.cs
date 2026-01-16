namespace AIntern.Core.Tests.Models;

using AIntern.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SnippetApplyOptions"/>.
/// </summary>
public class SnippetApplyOptionsTests
{
    [Fact]
    public void FullReplace_SetsCorrectMode()
    {
        var options = SnippetApplyOptions.FullReplace();
        Assert.Equal(SnippetInsertMode.ReplaceFile, options.InsertMode);
    }

    [Fact]
    public void ReplaceLines_SetsRangeCorrectly()
    {
        var options = SnippetApplyOptions.ReplaceLines(10, 20);
        Assert.Equal(SnippetInsertMode.Replace, options.InsertMode);
        Assert.NotNull(options.ReplaceRange);
        Assert.Equal(10, options.ReplaceRange.Value.StartLine);
        Assert.Equal(20, options.ReplaceRange.Value.EndLine);
    }

    [Fact]
    public void InsertAfterLine_SetsTargetLine()
    {
        var options = SnippetApplyOptions.InsertAfterLine(42);
        Assert.Equal(SnippetInsertMode.InsertAfter, options.InsertMode);
        Assert.Equal(42, options.TargetLine);
    }

    [Fact]
    public void AppendToFile_SetsBlankLineBefore()
    {
        var options = SnippetApplyOptions.AppendToFile(true);
        Assert.Equal(SnippetInsertMode.Append, options.InsertMode);
        Assert.True(options.AddBlankLineBefore);
    }

    [Fact]
    public void Validate_ReplaceWithoutRange_ReturnsError()
    {
        var options = new SnippetApplyOptions { InsertMode = SnippetInsertMode.Replace };
        var (isValid, error) = options.Validate();
        Assert.False(isValid);
        Assert.Contains("ReplaceRange", error);
    }

    [Fact]
    public void Validate_InsertAfterWithLine_ReturnsValid()
    {
        var options = SnippetApplyOptions.InsertAfterLine(10);
        var (isValid, error) = options.Validate();
        Assert.True(isValid);
        Assert.Null(error);
    }
}
