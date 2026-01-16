using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF VIEWER VIEWMODEL TESTS (v0.4.2d)                                    │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffViewerViewModelTests
{
    private readonly Mock<IDiffService> _mockDiffService;
    private readonly Mock<IInlineDiffService> _mockInlineDiffService;
    private readonly DiffViewerViewModel _viewModel;

    public DiffViewerViewModelTests()
    {
        _mockDiffService = new Mock<IDiffService>();
        _mockInlineDiffService = new Mock<IInlineDiffService>();
        _mockInlineDiffService
            .Setup(s => s.GetInlineSegments(It.IsAny<string>(), It.IsAny<IReadOnlyList<InlineChange>>(), It.IsAny<DiffSide>()))
            .Returns(new List<InlineSegment>());

        _viewModel = new DiffViewerViewModel(
            _mockDiffService.Object,
            _mockInlineDiffService.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_InitializesDefaults()
    {
        Assert.True(_viewModel.ShowInlineChanges);
        Assert.True(_viewModel.SynchronizedScroll);
        Assert.True(_viewModel.ShowLineNumbers);
        Assert.False(_viewModel.WordWrap);
        Assert.Equal(3, _viewModel.ContextLines);
        Assert.Empty(_viewModel.Hunks);
    }

    [Fact]
    public void Constructor_ThrowsOnNullDiffService()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DiffViewerViewModel(null!, _mockInlineDiffService.Object));
    }

    [Fact]
    public void Constructor_ThrowsOnNullInlineService()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DiffViewerViewModel(_mockDiffService.Object, null!));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LoadDiff Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void LoadDiff_SetsPropertiesCorrectly()
    {
        var result = CreateTestDiffResult();

        _viewModel.LoadDiff(result, "/test/file.cs");

        Assert.Equal(result, _viewModel.DiffResult);
        Assert.Equal("/test/file.cs", _viewModel.FilePath);
        Assert.Equal("file.cs", _viewModel.FileName);
        Assert.Equal(result.Hunks.Count, _viewModel.TotalHunks);
        Assert.Equal(0, _viewModel.CurrentHunkIndex);
    }

    [Fact]
    public void LoadDiff_BuildsHunkViewModels()
    {
        var result = CreateTestDiffResult(hunkCount: 3);

        _viewModel.LoadDiff(result);

        Assert.Equal(3, _viewModel.Hunks.Count);
    }

    [Fact]
    public void LoadDiff_ThrowsOnNullResult()
    {
        Assert.Throws<ArgumentNullException>(() => _viewModel.LoadDiff(null!));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Navigation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanNavigateNext_FalseWhenNoHunks()
    {
        Assert.False(_viewModel.CanNavigateNext);
    }

    [Fact]
    public void CanNavigateNext_TrueWhenNotAtEnd()
    {
        var result = CreateTestDiffResult(hunkCount: 3);
        _viewModel.LoadDiff(result);

        Assert.True(_viewModel.CanNavigateNext);
    }

    [Fact]
    public void CanNavigatePrevious_FalseAtStart()
    {
        var result = CreateTestDiffResult(hunkCount: 3);
        _viewModel.LoadDiff(result);

        Assert.False(_viewModel.CanNavigatePrevious);
    }

    [Fact]
    public void NextHunkCommand_IncreasesIndex()
    {
        var result = CreateTestDiffResult(hunkCount: 3);
        _viewModel.LoadDiff(result);

        _viewModel.NextHunkCommand.Execute(null);

        Assert.Equal(1, _viewModel.CurrentHunkIndex);
    }

    [Fact]
    public void PreviousHunkCommand_DecreasesIndex()
    {
        var result = CreateTestDiffResult(hunkCount: 3);
        _viewModel.LoadDiff(result);
        _viewModel.NextHunkCommand.Execute(null); // Move to index 1

        _viewModel.PreviousHunkCommand.Execute(null);

        Assert.Equal(0, _viewModel.CurrentHunkIndex);
    }

    [Fact]
    public void GoToHunkCommand_SetsIndexDirectly()
    {
        var result = CreateTestDiffResult(hunkCount: 5);
        _viewModel.LoadDiff(result);

        _viewModel.GoToHunkCommand.Execute(3);

        Assert.Equal(3, _viewModel.CurrentHunkIndex);
    }

    [Fact]
    public void FirstHunkCommand_SetsIndexToZero()
    {
        var result = CreateTestDiffResult(hunkCount: 5);
        _viewModel.LoadDiff(result);
        _viewModel.GoToHunkCommand.Execute(3);

        _viewModel.FirstHunkCommand.Execute(null);

        Assert.Equal(0, _viewModel.CurrentHunkIndex);
    }

    [Fact]
    public void LastHunkCommand_SetsIndexToLast()
    {
        var result = CreateTestDiffResult(hunkCount: 5);
        _viewModel.LoadDiff(result);

        _viewModel.LastHunkCommand.Execute(null);

        Assert.Equal(4, _viewModel.CurrentHunkIndex);
    }

    [Fact]
    public void HunkPositionDisplay_ShowsCorrectFormat()
    {
        var result = CreateTestDiffResult(hunkCount: 5);
        _viewModel.LoadDiff(result);

        Assert.Equal("1/5", _viewModel.HunkPositionDisplay);

        _viewModel.NextHunkCommand.Execute(null);
        Assert.Equal("2/5", _viewModel.HunkPositionDisplay);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Toggle Commands Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToggleInlineChangesCommand_TogglesProperty()
    {
        Assert.True(_viewModel.ShowInlineChanges);

        _viewModel.ToggleInlineChangesCommand.Execute(null);
        Assert.False(_viewModel.ShowInlineChanges);

        _viewModel.ToggleInlineChangesCommand.Execute(null);
        Assert.True(_viewModel.ShowInlineChanges);
    }

    [Fact]
    public void ToggleWordWrapCommand_TogglesProperty()
    {
        Assert.False(_viewModel.WordWrap);

        _viewModel.ToggleWordWrapCommand.Execute(null);
        Assert.True(_viewModel.WordWrap);
    }

    [Fact]
    public void ToggleSynchronizedScrollCommand_TogglesProperty()
    {
        Assert.True(_viewModel.SynchronizedScroll);

        _viewModel.ToggleSynchronizedScrollCommand.Execute(null);
        Assert.False(_viewModel.SynchronizedScroll);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HunkNavigationRequested_RaisedOnNavigation()
    {
        var result = CreateTestDiffResult(hunkCount: 3);
        _viewModel.LoadDiff(result);

        int? navigatedIndex = null;
        _viewModel.HunkNavigationRequested += (s, idx) => navigatedIndex = idx;

        _viewModel.NextHunkCommand.Execute(null);

        Assert.NotNull(navigatedIndex);
        Assert.Equal(1, navigatedIndex);
    }

    [Fact]
    public void ApplyRequested_RaisedOnRequestApply()
    {
        var wasRaised = false;
        _viewModel.ApplyRequested += (s, e) => wasRaised = true;

        _viewModel.RequestApplyCommand.Execute(null);

        Assert.True(wasRaised);
    }

    [Fact]
    public void RejectRequested_RaisedOnRequestReject()
    {
        var wasRaised = false;
        _viewModel.RejectRequested += (s, e) => wasRaised = true;

        _viewModel.RequestRejectCommand.Execute(null);

        Assert.True(wasRaised);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Clear Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Clear_ResetsAllState()
    {
        var result = CreateTestDiffResult(hunkCount: 3);
        _viewModel.LoadDiff(result, "/test/file.cs");

        _viewModel.Clear();

        Assert.Null(_viewModel.DiffResult);
        Assert.Empty(_viewModel.FilePath);
        Assert.Empty(_viewModel.FileName);
        Assert.Equal(0, _viewModel.TotalHunks);
        Assert.Empty(_viewModel.Hunks);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void StatsDisplay_ReturnsEmptyWhenNoResult()
    {
        Assert.Equal(string.Empty, _viewModel.StatsDisplay);
    }

    [Fact]
    public void HasChanges_FalseWhenNoResult()
    {
        Assert.False(_viewModel.HasChanges);
    }

    [Fact]
    public void HasChanges_TrueWhenDiffHasChanges()
    {
        var result = CreateTestDiffResult();
        _viewModel.LoadDiff(result);

        Assert.True(_viewModel.HasChanges);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ShowInlineChanges Propagation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ShowInlineChanges_PropagatestoHunks()
    {
        var result = CreateTestDiffResult(hunkCount: 2);
        _viewModel.LoadDiff(result);

        _viewModel.ShowInlineChanges = false;

        Assert.All(_viewModel.Hunks, h => Assert.False(h.ShowInlineChanges));

        _viewModel.ShowInlineChanges = true;

        Assert.All(_viewModel.Hunks, h => Assert.True(h.ShowInlineChanges));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private static DiffResult CreateTestDiffResult(int hunkCount = 1)
    {
        var hunks = new List<DiffHunk>();
        for (int i = 0; i < hunkCount; i++)
        {
            hunks.Add(new DiffHunk
            {
                Id = Guid.NewGuid(),
                Index = i,
                OriginalStartLine = i + 1,
                ProposedStartLine = i + 1,
                OriginalLineCount = 3,
                ProposedLineCount = 4,
                Lines = new List<DiffLine>
                {
                    DiffLine.Unchanged(i + 1, i + 1, "context"),
                    DiffLine.Added(i + 2, "added line")
                }
            });
        }

        return new DiffResult
        {
            OriginalFilePath = "/test/file.cs",
            OriginalContent = "original",
            ProposedContent = "proposed",
            Stats = new DiffStats { AddedLines = hunkCount, RemovedLines = 0 },
            Hunks = hunks
        };
    }
}
