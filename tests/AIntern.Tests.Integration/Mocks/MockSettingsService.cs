// ============================================================================
// File: MockSettingsService.cs
// Path: tests/AIntern.Tests.Integration/Mocks/MockSettingsService.cs
// Description: Mock settings service for testing keyboard shortcuts.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Integration.Mocks;

using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

/// <summary>
/// Mock settings service for testing.
/// Tracks save calls and allows test control over settings values.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
public sealed class MockSettingsService : ISettingsService
{
    // ═══════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public AppSettings CurrentSettings { get; private set; } = new();

    /// <summary>Gets whether SaveSettingsAsync was called.</summary>
    public bool WasSaveCalled { get; private set; }

    /// <summary>Gets the number of times SaveSettingsAsync was called.</summary>
    public int SaveCallCount { get; private set; }

    /// <summary>Gets whether LoadSettingsAsync was called.</summary>
    public bool WasLoadCalled { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════
    // ISettingsService Implementation
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public Task<AppSettings> LoadSettingsAsync()
    {
        WasLoadCalled = true;
        return Task.FromResult(CurrentSettings);
    }

    /// <inheritdoc/>
    public Task SaveSettingsAsync(AppSettings settings)
    {
        WasSaveCalled = true;
        SaveCallCount++;
        CurrentSettings = settings;
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { Settings = settings });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    // ═══════════════════════════════════════════════════════════════════════
    // Test Helpers
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raises the SettingsChanged event for testing.
    /// </summary>
    public void RaiseSettingsChanged()
    {
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { Settings = CurrentSettings });
    }

    /// <summary>
    /// Resets the tracking state for a fresh test.
    /// </summary>
    public void Reset()
    {
        WasSaveCalled = false;
        SaveCallCount = 0;
        WasLoadCalled = false;
    }
}
