using Xunit;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIntern.Desktop.Tests.Services;

/// <summary>
/// Unit tests for the QuickActionService (v0.4.5g).
/// </summary>
public sealed class QuickActionServiceTests
{
    private readonly Mock<IClipboardService> _mockClipboard;
    private readonly Mock<ILogger<QuickActionService>> _mockLogger;

    public QuickActionServiceTests()
    {
        _mockClipboard = new Mock<IClipboardService>();
        _mockLogger = new Mock<ILogger<QuickActionService>>();
    }

    private QuickActionService CreateService() =>
        new(_mockClipboard.Object, _mockLogger.Object);

    private static CodeBlock CreateTestBlock(
        string? content = "test content",
        string? targetPath = "test.cs",
        CodeBlockStatus status = CodeBlockStatus.Pending,
        CodeBlockType type = CodeBlockType.Snippet) =>
        new()
        {
            Id = Guid.NewGuid(),
            Content = content ?? string.Empty,
            TargetFilePath = targetPath,
            Status = status,
            BlockType = type
        };

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor registers default actions.
    /// </summary>
    [Fact]
    public void Constructor_RegistersDefaultActions()
    {
        // Act
        var service = CreateService();
        var actions = service.GetAllActions().ToList();

        // Assert
        Assert.Equal(8, actions.Count);
        Assert.Contains(actions, a => a.Id == "apply");
        Assert.Contains(actions, a => a.Id == "copy");
        Assert.Contains(actions, a => a.Id == "diff");
        Assert.Contains(actions, a => a.Id == "open");
        Assert.Contains(actions, a => a.Id == "options");
        Assert.Contains(actions, a => a.Id == "reject");
        Assert.Contains(actions, a => a.Id == "run");
        Assert.Contains(actions, a => a.Id == "insert");
    }

    #endregion

    #region GetAvailableActions Tests

    /// <summary>
    /// Verifies GetAvailableActions returns only enabled actions.
    /// </summary>
    [Fact]
    public void GetAvailableActions_ReturnsEnabledActionsOnly()
    {
        // Arrange
        var service = CreateService();
        var block = CreateTestBlock();

        // Act
        var actions = service.GetAvailableActions(block).ToList();

        // Assert - should include apply, copy, diff, open, options, reject, insert (not run - not command type)
        Assert.Contains(actions, a => a.Id == "apply");
        Assert.Contains(actions, a => a.Id == "copy");
        Assert.DoesNotContain(actions, a => a.Id == "run");
    }

    /// <summary>
    /// Verifies GetAvailableActions returns sorted by priority.
    /// </summary>
    [Fact]
    public void GetAvailableActions_SortedByPriority()
    {
        // Arrange
        var service = CreateService();
        var block = CreateTestBlock();

        // Act
        var actions = service.GetAvailableActions(block).ToList();

        // Assert - priorities should be in ascending order
        for (int i = 1; i < actions.Count; i++)
        {
            Assert.True(actions[i - 1].Priority <= actions[i].Priority);
        }
    }

    /// <summary>
    /// Verifies command block gets run action.
    /// </summary>
    [Fact]
    public void GetAvailableActions_CommandBlock_IncludesRunAction()
    {
        // Arrange
        var service = CreateService();
        var block = CreateTestBlock(type: CodeBlockType.Command);

        // Act
        var actions = service.GetAvailableActions(block).ToList();

        // Assert
        Assert.Contains(actions, a => a.Id == "run");
    }

    /// <summary>
    /// Verifies applied block has limited actions.
    /// </summary>
    [Fact]
    public void GetAvailableActions_AppliedBlock_ExcludesApplyAndReject()
    {
        // Arrange
        var service = CreateService();
        var block = CreateTestBlock(status: CodeBlockStatus.Applied);

        // Act
        var actions = service.GetAvailableActions(block).ToList();

        // Assert
        Assert.DoesNotContain(actions, a => a.Id == "apply");
        Assert.DoesNotContain(actions, a => a.Id == "reject");
        Assert.Contains(actions, a => a.Id == "copy");
    }

    #endregion

    #region ExecuteAsync Tests

    /// <summary>
    /// Verifies Copy action copies to clipboard.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Copy_CopiesToClipboard()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Copy();
        var block = CreateTestBlock(content: "test code");

