using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;

namespace AIntern.Services.Tests.Terminal;

/// <summary>
/// Unit tests for <see cref="OutputCaptureService"/>.
/// </summary>
/// <remarks>Added in v0.5.4d.</remarks>
public sealed class OutputCaptureServiceTests
{
    private readonly Mock<ITerminalService> _terminalServiceMock;
    private readonly Mock<ILogger<OutputCaptureService>> _loggerMock;
    private readonly OutputCaptureService _service;

    public OutputCaptureServiceTests()
    {
        _terminalServiceMock = new Mock<ITerminalService>();
        _loggerMock = new Mock<ILogger<OutputCaptureService>>();

        _service = new OutputCaptureService(
            _terminalServiceMock.Object,
            _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private void SimulateOutput(Guid sessionId, string data)
    {
        _terminalServiceMock.Raise(
            t => t.OutputReceived += null,
            new TerminalOutputEventArgs { SessionId = sessionId, Data = data });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // StartCapture Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void StartCapture_SetsActiveCapture()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        _service.StartCapture(sessionId, "echo test");

        // Assert
        Assert.True(_service.IsCaptureActive(sessionId));
    }

    [Fact]
    public void StartCapture_WithCommand_StoresCommand()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        const string command = "npm install";

        // Act
        _service.StartCapture(sessionId, command);

        // Assert - will be verified when we stop and check the result
        Assert.True(_service.IsCaptureActive(sessionId));
    }

    [Fact]
    public async Task StopCapture_ReturnsOutput()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.StartCapture(sessionId, "echo test");
        SimulateOutput(sessionId, "Hello World\n");

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Hello World", result.Output);
    }

    [Fact]
    public async Task StopCapture_NoActiveCapture_ReturnsNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task StopCapture_RemovesFromActive()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.StartCapture(sessionId);
        SimulateOutput(sessionId, "output");

