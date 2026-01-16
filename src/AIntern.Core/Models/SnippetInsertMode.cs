namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET INSERT MODE (v0.4.5c)                                           │
// │ Specifies how a code snippet should be inserted into a file.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Specifies how a code snippet should be inserted into a file.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5c.</para>
/// </remarks>
public enum SnippetInsertMode
{
    /// <summary>
    /// Replace specific lines in the file with the snippet content.
    /// Requires ReplaceRange to be specified.
    /// </summary>
    Replace = 0,

    /// <summary>
    /// Insert the snippet before a specific line.
    /// Requires TargetLine to be specified.
    /// </summary>
    InsertBefore = 1,

    /// <summary>
    /// Insert the snippet after a specific line.
    /// Requires TargetLine to be specified.
    /// </summary>
    InsertAfter = 2,

    /// <summary>
    /// Append the snippet to the end of the file.
    /// No additional parameters required.
    /// </summary>
    Append = 3,

    /// <summary>
    /// Prepend the snippet to the beginning of the file.
    /// No additional parameters required.
    /// </summary>
    Prepend = 4,

    /// <summary>
    /// Replace the entire file contents with the snippet.
    /// No additional parameters required.
    /// </summary>
    ReplaceFile = 5
}
