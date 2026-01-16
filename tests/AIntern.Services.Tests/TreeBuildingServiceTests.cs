namespace AIntern.Services.Tests;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="TreeBuildingService"/>.
/// </summary>
public class TreeBuildingServiceTests
{
    private readonly TreeBuildingService _service;
    private readonly Mock<ILogger<TreeBuildingService>> _loggerMock;

    public TreeBuildingServiceTests()
    {
        _loggerMock = new Mock<ILogger<TreeBuildingService>>();
        _service = new TreeBuildingService(_loggerMock.Object);
    }

    // Helper to create a file operation
    private static FileOperation CreateFileOp(string path, string content = "// code") => new()
    {
        Path = path,
        Content = content,
        Type = FileOperationType.Create
    };

    #region BuildTreeFromOperations Tests

    [Fact]
    public void BuildTreeFromOperations_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var operations = Array.Empty<FileOperation>();

        // Act
        var result = _service.BuildTreeFromOperations(operations);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void BuildTreeFromOperations_SingleFileRootLevel_ReturnsSingleNode()
    {
        // Arrange
        var operations = new[] { CreateFileOp("README.md", "# Hello") };

        // Act
        var result = _service.BuildTreeFromOperations(operations);

        // Assert
        Assert.Single(result);
        Assert.Equal("README.md", result[0].Name);
        Assert.False(result[0].IsDirectory);
        Assert.NotNull(result[0].Operation);
    }

    [Fact]
    public void BuildTreeFromOperations_SingleFileInDirectory_CreatesDirectoryNode()
    {
        // Arrange
        var operations = new[] { CreateFileOp("src/File.cs") };

        // Act
        var result = _service.BuildTreeFromOperations(operations);

        // Assert
        Assert.Single(result);
        Assert.Equal("src", result[0].Name);
        Assert.True(result[0].IsDirectory);
        Assert.Single(result[0].Children);
        Assert.Equal("File.cs", result[0].Children[0].Name);
    }

    [Fact]
    public void BuildTreeFromOperations_NestedDirectories_CreatesHierarchy()
    {
        // Arrange
        var operations = new[] { CreateFileOp("src/Models/User.cs") };

        // Act
        var result = _service.BuildTreeFromOperations(operations);

        // Assert
        Assert.Single(result);
        var src = result[0];
        Assert.Equal("src", src.Name);
        Assert.Single(src.Children);
        
        var models = src.Children[0];
        Assert.Equal("Models", models.Name);
        Assert.Single(models.Children);
        Assert.Equal("User.cs", models.Children[0].Name);
    }

    [Fact]
    public void BuildTreeFromOperations_MultipleFilesInSameDirectory_GroupsUnderParent()
    {
        // Arrange
        var operations = new[]
        {
            CreateFileOp("src/File1.cs"),
            CreateFileOp("src/File2.cs"),
            CreateFileOp("src/File3.cs")
        };

        // Act
        var result = _service.BuildTreeFromOperations(operations);

        // Assert
        Assert.Single(result);
        var src = result[0];
        Assert.Equal(3, src.Children.Count);
    }

    [Fact]
    public void BuildTreeFromOperations_SortsDirectoriesFirst()
    {
        // Arrange
        var operations = new[]
        {
            CreateFileOp("b.txt"),
            CreateFileOp("a/file.txt"),
            CreateFileOp("c.txt")
        };

        // Act
        var result = _service.BuildTreeFromOperations(operations);

        // Assert
        Assert.Equal(3, result.Count);
        // First item should be directory "a"
        Assert.True(result[0].IsDirectory);
        Assert.Equal("a", result[0].Name);
        // Then files alphabetically
        Assert.False(result[1].IsDirectory);
        Assert.Equal("b.txt", result[1].Name);
        Assert.False(result[2].IsDirectory);
        Assert.Equal("c.txt", result[2].Name);
    }

