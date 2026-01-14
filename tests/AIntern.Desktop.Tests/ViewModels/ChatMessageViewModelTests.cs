using Xunit;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for the <see cref="ChatMessageViewModel"/> class.
/// Verifies properties, computed properties, methods, and constructors.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the ChatMessageViewModel correctly handles:
/// </para>
/// <list type="bullet">
///   <item><description>Observable properties (Content, Role, IsStreaming, etc.)</description></item>
///   <item><description>Computed properties (IsUser, IsAssistant, RoleLabel, TokensPerSecond)</description></item>
///   <item><description>Methods (AppendContent, CompleteStreaming, MarkAsCancelled)</description></item>
///   <item><description>Constructor from ChatMessage domain model</description></item>
///   <item><description>ToChatMessage conversion back to domain model</description></item>
/// </list>
/// </remarks>
public class ChatMessageViewModelTests
{
    #region Default Constructor Tests

    /// <summary>
    /// Verifies that the default constructor initializes with a new Guid.
    /// </summary>
    [Fact]
    public void Constructor_Default_InitializesWithNewGuid()
    {
        // Act
        var vm = new ChatMessageViewModel();

        // Assert
        Assert.NotEqual(Guid.Empty, vm.Id);
    }

    /// <summary>
    /// Verifies that the default constructor sets timestamp to approximately now.
    /// </summary>
    [Fact]
    public void Constructor_Default_SetsTimestampToNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var vm = new ChatMessageViewModel();

