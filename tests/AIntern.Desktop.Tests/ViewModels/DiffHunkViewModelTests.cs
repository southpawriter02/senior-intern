using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF HUNK VIEWMODEL TESTS (v0.4.2d)                                      │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffHunkViewModelTests
{
    private readonly Mock<IInlineDiffService> _mockInlineDiffService;

    public DiffHunkViewModelTests()
    {
        _mockInlineDiffService = new Mock<IInlineDiffService>();
        _mockInlineDiffService
            .Setup(s => s.GetInlineSegments(It.IsAny<string>(), It.IsAny<IReadOnlyList<InlineChange>>(), It.IsAny<DiffSide>()))
            .Returns(new List<InlineSegment>());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_CopiesHunkProperties()
    {
        var hunk = CreateTestHunk();

        var vm = new DiffHunkViewModel(hunk, _mockInlineDiffService.Object, true);

        Assert.Equal(hunk.Id, vm.Id);
        Assert.Equal(hunk.Index, vm.Index);
        Assert.Equal(hunk.Header, vm.Header);
        Assert.Equal(hunk.ContextHeader, vm.ContextHeader);
        Assert.Equal(hunk.OriginalStartLine, vm.OriginalStartLine);
        Assert.Equal(hunk.ProposedStartLine, vm.ProposedStartLine);
    }

    [Fact]
    public void Constructor_CalculatesStatistics()
    {
        var lines = new List<DiffLine>
        {
            DiffLine.Added(1, "added1"),
            DiffLine.Added(2, "added2"),
            DiffLine.Removed(1, "removed1"),
            DiffLine.Unchanged(3, 3, "unchanged")
        };
        var hunk = CreateTestHunk(lines);

        var vm = new DiffHunkViewModel(hunk, _mockInlineDiffService.Object, true);

        Assert.Equal(2, vm.AddedCount);
        Assert.Equal(1, vm.RemovedCount);
        Assert.Equal(3, vm.TotalChanges);
    }

    [Fact]
    public void Constructor_ThrowsOnNullHunk()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DiffHunkViewModel(null!, _mockInlineDiffService.Object, true));
    }

    [Fact]
    public void Constructor_ThrowsOnNullInlineService()
    {
        var hunk = CreateTestHunk();
        Assert.Throws<ArgumentNullException>(() =>
            new DiffHunkViewModel(hunk, null!, true));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Side-by-Side Building Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildSideBySideLines_UnchangedLinesAddedToBothSides()
    {
        var lines = new List<DiffLine>
        {
            DiffLine.Unchanged(1, 1, "line1"),
            DiffLine.Unchanged(2, 2, "line2")
        };
        var hunk = CreateTestHunk(lines);

        var vm = new DiffHunkViewModel(hunk, _mockInlineDiffService.Object, true);

        Assert.Equal(2, vm.OriginalLines.Count);
        Assert.Equal(2, vm.ProposedLines.Count);
        Assert.All(vm.OriginalLines, l => Assert.False(l.IsPlaceholder));
        Assert.All(vm.ProposedLines, l => Assert.False(l.IsPlaceholder));
    }

    [Fact]
    public void BuildSideBySideLines_RemovedLineCreatesPlaceholder()
    {
        var lines = new List<DiffLine>
        {
            DiffLine.Removed(1, "removed line")
        };
        var hunk = CreateTestHunk(lines);

        var vm = new DiffHunkViewModel(hunk, _mockInlineDiffService.Object, true);

        Assert.Single(vm.OriginalLines);
        Assert.Single(vm.ProposedLines);
        Assert.False(vm.OriginalLines[0].IsPlaceholder);
        Assert.True(vm.ProposedLines[0].IsPlaceholder);
    }

    [Fact]
    public void BuildSideBySideLines_AddedLineCreatesPlaceholder()
    {
        var lines = new List<DiffLine>
        {
            DiffLine.Added(1, "added line")
        };
        var hunk = CreateTestHunk(lines);

        var vm = new DiffHunkViewModel(hunk, _mockInlineDiffService.Object, true);

        Assert.Single(vm.OriginalLines);
        Assert.Single(vm.ProposedLines);
        Assert.True(vm.OriginalLines[0].IsPlaceholder);
        Assert.False(vm.ProposedLines[0].IsPlaceholder);
    }

    [Fact]
    public void BuildSideBySideLines_ListsHaveSameLength()
    {
        var lines = new List<DiffLine>
        {
            DiffLine.Removed(1, "removed1"),
            DiffLine.Removed(2, "removed2"),
            DiffLine.Added(1, "added1")
        };
        var hunk = CreateTestHunk(lines);

        var vm = new DiffHunkViewModel(hunk, _mockInlineDiffService.Object, true);

        Assert.Equal(vm.OriginalLines.Count, vm.ProposedLines.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Property Change Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ShowInlineChanges_PropertyChangedTriggered()
    {
        var hunk = CreateTestHunk();
        var vm = new DiffHunkViewModel(hunk, _mockInlineDiffService.Object, true);

        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.ShowInlineChanges))
                propertyChanged = true;
        };

        vm.ShowInlineChanges = false;

        Assert.True(propertyChanged);
    }

    [Fact]
    public void ToggleExpandedCommand_TogglesIsExpanded()
    {
        var hunk = CreateTestHunk();
        var vm = new DiffHunkViewModel(hunk, _mockInlineDiffService.Object, true);

        Assert.True(vm.IsExpanded); // default

        vm.ToggleExpandedCommand.Execute(null);
        Assert.False(vm.IsExpanded);

        vm.ToggleExpandedCommand.Execute(null);
        Assert.True(vm.IsExpanded);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private static DiffHunk CreateTestHunk(List<DiffLine>? lines = null)
    {
        return new DiffHunk
        {
            Id = Guid.NewGuid(),
            Index = 0,
            ContextHeader = "function test()",
            OriginalStartLine = 1,
            ProposedStartLine = 1,
            OriginalLineCount = 5,
            ProposedLineCount = 7,
            Lines = lines ?? new List<DiffLine>
            {
                DiffLine.Unchanged(1, 1, "context line")
            }
        };
    }
}
