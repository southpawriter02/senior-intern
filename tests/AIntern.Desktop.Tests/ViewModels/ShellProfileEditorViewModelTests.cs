// ============================================================================
// File: ShellProfileEditorViewModelTests.cs
// Path: tests/AIntern.Desktop.Tests/ViewModels/ShellProfileEditorViewModelTests.cs
// Description: Unit tests for ShellProfileEditorViewModel.
// Created: 2026-01-19
// AI Intern v0.5.5g - Shell Profile Editor
// ============================================================================

namespace AIntern.Desktop.Tests.ViewModels;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// ═══════════════════════════════════════════════════════════════════════════════
// ShellProfileEditorViewModelTests
// ═══════════════════════════════════════════════════════════════════════════════
//
// Comprehensive unit tests for the Shell Profile Editor ViewModel.
// Tests cover:
//   - Constructor validation
//   - Profile loading (new and existing)
//   - Validation logic (name, path)
//   - Shell type and version detection
//   - Profile building and retrieval
//   - Command execution
//   - Environment variable parsing
//
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Unit tests for <see cref="ShellProfileEditorViewModel"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.5g.</para>
/// </remarks>
public class ShellProfileEditorViewModelTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Test Fixtures
    // ═══════════════════════════════════════════════════════════════════════════

    private readonly Mock<IShellDetectionService> _mockShellDetection;
    private readonly Mock<ILogger<ShellProfileEditorViewModel>> _mockLogger;

    /// <summary>
    /// Initializes test fixtures with default mock setup.
    /// </summary>
    public ShellProfileEditorViewModelTests()
    {
        _mockShellDetection = new Mock<IShellDetectionService>();
        _mockLogger = new Mock<ILogger<ShellProfileEditorViewModel>>();

        // ─────────────────────────────────────────────────────────────────────
        // Default mock behavior
        // ─────────────────────────────────────────────────────────────────────
        _mockShellDetection
            .Setup(s => s.DetectShellType(It.IsAny<string>()))
            .Returns(ShellType.Bash);

        _mockShellDetection
            .Setup(s => s.ValidateShellPath(It.IsAny<string>()))
            .Returns(true);

        _mockShellDetection
            .Setup(s => s.GetShellVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("5.1.0");

        _mockShellDetection
            .Setup(s => s.GetDefaultShellAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("/bin/bash");

        _mockShellDetection
            .Setup(s => s.FindInPath(It.IsAny<string>()))
            .Returns("/usr/bin/bash");
    }

    /// <summary>
    /// Creates a ViewModel instance with default mock dependencies.
    /// </summary>
    private ShellProfileEditorViewModel CreateViewModel() =>
        new(_mockShellDetection.Object, _mockLogger.Object);

    /// <summary>
    /// Creates a test ShellProfile with standard values.
    /// </summary>
    private static ShellProfile CreateTestProfile() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Profile",
        ShellPath = "/bin/zsh",
        ShellType = ShellType.Zsh,
        Arguments = "-l",  // -l indicates login shell
        StartingDirectory = "/home/user",
        Environment = new Dictionary<string, string>
        {
            ["TERM"] = "xterm-256color",
            ["EDITOR"] = "vim"
        },
        IsBuiltIn = false
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region Constructor Tests

    [Fact]
    public void Constructor_NullShellDetectionService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ShellProfileEditorViewModel(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_DoesNotThrow()
    {
        // Logger is optional - null is acceptable
        // Act
        var vm = new ShellProfileEditorViewModel(_mockShellDetection.Object, null);

        // Assert
        Assert.NotNull(vm);
    }

    [Fact]
    public void Constructor_ValidDependencies_CreatesInstance()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm);
        Assert.False(vm.IsEditMode);
        Assert.False(vm.IsBuiltIn);
        Assert.NotNull(vm.ShellTypes);
        Assert.Contains(ShellType.Bash, vm.ShellTypes);
    }

    [Fact]
    public void Constructor_InitializesShellTypesList()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotEmpty(vm.ShellTypes);
        Assert.Contains(ShellType.Bash, vm.ShellTypes);
        Assert.Contains(ShellType.Zsh, vm.ShellTypes);
        Assert.Contains(ShellType.PowerShell, vm.ShellTypes);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // LoadProfile Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region LoadProfile Tests

    [Fact]
    public void LoadProfile_PopulatesAllFields()
    {
        // Arrange
        var vm = CreateViewModel();
        var profile = CreateTestProfile();

        // Act
        vm.LoadProfile(profile);

        // Assert
        Assert.Equal(profile.Name, vm.ProfileName);
        Assert.Equal(profile.ShellPath, vm.ShellPath);
        Assert.Equal(profile.ShellType, vm.ShellType);
        Assert.Equal(profile.Arguments, vm.Arguments);
        Assert.Equal(profile.StartingDirectory, vm.WorkingDirectory);
        // IsLoginShell is derived from Arguments containing "-l"
        Assert.True(vm.IsLoginShell);
    }

    [Fact]
    public void LoadProfile_SetsIsEditModeTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        var profile = CreateTestProfile();

        // Act
        vm.LoadProfile(profile);

        // Assert
        Assert.True(vm.IsEditMode);
    }

    [Fact]
    public void LoadProfile_BuiltInProfile_SetsIsBuiltInTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        var profile = new ShellProfile
        {
            Id = Guid.NewGuid(),
            Name = "Built-in Profile",
            ShellPath = "/bin/bash",
            ShellType = ShellType.Bash,
            IsBuiltIn = true
        };

        // Act
        vm.LoadProfile(profile);

        // Assert
        Assert.True(vm.IsBuiltIn);
    }

    [Fact]
    public void LoadProfile_ParsesEnvironmentVariablesCorrectly()
    {
        // Arrange
        var vm = CreateViewModel();
        var profile = CreateTestProfile();

        // Act
        vm.LoadProfile(profile);

        // Assert
        Assert.Contains("TERM=xterm-256color", vm.EnvironmentVariables);
        Assert.Contains("EDITOR=vim", vm.EnvironmentVariables);
    }

    [Fact]
    public void LoadProfile_PreservesProfileId()
    {
        // Arrange
        var vm = CreateViewModel();
        var profile = CreateTestProfile();
        var originalId = profile.Id;

        // Act
        vm.LoadProfile(profile);
        var result = vm.GetProfile();

        // Assert
        Assert.Equal(originalId, result.Id);
    }

    [Fact]
    public void LoadProfile_EmptyEnvironment_HandlesGracefully()
    {
        // Arrange
        var vm = CreateViewModel();
        var profile = new ShellProfile
        {
            Id = Guid.NewGuid(),
            Name = "Profile with Empty Env",
            ShellPath = "/bin/bash",
            ShellType = ShellType.Bash,
            Environment = new Dictionary<string, string>()
        };

        // Act
        vm.LoadProfile(profile);

        // Assert
        Assert.True(string.IsNullOrEmpty(vm.EnvironmentVariables));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // InitializeNewProfileAsync Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region InitializeNewProfileAsync Tests

    [Fact]
    public async Task InitializeNewProfileAsync_SetsDefaultShellPath()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeNewProfileAsync();

        // Assert
        Assert.Equal("/bin/bash", vm.ShellPath);
    }

    [Fact]
    public async Task InitializeNewProfileAsync_DetectsShellType()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeNewProfileAsync();

        // Assert
        Assert.Equal(ShellType.Bash, vm.ShellType);
        _mockShellDetection.Verify(s => s.DetectShellType("/bin/bash"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task InitializeNewProfileAsync_SetsIsEditModeFalse()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeNewProfileAsync();

        // Assert
        Assert.False(vm.IsEditMode);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region Validation Tests

    [Fact]
    public void Validate_EmptyName_SetsNameError()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = string.Empty;
        vm.ShellPath = "/bin/bash";

        // Assert
        Assert.False(vm.IsValid);
        Assert.NotNull(vm.NameError);
        Assert.Contains("required", vm.NameError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WhitespaceOnlyName_SetsNameError()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "   ";
        vm.ShellPath = "/bin/bash";

        // Assert
        Assert.False(vm.IsValid);
        Assert.NotNull(vm.NameError);
    }

    [Fact]
    public void Validate_EmptyShellPath_SetsPathError()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = string.Empty;

        // Assert
        Assert.False(vm.IsValid);
        Assert.NotNull(vm.PathError);
        Assert.Contains("required", vm.PathError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InvalidShellPath_SetsPathError()
    {
        // Arrange
        _mockShellDetection
            .Setup(s => s.ValidateShellPath(It.IsAny<string>()))
            .Returns(false);

        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "/nonexistent/path";

        // Assert
        Assert.False(vm.IsValid);
        Assert.NotNull(vm.PathError);
        Assert.Contains("not found", vm.PathError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ValidConfiguration_ClearsAllErrors()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Valid Profile";
        vm.ShellPath = "/bin/bash";

        // Assert
        Assert.Null(vm.NameError);
        Assert.Null(vm.PathError);
    }

    [Fact]
    public void Validate_ValidConfiguration_SetsIsValidTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Valid Profile";
        vm.ShellPath = "/bin/bash";

        // Assert
        Assert.True(vm.IsValid);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Shell Detection Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region Shell Detection Tests

    [Fact]
    public void OnShellPathChanged_DetectsShellType()
    {
        // Arrange
        _mockShellDetection
            .Setup(s => s.DetectShellType("/bin/zsh"))
            .Returns(ShellType.Zsh);

        var vm = CreateViewModel();
        vm.ProfileName = "Test";

        // Act
        vm.ShellPath = "/bin/zsh";

        // Assert
        Assert.Equal(ShellType.Zsh, vm.ShellType);
    }

    [Fact]
    public async Task OnShellPathChanged_StartsVersionDetection()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Test";

        // Act
        vm.ShellPath = "/bin/bash";

        // Allow async version detection to complete
        await Task.Delay(100);

        // Assert
        _mockShellDetection.Verify(
            s => s.GetShellVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DetectVersionAsync_SetsDetectedVersion()
    {
        // Arrange
        _mockShellDetection
            .Setup(s => s.GetShellVersionAsync("/bin/bash", It.IsAny<CancellationToken>()))
            .ReturnsAsync("5.2.15");

        var vm = CreateViewModel();
        vm.ProfileName = "Test";

        // Act
        vm.ShellPath = "/bin/bash";
        await Task.Delay(150); // Allow async operation to complete

        // Assert
        Assert.Equal("5.2.15", vm.DetectedVersion);
    }

    [Fact]
    public void DetectFromPath_FindsExecutable()
    {
        // Arrange
        _mockShellDetection
            .Setup(s => s.FindInPath("bash"))
            .Returns("/usr/local/bin/bash");

        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "bash"; // Just the name, not full path

        // Act
        vm.DetectFromPathCommand.Execute(null);

        // Assert
        Assert.Equal("/usr/local/bin/bash", vm.ShellPath);
    }

    [Fact]
    public void DetectFromPath_NotFound_KeepsOriginalPath()
    {
        // Arrange
        _mockShellDetection
            .Setup(s => s.FindInPath("nonexistent"))
            .Returns((string?)null);

        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "nonexistent";

        // Act
        vm.DetectFromPathCommand.Execute(null);

        // Assert
        Assert.Equal("nonexistent", vm.ShellPath);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // GetProfile Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region GetProfile Tests

    [Fact]
    public void GetProfile_ReturnsConfiguredProfile()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "My Shell";
        vm.ShellPath = "/bin/zsh";
        vm.ShellType = ShellType.Zsh;
        vm.Arguments = "-i";
        vm.WorkingDirectory = "/home/user";
        vm.IsLoginShell = true;

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal("My Shell", profile.Name);
        Assert.Equal("/bin/zsh", profile.ShellPath);
        Assert.Equal(ShellType.Zsh, profile.ShellType);
        // When IsLoginShell is true, -l is prepended to arguments
        Assert.Contains("-l", profile.Arguments);
        Assert.Equal("/home/user", profile.StartingDirectory);
    }

    [Fact]
    public void GetProfile_ParsesEnvironmentVariables()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "/bin/bash";
        vm.EnvironmentVariables = "KEY1=value1\nKEY2=value2";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal(2, profile.Environment.Count);
        Assert.Equal("value1", profile.Environment["KEY1"]);
        Assert.Equal("value2", profile.Environment["KEY2"]);
    }

    [Fact]
    public void GetProfile_PreservesOriginalId()
    {
        // Arrange
        var vm = CreateViewModel();
        var originalProfile = CreateTestProfile();
        vm.LoadProfile(originalProfile);

        vm.ProfileName = "Modified Name";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal(originalProfile.Id, profile.Id);
    }

    [Fact]
    public void GetProfile_TrimsWhitespace()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "  Trimmed Name  ";
        vm.ShellPath = "  /bin/bash  ";
        vm.Arguments = "  -l  ";
        vm.WorkingDirectory = "  /home/user  ";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal("Trimmed Name", profile.Name);
        Assert.Equal("/bin/bash", profile.ShellPath);
        Assert.Equal("-l", profile.Arguments);
        Assert.Equal("/home/user", profile.StartingDirectory);
    }

    [Fact]
    public void GetProfile_NewProfile_GeneratesNewId()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "New Profile";
        vm.ShellPath = "/bin/bash";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.NotEqual(Guid.Empty, profile.Id);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Command Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region Command Tests

    [Fact]
    public void SaveCommand_WhenValid_RaisesSaveRequested()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Valid Profile";
        vm.ShellPath = "/bin/bash";

        ShellProfile? savedProfile = null;
        vm.SaveRequested += (s, p) => savedProfile = p;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.NotNull(savedProfile);
        Assert.Equal("Valid Profile", savedProfile!.Name);
    }

    [Fact]
    public void SaveCommand_WhenInvalid_DoesNotRaiseSaveRequested()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = string.Empty; // Invalid
        vm.ShellPath = "/bin/bash";

        var eventRaised = false;
        vm.SaveRequested += (s, p) => eventRaised = true;

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void CancelCommand_RaisesCancelRequested()
    {
        // Arrange
        var vm = CreateViewModel();
        var eventRaised = false;
        vm.CancelRequested += (s, e) => eventRaised = true;

        // Act
        vm.CancelCommand.Execute(null);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void ClearWorkingDirectoryCommand_ClearsWorkingDirectory()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.WorkingDirectory = "/some/path";

        // Act
        vm.ClearWorkingDirectoryCommand.Execute(null);

        // Assert
        Assert.Equal(string.Empty, vm.WorkingDirectory);
    }

    [Fact]
    public void AddEnvironmentVariableCommand_AddsTemplate()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.EnvironmentVariables = string.Empty;

        // Act
        vm.AddEnvironmentVariableCommand.Execute(null);

        // Assert
        Assert.Contains("KEY=value", vm.EnvironmentVariables);
    }

    [Fact]
    public void AddEnvironmentVariableCommand_AppendsToExisting()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.EnvironmentVariables = "EXISTING=value";

        // Act
        vm.AddEnvironmentVariableCommand.Execute(null);

        // Assert
        Assert.Contains("EXISTING=value", vm.EnvironmentVariables);
        Assert.Contains("KEY=value", vm.EnvironmentVariables);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Environment Variable Parsing Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region Environment Variable Parsing Tests

    [Fact]
    public void ParseEnvironmentVariables_ParsesValidKeyValuePairs()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "/bin/bash";
        vm.EnvironmentVariables = "VAR1=value1\nVAR2=value2\nVAR3=value3";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal(3, profile.Environment.Count);
        Assert.Equal("value1", profile.Environment["VAR1"]);
        Assert.Equal("value2", profile.Environment["VAR2"]);
        Assert.Equal("value3", profile.Environment["VAR3"]);
    }

    [Fact]
    public void ParseEnvironmentVariables_IgnoresEmptyLines()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "/bin/bash";
        vm.EnvironmentVariables = "VAR1=value1\n\n\nVAR2=value2";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal(2, profile.Environment.Count);
    }

    [Fact]
    public void ParseEnvironmentVariables_IgnoresLinesWithoutEquals()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "/bin/bash";
        vm.EnvironmentVariables = "VAR1=value1\nINVALID_LINE\nVAR2=value2";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal(2, profile.Environment.Count);
        Assert.False(profile.Environment.ContainsKey("INVALID_LINE"));
    }

    [Fact]
    public void ParseEnvironmentVariables_HandlesValuesWithEquals()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "/bin/bash";
        vm.EnvironmentVariables = "PATH=/usr/bin:/usr/local/bin\nVAR=a=b=c";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal("/usr/bin:/usr/local/bin", profile.Environment["PATH"]);
        Assert.Equal("a=b=c", profile.Environment["VAR"]);
    }

    [Fact]
    public void ParseEnvironmentVariables_TrimsWhitespace()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ProfileName = "Test";
        vm.ShellPath = "/bin/bash";
        vm.EnvironmentVariables = "  VAR1  =  value1  \n  VAR2=value2  ";

        // Act
        var profile = vm.GetProfile();

        // Assert
        Assert.Equal("value1", profile.Environment["VAR1"]);
        Assert.Equal("value2", profile.Environment["VAR2"]);
    }

    [Fact]
    public void FormatEnvironmentVariables_FormatsCorrectly()
    {
        // Arrange
        var vm = CreateViewModel();
        var profile = new ShellProfile
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            ShellPath = "/bin/bash",
            Environment = new Dictionary<string, string>
            {
                ["VAR1"] = "value1",
                ["VAR2"] = "value2"
            }
        };

        // Act
        vm.LoadProfile(profile);

        // Assert
        Assert.Contains("VAR1=value1", vm.EnvironmentVariables);
        Assert.Contains("VAR2=value2", vm.EnvironmentVariables);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Window Title Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region Window Title Tests

    [Fact]
    public void WindowTitle_NewProfile_ReturnsNewProfileTitle()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        Assert.Equal("New Shell Profile", vm.WindowTitle);
    }

    [Fact]
    public void WindowTitle_EditMode_IndicatesEditing()
    {
        // Arrange
        var vm = CreateViewModel();
        var profile = CreateTestProfile();

        // Act
        vm.LoadProfile(profile);

        // Assert
        Assert.Contains("Edit", vm.WindowTitle);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Property Notification Tests
    // ═══════════════════════════════════════════════════════════════════════════

    #region Property Notification Tests

    [Fact]
    public void ProfileName_Change_RaisesPropertyChanged()
    {
        // Arrange
        var vm = CreateViewModel();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.ProfileName))
                propertyChanged = true;
        };

        // Act
        vm.ProfileName = "New Name";

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void ShellPath_Change_RaisesMultiplePropertyChanges()
    {
        // Arrange
        var vm = CreateViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        vm.ShellPath = "/bin/zsh";

        // Assert - Should raise for ShellPath and potentially ShellType, IsValid, etc.
        Assert.Contains(nameof(vm.ShellPath), changedProperties);
    }

    #endregion
}
