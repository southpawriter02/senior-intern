namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET APPLY EXCEPTION (v0.4.5d)                                       │
// │ Specialized exception for snippet apply failures.                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// The operation that failed during snippet application.
/// </summary>
public enum SnippetApplyOperation
{
    /// <summary>
    /// Options validation failed.
    /// </summary>
    Validation,

    /// <summary>
    /// Backup creation failed.
    /// </summary>
    Backup,

    /// <summary>
    /// Apply operation failed.
    /// </summary>
    Apply,

    /// <summary>
    /// Indentation detection failed.
    /// </summary>
    IndentationDetection,

    /// <summary>
    /// Location suggestion failed.
    /// </summary>
    LocationSuggestion
}

/// <summary>
/// Exception thrown when snippet application fails.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5d.</para>
/// </remarks>
public class SnippetApplyException : Exception
{
    /// <summary>
    /// Target file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The operation that failed.
    /// </summary>
    public SnippetApplyOperation Operation { get; }

    /// <summary>
    /// Additional details about the failure.
    /// </summary>
    public string? InnerDetails { get; }

    public SnippetApplyException(
        string filePath,
        SnippetApplyOperation operation,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
        Operation = operation;
        InnerDetails = innerException?.Message;
    }
}