        // Act
        var result = await service.ExecuteAsync(action, block);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(QuickActionType.Copy, result.ActionType);
        _mockClipboard.Verify(c => c.SetTextAsync("test code"), Times.Once);
    }

    /// <summary>
    /// Verifies Copy action fails for empty block.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CopyEmptyBlock_Fails()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Copy();
        var block = CreateTestBlock(content: "");

        // Act
        var result = await service.ExecuteAsync(action, block);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Code block is empty", result.Message);
    }

    /// <summary>
    /// Verifies Reject action changes block status.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Reject_ChangesStatus()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Reject();
        var block = CreateTestBlock(status: CodeBlockStatus.Pending);

        // Act
        var result = await service.ExecuteAsync(action, block);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CodeBlockStatus.Rejected, block.Status);
    }

    /// <summary>
    /// Verifies Reject fails for non-pending block.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RejectAppliedBlock_Fails()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Reject();
        var block = CreateTestBlock(status: CodeBlockStatus.Applied);

        // Act
        var result = await service.ExecuteAsync(action, block);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Block is not pending", result.Message);
    }

    /// <summary>
    /// Verifies Apply returns success with block for ViewModel handling.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Apply_ReturnsBlockForViewModel()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Apply();
        var block = CreateTestBlock();

        // Act
        var result = await service.ExecuteAsync(action, block);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(block, result.Data);
    }

    /// <summary>
    /// Verifies Apply fails if no target path.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ApplyNoPath_Fails()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Apply();
        var block = CreateTestBlock(targetPath: null);

        // Act
        var result = await service.ExecuteAsync(action, block);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No target file path specified", result.Message);
    }

    #endregion

    #region ExecuteByIdAsync Tests

    /// <summary>
    /// Verifies ExecuteByIdAsync finds and executes action.
    /// </summary>
    [Fact]
    public async Task ExecuteByIdAsync_KnownAction_Executes()
    {
        // Arrange
        var service = CreateService();
        var block = CreateTestBlock();

        // Act
        var result = await service.ExecuteByIdAsync("copy", block);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(QuickActionType.Copy, result.ActionType);
    }

    /// <summary>
    /// Verifies ExecuteByIdAsync fails for unknown action.
    /// </summary>
    [Fact]
    public async Task ExecuteByIdAsync_UnknownAction_Fails()
    {
        // Arrange
        var service = CreateService();
        var block = CreateTestBlock();

        // Act
        var result = await service.ExecuteByIdAsync("unknown", block);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Unknown action", result.Message);
    }

    #endregion

    #region Action Registration Tests

    /// <summary>
    /// Verifies custom action can be registered.
    /// </summary>
    [Fact]
    public void RegisterAction_AddsNewAction()
    {
        // Arrange
        var service = CreateService();
        var customAction = new QuickAction(
            Id: "custom",
            Type: QuickActionType.Apply,
            Label: "Custom",
            Icon: "CustomIcon",
            Tooltip: "Custom action",
            Shortcut: null,
            IsEnabled: _ => true,
            Priority: 999);

        // Act
        service.RegisterAction(customAction);

        // Assert
        var action = service.GetAction("custom");
        Assert.NotNull(action);
        Assert.Equal("Custom", action.Label);
    }

    /// <summary>
    /// Verifies action can be unregistered.
    /// </summary>
    [Fact]
    public void UnregisterAction_RemovesAction()
    {
        // Arrange
        var service = CreateService();

        // Act
        var removed = service.UnregisterAction("copy");

        // Assert
        Assert.True(removed);
        Assert.Null(service.GetAction("copy"));
    }

    /// <summary>
    /// Verifies unregister returns false for unknown action.
    /// </summary>
    [Fact]
    public void UnregisterAction_UnknownAction_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var removed = service.UnregisterAction("unknown");

        // Assert
        Assert.False(removed);
    }

    #endregion

    #region Event Tests

    /// <summary>
    /// Verifies ActionExecuting event is raised.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RaisesActionExecutingEvent()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Copy();
        var block = CreateTestBlock();
        QuickActionExecutingEventArgs? eventArgs = null;
        service.ActionExecuting += (s, e) => eventArgs = e;

        // Act
        await service.ExecuteAsync(action, block);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(action, eventArgs.Action);
        Assert.Equal(block, eventArgs.Block);
    }

    /// <summary>
    /// Verifies ActionExecuted event is raised.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RaisesActionExecutedEvent()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Copy();
        var block = CreateTestBlock();
        QuickActionExecutedEventArgs? eventArgs = null;
        service.ActionExecuted += (s, e) => eventArgs = e;

        // Act
        await service.ExecuteAsync(action, block);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(action, eventArgs.Action);
        Assert.True(eventArgs.Result.IsSuccess);
    }

    /// <summary>
    /// Verifies cancellation via ActionExecuting event.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CancelledViaEvent_Fails()
    {
        // Arrange
        var service = CreateService();
        var action = QuickAction.Copy();
        var block = CreateTestBlock();
        service.ActionExecuting += (s, e) => e.Cancel = true;

        // Act
        var result = await service.ExecuteAsync(action, block);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Action was cancelled", result.Message);
        _mockClipboard.Verify(c => c.SetTextAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion
}
