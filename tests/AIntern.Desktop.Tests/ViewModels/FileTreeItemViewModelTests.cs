using Xunit;
using Moq;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using AIntern.Desktop.Utilities;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="FileTreeItemViewModel"/>.
/// </summary>
public class FileTreeItemViewModelTests
{
    private readonly Mock<IFileTreeItemParent> _mockParent;

    public FileTreeItemViewModelTests()
    {
        _mockParent = new Mock<IFileTreeItemParent>();
        _mockParent.Setup(p => p.GetRelativePath(It.IsAny<string>()))
            .Returns<string>(path => path);
    }

    #region Factory Tests

    [Fact]
    public void FromFileSystemItem_CreatesCorrectViewModel_ForFile()
    {
        // Arrange
        var item = new FileSystemItem
        {
            Name = "test.cs",
            Path = "/project/src/test.cs",
            Type = FileSystemItemType.File,
            HasChildren = false
        };

        // Act
        var vm = FileTreeItemViewModel.FromFileSystemItem(item, _mockParent.Object, depth: 2);

        // Assert
        Assert.Equal("test.cs", vm.Name);
        Assert.Equal("/project/src/test.cs", vm.Path);
        Assert.True(vm.IsFile);
        Assert.False(vm.IsDirectory);
        Assert.Equal(2, vm.Depth);
        Assert.False(vm.ChildrenLoaded);
    }

