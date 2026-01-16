namespace AIntern.Services.Tests;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SnippetApplyService"/>.
/// </summary>
public class SnippetApplyServiceTests
{
    private readonly Mock<IDiffService> _mockDiffService;
    private readonly Mock<IBackupService> _mockBackupService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<SnippetApplyService>> _mockLogger;
    private readonly AppSettings _settings;
    private readonly SnippetApplyService _service;

    public SnippetApplyServiceTests()
    {
        _mockDiffService = new Mock<IDiffService>();
        _mockBackupService = new Mock<IBackupService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<SnippetApplyService>>();

        _settings = new AppSettings { CreateBackupBeforeApply = false };
        _mockSettingsService.Setup(s => s.CurrentSettings).Returns(_settings);

        _mockDiffService
            .Setup(d => d.ComputeDiff(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string, string>((orig, proposed, path) => 
                DiffResult.NoChanges(path, proposed));

        _service = new SnippetApplyService(
            _mockDiffService.Object,
            _mockBackupService.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task PreviewSnippetAsync_ReplaceFile_ReturnsFullContent()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "old content");
            var options = SnippetApplyOptions.FullReplace();

            // Act
            var preview = await _service.PreviewSnippetAsync(tempFile, "new content", options);

            // Assert
            Assert.Equal("new content", preview.ResultContent);
            Assert.Equal(1, preview.LinesAdded);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PreviewSnippetAsync_InsertAfterLine_InsertsCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "line1\nline2\nline3");
            var options = SnippetApplyOptions.InsertAfterLine(2);

            // Act
            var preview = await _service.PreviewSnippetAsync(tempFile, "inserted", options);

            // Assert
            var lines = preview.ResultContent.Split('\n');
            Assert.Equal(4, lines.Length);
            Assert.Equal("line2", lines[1]);
            Assert.Equal("inserted", lines[2]);
            Assert.Equal("line3", lines[3]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ValidateOptionsAsync_InvalidTargetLine_ReturnsError()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "line1\nline2");
            var options = new SnippetApplyOptions
            {
                InsertMode = SnippetInsertMode.InsertAfter,
                TargetLine = -1
            };

            // Act
            var result = await _service.ValidateOptionsAsync(tempFile, options);

            // Assert
            Assert.False(result.IsValid);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DetectIndentationAsync_FileWithSpaces_ReturnsSpaces()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "class Foo {\n    public int X { get; }\n}");

            // Act
            var style = await _service.DetectIndentationAsync(tempFile);

            // Assert
            Assert.False(style.UseTabs);
            Assert.Equal(4, style.SpacesPerIndent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SuggestLocationAsync_NewFile_SuggestsReplaceFile()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs");

        // Act
        var suggestion = await _service.SuggestLocationAsync(nonExistentPath, "class Test {}");

        // Assert
        Assert.NotNull(suggestion);
        Assert.Equal(SnippetInsertMode.ReplaceFile, suggestion.SuggestedMode);
        Assert.Equal(1.0, suggestion.Confidence);
    }

    [Fact]
    public async Task ApplySnippetAsync_InvalidOptions_ReturnsFailed()
    {
        // Arrange
        var options = new SnippetApplyOptions
        {
            InsertMode = SnippetInsertMode.Replace,
            ReplaceRange = null // Invalid - Replace requires ReplaceRange
        };

        // Act
        var result = await _service.ApplySnippetAsync("/tmp/test.cs", "content", options);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