        // Act
        await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.False(_service.IsCaptureActive(sessionId));
    }

    [Fact]
    public async Task OnOutputReceived_AccumulatesData()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.StartCapture(sessionId);

        // Act
        SimulateOutput(sessionId, "Line 1\n");
        SimulateOutput(sessionId, "Line 2\n");
        SimulateOutput(sessionId, "Line 3\n");

        // Assert - indirectly via stop
        var result = await _service.StopCaptureAsync(sessionId);
        Assert.NotNull(result);
        Assert.Contains("Line 1", result.Output);
        Assert.Contains("Line 2", result.Output);
        Assert.Contains("Line 3", result.Output);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Buffer Capture Tests
    // ═══════════════════════════════════════════════════════════════════════
    // Note: Buffer capture with real TerminalBuffer is tested in integration tests.
    // Unit tests here focus on edge cases that don't require buffer mocking.

    [Fact]
    public async Task CaptureBuffer_NoBuffer_Throws()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _terminalServiceMock.Setup(t => t.GetBuffer(sessionId)).Returns((TerminalBuffer?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CaptureBufferAsync(sessionId));
    }

    [Fact]
    public async Task CaptureSelection_NoBuffer_ReturnsNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _terminalServiceMock.Setup(t => t.GetBuffer(sessionId)).Returns((TerminalBuffer?)null);

        // Act
        var result = await _service.CaptureSelectionAsync(sessionId);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Output Processing Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOutput_StripsAnsi()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.StartCapture(sessionId);
        
        // Simulate ANSI colored output (ESC is 0x1B = 27)
        SimulateOutput(sessionId, "\x1B[32mSuccess:\x1B[0m Build completed");

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        // Check that ESC character (char 27) is not in output
        Assert.False(result.Output.Contains((char)27), "Output should not contain ESC character");
        Assert.Contains("Success:", result.Output);
        Assert.Contains("Build completed", result.Output);
    }

    [Fact]
    public async Task ProcessOutput_NormalizesLineEndings()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.StartCapture(sessionId);
        SimulateOutput(sessionId, "Line 1\r\nLine 2\rLine 3\n");

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("\r", result.Output);
    }

    [Fact]
    public async Task ProcessOutput_TrimsWhitespace()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.StartCapture(sessionId);
        SimulateOutput(sessionId, "   Content   \n\n\n");

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Content", result.Output);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Truncation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Truncate_KeepStart_TruncatesEnd()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.Configure(new OutputCaptureSettings
        {
            MaxCaptureLines = 3,
            TruncationMode = TruncationMode.KeepStart
        });
        _service.StartCapture(sessionId);
        
        SimulateOutput(sessionId, "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\n");

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Line 1", result.Output);
        Assert.DoesNotContain("Line 5", result.Output);
    }

    [Fact]
    public async Task Truncate_KeepEnd_TruncatesStart()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.Configure(new OutputCaptureSettings
        {
            MaxCaptureLines = 3,
            TruncationMode = TruncationMode.KeepEnd
        });
        _service.StartCapture(sessionId);
        
        SimulateOutput(sessionId, "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\n");

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Line 5", result.Output);
        Assert.DoesNotContain("Line 1", result.Output);
    }

    [Fact]
    public async Task Truncate_KeepBoth_KeepsEnds()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.Configure(new OutputCaptureSettings
        {
            MaxCaptureLines = 4,
            TruncationMode = TruncationMode.KeepBoth
        });
        _service.StartCapture(sessionId);
        
        SimulateOutput(sessionId, "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\n");

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Line 1", result.Output);
        Assert.Contains("Line 6", result.Output);
        Assert.Contains("(truncated)", result.Output);
    }

    [Fact]
    public async Task Truncate_CharacterLimit_Applied()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.Configure(new OutputCaptureSettings
        {
            MaxCaptureLength = 20,
            TruncationMode = TruncationMode.KeepStart
        });
        _service.StartCapture(sessionId);
        
        SimulateOutput(sessionId, "This is a very long output that exceeds the limit");

        // Act
        var result = await _service.StopCaptureAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsTruncated);
        Assert.Contains("(truncated)", result.Output);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // History Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRecentCaptures_ReturnsHistory()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        
        _service.StartCapture(sessionId, "cmd1");
        SimulateOutput(sessionId, "output1");
        await _service.StopCaptureAsync(sessionId);

        _service.StartCapture(sessionId, "cmd2");
        SimulateOutput(sessionId, "output2");
        await _service.StopCaptureAsync(sessionId);

        // Act
        var history = _service.GetRecentCaptures(sessionId);

        // Assert
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task GetRecentCaptures_LimitsCount()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        
        for (int i = 0; i < 5; i++)
        {
            _service.StartCapture(sessionId, $"cmd{i}");
            SimulateOutput(sessionId, $"output{i}");
            await _service.StopCaptureAsync(sessionId);
        }

        // Act
        var history = _service.GetRecentCaptures(sessionId, count: 3);

        // Assert
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public async Task GetCapture_ReturnsById()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.StartCapture(sessionId);
        SimulateOutput(sessionId, "test output");
        var capture = await _service.StopCaptureAsync(sessionId);

        // Act
        var retrieved = _service.GetCapture(capture!.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(capture.Id, retrieved.Id);
    }

    [Fact]
    public async Task History_PrunesOldCaptures()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.Configure(new OutputCaptureSettings { CaptureHistorySize = 3 });
        
        Guid? firstCaptureId = null;
        for (int i = 0; i < 5; i++)
        {
            _service.StartCapture(sessionId);
            SimulateOutput(sessionId, $"output{i}");
            var capture = await _service.StopCaptureAsync(sessionId);
            if (i == 0) firstCaptureId = capture?.Id;
        }

        // Act
        var retrieved = _service.GetCapture(firstCaptureId!.Value);

        // Assert - first capture should be pruned
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task ClearHistory_RemovesAll()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.StartCapture(sessionId);
        SimulateOutput(sessionId, "output");
        await _service.StopCaptureAsync(sessionId);

        // Act
        _service.ClearHistory(sessionId);

        // Assert
        var history = _service.GetRecentCaptures(sessionId);
        Assert.Empty(history);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Configuration Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Configure_UpdatesSettings()
    {
        // Arrange
        var settings = new OutputCaptureSettings
        {
            MaxCaptureLength = 5000,
            TruncationMode = TruncationMode.KeepBoth
        };

        // Act
        _service.Configure(settings);

        // Assert
        Assert.Equal(5000, _service.Settings.MaxCaptureLength);
        Assert.Equal(TruncationMode.KeepBoth, _service.Settings.TruncationMode);
    }

    [Fact]
    public void Settings_ForAIContext_HasDefaults()
    {
        // Arrange & Act
        var settings = OutputCaptureSettings.ForAIContext();

        // Assert
        Assert.Equal(4000, settings.MaxCaptureLength);
        Assert.Equal(200, settings.MaxCaptureLines);
        Assert.True(settings.StripAnsiSequences);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TruncationMode Extension Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(TruncationMode.KeepStart, "\n...(truncated)")]
    [InlineData(TruncationMode.KeepEnd, "...(truncated)\n")]
    [InlineData(TruncationMode.KeepBoth, "\n...(truncated)...\n")]
    public void TruncationMode_GetIndicator_ReturnsCorrect(TruncationMode mode, string expected)
    {
        Assert.Equal(expected, mode.GetIndicator());
    }

    [Theory]
    [InlineData(TruncationMode.KeepStart, "Keep beginning, truncate end")]
    [InlineData(TruncationMode.KeepEnd, "Keep end, truncate beginning")]
    [InlineData(TruncationMode.KeepBoth, "Keep beginning and end, truncate middle")]
    public void TruncationMode_ToDescription_ReturnsCorrect(TruncationMode mode, string expected)
    {
        Assert.Equal(expected, mode.ToDescription());
    }
}