        // Assert
        var afterCreation = DateTime.UtcNow;
        Assert.InRange(vm.Timestamp, beforeCreation, afterCreation);
    }

    /// <summary>
    /// Verifies that the default constructor initializes with empty content.
    /// </summary>
    [Fact]
    public void Constructor_Default_InitializesEmptyContent()
    {
        // Act
        var vm = new ChatMessageViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.Content);
    }

    #endregion

    #region ChatMessage Constructor Tests

    /// <summary>
    /// Verifies that the constructor from ChatMessage copies all properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_FromChatMessage_CopiesAllProperties()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Content = "Hello, world!",
            Role = MessageRole.User,
            Timestamp = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            IsComplete = true,
            TokenCount = 50,
            GenerationTime = TimeSpan.FromSeconds(2.5)
        };

        // Act
        var vm = new ChatMessageViewModel(message);

        // Assert
        Assert.Equal(message.Id, vm.Id);
        Assert.Equal(message.Content, vm.Content);
        Assert.Equal(message.Role, vm.Role);
        Assert.Equal(message.Timestamp, vm.Timestamp);
        Assert.False(vm.IsStreaming); // IsComplete=true means IsStreaming=false
        Assert.Equal(message.TokenCount, vm.TokenCount);
        Assert.Equal(message.GenerationTime, vm.GenerationTime);
    }

    /// <summary>
    /// Verifies that IsStreaming is true when IsComplete is false.
    /// </summary>
    [Fact]
    public void Constructor_FromChatMessage_SetsIsStreamingWhenNotComplete()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Content = "In progress...",
            Role = MessageRole.Assistant,
            IsComplete = false
        };

        // Act
        var vm = new ChatMessageViewModel(message);

        // Assert
        Assert.True(vm.IsStreaming);
    }

    #endregion

    #region IsUser / IsAssistant / IsSystem Tests

    /// <summary>
    /// Verifies that IsUser returns true for User role.
    /// </summary>
    [Fact]
    public void IsUser_WithUserRole_ReturnsTrue()
    {
        // Arrange
        var vm = new ChatMessageViewModel { Role = MessageRole.User };

        // Assert
        Assert.True(vm.IsUser);
        Assert.False(vm.IsAssistant);
    }

    /// <summary>
    /// Verifies that IsAssistant returns true for Assistant role.
    /// </summary>
    [Fact]
    public void IsAssistant_WithAssistantRole_ReturnsTrue()
    {
        // Arrange
        var vm = new ChatMessageViewModel { Role = MessageRole.Assistant };

        // Assert
        Assert.True(vm.IsAssistant);
        Assert.False(vm.IsUser);
    }

    /// <summary>
    /// Verifies that both IsUser and IsAssistant are false for System role.
    /// </summary>
    [Fact]
    public void IsUserAndIsAssistant_WithSystemRole_BothReturnFalse()
    {
        // Arrange
        var vm = new ChatMessageViewModel { Role = MessageRole.System };

        // Assert
        Assert.False(vm.IsUser);
        Assert.False(vm.IsAssistant);
    }

    #endregion

    #region RoleLabel Tests

    /// <summary>
    /// Verifies that RoleLabel returns correct labels for each role.
    /// </summary>
    [Theory]
    [InlineData(MessageRole.User, "You")]
    [InlineData(MessageRole.Assistant, "AIntern")]
    [InlineData(MessageRole.System, "System")]
    public void RoleLabel_ReturnsCorrectLabel(MessageRole role, string expectedLabel)
    {
        // Arrange
        var vm = new ChatMessageViewModel { Role = role };

        // Act & Assert
        Assert.Equal(expectedLabel, vm.RoleLabel);
    }

    #endregion

    #region TokensPerSecond Tests

    /// <summary>
    /// Verifies that TokensPerSecond calculates correctly when both values are present.
    /// </summary>
    [Fact]
    public void TokensPerSecond_WithValidValues_CalculatesCorrectly()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            TokenCount = 100,
            GenerationTime = TimeSpan.FromSeconds(4)
        };

        // Act
        var tokensPerSecond = vm.TokensPerSecond;

        // Assert
        Assert.NotNull(tokensPerSecond);
        Assert.Equal(25.0, tokensPerSecond.Value, precision: 1);
    }

    /// <summary>
    /// Verifies that TokensPerSecond returns null when TokenCount is null.
    /// </summary>
    [Fact]
    public void TokensPerSecond_WithNullTokenCount_ReturnsNull()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            TokenCount = null,
            GenerationTime = TimeSpan.FromSeconds(4)
        };

        // Act & Assert
        Assert.Null(vm.TokensPerSecond);
    }

    /// <summary>
    /// Verifies that TokensPerSecond returns null when GenerationTime is null.
    /// </summary>
    [Fact]
    public void TokensPerSecond_WithNullGenerationTime_ReturnsNull()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            TokenCount = 100,
            GenerationTime = null
        };

        // Act & Assert
        Assert.Null(vm.TokensPerSecond);
    }

    /// <summary>
    /// Verifies that TokensPerSecond returns null when GenerationTime is zero.
    /// </summary>
    [Fact]
    public void TokensPerSecond_WithZeroGenerationTime_ReturnsNull()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            TokenCount = 100,
            GenerationTime = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Null(vm.TokensPerSecond);
    }

    #endregion

    #region PerformanceStats Tests

    /// <summary>
    /// Verifies that PerformanceStats returns formatted string when values are present.
    /// </summary>
    [Fact]
    public void PerformanceStats_WithValidValues_ReturnsFormattedString()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            TokenCount = 127,
            GenerationTime = TimeSpan.FromSeconds(3)
        };

        // Act
        var stats = vm.PerformanceStats;

        // Assert
        Assert.NotNull(stats);
        Assert.Equal("127 tokens | 42.3 tok/s", stats);
    }

    /// <summary>
    /// Verifies that PerformanceStats returns null when TokensPerSecond is null.
    /// </summary>
    [Fact]
    public void PerformanceStats_WithNoTokensPerSecond_ReturnsNull()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            TokenCount = null,
            GenerationTime = null
        };

        // Act & Assert
        Assert.Null(vm.PerformanceStats);
    }

    #endregion

    #region AppendContent Tests

    /// <summary>
    /// Verifies that AppendContent appends token to existing content.
    /// </summary>
    [Fact]
    public void AppendContent_AppendsTokenToContent()
    {
        // Arrange
        var vm = new ChatMessageViewModel { Content = "Hello" };

        // Act
        vm.AppendContent(" world");

        // Assert
        Assert.Equal("Hello world", vm.Content);
    }

    /// <summary>
    /// Verifies that AppendContent works with empty initial content.
    /// </summary>
    [Fact]
    public void AppendContent_WithEmptyContent_StartsFromToken()
    {
        // Arrange
        var vm = new ChatMessageViewModel { Content = string.Empty };

        // Act
        vm.AppendContent("First");

        // Assert
        Assert.Equal("First", vm.Content);
    }

    /// <summary>
    /// Verifies that AppendContent can be called multiple times.
    /// </summary>
    [Fact]
    public void AppendContent_MultipleCalls_AccumulatesContent()
    {
        // Arrange
        var vm = new ChatMessageViewModel();

        // Act
        vm.AppendContent("Hello");
        vm.AppendContent(" ");
        vm.AppendContent("World");

        // Assert
        Assert.Equal("Hello World", vm.Content);
    }

    #endregion

    #region CompleteStreaming Tests

    /// <summary>
    /// Verifies that CompleteStreaming sets IsStreaming to false.
    /// </summary>
    [Fact]
    public void CompleteStreaming_SetsIsStreamingToFalse()
    {
        // Arrange
        var vm = new ChatMessageViewModel { IsStreaming = true };

        // Act
        vm.CompleteStreaming();

        // Assert
        Assert.False(vm.IsStreaming);
    }

    #endregion

    #region MarkAsCancelled Tests

    /// <summary>
    /// Verifies that MarkAsCancelled sets IsStreaming to false.
    /// </summary>
    [Fact]
    public void MarkAsCancelled_SetsIsStreamingToFalse()
    {
        // Arrange
        var vm = new ChatMessageViewModel { IsStreaming = true };

        // Act
        vm.MarkAsCancelled();

        // Assert
        Assert.False(vm.IsStreaming);
    }

    /// <summary>
    /// Verifies that MarkAsCancelled appends [Cancelled] marker to content.
    /// </summary>
    [Fact]
    public void MarkAsCancelled_WithContent_AppendsCancelledMarker()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            Content = "Partial response",
            IsStreaming = true
        };

        // Act
        vm.MarkAsCancelled();

        // Assert
        Assert.EndsWith(" [Cancelled]", vm.Content);
    }

    /// <summary>
    /// Verifies that MarkAsCancelled does not add marker to empty content.
    /// </summary>
    [Fact]
    public void MarkAsCancelled_WithEmptyContent_DoesNotAddMarker()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            Content = string.Empty,
            IsStreaming = true
        };

        // Act
        vm.MarkAsCancelled();

        // Assert
        Assert.Equal(string.Empty, vm.Content);
    }

    /// <summary>
    /// Verifies that MarkAsCancelled does not add marker if content ends with ellipsis.
    /// </summary>
    [Fact]
    public void MarkAsCancelled_WithEllipsisEnding_DoesNotAddMarker()
    {
        // Arrange
        var vm = new ChatMessageViewModel
        {
            Content = "Thinking...",
            IsStreaming = true
        };

        // Act
        vm.MarkAsCancelled();

        // Assert
        Assert.Equal("Thinking...", vm.Content);
    }

    #endregion

    #region ToChatMessage Tests

    /// <summary>
    /// Verifies that ToChatMessage creates a ChatMessage with all properties.
    /// </summary>
    [Fact]
    public void ToChatMessage_CreatesCorrectDomainModel()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var vm = new ChatMessageViewModel
        {
            Id = id,
            Content = "Test message",
            Role = MessageRole.Assistant,
            Timestamp = timestamp,
            IsStreaming = false,
            TokenCount = 42,
            GenerationTime = TimeSpan.FromSeconds(2)
        };

        // Act
        var message = vm.ToChatMessage();

        // Assert
        Assert.Equal(id, message.Id);
        Assert.Equal("Test message", message.Content);
        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.Equal(timestamp, message.Timestamp);
        Assert.True(message.IsComplete); // IsStreaming=false means IsComplete=true
        Assert.Equal(42, message.TokenCount);
        Assert.Equal(TimeSpan.FromSeconds(2), message.GenerationTime);
    }

    /// <summary>
    /// Verifies that ToChatMessage sets IsComplete=false when IsStreaming=true.
    /// </summary>
    [Fact]
    public void ToChatMessage_WithStreaming_SetsIsCompleteFalse()
    {
        // Arrange
        var vm = new ChatMessageViewModel { IsStreaming = true };

        // Act
        var message = vm.ToChatMessage();

        // Assert
        Assert.False(message.IsComplete);
    }

    #endregion

    #region Property Change Notification Tests

    /// <summary>
    /// Verifies that Content property change fires PropertyChanged event.
    /// </summary>
    [Fact]
    public void Content_Changed_NotifiesPropertyChanged()
    {
        // Arrange
        var vm = new ChatMessageViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act
        vm.Content = "New content";

        // Assert
        Assert.Contains(nameof(ChatMessageViewModel.Content), changedProperties);
    }

    /// <summary>
    /// Verifies that IsStreaming property change fires PropertyChanged event.
    /// </summary>
    [Fact]
    public void IsStreaming_Changed_NotifiesPropertyChanged()
    {
        // Arrange
        var vm = new ChatMessageViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act
        vm.IsStreaming = true;

        // Assert
        Assert.Contains(nameof(ChatMessageViewModel.IsStreaming), changedProperties);
    }

    /// <summary>
    /// Verifies that Role property change fires PropertyChanged event.
    /// </summary>
    [Fact]
    public void Role_Changed_NotifiesPropertyChanged()
    {
        // Arrange
        var vm = new ChatMessageViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act
        vm.Role = MessageRole.Assistant;

        // Assert
        Assert.Contains(nameof(ChatMessageViewModel.Role), changedProperties);
    }

    #endregion
}
