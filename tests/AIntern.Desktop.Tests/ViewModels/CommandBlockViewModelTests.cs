using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="CommandBlockViewModel"/>.
/// </summary>
/// <remarks>Added in v0.5.4e.</remarks>
public sealed class CommandBlockViewModelTests
{
    private readonly Mock<ICommandExecutionService> _executionServiceMock;
    private readonly Mock<ITerminalService> _terminalServiceMock;
    private readonly Mock<ILogger<CommandBlockViewModel>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;

    public CommandBlockViewModelTests()
    {
        _executionServiceMock = new Mock<ICommandExecutionService>();
        _terminalServiceMock = new Mock<ITerminalService>();
        _loggerMock = new Mock<ILogger<CommandBlockViewModel>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();

        _loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
    }

    private CommandBlockViewModel CreateViewModel(CommandBlock? command = null)
    {
        var vm = new CommandBlockViewModel(
            _executionServiceMock.Object,
            _terminalServiceMock.Object,
            _loggerMock.Object);

        if (command != null)
        {
            vm.Command = command;
        }

        return vm;
    }

    private static CommandBlock CreateCommand(
        string text = "echo test",
        string? language = "bash",
        bool isDangerous = false,
        string? dangerWarning = null)
    {
        return new CommandBlock
        {
            Id = Guid.NewGuid(),
            Command = text,
            Language = language,
            Status = CommandBlockStatus.Pending,
            IsPotentiallyDangerous = isDangerous,
            DangerWarning = dangerWarning,
            ConfidenceScore = 0.95f
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Command Text Properties
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CommandText_ReturnsFullCommand()
    {
        var command = CreateCommand("npm install --save express");
        var vm = CreateViewModel(command);

        Assert.Equal("npm install --save express", vm.CommandText);
    }

    [Fact]
    public void CommandPreview_TruncatesLongLines()
    {
        var longCommand = "npm install some-very-long-package-name-that-exceeds-sixty-characters-limit-here";
        var command = CreateCommand(longCommand);
        var vm = CreateViewModel(command);

        Assert.True(vm.CommandPreview.Length <= 60);
        Assert.EndsWith("...", vm.CommandPreview);
    }

    [Fact]
    public void CommandPreview_ReturnsFirstLine()
    {
        var multiLine = "echo line1\necho line2\necho line3";
        var command = CreateCommand(multiLine);
        var vm = CreateViewModel(command);

        Assert.Equal("echo line1", vm.CommandPreview);
    }

    [Fact]
    public void LanguageBadge_ReturnsUppercase()
    {
        var command = CreateCommand(language: "powershell");
        var vm = CreateViewModel(command);

        Assert.Equal("POWERSHELL", vm.LanguageBadge);
    }

    [Fact]
    public void IsMultiLine_TrueForMultipleLines()
    {
        var multiLine = "line1\nline2\nline3";
        var command = CreateCommand(multiLine);
        var vm = CreateViewModel(command);

        Assert.True(vm.IsMultiLine);
    }

    [Fact]
    public void LineCount_ReturnsCorrectCount()
    {
        var multiLine = "line1\nline2\nline3";
        var command = CreateCommand(multiLine);
        var vm = CreateViewModel(command);

        Assert.Equal(3, vm.LineCount);
    }

    [Fact]
    public void AdditionalLinesText_ShowsCount()
    {
        var multiLine = "line1\nline2\nline3\nline4";
        var command = CreateCommand(multiLine);
        var vm = CreateViewModel(command);

        Assert.Equal("+3 more lines", vm.AdditionalLinesText);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Status Properties
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CommandBlockStatus.Pending, "")]
    [InlineData(CommandBlockStatus.Copied, "Copied")]
    [InlineData(CommandBlockStatus.SentToTerminal, "Sent")]
    [InlineData(CommandBlockStatus.Executing, "Running...")]
    [InlineData(CommandBlockStatus.Executed, "Executed")]
    [InlineData(CommandBlockStatus.Failed, "Failed")]
    [InlineData(CommandBlockStatus.Cancelled, "Cancelled")]
    public void StatusText_MapsCorrectly(CommandBlockStatus status, string expected)
    {
        var vm = CreateViewModel(CreateCommand());
        vm.Status = status;

        Assert.Equal(expected, vm.StatusText);
    }

    [Fact]
    public void ShowStatus_FalseWhenPending()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.Status = CommandBlockStatus.Pending;

        Assert.False(vm.ShowStatus);
    }

