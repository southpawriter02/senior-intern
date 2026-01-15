namespace AIntern.Desktop.ViewModels;

/// <summary>
/// Type of context attachment.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4c.</para>
/// </remarks>
public enum ContextAttachmentType
{
    /// <summary>
    /// Entire file attached.
    /// </summary>
    File,

    /// <summary>
    /// Code selection from editor.
    /// </summary>
    Selection,

    /// <summary>
    /// Content from clipboard.
    /// </summary>
    Clipboard
}
