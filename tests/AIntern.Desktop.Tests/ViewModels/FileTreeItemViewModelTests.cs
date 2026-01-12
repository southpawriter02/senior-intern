using Xunit;
using NSubstitute;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for FileTreeItemViewModel (v0.3.2a).
/// </summary>
public class FileTreeItemViewModelTests
{
    private readonly IFileExplorerParent _mockParent;

    public FileTreeItemViewModelTests()
    {
        _mockParent = Substitute.For<IFileExplorerParent>();
        _mockParent.GetRelativePath(Arg.Any<string>()).Returns(x => x.Arg<string>());
    }

    #region Factory Tests

    [Fact]
    public void FromFileSystemItem_CreatesCorrectViewModel()
    {
        var item = new FileSystemItem
        {
            Name = "test.cs",
            Path = "/project/src/test.cs",
            Type = FileSystemItemType.File,
            HasChildren = false
        };

        var vm = FileTreeItemViewModel.FromFileSystemItem(item, _mockParent, depth: 2);

        Assert.Equal("test.cs", vm.Name);
        Assert.Equal("/project/src/test.cs", vm.Path);
        Assert.True(vm.IsFile);
        Assert.False(vm.IsDirectory);
        Assert.Equal(2, vm.Depth);
        Assert.False(vm.ChildrenLoaded);
    }

    [Fact]
    public void FromFileSystemItem_SetsHasChildrenCorrectly()
    {
        var item = new FileSystemItem
        {
            Name = "src",
            Path = "/project/src",
            Type = FileSystemItemType.Directory,
            HasChildren = true
        };

        var vm = FileTreeItemViewModel.FromFileSystemItem(item, _mockParent);

        Assert.True(vm.IsDirectory);
        Assert.True(vm.HasChildren);
        Assert.True(vm.ShowExpander);
    }

