using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalOutputCapture"/>.
/// </summary>
/// <remarks>Added in v0.5.4a.</remarks>
public sealed class TerminalOutputCaptureTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Constructor / Default Value Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var capture = new TerminalOutputCapture();

        // Assert
        Assert.NotEqual(Guid.Empty, capture.Id);
        Assert.Equal(string.Empty, capture.Output);
        Assert.False(capture.IsTruncated);
        Assert.Equal(0, capture.OriginalLength);
        Assert.Null(capture.Command);
        Assert.Null(capture.ExitCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EstimatedTokens Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void EstimatedTokens_CalculatesFromContentLength()
    {
        // Arrange
        var capture = new TerminalOutputCapture
        {
            Command = "npm test",    // 8 chars
            Output = new string('x', 200)  // 200 chars
        };
        // Expected: (200 + 8 + 50) / 4 = 64.5 ≈ 64

        // Act
        var tokens = capture.EstimatedTokens;

        // Assert
        Assert.Equal(64, tokens);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Duration Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Duration_CalculatesTimeSpan()
    {
        // Arrange
        var start = DateTime.UtcNow.AddSeconds(-10);
        var end = DateTime.UtcNow;
        var capture = new TerminalOutputCapture
        {
            StartedAt = start,
            CompletedAt = end
        };

        // Act
        var duration = capture.Duration;

        // Assert
        Assert.True(duration.TotalSeconds >= 9.9 && duration.TotalSeconds <= 10.1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsSuccess Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    public void IsSuccess_ChecksExitCode(int? exitCode, bool expected)
    {
        // Arrange
        var capture = new TerminalOutputCapture { ExitCode = exitCode };

        // Act & Assert
        Assert.Equal(expected, capture.IsSuccess);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LineCount Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void LineCount_CountsNewlines()
    {
        // Arrange
        var capture = new TerminalOutputCapture
        {
            Output = "line1\nline2\nline3"
        };

        // Act & Assert
        Assert.Equal(3, capture.LineCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToContextString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToContextString_FormatsAsMarkdown()
    {
        // Arrange
        var capture = new TerminalOutputCapture
        {
            Command = "npm test",
            Output = "All tests passed",
            ExitCode = 0,
            WorkingDirectory = "/home/user/project"
        };

        // Act
        var context = capture.ToContextString();

        // Assert
        Assert.Contains("```terminal", context);
        Assert.Contains("# Directory: /home/user/project", context);
        Assert.Contains("$ npm test", context);
        Assert.Contains("All tests passed", context);
        Assert.Contains("# Exit code: 0", context);
        Assert.Contains("```", context);
    }

    [Fact]
    public void ToContextString_IncludesTruncationNote()
    {
        // Arrange
        var capture = new TerminalOutputCapture
        {
            Output = "truncated output",
            IsTruncated = true,
            OriginalLength = 50000
        };

        // Act
        var context = capture.ToContextString();

        // Assert
        Assert.Contains("Output truncated from 50,000 characters", context);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToSummary Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToSummary_TruncatesLongOutput()
    {
        // Arrange
        var longOutput = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i} with some content"));
        var capture = new TerminalOutputCapture { Output = longOutput };

        // Act
        var summary = capture.ToSummary();

        // Assert
        Assert.True(summary.Length <= 200);
        Assert.Contains("more lines", summary);
    }

    [Fact]
    public void ToSummary_ReturnFullOutput_WhenShort()
    {
        // Arrange
        var capture = new TerminalOutputCapture { Output = "Short output" };

        // Act
        var summary = capture.ToSummary();

        // Assert
        Assert.Equal("Short output", summary);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Truncate Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Truncate_ReturnsOriginal_WhenUnderLimit()
    {
        // Arrange
        var capture = new TerminalOutputCapture { Output = "Short" };

        // Act
        var result = capture.Truncate(1000);

        // Assert
        Assert.Same(capture, result);
    }

    [Fact]
    public void Truncate_KeepsFirstAndLastParts()
    {
        // Arrange
        var output = new string('a', 100) + new string('b', 100) + new string('c', 100);
        var capture = new TerminalOutputCapture
        {
            Output = output,
            OriginalLength = output.Length
        };

        // Act
        var result = capture.Truncate(200);

        // Assert
        Assert.NotSame(capture, result);
        Assert.True(result.IsTruncated);
        Assert.Equal(output.Length, result.OriginalLength);
        Assert.Contains("truncated", result.Output);
        Assert.StartsWith("aaaaa", result.Output); // First part preserved
        Assert.EndsWith("ccccc", result.Output);   // Last part preserved
    }

    [Fact]
    public void Truncate_PreservesMetadata()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var commandBlockId = Guid.NewGuid();
        var capture = new TerminalOutputCapture
        {
            SessionId = sessionId,
            SessionName = "Terminal 1",
            CommandBlockId = commandBlockId,
            Command = "test",
            Output = new string('x', 1000),
            ExitCode = 0,
            StartedAt = DateTime.UtcNow.AddMinutes(-1),
            CompletedAt = DateTime.UtcNow,
            WorkingDirectory = "/home/user",
            CaptureMode = OutputCaptureMode.LastCommand
        };

        // Act
        var result = capture.Truncate(200);

        // Assert
        Assert.Equal(capture.Id, result.Id);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal("Terminal 1", result.SessionName);
        Assert.Equal(commandBlockId, result.CommandBlockId);
        Assert.Equal("test", result.Command);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("/home/user", result.WorkingDirectory);
        Assert.Equal(OutputCaptureMode.LastCommand, result.CaptureMode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HasOutput Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("output", true)]
    public void HasOutput_DetectsContent(string? output, bool expected)
    {
        // Arrange
        var capture = new TerminalOutputCapture { Output = output ?? string.Empty };

        // Act & Assert
        Assert.Equal(expected, capture.HasOutput);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToString_ContainsKeyInfo()
    {
        // Arrange
        var capture = new TerminalOutputCapture
        {
            Output = "line1\nline2",
            CaptureMode = OutputCaptureMode.LastCommand
        };

        // Act
        var result = capture.ToString();

        // Assert
        Assert.Contains("TerminalOutputCapture", result);
        Assert.Contains("2 lines", result);
        Assert.Contains("LastCommand", result);
    }
}
