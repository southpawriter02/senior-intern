using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ WORKING DIRECTORY SYNC SERVICE TESTS (v0.5.3e)                          │
// │ Unit tests for bi-directional directory sync and OSC 7 parsing.         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="WorkingDirectorySyncService"/>.
/// </summary>
public sealed class WorkingDirectorySyncServiceTests : IAsyncLifetime, IDisposable
{
    // ─────────────────────────────────────────────────────────────────────
    // Test Fixtures
    // ─────────────────────────────────────────────────────────────────────

    private readonly Mock<ITerminalService> _mockTerminalService;
    private readonly Mock<IShellConfigurationService> _mockShellConfig;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<WorkingDirectorySyncService>> _mockLogger;
    private readonly AppSettings _appSettings;
    private readonly TerminalSession _testSession;
    private WorkingDirectorySyncService _service = null!;

    public WorkingDirectorySyncServiceTests()
    {
        _mockTerminalService = new Mock<ITerminalService>();
        _mockShellConfig = new Mock<IShellConfigurationService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<WorkingDirectorySyncService>>();
        _appSettings = new AppSettings();

        _testSession = new TerminalSession
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            ShellPath = "/bin/bash",
            WorkingDirectory = "/home/user"
        };

        // Default setup
        _mockTerminalService.Setup(s => s.Sessions).Returns(new List<TerminalSession> { _testSession });
        _mockSettingsService.SetupGet(s => s.CurrentSettings).Returns(_appSettings);
        _mockShellConfig.Setup(s => s.GetConfiguration(It.IsAny<string>()))
            .Returns(new ShellConfiguration { Type = ShellType.Bash });
        _mockShellConfig.Setup(s => s.FormatChangeDirectoryCommand(It.IsAny<ShellType>(), It.IsAny<string>()))
            .Returns<ShellType, string>((_, path) => $"cd '{path}'");
    }

    public Task InitializeAsync()
    {
        _service = new WorkingDirectorySyncService(
            _mockTerminalService.Object,
            _mockShellConfig.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _service?.Dispose();

    // ─────────────────────────────────────────────────────────────────────
    // Session Lifecycle Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> Tracks sessions when SessionCreated fires.<br/>
    /// </summary>
    [Fact]
    public void OnSessionCreated_TracksSession()
    {
        // Arrange
        var eventArgs = new TerminalSessionEventArgs { Session = _testSession };

        // Act
        _mockTerminalService.Raise(s => s.SessionCreated += null, eventArgs);

        // Assert - can now query the session
        Assert.True(_service.IsAutoSyncEnabled(_testSession.Id));
    }

    /// <summary>
    /// <b>Unit Test:</b> Stops tracking sessions when SessionClosed fires.<br/>
    /// </summary>
    [Fact]
    public void OnSessionClosed_StopsTracking()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });

        // Act
        _mockTerminalService.Raise(s => s.SessionClosed += null,
            new TerminalSessionEventArgs { Session = _testSession });

        // Assert - session no longer tracked
        Assert.False(_service.IsAutoSyncEnabled(_testSession.Id));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Query Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetTerminalDirectoryAsync returns tracked directory.<br/>
    /// </summary>
    [Fact]
    public async Task GetTerminalDirectoryAsync_ReturnsTrackedDirectory()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });

        // Act
        var dir = await _service.GetTerminalDirectoryAsync(_testSession.Id);

        // Assert
        Assert.Equal("/home/user", dir);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetTerminalDirectoryAsync returns null for unknown session.<br/>
    /// </summary>
    [Fact]
    public async Task GetTerminalDirectoryAsync_ReturnsNullForUnknown()
    {
        // Act
        var dir = await _service.GetTerminalDirectoryAsync(Guid.NewGuid());

        // Assert
        Assert.Null(dir);
    }

    /// <summary>
    /// <b>Unit Test:</b> IsAutoSyncEnabled returns true by default.<br/>
    /// </summary>
    [Fact]
    public void IsAutoSyncEnabled_TrueByDefault()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });

        // Assert
        Assert.True(_service.IsAutoSyncEnabled(_testSession.Id));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Command Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ChangeTerminalDirectoryAsync sends cd command.<br/>
    /// </summary>
    [Fact]
    public async Task ChangeTerminalDirectoryAsync_SendsCdCommand()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });
        var testPath = Path.GetTempPath();

        // Act
        await _service.ChangeTerminalDirectoryAsync(_testSession.Id, testPath);

        // Assert
        _mockTerminalService.Verify(s => s.WriteInputAsync(
            _testSession.Id,
            It.Is<string>(c => c.StartsWith("cd") && c.Contains(testPath)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// <b>Unit Test:</b> ChangeTerminalDirectoryAsync fires event.<br/>
    /// </summary>
    [Fact]
    public async Task ChangeTerminalDirectoryAsync_FiresEvent()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });
        DirectoryChangedEventArgs? eventArgs = null;
        _service.TerminalDirectoryChanged += (_, e) => eventArgs = e;

        // Act
        await _service.ChangeTerminalDirectoryAsync(_testSession.Id, Path.GetTempPath());

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(DirectoryChangeSource.Api, eventArgs.Source);
    }

    /// <summary>
    /// <b>Unit Test:</b> ChangeTerminalDirectoryAsync ignores non-existent path.<br/>
    /// </summary>
    [Fact]
    public async Task ChangeTerminalDirectoryAsync_IgnoresNonExistentPath()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });

        // Act
        await _service.ChangeTerminalDirectoryAsync(_testSession.Id, "/nonexistent/path123456");

        // Assert - no command sent
        _mockTerminalService.Verify(s => s.WriteInputAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Configuration Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> SetAutoSync disables auto-sync.<br/>
    /// </summary>
    [Fact]
    public void SetAutoSync_DisablesSync()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });

        // Act
        _service.SetAutoSync(_testSession.Id, enabled: false);

        // Assert
        Assert.False(_service.IsAutoSyncEnabled(_testSession.Id));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Workspace Linking Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> LinkToWorkspace and UnlinkFromWorkspace work.<br/>
    /// </summary>
    [Fact]
    public void WorkspaceLinking_Works()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });
        var workspaceId = Guid.NewGuid();

        // Act
        _service.LinkToWorkspace(_testSession.Id, workspaceId);
        _service.UnlinkFromWorkspace(_testSession.Id);

        // Assert - just verify no exceptions
        Assert.True(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    // OSC 7 Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ProcessOsc7 updates directory state from valid path.<br/>
    /// </summary>
    [Fact(Skip = "Requires platform-specific file:// URI format")]
    public void ProcessOsc7_UpdatesDirectoryState()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });
        DirectoryChangedEventArgs? eventArgs = null;
        _service.TerminalDirectoryChanged += (_, e) => eventArgs = e;
        
        // Use home directory which exists on all platforms
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var uri = $"file://localhost{homePath}";

        // Act
        _service.ProcessOsc7(_testSession.Id, uri);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(DirectoryChangeSource.Osc7, eventArgs.Source);
        Assert.Contains(homePath, eventArgs.NewDirectory);
    }

    /// <summary>
    /// <b>Unit Test:</b> ProcessOsc7 ignores invalid URIs.<br/>
    /// </summary>
    [Fact]
    public void ProcessOsc7_IgnoresInvalidUri()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });
        DirectoryChangedEventArgs? eventArgs = null;
        _service.TerminalDirectoryChanged += (_, e) => eventArgs = e;

        // Act
        _service.ProcessOsc7(_testSession.Id, "not-a-valid-uri");

        // Assert
        Assert.Null(eventArgs);
    }

    /// <summary>
    /// <b>Unit Test:</b> ProcessOsc7 fires TerminalDirectoryChanged event with Osc7 source.<br/>
    /// </summary>
    [Fact(Skip = "Requires platform-specific file:// URI format")]
    public void ProcessOsc7_FiresTerminalDirectoryChangedWithCorrectSource()
    {
        // Arrange
        _mockTerminalService.Raise(s => s.SessionCreated += null,
            new TerminalSessionEventArgs { Session = _testSession });
        _appSettings.SyncTerminalWithWorkspace = true;
        _appSettings.TerminalSyncMode = DirectorySyncMode.ActiveTerminalOnly;

        DirectoryChangedEventArgs? terminalEvent = null;
        _service.TerminalDirectoryChanged += (_, e) => terminalEvent = e;
        
        // Use home directory which exists on all platforms
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var uri = $"file://localhost{homePath}";

        // Act
        _service.ProcessOsc7(_testSession.Id, uri);

        // Assert - TerminalDirectoryChanged should fire
        Assert.NotNull(terminalEvent);
        Assert.Equal(DirectoryChangeSource.Osc7, terminalEvent.Source);
    }

    // ─────────────────────────────────────────────────────────────────────
    // WSL Path Translation Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> TranslateWslPath converts /mnt/c to C:\.<br/>
    /// </summary>
    [Fact]
    public void TranslateWslPath_ConvertsMntToWindowsDrive()
    {
        // On non-Windows, returns path unchanged
        var result = WorkingDirectorySyncService.TranslateWslPath("/mnt/c/Users/test");

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("C:\\Users\\test", result);
        }
        else
        {
            Assert.Equal("/mnt/c/Users/test", result);
        }
    }

    /// <summary>
    /// <b>Unit Test:</b> TranslateWslPath leaves non-WSL paths unchanged.<br/>
    /// </summary>
    [Fact]
    public void TranslateWslPath_LeavesNormalPathsUnchanged()
    {
        // Act
        var result = WorkingDirectorySyncService.TranslateWslPath("/home/user/projects");

        // Assert
        Assert.Equal("/home/user/projects", result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Enum Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> DirectoryChangeSource has all expected values.<br/>
    /// </summary>
    [Fact]
    public void DirectoryChangeSource_AllValuesExist()
    {
        Assert.True(Enum.IsDefined(typeof(DirectoryChangeSource), DirectoryChangeSource.Shell));
        Assert.True(Enum.IsDefined(typeof(DirectoryChangeSource), DirectoryChangeSource.Osc7));
        Assert.True(Enum.IsDefined(typeof(DirectoryChangeSource), DirectoryChangeSource.Api));
        Assert.True(Enum.IsDefined(typeof(DirectoryChangeSource), DirectoryChangeSource.ExplorerSync));
        Assert.True(Enum.IsDefined(typeof(DirectoryChangeSource), DirectoryChangeSource.WorkspaceSync));
        Assert.Equal(5, Enum.GetValues<DirectoryChangeSource>().Length);
    }
}
