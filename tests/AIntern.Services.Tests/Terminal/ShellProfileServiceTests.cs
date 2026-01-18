using System.Text.Json;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL PROFILE SERVICE TESTS (v0.5.3d)                                   │
// │ Unit tests for profile CRUD, persistence, and built-in generation.      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="ShellProfileService"/>.
/// </summary>
public sealed class ShellProfileServiceTests : IAsyncLifetime
{
    // ─────────────────────────────────────────────────────────────────────
    // Test Fixtures
    // ─────────────────────────────────────────────────────────────────────

    private readonly Mock<IShellDetectionService> _mockShellDetection;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<ShellProfileService>> _mockLogger;
    private readonly AppSettings _appSettings;
    private ShellProfileService _service = null!;

    public ShellProfileServiceTests()
    {
        _mockShellDetection = new Mock<IShellDetectionService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<ShellProfileService>>();
        _appSettings = new AppSettings();

        // Default setup: return single shell
        _mockShellDetection.Setup(s => s.GetAvailableShellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShellInfo>
            {
                new() { Name = "Test Shell", Path = "/bin/testsh", ShellType = ShellType.Bash, IsDefault = true }
            });

        _mockShellDetection.Setup(s => s.ValidateShellPath(It.IsAny<string>())).Returns(true);
        _mockShellDetection.Setup(s => s.DetectShellType(It.IsAny<string>())).Returns(ShellType.Bash);
        _mockSettingsService.SetupGet(s => s.CurrentSettings).Returns(_appSettings);
    }

