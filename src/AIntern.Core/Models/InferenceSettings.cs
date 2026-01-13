namespace AIntern.Core.Models;

/// <summary>
/// Value object representing inference settings for LLM text generation.
/// </summary>
/// <remarks>
/// <para>
/// This class encapsulates all parameters that control how the language model
/// generates text responses. Each parameter affects different aspects of generation:
/// </para>
/// <list type="bullet">
///   <item><description><b>Temperature:</b> Controls randomness (0.0-2.0)</description></item>
///   <item><description><b>TopP:</b> Nucleus sampling threshold (0.0-1.0)</description></item>
///   <item><description><b>TopK:</b> Limits token selection (0-100)</description></item>
///   <item><description><b>RepetitionPenalty:</b> Discourages repetition (1.0-2.0)</description></item>
///   <item><description><b>MaxTokens:</b> Response length limit (64-8192)</description></item>
///   <item><description><b>ContextSize:</b> Context window (512-32768)</description></item>
///   <item><description><b>Seed:</b> Random seed (-1 for random)</description></item>
/// </list>
/// <para>
/// The class provides:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Clone"/>: Deep copy for creating independent instances</description></item>
///   <item><description><see cref="Validate"/>: Range validation returning <see cref="ValidationResult"/></description></item>
/// </list>
/// <para>
/// Default values are sourced from <see cref="ParameterConstants"/> to ensure consistency
/// across the application.
/// </para>
/// </remarks>
/// <example>
/// Creating and validating inference settings:
/// <code>
/// var settings = new InferenceSettings
/// {
///     Temperature = 1.2f,
///     MaxTokens = 4096
/// };
///
/// var result = settings.Validate();
/// if (!result.IsValid)
/// {
///     Console.WriteLine(result.GetAllErrors());
/// }
/// </code>
/// </example>
public sealed class InferenceSettings
{
    #region Sampling Parameters

    /// <summary>
    /// Gets or sets the temperature parameter controlling randomness.
    /// </summary>
    /// <value>
    /// A float between 0.0 and 2.0. Default is 0.7.
    /// </value>
    /// <remarks>
    /// <para>
    /// Temperature controls the randomness of token selection:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Low (0.0-0.3):</b> More focused, deterministic outputs</description></item>
    ///   <item><description><b>Medium (0.5-0.9):</b> Balanced creativity and coherence</description></item>
    ///   <item><description><b>High (1.0-2.0):</b> More creative, potentially unpredictable</description></item>
    /// </list>
    /// </remarks>
    public float Temperature { get; set; } = ParameterConstants.Temperature.Default;

    /// <summary>
    /// Gets or sets the Top-P (nucleus sampling) parameter.
    /// </summary>
    /// <value>
    /// A float between 0.0 and 1.0. Default is 0.9.
    /// </value>
    /// <remarks>
    /// <para>
    /// Top-P controls diversity by sampling from the smallest set of tokens
    /// whose cumulative probability exceeds P.
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>0.9:</b> Considers tokens in the top 90% probability mass</description></item>
    ///   <item><description><b>1.0:</b> Considers all tokens (disabled)</description></item>
    /// </list>
    /// </remarks>
    public float TopP { get; set; } = ParameterConstants.TopP.Default;

    /// <summary>
    /// Gets or sets the Top-K parameter limiting token selection.
    /// </summary>
    /// <value>
    /// An integer between 0 and 100. Default is 40. 0 disables Top-K.
    /// </value>
    /// <remarks>
    /// <para>
    /// Top-K limits selection to the K most likely next tokens:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Lower values:</b> More focused outputs</description></item>
    ///   <item><description><b>Higher values:</b> More diversity</description></item>
    ///   <item><description><b>0:</b> Disabled (no limit)</description></item>
    /// </list>
    /// </remarks>
    public int TopK { get; set; } = ParameterConstants.TopK.Default;

    /// <summary>
    /// Gets or sets the repetition penalty to reduce repetition.
    /// </summary>
    /// <value>
    /// A float between 1.0 and 2.0. Default is 1.1.
    /// </value>
    /// <remarks>
    /// <para>
    /// The repetition penalty discourages repeating tokens:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>1.0:</b> No penalty (disabled)</description></item>
    ///   <item><description><b>1.1:</b> Light penalty (recommended)</description></item>
    ///   <item><description><b>1.5+:</b> Strong penalty (may affect coherence)</description></item>
    /// </list>
    /// </remarks>
    public float RepetitionPenalty { get; set; } = ParameterConstants.RepetitionPenalty.Default;

    /// <summary>
    /// Gets or sets the random seed for reproducible generation.
    /// </summary>
    /// <value>
    /// An integer. -1 means use a random seed each time. Default is -1.
    /// </value>
    /// <remarks>
    /// <para>
    /// The seed controls random number generation:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>-1:</b> Use a random seed each generation (non-reproducible)</description></item>
    ///   <item><description><b>0+:</b> Use the specified seed for reproducible output</description></item>
    /// </list>
    /// </remarks>
    public int Seed { get; set; } = ParameterConstants.Seed.Default;

    #endregion

    #region Length Parameters

