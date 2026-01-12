namespace AIntern.Core.Exceptions;

/// <summary>
/// Base exception for all LLM-related errors in the AIntern application.
/// </summary>
/// <remarks>
/// Derived exceptions provide more specific context:
/// <list type="bullet">
/// <item><see cref="ModelLoadException"/> - Model file not found or load failure</item>
/// <item><see cref="InferenceException"/> - Text generation errors</item>
/// </list>
/// </remarks>
public class LlmException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LlmException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message describing what went wrong.</param>
    public LlmException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message describing what went wrong.</param>
    /// <param name="innerException">The underlying exception that caused this error.</param>
    public LlmException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a model fails to load.
/// </summary>
/// <remarks>
/// Common causes include:
/// <list type="bullet">
/// <item>Model file not found at specified path</item>
/// <item>Corrupted or invalid GGUF file format</item>
/// <item>Insufficient memory (RAM or VRAM) to load model</item>
/// <item>Unsupported quantization format</item>
/// </list>
/// </remarks>
public class ModelLoadException : LlmException
{
    /// <summary>
    /// Gets the path to the model that failed to load.
    /// Useful for error messages and logging.
    /// </summary>
    public string? ModelPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelLoadException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the load failure.</param>
    /// <param name="modelPath">The path to the model that failed to load.</param>
    /// <param name="innerException">The underlying exception that caused the load failure.</param>
    public ModelLoadException(string message, string? modelPath = null, Exception? innerException = null)
        : base(message, innerException!)
    {
        ModelPath = modelPath;
    }
}

/// <summary>
/// Exception thrown when text generation inference fails.
/// </summary>
/// <remarks>
/// Common causes include:
/// <list type="bullet">
/// <item>No model is currently loaded</item>
/// <item>Model context exhausted (too many tokens)</item>
/// <item>Internal LLamaSharp error during generation</item>
/// </list>
/// </remarks>
public class InferenceException : LlmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the inference failure.</param>
    /// <param name="innerException">The underlying exception that caused the failure.</param>
    public InferenceException(string message, Exception? innerException = null)
        : base(message, innerException!) { }
}
