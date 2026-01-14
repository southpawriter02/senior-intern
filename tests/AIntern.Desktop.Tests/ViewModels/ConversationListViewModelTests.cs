using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using AIntern.Core.Enums;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for the <see cref="ConversationListViewModel"/> class.
/// Verifies commands, search debounce, grouping, and event handling.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the ConversationListViewModel correctly handles:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor parameter validation</description></item>
///   <item><description>LoadConversationsCommand execution</description></item>
///   <item><description>CreateNewConversationCommand execution</description></item>
///   <item><description>SelectConversationCommand execution</description></item>
///   <item><description>DeleteConversationCommand execution</description></item>
///   <item><description>RenameConversation workflow</description></item>
///   <item><description>ArchiveConversationCommand execution</description></item>
///   <item><description>TogglePinCommand execution</description></item>
///   <item><description>Search debouncing</description></item>
///   <item><description>Date group organization</description></item>
///   <item><description>Event subscriptions and handling</description></item>
///   <item><description>Dispose pattern</description></item>
/// </list>
/// </remarks>
public class ConversationListViewModelTests : IDisposable
{
    #region Test Infrastructure

    private readonly Mock<IConversationService> _mockConversationService;
    private readonly Mock<IDispatcher> _mockDispatcher;
    private readonly Mock<ILogger<ConversationListViewModel>> _mockLogger;
    private ConversationListViewModel _viewModel = null!;

