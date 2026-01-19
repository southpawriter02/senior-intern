// ============================================================================
// File: TerminalSettingsPanel.axaml.cs
// Path: src/AIntern.Desktop/Views/TerminalSettingsPanel.axaml.cs
// Description: Code-behind for the terminal settings panel.
// Created: 2026-01-19
// AI Intern v0.5.5f - Terminal Settings Panel
// ============================================================================

namespace AIntern.Desktop.Views;

using Avalonia.Controls;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalSettingsPanel (v0.5.5f)                                              │
// │ Code-behind for the terminal settings panel UserControl.                    │
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
/// <para>Added in v0.5.5f.</para>
/// </remarks>
public partial class TerminalSettingsPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="TerminalSettingsPanel"/>.
    /// </summary>
    public TerminalSettingsPanel()
    {
        InitializeComponent();
    }
}
