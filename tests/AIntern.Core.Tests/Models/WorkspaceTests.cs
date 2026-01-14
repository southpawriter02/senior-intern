using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="Workspace"/> class.
/// Verifies default values, path operations, and computed properties.
/// </summary>
public class WorkspaceTests
{
    #region DisplayName Tests

    /// <summary>
    /// Verifies that DisplayName returns the custom name when set.
    /// </summary>
    [Fact]
    public void DisplayName_WithCustomName_ReturnsCustomName()
    {
        // Arrange
        var workspace = new Workspace
        {
            RootPath = "/path/to/project",
            Name = "My Custom Project"
        };

        // Act & Assert
        Assert.Equal("My Custom Project", workspace.DisplayName);
    }

    /// <summary>
    /// Verifies that DisplayName falls back to folder name when no custom name is set.
    /// </summary>
    [Fact]
    public void DisplayName_WithoutCustomName_ReturnsFolderName()
    {
        // Arrange
        var workspace = new Workspace { RootPath = "/path/to/project" };

        // Act & Assert
        Assert.Equal("project", workspace.DisplayName);
    }

    /// <summary>
    /// Verifies that DisplayName falls back to folder name for whitespace-only names.
    /// </summary>
    [Fact]
    public void DisplayName_WithWhitespaceName_ReturnsFolderName()
    {
        // Arrange
        var workspace = new Workspace
        {
            RootPath = "/path/to/myapp",
            Name = "   "
        };

        // Act & Assert
        Assert.Equal("myapp", workspace.DisplayName);
    }

    #endregion

    #region Path Operations Tests

    /// <summary>
    /// Verifies that GetAbsolutePath combines root and relative paths correctly.
    /// </summary>
    [Fact]
    public void GetAbsolutePath_ReturnsCorrectPath()
    {
        // Arrange
        var workspace = new Workspace { RootPath = "/home/user/project" };

        // Act
        var absolute = workspace.GetAbsolutePath("src/file.cs");

        // Assert
        Assert.EndsWith("src/file.cs", absolute.Replace('\\', '/'));
        Assert.StartsWith("/", absolute.Replace('\\', '/'));
    }

    /// <summary>
    /// Verifies that GetRelativePath extracts relative path correctly.
    /// </summary>
    [Fact]
    public void GetRelativePath_ReturnsCorrectPath()
    {
        // Arrange
        var workspace = new Workspace { RootPath = "/home/user/project" };

        // Act
        var relative = workspace.GetRelativePath("/home/user/project/src/file.cs");

        // Assert
        // Path.GetRelativePath uses platform-specific separators
        Assert.Equal("src/file.cs", relative.Replace('\\', '/'));
    }

    /// <summary>
    /// Verifies that ContainsPath returns true for paths inside the workspace.
    /// </summary>
    [Fact]
    public void ContainsPath_InsideWorkspace_ReturnsTrue()
    {
        // Arrange
        var workspace = new Workspace { RootPath = "/home/user/project" };

        // Act & Assert
        Assert.True(workspace.ContainsPath("/home/user/project/src/file.cs"));
        Assert.True(workspace.ContainsPath("/home/user/project/README.md"));
    }

    /// <summary>
    /// Verifies that ContainsPath returns false for paths outside the workspace.
    /// </summary>
    [Fact]
    public void ContainsPath_OutsideWorkspace_ReturnsFalse()
    {
        // Arrange
        var workspace = new Workspace { RootPath = "/home/user/project" };

        // Act & Assert
        Assert.False(workspace.ContainsPath("/home/user/other/file.cs"));
        Assert.False(workspace.ContainsPath("/tmp/file.cs"));
    }

    #endregion

    #region Touch Tests

    /// <summary>
    /// Verifies that Touch updates the LastAccessedAt timestamp.
    /// </summary>
    [Fact]
    public void Touch_UpdatesLastAccessedAt()
    {
        // Arrange
        var workspace = new Workspace { RootPath = "/test" };
        var originalTime = workspace.LastAccessedAt;

        // Small delay to ensure time difference
        System.Threading.Thread.Sleep(10);

        // Act
        workspace.Touch();

        // Assert
        Assert.True(workspace.LastAccessedAt > originalTime);
    }

    #endregion

    #region Default Values Tests

    /// <summary>
    /// Verifies that a new Workspace has correct default values.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var workspace = new Workspace { RootPath = "/test" };

        // Assert
        Assert.NotEqual(Guid.Empty, workspace.Id);
        Assert.Equal(string.Empty, workspace.Name);
        Assert.Empty(workspace.OpenFiles);
        Assert.Null(workspace.ActiveFilePath);
        Assert.Empty(workspace.ExpandedFolders);
        Assert.False(workspace.IsPinned);
        Assert.Empty(workspace.GitIgnorePatterns);
    }

    #endregion
}
