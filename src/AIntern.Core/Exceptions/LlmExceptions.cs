namespace SeniorIntern.Core.Exceptions;

public class LlmException : Exception
{
    public LlmException(string message) : base(message) { }
    public LlmException(string message, Exception innerException) : base(message, innerException) { }
}

public class ModelLoadException : LlmException
{
    public string? ModelPath { get; }

    public ModelLoadException(string message, string? modelPath = null, Exception? innerException = null)
        : base(message, innerException!)
    {
        ModelPath = modelPath;
    }
}

public class InferenceException : LlmException
{
    public InferenceException(string message, Exception? innerException = null)
        : base(message, innerException!) { }
}