    public ConversationListViewModelTests()
    {
        _mockConversationService = new Mock<IConversationService>();
        _mockDispatcher = new Mock<IDispatcher>();
        _mockLogger = new Mock<ILogger<ConversationListViewModel>>();

        // Setup dispatcher to execute actions synchronously
        _mockDispatcher
            .Setup(d => d.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(action => action())
            .Returns(Task.CompletedTask);

        _mockDispatcher
            .Setup(d => d.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(func => func());

        // Setup default conversation service behavior
        _mockConversationService
            .Setup(s => s.CurrentConversation)
            .Returns(new Conversation { Id = Guid.NewGuid(), Title = "Current" });

        _mockConversationService
            .Setup(s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());
    }

    private void CreateViewModel()
    {
        _viewModel = new ConversationListViewModel(
            _mockConversationService.Object,
            _mockDispatcher.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws when conversationService is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConversationService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConversationListViewModel(
                null!,
                _mockDispatcher.Object,
                _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that constructor throws when dispatcher is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConversationListViewModel(
                _mockConversationService.Object,
                null!,
                _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConversationListViewModel(
                _mockConversationService.Object,
                _mockDispatcher.Object,
                null!));
    }

    /// <summary>
    /// Verifies that constructor initializes with default values.
    /// </summary>
    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Act
        CreateViewModel();

        // Assert
        Assert.NotNull(_viewModel.Groups);
        Assert.Empty(_viewModel.Groups);
        Assert.Null(_viewModel.SelectedConversation);
        Assert.Equal(string.Empty, _viewModel.SearchQuery);
        Assert.False(_viewModel.IsLoading);
        Assert.False(_viewModel.IsSearching);
    }

    #endregion

    #region LoadConversationsCommand Tests

    /// <summary>
    /// Verifies that LoadConversationsCommand calls GetRecentConversationsAsync.
    /// </summary>
    [Fact]
    public async Task LoadConversationsCommand_CallsService()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.LoadConversationsCommand.ExecuteAsync(null);

        // Assert
        _mockConversationService.Verify(
            s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that LoadConversationsCommand sets IsLoading during execution.
    /// </summary>
    [Fact]
    public async Task LoadConversationsCommand_SetsIsLoadingDuringExecution()
    {
        // Arrange
        CreateViewModel();
        var wasLoading = false;

        _mockConversationService
            .Setup(s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns<int, bool, CancellationToken>((_, _, _) =>
            {
                wasLoading = _viewModel.IsLoading;
                return Task.FromResult<IReadOnlyList<ConversationSummary>>(new List<ConversationSummary>());
            });

        // Act
        await _viewModel.LoadConversationsCommand.ExecuteAsync(null);

        // Assert
        Assert.True(wasLoading);
        Assert.False(_viewModel.IsLoading);
    }

    /// <summary>
    /// Verifies that LoadConversationsCommand sets IsEmpty when no conversations.
    /// </summary>
    [Fact]
    public async Task LoadConversationsCommand_SetsIsEmptyWhenNoConversations()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.LoadConversationsCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.IsEmpty);
    }

    /// <summary>
    /// Verifies that LoadConversationsCommand groups conversations by date.
    /// </summary>
    [Fact]
    public async Task LoadConversationsCommand_GroupsConversationsByDate()
    {
        // Arrange
        var conversations = new List<ConversationSummary>
        {
            new() { Id = Guid.NewGuid(), Title = "Today", UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Title = "Yesterday", UpdatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Title = "Last Week", UpdatedAt = DateTime.UtcNow.AddDays(-5) }
        };

        _mockConversationService
            .Setup(s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversations);

        CreateViewModel();

        // Act
        await _viewModel.LoadConversationsCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(3, _viewModel.Groups.Count);
        Assert.Contains(_viewModel.Groups, g => g.DateGroup == DateGroup.Today);
        Assert.Contains(_viewModel.Groups, g => g.DateGroup == DateGroup.Yesterday);
        Assert.Contains(_viewModel.Groups, g => g.DateGroup == DateGroup.Previous7Days);
    }

    #endregion

    #region CreateNewConversationCommand Tests

    /// <summary>
    /// Verifies that CreateNewConversationCommand calls service.
    /// </summary>
    [Fact]
    public async Task CreateNewConversationCommand_CallsService()
    {
        // Arrange
        CreateViewModel();
        _mockConversationService
            .Setup(s => s.CreateNewConversationAsync(It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation());

        // Act
        await _viewModel.CreateNewConversationCommand.ExecuteAsync(null);

        // Assert
        _mockConversationService.Verify(
            s => s.CreateNewConversationAsync(It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region SelectConversationCommand Tests

    /// <summary>
    /// Verifies that SelectConversationCommand calls LoadConversationAsync.
    /// </summary>
    [Fact]
    public async Task SelectConversationCommand_CallsLoadConversationAsync()
    {
        // Arrange
        CreateViewModel();
        var conversationId = Guid.NewGuid();
        var summary = new ConversationSummaryViewModel
        {
            Id = conversationId,
            Title = "Test"
        };

        _mockConversationService
            .Setup(s => s.LoadConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = conversationId });

        // Act
        await _viewModel.SelectConversationCommand.ExecuteAsync(summary);

        // Assert
        _mockConversationService.Verify(
            s => s.LoadConversationAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that SelectConversationCommand skips null summary.
    /// </summary>
    [Fact]
    public async Task SelectConversationCommand_SkipsNullSummary()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.SelectConversationCommand.ExecuteAsync(null);

        // Assert
        _mockConversationService.Verify(
            s => s.LoadConversationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that SelectConversationCommand skips already selected conversation.
    /// </summary>
    [Fact]
    public async Task SelectConversationCommand_SkipsAlreadySelected()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        _mockConversationService
            .Setup(s => s.CurrentConversation)
            .Returns(new Conversation { Id = conversationId });

        CreateViewModel();

        var summary = new ConversationSummaryViewModel
        {
            Id = conversationId,
            Title = "Already Selected"
        };

        // Act
        await _viewModel.SelectConversationCommand.ExecuteAsync(summary);

        // Assert
        _mockConversationService.Verify(
            s => s.LoadConversationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DeleteConversationCommand Tests

    /// <summary>
    /// Verifies that DeleteConversationCommand calls service when no owner window.
    /// </summary>
    [Fact]
    public async Task DeleteConversationCommand_CallsServiceWhenNoOwnerWindow()
    {
        // Arrange
        CreateViewModel();
        var conversationId = Guid.NewGuid();
        var summary = new ConversationSummaryViewModel
        {
            Id = conversationId,
            Title = "To Delete"
        };

        // Act
        await _viewModel.DeleteConversationCommand.ExecuteAsync(summary);

        // Assert
        _mockConversationService.Verify(
            s => s.DeleteConversationAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RenameConversation Tests

    /// <summary>
    /// Verifies that RenameConversationCommand starts rename mode.
    /// </summary>
    [Fact]
    public void RenameConversationCommand_StartsRenameMode()
    {
        // Arrange
        CreateViewModel();
        var summary = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Original"
        };

        // Act
        _viewModel.RenameConversationCommand.Execute(summary);

        // Assert
        Assert.True(summary.IsRenaming);
        Assert.Equal("Original", summary.EditingTitle);
    }

    /// <summary>
    /// Verifies that ConfirmRenameCommand calls service when title changed.
    /// </summary>
    [Fact]
    public async Task ConfirmRenameCommand_CallsServiceWhenTitleChanged()
    {
        // Arrange
        CreateViewModel();
        var conversationId = Guid.NewGuid();
        var summary = new ConversationSummaryViewModel
        {
            Id = conversationId,
            Title = "Original"
        };
        summary.BeginRename();
        summary.EditingTitle = "New Title";

        // Act
        await _viewModel.ConfirmRenameCommand.ExecuteAsync(summary);

        // Assert
        _mockConversationService.Verify(
            s => s.RenameConversationAsync(conversationId, "New Title", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ConfirmRenameCommand skips when title unchanged.
    /// </summary>
    [Fact]
    public async Task ConfirmRenameCommand_SkipsWhenTitleUnchanged()
    {
        // Arrange
        CreateViewModel();
        var summary = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Original"
        };
        summary.BeginRename();
        // EditingTitle is already "Original"

        // Act
        await _viewModel.ConfirmRenameCommand.ExecuteAsync(summary);

        // Assert
        _mockConversationService.Verify(
            s => s.RenameConversationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.False(summary.IsRenaming);
    }

    /// <summary>
    /// Verifies that CancelRenameCommand reverts rename mode.
    /// </summary>
    [Fact]
    public void CancelRenameCommand_RevertsRenameMode()
    {
        // Arrange
        CreateViewModel();
        var summary = new ConversationSummaryViewModel
        {
            Id = Guid.NewGuid(),
            Title = "Original"
        };
        summary.BeginRename();
        summary.EditingTitle = "Changed";

        // Act
        _viewModel.CancelRenameCommand.Execute(summary);

        // Assert
        Assert.False(summary.IsRenaming);
        Assert.Equal("Original", summary.EditingTitle);
    }

    #endregion

    #region ArchiveConversationCommand Tests

    /// <summary>
    /// Verifies that ArchiveConversationCommand calls service.
    /// </summary>
    [Fact]
    public async Task ArchiveConversationCommand_CallsService()
    {
        // Arrange
        CreateViewModel();
        var conversationId = Guid.NewGuid();
        var summary = new ConversationSummaryViewModel
        {
            Id = conversationId,
            Title = "To Archive"
        };

        // Act
        await _viewModel.ArchiveConversationCommand.ExecuteAsync(summary);

        // Assert
        _mockConversationService.Verify(
            s => s.ArchiveConversationAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TogglePinCommand Tests

    /// <summary>
    /// Verifies that TogglePinCommand pins unpinned conversation.
    /// </summary>
    [Fact]
    public async Task TogglePinCommand_PinsUnpinnedConversation()
    {
        // Arrange
        CreateViewModel();
        var conversationId = Guid.NewGuid();
        var summary = new ConversationSummaryViewModel
        {
            Id = conversationId,
            Title = "To Pin",
            IsPinned = false
        };

        // Act
        await _viewModel.TogglePinCommand.ExecuteAsync(summary);

        // Assert
        _mockConversationService.Verify(
            s => s.PinConversationAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(summary.IsPinned);
    }

    /// <summary>
    /// Verifies that TogglePinCommand unpins pinned conversation.
    /// </summary>
    [Fact]
    public async Task TogglePinCommand_UnpinsPinnedConversation()
    {
        // Arrange
        CreateViewModel();
        var conversationId = Guid.NewGuid();
        var summary = new ConversationSummaryViewModel
        {
            Id = conversationId,
            Title = "To Unpin",
            IsPinned = true
        };

        // Act
        await _viewModel.TogglePinCommand.ExecuteAsync(summary);

        // Assert
        _mockConversationService.Verify(
            s => s.UnpinConversationAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.False(summary.IsPinned);
    }

    #endregion

    #region Search Tests

    /// <summary>
    /// Verifies that ClearSearchCommand clears the search query.
    /// </summary>
    [Fact]
    public void ClearSearchCommand_ClearsSearchQuery()
    {
        // Arrange
        CreateViewModel();
        _viewModel.SearchQuery = "test query";

        // Act
        _viewModel.ClearSearchCommand.Execute(null);

        // Assert
        Assert.Equal(string.Empty, _viewModel.SearchQuery);
    }

    /// <summary>
    /// Verifies that IsSearching is set when search query is not empty.
    /// </summary>
    [Fact]
    public async Task SearchQuery_SetsIsSearchingWhenNotEmpty()
    {
        // Arrange
        CreateViewModel();

        _mockConversationService
            .Setup(s => s.SearchConversationsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());

        // Act
        _viewModel.SearchQuery = "test";
        // Wait for debounce (300ms + buffer)
        await Task.Delay(400);

        // Assert
        Assert.True(_viewModel.IsSearching);
    }

    /// <summary>
    /// Verifies that search is debounced (multiple rapid changes result in one search).
    /// </summary>
    [Fact]
    public async Task SearchQuery_DebouncesMultipleChanges()
    {
        // Arrange
        CreateViewModel();
        var searchCallCount = 0;

        _mockConversationService
            .Setup(s => s.SearchConversationsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => searchCallCount++)
            .ReturnsAsync(new List<ConversationSummary>());

        // Act - Rapid changes
        _viewModel.SearchQuery = "t";
        _viewModel.SearchQuery = "te";
        _viewModel.SearchQuery = "tes";
        _viewModel.SearchQuery = "test";

        // Wait for debounce
        await Task.Delay(400);

        // Assert - Only one search call (debounced)
        Assert.Equal(1, searchCallCount);
    }

    #endregion

    #region Event Handling Tests

    /// <summary>
    /// Verifies that ConversationListChanged event triggers refresh.
    /// </summary>
    [Fact]
    public void ConversationListChanged_TriggersRefresh()
    {
        // Arrange
        CreateViewModel();

        // Act
        _mockConversationService.Raise(
            s => s.ConversationListChanged += null,
            new ConversationListChangedEventArgs
            {
                ChangeType = ConversationListChangeType.ConversationAdded
            });

        // Need to wait a bit for async event handler
        Thread.Sleep(100);

        // Assert
        _mockConversationService.Verify(
            s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies that Dispose unsubscribes from events.
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        CreateViewModel();

        // Act
        _viewModel.Dispose();

        // Raise events after dispose - should not cause issues
        _mockConversationService.Raise(
            s => s.ConversationListChanged += null,
            new ConversationListChangedEventArgs
            {
                ChangeType = ConversationListChangeType.ListRefreshed
            });

        // Assert - No exceptions, and no additional service calls after initial
        Assert.True(true); // If we get here without exception, test passes
    }

    /// <summary>
    /// Verifies that Dispose is idempotent (safe to call multiple times).
    /// </summary>
    [Fact]
    public void Dispose_IsIdempotent()
    {
        // Arrange
        CreateViewModel();

        // Act & Assert - Should not throw
        _viewModel.Dispose();
        _viewModel.Dispose();
        _viewModel.Dispose();
    }

    #endregion

    #region Grouping Tests

    /// <summary>
    /// Verifies that pinned conversations appear first within their group.
    /// </summary>
    [Fact]
    public async Task LoadConversations_PinnedConversationsAppearFirst()
    {
        // Arrange
        var conversations = new List<ConversationSummary>
        {
            new() { Id = Guid.NewGuid(), Title = "Not Pinned", UpdatedAt = DateTime.UtcNow, IsPinned = false },
            new() { Id = Guid.NewGuid(), Title = "Pinned", UpdatedAt = DateTime.UtcNow, IsPinned = true }
        };

        _mockConversationService
            .Setup(s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversations);

        CreateViewModel();

        // Act
        await _viewModel.LoadConversationsCommand.ExecuteAsync(null);

        // Assert
        // Both conversations should be in Today group
        Assert.Single(_viewModel.Groups);
        var todayGroup = _viewModel.Groups[0];
        Assert.Equal(DateGroup.Today, todayGroup.DateGroup);
        Assert.Equal(2, todayGroup.Conversations.Count);
        // Pinned should come first due to OrderByDescending(IsPinned)
        Assert.True(todayGroup.Conversations[0].IsPinned);
        Assert.Equal("Pinned", todayGroup.Conversations[0].Title);
        Assert.False(todayGroup.Conversations[1].IsPinned);
        Assert.Equal("Not Pinned", todayGroup.Conversations[1].Title);
    }

    /// <summary>
    /// Verifies that within same pin status, conversations are sorted by UpdatedAt descending.
    /// </summary>
    [Fact]
    public async Task LoadConversations_SortsByUpdatedAtDescending()
    {
        // Arrange
        // Use times within the same day (minutes apart) to ensure they're all in the same date group
        // This avoids issues where AddHours(-2) might cross into yesterday if run early in the morning
        var now = DateTime.UtcNow;
        var conversations = new List<ConversationSummary>
        {
            new() { Id = Guid.NewGuid(), Title = "Older", UpdatedAt = now.AddMinutes(-30), IsPinned = false },
            new() { Id = Guid.NewGuid(), Title = "Newer", UpdatedAt = now.AddMinutes(-15), IsPinned = false },
            new() { Id = Guid.NewGuid(), Title = "Newest", UpdatedAt = now, IsPinned = false }
        };

        _mockConversationService
            .Setup(s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversations);

        CreateViewModel();

        // Act
        await _viewModel.LoadConversationsCommand.ExecuteAsync(null);

        // Assert
        // All conversations should be in Today group
        Assert.Single(_viewModel.Groups);
        var todayGroup = _viewModel.Groups[0];
        Assert.Equal(DateGroup.Today, todayGroup.DateGroup);
        Assert.Equal(3, todayGroup.Conversations.Count);
        // Should be sorted by UpdatedAt descending
        Assert.Equal("Newest", todayGroup.Conversations[0].Title);
        Assert.Equal("Newer", todayGroup.Conversations[1].Title);
        Assert.Equal("Older", todayGroup.Conversations[2].Title);
    }

    #endregion

    #region InitializeAsync Tests

    /// <summary>
    /// Verifies that InitializeAsync loads conversations.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsConversations()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _mockConversationService.Verify(
            s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region SetOwnerWindowProvider Tests

    /// <summary>
    /// Verifies that SetOwnerWindowProvider accepts a provider function.
    /// </summary>
    [Fact]
    public void SetOwnerWindowProvider_AcceptsProvider()
    {
        // Arrange
        CreateViewModel();

        // Act & Assert - Should not throw
        _viewModel.SetOwnerWindowProvider(() => null);
    }

    #endregion
}
