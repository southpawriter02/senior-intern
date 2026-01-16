namespace AIntern.Core.Tests.Models;

using AIntern.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="LineRange"/> (v0.4.5c extensions).
/// </summary>
public class LineRangeExtendedTests
{
    [Fact]
    public void FromLine_CreatesToMaxValue()
    {
        var range = LineRange.FromLine(5);
        Assert.Equal(5, range.StartLine);
        Assert.Equal(int.MaxValue, range.EndLine);
    }

    [Fact]
    public void EntireFile_CreatesFullRange()
    {
        var range = LineRange.EntireFile(100);
        Assert.Equal(1, range.StartLine);
        Assert.Equal(100, range.EndLine);
    }

    [Fact]
    public void Merge_CombinesTwoRanges()
    {
        var a = new LineRange(5, 10);
        var b = new LineRange(15, 20);
        var merged = a.Merge(b);
        Assert.Equal(5, merged.StartLine);
        Assert.Equal(20, merged.EndLine);
    }

    [Fact]
    public void Intersect_ReturnsOverlap()
    {
        var a = new LineRange(5, 15);
        var b = new LineRange(10, 20);
        var intersection = a.Intersect(b);
        Assert.Equal(10, intersection.StartLine);
        Assert.Equal(15, intersection.EndLine);
    }

    [Fact]
    public void Intersect_NoOverlap_ReturnsEmpty()
    {
        var a = new LineRange(5, 10);
        var b = new LineRange(15, 20);
        var intersection = a.Intersect(b);
        Assert.True(intersection.IsEmpty);
    }

    [Fact]
    public void Expand_ExtendsRange()
    {
        var range = new LineRange(10, 20);
        var expanded = range.Expand(5, 10);
        Assert.Equal(5, expanded.StartLine);
        Assert.Equal(30, expanded.EndLine);
    }

    [Fact]
    public void Shift_MovesRange()
    {
        var range = new LineRange(10, 20);
        var shifted = range.Shift(5);
        Assert.Equal(15, shifted.StartLine);
        Assert.Equal(25, shifted.EndLine);
    }

    [Fact]
    public void ClampTo_RestrictsRange()
    {
        var range = new LineRange(5, 50);
        var clamped = range.ClampTo(30);
        Assert.Equal(5, clamped.StartLine);
        Assert.Equal(30, clamped.EndLine);
    }

    [Fact]
    public void EnumerateLines_ReturnsAllLines()
    {
        var range = new LineRange(5, 8);
        var lines = range.EnumerateLines().ToList();
        Assert.Equal([5, 6, 7, 8], lines);
    }

    [Fact]
    public void ToDiffHeader_SingleLine_ReturnsNumber()
    {
        var range = LineRange.SingleLine(42);
        Assert.Equal("42", range.ToDiffHeader());
    }

    [Fact]
    public void ToDiffHeader_MultiLine_ReturnsRange()
    {
        var range = new LineRange(10, 15);
        Assert.Equal("10,6", range.ToDiffHeader());
    }
}