    [Fact]
    public void BuildTreeFromOperations_SortsFilesAlphabetically()
    {
        // Arrange
        var operations = new[]
        {
            CreateFileOp("z.txt"),
            CreateFileOp("a.txt"),
            CreateFileOp("m.txt")
        };

        // Act
        var result = _service.BuildTreeFromOperations(operations);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("a.txt", result[0].Name);
        Assert.Equal("m.txt", result[1].Name);
        Assert.Equal("z.txt", result[2].Name);
    }

    [Fact]
    public void BuildTreeFromOperations_ReturnsReadOnlyList()
    {
        // Arrange
        var operations = new[] { CreateFileOp("test.txt") };

        // Act
        var result = _service.BuildTreeFromOperations(operations);

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<TreeNode>>(result);
    }

    #endregion

    #region BuildTree Tests

    [Fact]
    public void BuildTree_FromProposal_BuildsTree()
    {
        // Arrange
        var proposal = new FileTreeProposal
        {
            Operations = new[] { CreateFileOp("src/Service.cs") }
        };

        // Act
        var result = _service.BuildTree(proposal);

        // Assert
        Assert.Single(result);
        Assert.Equal("src", result[0].Name);
    }

    #endregion

    #region FlattenTree Tests

    [Fact]
    public void FlattenTree_ReturnsAllNodes()
    {
        // Arrange
        var operations = new[]
        {
            CreateFileOp("src/A.cs"),
            CreateFileOp("src/B.cs")
        };
        var tree = _service.BuildTreeFromOperations(operations);

        // Act
        var flattened = _service.FlattenTree(tree).ToList();

        // Assert
        Assert.Equal(3, flattened.Count); // 1 directory + 2 files
    }

    [Fact]
    public void FlattenTree_ExcludesDirectories_WhenSpecified()
    {
        // Arrange
        var operations = new[]
        {
            CreateFileOp("src/A.cs"),
            CreateFileOp("src/B.cs")
        };
        var tree = _service.BuildTreeFromOperations(operations);

        // Act
        var flattened = _service.FlattenTree(tree, includeDirectories: false).ToList();

        // Assert
        Assert.Equal(2, flattened.Count); // Only files
        Assert.All(flattened, n => Assert.False(n.IsDirectory));
    }

    #endregion

    #region FindAll Tests

    [Fact]
    public void FindAll_ReturnsMatchingNodes()
    {
        // Arrange
        var operations = new[]
        {
            CreateFileOp("src/A.cs"),
            CreateFileOp("src/B.txt"),
            CreateFileOp("tests/C.cs")
        };
        var tree = _service.BuildTreeFromOperations(operations);

        // Act
        var csFiles = _service.FindAll(tree, n => n.Extension == "cs").ToList();

        // Assert
        Assert.Equal(2, csFiles.Count);
    }

    #endregion

    #region FindByPath Tests

    [Fact]
    public void FindByPath_ExistingPath_ReturnsNode()
    {
        // Arrange
        var operations = new[] { CreateFileOp("src/Models/User.cs") };
        var tree = _service.BuildTreeFromOperations(operations);

        // Act
        var node = _service.FindByPath(tree, "src/Models/User.cs");

        // Assert
        Assert.NotNull(node);
        Assert.Equal("User.cs", node.Name);
    }

    [Fact]
    public void FindByPath_NonExistentPath_ReturnsNull()
    {
        // Arrange
        var operations = new[] { CreateFileOp("src/File.cs") };
        var tree = _service.BuildTreeFromOperations(operations);

        // Act
        var node = _service.FindByPath(tree, "nonexistent.cs");

        // Assert
        Assert.Null(node);
    }

    [Fact]
    public void FindByPath_DirectoryPath_ReturnsDirectory()
    {
        // Arrange
        var operations = new[] { CreateFileOp("src/Models/User.cs") };
        var tree = _service.BuildTreeFromOperations(operations);

        // Act
        var node = _service.FindByPath(tree, "src/Models");

        // Assert
        Assert.NotNull(node);
        Assert.True(node.IsDirectory);
        Assert.Equal("Models", node.Name);
    }

    #endregion
}
