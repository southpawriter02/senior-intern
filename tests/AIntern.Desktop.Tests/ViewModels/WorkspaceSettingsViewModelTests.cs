using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Tests for WorkspaceSettingsViewModel.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.5a.</para>
/// </remarks>
public class WorkspaceSettingsViewModelTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<WorkspaceSettingsViewModel>> _mockLogger;

    public WorkspaceSettingsViewModelTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<WorkspaceSettingsViewModel>>();
    }

    private WorkspaceSettingsViewModel CreateViewModel(AppSettings? settings = null)
    {
        var appSettings = settings ?? new AppSettings();
        _mockSettingsService.Setup(s => s.CurrentSettings).Returns(appSettings);
        return new WorkspaceSettingsViewModel(_mockSettingsService.Object, _mockLogger.Object);
    }

    #region LoadFromSettings Tests

    [Fact]
    public void Constructor_LoadsFileExplorerSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            RestoreLastWorkspace = false,
            ShowHiddenFiles = true,
            UseGitIgnore = false,
            MaxRecentWorkspaces = 25
        };

        // Act
        var viewModel = CreateViewModel(settings);

        // Assert
        Assert.False(viewModel.RestoreLastWorkspace);
        Assert.True(viewModel.ShowHiddenFiles);
        Assert.False(viewModel.UseGitIgnore);
        Assert.Equal(25, viewModel.MaxRecentWorkspaces);
    }

    [Fact]
    public void Constructor_LoadsEditorSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            EditorFontFamily = "JetBrains Mono",
            EditorFontSize = 16,
            TabSize = 2,
            ConvertTabsToSpaces = false,
            ShowLineNumbers = false
        };

        // Act
        var viewModel = CreateViewModel(settings);

        // Assert
        Assert.Equal("JetBrains Mono", viewModel.EditorFontFamily);
        Assert.Equal(16, viewModel.EditorFontSize);
        Assert.Equal(2, viewModel.TabSize);
        Assert.False(viewModel.ConvertTabsToSpaces);
        Assert.False(viewModel.ShowLineNumbers);
    }

    [Fact]
    public void Constructor_LoadsContextSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            ContextLimits = new ContextLimitsConfig
            {
                MaxFilesAttached = 20,
                MaxTokensPerFile = 8000,
                MaxTotalContextTokens = 16000,
                MaxFileSizeBytes = 1024 * 1024 // 1MB
            },
            ShowTokenCount = false,
            WarnOnTokenLimit = false
        };

        // Act
        var viewModel = CreateViewModel(settings);

        // Assert
        Assert.Equal(20, viewModel.MaxFilesAttached);
        Assert.Equal(8000, viewModel.MaxTokensPerFile);
        Assert.Equal(16000, viewModel.MaxTotalContextTokens);
        Assert.Equal(1024, viewModel.MaxFileSizeKb);
        Assert.False(viewModel.ShowTokenCount);
        Assert.False(viewModel.WarnOnTokenLimit);
    }

    #endregion

    #region HasChanges Tests

    [Fact]
    public void HasChanges_InitiallyFalse()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.HasChanges);
    }

    [Fact]
    public void HasChanges_TrueAfterPropertyChange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        Assert.False(viewModel.HasChanges);

        // Act
        viewModel.TabSize = 8;

        // Assert
        Assert.True(viewModel.HasChanges);
    }

    [Fact]
    public void HasChanges_TrackedForSettingProperties()
    {
        // Arrange
        var viewModel = CreateViewModel();
        Assert.False(viewModel.HasChanges);

        // Act - Setting any property to different value triggers HasChanges
        var originalValue = viewModel.TabSize;
        viewModel.TabSize = originalValue == 4 ? 2 : 4;

        // Assert - HasChanges becomes true because property changed
        Assert.True(viewModel.HasChanges);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_FailsWhenMaxTokensPerFileExceedsTotal()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.MaxTokensPerFile = 10000;
        viewModel.MaxTotalContextTokens = 5000;

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("Max tokens per file cannot exceed max total tokens.", viewModel.ValidationError);
    }

    [Fact]
    public void Validate_FailsWhenFontSizeOutOfRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.EditorFontSize = 100;

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("Font size must be between 8 and 72.", viewModel.ValidationError);
    }

    [Fact]
    public void Validate_FailsWhenMaxRecentWorkspacesOutOfRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.MaxRecentWorkspaces = 100;

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("Recent workspaces must be between 1 and 50.", viewModel.ValidationError);
    }

    [Fact]
    public void Validate_FailsWhenTabSizeOutOfRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TabSize = 32;

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("Tab size must be between 1 and 16.", viewModel.ValidationError);
    }

    [Fact]
    public void Validate_FailsWhenMaxFileSizeOutOfRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.MaxFileSizeKb = 50000;

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("Max file size must be between 1 and 10000 KB.", viewModel.ValidationError);
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_SavesSettings()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TabSize = 8;
        AppSettings? savedSettings = null;
        _mockSettingsService
            .Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>()))
            .Callback<AppSettings>((s) => savedSettings = s)
            .Returns(Task.CompletedTask);

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(savedSettings);
        Assert.Equal(8, savedSettings.TabSize);
    }

    [Fact]
    public async Task SaveAsync_RaisesSaveCompletedEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TabSize = 8;
        var eventRaised = false;
        viewModel.SaveCompleted += (_, _) => eventRaised = true;

        _mockSettingsService
            .Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>()))
            .Returns(Task.CompletedTask);

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion

    #region ResetToDefaults Tests

    [Fact]
    public void ResetToDefaults_LoadsDefaultValues()
    {
        // Arrange
        var settings = new AppSettings { TabSize = 8, EditorFontSize = 20 };
        var viewModel = CreateViewModel(settings);

        // Act
        viewModel.ResetToDefaultsCommand.Execute(null);

        // Assert
        Assert.Equal(4, viewModel.TabSize);
        Assert.Equal(14, viewModel.EditorFontSize);
    }

    [Fact]
    public void ResetToDefaults_SetsHasChanges()
    {
        // Arrange
        var viewModel = CreateViewModel();
        Assert.False(viewModel.HasChanges);

        // Act
        viewModel.ResetToDefaultsCommand.Execute(null);

        // Assert
        Assert.True(viewModel.HasChanges);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_RevertsToOriginalSettings()
    {
        // Arrange
        var settings = new AppSettings { TabSize = 8 };
        var viewModel = CreateViewModel(settings);
        viewModel.TabSize = 2;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.Equal(8, viewModel.TabSize);
    }

    [Fact]
    public void Cancel_RaisesCancelRequestedEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.CancelRequested += (_, _) => eventRaised = true;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion

    #region Selection Options Tests

    [Fact]
    public void SelectionOptions_ArePopulated()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotEmpty(viewModel.AvailableFonts);
        Assert.NotEmpty(viewModel.AvailableThemes);
        Assert.NotEmpty(viewModel.TabSizeOptions);
        Assert.NotEmpty(viewModel.FontSizeOptions);
        Assert.NotEmpty(viewModel.RulerColumnOptions);
    }

    #endregion
}
