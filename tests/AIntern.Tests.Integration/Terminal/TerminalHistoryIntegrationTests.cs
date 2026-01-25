// ============================================================================
// File: TerminalHistoryIntegrationTests.cs
// Path: tests/AIntern.Tests.Integration/Terminal/TerminalHistoryIntegrationTests.cs
// Description: Integration tests for terminal history service.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Integration.Terminal;

using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;
using AIntern.Tests.Integration.Mocks;

/// <summary>
/// Integration tests for terminal history service.
/// Tests CRUD operations, search, and export with in-memory database.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
public sealed class TerminalHistoryIntegrationTests : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════
    // Test Fixtures
    // ═══════════════════════════════════════════════════════════════════════

    private readonly TestAppDbContextFactory _dbContextFactory;
    private readonly TerminalHistoryService _historyService;

    public TerminalHistoryIntegrationTests()
    {
        _dbContextFactory = new TestAppDbContextFactory();
        _historyService = new TerminalHistoryService(
            _dbContextFactory,
            NullLogger<TerminalHistoryService>.Instance);
    }

    public void Dispose()
    {
        _historyService.Dispose();
        _dbContextFactory.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Add Command Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddCommand_StoresInDatabase()
    {
        // Arrange
        var entry = new TerminalHistoryEntry
        {
            Command = "ls -la",
            WorkingDirectory = "/home/user"
        };

        // Act
        await _historyService.AddCommandAsync("session1", entry);

        // Assert
        var history = await _historyService.GetRecentCommandsAsync(10);
        Assert.Single(history);
        Assert.Equal("ls -la", history[0].Command);
        Assert.Equal("/home/user", history[0].WorkingDirectory);
    }

    [Fact]
    public async Task AddCommand_EmptyCommand_NotStored()
    {
        // Arrange
        var entry = new TerminalHistoryEntry { Command = "" };

        // Act
        await _historyService.AddCommandAsync("session1", entry);

        // Assert
        var history = await _historyService.GetRecentCommandsAsync(10);
        Assert.Empty(history);
    }

    [Fact]
    public async Task AddCommand_WithMetadata_StoresAll()
    {
        // Arrange
        var entry = new TerminalHistoryEntry
        {
            Command = "npm install",
            ExitCode = 0,
            Duration = TimeSpan.FromSeconds(5.5),
            ProfileId = "profile-123"
        };

        // Act
        await _historyService.AddCommandAsync("session1", entry);

        // Assert
        var history = await _historyService.GetRecentCommandsAsync(10);
        Assert.Equal(0, history[0].ExitCode);
        Assert.NotNull(history[0].Duration);
        Assert.Equal(5.5, history[0].Duration!.Value.TotalSeconds, 0.1);
        Assert.Equal("profile-123", history[0].ProfileId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Get Recent Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRecentCommands_ReturnsNewestFirst()
    {
        // Arrange
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry
        {
            Command = "first",
            ExecutedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry
        {
            Command = "second",
            ExecutedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry
        {
            Command = "third",
            ExecutedAt = DateTime.UtcNow
        });

        // Act
        var history = await _historyService.GetRecentCommandsAsync(10);

        // Assert
        Assert.Equal("third", history[0].Command);
        Assert.Equal("second", history[1].Command);
        Assert.Equal("first", history[2].Command);
    }

    [Fact]
    public async Task GetRecentCommands_RespectsCount()
    {
        // Arrange
        for (int i = 0; i < 50; i++)
        {
            await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry
            {
                Command = $"command {i}"
            });
        }

        // Act
        var history = await _historyService.GetRecentCommandsAsync(10);

        // Assert
        Assert.Equal(10, history.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Search Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchHistory_FindsMatchingCommands()
    {
        // Arrange
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry { Command = "git status" });
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry { Command = "git commit" });
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry { Command = "npm install" });

        // Act
        var results = await _historyService.SearchHistoryAsync("git");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("git", r.Command));
    }

    [Fact]
    public async Task SearchHistory_CaseInsensitive()
    {
        // Arrange
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry { Command = "Git Status" });
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry { Command = "GIT COMMIT" });

        // Act
        var results = await _historyService.SearchHistoryAsync("git");

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchHistory_EmptyQuery_ReturnsEmpty()
    {
        // Arrange
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry { Command = "test" });

        // Act
        var results = await _historyService.SearchHistoryAsync("");

        // Assert
        Assert.Empty(results);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Clear Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact(Skip = "ExecuteDelete() not supported by InMemory provider")]
    public async Task ClearAllHistory_DeletesEverything()
    {
        // Arrange
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry { Command = "cmd1" });
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry { Command = "cmd2" });

        // Act
        await _historyService.ClearAllHistoryAsync();

        // Assert
        var history = await _historyService.GetRecentCommandsAsync(10);
        Assert.Empty(history);
    }

    [Fact(Skip = "ExecuteDelete() not supported by InMemory provider")]
    public async Task ClearHistoryOlderThan_RemovesOldEntries()
    {
        // Arrange
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry
        {
            Command = "old command",
            ExecutedAt = DateTime.UtcNow.AddDays(-30)
        });
        await _historyService.AddCommandAsync("s1", new TerminalHistoryEntry
        {
            Command = "new command",
            ExecutedAt = DateTime.UtcNow
        });

        // Act
        await _historyService.ClearHistoryOlderThanAsync(DateTime.UtcNow.AddDays(-7));

        // Assert
        var history = await _historyService.GetRecentCommandsAsync(10);
        Assert.Single(history);
        Assert.Equal("new command", history[0].Command);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Session History Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSessionHistory_ReturnsOnlySessionCommands()
    {
        // Arrange
        await _historyService.AddCommandAsync("session1", new TerminalHistoryEntry { Command = "cmd1" });
        await _historyService.AddCommandAsync("session2", new TerminalHistoryEntry { Command = "cmd2" });
        await _historyService.AddCommandAsync("session1", new TerminalHistoryEntry { Command = "cmd3" });

        // Act
        var history = await _historyService.GetSessionHistoryAsync("session1");

        // Assert
        Assert.Equal(2, history.Count);
        Assert.Contains(history, h => h.Command == "cmd1");
        Assert.Contains(history, h => h.Command == "cmd3");
        Assert.DoesNotContain(history, h => h.Command == "cmd2");
    }
}
