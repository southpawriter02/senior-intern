// ============================================================================
// File: TerminalSessionHistoryTests.cs
// Path: tests/AIntern.Core.Tests/Models/Terminal/TerminalSessionHistoryTests.cs
// Description: Unit tests for the TerminalSessionHistory model, testing session
//              grouping, computed properties, and factory methods.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSessionHistory"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class TerminalSessionHistoryTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Property Default Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Property Default Tests

    [Fact]
    public void SessionId_DefaultsToEmptyString()
    {
        // Arrange & Act
        var session = new TerminalSessionHistory();

        // Assert
        Assert.Equal(string.Empty, session.SessionId);
    }

    [Fact]
    public void StartedAt_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var session = new TerminalSessionHistory();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(session.StartedAt >= before);
        Assert.True(session.StartedAt <= after);
    }

    [Fact]
    public void EndedAt_DefaultsToNull()
    {
        // Arrange & Act
        var session = new TerminalSessionHistory();

        // Assert
        Assert.Null(session.EndedAt);
    }

    [Fact]
    public void Commands_DefaultsToEmptyList()
    {
        // Arrange & Act
        var session = new TerminalSessionHistory();

        // Assert
        Assert.NotNull(session.Commands);
        Assert.Empty(session.Commands);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // CommandCount Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region CommandCount Tests

    [Fact]
    public void CommandCount_EmptyList_ReturnsZero()
    {
        // Arrange
        var session = new TerminalSessionHistory();

        // Act & Assert
        Assert.Equal(0, session.CommandCount);
    }

    [Fact]
    public void CommandCount_WithCommands_ReturnsCount()
    {
        // Arrange
        var session = new TerminalSessionHistory
        {
            Commands = new List<TerminalHistoryEntry>
            {
                TerminalHistoryEntry.Create("cmd1"),
                TerminalHistoryEntry.Create("cmd2"),
                TerminalHistoryEntry.Create("cmd3")
            }
        };

        // Act & Assert
        Assert.Equal(3, session.CommandCount);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // IsActive Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region IsActive Tests

    [Fact]
    public void IsActive_EndedAtNull_ReturnsTrue()
    {
        // Arrange
        var session = new TerminalSessionHistory { EndedAt = null };

        // Act & Assert
        Assert.True(session.IsActive);
    }

    [Fact]
    public void IsActive_EndedAtSet_ReturnsFalse()
    {
        // Arrange
        var session = new TerminalSessionHistory { EndedAt = DateTime.UtcNow };

        // Act & Assert
        Assert.False(session.IsActive);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // Duration Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Duration Tests

    [Fact]
    public void Duration_ActiveSession_CalculatesFromNow()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        var session = new TerminalSessionHistory
        {
            StartedAt = startTime,
            EndedAt = null
        };

        // Act
        var duration = session.Duration;

        // Assert - Should be approximately 10 minutes
        Assert.True(duration.TotalMinutes >= 9.5);
        Assert.True(duration.TotalMinutes <= 10.5);
    }

    [Fact]
    public void Duration_EndedSession_CalculatesExactDuration()
    {
        // Arrange
        var startTime = new DateTime(2026, 1, 19, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2026, 1, 19, 10, 30, 0, DateTimeKind.Utc);
        var session = new TerminalSessionHistory
        {
            StartedAt = startTime,
            EndedAt = endTime
        };

        // Act
        var duration = session.Duration;

        // Assert
        Assert.Equal(30, duration.TotalMinutes);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // DurationDisplay Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region DurationDisplay Tests

    [Fact]
    public void DurationDisplay_LessThanMinute_FormatsAsSeconds()
    {
        // Arrange
        var session = new TerminalSessionHistory
        {
            StartedAt = DateTime.UtcNow.AddSeconds(-45),
            EndedAt = DateTime.UtcNow
        };

        // Act
        var result = session.DurationDisplay;

        // Assert
        Assert.EndsWith("s", result);
    }

    [Fact]
    public void DurationDisplay_LessThanHour_FormatsAsMinutes()
    {
        // Arrange
        var session = new TerminalSessionHistory
        {
            StartedAt = DateTime.UtcNow.AddMinutes(-15),
            EndedAt = DateTime.UtcNow
        };

        // Act
        var result = session.DurationDisplay;

        // Assert
        Assert.Equal("15m", result);
    }

    [Fact]
    public void DurationDisplay_OverAnHour_FormatsAsHours()
    {
        // Arrange
        var session = new TerminalSessionHistory
        {
            StartedAt = DateTime.UtcNow.AddHours(-2.5),
            EndedAt = DateTime.UtcNow
        };

        // Act
        var result = session.DurationDisplay;

        // Assert
        Assert.Equal("2.5h", result);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // SuccessfulCommandCount Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region SuccessfulCommandCount Tests

    [Fact]
    public void SuccessfulCommandCount_CountsExitCodeZero()
    {
        // Arrange
        var session = new TerminalSessionHistory
        {
            Commands = new List<TerminalHistoryEntry>
            {
                new() { Command = "cmd1", ExitCode = 0 },
                new() { Command = "cmd2", ExitCode = 1 },
                new() { Command = "cmd3", ExitCode = 0 },
                new() { Command = "cmd4", ExitCode = null }
            }
        };

        // Act & Assert
        Assert.Equal(2, session.SuccessfulCommandCount);
    }

    [Fact]
    public void SuccessfulCommandCount_EmptyList_ReturnsZero()
    {
        // Arrange
        var session = new TerminalSessionHistory();

        // Act & Assert
        Assert.Equal(0, session.SuccessfulCommandCount);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // FailedCommandCount Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region FailedCommandCount Tests

    [Fact]
    public void FailedCommandCount_CountsNonZeroExitCodes()
    {
        // Arrange
        var session = new TerminalSessionHistory
        {
            Commands = new List<TerminalHistoryEntry>
            {
                new() { Command = "cmd1", ExitCode = 0 },
                new() { Command = "cmd2", ExitCode = 1 },
                new() { Command = "cmd3", ExitCode = 127 },
                new() { Command = "cmd4", ExitCode = null }
            }
        };

        // Act & Assert
        Assert.Equal(2, session.FailedCommandCount);
    }

    [Fact]
    public void FailedCommandCount_NullExitCodes_NotCounted()
    {
        // Arrange
        var session = new TerminalSessionHistory
        {
            Commands = new List<TerminalHistoryEntry>
            {
                new() { Command = "cmd1", ExitCode = null },
                new() { Command = "cmd2", ExitCode = null }
            }
        };

        // Act & Assert
        Assert.Equal(0, session.FailedCommandCount);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Method Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Factory Method Tests

    [Fact]
    public void Create_SetsSessionId()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var session = TerminalSessionHistory.Create(sessionId);

        // Assert
        Assert.Equal(sessionId, session.SessionId);
    }

    [Fact]
    public void Create_SetsOptionalProperties()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var profileId = "bash-profile";
        var profileName = "Bash";

        // Act
        var session = TerminalSessionHistory.Create(
            sessionId,
            profileId: profileId,
            profileName: profileName);

        // Assert
        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal(profileId, session.ProfileId);
        Assert.Equal(profileName, session.ProfileName);
    }

    [Fact]
    public void Empty_CreatesDefaultInstance()
    {
        // Arrange & Act
        var session = TerminalSessionHistory.Empty;

        // Assert
        Assert.Equal(string.Empty, session.SessionId);
        Assert.Empty(session.Commands);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // StartedAtDisplay Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region StartedAtDisplay Tests

    [Fact]
    public void StartedAtDisplay_FormatsCorrectly()
    {
        // Arrange
        var utcTime = new DateTime(2026, 1, 19, 14, 30, 0, DateTimeKind.Utc);
        var session = new TerminalSessionHistory { StartedAt = utcTime };

        // Act
        var result = session.StartedAtDisplay;

        // Assert - Should contain the month, day, and year
        Assert.Contains("2026", result);
        Assert.Contains("Jan", result);
        Assert.Contains("19", result);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region ToString Tests

    [Fact]
    public void ToString_IncludesSessionIdAndCount()
    {
        // Arrange
        var session = new TerminalSessionHistory
        {
            SessionId = "test-session-123",
            Commands = new List<TerminalHistoryEntry>
            {
                TerminalHistoryEntry.Create("cmd1"),
                TerminalHistoryEntry.Create("cmd2")
            }
        };

        // Act
        var result = session.ToString();

        // Assert
        Assert.Contains("test-session-123", result);
        Assert.Contains("2 commands", result);
    }

    #endregion
}
