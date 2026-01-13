namespace AIntern.Core;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record provides a structured way to return validation results
/// from validation methods like <see cref="Models.InferenceOptions.Validate"/>.
/// </para>
/// <para>
/// The record is designed to be:
/// </para>
/// <list type="bullet">
///   <item><description><b>Immutable:</b> Cannot be modified after creation</description></item>
///   <item><description><b>Value-semantic:</b> Equality is based on content, not reference</description></item>
///   <item><description><b>Factory-method friendly:</b> Static methods for common cases</description></item>
/// </list>
/// </remarks>
/// <example>
/// Using ValidationResult in validation logic:
/// <code>
/// public ValidationResult Validate()
/// {
///     var errors = new List&lt;string&gt;();
///
///     if (Temperature &lt; 0 || Temperature &gt; 2)
///         errors.Add("Temperature must be between 0.0 and 2.0");
///
///     return errors.Count == 0
///         ? ValidationResult.Success
///         : ValidationResult.Failure(errors);
/// }
/// </code>
/// </example>
/// <param name="IsValid">Whether validation passed with no errors.</param>
/// <param name="Errors">Collection of validation error messages. Empty if valid.</param>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    #region Static Factory Methods

    /// <summary>
    /// Gets a successful validation result with no errors.
    /// </summary>
    /// <value>A ValidationResult where IsValid is true and Errors is empty.</value>
    /// <remarks>
    /// This property returns a cached instance for efficiency when validation passes.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (allValuesValid)
    ///     return ValidationResult.Success;
    /// </code>
    /// </example>
    public static ValidationResult Success { get; } = new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a failed validation result with the specified error messages.
    /// </summary>
    /// <param name="errors">One or more validation error messages.</param>
    /// <returns>A ValidationResult where IsValid is false and Errors contains the messages.</returns>
    /// <remarks>
    /// Use this overload when you have a fixed set of error messages known at compile time.
    /// </remarks>
    /// <example>
    /// <code>
    /// return ValidationResult.Failure(
    ///     "Temperature must be between 0.0 and 2.0",
    ///     "TopP must be between 0.0 and 1.0");
    /// </code>
    /// </example>
    public static ValidationResult Failure(params string[] errors)
        => new(false, errors);

    /// <summary>
    /// Creates a failed validation result from an existing error collection.
    /// </summary>
    /// <param name="errors">A collection of validation error messages.</param>
    /// <returns>A ValidationResult where IsValid is false and Errors contains the messages.</returns>
    /// <remarks>
    /// Use this overload when building up errors dynamically in a List or similar collection.
    /// </remarks>
    /// <example>
    /// <code>
    /// var errors = new List&lt;string&gt;();
    /// // ... add errors based on validation checks ...
    /// return ValidationResult.Failure(errors);
    /// </code>
    /// </example>
    public static ValidationResult Failure(IReadOnlyList<string> errors)
        => new(false, errors);

    #endregion

    #region Instance Methods

    /// <summary>
    /// Gets the first error message, or null if validation passed.
    /// </summary>
    /// <value>The first error message if any exist; otherwise, null.</value>
    /// <remarks>
    /// Useful when you only need to display a single error message to the user.
    /// </remarks>
    public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

    /// <summary>
    /// Gets all error messages as a single string, separated by the specified delimiter.
    /// </summary>
    /// <param name="separator">The delimiter to use between error messages. Defaults to newline.</param>
    /// <returns>All error messages joined by the separator, or empty string if valid.</returns>
    /// <remarks>
    /// Useful for displaying all errors in a single text block or log message.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = options.Validate();
    /// if (!result.IsValid)
    /// {
    ///     MessageBox.Show(result.GetAllErrors());
    /// }
    /// </code>
    /// </example>
    public string GetAllErrors(string separator = "\n")
        => string.Join(separator, Errors);

    #endregion
}
