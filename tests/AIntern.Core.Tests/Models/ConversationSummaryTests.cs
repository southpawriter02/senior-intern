using Xunit;
using AIntern.Core.Enums;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="ConversationSummary"/> class.
/// Verifies GetDateGroup logic and default property values.
/// </summary>
public class ConversationSummaryTests
{
    #region GetDateGroup Tests

    /// <summary>
    /// Verifies that GetDateGroup returns Today for conversations updated today.
    /// </summary>
    [Fact]
    public void GetDateGroup_Today_ReturnsToday()
    {
        // Arrange
        var summary = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = summary.GetDateGroup();

        // Assert
        Assert.Equal(DateGroup.Today, result);
    }

    /// <summary>
    /// Verifies that GetDateGroup returns Yesterday for conversations updated yesterday.
    /// </summary>
    [Fact]
    public void GetDateGroup_Yesterday_ReturnsYesterday()
    {
        // Arrange
        var summary = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = summary.GetDateGroup();

        // Assert
        Assert.Equal(DateGroup.Yesterday, result);
    }

    /// <summary>
    /// Verifies that GetDateGroup returns Previous7Days for conversations updated 3 days ago.
    /// </summary>
    [Fact]
    public void GetDateGroup_ThreeDaysAgo_ReturnsPrevious7Days()
    {
        // Arrange
        var summary = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        };

        // Act
        var result = summary.GetDateGroup();

        // Assert
        Assert.Equal(DateGroup.Previous7Days, result);
    }

    /// <summary>
    /// Verifies that GetDateGroup returns Previous30Days for conversations updated 15 days ago.
    /// </summary>
    [Fact]
    public void GetDateGroup_FifteenDaysAgo_ReturnsPrevious30Days()
    {
        // Arrange
        var summary = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow.AddDays(-15)
        };

        // Act
        var result = summary.GetDateGroup();

        // Assert
        Assert.Equal(DateGroup.Previous30Days, result);
    }

    /// <summary>
    /// Verifies that GetDateGroup returns Older for conversations updated 60 days ago.
    /// </summary>
    [Fact]
    public void GetDateGroup_SixtyDaysAgo_ReturnsOlder()
    {
        // Arrange
        var summary = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow.AddDays(-60)
        };

        // Act
        var result = summary.GetDateGroup();

        // Assert
        Assert.Equal(DateGroup.Older, result);
    }

    #endregion

    #region Default Value Tests

    /// <summary>
    /// Verifies that ConversationSummary has correct default values.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var summary = new ConversationSummary();

        // Assert
        Assert.Equal(Guid.Empty, summary.Id);
        Assert.Equal(string.Empty, summary.Title);
        Assert.Equal(default(DateTime), summary.CreatedAt);
        Assert.Equal(default(DateTime), summary.UpdatedAt);
        Assert.Equal(0, summary.MessageCount);
        Assert.Null(summary.Preview);
        Assert.False(summary.IsArchived);
        Assert.False(summary.IsPinned);
        Assert.Null(summary.ModelName);
    }

    #endregion
}