    public Task InitializeAsync()
    {
        _service = new ShellProfileService(
            _mockShellDetection.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ─────────────────────────────────────────────────────────────────────
    // GetAllProfilesAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetAllProfilesAsync returns all profiles including built-in.<br/>
    /// </summary>
    [Fact]
    public async Task GetAllProfilesAsync_ReturnsAllProfiles()
    {
        // Act
        var profiles = await _service.GetAllProfilesAsync();

        // Assert
        Assert.NotEmpty(profiles);
        Assert.Contains(profiles, p => p.IsBuiltIn);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetVisibleProfilesAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetVisibleProfilesAsync excludes hidden profiles.<br/>
    /// </summary>
    [Fact]
    public async Task GetVisibleProfilesAsync_ExcludesHidden()
    {
        // Arrange
        await _service.GetAllProfilesAsync(); // Initialize
        var hiddenProfile = new ShellProfile
        {
            Name = "Hidden",
            ShellPath = "/bin/hidden",
            IsHidden = true
        };
        await _service.CreateProfileAsync(hiddenProfile);

        // Act
        var visible = await _service.GetVisibleProfilesAsync();

        // Assert
        Assert.DoesNotContain(visible, p => p.Name == "Hidden");
    }

    /// <summary>
    /// <b>Unit Test:</b> GetVisibleProfilesAsync sorts by SortOrder then Name.<br/>
    /// </summary>
    [Fact]
    public async Task GetVisibleProfilesAsync_SortsBySortOrderThenName()
    {
        // Arrange
        _mockShellDetection.Setup(s => s.GetAvailableShellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShellInfo>
            {
                new() { Name = "Zsh", Path = "/bin/zsh", ShellType = ShellType.Zsh, IsDefault = false },
                new() { Name = "Bash", Path = "/bin/bash", ShellType = ShellType.Bash, IsDefault = true }
            });

        _service = new ShellProfileService(
            _mockShellDetection.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);

        // Act
        var visible = await _service.GetVisibleProfilesAsync();

        // Assert
        Assert.True(visible.Count >= 2);
        // Default (Bash, SortOrder=0) should come before Zsh (SortOrder=100)
        var bashIndex = visible.ToList().FindIndex(p => p.Name == "Bash");
        var zshIndex = visible.ToList().FindIndex(p => p.Name == "Zsh");
        Assert.True(bashIndex < zshIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetDefaultProfileAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultProfileAsync uses configured default from AppSettings.<br/>
    /// </summary>
    [Fact]
    public async Task GetDefaultProfileAsync_ReturnsConfiguredDefault()
    {
        // Arrange
        var profiles = await _service.GetAllProfilesAsync();
        var firstProfile = profiles.First();
        _appSettings.DefaultShellProfileId = firstProfile.Id;

        // Act
        var defaultProfile = await _service.GetDefaultProfileAsync();

        // Assert
        Assert.Equal(firstProfile.Id, defaultProfile.Id);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultProfileAsync falls back to IsDefault flag.<br/>
    /// </summary>
    [Fact]
    public async Task GetDefaultProfileAsync_FallsBackToIsDefault()
    {
        // Arrange
        _appSettings.DefaultShellProfileId = null;

        // Act
        var defaultProfile = await _service.GetDefaultProfileAsync();

        // Assert
        Assert.True(defaultProfile.IsDefault);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultProfileAsync falls back to first profile.<br/>
    /// </summary>
    [Fact]
    public async Task GetDefaultProfileAsync_FallsBackToFirst()
    {
        // Arrange
        _mockShellDetection.Setup(s => s.GetAvailableShellsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShellInfo>
            {
                new() { Name = "Shell1", Path = "/bin/sh1", ShellType = ShellType.Bash, IsDefault = false },
                new() { Name = "Shell2", Path = "/bin/sh2", ShellType = ShellType.Zsh, IsDefault = false }
            });

        _service = new ShellProfileService(
            _mockShellDetection.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);

        // Act
        var defaultProfile = await _service.GetDefaultProfileAsync();

        // Assert
        Assert.NotNull(defaultProfile);
        // First profile should be marked default by GenerateBuiltInProfilesAsync
        Assert.True(defaultProfile.IsDefault);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CreateProfileAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> CreateProfileAsync validates shell path and throws.<br/>
    /// </summary>
    [Fact]
    public async Task CreateProfileAsync_ValidatesShellPath()
    {
        // Arrange
        _mockShellDetection.Setup(s => s.ValidateShellPath("/invalid/path")).Returns(false);
        var profile = new ShellProfile { Name = "Test", ShellPath = "/invalid/path" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateProfileAsync(profile));
    }

    /// <summary>
    /// <b>Unit Test:</b> CreateProfileAsync auto-detects ShellType when Unknown.<br/>
    /// </summary>
    [Fact]
    public async Task CreateProfileAsync_AutoDetectsShellType()
    {
        // Arrange
        var profile = new ShellProfile
        {
            Name = "Test",
            ShellPath = "/bin/bash",
            ShellType = ShellType.Unknown
        };

        // Act
        var created = await _service.CreateProfileAsync(profile);

        // Assert
        Assert.Equal(ShellType.Bash, created.ShellType);
    }

    /// <summary>
    /// <b>Unit Test:</b> CreateProfileAsync raises ProfilesChanged event.<br/>
    /// </summary>
    [Fact]
    public async Task CreateProfileAsync_RaisesProfilesChanged()
    {
        // Arrange
        ProfilesChangedEventArgs? eventArgs = null;
        _service.ProfilesChanged += (_, e) => eventArgs = e;

        var profile = new ShellProfile { Name = "Test", ShellPath = "/bin/test" };

        // Act
        await _service.CreateProfileAsync(profile);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ProfileChangeType.Added, eventArgs.ChangeType);
        Assert.Equal(profile.Id, eventArgs.ProfileId);
    }

    // ─────────────────────────────────────────────────────────────────────
    // UpdateProfileAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> UpdateProfileAsync throws for built-in profiles.<br/>
    /// </summary>
    [Fact]
    public async Task UpdateProfileAsync_ThrowsForBuiltIn()
    {
        // Arrange
        var profiles = await _service.GetAllProfilesAsync();
        var builtIn = profiles.First(p => p.IsBuiltIn);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProfileAsync(builtIn));
    }

    /// <summary>
    /// <b>Unit Test:</b> UpdateProfileAsync updates ModifiedAt timestamp.<br/>
    /// </summary>
    [Fact]
    public async Task UpdateProfileAsync_UpdatesModifiedAt()
    {
        // Arrange
        var profile = new ShellProfile { Name = "Test", ShellPath = "/bin/test" };
        var created = await _service.CreateProfileAsync(profile);
        var originalModified = created.ModifiedAt;
        await Task.Delay(10); // Ensure time difference

        // Act
        created.Name = "Updated";
        await _service.UpdateProfileAsync(created);

        // Assert
        var updated = await _service.GetProfileAsync(created.Id);
        Assert.NotNull(updated);
        Assert.True(updated.ModifiedAt > originalModified);
    }

    // ─────────────────────────────────────────────────────────────────────
    // DeleteProfileAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> DeleteProfileAsync throws for built-in profiles.<br/>
    /// </summary>
    [Fact]
    public async Task DeleteProfileAsync_ThrowsForBuiltIn()
    {
        // Arrange
        var profiles = await _service.GetAllProfilesAsync();
        var builtIn = profiles.First(p => p.IsBuiltIn);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteProfileAsync(builtIn.Id));
    }

    /// <summary>
    /// <b>Unit Test:</b> DeleteProfileAsync clears default if deleted was default.<br/>
    /// </summary>
    [Fact]
    public async Task DeleteProfileAsync_ClearsDefaultIfDeleted()
    {
        // Arrange
        var profile = new ShellProfile { Name = "Test", ShellPath = "/bin/test" };
        var created = await _service.CreateProfileAsync(profile);
        _appSettings.DefaultShellProfileId = created.Id;

        // Act
        await _service.DeleteProfileAsync(created.Id);

        // Assert
        _mockSettingsService.Verify(s => s.SaveSettingsAsync(It.Is<AppSettings>(
            a => a.DefaultShellProfileId == null)), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SetDefaultProfileAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> SetDefaultProfileAsync clears other defaults.<br/>
    /// </summary>
    [Fact]
    public async Task SetDefaultProfileAsync_ClearsOtherDefaults()
    {
        // Arrange
        var profile1 = new ShellProfile { Name = "Test1", ShellPath = "/bin/t1" };
        var profile2 = new ShellProfile { Name = "Test2", ShellPath = "/bin/t2" };
        var created1 = await _service.CreateProfileAsync(profile1);
        var created2 = await _service.CreateProfileAsync(profile2);

        // Set first as default
        await _service.SetDefaultProfileAsync(created1.Id);
        Assert.True((await _service.GetProfileAsync(created1.Id))!.IsDefault);

        // Act - Set second as default
        await _service.SetDefaultProfileAsync(created2.Id);

        // Assert - First is no longer default
        Assert.False((await _service.GetProfileAsync(created1.Id))!.IsDefault);
        Assert.True((await _service.GetProfileAsync(created2.Id))!.IsDefault);
    }

    /// <summary>
    /// <b>Unit Test:</b> SetDefaultProfileAsync updates AppSettings.<br/>
    /// </summary>
    [Fact]
    public async Task SetDefaultProfileAsync_UpdatesAppSettings()
    {
        // Arrange
        var profile = new ShellProfile { Name = "Test", ShellPath = "/bin/test" };
        var created = await _service.CreateProfileAsync(profile);

        // Act
        await _service.SetDefaultProfileAsync(created.Id);

        // Assert
        _mockSettingsService.Verify(s => s.SaveSettingsAsync(It.Is<AppSettings>(
            a => a.DefaultShellProfileId == created.Id)), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────
    // DuplicateProfileAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> DuplicateProfileAsync uses Clone method correctly.<br/>
    /// </summary>
    [Fact]
    public async Task DuplicateProfileAsync_UsesClone()
    {
        // Arrange
        var profile = new ShellProfile
        {
            Name = "Original",
            ShellPath = "/bin/test",
            FontFamily = "Custom Font"
        };
        var created = await _service.CreateProfileAsync(profile);

        // Act
        var duplicated = await _service.DuplicateProfileAsync(created.Id);

        // Assert
        Assert.NotEqual(created.Id, duplicated.Id);
        Assert.Equal("Original (Copy)", duplicated.Name);
        Assert.Equal("Custom Font", duplicated.FontFamily);
        Assert.False(duplicated.IsBuiltIn);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ResetToDefaultsAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ResetToDefaultsAsync regenerates built-in profiles.<br/>
    /// </summary>
    [Fact]
    public async Task ResetToDefaultsAsync_RegeneratesBuiltIn()
    {
        // Arrange
        var customProfile = new ShellProfile { Name = "Custom", ShellPath = "/bin/custom" };
        await _service.CreateProfileAsync(customProfile);
        var profilesBefore = await _service.GetAllProfilesAsync();
        Assert.Contains(profilesBefore, p => p.Name == "Custom");

        // Act
        await _service.ResetToDefaultsAsync();

        // Assert
        var profilesAfter = await _service.GetAllProfilesAsync();
        Assert.DoesNotContain(profilesAfter, p => p.Name == "Custom");
        Assert.Contains(profilesAfter, p => p.IsBuiltIn);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Import/Export Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ImportProfilesAsync generates new IDs.<br/>
    /// </summary>
    [Fact]
    public async Task ImportProfilesAsync_GeneratesNewIds()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var profiles = new List<ShellProfile>
        {
            new() { Id = originalId, Name = "Imported", ShellPath = "/bin/test" }
        };
        var json = JsonSerializer.Serialize(profiles);

        // Act
        var count = await _service.ImportProfilesAsync(json);

        // Assert
        Assert.Equal(1, count);
        var all = await _service.GetAllProfilesAsync();
        var imported = all.FirstOrDefault(p => p.Name == "Imported");
        Assert.NotNull(imported);
        Assert.NotEqual(originalId, imported.Id);
    }

    /// <summary>
    /// <b>Unit Test:</b> ImportProfilesAsync validates shell paths and skips invalid.<br/>
    /// </summary>
    [Fact]
    public async Task ImportProfilesAsync_ValidatesShellPaths()
    {
        // Arrange
        _mockShellDetection.Setup(s => s.ValidateShellPath("/invalid")).Returns(false);
        var profiles = new List<ShellProfile>
        {
            new() { Name = "Valid", ShellPath = "/bin/test" },
            new() { Name = "Invalid", ShellPath = "/invalid" }
        };
        var json = JsonSerializer.Serialize(profiles);

        // Act
        var count = await _service.ImportProfilesAsync(json);

        // Assert
        Assert.Equal(1, count);
        var all = await _service.GetAllProfilesAsync();
        Assert.Contains(all, p => p.Name == "Valid");
        Assert.DoesNotContain(all, p => p.Name == "Invalid");
    }

    /// <summary>
    /// <b>Unit Test:</b> ExportProfilesAsync excludes built-in by default.<br/>
    /// </summary>
    [Fact]
    public async Task ExportProfilesAsync_ExcludesBuiltInByDefault()
    {
        // Arrange
        var customProfile = new ShellProfile { Name = "Custom", ShellPath = "/bin/custom" };
        await _service.CreateProfileAsync(customProfile);

        // Act
        var json = await _service.ExportProfilesAsync();
        var exported = JsonSerializer.Deserialize<List<ShellProfile>>(json);

        // Assert
        Assert.NotNull(exported);
        Assert.DoesNotContain(exported!, p => p.IsBuiltIn);
        Assert.Contains(exported!, p => p.Name == "Custom");
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetEffectiveSettings Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetEffectiveSettings merges profile with AppSettings defaults.<br/>
    /// </summary>
    [Fact]
    public async Task GetEffectiveSettings_MergesWithAppSettings()
    {
        // Arrange
        _appSettings.TerminalFontFamily = "AppDefault Font";
        _appSettings.TerminalFontSize = 16;

        var profile = new ShellProfile
        {
            Name = "Test",
            ShellPath = "/bin/test",
            FontFamily = "Profile Font",  // Override
            FontSize = null               // Use default
        };

        await _service.GetAllProfilesAsync(); // Initialize

        // Act
        var effective = _service.GetEffectiveSettings(profile);

        // Assert
        Assert.Equal("Profile Font", effective.FontFamily);  // From profile
        Assert.Equal(16, effective.FontSize);                // From AppSettings
    }

    // ─────────────────────────────────────────────────────────────────────
    // Event Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ProfileChangeType enum has all values.<br/>
    /// </summary>
    [Fact]
    public void ProfileChangeType_AllValuesExist()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ProfileChangeType), ProfileChangeType.Added));
        Assert.True(Enum.IsDefined(typeof(ProfileChangeType), ProfileChangeType.Updated));
        Assert.True(Enum.IsDefined(typeof(ProfileChangeType), ProfileChangeType.Deleted));
        Assert.True(Enum.IsDefined(typeof(ProfileChangeType), ProfileChangeType.DefaultChanged));
        Assert.True(Enum.IsDefined(typeof(ProfileChangeType), ProfileChangeType.Reset));
        Assert.Equal(5, Enum.GetValues<ProfileChangeType>().Length);
    }
}
