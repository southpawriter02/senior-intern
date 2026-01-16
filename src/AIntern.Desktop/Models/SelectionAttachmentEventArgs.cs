namespace AIntern.Desktop.Models;

using System;

/// <summary>
/// Event args for selection attachment requests from the editor.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4f.</para>
/// </remarks>
public sealed class SelectionAttachmentEventArgs : EventArgs
{
    /// <summary>
    /// The selection information to attach.
    /// </summary>
    public SelectionInfo Selection { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionAttachmentEventArgs"/>.
    /// </summary>
    /// <param name="selection">The selection to attach.</param>
    /// <exception cref="ArgumentNullException">Thrown when selection is null.</exception>
    public SelectionAttachmentEventArgs(SelectionInfo selection)
    {
        Selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }
}