    [Fact]
    public void FromFileSystemItem_CreatesCorrectViewModel_ForDirectory()
    {
        // Arrange
        var item = new FileSystemItem
        {
            Name = "src",
            Path = "/project/src",
            Type = FileSystemItemType.Directory,
            HasChildren = true
        };

        // Act
        var vm = FileTreeItemViewModel.FromFileSystemItem(item, _mockParent.Object);

        // Assert
        Assert.True(vm.IsDirectory);
        Assert.True(vm.HasChildren);
        Assert.True(vm.ShowExpander);
        Assert.Equal(0, vm.Depth);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Extension_ReturnsCorrectValue_ForFile()
    {
        var vm = CreateFileViewModel("test.cs", "/project/test.cs");
        Assert.Equal(".cs", vm.Extension);
    }

    [Fact]
    public void Extension_ReturnsEmpty_ForDirectory()
    {
        var vm = CreateDirectoryViewModel("src", "/project/src");
        Assert.Equal(string.Empty, vm.Extension);
    }

    [Fact]
    public void IndentMargin_CalculatesCorrectly()
    {
        var vm = CreateFileViewModel("test.cs", "/project/test.cs");
        vm.Depth = 3;
        Assert.Equal(48, vm.IndentMargin); // 3 * 16
    }

    [Fact]
    public void ShowExpander_TrueForDirectoryWithChildren()
    {
        var vm = CreateDirectoryViewModel("src", "/project/src");
        vm.HasChildren = true;
        Assert.True(vm.ShowExpander);
    }

    [Fact]
    public void ShowExpander_FalseForEmptyDirectory()
    {
        var vm = CreateDirectoryViewModel("empty", "/project/empty");
        vm.HasChildren = false;
        Assert.False(vm.ShowExpander);
    }

    #endregion

    #region Inline Rename Tests

    [Fact]
    public void BeginRename_SetsRenameState()
    {
        var vm = CreateFileViewModel("test.cs", "/project/test.cs");

        vm.BeginRename();

        Assert.True(vm.IsRenaming);
        Assert.Equal("test.cs", vm.EditingName);
    }

    [Fact]
    public void CancelRename_RevertsState()
    {
        var vm = CreateFileViewModel("test.cs", "/project/test.cs");
        vm.BeginRename();
        vm.EditingName = "newname.cs";

        vm.CancelRenameCommand.Execute(null);

        Assert.False(vm.IsRenaming);
        Assert.Equal("test.cs", vm.EditingName);
    }

    #endregion

    #region Filter Tests

    [Fact]
    public void MatchesFilter_ReturnsTrueForMatch()
    {
        var vm = CreateFileViewModel("TestFile.cs", "/project/TestFile.cs");
        Assert.True(vm.MatchesFilter("test"));
    }

    [Fact]
    public void MatchesFilter_IsCaseInsensitive()
    {
        var vm = CreateFileViewModel("TestFile.cs", "/project/TestFile.cs");
        Assert.True(vm.MatchesFilter("TESTFILE"));
    }

    [Fact]
    public void MatchesFilter_ReturnsTrueForEmptyFilter()
    {
        var vm = CreateFileViewModel("TestFile.cs", "/project/TestFile.cs");
        Assert.True(vm.MatchesFilter(""));
    }

    [Fact]
    public void MatchesFilter_ReturnsFalseForNoMatch()
    {
        var vm = CreateFileViewModel("TestFile.cs", "/project/TestFile.cs");
        Assert.False(vm.MatchesFilter("xyz"));
    }

    [Fact]
    public void ApplyFilter_SetsVisibilityAndHighlight()
    {
        var vm = CreateFileViewModel("TestCard.tsx", "/project/TestCard.tsx");

        vm.ApplyFilter("test");

        Assert.True(vm.IsVisible);
        Assert.True(vm.IsHighlighted);
    }

    [Fact]
    public void ApplyFilter_HidesNonMatchingItems()
    {
        var vm = CreateFileViewModel("Button.tsx", "/project/Button.tsx");

        vm.ApplyFilter("test");

        Assert.False(vm.IsVisible);
        Assert.False(vm.IsHighlighted);
    }

    [Fact]
    public void ClearFilter_ResetsAllState()
    {
        var vm = CreateFileViewModel("TestCard.tsx", "/project/TestCard.tsx");
        vm.ApplyFilter("test");

        vm.ClearFilter();

        Assert.True(vm.IsVisible);
        Assert.False(vm.IsHighlighted);
    }

    #endregion

    #region Icon Tests

    [Fact]
    public void IconKey_ReturnsCorrectKey_ForCSharpFile()
    {
        var vm = CreateFileViewModel("test.cs", "/project/test.cs");
        Assert.Equal("file-csharp", vm.IconKey);
    }

    [Fact]
    public void IconKey_ReturnsFolder_ForDirectory()
    {
        var vm = CreateDirectoryViewModel("utils", "/project/utils");
        vm.IsExpanded = false;
        Assert.Equal("folder", vm.IconKey);
    }

    [Fact]
    public void IconKey_ReturnsFolderOpen_ForExpandedDirectory()
    {
        _mockParent.Setup(p => p.LoadChildrenForItemAsync(It.IsAny<FileTreeItemViewModel>()))
            .ReturnsAsync(new List<FileTreeItemViewModel>());

        var vm = CreateDirectoryViewModel("utils", "/project/utils");
        vm.IsExpanded = true;

        Assert.Equal("folder-open", vm.IconKey);
    }

    [Fact]
    public void IconKey_ReturnsSpecialFolder_ForSrc()
    {
        var vm = CreateDirectoryViewModel("src", "/project/src");
        Assert.Equal("folder-src", vm.IconKey);
    }

    #endregion

    #region FileIconProvider Tests

    [Theory]
    [InlineData(".cs", "file-csharp")]
    [InlineData(".ts", "file-typescript")]
    [InlineData(".py", "file-python")]
    [InlineData(".md", "file-markdown")]
    [InlineData(".json", "file-json")]
    [InlineData(".unknown", "file-code")]
    public void FileIconProvider_ReturnsCorrectKeys(string extension, string expected)
    {
        Assert.Equal(expected, FileIconProvider.GetIconKeyForExtension(extension));
    }

    [Theory]
    [InlineData("README.md", "file-markdown")]
    [InlineData("package.json", "file-json")]
    [InlineData(".gitignore", "file-git")]
    [InlineData("random.txt", null)]
    public void FileIconProvider_IdentifiesSpecialFiles(string fileName, string? expected)
    {
        Assert.Equal(expected, FileIconProvider.GetIconKeyForSpecialFile(fileName));
    }

    #endregion

    #region Helpers

    private FileTreeItemViewModel CreateFileViewModel(string name, string path)
    {
        return new FileTreeItemViewModel(_mockParent.Object)
        {
            Name = name,
            Path = path,
            ItemType = FileSystemItemType.File,
            HasChildren = false
        };
    }

    private FileTreeItemViewModel CreateDirectoryViewModel(string name, string path)
    {
        return new FileTreeItemViewModel(_mockParent.Object)
        {
            Name = name,
            Path = path,
            ItemType = FileSystemItemType.Directory,
            HasChildren = true
        };
    }

    #endregion
}