    [Fact]
    public void ShowStatus_TrueWhenNotPending()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.Status = CommandBlockStatus.Copied;

        Assert.True(vm.ShowStatus);
    }

    [Theory]
    [InlineData(CommandBlockStatus.Executed, "success")]
    [InlineData(CommandBlockStatus.Failed, "error")]
    [InlineData(CommandBlockStatus.Cancelled, "warning")]
    [InlineData(CommandBlockStatus.Executing, "running")]
    [InlineData(CommandBlockStatus.Copied, "info")]
    public void StatusClass_ReturnsCorrectClass(CommandBlockStatus status, string expected)
    {
        var vm = CreateViewModel(CreateCommand());
        vm.Status = status;

        Assert.Equal(expected, vm.StatusClass);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Danger Properties
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsDangerous_DelegatesToModel()
    {
        var command = CreateCommand(isDangerous: true, dangerWarning: "Deletes files");
        var vm = CreateViewModel(command);

        Assert.True(vm.IsDangerous);
        Assert.Equal("Deletes files", vm.DangerWarning);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Terminal State
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HasActiveTerminal_QueriesService()
    {
        var session = new TerminalSession { Id = Guid.NewGuid() };
        _terminalServiceMock.Setup(s => s.ActiveSession).Returns(session);

        var vm = CreateViewModel(CreateCommand());

        Assert.True(vm.HasActiveTerminal);
    }

    [Fact]
    public void HasActiveTerminal_FalseWhenNoSession()
    {
        _terminalServiceMock.Setup(s => s.ActiveSession).Returns((TerminalSession?)null);

        var vm = CreateViewModel(CreateCommand());

        Assert.False(vm.HasActiveTerminal);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CanExecute
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanExecute_FalseWhenExecuting()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.IsExecuting = true;

        Assert.False(vm.CanExecute);
    }

    [Fact]
    public void CanExecute_FalseWhenTerminalStatus()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.Status = CommandBlockStatus.Executed;

        Assert.False(vm.CanExecute);
    }

    [Fact]
    public void CanExecute_TrueWhenPending()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.Status = CommandBlockStatus.Pending;
        vm.IsExecuting = false;

        Assert.True(vm.CanExecute);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Copy Command
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CopyAsync_CallsExecutionService()
    {
        var command = CreateCommand();
        _executionServiceMock
            .Setup(s => s.CopyToClipboardAsync(It.Is<CommandBlock>(c => c.Id == command.Id), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel(command);
        await vm.CopyCommand.ExecuteAsync(null);

        _executionServiceMock.Verify(s => s.CopyToClipboardAsync(It.Is<CommandBlock>(c => c.Id == command.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CopyAsync_SetsStatusMessage()
    {
        var command = CreateCommand();
        _executionServiceMock
            .Setup(s => s.CopyToClipboardAsync(It.IsAny<CommandBlock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel(command);
        
        // Capture the status message when it's set (before ClearStatusMessageAfterDelayAsync clears it)
        string? capturedMessage = null;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.StatusMessage) && vm.StatusMessage != null)
            {
                capturedMessage = vm.StatusMessage;
            }
        };

        // Start the command but don't wait for the full delay
        var copyTask = vm.CopyCommand.ExecuteAsync(null);
        
        // Give it a moment to set the message
        await Task.Delay(100);
        
        // The message should have been set at some point
        Assert.Equal("Copied to clipboard", capturedMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Danger Flow
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendToTerminal_ShowsWarningIfDangerous()
    {
        var command = CreateCommand(isDangerous: true);
        _terminalServiceMock.Setup(s => s.ActiveSession).Returns(new TerminalSession());

        var vm = CreateViewModel(command);
        await vm.SendToTerminalCommand.ExecuteAsync(null);

        Assert.True(vm.ShowDangerWarning);
    }

    [Fact]
    public async Task SendToTerminal_ProceedsIfConfirmed()
    {
        var command = CreateCommand(isDangerous: true);
        var session = new TerminalSession { Id = Guid.NewGuid() };
        _terminalServiceMock.Setup(s => s.ActiveSession).Returns(session);
        _executionServiceMock
            .Setup(s => s.SendToTerminalAsync(It.IsAny<CommandBlock>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel(command);
        vm.DangerConfirmed = true;
        await vm.SendToTerminalCommand.ExecuteAsync(null);

        _executionServiceMock.Verify(s => s.SendToTerminalAsync(It.Is<CommandBlock>(c => c.Id == command.Id), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ConfirmDanger_SetsDangerConfirmed()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.ShowDangerWarning = true;

        vm.ConfirmDangerCommand.Execute(null);

        Assert.True(vm.DangerConfirmed);
    }

    [Fact]
    public void ConfirmDanger_HidesWarning()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.ShowDangerWarning = true;

        vm.ConfirmDangerCommand.Execute(null);

        Assert.False(vm.ShowDangerWarning);
    }

    [Fact]
    public void CancelDanger_HidesWarning()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.ShowDangerWarning = true;

        vm.CancelDangerCommand.Execute(null);

        Assert.False(vm.ShowDangerWarning);
    }

    [Fact]
    public void CancelDanger_KeepsDangerUnconfirmed()
    {
        var vm = CreateViewModel(CreateCommand());
        vm.ShowDangerWarning = true;

        vm.CancelDangerCommand.Execute(null);

        Assert.False(vm.DangerConfirmed);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Toggle Expanded
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToggleExpanded_TogglesState()
    {
        var vm = CreateViewModel(CreateCommand());
        Assert.False(vm.IsExpanded);

        vm.ToggleExpandedCommand.Execute(null);
        Assert.True(vm.IsExpanded);

        vm.ToggleExpandedCommand.Execute(null);
        Assert.False(vm.IsExpanded);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Subscription
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void OnStatusChanged_UpdatesFromEvent()
    {
        var command = CreateCommand();
        var vm = CreateViewModel(command);

        var args = new CommandStatusChangedEventArgs
        {
            CommandId = command.Id,
            OldStatus = CommandBlockStatus.Pending,
            NewStatus = CommandBlockStatus.Executing,
            SessionId = Guid.NewGuid()
        };

        _executionServiceMock.Raise(s => s.StatusChanged += null, args);

        Assert.Equal(CommandBlockStatus.Executing, vm.Status);
        Assert.True(vm.IsExecuting);
    }

    [Fact]
    public void OnCommandChanged_NotifiesComputed()
    {
        var vm = CreateViewModel();
        var notified = new List<string>();
        vm.PropertyChanged += (s, e) => notified.Add(e.PropertyName!);

        vm.Command = CreateCommand("test");

        Assert.Contains("CommandText", notified);
        Assert.Contains("CommandPreview", notified);
        Assert.Contains("LanguageBadge", notified);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Factory_Create_SetsCommand()
    {
        var factory = new CommandBlockViewModelFactory(
            _executionServiceMock.Object,
            _terminalServiceMock.Object,
            _loggerFactoryMock.Object);

        var command = CreateCommand();
        var vm = factory.Create(command);

        Assert.Same(command, vm.Command);
    }

    [Fact]
    public void Factory_CreateRange_CreatesAll()
    {
        var factory = new CommandBlockViewModelFactory(
            _executionServiceMock.Object,
            _terminalServiceMock.Object,
            _loggerFactoryMock.Object);

        var commands = new[]
        {
            CreateCommand("cmd1"),
            CreateCommand("cmd2"),
            CreateCommand("cmd3")
        };

        var viewModels = factory.CreateRange(commands).ToList();

        Assert.Equal(3, viewModels.Count);
        Assert.Equal("cmd1", viewModels[0].CommandText);
        Assert.Equal("cmd2", viewModels[1].CommandText);
        Assert.Equal("cmd3", viewModels[2].CommandText);
    }
}
