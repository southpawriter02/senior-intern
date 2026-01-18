using Xunit;
using AIntern.Core.Models.Terminal;
using AIntern.Core.Interfaces;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="CommandBlock"/>.
/// </summary>
/// <remarks>Added in v0.5.4a.</remarks>
public sealed class CommandBlockTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Constructor / Default Value Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var block = new CommandBlock();

        // Assert
        Assert.NotEqual(Guid.Empty, block.Id);
        Assert.Equal(string.Empty, block.Command);
        Assert.Null(block.Language);
        Assert.Null(block.Description);
        Assert.Null(block.WorkingDirectory);
        Assert.Null(block.DetectedShellType);
        Assert.Equal(1.0f, block.ConfidenceScore);
        Assert.False(block.IsPotentiallyDangerous);
        Assert.Null(block.DangerWarning);
        Assert.Equal(CommandBlockStatus.Pending, block.Status);
        Assert.Null(block.ExecutedAt);
        Assert.Null(block.ExecutedInSessionId);
        Assert.Null(block.OutputCaptureId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsMultiLine Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsMultiLine_ReturnsFalse_ForSingleLineCommand()
    {
        // Arrange
        var block = new CommandBlock { Command = "dotnet build" };

        // Act & Assert
        Assert.False(block.IsMultiLine);
        Assert.Equal(1, block.LineCount);
    }

    [Fact]
    public void IsMultiLine_ReturnsTrue_ForMultiLineCommand()
    {
        // Arrange
        var block = new CommandBlock
        {
            Command = "cd /project\ndotnet build\ndotnet test"
        };

        // Act & Assert
        Assert.True(block.IsMultiLine);
        Assert.Equal(3, block.LineCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FirstLine Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FirstLine_ReturnsFirstLineOnly()
    {
        // Arrange
        var block = new CommandBlock
        {
            Command = "npm install\nnpm run build"
        };

        // Act & Assert
        Assert.Equal("npm install", block.FirstLine);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CanRun Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Pending, true)]
    [InlineData(CommandBlockStatus.Copied, true)]
    [InlineData(CommandBlockStatus.SentToTerminal, true)]
    [InlineData(CommandBlockStatus.Executing, false)]
    [InlineData(CommandBlockStatus.Executed, false)]
    [InlineData(CommandBlockStatus.Failed, false)]
    [InlineData(CommandBlockStatus.Cancelled, false)]
    public void CanRun_AllowsNonExecutingStates(CommandBlockStatus status, bool expected)
    {
        // Arrange
        var block = new CommandBlock { Status = status };

        // Act & Assert
        Assert.Equal(expected, block.CanRun);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MarkCompleted Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkCompleted_SetsExecuted_OnZeroExitCode()
    {
        // Arrange
        var block = new CommandBlock();
        var captureId = Guid.NewGuid();

        // Act
        block.MarkCompleted(0, captureId);

        // Assert
        Assert.Equal(CommandBlockStatus.Executed, block.Status);
        Assert.Equal(captureId, block.OutputCaptureId);
        Assert.True(block.IsCompleted);
    }

    [Fact]
    public void MarkCompleted_SetsFailed_OnNonZeroExitCode()
    {
        // Arrange
        var block = new CommandBlock();

        // Act
        block.MarkCompleted(1);

        // Assert
        Assert.Equal(CommandBlockStatus.Failed, block.Status);
        Assert.True(block.IsCompleted);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MarkCopied Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkCopied_TransitionsFromPending()
    {
        // Arrange
        var block = new CommandBlock { Status = CommandBlockStatus.Pending };

        // Act
        block.MarkCopied();

        // Assert
        Assert.Equal(CommandBlockStatus.Copied, block.Status);
    }

    [Fact]
    public void MarkCopied_PreservesNonPendingStatus()
    {
        // Arrange
        var block = new CommandBlock { Status = CommandBlockStatus.Executed };

        // Act
        block.MarkCopied();

        // Assert - Status should not change
        Assert.Equal(CommandBlockStatus.Executed, block.Status);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MarkSentToTerminal Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkSentToTerminal_SetsSessionId()
    {
        // Arrange
        var block = new CommandBlock();
        var sessionId = Guid.NewGuid();

        // Act
        block.MarkSentToTerminal(sessionId);

        // Assert
        Assert.Equal(CommandBlockStatus.SentToTerminal, block.Status);
        Assert.Equal(sessionId, block.ExecutedInSessionId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MarkExecuting Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkExecuting_SetsStatusAndTimestamp()
    {
        // Arrange
        var block = new CommandBlock();
        var sessionId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        // Act
        block.MarkExecuting(sessionId);

        // Assert
        Assert.Equal(CommandBlockStatus.Executing, block.Status);
        Assert.Equal(sessionId, block.ExecutedInSessionId);
        Assert.NotNull(block.ExecutedAt);
        Assert.True(block.ExecutedAt >= before);
        Assert.True(block.IsRunning);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MarkCancelled Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkCancelled_SetsStatus()
    {
        // Arrange
        var block = new CommandBlock { Status = CommandBlockStatus.Executing };

        // Act
        block.MarkCancelled();

        // Assert
        Assert.Equal(CommandBlockStatus.Cancelled, block.Status);
        Assert.True(block.IsCompleted);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToDisplaySummary Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToDisplaySummary_TruncatesLongCommand()
    {
        // Arrange
        var longCommand = new string('x', 100);
        var block = new CommandBlock { Command = longCommand };

        // Act
        var summary = block.ToDisplaySummary();

        // Assert
        Assert.Equal(60, summary.Length);
        Assert.EndsWith("...", summary);
    }

    [Fact]
    public void ToDisplaySummary_IndicatesAdditionalLines()
    {
        // Arrange
        var block = new CommandBlock
        {
            Command = "line1\nline2\nline3\nline4"
        };

        // Act
        var summary = block.ToDisplaySummary();

        // Assert
        Assert.Contains("(+3 more lines)", summary);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToString_ContainsKeyInfo()
    {
        // Arrange
        var block = new CommandBlock
        {
            Command = "npm test",
            Status = CommandBlockStatus.Executing
        };

        // Act
        var result = block.ToString();

        // Assert
        Assert.Contains("CommandBlock", result);
        Assert.Contains("npm test", result);
        Assert.Contains("Executing", result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ShellType Integration Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectedShellType_CanBeSet()
    {
        // Arrange & Act
        var block = new CommandBlock
        {
            Command = "ls -la",
            Language = "bash",
            DetectedShellType = ShellType.Bash
        };

        // Assert
        Assert.Equal(ShellType.Bash, block.DetectedShellType);
    }
}
