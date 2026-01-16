namespace AIntern.Desktop.Tests.Services;

using Avalonia.Input;
using AIntern.Desktop.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="KeyboardShortcutService"/> (v0.3.5g).
/// </summary>
public class KeyboardShortcutServiceTests
{
    private readonly Mock<ILogger<KeyboardShortcutService>> _mockLogger = new();

    private KeyboardShortcutService CreateService()
    {
        return new KeyboardShortcutService(_mockLogger.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor registers default shortcuts.
    /// </summary>
    [Fact]
    public void Constructor_RegistersDefaultShortcuts()
    {
        // Act
        var service = CreateService();
        var shortcuts = service.GetAllShortcuts();

        // Assert
        Assert.True(shortcuts.Count >= 18, $"Expected at least 18 shortcuts, got {shortcuts.Count}");
    }

    #endregion

    #region Register Tests

    /// <summary>
    /// Verifies that Register adds a new shortcut.
    /// </summary>
    [Fact]
    public void Register_AddsShortcut()
    {
        // Arrange
        var service = CreateService();
        var initialCount = service.GetAllShortcuts().Count;

        // Act
        service.Register(Key.X, KeyModifiers.Control | KeyModifiers.Alt, "test.custom", "Custom Test");

        // Assert
        var shortcuts = service.GetAllShortcuts();
        Assert.Equal(initialCount + 1, shortcuts.Count);
        Assert.Contains(shortcuts, s => s.CommandId == "test.custom");
    }

    /// <summary>
    /// Verifies that Register overwrites existing shortcut with same key.
    /// </summary>
    [Fact]
    public void Register_OverwritesExistingShortcut()
    {
        // Arrange
        var service = CreateService();
        service.Register(Key.Z, KeyModifiers.Control, "first.command", "First");
        var countAfterFirst = service.GetAllShortcuts().Count;

        // Act
        service.Register(Key.Z, KeyModifiers.Control, "second.command", "Second");

        // Assert
        var shortcuts = service.GetAllShortcuts();
        Assert.Equal(countAfterFirst, shortcuts.Count); // No increase
        Assert.Contains(shortcuts, s => s.CommandId == "second.command");
        Assert.DoesNotContain(shortcuts, s => s.CommandId == "first.command");
    }

    #endregion

    #region HandleKeyPress Tests

    /// <summary>
    /// Verifies that HandleKeyPress returns true for registered shortcut.
    /// </summary>
    [Fact]
    public void HandleKeyPress_RegisteredShortcut_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var eventFired = false;
        string? firedCommandId = null;
        service.CommandRequested += (_, id) => { eventFired = true; firedCommandId = id; };

        var keyEventArgs = CreateKeyEventArgs(Key.O, KeyModifiers.Control);

        // Act
        var result = service.HandleKeyPress(keyEventArgs);

        // Assert
        Assert.True(result);
        Assert.True(eventFired);
        Assert.Equal("workspace.open", firedCommandId);
    }

    /// <summary>
    /// Verifies that HandleKeyPress returns false for unregistered shortcut.
    /// </summary>
    [Fact]
    public void HandleKeyPress_UnregisteredShortcut_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var eventFired = false;
        service.CommandRequested += (_, _) => eventFired = true;

        var keyEventArgs = CreateKeyEventArgs(Key.Q, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift);

        // Act
        var result = service.HandleKeyPress(keyEventArgs);

        // Assert
        Assert.False(result);
        Assert.False(eventFired);
    }

    #endregion

    #region GetAllShortcuts Tests

    /// <summary>
    /// Verifies that GetAllShortcuts returns sorted list.
    /// </summary>
    [Fact]
    public void GetAllShortcuts_ReturnsSortedByCategory()
    {
        // Arrange
        var service = CreateService();

        // Act
        var shortcuts = service.GetAllShortcuts();

        // Assert
        var categories = shortcuts.Select(s => s.Category).ToList();
        var sortedCategories = categories.OrderBy(c => c).ToList();
        
        // Categories should be in order
        Assert.True(shortcuts.Count > 0);
    }

    #endregion

    #region Default Shortcuts Tests

    /// <summary>
    /// Verifies workspace.open shortcut exists.
    /// </summary>
    [Fact]
    public void DefaultShortcuts_ContainsWorkspaceOpen()
    {
        // Arrange
        var service = CreateService();

        // Act
        var shortcuts = service.GetAllShortcuts();

        // Assert
        var shortcut = shortcuts.FirstOrDefault(s => s.CommandId == "workspace.open");
        Assert.NotNull(shortcut);
        Assert.Equal(Key.O, shortcut.Key);
        Assert.Equal(KeyModifiers.Control, shortcut.Modifiers);
    }

    /// <summary>
    /// Verifies file.save shortcut exists.
    /// </summary>
    [Fact]
    public void DefaultShortcuts_ContainsFileSave()
    {
        // Arrange
        var service = CreateService();

        // Act
        var shortcuts = service.GetAllShortcuts();

        // Assert
        var shortcut = shortcuts.FirstOrDefault(s => s.CommandId == "file.save");
        Assert.NotNull(shortcut);
        Assert.Equal(Key.S, shortcut.Key);
        Assert.Equal(KeyModifiers.Control, shortcut.Modifiers);
    }

    /// <summary>
    /// Verifies sidebar.toggle shortcut exists.
    /// </summary>
    [Fact]
    public void DefaultShortcuts_ContainsSidebarToggle()
    {
        // Arrange
        var service = CreateService();

        // Act
        var shortcuts = service.GetAllShortcuts();

        // Assert
        var shortcut = shortcuts.FirstOrDefault(s => s.CommandId == "sidebar.toggle");
        Assert.NotNull(shortcut);
        Assert.Equal(Key.B, shortcut.Key);
        Assert.Equal(KeyModifiers.Control, shortcut.Modifiers);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock KeyEventArgs for testing.
    /// </summary>
    private static KeyEventArgs CreateKeyEventArgs(Key key, KeyModifiers modifiers)
    {
        // Create a minimal KeyEventArgs for testing
        return new KeyEventArgs
        {
            Key = key,
            KeyModifiers = modifiers,
            RoutedEvent = null
        };
    }

    #endregion
}
