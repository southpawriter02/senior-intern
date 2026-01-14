using Xunit;
using AIntern.Core.Enums;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for the <see cref="ConversationGroupViewModel"/> class.
/// Verifies Conversations collection, Count, and expand state.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the ConversationGroupViewModel correctly handles:
/// </para>
/// <list type="bullet">
///   <item><description>Conversations collection initialization</description></item>
///   <item><description>Count property computed from collection</description></item>
///   <item><description>Count property updated when collection changes</description></item>
///   <item><description>IsExpanded default and toggle state</description></item>
///   <item><description>GetTitleForGroup static factory method</description></item>
/// </list>
/// </remarks>
public class ConversationGroupViewModelTests
{
    #region Conversations Collection Tests

    /// <summary>
    /// Verifies that Conversations collection is initialized and not null.
    /// </summary>
    [Fact]
    public void Conversations_IsInitialized()
    {
        // Arrange & Act
        var viewModel = new ConversationGroupViewModel();

        // Assert
        Assert.NotNull(viewModel.Conversations);
        Assert.Empty(viewModel.Conversations);
    }

    /// <summary>
    /// Verifies that items can be added to the Conversations collection.
    /// </summary>
    [Fact]
    public void Conversations_CanAddItems()
    {
        // Arrange
        var viewModel = new ConversationGroupViewModel();
        var summary = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Test Conversation"
        };

        // Act
        viewModel.Conversations.Add(summary);

        // Assert
        Assert.Single(viewModel.Conversations);
        Assert.Equal(summary, viewModel.Conversations[0]);
    }

    #endregion

    #region Count Tests

    /// <summary>
    /// Verifies that Count returns the number of conversations in the collection.
    /// </summary>
    [Fact]
    public void Count_ReturnsConversationCount()
    {
        // Arrange
        var viewModel = new ConversationGroupViewModel();
        viewModel.Conversations.Add(new ConversationSummaryViewModel { Id = Guid.NewGuid(), Title = "Test 1" });
        viewModel.Conversations.Add(new ConversationSummaryViewModel { Id = Guid.NewGuid(), Title = "Test 2" });
        viewModel.Conversations.Add(new ConversationSummaryViewModel { Id = Guid.NewGuid(), Title = "Test 3" });

        // Act
        var count = viewModel.Count;

        // Assert
        Assert.Equal(3, count);
    }

    /// <summary>
    /// Verifies that Count is updated when items are added to the collection.
    /// </summary>
    [Fact]
    public void Count_UpdatesWhenItemAdded()
    {
        // Arrange
        var viewModel = new ConversationGroupViewModel();
        Assert.Equal(0, viewModel.Count);

        // Act
        viewModel.Conversations.Add(new ConversationSummaryViewModel { Id = Guid.NewGuid(), Title = "Test" });

        // Assert
        Assert.Equal(1, viewModel.Count);
    }

    /// <summary>
    /// Verifies that Count is updated when items are removed from the collection.
    /// </summary>
    [Fact]
    public void Count_UpdatesWhenItemRemoved()
    {
        // Arrange
        var viewModel = new ConversationGroupViewModel();
        var summary = new ConversationSummaryViewModel { Id = Guid.NewGuid(), Title = "Test" };
        viewModel.Conversations.Add(summary);
        Assert.Equal(1, viewModel.Count);

        // Act
        viewModel.Conversations.Remove(summary);

        // Assert
        Assert.Equal(0, viewModel.Count);
    }

    /// <summary>
    /// Verifies that Count property change notification fires when collection changes.
    /// </summary>
    [Fact]
    public void Count_NotifiesPropertyChangedWhenCollectionChanges()
    {
        // Arrange
        var viewModel = new ConversationGroupViewModel();
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConversationGroupViewModel.Count))
                propertyChangedCount++;
        };

        // Act
        viewModel.Conversations.Add(new ConversationSummaryViewModel { Id = Guid.NewGuid(), Title = "Test" });

        // Assert
        Assert.Equal(1, propertyChangedCount);
    }

    #endregion

    #region IsExpanded Tests

    /// <summary>
    /// Verifies that IsExpanded defaults to true.
    /// </summary>
    [Fact]
    public void IsExpanded_DefaultsToTrue()
    {
        // Arrange & Act
        var viewModel = new ConversationGroupViewModel();

        // Assert
        Assert.True(viewModel.IsExpanded);
    }

    /// <summary>
    /// Verifies that IsExpanded can be set to false.
    /// </summary>
    [Fact]
    public void IsExpanded_CanBeSetToFalse()
    {
        // Arrange
        var viewModel = new ConversationGroupViewModel();

        // Act
        viewModel.IsExpanded = false;

        // Assert
        Assert.False(viewModel.IsExpanded);
    }

    /// <summary>
    /// Verifies that IsExpanded notifies property changed when modified.
    /// </summary>
    [Fact]
    public void IsExpanded_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new ConversationGroupViewModel();
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConversationGroupViewModel.IsExpanded))
                propertyChangedCount++;
        };

        // Act
        viewModel.IsExpanded = false;

        // Assert
        Assert.Equal(1, propertyChangedCount);
    }

    #endregion

    #region GetTitleForGroup Tests

    /// <summary>
    /// Verifies that GetTitleForGroup returns correct title for each DateGroup.
    /// </summary>
    [Theory]
    [InlineData(DateGroup.Today, "Today")]
    [InlineData(DateGroup.Yesterday, "Yesterday")]
    [InlineData(DateGroup.Previous7Days, "Previous 7 Days")]
    [InlineData(DateGroup.Previous30Days, "Previous 30 Days")]
    [InlineData(DateGroup.Older, "Older")]
    public void GetTitleForGroup_ReturnsCorrectTitle(DateGroup dateGroup, string expectedTitle)
    {
        // Act
        var result = ConversationGroupViewModel.GetTitleForGroup(dateGroup);

        // Assert
        Assert.Equal(expectedTitle, result);
    }

    #endregion

    #region Default Value Tests

    /// <summary>
    /// Verifies that ConversationGroupViewModel has correct default values.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var viewModel = new ConversationGroupViewModel();

        // Assert
        Assert.Equal(default(DateGroup), viewModel.DateGroup);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.True(viewModel.IsExpanded);
        Assert.NotNull(viewModel.Conversations);
        Assert.Empty(viewModel.Conversations);
        Assert.Equal(0, viewModel.Count);
    }

    /// <summary>
    /// Verifies that DateGroup can be set via init-only property.
    /// </summary>
    [Fact]
    public void DateGroup_CanBeSetViaInit()
    {
        // Arrange & Act
        var viewModel = new ConversationGroupViewModel
        {
            DateGroup = DateGroup.Yesterday
        };

        // Assert
        Assert.Equal(DateGroup.Yesterday, viewModel.DateGroup);
    }

    #endregion
}
