// ============================================================================
// File: TerminalHistoryEntryTests.cs
// Path: tests/AIntern.Core.Tests/Models/Terminal/TerminalHistoryEntryTests.cs
// Description: Unit tests for the TerminalHistoryEntry model, testing property
//              defaults, computed properties, factory methods, and formatting.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalHistoryEntry"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class TerminalHistoryEntryTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Property Default Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Property Default Tests

    [Fact]
    public void Id_DefaultsToNewGuid()
    {
        // Arrange & Act
        var entry = new TerminalHistoryEntry();

        // Assert
        Assert.False(string.IsNullOrEmpty(entry.Id));
        Assert.True(Guid.TryParse(entry.Id, out _));
    }

    [Fact]
    public void Id_IsUniquePerInstance()
    {
        // Arrange & Act
        var entry1 = new TerminalHistoryEntry();
        var entry2 = new TerminalHistoryEntry();

        // Assert
        Assert.NotEqual(entry1.Id, entry2.Id);
    }

    [Fact]
    public void ExecutedAt_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var entry = new TerminalHistoryEntry();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(entry.ExecutedAt >= before);
        Assert.True(entry.ExecutedAt <= after);
    }

    [Fact]
    public void Command_DefaultsToEmptyString()
    {
        // Arrange & Act
        var entry = new TerminalHistoryEntry();

        // Assert
        Assert.Equal(string.Empty, entry.Command);
    }

    [Fact]
    public void OptionalProperties_DefaultToNull()
    {
        // Arrange & Act
        var entry = new TerminalHistoryEntry();

        // Assert
        Assert.Null(entry.WorkingDirectory);
        Assert.Null(entry.ExitCode);
        Assert.Null(entry.Duration);
        Assert.Null(entry.ProfileId);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // IsSuccess Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region IsSuccess Tests

    [Fact]
    public void IsSuccess_TrueWhenExitCodeZero()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { ExitCode = 0 };

        // Act & Assert
        Assert.True(entry.IsSuccess);
    }

    [Fact]
    public void IsSuccess_FalseWhenExitCodeNonZero()
    {
        // Arrange
        var entry1 = new TerminalHistoryEntry { ExitCode = 1 };
        var entry2 = new TerminalHistoryEntry { ExitCode = -1 };
        var entry3 = new TerminalHistoryEntry { ExitCode = 127 };

        // Act & Assert
        Assert.False(entry1.IsSuccess);
        Assert.False(entry2.IsSuccess);
        Assert.False(entry3.IsSuccess);
    }

    [Fact]
    public void IsSuccess_FalseWhenExitCodeNull()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { ExitCode = null };

        // Act & Assert
        Assert.False(entry.IsSuccess);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // RelativeTime Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region RelativeTime Tests

    [Fact]
    public void RelativeTime_JustNow_ReturnsJustNow()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { ExecutedAt = DateTime.UtcNow };

        // Act
        var result = entry.RelativeTime;

        // Assert
        Assert.Equal("just now", result);
    }

    [Fact]
    public void RelativeTime_FewSecondsAgo_ReturnsJustNow()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { ExecutedAt = DateTime.UtcNow.AddSeconds(-30) };

        // Act
        var result = entry.RelativeTime;

        // Assert
        Assert.Equal("just now", result);
    }

    [Fact]
    public void RelativeTime_FewMinutesAgo_ReturnsMinutes()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { ExecutedAt = DateTime.UtcNow.AddMinutes(-5) };

        // Act
        var result = entry.RelativeTime;

        // Assert
        Assert.Equal("5m ago", result);
    }

    [Fact]
    public void RelativeTime_FewHoursAgo_ReturnsHours()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { ExecutedAt = DateTime.UtcNow.AddHours(-3) };

        // Act
        var result = entry.RelativeTime;

        // Assert
        Assert.Equal("3h ago", result);
    }

    [Fact]
    public void RelativeTime_FewDaysAgo_ReturnsDays()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { ExecutedAt = DateTime.UtcNow.AddDays(-5) };

        // Act
        var result = entry.RelativeTime;

        // Assert
        Assert.Equal("5d ago", result);
    }

    [Fact]
    public void RelativeTime_FewWeeksAgo_ReturnsWeeks()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { ExecutedAt = DateTime.UtcNow.AddDays(-14) };

        // Act
        var result = entry.RelativeTime;

        // Assert
        Assert.Equal("2w ago", result);
    }

    [Fact]
    public void RelativeTime_OverAMonthAgo_ReturnsDate()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-60);
        var entry = new TerminalHistoryEntry { ExecutedAt = oldDate };

        // Act
        var result = entry.RelativeTime;

        // Assert
        Assert.Contains(oldDate.Year.ToString(), result);
    }

    [Fact]
    public void RelativeTime_FutureDate_ReturnsJustNow()
    {
        // Arrange - This shouldn't happen but test defensive handling
        var entry = new TerminalHistoryEntry { ExecutedAt = DateTime.UtcNow.AddMinutes(5) };

        // Act
        var result = entry.RelativeTime;

        // Assert
        Assert.Equal("just now", result);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // DurationDisplay Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region DurationDisplay Tests

    [Fact]
    public void DurationDisplay_Null_ReturnsNull()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { Duration = null };

        // Act & Assert
        Assert.Null(entry.DurationDisplay);
    }

    [Fact]
    public void DurationDisplay_Milliseconds_FormatsCorrectly()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { Duration = TimeSpan.FromMilliseconds(150) };

        // Act
        var result = entry.DurationDisplay;

        // Assert
        Assert.Equal("150ms", result);
    }

    [Fact]
    public void DurationDisplay_Seconds_FormatsCorrectly()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { Duration = TimeSpan.FromSeconds(2.5) };

        // Act
        var result = entry.DurationDisplay;

        // Assert
        Assert.Equal("2.5s", result);
    }

    [Fact]
    public void DurationDisplay_Minutes_FormatsCorrectly()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { Duration = TimeSpan.FromMinutes(2.3) };

        // Act
        var result = entry.DurationDisplay;

        // Assert
        Assert.Equal("2.3m", result);
    }

    [Fact]
    public void DurationDisplay_ExactlyOneSecond_FormatsAsSeconds()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { Duration = TimeSpan.FromSeconds(1) };

        // Act
        var result = entry.DurationDisplay;

        // Assert
        Assert.Equal("1.0s", result);
    }

    [Fact]
    public void DurationDisplay_ExactlyOneMinute_FormatsAsMinutes()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { Duration = TimeSpan.FromMinutes(1) };

        // Act
        var result = entry.DurationDisplay;

        // Assert
        Assert.Equal("1.0m", result);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Method Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Factory Method Tests

    [Fact]
    public void Create_SetsAllProperties()
    {
        // Arrange
        var command = "git status";
        var workDir = "/home/user";
        var exitCode = 0;
        var duration = TimeSpan.FromSeconds(1.5);
        var profileId = "bash-default";

        // Act
        var entry = TerminalHistoryEntry.Create(
            command,
            workingDirectory: workDir,
            exitCode: exitCode,
            duration: duration,
            profileId: profileId);

        // Assert
        Assert.Equal(command, entry.Command);
        Assert.Equal(workDir, entry.WorkingDirectory);
        Assert.Equal(exitCode, entry.ExitCode);
        Assert.Equal(duration, entry.Duration);
        Assert.Equal(profileId, entry.ProfileId);
    }

    [Fact]
    public void Create_GeneratesNewId()
    {
        // Arrange & Act
        var entry1 = TerminalHistoryEntry.Create("cmd1");
        var entry2 = TerminalHistoryEntry.Create("cmd2");

        // Assert
        Assert.NotEqual(entry1.Id, entry2.Id);
    }

    [Fact]
    public void Create_CommandOnly_SetsDefaults()
    {
        // Arrange & Act
        var entry = TerminalHistoryEntry.Create("test command");

        // Assert
        Assert.Equal("test command", entry.Command);
        Assert.Null(entry.WorkingDirectory);
        Assert.Null(entry.ExitCode);
        Assert.Null(entry.Duration);
        Assert.Null(entry.ProfileId);
    }

    [Fact]
    public void Empty_CreatesDefaultInstance()
    {
        // Arrange & Act
        var entry = TerminalHistoryEntry.Empty;

        // Assert
        Assert.Equal(string.Empty, entry.Command);
        Assert.NotNull(entry.Id);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsCommand()
    {
        // Arrange
        var entry = TerminalHistoryEntry.Create("git status --short");

        // Act
        var result = entry.ToString();

        // Assert
        Assert.Equal("git status --short", result);
    }

    [Fact]
    public void ToString_EmptyCommand_ReturnsEmptyString()
    {
        // Arrange
        var entry = TerminalHistoryEntry.Empty;

        // Act
        var result = entry.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // Init Properties Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Init Properties Tests

    [Fact]
    public void InitProperties_CanBeSetAtCreation()
    {
        // Arrange & Act
        var customId = Guid.NewGuid().ToString();
        var entry = new TerminalHistoryEntry
        {
            Id = customId,
            Command = "custom command",
            ExecutedAt = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            WorkingDirectory = "/custom/path",
            ExitCode = 1,
            Duration = TimeSpan.FromSeconds(5),
            ProfileId = "custom-profile"
        };

        // Assert
        Assert.Equal(customId, entry.Id);
        Assert.Equal("custom command", entry.Command);
        Assert.Equal(new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc), entry.ExecutedAt);
        Assert.Equal("/custom/path", entry.WorkingDirectory);
        Assert.Equal(1, entry.ExitCode);
        Assert.Equal(TimeSpan.FromSeconds(5), entry.Duration);
        Assert.Equal("custom-profile", entry.ProfileId);
    }

    #endregion
}
