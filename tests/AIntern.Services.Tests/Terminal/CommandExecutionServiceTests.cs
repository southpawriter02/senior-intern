using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;

namespace AIntern.Services.Tests.Terminal;

/// <summary>
/// Unit tests for <see cref="CommandExecutionService"/>.
/// </summary>
/// <remarks>Added in v0.5.4c.</remarks>
public sealed class CommandExecutionServiceTests
{
    private readonly Mock<ITerminalService> _terminalServiceMock;
    private readonly Mock<IShellProfileService> _profileServiceMock;
    private readonly Mock<IClipboardService> _clipboardServiceMock;
    private readonly Mock<ILogger<CommandExecutionService>> _loggerMock;
    private readonly CommandExecutionService _service;

    public CommandExecutionServiceTests()
    {
        _terminalServiceMock = new Mock<ITerminalService>();
        _profileServiceMock = new Mock<IShellProfileService>();
        _clipboardServiceMock = new Mock<IClipboardService>();
        _loggerMock = new Mock<ILogger<CommandExecutionService>>();

        _service = new CommandExecutionService(
            _terminalServiceMock.Object,
            _profileServiceMock.Object,
            _clipboardServiceMock.Object,
            _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private static CommandBlock CreateCommand(string command = "echo test")
    {
        return new CommandBlock
        {
            Command = command,
            MessageId = Guid.NewGuid()
        };
    }

    private static ShellProfile CreateProfile(ShellType shellType = ShellType.Bash)
    {
        return new ShellProfile
        {
            Name = "Test Profile",
            ShellPath = "/bin/bash",
            ShellType = shellType
        };
    }

    private TerminalSession CreateSession()
    {
        return new TerminalSession
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            ShellPath = "/bin/bash"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CopyToClipboard Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CopyToClipboard_SetsClipboardText()
    {
        // Arrange
        var command = CreateCommand("npm install");

        // Act
        await _service.CopyToClipboardAsync(command);

        // Assert
        _clipboardServiceMock.Verify(c => c.SetTextAsync("npm install"), Times.Once);
    }

    [Fact]
    public async Task CopyToClipboard_UpdatesStatusToCopied()
    {
        // Arrange
        var command = CreateCommand();

        // Act
        await _service.CopyToClipboardAsync(command);

        // Assert
        Assert.Equal(CommandBlockStatus.Copied, command.Status);
    }

    [Fact]
    public async Task CopyToClipboard_RaisesStatusChangedEvent()
    {
        // Arrange
        var command = CreateCommand();
        CommandStatusChangedEventArgs? receivedArgs = null;
        _service.StatusChanged += (s, e) => receivedArgs = e;

        // Act
        await _service.CopyToClipboardAsync(command);

        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(command.Id, receivedArgs.CommandId);
        Assert.Equal(CommandBlockStatus.Copied, receivedArgs.NewStatus);
    }

    [Fact]
    public async Task CopyToClipboard_ThrowsWhenClipboardNull()
    {
        // Arrange
        var serviceNoClipboard = new CommandExecutionService(
            _terminalServiceMock.Object,
            _profileServiceMock.Object,
            null,  // No clipboard
            _loggerMock.Object);
        var command = CreateCommand();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => serviceNoClipboard.CopyToClipboardAsync(command));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SendToTerminal Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendToTerminal_WritesToSession()
    {
        // Arrange
        var command = CreateCommand("git status");
        var session = CreateSession();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(session.Id, "git status", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendToTerminalAsync(command);

        // Assert - command sent WITHOUT newline
        _terminalServiceMock.Verify(
            t => t.WriteInputAsync(session.Id, "git status", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendToTerminal_UpdatesStatus()
    {
        // Arrange
        var command = CreateCommand();
        var session = CreateSession();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendToTerminalAsync(command);

        // Assert
        Assert.Equal(CommandBlockStatus.SentToTerminal, command.Status);
    }

    [Fact]
    public async Task SendToTerminal_CreatesSessionIfNeeded()
    {
        // Arrange
        var command = CreateCommand();
        var session = CreateSession();
        var profile = CreateProfile();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns((TerminalSession?)null);
        _profileServiceMock
            .Setup(p => p.GetDefaultProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _terminalServiceMock
            .Setup(t => t.CreateSessionAsync(It.IsAny<TerminalSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(session.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendToTerminalAsync(command);

        // Assert
        _terminalServiceMock.Verify(
            t => t.CreateSessionAsync(It.IsAny<TerminalSessionOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ExecuteAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExecuteAsync_SendsCommandWithNewline()
    {
        // Arrange
        var command = CreateCommand("npm install");
        var session = CreateSession();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.ExecuteAsync(command);

        // Assert - command WITH newline (\\r)
        _terminalServiceMock.Verify(
            t => t.WriteInputAsync(session.Id, "npm install\r", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessResult()
    {
        // Arrange
        var command = CreateCommand();
        var session = CreateSession();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExecuteAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CommandBlockStatus.Executed, result.Status);
        Assert.Equal(session.Id, result.SessionId);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesStatusExecuting()
    {
        // Arrange
        var command = CreateCommand();
        var session = CreateSession();
        var statusChanges = new List<CommandBlockStatus>();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service.StatusChanged += (s, e) => statusChanges.Add(e.NewStatus);

        // Act
        await _service.ExecuteAsync(command);

        // Assert - should have Executing then Executed
        Assert.Contains(CommandBlockStatus.Executing, statusChanges);
        Assert.Contains(CommandBlockStatus.Executed, statusChanges);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesStatusFailed_OnError()
    {
        // Arrange
        var command = CreateCommand();
        var session = CreateSession();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);  // Write fails

        // Act
        var result = await _service.ExecuteAsync(command);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal(CommandBlockStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SetsDuration()
    {
        // Arrange
        var command = CreateCommand();
        var session = CreateSession();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExecuteAsync(command);

        // Assert
        Assert.NotNull(result.Duration);
        Assert.True(result.Duration.Value >= TimeSpan.Zero);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ExecuteAllAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExecuteAllAsync_ExecutesInOrder()
    {
        // Arrange
        var commands = new[]
        {
            CreateCommand("cmd1"),
            CreateCommand("cmd2"),
            CreateCommand("cmd3")
        };
        var session = CreateSession();
        var executionOrder = new List<string>();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, CancellationToken>((_, cmd, _) => executionOrder.Add(cmd.TrimEnd('\r')))
            .ReturnsAsync(true);

        // Act
        var results = await _service.ExecuteAllAsync(commands);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("cmd1", executionOrder[0]);
        Assert.Equal("cmd2", executionOrder[1]);
        Assert.Equal("cmd3", executionOrder[2]);
    }

    [Fact]
    public async Task ExecuteAllAsync_StopsOnError_WhenStopOnErrorTrue()
    {
        // Arrange
        var commands = new[]
        {
            CreateCommand("cmd1"),
            CreateCommand("cmd2"),
            CreateCommand("cmd3")
        };
        var session = CreateSession();
        var callCount = 0;

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount != 2;  // Fail on second command
            });

        // Act
        var results = await _service.ExecuteAllAsync(commands, stopOnError: true);

        // Assert - should stop after second command fails
        Assert.Equal(2, results.Count);
        Assert.True(results[1].IsFailed);
    }

    [Fact]
    public async Task ExecuteAllAsync_ContinuesOnError_WhenStopOnErrorFalse()
    {
        // Arrange
        var commands = new[]
        {
            CreateCommand("cmd1"),
            CreateCommand("cmd2"),
            CreateCommand("cmd3")
        };
        var session = CreateSession();
        var callCount = 0;

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _terminalServiceMock
            .Setup(t => t.WriteInputAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount != 2;  // Fail on second command
            });

        // Act
        var results = await _service.ExecuteAllAsync(commands, stopOnError: false);

        // Assert - should continue and execute all 3
        Assert.Equal(3, results.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CancelExecutionAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CancelExecution_SendsInterruptSignal()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        await _service.CancelExecutionAsync(sessionId);

        // Assert
        _terminalServiceMock.Verify(
            t => t.SendSignalAsync(sessionId, TerminalSignal.Interrupt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetStatus Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStatus_ReturnsCurrentStatus()
    {
        // Arrange
        var command = CreateCommand();
        var session = CreateSession();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);
        _clipboardServiceMock.Setup(c => c.SetTextAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _service.CopyToClipboardAsync(command);
        var status = _service.GetStatus(command.Id);

        // Assert
        Assert.Equal(CommandBlockStatus.Copied, status);
    }

    [Fact]
    public void GetStatus_ReturnsPendingForUnknown()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var status = _service.GetStatus(unknownId);

        // Assert
        Assert.Equal(CommandBlockStatus.Pending, status);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EnsureTerminalSessionAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EnsureSession_ReusesActiveIfNoPreference()
    {
        // Arrange
        var session = CreateSession();
        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(session);

        // Act
        var sessionId = await _service.EnsureTerminalSessionAsync();

        // Assert
        Assert.Equal(session.Id, sessionId);
        _terminalServiceMock.Verify(
            t => t.CreateSessionAsync(It.IsAny<TerminalSessionOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnsureSession_CreatesNewIfShellPreferred()
    {
        // Arrange
        var activeSession = CreateSession();
        var newSession = CreateSession();
        var profile = CreateProfile(ShellType.Zsh);

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns(activeSession);
        _profileServiceMock
            .Setup(p => p.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { profile });
        _terminalServiceMock
            .Setup(t => t.CreateSessionAsync(It.IsAny<TerminalSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newSession);

        // Act
        var sessionId = await _service.EnsureTerminalSessionAsync(ShellType.Zsh);

        // Assert - should create new session since we specified a shell type
        Assert.Equal(newSession.Id, sessionId);
        _terminalServiceMock.Verify(
            t => t.CreateSessionAsync(It.IsAny<TerminalSessionOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureSession_UsesDefaultProfile_WhenNoMatch()
    {
        // Arrange
        var session = CreateSession();
        var defaultProfile = CreateProfile();

        _terminalServiceMock.Setup(t => t.ActiveSession).Returns((TerminalSession?)null);
        _profileServiceMock
            .Setup(p => p.GetDefaultProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);
        _terminalServiceMock
            .Setup(t => t.CreateSessionAsync(It.IsAny<TerminalSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        await _service.EnsureTerminalSessionAsync();

        // Assert
        _profileServiceMock.Verify(
            p => p.GetDefaultProfileAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
