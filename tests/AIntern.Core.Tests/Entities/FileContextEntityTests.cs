using Xunit;
using AIntern.Core.Entities;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Entities;

/// <summary>
/// Unit tests for the <see cref="FileContextEntity"/> class.
/// </summary>
public class FileContextEntityTests
{
    #region FromFileContext Tests

    /// <summary>
    /// Verifies that FromFileContext maps all properties correctly.
    /// </summary>
    [Fact]
    public void FromFileContext_MapsAllProperties()
    {
        // Arrange
        var content = "public class Test { }";
        var context = FileContext.FromFile("/path/test.cs", content);
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        // Act
        var entity = FileContextEntity.FromFileContext(context, conversationId, messageId);

        // Assert
        Assert.Equal(context.Id, entity.Id);
        Assert.Equal(conversationId, entity.ConversationId);
        Assert.Equal(messageId, entity.MessageId);
        Assert.Equal("/path/test.cs", entity.FilePath);
        Assert.Equal("test.cs", entity.FileName);
        Assert.Equal("csharp", entity.Language);
        Assert.Equal(context.ContentHash, entity.ContentHash);
        Assert.Equal(context.LineCount, entity.LineCount);
        Assert.Equal(context.EstimatedTokens, entity.EstimatedTokens);
    }

    /// <summary>
    /// Verifies that FromFileContext maps partial content properties.
    /// </summary>
    [Fact]
    public void FromFileContext_PartialContent_MapsLineRange()
    {
        // Arrange
        var context = FileContext.FromSelection("/path/test.cs", "selected", 10, 25);
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        // Act
        var entity = FileContextEntity.FromFileContext(context, conversationId, messageId);

        // Assert
        Assert.Equal(10, entity.StartLine);
        Assert.Equal(25, entity.EndLine);
        Assert.True(entity.IsPartialContent);
    }

    #endregion

    #region ToFileContextStub Tests

    /// <summary>
    /// Verifies that ToFileContextStub creates a lightweight object.
    /// </summary>
    [Fact]
    public void ToFileContextStub_CreatesLightweightObject()
    {
        // Arrange
        var entity = new FileContextEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            FilePath = "/path/test.cs",
            FileName = "test.cs",
            Language = "csharp",
            ContentHash = "ABC123DEF456",
            LineCount = 50,
            EstimatedTokens = 100,
            AttachedAt = DateTime.UtcNow
        };

        // Act
        var stub = entity.ToFileContextStub();

        // Assert
        Assert.Equal(entity.Id, stub.Id);
        Assert.Equal(entity.FilePath, stub.FilePath);
        Assert.Equal(entity.FileName, stub.FileName);
        Assert.Equal(entity.ContentHash, stub.ContentHash);
        Assert.False(stub.IsPartialContent);
    }

    /// <summary>
    /// Verifies that ToFileContextStub preserves partial content info.
    /// </summary>
    [Fact]
    public void ToFileContextStub_PartialContent_ShowsLineRange()
    {
        // Arrange
        var entity = new FileContextEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            FilePath = "/path/test.cs",
            FileName = "test.cs",
            ContentHash = "ABC123",
            StartLine = 10,
            EndLine = 25,
            AttachedAt = DateTime.UtcNow
        };

        // Act
        var stub = entity.ToFileContextStub();

        // Assert
        Assert.True(stub.IsPartialContent);
        Assert.Equal("test.cs (lines 10-25)", stub.DisplayLabel);
    }

    #endregion

    #region Computed Properties Tests

    /// <summary>
    /// Verifies that IsPartialContent returns false when no line range.
    /// </summary>
    [Fact]
    public void IsPartialContent_NoLineRange_ReturnsFalse()
    {
        // Arrange
        var entity = new FileContextEntity
        {
            Id = Guid.NewGuid(),
            FilePath = "/test.cs",
            FileName = "test.cs",
            ContentHash = "ABC",
            StartLine = null,
            EndLine = null
        };

        // Act & Assert
        Assert.False(entity.IsPartialContent);
    }

    /// <summary>
    /// Verifies that IsPartialContent returns true when line range set.
    /// </summary>
    [Fact]
    public void IsPartialContent_WithLineRange_ReturnsTrue()
    {
        // Arrange
        var entity = new FileContextEntity
        {
            Id = Guid.NewGuid(),
            FilePath = "/test.cs",
            FileName = "test.cs",
            ContentHash = "ABC",
            StartLine = 5,
            EndLine = 15
        };

        // Act & Assert
        Assert.True(entity.IsPartialContent);
    }

    /// <summary>
    /// Verifies that DisplayLabel shows file name for full file.
    /// </summary>
    [Fact]
    public void DisplayLabel_FullFile_ShowsFileName()
    {
        // Arrange
        var entity = new FileContextEntity
        {
            Id = Guid.NewGuid(),
            FilePath = "/path/test.cs",
            FileName = "test.cs",
            ContentHash = "ABC"
        };

        // Act & Assert
        Assert.Equal("test.cs", entity.DisplayLabel);
    }

    /// <summary>
    /// Verifies that DisplayLabel shows line range for partial file.
    /// </summary>
    [Fact]
    public void DisplayLabel_PartialFile_ShowsLineRange()
    {
        // Arrange
        var entity = new FileContextEntity
        {
            Id = Guid.NewGuid(),
            FilePath = "/path/test.cs",
            FileName = "test.cs",
            ContentHash = "ABC",
            StartLine = 10,
            EndLine = 20
        };

        // Act & Assert
        Assert.Equal("test.cs (lines 10-20)", entity.DisplayLabel);
    }

    #endregion
}
