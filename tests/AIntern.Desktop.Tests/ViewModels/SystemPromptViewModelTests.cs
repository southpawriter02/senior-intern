using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for SystemPromptViewModel (v0.2.4c).
/// Tests constructor mapping, computed properties, and property change notifications.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor properly maps domain model properties</description></item>
///   <item><description>Computed properties calculate correct values</description></item>
///   <item><description>ContentPreview handles truncation and newlines</description></item>
///   <item><description>TypeLabel returns correct value based on IsBuiltIn</description></item>
///   <item><description>CategoryIcon returns correct icon key based on category</description></item>
///   <item><description>Property change notifications fire correctly</description></item>
/// </list>
/// <para>Added in v0.2.5a (test coverage for v0.2.4c).</para>
/// </remarks>
public class SystemPromptViewModelTests
{
    #region Test Helpers

    private static SystemPrompt CreateTestPrompt(
        string name = "Test Prompt",
        string content = "You are a helpful assistant.",
        string? description = "Test description",
        string category = "General",
        bool isBuiltIn = false,
        bool isDefault = false,
        int usageCount = 0)
    {
        return new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = name,
            Content = content,
            Description = description,
            Category = category,
            IsBuiltIn = isBuiltIn,
            IsDefault = isDefault,
            IsActive = true,
            UsageCount = usageCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies default constructor creates empty ViewModel.
    /// </summary>
    [Fact]
    public void Constructor_Default_CreatesEmptyViewModel()
    {
        // Act
        var vm = new SystemPromptViewModel();

        // Assert
        Assert.Equal(Guid.Empty, vm.Id);
        Assert.Equal(string.Empty, vm.Name);
        Assert.Equal(string.Empty, vm.Content);
        Assert.Null(vm.Description);
        Assert.Equal("General", vm.Category);
        Assert.False(vm.IsBuiltIn);
        Assert.False(vm.IsDefault);
        Assert.False(vm.IsSelected);
        Assert.Equal(0, vm.UsageCount);
    }

    /// <summary>
    /// Verifies constructor with domain model maps all properties.
    /// </summary>
    [Fact]
    public void Constructor_WithDomainModel_MapsAllProperties()
    {
        // Arrange
        var prompt = CreateTestPrompt(
            name: "The Senior Intern",
            content: "You are a senior intern...",
            description: "An experienced intern persona",
            category: "Code",
            isBuiltIn: true,
            isDefault: true,
            usageCount: 42);

        // Act
        var vm = new SystemPromptViewModel(prompt);

        // Assert
        Assert.Equal(prompt.Id, vm.Id);
        Assert.Equal("The Senior Intern", vm.Name);
        Assert.Equal("You are a senior intern...", vm.Content);
        Assert.Equal("An experienced intern persona", vm.Description);
        Assert.Equal("Code", vm.Category);
        Assert.True(vm.IsBuiltIn);
        Assert.True(vm.IsDefault);
        Assert.Equal(42, vm.UsageCount);
    }

    /// <summary>
    /// Verifies constructor throws for null domain model.
    /// </summary>
    [Fact]
    public void Constructor_NullDomainModel_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SystemPromptViewModel(null!));
    }

    /// <summary>
    /// Verifies constructor handles null description.
    /// </summary>
    [Fact]
    public void Constructor_NullDescription_SetsNullDescription()
    {
        // Arrange
        var prompt = CreateTestPrompt(description: null);

        // Act
        var vm = new SystemPromptViewModel(prompt);

        // Assert
        Assert.Null(vm.Description);
    }

    #endregion

    #region CharacterCount Tests

    /// <summary>
    /// Verifies CharacterCount returns content length.
    /// </summary>
    [Fact]
    public void CharacterCount_WithContent_ReturnsLength()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = "Hello World" }; // 11 chars

        // Assert
        Assert.Equal(11, vm.CharacterCount);
    }

    /// <summary>
    /// Verifies CharacterCount returns 0 for empty content.
    /// </summary>
    [Fact]
    public void CharacterCount_EmptyContent_ReturnsZero()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = string.Empty };

        // Assert
        Assert.Equal(0, vm.CharacterCount);
    }

    /// <summary>
    /// Verifies CharacterCount returns 0 for null-like content.
    /// </summary>
    [Fact]
    public void CharacterCount_NullContent_ReturnsZero()
    {
        // Arrange
        var vm = new SystemPromptViewModel();
        // Content is initialized to string.Empty by default

        // Assert
        Assert.Equal(0, vm.CharacterCount);
    }

    #endregion

    #region EstimatedTokenCount Tests

    /// <summary>
    /// Verifies EstimatedTokenCount calculates correctly (~4 chars per token).
    /// </summary>
    [Fact]
    public void EstimatedTokenCount_WithContent_CalculatesCorrectly()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = new string('a', 100) }; // 100 chars

        // Assert - 100 / 4 = 25 tokens
        Assert.Equal(25, vm.EstimatedTokenCount);
    }

    /// <summary>
    /// Verifies EstimatedTokenCount returns 0 for empty content.
    /// </summary>
    [Fact]
    public void EstimatedTokenCount_EmptyContent_ReturnsZero()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = string.Empty };

        // Assert
        Assert.Equal(0, vm.EstimatedTokenCount);
    }

    /// <summary>
    /// Verifies EstimatedTokenCount handles integer division correctly.
    /// </summary>
    [Fact]
    public void EstimatedTokenCount_IntegerDivision_TruncatesCorrectly()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = "abc" }; // 3 chars

        // Assert - 3 / 4 = 0 (integer division)
        Assert.Equal(0, vm.EstimatedTokenCount);
    }

    #endregion

    #region ContentPreview Tests

    /// <summary>
    /// Verifies ContentPreview returns full content for short text.
    /// </summary>
    [Fact]
    public void ContentPreview_ShortContent_ReturnsFullContent()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = "Short content" };

        // Assert
        Assert.Equal("Short content", vm.ContentPreview);
    }

    /// <summary>
    /// Verifies ContentPreview truncates long content with ellipsis.
    /// </summary>
    [Fact]
    public void ContentPreview_LongContent_TruncatesWithEllipsis()
    {
        // Arrange
        var longContent = new string('a', 200);
        var vm = new SystemPromptViewModel { Content = longContent };

        // Assert - 100 chars + "..."
        Assert.Equal(103, vm.ContentPreview.Length);
        Assert.EndsWith("...", vm.ContentPreview);
    }

    /// <summary>
    /// Verifies ContentPreview replaces newlines with spaces.
    /// </summary>
    [Fact]
    public void ContentPreview_WithNewlines_ReplacesWithSpaces()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = "Line 1\nLine 2\rLine 3\r\nLine 4" };

        // Assert - all newlines replaced with single spaces
        Assert.DoesNotContain("\n", vm.ContentPreview);
        Assert.DoesNotContain("\r", vm.ContentPreview);
        Assert.Equal("Line 1 Line 2 Line 3 Line 4", vm.ContentPreview);
    }

    /// <summary>
    /// Verifies ContentPreview returns empty for whitespace-only content.
    /// </summary>
    [Fact]
    public void ContentPreview_WhitespaceContent_ReturnsEmpty()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = "   \n\t  " };

        // Assert
        Assert.Equal(string.Empty, vm.ContentPreview);
    }

    /// <summary>
    /// Verifies ContentPreview returns empty for empty content.
    /// </summary>
    [Fact]
    public void ContentPreview_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = string.Empty };

        // Assert
        Assert.Equal(string.Empty, vm.ContentPreview);
    }

    /// <summary>
    /// Verifies ContentPreview trims leading and trailing whitespace.
    /// </summary>
    [Fact]
    public void ContentPreview_TrimsWhitespace()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = "  Hello World  " };

        // Assert
        Assert.Equal("Hello World", vm.ContentPreview);
    }

    /// <summary>
    /// Verifies ContentPreview collapses multiple consecutive spaces.
    /// </summary>
    [Fact]
    public void ContentPreview_CollapsesMultipleSpaces()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Content = "Word1    Word2     Word3" };

        // Assert
        Assert.Equal("Word1 Word2 Word3", vm.ContentPreview);
    }

    /// <summary>
    /// Verifies ContentPreview handles exactly 100 characters.
    /// </summary>
    [Fact]
    public void ContentPreview_Exactly100Chars_NoTruncation()
    {
        // Arrange
        var content = new string('x', 100);
        var vm = new SystemPromptViewModel { Content = content };

        // Assert - exactly 100 chars, no ellipsis
        Assert.Equal(100, vm.ContentPreview.Length);
        Assert.DoesNotContain("...", vm.ContentPreview);
    }

    /// <summary>
    /// Verifies ContentPreview handles 101 characters.
    /// </summary>
    [Fact]
    public void ContentPreview_101Chars_Truncates()
    {
        // Arrange
        var content = new string('x', 101);
        var vm = new SystemPromptViewModel { Content = content };

        // Assert - truncated to 100 + "..."
        Assert.Equal(103, vm.ContentPreview.Length);
        Assert.EndsWith("...", vm.ContentPreview);
    }

    #endregion

    #region TypeLabel Tests

    /// <summary>
    /// Verifies TypeLabel returns "Template" for built-in prompts.
    /// </summary>
    [Fact]
    public void TypeLabel_BuiltIn_ReturnsTemplate()
    {
        // Arrange
        var vm = new SystemPromptViewModel { IsBuiltIn = true };

        // Assert
        Assert.Equal("Template", vm.TypeLabel);
    }

    /// <summary>
    /// Verifies TypeLabel returns "Custom" for user prompts.
    /// </summary>
    [Fact]
    public void TypeLabel_NotBuiltIn_ReturnsCustom()
    {
        // Arrange
        var vm = new SystemPromptViewModel { IsBuiltIn = false };

        // Assert
        Assert.Equal("Custom", vm.TypeLabel);
    }

    #endregion

    #region CategoryIcon Tests

    /// <summary>
    /// Verifies CategoryIcon returns "CodeIcon" for Code category.
    /// </summary>
    [Fact]
    public void CategoryIcon_Code_ReturnsCodeIcon()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Category = "Code" };

        // Assert
        Assert.Equal("CodeIcon", vm.CategoryIcon);
    }

    /// <summary>
    /// Verifies CategoryIcon returns "PaletteIcon" for Creative category.
    /// </summary>
    [Fact]
    public void CategoryIcon_Creative_ReturnsPaletteIcon()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Category = "Creative" };

        // Assert
        Assert.Equal("PaletteIcon", vm.CategoryIcon);
    }

    /// <summary>
    /// Verifies CategoryIcon returns "DocumentIcon" for Technical category.
    /// </summary>
    [Fact]
    public void CategoryIcon_Technical_ReturnsDocumentIcon()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Category = "Technical" };

        // Assert
        Assert.Equal("DocumentIcon", vm.CategoryIcon);
    }

    /// <summary>
    /// Verifies CategoryIcon returns "ChatIcon" for General category.
    /// </summary>
    [Fact]
    public void CategoryIcon_General_ReturnsChatIcon()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Category = "General" };

        // Assert
        Assert.Equal("ChatIcon", vm.CategoryIcon);
    }

    /// <summary>
    /// Verifies CategoryIcon returns "PromptIcon" for unknown category.
    /// </summary>
    [Fact]
    public void CategoryIcon_UnknownCategory_ReturnsPromptIcon()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Category = "Unknown" };

        // Assert
        Assert.Equal("PromptIcon", vm.CategoryIcon);
    }

    /// <summary>
    /// Verifies CategoryIcon returns "PromptIcon" for empty category.
    /// </summary>
    [Fact]
    public void CategoryIcon_EmptyCategory_ReturnsPromptIcon()
    {
        // Arrange
        var vm = new SystemPromptViewModel { Category = string.Empty };

        // Assert
        Assert.Equal("PromptIcon", vm.CategoryIcon);
    }

    #endregion

    #region Property Change Notification Tests

    /// <summary>
    /// Verifies OnContentChanged fires CharacterCount notification.
    /// </summary>
    [Fact]
    public void OnContentChanged_FiresCharacterCountNotification()
    {
        // Arrange
        var vm = new SystemPromptViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        vm.Content = "New content";

        // Assert
        Assert.Contains(nameof(SystemPromptViewModel.CharacterCount), changedProperties);
    }

    /// <summary>
    /// Verifies OnContentChanged fires EstimatedTokenCount notification.
    /// </summary>
    [Fact]
    public void OnContentChanged_FiresEstimatedTokenCountNotification()
    {
        // Arrange
        var vm = new SystemPromptViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        vm.Content = "New content";

        // Assert
        Assert.Contains(nameof(SystemPromptViewModel.EstimatedTokenCount), changedProperties);
    }

    /// <summary>
    /// Verifies OnContentChanged fires ContentPreview notification.
    /// </summary>
    [Fact]
    public void OnContentChanged_FiresContentPreviewNotification()
    {
        // Arrange
        var vm = new SystemPromptViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        vm.Content = "New content";

        // Assert
        Assert.Contains(nameof(SystemPromptViewModel.ContentPreview), changedProperties);
    }

    /// <summary>
    /// Verifies OnCategoryChanged fires CategoryIcon notification.
    /// </summary>
    [Fact]
    public void OnCategoryChanged_FiresCategoryIconNotification()
    {
        // Arrange
        var vm = new SystemPromptViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        vm.Category = "Code";

        // Assert
        Assert.Contains(nameof(SystemPromptViewModel.CategoryIcon), changedProperties);
    }

    /// <summary>
    /// Verifies OnIsBuiltInChanged fires TypeLabel notification.
    /// </summary>
    [Fact]
    public void OnIsBuiltInChanged_FiresTypeLabelNotification()
    {
        // Arrange
        var vm = new SystemPromptViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        vm.IsBuiltIn = true;

        // Assert
        Assert.Contains(nameof(SystemPromptViewModel.TypeLabel), changedProperties);
    }

    #endregion

    #region Observable Properties Tests

    /// <summary>
    /// Verifies Name property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Name_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var vm = new SystemPromptViewModel();

        // Act
        vm.Name = "My Prompt";

        // Assert
        Assert.Equal("My Prompt", vm.Name);
    }

    /// <summary>
    /// Verifies Description property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Description_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var vm = new SystemPromptViewModel();

        // Act
        vm.Description = "A helpful prompt";

        // Assert
        Assert.Equal("A helpful prompt", vm.Description);
    }

    /// <summary>
    /// Verifies IsSelected property can be set and retrieved.
    /// </summary>
    [Fact]
    public void IsSelected_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var vm = new SystemPromptViewModel();

        // Act
        vm.IsSelected = true;

        // Assert
        Assert.True(vm.IsSelected);
    }

    /// <summary>
    /// Verifies IsDefault property can be set and retrieved.
    /// </summary>
    [Fact]
    public void IsDefault_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var vm = new SystemPromptViewModel();

        // Act
        vm.IsDefault = true;

        // Assert
        Assert.True(vm.IsDefault);
    }

    /// <summary>
    /// Verifies UsageCount property can be set and retrieved.
    /// </summary>
    [Fact]
    public void UsageCount_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var vm = new SystemPromptViewModel();

        // Act
        vm.UsageCount = 99;

        // Assert
        Assert.Equal(99, vm.UsageCount);
    }

    #endregion
}
