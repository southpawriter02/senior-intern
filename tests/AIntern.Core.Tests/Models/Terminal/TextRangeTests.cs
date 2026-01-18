using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TextRange"/>.
/// </summary>
/// <remarks>Added in v0.5.4a.</remarks>
public sealed class TextRangeTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Length Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Length_CalculatesCorrectly()
    {
        // Arrange
        var range = new TextRange(5, 15);

        // Act & Assert
        Assert.Equal(10, range.Length);
    }

    [Fact]
    public void Length_IsZeroForEmptyRange()
    {
        // Arrange
        var range = new TextRange(10, 10);

        // Act & Assert
        Assert.Equal(0, range.Length);
        Assert.True(range.IsEmpty);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsValid Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0, 10, true)]
    [InlineData(5, 5, true)]
    [InlineData(0, 0, true)]
    [InlineData(-1, 5, false)]
    [InlineData(10, 5, false)]
    [InlineData(-5, -3, false)]
    public void IsValid_ReturnsExpectedValue(int start, int end, bool expected)
    {
        // Arrange
        var range = new TextRange(start, end);

        // Act & Assert
        Assert.Equal(expected, range.IsValid);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Extract Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_ReturnsCorrectSubstring()
    {
        // Arrange
        var range = new TextRange(7, 12);
        var source = "Hello, World!";

        // Act
        var result = range.Extract(source);

        // Assert
        Assert.Equal("World", result);
    }

    [Fact]
    public void Extract_ClampsEndToSourceLength()
    {
        // Arrange
        var range = new TextRange(7, 100); // End exceeds source length
        var source = "Hello, World!";

        // Act
        var result = range.Extract(source);

        // Assert
        Assert.Equal("World!", result);
    }

    [Fact]
    public void Extract_ReturnsEmptyForInvalidRange()
    {
        // Arrange
        var invalidRange = new TextRange(10, 5);
        var source = "Hello, World!";

        // Act
        var result = invalidRange.Extract(source);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Extract_ReturnsEmptyForNullSource()
    {
        // Arrange
        var range = new TextRange(0, 5);

        // Act
        var result = range.Extract(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Extract_ReturnsEmptyWhenStartExceedsLength()
    {
        // Arrange
        var range = new TextRange(50, 60);
        var source = "Hello";

        // Act
        var result = range.Extract(source);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Contains Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(5, true)]
    [InlineData(9, true)]
    [InlineData(4, false)]
    [InlineData(10, false)]
    public void Contains_ChecksRangeCorrectly(int index, bool expected)
    {
        // Arrange
        var range = new TextRange(5, 10);

        // Act & Assert
        Assert.Equal(expected, range.Contains(index));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Overlaps Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0, 10, 5, 15, true)]   // Overlapping
    [InlineData(5, 10, 8, 12, true)]   // One contains part of other
    [InlineData(0, 5, 5, 10, false)]   // Adjacent (not overlapping)
    [InlineData(0, 5, 10, 15, false)]  // Completely separate
    [InlineData(5, 10, 0, 15, true)]   // One contains the other
    public void Overlaps_DetectsCorrectly(int s1, int e1, int s2, int e2, bool expected)
    {
        // Arrange
        var range1 = new TextRange(s1, e1);
        var range2 = new TextRange(s2, e2);

        // Act & Assert
        Assert.Equal(expected, range1.Overlaps(range2));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Offset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Offset_ShiftsRangeCorrectly()
    {
        // Arrange
        var range = new TextRange(5, 15);

        // Act
        var offset = range.Offset(10);

        // Assert
        Assert.Equal(15, offset.Start);
        Assert.Equal(25, offset.End);
        Assert.Equal(range.Length, offset.Length);
    }

    [Fact]
    public void Offset_HandlesNegativeOffset()
    {
        // Arrange
        var range = new TextRange(10, 20);

        // Act
        var offset = range.Offset(-5);

        // Assert
        Assert.Equal(5, offset.Start);
        Assert.Equal(15, offset.End);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Union and Intersect Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Union_EncompassesBothRanges()
    {
        // Arrange
        var range1 = new TextRange(5, 10);
        var range2 = new TextRange(8, 15);

        // Act
        var union = range1.Union(range2);

        // Assert
        Assert.Equal(5, union.Start);
        Assert.Equal(15, union.End);
    }

    [Fact]
    public void Intersect_ReturnsOverlappingPortion()
    {
        // Arrange
        var range1 = new TextRange(5, 15);
        var range2 = new TextRange(10, 20);

        // Act
        var intersection = range1.Intersect(range2);

        // Assert
        Assert.Equal(10, intersection.Start);
        Assert.Equal(15, intersection.End);
    }

    [Fact]
    public void Intersect_ReturnsEmptyForNoOverlap()
    {
        // Arrange
        var range1 = new TextRange(0, 5);
        var range2 = new TextRange(10, 15);

        // Act
        var intersection = range1.Intersect(range2);

        // Assert
        Assert.Equal(TextRange.Empty, intersection);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToString_ReturnsFormattedRange()
    {
        // Arrange
        var range = new TextRange(5, 15);

        // Act
        var result = range.ToString();

        // Assert
        Assert.Equal("[5..15]", result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Property Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Empty_ReturnsZeroRange()
    {
        // Act
        var empty = TextRange.Empty;

        // Assert
        Assert.Equal(0, empty.Start);
        Assert.Equal(0, empty.End);
        Assert.Equal(0, empty.Length);
        Assert.True(empty.IsEmpty);
        Assert.True(empty.IsValid);
    }
}
