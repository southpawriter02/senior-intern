// -----------------------------------------------------------------------
// <copyright file="SnippetApplyOptionsPopup.axaml.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Code-behind for the snippet apply options popup.
//     Added in v0.4.5e.
// </summary>
// -----------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// Code-behind for the snippet apply options popup.
/// Handles ViewModel events and popup lifecycle.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5e.</para>
/// </remarks>
public partial class SnippetApplyOptionsPopup : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnippetApplyOptionsPopup"/> class.
    /// </summary>
    public SnippetApplyOptionsPopup()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Subscribes to ViewModel events when DataContext changes.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SnippetApplyOptionsViewModel vm)
        {
            vm.RequestClose += OnRequestClose;
        }
    }

    /// <summary>
    /// Handles the close request from the ViewModel.
    /// </summary>
    private void OnRequestClose(object? sender, object? result)
    {
        // Find the parent Popup and close it
        var popup = FindParentPopup();
        if (popup is not null)
        {
            popup.IsOpen = false;
        }
    }

    /// <summary>
    /// Finds the parent Popup control in the visual tree.
    /// </summary>
    private Popup? FindParentPopup()
    {
        var parent = Parent;
        while (parent is not null)
        {
            if (parent is Popup popup)
            {
                return popup;
            }
            parent = (parent as Control)?.Parent;
        }
        return null;
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Unsubscribe from ViewModel events
        if (DataContext is SnippetApplyOptionsViewModel vm)
        {
            vm.RequestClose -= OnRequestClose;
        }
    }
}
