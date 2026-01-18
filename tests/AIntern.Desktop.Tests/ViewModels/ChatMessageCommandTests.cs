using Xunit;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AIntern.Core.Models;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;
using Moq;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for command block integration in <see cref="ChatMessageViewModel"/>.
/// </summary>
/// <remarks>Added in v0.5.4h.</remarks>
public sealed class ChatMessageCommandTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private static ChatMessageViewModel CreateAssistantMessage(string content = "Test content")
    {
        return new ChatMessageViewModel
        {
            Content = content,
            Role = MessageRole.Assistant,
            IsStreaming = false
        };
    }

    private static CommandBlockViewModel CreateCommandBlockViewModel(
        string command = "npm install",
        bool isDangerous = false)
    {
        var commandBlock = new CommandBlock
        {
            Id = Guid.NewGuid(),
            Command = command,
            Language = "bash",
            Description = "Test command",
            IsPotentiallyDangerous = isDangerous
        };

        // Create mock ViewModel with required services
        var mockExecService = new Mock<AIntern.Core.Interfaces.ICommandExecutionService>();
        var mockTermService = new Mock<AIntern.Core.Interfaces.ITerminalService>();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CommandBlockViewModel>.Instance;

        return new CommandBlockViewModel(mockExecService.Object, mockTermService.Object, logger)
        {
            Command = commandBlock
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HasCommands Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HasCommands_NoCommands_ReturnsFalse()
    {
        var vm = CreateAssistantMessage();

        Assert.False(vm.HasCommands);
    }

    [Fact]
    public void HasCommands_WithCommands_ReturnsTrue()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel());

        Assert.True(vm.HasCommands);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CommandCount Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CommandCount_ReturnsCorrectCount()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("npm install"));
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("npm build"));

        Assert.Equal(2, vm.CommandCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DangerousCommandCount Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DangerousCommandCount_CountsDangerous()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("npm install", isDangerous: false));
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("rm -rf /", isDangerous: true));
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("sudo rm", isDangerous: true));

        Assert.Equal(2, vm.DangerousCommandCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HasDangerousCommands Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HasDangerousCommands_NoDangerous_ReturnsFalse()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("npm install", isDangerous: false));

        Assert.False(vm.HasDangerousCommands);
    }

    [Fact]
    public void HasDangerousCommands_WithDangerous_ReturnsTrue()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("rm -rf", isDangerous: true));

        Assert.True(vm.HasDangerousCommands);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CommandSummary Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CommandSummary_NoCommands_ReturnsEmpty()
    {
        var vm = CreateAssistantMessage();

        Assert.Equal("", vm.CommandSummary);
    }

    [Fact]
    public void CommandSummary_OneCommand_ReturnsSingular()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel());

        Assert.Equal("1 command", vm.CommandSummary);
    }

    [Fact]
    public void CommandSummary_MultipleCommands_ReturnsPlural()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("npm install"));
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("npm build"));
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("npm test"));

        Assert.Equal("3 commands", vm.CommandSummary);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DangerWarning Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DangerWarning_NoDangerous_ReturnsEmpty()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("npm install", isDangerous: false));

        Assert.Equal("", vm.DangerWarning);
    }

    [Fact]
    public void DangerWarning_OneDangerous_ReturnsSingular()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("rm -rf", isDangerous: true));

        Assert.Contains("1 dangerous command", vm.DangerWarning);
    }

    [Fact]
    public void DangerWarning_MultipleDangerous_ReturnsPlural()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("rm -rf /", isDangerous: true));
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("sudo dd", isDangerous: true));

        Assert.Contains("2 dangerous commands", vm.DangerWarning);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ShowCommandActions Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ShowCommandActions_NoCommands_ReturnsFalse()
    {
        var vm = CreateAssistantMessage();
        vm.IsStreaming = false;

        Assert.False(vm.ShowCommandActions);
    }

    [Fact]
    public void ShowCommandActions_AssistantWithCommands_ReturnsTrue()
    {
        var vm = CreateAssistantMessage();
        vm.IsStreaming = false;
        vm.CommandBlocks.Add(CreateCommandBlockViewModel());

        Assert.True(vm.ShowCommandActions);
    }

    [Fact]
    public void ShowCommandActions_Streaming_ReturnsFalse()
    {
        var vm = CreateAssistantMessage();
        vm.IsStreaming = true;
        vm.CommandBlocks.Add(CreateCommandBlockViewModel());

        Assert.False(vm.ShowCommandActions);
    }

    [Fact]
    public void ShowCommandActions_UserMessage_ReturnsFalse()
    {
        var vm = new ChatMessageViewModel
        {
            Content = "Test",
            Role = MessageRole.User,
            IsStreaming = false
        };
        vm.CommandBlocks.Add(CreateCommandBlockViewModel());

        Assert.False(vm.ShowCommandActions);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SetCommandBlocks Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetCommandBlocks_ClearsAndAdds()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel("old command"));

        var newCommands = new[]
        {
            CreateCommandBlockViewModel("new command 1"),
            CreateCommandBlockViewModel("new command 2")
        };

        vm.SetCommandBlocks(newCommands);

        Assert.Equal(2, vm.CommandCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DisposeCommands Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DisposeCommands_ClearsCollection()
    {
        var vm = CreateAssistantMessage();
        vm.CommandBlocks.Add(CreateCommandBlockViewModel());
        vm.CommandBlocks.Add(CreateCommandBlockViewModel());

        vm.DisposeCommands();

        Assert.Empty(vm.CommandBlocks);
        Assert.False(vm.HasCommands);
    }
}