    /// <summary>
    /// Gets or sets the maximum tokens to generate in response.
    /// </summary>
    /// <value>
    /// An integer between 64 and 8192. Default is 2048.
    /// </value>
    /// <remarks>
    /// <para>
    /// Limits the length of generated responses:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>512-1024:</b> Short responses</description></item>
    ///   <item><description><b>2048-4096:</b> Medium responses</description></item>
    ///   <item><description><b>4096-8192:</b> Long responses</description></item>
    /// </list>
    /// <para>
    /// Higher values allow longer responses but use more memory.
    /// </para>
    /// </remarks>
    public int MaxTokens { get; set; } = ParameterConstants.MaxTokens.Default;

    /// <summary>
    /// Gets or sets the context window size in tokens.
    /// </summary>
    /// <value>
    /// An unsigned integer between 512 and 32768. Default is 4096.
    /// </value>
    /// <remarks>
    /// <para>
    /// Total tokens available for prompt plus response:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>2048-4096:</b> Standard conversations</description></item>
    ///   <item><description><b>8192-16384:</b> Extended conversations</description></item>
    ///   <item><description><b>32768:</b> Very long documents</description></item>
    /// </list>
    /// <para>
    /// Must not exceed the model's maximum context length.
    /// </para>
    /// </remarks>
    public uint ContextSize { get; set; } = ParameterConstants.ContextSize.Default;

    #endregion

    #region Methods

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    /// <returns>
    /// A new <see cref="InferenceSettings"/> instance with the same property values.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned instance is completely independent of this instance.
    /// Changes to the clone will not affect the original, and vice versa.
    /// </para>
    /// <para>
    /// Use this when you need to create a preset from existing settings
    /// or when passing settings to a component that might modify them.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var original = new InferenceSettings { Temperature = 1.5f };
    /// var clone = original.Clone();
    /// clone.Temperature = 0.5f; // Does not affect original
    /// </code>
    /// </example>
    public InferenceSettings Clone() => new()
    {
        Temperature = Temperature,
        TopP = TopP,
        TopK = TopK,
        RepetitionPenalty = RepetitionPenalty,
        Seed = Seed,
        MaxTokens = MaxTokens,
        ContextSize = ContextSize
    };

    /// <summary>
    /// Validates that all parameter values are within their allowed ranges.
    /// </summary>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating whether validation passed
    /// and containing any error messages for out-of-range values.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks all parameters against the ranges defined in
    /// <see cref="ParameterConstants"/>. Each out-of-range value adds
    /// an error message to the result.
    /// </para>
    /// <para>
    /// Parameters validated:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Temperature: 0.0-2.0</description></item>
    ///   <item><description>TopP: 0.0-1.0</description></item>
    ///   <item><description>TopK: 0-100</description></item>
    ///   <item><description>RepetitionPenalty: 1.0-2.0</description></item>
    ///   <item><description>MaxTokens: 64-8192</description></item>
    ///   <item><description>ContextSize: 512-32768</description></item>
    ///   <item><description>Seed: -1 or greater</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new InferenceOptions { Temperature = 3.0f };
    /// var result = options.Validate();
    ///
    /// if (!result.IsValid)
    /// {
    ///     foreach (var error in result.Errors)
    ///     {
    ///         Console.WriteLine(error);
    ///     }
    /// }
    /// </code>
    /// </example>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        // Validate Temperature
        if (Temperature < ParameterConstants.Temperature.Min ||
            Temperature > ParameterConstants.Temperature.Max)
        {
            errors.Add($"Temperature must be between {ParameterConstants.Temperature.Min} and {ParameterConstants.Temperature.Max}. Current value: {Temperature}");
        }

        // Validate TopP
        if (TopP < ParameterConstants.TopP.Min ||
            TopP > ParameterConstants.TopP.Max)
        {
            errors.Add($"Top-P must be between {ParameterConstants.TopP.Min} and {ParameterConstants.TopP.Max}. Current value: {TopP}");
        }

        // Validate TopK
        if (TopK < ParameterConstants.TopK.Min ||
            TopK > ParameterConstants.TopK.Max)
        {
            errors.Add($"Top-K must be between {ParameterConstants.TopK.Min} and {ParameterConstants.TopK.Max}. Current value: {TopK}");
        }

        // Validate RepetitionPenalty
        if (RepetitionPenalty < ParameterConstants.RepetitionPenalty.Min ||
            RepetitionPenalty > ParameterConstants.RepetitionPenalty.Max)
        {
            errors.Add($"Repetition Penalty must be between {ParameterConstants.RepetitionPenalty.Min} and {ParameterConstants.RepetitionPenalty.Max}. Current value: {RepetitionPenalty}");
        }

        // Validate MaxTokens
        if (MaxTokens < ParameterConstants.MaxTokens.Min ||
            MaxTokens > ParameterConstants.MaxTokens.Max)
        {
            errors.Add($"Max Tokens must be between {ParameterConstants.MaxTokens.Min} and {ParameterConstants.MaxTokens.Max}. Current value: {MaxTokens}");
        }

        // Validate ContextSize
        if (ContextSize < ParameterConstants.ContextSize.Min ||
            ContextSize > ParameterConstants.ContextSize.Max)
        {
            errors.Add($"Context Size must be between {ParameterConstants.ContextSize.Min} and {ParameterConstants.ContextSize.Max}. Current value: {ContextSize}");
        }

        // Validate Seed
        if (Seed < ParameterConstants.Seed.Min)
        {
            errors.Add($"Seed must be {ParameterConstants.Seed.Min} or greater. Current value: {Seed}");
        }

        return errors.Count == 0
            ? ValidationResult.Success
            : ValidationResult.Failure(errors);
    }

    #endregion
}
