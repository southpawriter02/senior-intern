namespace AIntern.Core.Tests.Models;

using Xunit;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF OPTIONS TESTS (v0.4.2b)                                             │
// │ Unit tests for the DiffOptions model.                                    │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffOptionsTests
{
    [Fact]
    public void Default_HasStandardValues()
    {
        var options = DiffOptions.Default;

        Assert.Equal(3, options.ContextLines);
        Assert.Equal(6, options.HunkSeparationThreshold);
        Assert.True(options.ComputeInlineDiffs);
        Assert.False(options.IgnoreWhitespace);
        Assert.False(options.IgnoreCase);
        Assert.True(options.TrimTrailingWhitespace);
        Assert.Equal(500, options.MaxInlineDiffLineLength);
        Assert.Equal(0.3, options.InlineDiffSimilarityThreshold);
    }

    [Fact]
    public void Compact_HasFewerContextLines()
    {
        var options = DiffOptions.Compact;

        Assert.Equal(1, options.ContextLines);
        Assert.Equal(4, options.HunkSeparationThreshold);
    }

    [Fact]
    public void Full_HasMoreContextLines()
    {
        var options = DiffOptions.Full;

        Assert.Equal(10, options.ContextLines);
        Assert.Equal(20, options.HunkSeparationThreshold);
    }

    [Fact]
    public void IgnoreWhitespaceOptions_SetsCorrectFlags()
    {
        var options = DiffOptions.IgnoreWhitespaceOptions;

        Assert.True(options.IgnoreWhitespace);
        Assert.True(options.TrimTrailingWhitespace);
    }

    [Fact]
    public void CustomOptions_AllPropertiesSettable()
    {
        var options = new DiffOptions
        {
            ContextLines = 5,
            ComputeInlineDiffs = false,
            IgnoreWhitespace = true,
            IgnoreCase = true,
            TrimTrailingWhitespace = false,
            HunkSeparationThreshold = 10,
            MaxInlineDiffLineLength = 200,
            InlineDiffSimilarityThreshold = 0.5
        };

        Assert.Equal(5, options.ContextLines);
        Assert.False(options.ComputeInlineDiffs);
        Assert.True(options.IgnoreWhitespace);
        Assert.True(options.IgnoreCase);
        Assert.False(options.TrimTrailingWhitespace);
        Assert.Equal(10, options.HunkSeparationThreshold);
        Assert.Equal(200, options.MaxInlineDiffLineLength);
        Assert.Equal(0.5, options.InlineDiffSimilarityThreshold);
    }
}
