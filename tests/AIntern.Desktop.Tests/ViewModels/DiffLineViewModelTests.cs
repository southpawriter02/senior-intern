using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF LINE VIEWMODEL TESTS (v0.4.2d)                                      │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffLineViewModelTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Placeholder_CreatesPlaceholderLine()
    {
        var line = DiffLineViewModel.Placeholder();

        Assert.True(line.IsPlaceholder);
        Assert.Null(line.LineNumber);
        Assert.Equal(string.Empty, line.Content);
        Assert.Equal(DiffLineType.Unchanged, line.Type);
    }

    [Fact]
    public void FromDiffLine_CreatesCorrectViewModel()
    {
        var diffLine = new DiffLine
        {
            OriginalLineNumber = 10,
            ProposedLineNumber = 12,
            Content = "var x = 5;",
            Type = DiffLineType.Added
        };

        var vm = DiffLineViewModel.FromDiffLine(diffLine, DiffSide.Proposed);

        Assert.Equal(12, vm.LineNumber);
        Assert.Equal("var x = 5;", vm.Content);
        Assert.Equal(DiffLineType.Added, vm.Type);
        Assert.Equal(DiffSide.Proposed, vm.Side);
        Assert.False(vm.IsPlaceholder);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(DiffLineType.Added, true, false, false)]
    [InlineData(DiffLineType.Removed, false, true, false)]
    [InlineData(DiffLineType.Modified, false, false, true)]
    [InlineData(DiffLineType.Unchanged, false, false, false)]
    public void TypeProperties_ReturnCorrectValues(
        DiffLineType type,
        bool expectedIsAdded,
        bool expectedIsRemoved,
        bool expectedIsModified)
    {
        var vm = new DiffLineViewModel { Type = type };

        Assert.Equal(expectedIsAdded, vm.IsAdded);
        Assert.Equal(expectedIsRemoved, vm.IsRemoved);
        Assert.Equal(expectedIsModified, vm.IsModified);
    }

    [Fact]
    public void IsChanged_TrueForChangedTypes()
    {
        var added = new DiffLineViewModel { Type = DiffLineType.Added };
        var removed = new DiffLineViewModel { Type = DiffLineType.Removed };
        var unchanged = new DiffLineViewModel { Type = DiffLineType.Unchanged };

        Assert.True(added.IsChanged);
        Assert.True(removed.IsChanged);
        Assert.False(unchanged.IsChanged);
    }

    [Theory]
    [InlineData(DiffLineType.Added, '+')]
    [InlineData(DiffLineType.Removed, '-')]
    [InlineData(DiffLineType.Modified, '~')]
    [InlineData(DiffLineType.Unchanged, ' ')]
    public void Prefix_ReturnsCorrectCharacter(DiffLineType type, char expected)
    {
        var vm = new DiffLineViewModel { Type = type };
        Assert.Equal(expected, vm.Prefix);
    }

    [Theory]
    [InlineData(DiffLineType.Added, "diff-added")]
    [InlineData(DiffLineType.Removed, "diff-removed")]
    [InlineData(DiffLineType.Modified, "diff-modified")]
    [InlineData(DiffLineType.Unchanged, "diff-unchanged")]
    public void ChangeClass_ReturnsCorrectClass(DiffLineType type, string expected)
    {
        var vm = new DiffLineViewModel { Type = type };
        Assert.Equal(expected, vm.ChangeClass);
    }

    [Fact]
    public void ChangeClass_PlaceholderReturnsPlaceholderClass()
    {
        var vm = new DiffLineViewModel { IsPlaceholder = true };
        Assert.Equal("diff-placeholder", vm.ChangeClass);
    }

    [Fact]
    public void LineNumberDisplay_ReturnsValueOrEmpty()
    {
        var withNumber = new DiffLineViewModel { LineNumber = 42 };
        var withoutNumber = new DiffLineViewModel { LineNumber = null };

        Assert.Equal("42", withNumber.LineNumberDisplay);
        Assert.Equal(string.Empty, withoutNumber.LineNumberDisplay);
    }

    [Fact]
    public void HasInlineSegments_FalseWhenNull()
    {
        var vm = new DiffLineViewModel();
        Assert.False(vm.HasInlineSegments);
    }

    [Fact]
    public void HasInlineSegments_TrueWhenHasSegments()
    {
        var segments = new List<InlineSegment>
        {
            InlineSegment.Unchanged("hello")
        };
        var vm = new DiffLineViewModel { InlineSegments = segments };

        Assert.True(vm.HasInlineSegments);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetSegmentAtColumn Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetSegmentAtColumn_ReturnsNullWhenNoSegments()
    {
        var vm = new DiffLineViewModel();
        Assert.Null(vm.GetSegmentAtColumn(5));
    }

    [Fact]
    public void GetSegmentAtColumn_ReturnsCorrectSegment()
    {
        var segments = new List<InlineSegment>
        {
            InlineSegment.Unchanged("Hello "), // 0-5
            InlineSegment.Added("world")       // 6-10
        };
        var vm = new DiffLineViewModel { InlineSegments = segments };

        var segment0 = vm.GetSegmentAtColumn(0);
        var segment5 = vm.GetSegmentAtColumn(5);
        var segment6 = vm.GetSegmentAtColumn(6);

        Assert.NotNull(segment0);
        Assert.Equal("Hello ", segment0!.Text);
        Assert.NotNull(segment5);
        Assert.Equal("Hello ", segment5!.Text);
        Assert.NotNull(segment6);
        Assert.Equal("world", segment6!.Text);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToString_PlaceholderReturnsPlaceholderText()
    {
        var vm = DiffLineViewModel.Placeholder();
        Assert.Equal("[Placeholder]", vm.ToString());
    }

    [Fact]
    public void ToString_ReturnsFormattedLine()
    {
        var vm = new DiffLineViewModel
        {
            LineNumber = 10,
            Content = "test content",
            Type = DiffLineType.Added
        };

        Assert.Equal("[10] + test content", vm.ToString());
    }

    [Fact]
    public void ToString_TruncatesLongContent()
    {
        var longContent = new string('x', 100);
        var vm = new DiffLineViewModel
        {
            LineNumber = 1,
            Content = longContent,
            Type = DiffLineType.Unchanged
        };

        var result = vm.ToString();
        Assert.Contains("...", result);
        Assert.True(result.Length < 100);
    }
}
