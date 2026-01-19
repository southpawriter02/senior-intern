// ============================================================================
// File: TerminalSettingsPanel.axaml.cs
// Path: src/AIntern.Desktop/Views/TerminalSettingsPanel.axaml.cs
// Description: Code-behind for the terminal settings panel with profile editor integration.
// Created: 2026-01-19
// Modified: 2026-01-19 (v0.5.5g - Shell Profile Editor integration)
// AI Intern v0.5.5g - Terminal Settings Panel
// ============================================================================

namespace AIntern.Desktop.Views;

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalSettingsPanel (v0.5.5g)                                              │
// │ Code-behind for the terminal settings panel UserControl.                    │
// │                                                                              │
// │ Responsibilities:                                                            │
// │   - Subscribe to ViewModel ProfileEditRequested event                       │
// │   - Open ShellProfileEditor dialog for new/editing profiles                 │
// │   - Handle dialog results (save/cancel)                                     │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Code-behind for the terminal settings panel.
/// </summary>
/// <remarks>
/// <para>
/// This UserControl displays terminal settings organized into sections:
/// </para>
/// <list type="bullet">
///   <item><description>Appearance: Font, theme, cursor</description></item>
///   <item><description>Behavior: Scrollback, bell, clipboard</description></item>
///   <item><description>Shell Profiles: List, add, edit, delete</description></item>
/// </list>
/// <para>
/// In v0.5.5g, profile editor integration was added to open the
/// <see cref="ShellProfileEditor"/> dialog when profiles are added or edited.
/// </para>
/// <para>Added in v0.5.5f. Updated in v0.5.5g.</para>
/// </remarks>
public partial class TerminalSettingsPanel : UserControl
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Fields
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<TerminalSettingsPanel>? _logger;

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of <see cref="TerminalSettingsPanel"/>.
    /// </summary>
    public TerminalSettingsPanel()
    {
        InitializeComponent();

        // Resolve logger from DI (may be null during design time)
        try
        {
            _logger = App.Services?.GetService<ILogger<TerminalSettingsPanel>>();
        }
        catch
        {
            // Design-time or DI not configured
        }

        _logger?.LogDebug("[TerminalSettingsPanel] Instance created");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Lifecycle Event Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the control is loaded. Subscribes to ViewModel events.
    /// </summary>
    /// <param name="e">The routed event arguments.</param>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _logger?.LogDebug("[TerminalSettingsPanel] OnLoaded - subscribing to ViewModel events");

        // Subscribe to ViewModel events
        if (DataContext is TerminalSettingsViewModel vm)
        {
            vm.ProfileEditRequested += OnProfileEditRequested;
            _logger?.LogDebug("[TerminalSettingsPanel] Subscribed to ProfileEditRequested event");
        }
        else
        {
            _logger?.LogWarning("[TerminalSettingsPanel] DataContext is not TerminalSettingsViewModel");
        }
    }

    /// <summary>
    /// Called when the control is unloaded. Unsubscribes from ViewModel events.
    /// </summary>
    /// <param name="e">The routed event arguments.</param>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _logger?.LogDebug("[TerminalSettingsPanel] OnUnloaded - unsubscribing from ViewModel events");

        // Unsubscribe from ViewModel events to prevent memory leaks
        if (DataContext is TerminalSettingsViewModel vm)
        {
            vm.ProfileEditRequested -= OnProfileEditRequested;
            _logger?.LogDebug("[TerminalSettingsPanel] Unsubscribed from ProfileEditRequested event");
        }

        base.OnUnloaded(e);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Profile Editor Integration
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles the ProfileEditRequested event from the ViewModel.
    /// Opens the Shell Profile Editor dialog for creating or editing profiles.
    /// </summary>
    /// <param name="sender">The event sender (ViewModel).</param>
    /// <param name="profile">The profile to edit, or a new profile to create.</param>
    /// <remarks>
    /// <para>
    /// This method determines whether to create a new profile or edit an existing one
    /// based on whether the profile has an empty ShellPath (new) or not (edit).
    /// </para>
    /// <para>
    /// The dialog result is handled as follows:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     If saved: For new profiles, the original empty profile is updated.
    ///     For existing profiles, the ViewModel's UpdateProfile method is called.
    ///   </description></item>
    ///   <item><description>
    ///     If cancelled: For new profiles, the empty profile is removed from the list.
    ///     For existing profiles, no changes are made.
    ///   </description></item>
    /// </list>
    /// <para>Added in v0.5.5g.</para>
    /// </remarks>
    private async void OnProfileEditRequested(object? sender, ShellProfile profile)
    {
        _logger?.LogDebug(
            "[TerminalSettingsPanel] ProfileEditRequested - Name: {Name}, Id: {Id}",
            profile.Name,
            profile.Id);

        try
        {
            await ShowProfileEditorAsync(profile);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[TerminalSettingsPanel] Failed to show profile editor");
        }
    }

    /// <summary>
    /// Shows the Shell Profile Editor dialog and handles the result.
    /// </summary>
    /// <param name="profile">The profile to edit or create.</param>
    /// <returns>A task representing the async operation.</returns>
    private async Task ShowProfileEditorAsync(ShellProfile profile)
    {
        // ─────────────────────────────────────────────────────────────────────
        // Find the owner window
        // ─────────────────────────────────────────────────────────────────────
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null)
        {
            _logger?.LogWarning("[TerminalSettingsPanel] Cannot show editor - no owner window found");
            return;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Resolve dependencies from DI container
        // ─────────────────────────────────────────────────────────────────────
        var shellDetection = App.Services.GetRequiredService<IShellDetectionService>();
        var logger = App.Services.GetRequiredService<ILogger<ShellProfileEditorViewModel>>();

        // ─────────────────────────────────────────────────────────────────────
        // Create and configure the editor ViewModel
        // ─────────────────────────────────────────────────────────────────────
        var editorVm = new ShellProfileEditorViewModel(shellDetection, logger);

        // Determine if this is a new profile or editing existing
        // New profiles have Name="New Profile" and typically have default/detected shell path
        var isNewProfile = profile.Name == "New Profile" && !profile.IsBuiltIn;

        if (isNewProfile)
        {
            _logger?.LogDebug("[TerminalSettingsPanel] Initializing new profile editor");
            await editorVm.InitializeNewProfileAsync();
        }
        else
        {
            _logger?.LogDebug("[TerminalSettingsPanel] Loading existing profile for editing: {Name}", profile.Name);
            editorVm.LoadProfile(profile);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Show the dialog and handle result
        // ─────────────────────────────────────────────────────────────────────
        var result = await ShellProfileEditor.ShowAsync(window, editorVm);

        if (result != null)
        {
            _logger?.LogInformation(
                "[TerminalSettingsPanel] Profile saved - Name: {Name}, Id: {Id}",
                result.Name,
                result.Id);

            // Update the profile in the ViewModel
            if (DataContext is TerminalSettingsViewModel settingsVm)
            {
                if (isNewProfile)
                {
                    // For new profiles, we need to update the placeholder that was added
                    // The placeholder has the original profile's ID, so create a new profile
                    // with the placeholder's ID to enable the UpdateProfile lookup
                    var profileToUpdate = new ShellProfile
                    {
                        Id = profile.Id,
                        Name = result.Name,
                        ShellPath = result.ShellPath,
                        ShellType = result.ShellType,
                        Arguments = result.Arguments,
                        StartingDirectory = result.StartingDirectory,
                        Environment = result.Environment,
                        IsBuiltIn = false,
                        IsDefault = result.IsDefault,
                        ModifiedAt = result.ModifiedAt
                    };
                    settingsVm.UpdateProfile(profileToUpdate);
                }
                else
                {
                    // For existing profiles, just update
                    settingsVm.UpdateProfile(result);
                }
            }
        }
        else
        {
            _logger?.LogDebug("[TerminalSettingsPanel] Profile editor cancelled");

            // If this was a new profile and it was cancelled, remove the placeholder
            if (isNewProfile && DataContext is TerminalSettingsViewModel settingsVm)
            {
                settingsVm.ShellProfiles.Remove(profile);
                _logger?.LogDebug("[TerminalSettingsPanel] Removed cancelled new profile placeholder");
            }
        }
    }
}
