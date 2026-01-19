// ============================================================================
// File: ShellProfileEditor.axaml.cs
// Path: src/AIntern.Desktop/Views/ShellProfileEditor.axaml.cs
// Description: Code-behind for the Shell Profile Editor dialog window.
// Created: 2026-01-19
// AI Intern v0.5.5g - Shell Profile Editor
// ============================================================================

namespace AIntern.Desktop.Views;

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;

// ═══════════════════════════════════════════════════════════════════════════════
// ShellProfileEditor - Modal Dialog Code-Behind
// ═══════════════════════════════════════════════════════════════════════════════
//
// This code-behind class handles the dialog lifecycle for the Shell Profile Editor:
//   - Event subscription/unsubscription for ViewModel events
//   - Dialog result handling (Save returns profile, Cancel returns null)
//   - Keyboard shortcut handling (Escape to cancel)
//   - Static helper method for showing the dialog
//
// Usage:
//   var editorVm = new ShellProfileEditorViewModel(shellDetection, logger);
//   editorVm.LoadProfile(existingProfile); // or InitializeNewProfileAsync()
//   var result = await ShellProfileEditor.ShowAsync(ownerWindow, editorVm);
//   if (result != null) { /* profile was saved */ }
//
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Code-behind for the Shell Profile Editor dialog window.
/// Manages dialog lifecycle, event subscriptions, and keyboard shortcuts.
/// </summary>
/// <remarks>
/// <para>
/// The Shell Profile Editor is a modal dialog for creating and editing shell profiles.
/// It supports validation, auto-detection of shell type and version, and environment
/// variable configuration.
/// </para>
/// <para>Added in v0.5.5g.</para>
/// </remarks>
public partial class ShellProfileEditor : Window
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellProfileEditor"/> class.
    /// </summary>
    public ShellProfileEditor()
    {
        InitializeComponent();

        // Log window creation
        System.Diagnostics.Debug.WriteLine("[ShellProfileEditor] Window instance created");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Static Helper Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Shows the Shell Profile Editor dialog and returns the saved profile, or null if cancelled.
    /// </summary>
    /// <param name="owner">The owner window for the modal dialog.</param>
    /// <param name="viewModel">The configured ViewModel instance.</param>
    /// <returns>
    /// The saved <see cref="ShellProfile"/> if the user clicked Save,
    /// or <c>null</c> if the user cancelled.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="owner"/> or <paramref name="viewModel"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the preferred method for showing the Shell Profile Editor.
    /// It handles all dialog lifecycle management automatically.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var vm = new ShellProfileEditorViewModel(shellDetection, logger);
    /// await vm.InitializeNewProfileAsync();
    /// var result = await ShellProfileEditor.ShowAsync(this, vm);
    /// if (result != null)
    /// {
    ///     // Profile was saved, add or update it
    ///     shellProfiles.Add(result);
    /// }
    /// </code>
    /// </para>
    /// <para>Added in v0.5.5g.</para>
    /// </remarks>
    public static async Task<ShellProfile?> ShowAsync(
        Window owner,
        ShellProfileEditorViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(viewModel);

        System.Diagnostics.Debug.WriteLine(
            "[ShellProfileEditor] ShowAsync called - IsEditMode: {0}",
            viewModel.IsEditMode);

        var dialog = new ShellProfileEditor
        {
            DataContext = viewModel
        };

        return await dialog.ShowDialog<ShellProfile?>(owner);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Lifecycle Event Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the window is loaded. Subscribes to ViewModel events.
    /// </summary>
    /// <param name="e">The routed event arguments.</param>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        System.Diagnostics.Debug.WriteLine("[ShellProfileEditor] OnLoaded - subscribing to ViewModel events");

        // Subscribe to ViewModel events
        if (DataContext is ShellProfileEditorViewModel vm)
        {
            vm.SaveRequested += OnSaveRequested;
            vm.CancelRequested += OnCancelRequested;

            System.Diagnostics.Debug.WriteLine(
                "[ShellProfileEditor] Subscribed to events - Profile: {0}",
                vm.ProfileName ?? "(new)");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine(
                "[ShellProfileEditor] WARNING: DataContext is not ShellProfileEditorViewModel");
        }
    }

    /// <summary>
    /// Called when the window is unloaded. Unsubscribes from ViewModel events.
    /// </summary>
    /// <param name="e">The routed event arguments.</param>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[ShellProfileEditor] OnUnloaded - unsubscribing from ViewModel events");

        // Unsubscribe from ViewModel events to prevent memory leaks
        if (DataContext is ShellProfileEditorViewModel vm)
        {
            vm.SaveRequested -= OnSaveRequested;
            vm.CancelRequested -= OnCancelRequested;

            System.Diagnostics.Debug.WriteLine("[ShellProfileEditor] Unsubscribed from events");
        }

        base.OnUnloaded(e);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Keyboard Handling
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles key down events for keyboard shortcuts.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <para>
    /// Supported shortcuts:
    /// <list type="bullet">
    ///   <item><description>Escape - Cancel and close the dialog</description></item>
    /// </list>
    /// </para>
    /// <para>Added in v0.5.5g.</para>
    /// </remarks>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Handle Escape key to cancel
        if (e.Key == Key.Escape)
        {
            System.Diagnostics.Debug.WriteLine("[ShellProfileEditor] Escape key pressed - cancelling");

            e.Handled = true;
            CloseWithResult(null);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ViewModel Event Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles the SaveRequested event from the ViewModel.
    /// Closes the dialog with the saved profile as the result.
    /// </summary>
    /// <param name="sender">The event sender (ViewModel).</param>
    /// <param name="profile">The saved shell profile.</param>
    private void OnSaveRequested(object? sender, ShellProfile profile)
    {
        System.Diagnostics.Debug.WriteLine(
            "[ShellProfileEditor] SaveRequested - Profile: {0}, Id: {1}",
            profile.Name,
            profile.Id);

        CloseWithResult(profile);
    }

    /// <summary>
    /// Handles the CancelRequested event from the ViewModel.
    /// Closes the dialog with null as the result.
    /// </summary>
    /// <param name="sender">The event sender (ViewModel).</param>
    /// <param name="e">The event arguments.</param>
    private void OnCancelRequested(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[ShellProfileEditor] CancelRequested");

        CloseWithResult(null);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Closes the dialog with the specified result.
    /// </summary>
    /// <param name="result">
    /// The profile to return as the dialog result, or null if cancelled.
    /// </param>
    private void CloseWithResult(ShellProfile? result)
    {
        System.Diagnostics.Debug.WriteLine(
            "[ShellProfileEditor] CloseWithResult - HasResult: {0}",
            result != null);

        Close(result);
    }
}
