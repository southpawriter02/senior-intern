using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="OutputCaptureMode"/> and <see cref="OutputCaptureModeExtensions"/>.
/// </summary>
/// <remarks>Added in v0.5.4a.</remarks>
public sealed class OutputCaptureModeTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // ToDescription Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(OutputCaptureMode.FullBuffer, "Entire terminal buffer")]
    [InlineData(OutputCaptureMode.LastCommand, "Last command output")]
    [InlineData(OutputCaptureMode.Selection, "Selected text")]
    [InlineData(OutputCaptureMode.LastNLines, "Last N lines")]
    [InlineData(OutputCaptureMode.Manual, "Manual selection")]
    public void ToDescription_ReturnsHumanReadableText(OutputCaptureMode mode, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, mode.ToDescription());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetDefaultMaxCharacters Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(OutputCaptureMode.FullBuffer, 50000)]
    [InlineData(OutputCaptureMode.LastCommand, 20000)]
    [InlineData(OutputCaptureMode.Selection, 10000)]
    [InlineData(OutputCaptureMode.LastNLines, 10000)]
    [InlineData(OutputCaptureMode.Manual, 50000)]
    public void GetDefaultMaxCharacters_ReturnsExpectedLimit(OutputCaptureMode mode, int expected)
    {
        // Act & Assert
        Assert.Equal(expected, mode.GetDefaultMaxCharacters());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetEstimatedMaxTokens Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(OutputCaptureMode.FullBuffer, 12500)]
    [InlineData(OutputCaptureMode.LastCommand, 5000)]
    [InlineData(OutputCaptureMode.Selection, 2500)]
    public void GetEstimatedMaxTokens_CalculatesFromCharacters(OutputCaptureMode mode, int expected)
    {
        // Act & Assert
        Assert.Equal(expected, mode.GetEstimatedMaxTokens());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RequiresUserInteraction Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(OutputCaptureMode.FullBuffer, false)]
    [InlineData(OutputCaptureMode.LastCommand, false)]
    [InlineData(OutputCaptureMode.Selection, true)]
    [InlineData(OutputCaptureMode.LastNLines, false)]
    [InlineData(OutputCaptureMode.Manual, true)]
    public void RequiresUserInteraction_IdentifiesInteractiveModes(OutputCaptureMode mode, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, mode.RequiresUserInteraction());
    }
}
