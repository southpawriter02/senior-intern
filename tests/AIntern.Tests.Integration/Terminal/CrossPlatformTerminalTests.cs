// ============================================================================
// File: CrossPlatformTerminalTests.cs
// Path: tests/AIntern.Tests.Integration/Terminal/CrossPlatformTerminalTests.cs
// Description: Cross-platform integration tests for terminal functionality.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Integration.Terminal;

using System.Runtime.InteropServices;
using Xunit;
using AIntern.Core.Models.Terminal;

/// <summary>
/// Cross-platform integration tests for terminal functionality.
/// Uses conditional execution based on platform.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
public sealed class CrossPlatformTerminalTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Font Defaults Tests
    // ═══════════════════════════════════════════════════════════════════════

    [SkippableFact]
    public void DefaultFont_Windows_IsCascadiaOrConsolas()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        // Act
        var settings = new TerminalSettings();

        // Assert
        var fontFamily = settings.FontFamily;
        Assert.True(
            fontFamily.Contains("Cascadia") || fontFamily.Contains("Consolas"),
            $"Expected Windows default font to be Cascadia or Consolas, got: {fontFamily}");
    }

    [SkippableFact]
    public void DefaultFont_MacOS_IsSFMonoOrMenlo()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));

        // Act
        var settings = new TerminalSettings();

        // Assert
        var fontFamily = settings.FontFamily;
        Assert.True(
            fontFamily.Contains("SF Mono") ||
            fontFamily.Contains("Menlo") ||
            fontFamily.Contains("Monaco"),
            $"Expected macOS default font to be SF Mono, Menlo, or Monaco, got: {fontFamily}");
    }

    [SkippableFact]
    public void DefaultFont_Linux_IsUbuntuMonoOrDejaVu()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));

        // Act
        var settings = new TerminalSettings();

        // Assert
        var fontFamily = settings.FontFamily;
        Assert.True(
            fontFamily.Contains("Ubuntu Mono") ||
            fontFamily.Contains("DejaVu") ||
            fontFamily.Contains("Liberation Mono"),
            $"Expected Linux default font to be Ubuntu Mono, DejaVu, or Liberation Mono, got: {fontFamily}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Path Handling Tests
    // ═══════════════════════════════════════════════════════════════════════

    [SkippableFact]
    public void PathSeparator_Windows_IsBackslash()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        // Assert
        Assert.Equal('\\', Path.DirectorySeparatorChar);
    }

    [SkippableFact]
    public void PathSeparator_Unix_IsForwardSlash()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux));

        // Assert
        Assert.Equal('/', Path.DirectorySeparatorChar);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Home Directory Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HomeDirectory_IsNotEmpty()
    {
        // Act
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Assert
        Assert.NotEmpty(home);
        Assert.True(Directory.Exists(home));
    }
}
