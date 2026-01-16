namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET ANCHOR TYPE (v0.4.5c)                                           │
// │ Type of text-based anchor for locating insertion points.                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Type of text-based anchor for locating insertion points.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5c.</para>
/// </remarks>
public enum SnippetAnchorType
{
    /// <summary>
    /// Match exact text (literal string match).
    /// </summary>
    ExactText = 0,

    /// <summary>
    /// Match using a regular expression pattern.
    /// </summary>
    Regex = 1,

    /// <summary>
    /// Match a function or method signature.
    /// Pattern should be the function name or partial signature.
    /// </summary>
    FunctionSignature = 2,

    /// <summary>
    /// Match a class, struct, or interface declaration.
    /// Pattern should be the type name.
    /// </summary>
    ClassDeclaration = 3,

    /// <summary>
    /// Match a comment marker (e.g., // TODO, // FIXME).
    /// Pattern should be the marker text.
    /// </summary>
    CommentMarker = 4,

    /// <summary>
    /// Match an import or using statement.
    /// Pattern should be the namespace or module.
    /// </summary>
    ImportStatement = 5,

    /// <summary>
    /// Match a property or field declaration.
    /// Pattern should be the member name.
    /// </summary>
    MemberDeclaration = 6
}
