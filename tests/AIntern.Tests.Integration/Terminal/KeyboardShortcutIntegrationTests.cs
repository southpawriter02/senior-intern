// ============================================================================
// File: KeyboardShortcutIntegrationTests.cs
// Path: tests/AIntern.Tests.Integration/Terminal/KeyboardShortcutIntegrationTests.cs
// Description: Integration tests for keyboard shortcut service.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Integration.Terminal;

using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Models.Terminal;
using AIntern.Services;
using AIntern.Tests.Integration.Mocks;

/// <summary>
/// Integration tests for keyboard shortcut service.
/// Tests binding management, conflicts, and persistence.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
public sealed class KeyboardShortcutIntegrationTests : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════
    // Test Fixtures
    // ═══════════════════════════════════════════════════════════════════════

    private readonly Mock<ILogger<TerminalShortcutService>> _loggerMock;
    private readonly MockSettingsService _settingsService;
    private readonly TerminalShortcutService _shortcutService;

    public KeyboardShortcutIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<TerminalShortcutService>>();
        _settingsService = new MockSettingsService();
        _shortcutService = new TerminalShortcutService(_loggerMock.Object, _settingsService);
    }

    public void Dispose()
    {
        // No explicit dispose needed for this implementation
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Default Bindings Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TryGetAction_CtrlBacktick_ReturnsToggleTerminal()
    {
        // Act
        var found = _shortcutService.TryGetAction(
            "OemTilde",
            KeyModifierFlags.Control,
            out var action);

        // Assert
        Assert.True(found);
        Assert.Equal(TerminalShortcutAction.ToggleTerminal, action);
    }

    [Fact]
    public void TryGetAction_CtrlF_ReturnsOpenSearch()
    {
        // Act
        var found = _shortcutService.TryGetAction(
            "F",
            KeyModifierFlags.Control,
            out var action);

        // Assert
        Assert.True(found);
        Assert.Equal(TerminalShortcutAction.OpenSearch, action);
    }

    [Fact]
    public void TryGetAction_CtrlShiftC_ReturnsCopy()
    {
        // Act - Copy uses Ctrl+Shift+C to avoid conflict with PTY interrupt
        var found = _shortcutService.TryGetAction(
            "C",
            KeyModifierFlags.Control | KeyModifierFlags.Shift,
            out var action);

        // Assert
        Assert.True(found);
        Assert.Equal(TerminalShortcutAction.Copy, action);
    }

    [Fact]
    public void TryGetAction_UnboundKey_ReturnsFalse()
    {
        // Act
        var found = _shortcutService.TryGetAction(
            "Q",
            KeyModifierFlags.Control | KeyModifierFlags.Alt | KeyModifierFlags.Shift,
            out var action);

        // Assert
        Assert.False(found);
        Assert.Equal(default(TerminalShortcutAction), action);
    }

    [Fact]
    public void GetAllBindings_ReturnsAllDefaultBindings()
    {
        // Act
        var bindings = _shortcutService.GetAllBindings();

        // Assert
        Assert.True(bindings.Count >= 20);
        Assert.Contains(bindings, b => b.Action == TerminalShortcutAction.ToggleTerminal);
        Assert.Contains(bindings, b => b.Action == TerminalShortcutAction.NewTerminal);
        Assert.Contains(bindings, b => b.Action == TerminalShortcutAction.Copy);
        Assert.Contains(bindings, b => b.Action == TerminalShortcutAction.Paste);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Binding Updates Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateBinding_UpdatesSuccessfully()
    {
        // Act
        var updated = _shortcutService.UpdateBinding(
            TerminalShortcutAction.ToggleTerminal,
            "T",
            KeyModifierFlags.Control | KeyModifierFlags.Alt);

        // Assert
        Assert.True(updated);

        var found = _shortcutService.TryGetAction(
            "T",
            KeyModifierFlags.Control | KeyModifierFlags.Alt,
            out var action);
        Assert.True(found);
        Assert.Equal(TerminalShortcutAction.ToggleTerminal, action);
    }

    [Fact]
    public void UpdateBinding_OldBindingNoLongerWorks()
    {
        // Arrange
        _shortcutService.UpdateBinding(
            TerminalShortcutAction.ToggleTerminal,
            "T",
            KeyModifierFlags.Control | KeyModifierFlags.Alt);

        // Act
        var found = _shortcutService.TryGetAction(
            "OemTilde",
            KeyModifierFlags.Control,
            out _);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void UpdateBinding_Conflict_ReturnsFalse()
    {
        // Arrange - Ctrl+F is already OpenSearch

        // Act
        var updated = _shortcutService.UpdateBinding(
            TerminalShortcutAction.NewTerminal,
            "F",
            KeyModifierFlags.Control);

        // Assert
        Assert.False(updated);
    }

    [Fact]
    public void GetConflict_ReturnsConflictingBinding()
    {
        // Act
        var conflict = _shortcutService.GetConflictingBinding(
            "F",
            KeyModifierFlags.Control);

        // Assert
        Assert.NotNull(conflict);
        Assert.Equal(TerminalShortcutAction.OpenSearch, conflict.Action);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Reset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ResetBinding_RestoresDefault()
    {
        // Arrange
        _shortcutService.UpdateBinding(
            TerminalShortcutAction.ToggleTerminal,
            "T",
            KeyModifierFlags.Control);

        // Act
        _shortcutService.ResetBinding(TerminalShortcutAction.ToggleTerminal);

        // Assert
        var found = _shortcutService.TryGetAction(
            "OemTilde",
            KeyModifierFlags.Control,
            out var action);
        Assert.True(found);
        Assert.Equal(TerminalShortcutAction.ToggleTerminal, action);
    }

    [Fact]
    public void ResetAllBindings_RestoresAllDefaults()
    {
        // Arrange
        _shortcutService.UpdateBinding(TerminalShortcutAction.ToggleTerminal, "A", KeyModifierFlags.Control);
        _shortcutService.UpdateBinding(TerminalShortcutAction.MaximizeTerminal, "B", KeyModifierFlags.Control);

        // Act
        _shortcutService.ResetAllBindings();

        // Assert
        var toggleFound = _shortcutService.TryGetAction("OemTilde", KeyModifierFlags.Control, out _);
        Assert.True(toggleFound);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Persistence Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateBinding_PersistsToSettings()
    {
        // Act
        _shortcutService.UpdateBinding(
            TerminalShortcutAction.ToggleTerminal,
            "T",
            KeyModifierFlags.Control);

        // Allow async save to complete
        await Task.Delay(100);

        // Assert
        Assert.True(_settingsService.WasSaveCalled);
    }
}
