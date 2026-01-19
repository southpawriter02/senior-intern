// ============================================================================
// File: TerminalSettingsViewModelTests.cs
// Path: tests/AIntern.Desktop.Tests/ViewModels/TerminalSettingsViewModelTests.cs
// Description: Unit tests for TerminalSettingsViewModel.
// Created: 2026-01-19
// AI Intern v0.5.5f - Terminal Settings Panel
// ============================================================================

namespace AIntern.Desktop.Tests.ViewModels;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="TerminalSettingsViewModel"/>.
/// </summary>
public class TerminalSettingsViewModelTests
{
    #region Test Fixtures

    private readonly Mock<ILogger<TerminalSettingsViewModel>> _mockLogger;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IFontService> _mockFontService;
    private readonly Mock<IShellDetectionService> _mockShellDetection;
    private readonly Mock<IShellProfileService> _mockProfileService;
    private readonly AppSettings _testSettings;

    public TerminalSettingsViewModelTests()
    {
        _mockLogger = new Mock<ILogger<TerminalSettingsViewModel>>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockFontService = new Mock<IFontService>();
        _mockShellDetection = new Mock<IShellDetectionService>();
        _mockProfileService = new Mock<IShellProfileService>();

        _testSettings = new AppSettings
        {
            TerminalFontFamily = "Consolas",
            TerminalFontSize = 14,
            TerminalTheme = "Dark",
            TerminalCursorStyle = TerminalCursorStyle.Block,
            TerminalCursorBlink = true,
            TerminalScrollbackLines = 10000,
            TerminalBellStyle = TerminalBellStyle.Visual,
            TerminalCopyOnSelect = false,
            SyncTerminalWithWorkspace = true
        };

        _mockSettingsService.Setup(s => s.CurrentSettings).Returns(_testSettings);
        _mockFontService.Setup(f => f.GetMonospaceFonts())
            .Returns(new[] { "Consolas", "Courier New" });
        _mockFontService.Setup(f => f.GetBestAvailableFont(It.IsAny<string>()))
            .Returns("Consolas");
        _mockShellDetection.Setup(s => s.GetAvailableShellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ShellInfo>());
        _mockShellDetection.Setup(s => s.GetDefaultShellAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("/bin/bash");
        _mockShellDetection.Setup(s => s.DetectShellType(It.IsAny<string>()))
            .Returns(ShellType.Bash);
    }

    private TerminalSettingsViewModel CreateViewModel() => new(
        _mockLogger.Object,
        _mockSettingsService.Object,
        _mockFontService.Object,
        _mockShellDetection.Object,
        _mockProfileService.Object);

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TerminalSettingsViewModel(
                null!,
                _mockSettingsService.Object,
                _mockFontService.Object,
                _mockShellDetection.Object,
                _mockProfileService.Object));
    }

    [Fact]
    public void Constructor_ThrowsOnNullSettingsService()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TerminalSettingsViewModel(
                _mockLogger.Object,
                null!,
                _mockFontService.Object,
                _mockShellDetection.Object,
                _mockProfileService.Object));
    }

    [Fact]
    public void Constructor_CreatesWithValidDependencies()
    {
        var vm = CreateViewModel();
        Assert.NotNull(vm);
    }

    #endregion

    #region Initialize Tests

    [Fact]
    public async Task InitializeAsync_LoadsFonts()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.Contains("Consolas", vm.AvailableFonts);
        Assert.Contains("Courier New", vm.AvailableFonts);
    }

    [Fact]
    public async Task InitializeAsync_LoadsThemes()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.Contains("Dark", vm.AvailableThemes);
        Assert.Contains("Light", vm.AvailableThemes);
    }

    [Fact]
    public async Task InitializeAsync_ClearsUnsavedChanges()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.False(vm.HasUnsavedChanges);
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public async Task PropertyChange_SetsUnsavedChanges()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Act
        vm.SelectedFontSize = 18;

        // Assert
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task PropertyChange_RaisesPreviewEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        TerminalSettings? previewSettings = null;
        vm.PreviewSettingsChanged += (s, e) => previewSettings = e;

        // Act
        vm.SelectedFontSize = 18;

        // Assert
        Assert.NotNull(previewSettings);
        Assert.Equal(18, previewSettings!.FontSize);
    }

    #endregion

    #region Command Tests

    [Fact]
    public async Task ResetToDefaults_AppliesDefaultValues()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        vm.SelectedFontSize = 20;

        // Act
        vm.ResetToDefaultsCommand.Execute(null);

        // Assert
        Assert.Equal(14, vm.SelectedFontSize);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task DiscardChanges_ReloadsSettings()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        vm.SelectedFontSize = 20;

        // Act
        vm.DiscardChangesCommand.Execute(null);

        // Assert
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task AddProfileAsync_CreatesNewProfile()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        var initialCount = vm.ShellProfiles.Count;

        // Act
        await vm.AddProfileCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(initialCount + 1, vm.ShellProfiles.Count);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task DeleteProfile_RemovesCustomProfile()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        await vm.AddProfileCommand.ExecuteAsync(null);
        var profile = vm.ShellProfiles.First(p => !p.IsBuiltIn);
        var countBefore = vm.ShellProfiles.Count;

        // Act
        vm.DeleteProfileCommand.Execute(profile);

        // Assert
        Assert.Equal(countBefore - 1, vm.ShellProfiles.Count);
    }

    [Fact]
    public async Task DeleteProfile_ProtectsBuiltInProfiles()
    {
        // Arrange
        _mockShellDetection.Setup(s => s.GetAvailableShellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ShellInfo { Name = "Bash", Path = "/bin/bash", ShellType = ShellType.Bash }
            });

        var vm = CreateViewModel();
        await vm.InitializeAsync();
        var builtInProfile = vm.ShellProfiles.FirstOrDefault(p => p.IsBuiltIn);

        if (builtInProfile != null)
        {
            var countBefore = vm.ShellProfiles.Count;

            // Act
            vm.DeleteProfileCommand.Execute(builtInProfile);

            // Assert - count should remain the same
            Assert.Equal(countBefore, vm.ShellProfiles.Count);
        }
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task Validate_DetectsInvalidFontSize()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Act
        vm.SelectedFontSize = 5; // Below minimum

        // Assert
        Assert.NotNull(vm.ValidationError);
    }

    #endregion
}
