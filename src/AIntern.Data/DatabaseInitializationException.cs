namespace AIntern.Data;

/// <summary>
/// Exception thrown when database initialization fails.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by <see cref="DatabaseInitializer.InitializeAsync"/>
/// when any step of the initialization process fails, including:
/// </para>
/// <list type="bullet">
///   <item><description>Migration application failures</description></item>
///   <item><description>Database creation errors</description></item>
///   <item><description>Seed data insertion failures</description></item>
/// </list>
/// <para>
/// The inner exception contains the original error details.
/// </para>
/// </remarks>
public class DatabaseInitializationException : Exception
{
    /// <summary>
    /// Creates a new database initialization exception.
    /// </summary>
    public DatabaseInitializationException()
    {
    }

    /// <summary>
    /// Creates a new database initialization exception with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DatabaseInitializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new database initialization exception with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this failure.</param>
    public DatabaseInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