    [Fact]
    public void FromFileSystemItem_ThrowsForNullItem()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FileTreeItemViewModel.FromFileSystemItem(null!, _mockParent));
    }

    #endregion

    #region Lazy Loading Tests

    [Fact]
    public async Task IsExpanded_TriggersLoadChildren_WhenNotLoaded()
    {
        _mockParent.LoadChildrenForItemAsync(Arg.Any<FileTreeItemViewModel>())
            .Returns(Task.FromResult<IReadOnlyList<FileTreeItemViewModel>>([]));

        var vm = CreateFolderViewModel();

        vm.IsExpanded = true;
        await Task.Delay(100); // Allow async to complete

        await _mockParent.Received(1).LoadChildrenForItemAsync(vm);
    }

    [Fact]
    public async Task IsExpanded_DoesNotReload_WhenAlreadyLoaded()
    {
        _mockParent.LoadChildrenForItemAsync(Arg.Any<FileTreeItemViewModel>())
            .Returns(Task.FromResult<IReadOnlyList<FileTreeItemViewModel>>([]));

        var vm = CreateFolderViewModel();
        vm.IsExpanded = true;
        await Task.Delay(100);

        // Collapse and re-expand
        vm.IsExpanded = false;
        _mockParent.ClearReceivedCalls();
        vm.IsExpanded = true;
        await Task.Delay(100);

        await _mockParent.DidNotReceive().LoadChildrenForItemAsync(Arg.Any<FileTreeItemViewModel>());
    }

    [Fact]
    public async Task RefreshChildrenAsync_ForcesReload()
    {
        _mockParent.LoadChildrenForItemAsync(Arg.Any<FileTreeItemViewModel>())
            .Returns(Task.FromResult<IReadOnlyList<FileTreeItemViewModel>>([]));

        var vm = CreateFolderViewModel();
        vm.IsExpanded = true;
        await Task.Delay(100);
        _mockParent.ClearReceivedCalls();

        await vm.RefreshChildrenAsync();

        await _mockParent.Received(1).LoadChildrenForItemAsync(vm);
    }

    [Fact]
    public void InvalidateChildren_ClearsLoadedState()
    {
        var vm = CreateFolderViewModel();
        
        vm.InvalidateChildren();

        Assert.False(vm.ChildrenLoaded);
        Assert.Empty(vm.Children);
    }

    #endregion

    #region Inline Rename Tests

    [Fact]
    public void BeginRename_SetsRenameState()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = "test.cs" };

        vm.BeginRename();

        Assert.True(vm.IsRenaming);
        Assert.Equal("test.cs", vm.EditingName);
    }

    [Fact]
    public async Task CommitRenameAsync_RenamesSuccessfully()
    {
        var vm = new FileTreeItemViewModel(_mockParent)
        {
            Name = "old.cs",
            Path = "/project/old.cs"
        };
        vm.BeginRename();
        vm.EditingName = "new.cs";

        await vm.CommitRenameAsync();

        await _mockParent.Received(1).RenameItemAsync(vm, "new.cs");
        Assert.False(vm.IsRenaming);
        Assert.Equal("new.cs", vm.Name);
    }

    [Fact]
    public async Task CommitRenameAsync_CancelsWhenEmpty()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = "test.cs" };
        vm.BeginRename();
        vm.EditingName = "   ";

        await vm.CommitRenameAsync();

        await _mockParent.DidNotReceive().RenameItemAsync(Arg.Any<FileTreeItemViewModel>(), Arg.Any<string>());
        Assert.False(vm.IsRenaming);
    }

    [Fact]
    public async Task CommitRenameAsync_CancelsWhenSameName()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = "test.cs" };
        vm.BeginRename();
        vm.EditingName = "test.cs";

        await vm.CommitRenameAsync();

        await _mockParent.DidNotReceive().RenameItemAsync(Arg.Any<FileTreeItemViewModel>(), Arg.Any<string>());
        Assert.False(vm.IsRenaming);
    }

    [Fact]
    public async Task CommitRenameAsync_RejectsInvalidCharacters()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = "test.cs" };
        vm.BeginRename();
        vm.EditingName = "test/invalid.cs"; // Contains path separator

        await vm.CommitRenameAsync();

        _mockParent.Received(1).ShowError(Arg.Any<string>());
        Assert.True(vm.IsRenaming); // Should stay in rename mode
    }

    [Fact]
    public void CancelRename_RevertsState()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = "original.cs" };
        vm.BeginRename();
        vm.EditingName = "changed.cs";

        vm.CancelRename();

        Assert.False(vm.IsRenaming);
        Assert.Equal("original.cs", vm.EditingName);
    }

    #endregion

    #region Filter Tests

    [Theory]
    [InlineData("test", "TestFile.cs", true)]
    [InlineData("TEST", "testfile.cs", true)]  // Case insensitive
    [InlineData("xyz", "TestFile.cs", false)]
    [InlineData("", "TestFile.cs", true)]       // Empty filter matches all
    public void MatchesFilter_ReturnsCorrectResult(string filter, string name, bool expected)
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = name };

        Assert.Equal(expected, vm.MatchesFilter(filter));
    }

    [Fact]
    public void ApplyFilter_SetsVisibilityCorrectly()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = "TestFile.cs" };

        vm.ApplyFilter("test");

        Assert.True(vm.IsVisible);
        Assert.True(vm.IsHighlighted);
    }

    [Fact]
    public void ApplyFilter_HidesNonMatchingItems()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = "Other.cs" };

        vm.ApplyFilter("test");

        Assert.False(vm.IsVisible);
        Assert.False(vm.IsHighlighted);
    }

    [Fact]
    public void ApplyFilter_ShowsParentWhenChildMatches()
    {
        var parent = CreateFolderViewModel("src");
        var child = new FileTreeItemViewModel(_mockParent) { Name = "match.cs" };
        parent.Children.Add(child);

        parent.ApplyFilter("match");

        Assert.True(parent.IsVisible);
        Assert.False(parent.IsHighlighted); // Parent doesn't match
        Assert.True(child.IsVisible);
        Assert.True(child.IsHighlighted);   // Child matches
    }

    [Fact]
    public void ApplyFilter_AutoExpandsWhenChildMatches()
    {
        var parent = CreateFolderViewModel("src");
        var child = new FileTreeItemViewModel(_mockParent) { Name = "match.cs" };
        parent.Children.Add(child);

        parent.ApplyFilter("match");

        Assert.True(parent.IsExpanded);
    }

    [Fact]
    public void ClearFilter_ResetsAllState()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Name = "test.cs" };
        vm.ApplyFilter("xyz"); // Would hide it

        vm.ClearFilter();

        Assert.True(vm.IsVisible);
        Assert.False(vm.IsHighlighted);
    }

    #endregion

    #region Icon Tests

    [Theory]
    [InlineData("src", false, "folder-src")]
    [InlineData("src", true, "folder-src-open")]
    [InlineData("test", false, "folder-test")]
    [InlineData("docs", false, "folder-docs")]
    [InlineData("random", false, "folder")]
    [InlineData("random", true, "folder-open")]
    public void IconKey_ReturnsCorrectKey_ForDirectories(string name, bool expanded, string expected)
    {
        var vm = new FileTreeItemViewModel(_mockParent)
        {
            Name = name,
            ItemType = FileSystemItemType.Directory
        };
        if (expanded)
        {
            // Manually set without triggering the partial method
            typeof(FileTreeItemViewModel)
                .GetField("_isExpanded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(vm, true);
        }

        Assert.Equal(expected, vm.IconKey);
    }

    [Theory]
    [InlineData("test.cs", "file-csharp")]
    [InlineData("app.tsx", "file-typescript-react")]
    [InlineData("readme.md", "file-markdown")]
    [InlineData("config.json", "file-json")]
    [InlineData("unknown.xyz", "file-code")]
    public void IconKey_ReturnsCorrectKey_ForFiles(string name, string expected)
    {
        var vm = new FileTreeItemViewModel(_mockParent)
        {
            Name = name,
            Path = $"/project/{name}",
            ItemType = FileSystemItemType.File
        };

        Assert.Equal(expected, vm.IconKey);
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void Extension_ReturnsCorrectExtension()
    {
        var vm = new FileTreeItemViewModel(_mockParent)
        {
            Name = "test.cs",
            Path = "/project/test.cs",
            ItemType = FileSystemItemType.File
        };

        Assert.Equal(".cs", vm.Extension);
    }

    [Fact]
    public void ShowExpander_TrueForDirectoryWithChildren()
    {
        var vm = new FileTreeItemViewModel(_mockParent)
        {
            ItemType = FileSystemItemType.Directory,
            HasChildren = true
        };

        Assert.True(vm.ShowExpander);
    }

    [Fact]
    public void IndentMargin_CalculatesCorrectly()
    {
        var vm = new FileTreeItemViewModel(_mockParent) { Depth = 3 };

        Assert.Equal(48, vm.IndentMargin); // 3 * 16
    }

    #endregion

    #region Helper Methods

    private FileTreeItemViewModel CreateFolderViewModel(string name = "folder")
    {
        return new FileTreeItemViewModel(_mockParent)
        {
            Name = name,
            Path = $"/project/{name}",
            ItemType = FileSystemItemType.Directory,
            HasChildren = true
        };
    }

    #endregion
}
