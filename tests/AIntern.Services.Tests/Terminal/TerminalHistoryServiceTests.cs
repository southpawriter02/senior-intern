// ============================================================================
// File: TerminalHistoryServiceTests.cs
// Path: tests/AIntern.Services.Tests/Terminal/TerminalHistoryServiceTests.cs
// Description: Unit tests for the TerminalHistoryService, testing command
//              persistence, retrieval, search, and export functionality.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Data;
using AIntern.Services.Terminal;

namespace AIntern.Services.Tests.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalHistoryService"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests use an in-memory SQLite database to verify the service's
/// interaction with Entity Framework Core. Each test creates a fresh
/// database instance to ensure isolation.
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public sealed class TerminalHistoryServiceTests : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════
    // Test Fixtures
    // ═══════════════════════════════════════════════════════════════════════

    private readonly Mock<ILogger<TerminalHistoryService>> _loggerMock;
    private readonly IDbContextFactory<AInternDbContext> _dbContextFactory;
    private readonly TerminalHistoryService _service;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    /// <summary>
    /// Initializes test fixtures with in-memory SQLite database.
    /// </summary>
    public TerminalHistoryServiceTests()
    {
        _loggerMock = new Mock<ILogger<TerminalHistoryService>>();

        // Create and keep connection open for the lifetime of the test
        // This ensures the in-memory database persists across multiple contexts
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Use the shared connection for all contexts
        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Ensure database is created once
        using (var setupContext = new AInternDbContext(options))
        {
            setupContext.Database.EnsureCreated();
        }

        // Create factory that returns contexts using the shared connection
        var factoryMock = new Mock<IDbContextFactory<AInternDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AInternDbContext(options));
        factoryMock.Setup(f => f.CreateDbContext())
            .Returns(() => new AInternDbContext(options));

        _dbContextFactory = factoryMock.Object;
        _service = new TerminalHistoryService(_dbContextFactory, _loggerMock.Object);
    }

    /// <summary>
    /// Disposes the service under test and closes the database connection.
    /// </summary>
    public void Dispose()
    {
        _service.Dispose();
        _connection.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDbContextFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new TerminalHistoryService(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new TerminalHistoryService(_dbContextFactory, null!));
    }

    [Fact]
    public void Constructor_ValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        using var service = new TerminalHistoryService(_dbContextFactory, _loggerMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // AddCommandAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region AddCommandAsync Tests

    [Fact]
    public async Task AddCommandAsync_ValidEntry_PersistsToDatabase()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var entry = TerminalHistoryEntry.Create("git status");

        // Act
        await _service.AddCommandAsync(sessionId, entry);

        // Assert
        var results = await _service.GetRecentCommandsAsync(10);
        Assert.Single(results);
        Assert.Equal("git status", results[0].Command);
    }

    [Fact]
    public async Task AddCommandAsync_EmptyCommand_Skipped()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var entry = TerminalHistoryEntry.Create("");

        // Act
        await _service.AddCommandAsync(sessionId, entry);

        // Assert
        var results = await _service.GetRecentCommandsAsync(10);
        Assert.Empty(results);
    }

    [Fact]
    public async Task AddCommandAsync_WhitespaceOnlyCommand_Skipped()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var entry = TerminalHistoryEntry.Create("   ");

        // Act
        await _service.AddCommandAsync(sessionId, entry);

        // Assert
        var results = await _service.GetRecentCommandsAsync(10);
        Assert.Empty(results);
    }

    [Fact]
    public async Task AddCommandAsync_RaisesHistoryChangedEvent()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var entry = TerminalHistoryEntry.Create("ls -la");
        var eventRaised = false;
        HistoryChangedEventArgs? eventArgs = null;

        _service.HistoryChanged += (s, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        await _service.AddCommandAsync(sessionId, entry);

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(HistoryChangeType.Added, eventArgs.ChangeType);
        Assert.Equal(entry.Command, eventArgs.Entry?.Command);
    }

    [Fact]
    public async Task AddCommandAsync_MultipleEntries_AllPersisted()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var entries = new[]
        {
            TerminalHistoryEntry.Create("git status"),
            TerminalHistoryEntry.Create("git add ."),
            TerminalHistoryEntry.Create("git commit -m 'test'")
        };

        // Act
        foreach (var entry in entries)
        {
            await _service.AddCommandAsync(sessionId, entry);
        }

        // Assert
        var results = await _service.GetRecentCommandsAsync(10);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task AddCommandAsync_NullSessionId_ThrowsArgumentNullException()
    {
        // Arrange
        var entry = TerminalHistoryEntry.Create("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.AddCommandAsync(null!, entry));
    }

    [Fact]
    public async Task AddCommandAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.AddCommandAsync(sessionId, null!));
    }

    [Fact]
    public async Task AddCommandAsync_PreservesAllMetadata()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var entry = TerminalHistoryEntry.Create(
            command: "npm install",
            workingDirectory: "/home/user/project",
            exitCode: 0,
            duration: TimeSpan.FromSeconds(5.5),
            profileId: "bash-default");

        // Act
        await _service.AddCommandAsync(sessionId, entry);

        // Assert
        var results = await _service.GetRecentCommandsAsync(1);
        Assert.Single(results);
        var result = results[0];
        Assert.Equal("npm install", result.Command);
        Assert.Equal("/home/user/project", result.WorkingDirectory);
        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(result.Duration);
        Assert.Equal(5.5, result.Duration.Value.TotalSeconds, precision: 1);
        Assert.Equal("bash-default", result.ProfileId);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetRecentCommandsAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region GetRecentCommandsAsync Tests

    [Fact]
    public async Task GetRecentCommandsAsync_ReturnsNewestFirst()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var entries = new[]
        {
            new TerminalHistoryEntry { Command = "first", ExecutedAt = DateTime.UtcNow.AddMinutes(-3) },
            new TerminalHistoryEntry { Command = "second", ExecutedAt = DateTime.UtcNow.AddMinutes(-2) },
            new TerminalHistoryEntry { Command = "third", ExecutedAt = DateTime.UtcNow.AddMinutes(-1) }
        };

        foreach (var entry in entries)
        {
            await _service.AddCommandAsync(sessionId, entry);
        }

        // Act
        var results = await _service.GetRecentCommandsAsync(10);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("third", results[0].Command);
        Assert.Equal("second", results[1].Command);
        Assert.Equal("first", results[2].Command);
    }

    [Fact]
    public async Task GetRecentCommandsAsync_RespectsCountLimit()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        for (int i = 0; i < 20; i++)
        {
            await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create($"command {i}"));
        }

        // Act
        var results = await _service.GetRecentCommandsAsync(5);

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task GetRecentCommandsAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var results = await _service.GetRecentCommandsAsync(10);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetSessionHistoryAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region GetSessionHistoryAsync Tests

    [Fact]
    public async Task GetSessionHistoryAsync_ReturnsChronologically()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var entries = new[]
        {
            new TerminalHistoryEntry { Command = "first", ExecutedAt = DateTime.UtcNow.AddMinutes(-3) },
            new TerminalHistoryEntry { Command = "second", ExecutedAt = DateTime.UtcNow.AddMinutes(-2) },
            new TerminalHistoryEntry { Command = "third", ExecutedAt = DateTime.UtcNow.AddMinutes(-1) }
        };

        foreach (var entry in entries)
        {
            await _service.AddCommandAsync(sessionId, entry);
        }

        // Act
        var results = await _service.GetSessionHistoryAsync(sessionId);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("first", results[0].Command);  // Oldest first
        Assert.Equal("second", results[1].Command);
        Assert.Equal("third", results[2].Command);
    }

    [Fact]
    public async Task GetSessionHistoryAsync_OnlyReturnsMatchingSession()
    {
        // Arrange
        var session1 = Guid.NewGuid().ToString();
        var session2 = Guid.NewGuid().ToString();

        await _service.AddCommandAsync(session1, TerminalHistoryEntry.Create("session1 command"));
        await _service.AddCommandAsync(session2, TerminalHistoryEntry.Create("session2 command"));

        // Act
        var results = await _service.GetSessionHistoryAsync(session1);

        // Assert
        Assert.Single(results);
        Assert.Equal("session1 command", results[0].Command);
    }

    [Fact]
    public async Task GetSessionHistoryAsync_UnknownSession_ReturnsEmpty()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("test"));

        // Act
        var results = await _service.GetSessionHistoryAsync("unknown-session-id");

        // Assert
        Assert.Empty(results);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // SearchHistoryAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region SearchHistoryAsync Tests

    [Fact]
    public async Task SearchHistoryAsync_FindsPartialMatches()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git status"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git pull"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("npm install"));

        // Act
        var results = await _service.SearchHistoryAsync("git");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("git", r.Command));
    }

    [Fact]
    public async Task SearchHistoryAsync_EmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("test"));

        // Act
        var results = await _service.SearchHistoryAsync("");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchHistoryAsync_WhitespaceQuery_ReturnsEmpty()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("test"));

        // Act
        var results = await _service.SearchHistoryAsync("   ");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchHistoryAsync_CaseInsensitive()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("Git Status"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("GIT PULL"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git push"));

        // Act
        var results = await _service.SearchHistoryAsync("git");

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task SearchHistoryAsync_RespectsMaxResults()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        for (int i = 0; i < 20; i++)
        {
            await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create($"test command {i}"));
        }

        // Act
        var results = await _service.SearchHistoryAsync("test", maxResults: 5);

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task SearchHistoryAsync_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git status"));

        // Act
        var results = await _service.SearchHistoryAsync("notfound");

        // Assert
        Assert.Empty(results);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // ClearHistoryAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region ClearHistoryAsync Tests

    [Fact]
    public async Task ClearAllHistoryAsync_DeletesAllEntries()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("cmd1"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("cmd2"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("cmd3"));

        // Act
        await _service.ClearAllHistoryAsync();

        // Assert
        var results = await _service.GetRecentCommandsAsync(100);
        Assert.Empty(results);
    }

    [Fact]
    public async Task ClearAllHistoryAsync_RaisesHistoryChangedEvent()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("test"));

        var eventRaised = false;
        HistoryChangedEventArgs? eventArgs = null;

        _service.HistoryChanged += (s, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        await _service.ClearAllHistoryAsync();

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(HistoryChangeType.Cleared, eventArgs.ChangeType);
        Assert.Null(eventArgs.Entry);
    }

    [Fact]
    public async Task ClearHistoryOlderThanAsync_OnlyDeletesOldEntries()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        await _service.AddCommandAsync(sessionId, new TerminalHistoryEntry
        {
            Command = "old",
            ExecutedAt = now.AddDays(-10)
        });
        await _service.AddCommandAsync(sessionId, new TerminalHistoryEntry
        {
            Command = "recent",
            ExecutedAt = now
        });

        // Act
        await _service.ClearHistoryOlderThanAsync(now.AddDays(-5));

        // Assert
        var results = await _service.GetRecentCommandsAsync(100);
        Assert.Single(results);
        Assert.Equal("recent", results[0].Command);
    }

    [Fact]
    public async Task ClearHistoryOlderThanAsync_PreservesNewEntries()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        await _service.AddCommandAsync(sessionId, new TerminalHistoryEntry
        {
            Command = "new1",
            ExecutedAt = now.AddMinutes(-30)
        });
        await _service.AddCommandAsync(sessionId, new TerminalHistoryEntry
        {
            Command = "new2",
            ExecutedAt = now.AddMinutes(-15)
        });

        // Act
        await _service.ClearHistoryOlderThanAsync(now.AddDays(-1));

        // Assert
        var results = await _service.GetRecentCommandsAsync(100);
        Assert.Equal(2, results.Count);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetUniqueCommandsAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region GetUniqueCommandsAsync Tests

    [Fact]
    public async Task GetUniqueCommandsAsync_ReturnsDeduplicated()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git status"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git pull"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git status")); // duplicate

        // Act
        var results = await _service.GetUniqueCommandsAsync(100);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains("git status", results);
        Assert.Contains("git pull", results);
    }

    [Fact]
    public async Task GetUniqueCommandsAsync_RespectsCount()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        for (int i = 0; i < 20; i++)
        {
            await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create($"unique command {i}"));
        }

        // Act
        var results = await _service.GetUniqueCommandsAsync(5);

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task GetUniqueCommandsAsync_MostRecentFirst()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, new TerminalHistoryEntry
        {
            Command = "old command",
            ExecutedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await _service.AddCommandAsync(sessionId, new TerminalHistoryEntry
        {
            Command = "new command",
            ExecutedAt = DateTime.UtcNow
        });

        // Act
        var results = await _service.GetUniqueCommandsAsync(10);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("new command", results[0]); // Most recent first
        Assert.Equal("old command", results[1]);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetTotalCommandCountAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region GetTotalCommandCountAsync Tests

    [Fact]
    public async Task GetTotalCommandCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("cmd1"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("cmd2"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("cmd3"));

        // Act
        var count = await _service.GetTotalCommandCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetTotalCommandCountAsync_EmptyDatabase_ReturnsZero()
    {
        // Act
        var count = await _service.GetTotalCommandCountAsync();

        // Assert
        Assert.Equal(0, count);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // Export Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Export Tests

    [Fact]
    public async Task ExportHistoryAsync_Json_ValidFormat()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create(
            "git status",
            workingDirectory: "/home/user",
            exitCode: 0));

        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            await _service.ExportHistoryAsync(tempFile, HistoryExportFormat.Json);

            // Assert
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("\"command\"", content);
            Assert.Contains("git status", content);
            Assert.Contains("\"exitCode\"", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportHistoryAsync_Csv_ValidFormat()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create(
            "git status",
            workingDirectory: "/home/user",
            exitCode: 0));

        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            await _service.ExportHistoryAsync(tempFile, HistoryExportFormat.Csv);

            // Assert
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("Command,ExecutedAt,WorkingDirectory,ExitCode,DurationMs", content);
            Assert.Contains("git status", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportHistoryAsync_Text_OneCommandPerLine()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git status"));
        await _service.AddCommandAsync(sessionId, TerminalHistoryEntry.Create("git pull"));

        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            await _service.ExportHistoryAsync(tempFile, HistoryExportFormat.Text);

            // Assert
            var lines = await File.ReadAllLinesAsync(tempFile);
            Assert.Equal(2, lines.Length);
            Assert.Contains("git status", lines);
            Assert.Contains("git pull", lines);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportHistoryAsync_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        // ArgumentException.ThrowIfNullOrEmpty throws ArgumentNullException for null
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.ExportHistoryAsync(null!, HistoryExportFormat.Json));
    }

    [Fact]
    public async Task ExportHistoryAsync_EmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ExportHistoryAsync("", HistoryExportFormat.Json));
    }

    [Fact]
    public async Task ExportHistoryAsync_InvalidFormat_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _service.ExportHistoryAsync(tempFile, (HistoryExportFormat)999));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose Tests
    // ═══════════════════════════════════════════════════════════════════════

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        using var service = new TerminalHistoryService(_dbContextFactory, _loggerMock.Object);

        // Act & Assert - Should not throw
        service.Dispose();
        service.Dispose();
        service.Dispose();
    }

    #endregion
}
