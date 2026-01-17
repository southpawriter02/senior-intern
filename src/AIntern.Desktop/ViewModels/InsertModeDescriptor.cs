// -----------------------------------------------------------------------
// <copyright file="InsertModeDescriptor.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Describes an insert mode for display in the UI.
//     Added in v0.4.5e.
// </summary>
// -----------------------------------------------------------------------

using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// Describes an insert mode for display in the UI.
/// </summary>
/// <remarks>
/// <para>
/// Used to populate the insert mode selector in the snippet apply options popup.
/// Provides display-friendly labels and descriptions for each mode.
/// </para>
/// <para>Added in v0.4.5e.</para>
/// </remarks>
/// <param name="Mode">The insert mode value.</param>
/// <param name="Label">Short display label for the mode.</param>
/// <param name="Description">Longer description for tooltips.</param>
public sealed record InsertModeDescriptor(
    SnippetInsertMode Mode,
    string Label,
    string Description);
