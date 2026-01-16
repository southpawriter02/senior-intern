namespace AIntern.Services.Tests.Streaming;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Services.Streaming;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="StreamingDiffCoordinator"/>.
/// </summary>
public class StreamingDiffCoordinatorTests : IDisposable
{
    private readonly Mock<IDiffService> _mockDiffService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<StreamingDiffCoordinator>> _mockLogger;
    private readonly AppSettings _settings;
    private readonly StreamingDiffCoordinator _coordinator;

    public StreamingDiffCoordinatorTests()
    {
        _mockDiffService = new Mock<IDiffService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<StreamingDiffCoordinator>>();

        _settings = new AppSettings { ShowDiffPreviewDuringStreaming = true };
        _mockSettingsService.Setup(s => s.CurrentSettings).Returns(_settings);

        _coordinator = new StreamingDiffCoordinator(
            _mockDiffService.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task OnCodeBlockDetectedAsync_WithValidBlock_CreatesState()
    {
        // Arrange
        var block = new CodeBlock
        {
            Id = Guid.NewGuid(),
            TargetFilePath = "test.cs",
            Content = "public class Test { }"
        };

        // Act
        var state = await _coordinator.OnCodeBlockDetectedAsync(block, "/workspace");

        // Assert
        Assert.NotNull(state);
        Assert.Equal(block.Id, state.BlockId);
        Assert.Equal(DiffComputationStatus.Pending, state.Status);
    }

    [Fact]
    public async Task OnCodeBlockDetectedAsync_WithDisabledSetting_ReturnsCancelledState()
    {
        // Arrange
        _settings.ShowDiffPreviewDuringStreaming = false;
        var block = new CodeBlock
        {
            Id = Guid.NewGuid(),
            TargetFilePath = "test.cs",
            Content = "content"
        };

        // Act
        var state = await _coordinator.OnCodeBlockDetectedAsync(block, "/workspace");

        // Assert
        Assert.Equal(DiffComputationStatus.Cancelled, state.Status);
    }

    [Fact]
    public async Task OnCodeBlockDetectedAsync_WithNoTargetPath_ReturnsCompletedState()
    {
        // Arrange
        var block = new CodeBlock
        {
            Id = Guid.NewGuid(),
            TargetFilePath = null,
            Content = "content"
        };

        // Act
        var state = await _coordinator.OnCodeBlockDetectedAsync(block, "/workspace");

        // Assert
        Assert.Equal(DiffComputationStatus.Completed, state.Status);
    }

    [Fact]
    public void GetComputationState_WithUnknownId_ReturnsNull()
    {
        // Act
        var state = _coordinator.GetComputationState(Guid.NewGuid());

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public void CancelAll_MarksAllStatesAsCancelled()
    {
        // Arrange - pre-populate states by calling detect
        var block1 = new CodeBlock { Id = Guid.NewGuid(), TargetFilePath = "a.cs" };
        var block2 = new CodeBlock { Id = Guid.NewGuid(), TargetFilePath = "b.cs" };
        
        _coordinator.OnCodeBlockDetectedAsync(block1, "/workspace");
        _coordinator.OnCodeBlockDetectedAsync(block2, "/workspace");

        // Act
        _coordinator.CancelAll();

        // Assert
        var states = _coordinator.GetAllStates();
        Assert.All(states, s => Assert.Equal(DiffComputationStatus.Cancelled, s.Status));
    }

    [Fact]
    public void Reset_ClearsAllStates()
    {
        // Arrange
        var block = new CodeBlock { Id = Guid.NewGuid(), TargetFilePath = "test.cs" };
        _coordinator.OnCodeBlockDetectedAsync(block, "/workspace");

        // Act
        _coordinator.Reset();

        // Assert
        Assert.Empty(_coordinator.GetAllStates());
    }

    public void Dispose()
    {
        _coordinator.Dispose();
    }
}
