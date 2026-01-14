using Xunit;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for the <see cref="ConversationSummaryViewModel"/> class.
/// Verifies RelativeTime formatting, MessageCountText, and state changes.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the ConversationSummaryViewModel correctly handles:
/// </para>
/// <list type="bullet">
///   <item><description>RelativeTime formatting for various time differences</description></item>
///   <item><description>MessageCountText pluralization</description></item>
///   <item><description>DisplayTitle truncation</description></item>
///   <item><description>Rename workflow (BeginRename, CancelRename)</description></item>
/// </list>
/// </remarks>
public class ConversationSummaryViewModelTests
{
    #region RelativeTime Tests

    /// <summary>
    /// Verifies that RelativeTime returns "Just now" for times less than 1 minute ago.
    /// </summary>
    [Fact]
    public void RelativeTime_LessThanOneMinute_ReturnsJustNow()
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTime.UtcNow.AddSeconds(-30)
        };

        // Act
        var result = viewModel.RelativeTime;

        // Assert
        Assert.Equal("Just now", result);
    }

    /// <summary>
    /// Verifies that RelativeTime returns minutes format for times 1-59 minutes ago.
    /// </summary>
    [Theory]
    [InlineData(5, "5m ago")]
    [InlineData(30, "30m ago")]
    [InlineData(59, "59m ago")]
    public void RelativeTime_Minutes_ReturnsMinutesFormat(int minutes, string expected)
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-minutes)
        };

        // Act
        var result = viewModel.RelativeTime;

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that RelativeTime returns hours format for times 1-23 hours ago.
    /// </summary>
    [Theory]
    [InlineData(1, "1h ago")]
    [InlineData(12, "12h ago")]
    [InlineData(23, "23h ago")]
    public void RelativeTime_Hours_ReturnsHoursFormat(int hours, string expected)
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTime.UtcNow.AddHours(-hours)
        };

        // Act
        var result = viewModel.RelativeTime;

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that RelativeTime returns days format for times 1-6 days ago.
    /// </summary>
    [Theory]
    [InlineData(1, "1d ago")]
    [InlineData(3, "3d ago")]
    [InlineData(6, "6d ago")]
    public void RelativeTime_Days_ReturnsDaysFormat(int days, string expected)
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTime.UtcNow.AddDays(-days)
        };

        // Act
        var result = viewModel.RelativeTime;

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that RelativeTime returns weeks format for times 7-29 days ago.
    /// </summary>
    [Theory]
    [InlineData(7, "1w ago")]
    [InlineData(14, "2w ago")]
    [InlineData(21, "3w ago")]
    public void RelativeTime_Weeks_ReturnsWeeksFormat(int days, string expected)
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTime.UtcNow.AddDays(-days)
        };

        // Act
        var result = viewModel.RelativeTime;

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that RelativeTime returns date format for times 30+ days ago.
    /// </summary>
    [Fact]
    public void RelativeTime_OlderThan30Days_ReturnsDateFormat()
    {
        // Arrange
        var testDate = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = testDate
        };

        // Act
        var result = viewModel.RelativeTime;

        // Assert
        Assert.Equal("Jan 15", result);
    }

    #endregion

    #region MessageCountText Tests

    /// <summary>
    /// Verifies that MessageCountText returns "No messages" for zero count.
    /// </summary>
    [Fact]
    public void MessageCountText_ZeroMessages_ReturnsNoMessages()
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            MessageCount = 0
        };

        // Act
        var result = viewModel.MessageCountText;

        // Assert
        Assert.Equal("No messages", result);
    }

    /// <summary>
    /// Verifies that MessageCountText returns singular form for one message.
    /// </summary>
    [Fact]
    public void MessageCountText_OneMessage_ReturnsSingular()
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            MessageCount = 1
        };

        // Act
        var result = viewModel.MessageCountText;

        // Assert
        Assert.Equal("1 message", result);
    }

    /// <summary>
    /// Verifies that MessageCountText returns plural form for multiple messages.
    /// </summary>
    [Theory]
    [InlineData(2, "2 messages")]
    [InlineData(10, "10 messages")]
    [InlineData(1000, "1,000 messages")]
    public void MessageCountText_MultipleMessages_ReturnsPlural(int count, string expected)
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            MessageCount = count
        };

        // Act
        var result = viewModel.MessageCountText;

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region DisplayTitle Tests

    /// <summary>
    /// Verifies that DisplayTitle returns full title when under 50 characters.
    /// </summary>
    [Fact]
    public void DisplayTitle_ShortTitle_ReturnsFullTitle()
    {
        // Arrange
        const string title = "Short Title";
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = title
        };

        // Act
        var result = viewModel.DisplayTitle;

        // Assert
        Assert.Equal(title, result);
    }

    /// <summary>
    /// Verifies that DisplayTitle truncates with ellipsis when over 50 characters.
    /// </summary>
    [Fact]
    public void DisplayTitle_LongTitle_TruncatesWithEllipsis()
    {
        // Arrange
        var longTitle = new string('A', 60);
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = longTitle
        };

        // Act
        var result = viewModel.DisplayTitle;

        // Assert
        Assert.Equal(50, result.Length);
        Assert.EndsWith("...", result);
        Assert.StartsWith(new string('A', 47), result);
    }

    #endregion

    #region Rename State Tests

    /// <summary>
    /// Verifies that BeginRename sets IsRenaming and initializes EditingTitle.
    /// </summary>
    [Fact]
    public void BeginRename_SetsIsRenamingAndInitializesEditingTitle()
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Original Title"
        };

        // Act
        viewModel.BeginRename();

        // Assert
        Assert.True(viewModel.IsRenaming);
        Assert.Equal("Original Title", viewModel.EditingTitle);
    }

    /// <summary>
    /// Verifies that CancelRename clears IsRenaming and resets EditingTitle.
    /// </summary>
    [Fact]
    public void CancelRename_ClearsIsRenamingAndResetsEditingTitle()
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Original Title"
        };
        viewModel.BeginRename();
        viewModel.EditingTitle = "Changed Title";

        // Act
        viewModel.CancelRename();

        // Assert
        Assert.False(viewModel.IsRenaming);
        Assert.Equal("Original Title", viewModel.EditingTitle);
    }

    #endregion

    #region Default Value Tests

    /// <summary>
    /// Verifies that ConversationSummaryViewModel has correct default values.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var viewModel = new ConversationSummaryViewModel();

        // Assert
        Assert.Equal(Guid.Empty, viewModel.Id);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.Equal(default(DateTime), viewModel.UpdatedAt);
        Assert.Equal(0, viewModel.MessageCount);
        Assert.Null(viewModel.Preview);
        Assert.Null(viewModel.ModelName);
        Assert.False(viewModel.IsSelected);
        Assert.False(viewModel.IsPinned);
        Assert.False(viewModel.IsRenaming);
        Assert.Equal(string.Empty, viewModel.EditingTitle);
    }

    #endregion

    #region Property Change Notification Tests

    /// <summary>
    /// Verifies that changing UpdatedAt notifies RelativeTime property changed.
    /// </summary>
    [Fact]
    public void UpdatedAt_Changed_NotifiesRelativeTimeChanged()
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConversationSummaryViewModel.RelativeTime))
                propertyChangedCount++;
        };

        // Act
        viewModel.UpdatedAt = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, propertyChangedCount);
    }

    /// <summary>
    /// Verifies that changing MessageCount notifies MessageCountText property changed.
    /// </summary>
    [Fact]
    public void MessageCount_Changed_NotifiesMessageCountTextChanged()
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            MessageCount = 0
        };
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConversationSummaryViewModel.MessageCountText))
                propertyChangedCount++;
        };

        // Act
        viewModel.MessageCount = 5;

        // Assert
        Assert.Equal(1, propertyChangedCount);
    }

    /// <summary>
    /// Verifies that changing Title notifies DisplayTitle property changed.
    /// </summary>
    [Fact]
    public void Title_Changed_NotifiesDisplayTitleChanged()
    {
        // Arrange
        var viewModel = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Original"
        };
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConversationSummaryViewModel.DisplayTitle))
                propertyChangedCount++;
        };

        // Act
        viewModel.Title = "New Title";

        // Assert
        Assert.Equal(1, propertyChangedCount);
    }

    #endregion
}
