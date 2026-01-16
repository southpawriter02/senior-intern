namespace AIntern.Services.Tests;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ProposalDetectionService"/>.
/// </summary>
public class ProposalDetectionServiceTests
{
    private readonly Mock<IFileTreeParser> _mockParser = new();
    private readonly Mock<ILogger<ProposalDetectionService>> _mockLogger = new();
    private readonly ProposalDetectionOptions _options = new() { MinimumFilesForPanel = 2 };

    private ProposalDetectionService CreateService()
    {
        return new ProposalDetectionService(
            _mockParser.Object,
            Options.Create(_options),
            _mockLogger.Object);
    }

    [Fact]
    public void DetectProposal_BelowThreshold_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        var blocks = new[]
        {
            new CodeBlock
            {
                BlockType = CodeBlockType.CompleteFile,
                TargetFilePath = "src/file.cs",
                Language = "csharp",
                Content = "code"
            }
        };

        // Act
        var result = service.DetectProposal("content", Guid.NewGuid(), blocks);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DetectProposal_IgnoredLanguage_FiltersOut()
    {
        // Arrange
        var service = CreateService();
        var blocks = new[]
        {
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "f1.cs", Language = "csharp", Content = "c" },
            new CodeBlock { BlockType = CodeBlockType.Output, TargetFilePath = "log.txt", Language = "output", Content = "c" }
        };

        // Act
        var result = service.DetectProposal("content", Guid.NewGuid(), blocks);

        // Assert - only 1 applicable block, below threshold
        Assert.Null(result);
    }

    [Fact]
    public void DetectProposal_NoFilePath_FiltersOut()
    {
        // Arrange
        var service = CreateService();
        var blocks = new[]
        {
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "f1.cs", Language = "csharp", Content = "c" },
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "", Language = "csharp", Content = "c" }
        };

        // Act
        var result = service.DetectProposal("content", Guid.NewGuid(), blocks);

        // Assert - only 1 applicable block, below threshold
        Assert.Null(result);
    }

    [Fact]
    public void GetCachedProposal_NotInCache_ReturnsNull()
    {
        var service = CreateService();
        var result = service.GetCachedProposal(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void ClearCache_Clears()
    {
        var service = CreateService();
        var messageId = Guid.NewGuid();

        // Add to cache
        service.DetectProposal("content", messageId, Array.Empty<CodeBlock>());

        // Clear
        service.ClearCache();

        // Should not be in cache
        var result = service.GetCachedProposal(messageId);
        Assert.Null(result);
    }

    [Fact]
    public void ShouldShowProposalPanel_Null_ReturnsFalse()
    {
        var service = CreateService();
        var result = service.ShouldShowProposalPanel(null);
        Assert.False(result);
    }

    [Fact]
    public void ShouldShowProposalPanel_NullProposal_ReturnsFalse()
    {
        var service = CreateService();

        // Null proposal should return false
        var result = service.ShouldShowProposalPanel(null);
        Assert.False(result);
    }
}
