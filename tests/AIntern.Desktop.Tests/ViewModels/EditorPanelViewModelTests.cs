using Xunit;
using NSubstitute;
using AIntern.Core.Interfaces;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

public class EditorPanelViewModelTests
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IDialogService _dialogService;
    private readonly EditorPanelViewModel _viewModel;

    public EditorPanelViewModelTests()
    {
        _fileSystemService = Substitute.For<IFileSystemService>();
        _dialogService = Substitute.For<IDialogService>();
        _viewModel = new EditorPanelViewModel(_fileSystemService, _dialogService);
    }

    #region OpenFileAsync Tests

    [Fact]
    public async Task OpenFileAsync_CreatesNewTab()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");

        await _viewModel.OpenFileAsync("/test.txt");

        Assert.Single(_viewModel.Tabs);
        Assert.Equal("/test.txt", _viewModel.ActiveTab?.FilePath);
    }

    [Fact]
    public async Task OpenFileAsync_ActivatesExistingTab()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");

        await _viewModel.OpenFileAsync("/test.txt");
        await _viewModel.OpenFileAsync("/test.txt");

        Assert.Single(_viewModel.Tabs); // Still only one tab
    }

    [Fact]
    public async Task OpenFileAsync_ShowsErrorOnFailure()
    {
        _fileSystemService.ReadFileAsync("/fail.txt", Arg.Any<CancellationToken>())
            .Returns<string>(x => throw new IOException("File not found"));

        await _viewModel.OpenFileAsync("/fail.txt");

        await _dialogService.Received(1).ShowErrorAsync(
            "Error Opening File",
            Arg.Any<string>());
        Assert.Empty(_viewModel.Tabs);
    }

    #endregion

    #region NewFile Tests

    [Fact]
    public void NewFile_IncrementCounter()
    {
        _viewModel.NewFile();
        _viewModel.NewFile();

        Assert.Equal(2, _viewModel.Tabs.Count);
        Assert.Equal("Untitled-1", _viewModel.Tabs[0].FileName);
        Assert.Equal("Untitled-2", _viewModel.Tabs[1].FileName);
    }

    [Fact]
    public void NewFile_ActivatesNewTab()
    {
        _viewModel.NewFile();

        Assert.NotNull(_viewModel.ActiveTab);
        Assert.True(_viewModel.ActiveTab.IsNewFile);
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_SavesExistingFile()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        _viewModel.ActiveTab!.Document.Text = "modified";

        await _viewModel.SaveAsync();

        await _fileSystemService.Received(1).WriteFileAsync(
            "/test.txt", "modified", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_PromptsForPathOnNewFile()
    {
        _dialogService.ShowSaveDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string[])>>())
            .Returns("/new/path.txt");

        _viewModel.NewFile();

        await _viewModel.SaveAsync();

        await _dialogService.Received(1).ShowSaveDialogAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string[])>>());
    }

    [Fact]
    public async Task SaveAsync_ShowsErrorOnFailure()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        _viewModel.ActiveTab!.Document.Text = "modified";
        
        _fileSystemService.WriteFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new IOException("Write failed"));

        await _viewModel.SaveAsync();

        await _dialogService.Received(1).ShowErrorAsync(
            "Error Saving File", Arg.Any<string>());
    }

    #endregion

    #region SaveAsAsync Tests

    [Fact]
    public async Task SaveAsAsync_SavesWithNewPath()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        
        _dialogService.ShowSaveDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string[])>>())
            .Returns("/new/path.txt");

        await _viewModel.SaveAsAsync();

        await _fileSystemService.Received(1).WriteFileAsync(
            "/new/path.txt", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsAsync_CancelDoesNothing()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        
        _dialogService.ShowSaveDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string[])>>())
            .Returns((string?)null);

        await _viewModel.SaveAsAsync();

        await _fileSystemService.DidNotReceive().WriteFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region SaveAllAsync Tests

    [Fact]
    public async Task SaveAllAsync_SavesAllDirtyTabs()
    {
        _fileSystemService.ReadFileAsync("/a.txt", Arg.Any<CancellationToken>()).Returns("a");
        _fileSystemService.ReadFileAsync("/b.txt", Arg.Any<CancellationToken>()).Returns("b");
        
        await _viewModel.OpenFileAsync("/a.txt");
        await _viewModel.OpenFileAsync("/b.txt");
        _viewModel.Tabs[0].Document.Text = "modified-a";
        _viewModel.Tabs[1].Document.Text = "modified-b";

        await _viewModel.SaveAllAsync();

        await _fileSystemService.Received(1).WriteFileAsync("/a.txt", "modified-a", Arg.Any<CancellationToken>());
        await _fileSystemService.Received(1).WriteFileAsync("/b.txt", "modified-b", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAllAsync_SkipsCleanTabs()
    {
        _fileSystemService.ReadFileAsync("/a.txt", Arg.Any<CancellationToken>()).Returns("a");
        await _viewModel.OpenFileAsync("/a.txt");

        await _viewModel.SaveAllAsync();

        await _fileSystemService.DidNotReceive().WriteFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region CloseTabAsync Tests

    [Fact]
    public async Task CloseTabAsync_ClosesCleanTab()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        var tab = _viewModel.ActiveTab!;

        await _viewModel.CloseTabAsync(tab);

        Assert.Empty(_viewModel.Tabs);
    }

    [Fact]
    public async Task CloseTabAsync_PromptsSaveForDirtyTab()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        _viewModel.ActiveTab!.Document.Text = "modified";
        var tab = _viewModel.ActiveTab;

        _dialogService.ShowConfirmDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns("Don't Save");

        await _viewModel.CloseTabAsync(tab);

        Assert.Empty(_viewModel.Tabs);
    }

    [Fact]
    public async Task CloseTabAsync_CancelKeepsTabOpen()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        _viewModel.ActiveTab!.Document.Text = "modified";
        var tab = _viewModel.ActiveTab;

        _dialogService.ShowConfirmDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns("Cancel");

        await _viewModel.CloseTabAsync(tab);

        Assert.Single(_viewModel.Tabs);
    }

    #endregion

    #region CloseAllTabsAsync Tests

    [Fact]
    public async Task CloseAllTabsAsync_ClosesAllCleanTabs()
    {
        _fileSystemService.ReadFileAsync("/a.txt", Arg.Any<CancellationToken>()).Returns("a");
        _fileSystemService.ReadFileAsync("/b.txt", Arg.Any<CancellationToken>()).Returns("b");
        await _viewModel.OpenFileAsync("/a.txt");
        await _viewModel.OpenFileAsync("/b.txt");

        await _viewModel.CloseAllTabsAsync();

        Assert.Empty(_viewModel.Tabs);
    }

    [Fact]
    public async Task CloseAllTabsAsync_StopsOnCancel()
    {
        _fileSystemService.ReadFileAsync("/a.txt", Arg.Any<CancellationToken>()).Returns("a");
        _fileSystemService.ReadFileAsync("/b.txt", Arg.Any<CancellationToken>()).Returns("b");
        await _viewModel.OpenFileAsync("/a.txt");
        await _viewModel.OpenFileAsync("/b.txt");
        _viewModel.Tabs[1].Document.Text = "modified"; // b.txt is dirty (last tab)

        _dialogService.ShowConfirmDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns("Cancel");

        await _viewModel.CloseAllTabsAsync();

        // Close iterates back-to-front, so cancel on b.txt means nothing is closed
        Assert.Equal(2, _viewModel.Tabs.Count);
    }

    #endregion

    #region CloseOtherTabsAsync Tests

    [Fact]
    public async Task CloseOtherTabsAsync_KeepsCorrectTab()
    {
        _fileSystemService.ReadFileAsync("/a.txt", Arg.Any<CancellationToken>()).Returns("a");
        _fileSystemService.ReadFileAsync("/b.txt", Arg.Any<CancellationToken>()).Returns("b");
        _fileSystemService.ReadFileAsync("/c.txt", Arg.Any<CancellationToken>()).Returns("c");
        await _viewModel.OpenFileAsync("/a.txt");
        await _viewModel.OpenFileAsync("/b.txt");
        await _viewModel.OpenFileAsync("/c.txt");
        var keepTab = _viewModel.Tabs[1];

        await _viewModel.CloseOtherTabsAsync(keepTab);

        Assert.Single(_viewModel.Tabs);
        Assert.Equal("/b.txt", _viewModel.Tabs[0].FilePath);
    }

    [Fact]
    public async Task CloseOtherTabsAsync_SetActiveTab()
    {
        _fileSystemService.ReadFileAsync("/a.txt", Arg.Any<CancellationToken>()).Returns("a");
        _fileSystemService.ReadFileAsync("/b.txt", Arg.Any<CancellationToken>()).Returns("b");
        await _viewModel.OpenFileAsync("/a.txt");
        await _viewModel.OpenFileAsync("/b.txt");
        var keepTab = _viewModel.Tabs[0];

        await _viewModel.CloseOtherTabsAsync(keepTab);

        Assert.Equal(keepTab, _viewModel.ActiveTab);
    }

    #endregion

    #region NextTab/PreviousTab Tests

    [Fact]
    public void NextTab_WrapsAround()
    {
        _viewModel.NewFile();
        _viewModel.NewFile();
        _viewModel.NewFile();
        _viewModel.ActiveTab = _viewModel.Tabs[2]; // Last tab

        _viewModel.NextTab();

        Assert.Equal(_viewModel.Tabs[0], _viewModel.ActiveTab);
    }

    [Fact]
    public void PreviousTab_WrapsAround()
    {
        _viewModel.NewFile();
        _viewModel.NewFile();
        _viewModel.ActiveTab = _viewModel.Tabs[0]; // First tab

        _viewModel.PreviousTab();

        Assert.Equal(_viewModel.Tabs[1], _viewModel.ActiveTab);
    }

    [Fact]
    public void NextTab_NoOpWithSingleTab()
    {
        _viewModel.NewFile();

        _viewModel.NextTab();

        Assert.Equal(_viewModel.Tabs[0], _viewModel.ActiveTab);
    }

    #endregion

    #region MoveTab Tests

    [Fact]
    public void MoveTab_ReordersTabs()
    {
        _viewModel.NewFile();
        _viewModel.NewFile();
        _viewModel.NewFile();
        var tab = _viewModel.Tabs[0];

        _viewModel.MoveTab(0, 2);

        Assert.Equal(tab, _viewModel.Tabs[2]);
    }

    [Fact]
    public void MoveTab_IgnoresOutOfBounds()
    {
        _viewModel.NewFile();
        _viewModel.NewFile();

        _viewModel.MoveTab(-1, 0);
        _viewModel.MoveTab(0, 10);

        Assert.Equal(2, _viewModel.Tabs.Count);
    }

    #endregion

    #region PromptSaveChangesAsync Tests

    [Fact]
    public async Task PromptSaveChangesAsync_ReturnsTrueForCleanTab()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");

        var result = await _viewModel.PromptSaveChangesAsync(_viewModel.ActiveTab!);

        Assert.True(result);
        await _dialogService.DidNotReceive().ShowConfirmDialogAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>());
    }

    [Fact]
    public async Task PromptSaveChangesAsync_SaveOption_SavesAndReturnsTrue()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        _viewModel.ActiveTab!.Document.Text = "modified";

        _dialogService.ShowConfirmDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns("Save");

        var result = await _viewModel.PromptSaveChangesAsync(_viewModel.ActiveTab);

        Assert.True(result);
        await _fileSystemService.Received(1).WriteFileAsync(
            "/test.txt", "modified", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PromptSaveChangesAsync_DontSaveOption_ReturnsTrueWithoutSaving()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        _viewModel.ActiveTab!.Document.Text = "modified";

        _dialogService.ShowConfirmDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns("Don't Save");

        var result = await _viewModel.PromptSaveChangesAsync(_viewModel.ActiveTab);

        Assert.True(result);
        await _fileSystemService.DidNotReceive().WriteFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region CanCloseAsync Tests

    [Fact]
    public async Task CanCloseAsync_ReturnsTrueForAllClean()
    {
        _fileSystemService.ReadFileAsync("/a.txt", Arg.Any<CancellationToken>()).Returns("a");
        await _viewModel.OpenFileAsync("/a.txt");

        var result = await _viewModel.CanCloseAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task CanCloseAsync_ReturnsFalseOnCancel()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");
        _viewModel.ActiveTab!.Document.Text = "modified";

        _dialogService.ShowConfirmDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns("Cancel");

        var result = await _viewModel.CanCloseAsync();

        Assert.False(result);
    }

    #endregion

    #region GetTabByPath Tests

    [Fact]
    public async Task GetTabByPath_ReturnsTab()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");

        var tab = _viewModel.GetTabByPath("/test.txt");

        Assert.NotNull(tab);
    }

    [Fact]
    public void GetTabByPath_ReturnsNullIfNotFound()
    {
        var tab = _viewModel.GetTabByPath("/nonexistent.txt");

        Assert.Null(tab);
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public async Task HasUnsavedChanges_ReflectsDirtyState()
    {
        _fileSystemService.ReadFileAsync("/test.txt", Arg.Any<CancellationToken>())
            .Returns("content");
        await _viewModel.OpenFileAsync("/test.txt");

        Assert.False(_viewModel.HasUnsavedChanges);

        _viewModel.ActiveTab!.Document.Text = "modified";
        // Force property update check through collection change
        _viewModel.NewFile();
        _viewModel.Tabs.RemoveAt(1);

        Assert.True(_viewModel.HasUnsavedChanges);
    }

    [Fact]
    public void HasOpenTabs_ReflectsTabCount()
    {
        Assert.False(_viewModel.HasOpenTabs);

        _viewModel.NewFile();

        Assert.True(_viewModel.HasOpenTabs);
    }

    #endregion
}
