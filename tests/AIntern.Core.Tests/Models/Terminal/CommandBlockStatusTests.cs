using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="CommandBlockStatus"/> and <see cref="CommandBlockStatusExtensions"/>.
/// </summary>
/// <remarks>Added in v0.5.4a.</remarks>
public sealed class CommandBlockStatusTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // IsTerminal Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Pending, false)]
    [InlineData(CommandBlockStatus.Copied, false)]
    [InlineData(CommandBlockStatus.SentToTerminal, false)]
    [InlineData(CommandBlockStatus.Executing, false)]
    [InlineData(CommandBlockStatus.Executed, true)]
    [InlineData(CommandBlockStatus.Failed, true)]
    [InlineData(CommandBlockStatus.Cancelled, true)]
    public void IsTerminal_IdentifiesFinalStates(CommandBlockStatus status, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, status.IsTerminal());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WasAttempted Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Pending, false)]
    [InlineData(CommandBlockStatus.Copied, false)]
    [InlineData(CommandBlockStatus.SentToTerminal, false)]
    [InlineData(CommandBlockStatus.Executing, true)]
    [InlineData(CommandBlockStatus.Executed, true)]
    [InlineData(CommandBlockStatus.Failed, true)]
    [InlineData(CommandBlockStatus.Cancelled, true)]
    public void WasAttempted_IdentifiesExecutionAttempts(CommandBlockStatus status, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, status.WasAttempted());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsRunning Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Pending, false)]
    [InlineData(CommandBlockStatus.Executing, true)]
    [InlineData(CommandBlockStatus.Executed, false)]
    public void IsRunning_IdentifiesExecutingState(CommandBlockStatus status, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, status.IsRunning());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CanExecute Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Pending, true)]
    [InlineData(CommandBlockStatus.Copied, true)]
    [InlineData(CommandBlockStatus.SentToTerminal, true)]
    [InlineData(CommandBlockStatus.Executing, false)]
    [InlineData(CommandBlockStatus.Executed, false)]
    [InlineData(CommandBlockStatus.Failed, false)]
    [InlineData(CommandBlockStatus.Cancelled, false)]
    public void CanExecute_IdentifiesExecutableStates(CommandBlockStatus status, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, status.CanExecute());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToDisplayString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Pending, "Ready")]
    [InlineData(CommandBlockStatus.Copied, "Copied")]
    [InlineData(CommandBlockStatus.SentToTerminal, "In Terminal")]
    [InlineData(CommandBlockStatus.Executing, "Running...")]
    [InlineData(CommandBlockStatus.Executed, "Completed")]
    [InlineData(CommandBlockStatus.Failed, "Failed")]
    [InlineData(CommandBlockStatus.Cancelled, "Cancelled")]
    public void ToDisplayString_ReturnsUserFriendlyText(CommandBlockStatus status, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, status.ToDisplayString());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToIconName Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Pending, "PlayIcon")]
    [InlineData(CommandBlockStatus.Copied, "ClipboardIcon")]
    [InlineData(CommandBlockStatus.Executing, "SpinnerIcon")]
    [InlineData(CommandBlockStatus.Executed, "CheckIcon")]
    [InlineData(CommandBlockStatus.Failed, "ErrorIcon")]
    [InlineData(CommandBlockStatus.Cancelled, "CancelIcon")]
    public void ToIconName_ReturnsCorrectIconName(CommandBlockStatus status, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, status.ToIconName());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToColorKey Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Executed, "success")]
    [InlineData(CommandBlockStatus.Failed, "error")]
    [InlineData(CommandBlockStatus.Cancelled, "warning")]
    [InlineData(CommandBlockStatus.Executing, "info")]
    [InlineData(CommandBlockStatus.Pending, "neutral")]
    public void ToColorKey_ReturnsSemanticColorKey(CommandBlockStatus status, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, status.ToColorKey());
    }
}
