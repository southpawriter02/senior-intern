// ============================================================================
// File: TerminalSearchBar.axaml.cs
// Path: src/AIntern.Desktop/Views/TerminalSearchBar.axaml.cs
// Description: Code-behind for the terminal search bar control.
//              Handles keyboard shortcuts and focus management.
// Created: 2026-01-18
// AI Intern v0.5.5c - Terminal Search UI
// ============================================================================

namespace AIntern.Desktop.Views;

using Avalonia.Controls;
using Avalonia.Input;
using AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalSearchBar Code-Behind (v0.5.5c)                                  │
// │ Handles keyboard navigation and focus management.                        │
// │                                                                          │
// │ Keyboard Shortcuts:                                                      │
// │ - Enter: Navigate to next result                                         │
// │ - Shift+Enter: Navigate to previous result                              │
// │ - Escape: Close search bar                                               │
// │ - Alt+C: Toggle case sensitivity                                         │
// │ - Alt+R: Toggle regex mode                                              │
// │ - F3: Next result (alternative)                                          │
// │ - Shift+F3: Previous result (alternative)                               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Code-behind for the terminal search bar control.
/// </summary>
/// <remarks>
/// <para>
/// This code-behind handles:
/// </para>
/// <list type="bullet">
///   <item><description>Keyboard shortcuts for navigation and options</description></item>
///   <item><description>Focus management when search bar becomes visible</description></item>
///   <item><description>Text selection on focus for easy replacement</description></item>
/// </list>
/// <para>Added in v0.5.5c.</para>
/// </remarks>
public partial class TerminalSearchBar : UserControl
{
    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalSearchBar"/> control.
    /// </summary>
    public TerminalSearchBar()
    {
        InitializeComponent();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Keyboard Handling
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles keyboard input for search navigation and shortcuts.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <para>
    /// Supported shortcuts:
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Shortcut</term>
    ///     <description>Action</description>
    ///   </listheader>
    ///   <item>
    ///     <term>Enter</term>
    ///     <description>Navigate to next result</description>
    ///   </item>
    ///   <item>
    ///     <term>Shift+Enter</term>
    ///     <description>Navigate to previous result</description>
    ///   </item>
    ///   <item>
    ///     <term>Escape</term>
    ///     <description>Close search bar</description>
    ///   </item>
    ///   <item>
    ///     <term>Alt+C</term>
    ///     <description>Toggle case sensitivity</description>
    ///   </item>
    ///   <item>
    ///     <term>Alt+R</term>
    ///     <description>Toggle regex mode</description>
    ///   </item>
    ///   <item>
    ///     <term>F3</term>
    ///     <description>Navigate to next result</description>
    ///   </item>
    ///   <item>
    ///     <term>Shift+F3</term>
    ///     <description>Navigate to previous result</description>
    ///   </item>
    /// </list>
    /// </remarks>
    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        // ─────────────────────────────────────────────────────────────────
        // Guard: Require ViewModel
        // ─────────────────────────────────────────────────────────────────
        if (DataContext is not TerminalSearchBarViewModel vm)
            return;

        switch (e.Key)
        {
            // ─────────────────────────────────────────────────────────────
            // Shift+Enter: Previous result
            // ─────────────────────────────────────────────────────────────
            case Key.Enter when e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                vm.PreviousSearchResultCommand.Execute(null);
                e.Handled = true;
                break;

            // ─────────────────────────────────────────────────────────────
            // Enter: Next result
            // ─────────────────────────────────────────────────────────────
            case Key.Enter:
                vm.NextSearchResultCommand.Execute(null);
                e.Handled = true;
                break;

            // ─────────────────────────────────────────────────────────────
            // Escape: Close search
            // ─────────────────────────────────────────────────────────────
            case Key.Escape:
                vm.CloseSearchCommand.Execute(null);
                e.Handled = true;
                break;

            // ─────────────────────────────────────────────────────────────
            // Alt+C: Toggle case sensitivity
            // ─────────────────────────────────────────────────────────────
            case Key.C when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                vm.ToggleCaseSensitiveCommand.Execute(null);
                e.Handled = true;
                break;

            // ─────────────────────────────────────────────────────────────
            // Alt+R: Toggle regex mode
            // ─────────────────────────────────────────────────────────────
            case Key.R when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                vm.ToggleRegexCommand.Execute(null);
                e.Handled = true;
                break;

            // ─────────────────────────────────────────────────────────────
            // F3: Next result (alternative)
            // ─────────────────────────────────────────────────────────────
            case Key.F3 when !e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                vm.NextSearchResultCommand.Execute(null);
                e.Handled = true;
                break;

            // ─────────────────────────────────────────────────────────────
            // Shift+F3: Previous result (alternative)
            // ─────────────────────────────────────────────────────────────
            case Key.F3 when e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                vm.PreviousSearchResultCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Focus Management
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Focuses the search input and selects all text.
    /// </summary>
    /// <remarks>
    /// Called when the search bar is opened to provide immediate typing
    /// capability. Selecting all text allows easy replacement of the query.
    /// </remarks>
    public void FocusSearchInput()
    {
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }

    /// <summary>
    /// Called when a property on this control changes.
    /// </summary>
    /// <param name="change">The property change event arguments.</param>
    /// <remarks>
    /// Automatically focuses the search input when the control becomes visible.
    /// Uses Dispatcher.UIThread.Post to defer focus until after layout.
    /// </remarks>
    protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // ─────────────────────────────────────────────────────────────────
        // Auto-focus when becoming visible
        // ─────────────────────────────────────────────────────────────────
        if (change.Property == IsVisibleProperty && IsVisible)
        {
            // Defer focus to after layout completes
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                FocusSearchInput();
            });
        }
    }
}
