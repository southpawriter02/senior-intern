using Xunit;
using AIntern.Core.Entities;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Entities;

/// <summary>
/// Unit tests for the <see cref="RecentWorkspaceEntity"/> class.
/// </summary>
public class RecentWorkspaceEntityTests
{
    #region ToWorkspace Tests

    /// <summary>
    /// Verifies that ToWorkspace deserializes open files JSON correctly.
    /// </summary>
    [Fact]
    public void ToWorkspace_DeserializesOpenFiles()
    {
        // Arrange
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            RootPath = "/project",
            OpenFilesJson = "[\"src/file1.cs\",\"src/file2.cs\"]",
            LastAccessedAt = DateTime.UtcNow
        };

        // Act
        var workspace = entity.ToWorkspace();

        // Assert
        Assert.Equal(2, workspace.OpenFiles.Count);
        Assert.Contains("src/file1.cs", workspace.OpenFiles);
        Assert.Contains("src/file2.cs", workspace.OpenFiles);
    }

    /// <summary>
    /// Verifies that ToWorkspace deserializes expanded folders JSON correctly.
    /// </summary>
    [Fact]
    public void ToWorkspace_DeserializesExpandedFolders()
    {
        // Arrange
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            RootPath = "/project",
            ExpandedFoldersJson = "[\"src\",\"src/models\"]",
            LastAccessedAt = DateTime.UtcNow
        };

        // Act
        var workspace = entity.ToWorkspace();

        // Assert
        Assert.Equal(2, workspace.ExpandedFolders.Count);
        Assert.Contains("src", workspace.ExpandedFolders);
    }

    /// <summary>
    /// Verifies that ToWorkspace handles null JSON gracefully.
    /// </summary>
    [Fact]
    public void ToWorkspace_HandlesNullJson()
    {
        // Arrange
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            RootPath = "/project",
            OpenFilesJson = null,
            ExpandedFoldersJson = null,
            LastAccessedAt = DateTime.UtcNow
        };

        // Act
        var workspace = entity.ToWorkspace();

        // Assert
        Assert.Empty(workspace.OpenFiles);
        Assert.Empty(workspace.ExpandedFolders);
    }

    /// <summary>
    /// Verifies that ToWorkspace handles invalid JSON gracefully.
    /// </summary>
    [Fact]
    public void ToWorkspace_HandlesInvalidJson()
    {
        // Arrange
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            RootPath = "/project",
            OpenFilesJson = "not valid json",
            LastAccessedAt = DateTime.UtcNow
        };

        // Act
        var workspace = entity.ToWorkspace();

        // Assert
        Assert.Empty(workspace.OpenFiles);
    }

    #endregion

    #region FromWorkspace Tests

    /// <summary>
    /// Verifies that FromWorkspace serializes open files to JSON.
    /// </summary>
    [Fact]
    public void FromWorkspace_SerializesOpenFiles()
    {
        // Arrange
        var workspace = new Workspace
        {
            RootPath = "/project",
            OpenFiles = ["file1.cs", "file2.cs"]
        };

        // Act
        var entity = RecentWorkspaceEntity.FromWorkspace(workspace);

        // Assert
        Assert.NotNull(entity.OpenFilesJson);
        Assert.Contains("file1.cs", entity.OpenFilesJson);
        Assert.Contains("file2.cs", entity.OpenFilesJson);
    }

    /// <summary>
    /// Verifies that FromWorkspace handles empty lists.
    /// </summary>
    [Fact]
    public void FromWorkspace_EmptyLists_SetsNullJson()
    {
        // Arrange
        var workspace = new Workspace
        {
            RootPath = "/project",
            OpenFiles = [],
            ExpandedFolders = []
        };

        // Act
        var entity = RecentWorkspaceEntity.FromWorkspace(workspace);

        // Assert
        Assert.Null(entity.OpenFilesJson);
        Assert.Null(entity.ExpandedFoldersJson);
    }

    #endregion

    #region UpdateFrom Tests

    /// <summary>
    /// Verifies that UpdateFrom preserves the Id.
    /// </summary>
    [Fact]
    public void UpdateFrom_PreservesId()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var entity = new RecentWorkspaceEntity
        {
            Id = originalId,
            RootPath = "/project",
            LastAccessedAt = DateTime.UtcNow.AddDays(-1)
        };

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(), // Different Id
            RootPath = "/project",
            Name = "Updated Name",
            OpenFiles = ["new.cs"]
        };

        // Act
        entity.UpdateFrom(workspace);

        // Assert
        Assert.Equal(originalId, entity.Id); // Id unchanged
        Assert.Equal("Updated Name", entity.Name);
        Assert.Contains("new.cs", entity.OpenFilesJson!);
    }

    #endregion

    #region Roundtrip Tests

    /// <summary>
    /// Verifies roundtrip conversion preserves all properties.
    /// </summary>
    [Fact]
    public void Roundtrip_PreservesAllProperties()
    {
        // Arrange
        var original = new Workspace
        {
            Id = Guid.NewGuid(),
            RootPath = "/project",
            Name = "My Project",
            OpenFiles = ["a.cs", "b.cs"],
            ExpandedFolders = ["src"],
            ActiveFilePath = "a.cs",
            IsPinned = true
        };

        // Act
        var entity = RecentWorkspaceEntity.FromWorkspace(original);
        var restored = entity.ToWorkspace();

        // Assert
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.RootPath, restored.RootPath);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.OpenFiles.Count, restored.OpenFiles.Count);
        Assert.Equal(original.ActiveFilePath, restored.ActiveFilePath);
        Assert.Equal(original.IsPinned, restored.IsPinned);
    }

    #endregion
}
