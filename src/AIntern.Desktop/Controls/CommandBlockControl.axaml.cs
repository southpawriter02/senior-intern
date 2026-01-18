// -----------------------------------------------------------------------
// <copyright file="CommandBlockControl.axaml.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Desktop.Controls;

using Avalonia.Controls;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ COMMAND BLOCK CONTROL (v0.5.4f)                                         │
// │ Renders command blocks in chat messages with action buttons.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Control for rendering command blocks in chat messages.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4f.</para>
/// <para>
/// This control displays:
/// </para>
/// <list type="bullet">
/// <item>Command text with monospace font and prompt symbol</item>
/// <item>Language badge (BASH, PowerShell, etc.)</item>
/// <item>Status badge (Copied, Sent, Running, Executed, Failed)</item>
/// <item>Action buttons: Copy, Send to Terminal, Run</item>
/// <item>Danger warning overlay for dangerous commands</item>
/// </list>
/// <para>
/// Binds to <see cref="AIntern.Desktop.ViewModels.CommandBlockViewModel"/>.
/// </para>
/// </remarks>
public partial class CommandBlockControl : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBlockControl"/> class.
    /// </summary>
    public CommandBlockControl()
    {
        InitializeComponent();
    }
}
