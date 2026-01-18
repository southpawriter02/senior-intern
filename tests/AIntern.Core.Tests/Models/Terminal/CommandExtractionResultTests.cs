using Xunit;
using AIntern.Core.Models.Terminal;
using AIntern.Core.Interfaces;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="CommandExtractionResult"/>.
/// </summary>
/// <remarks>Added in v0.5.4a.</remarks>
public sealed class CommandExtractionResultTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Empty Factory Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Empty_HasNoCommands()
    {
        // Act
        var result = CommandExtractionResult.Empty;

        // Assert
        Assert.False(result.HasCommands);
        Assert.Equal(0, result.CommandCount);
        Assert.Empty(result.Commands);
        Assert.Empty(result.Warnings);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Single Factory Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Single_ContainsOneCommand()
    {
        // Arrange
        var block = new CommandBlock { Command = "npm test" };

        // Act
        var result = CommandExtractionResult.Single(block);

        // Assert
        Assert.True(result.HasCommands);
        Assert.Equal(1, result.CommandCount);
        Assert.Same(block, result.Commands[0]);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // From Factory Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void From_CreatesResultWithCommandsAndWarnings()
    {
        // Arrange
        var commands = new[]
        {
            new CommandBlock { Command = "cmd1" },
            new CommandBlock { Command = "cmd2" }
        };
        var warnings = new[] { "Warning 1", "Warning 2" };

        // Act
        var result = CommandExtractionResult.From(commands, warnings);

        // Assert
        Assert.Equal(2, result.CommandCount);
        Assert.Equal(2, result.Warnings.Count);
        Assert.True(result.HasWarnings);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DangerousCommandCount Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DangerousCommandCount_CountsDangerousCommands()
    {
        // Arrange
        var result = CommandExtractionResult.From(new[]
        {
            new CommandBlock { Command = "safe", IsPotentiallyDangerous = false },
            new CommandBlock { Command = "rm -rf /", IsPotentiallyDangerous = true },
            new CommandBlock { Command = "harmless", IsPotentiallyDangerous = false },
            new CommandBlock { Command = "FORMAT C:", IsPotentiallyDangerous = true }
        });

        // Act & Assert
        Assert.Equal(2, result.DangerousCommandCount);
        Assert.True(result.HasDangerousCommands);
    }

    [Fact]
    public void HasDangerousCommands_ReturnsFalse_WhenNoneDangerous()
    {
        // Arrange
        var result = CommandExtractionResult.From(new[]
        {
            new CommandBlock { Command = "safe1", IsPotentiallyDangerous = false },
            new CommandBlock { Command = "safe2", IsPotentiallyDangerous = false }
        });

        // Act & Assert
        Assert.Equal(0, result.DangerousCommandCount);
        Assert.False(result.HasDangerousCommands);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DetectedShellTypes Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectedShellTypes_ReturnsDistinctTypes()
    {
        // Arrange
        var result = CommandExtractionResult.From(new[]
        {
            new CommandBlock { DetectedShellType = ShellType.Bash },
            new CommandBlock { DetectedShellType = ShellType.Bash },
            new CommandBlock { DetectedShellType = ShellType.PowerShell },
            new CommandBlock { DetectedShellType = null }
        });

        // Act
        var shellTypes = result.DetectedShellTypes.ToList();

        // Assert
        Assert.Equal(2, shellTypes.Count);
        Assert.Contains(ShellType.Bash, shellTypes);
        Assert.Contains(ShellType.PowerShell, shellTypes);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MultiLineCommandCount Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MultiLineCommandCount_CountsMultiLineCommands()
    {
        // Arrange
        var result = CommandExtractionResult.From(new[]
        {
            new CommandBlock { Command = "single" },
            new CommandBlock { Command = "line1\nline2" },
            new CommandBlock { Command = "a\nb\nc" }
        });

        // Act & Assert
        Assert.Equal(2, result.MultiLineCommandCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AverageConfidence Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void AverageConfidence_CalculatesCorrectly()
    {
        // Arrange
        var result = CommandExtractionResult.From(new[]
        {
            new CommandBlock { ConfidenceScore = 0.8f },
            new CommandBlock { ConfidenceScore = 0.6f },
            new CommandBlock { ConfidenceScore = 1.0f }
        });

        // Act
        var avg = result.AverageConfidence;

        // Assert
        Assert.Equal(0.8f, avg, 0.001f);
    }

    [Fact]
    public void AverageConfidence_ReturnsZero_WhenEmpty()
    {
        // Act & Assert
        Assert.Equal(0f, CommandExtractionResult.Empty.AverageConfidence);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Filter Method Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetCommandsByShellType_FiltersCorrectly()
    {
        // Arrange
        var result = CommandExtractionResult.From(new[]
        {
            new CommandBlock { DetectedShellType = ShellType.Bash },
            new CommandBlock { DetectedShellType = ShellType.PowerShell },
            new CommandBlock { DetectedShellType = ShellType.Bash }
        });

        // Act
        var bashCommands = result.GetCommandsByShellType(ShellType.Bash).ToList();

        // Assert
        Assert.Equal(2, bashCommands.Count);
    }

    [Fact]
    public void GetHighConfidenceCommands_FiltersAboveThreshold()
    {
        // Arrange
        var result = CommandExtractionResult.From(new[]
        {
            new CommandBlock { ConfidenceScore = 0.95f },
            new CommandBlock { ConfidenceScore = 0.5f },
            new CommandBlock { ConfidenceScore = 0.85f }
        });

        // Act
        var highConfidence = result.GetHighConfidenceCommands(0.8f).ToList();

        // Assert
        Assert.Equal(2, highConfidence.Count);
    }

    [Fact]
    public void GetSafeCommands_ExcludesDangerous()
    {
        // Arrange
        var result = CommandExtractionResult.From(new[]
        {
            new CommandBlock { IsPotentiallyDangerous = false },
            new CommandBlock { IsPotentiallyDangerous = true },
            new CommandBlock { IsPotentiallyDangerous = false }
        });

        // Act
        var safeCommands = result.GetSafeCommands().ToList();

        // Assert
        Assert.Equal(2, safeCommands.Count);
        Assert.All(safeCommands, c => Assert.False(c.IsPotentiallyDangerous));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToString_DescribesResult()
    {
        // Arrange
        var result = CommandExtractionResult.From(
            new[]
            {
                new CommandBlock { IsPotentiallyDangerous = true },
                new CommandBlock()
            },
            new[] { "Warning" }
        );

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("2 commands", str);
        Assert.Contains("1 dangerous", str);
        Assert.Contains("1 warnings", str);
    }

    [Fact]
    public void ToString_IndicatesEmpty()
    {
        // Act
        var str = CommandExtractionResult.Empty.ToString();

        // Assert
        Assert.Contains("No commands found", str);
    }
}
