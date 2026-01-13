namespace AIntern.Core;

/// <summary>
/// Static constants defining valid ranges and defaults for inference parameters.
/// </summary>
/// <remarks>
/// <para>
/// These constants serve multiple purposes throughout the application:
/// </para>
/// <list type="bullet">
///   <item><description><b>UI slider configuration:</b> Min, max, and step values for parameter sliders</description></item>
///   <item><description><b>Validation logic:</b> Range validation in <see cref="Models.InferenceOptions"/></description></item>
///   <item><description><b>Default values:</b> Sensible defaults for new presets and options</description></item>
/// </list>
/// <para>
/// All values are aligned with LLamaSharp's supported parameter ranges to ensure
/// compatibility with the underlying inference engine.
/// </para>
/// </remarks>
/// <example>
/// Using constants for slider configuration:
/// <code>
/// slider.Minimum = ParameterConstants.Temperature.Min;
/// slider.Maximum = ParameterConstants.Temperature.Max;
/// slider.Value = ParameterConstants.Temperature.Default;
/// slider.SmallChange = ParameterConstants.Temperature.Step;
/// </code>
/// </example>
public static class ParameterConstants
{
    #region Temperature

    /// <summary>
    /// Constants for the Temperature parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Temperature controls the randomness of token selection during generation.
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Low (0.0-0.3):</b> More focused, deterministic outputs</description></item>
    ///   <item><description><b>Medium (0.5-0.9):</b> Balanced creativity and coherence</description></item>
    ///   <item><description><b>High (1.0-2.0):</b> More creative, potentially unpredictable</description></item>
    /// </list>
    /// </remarks>
    public static class Temperature
    {
        /// <summary>Minimum allowed temperature (completely deterministic).</summary>
        public const float Min = 0.0f;

        /// <summary>Maximum allowed temperature (maximum randomness).</summary>
        public const float Max = 2.0f;

        /// <summary>Default temperature for balanced creativity and coherence.</summary>
        public const float Default = 0.7f;

        /// <summary>Step size for slider increments.</summary>
        public const float Step = 0.1f;
    }

    #endregion

    #region TopP (Nucleus Sampling)

    /// <summary>
    /// Constants for the Top-P (nucleus sampling) parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Top-P controls diversity by sampling from the smallest set of tokens
    /// whose cumulative probability exceeds P.
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Low (0.5-0.7):</b> More focused, fewer choices</description></item>
    ///   <item><description><b>Medium (0.8-0.9):</b> Good balance of diversity and coherence</description></item>
    ///   <item><description><b>High (0.95-1.0):</b> More diverse outputs, 1.0 disables Top-P</description></item>
    /// </list>
    /// </remarks>
    public static class TopP
    {
        /// <summary>Minimum Top-P value (most focused).</summary>
        public const float Min = 0.0f;

        /// <summary>Maximum Top-P value (1.0 = disabled, considers all tokens).</summary>
        public const float Max = 1.0f;

        /// <summary>Default Top-P for balanced diversity.</summary>
        public const float Default = 0.9f;

        /// <summary>Step size for slider increments.</summary>
        public const float Step = 0.05f;
    }

    #endregion

    #region TopK

    /// <summary>
    /// Constants for the Top-K parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Top-K limits token selection to the K most probable tokens at each step.
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Low (1-20):</b> Very focused, most probable tokens only</description></item>
    ///   <item><description><b>Medium (30-50):</b> Good balance of focus and diversity</description></item>
    ///   <item><description><b>High (60-100):</b> More diverse token selection</description></item>
    ///   <item><description><b>0:</b> Disables Top-K filtering</description></item>
    /// </list>
    /// </remarks>
    public static class TopK
    {
        /// <summary>Minimum Top-K value (0 = disabled).</summary>
        public const int Min = 0;

        /// <summary>Maximum Top-K value.</summary>
        public const int Max = 100;

        /// <summary>Default Top-K for balanced focus.</summary>
        public const int Default = 40;

        /// <summary>Step size for slider increments.</summary>
        public const int Step = 5;
    }

    #endregion

    #region RepetitionPenalty

    /// <summary>
    /// Constants for the Repetition Penalty parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Repetition penalty discourages the model from repeating tokens that
    /// have already appeared in the context.
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>1.0:</b> No penalty (disabled)</description></item>
    ///   <item><description><b>1.05-1.15:</b> Light penalty, recommended for most uses</description></item>
    ///   <item><description><b>1.2-1.5:</b> Moderate penalty, reduces repetition significantly</description></item>
    ///   <item><description><b>1.5+:</b> Strong penalty, may affect coherence</description></item>
    /// </list>
    /// </remarks>
    public static class RepetitionPenalty
    {
        /// <summary>Minimum penalty (1.0 = disabled).</summary>
        public const float Min = 1.0f;

        /// <summary>Maximum penalty value.</summary>
        public const float Max = 2.0f;

        /// <summary>Default penalty for light repetition discouragement.</summary>
        public const float Default = 1.1f;

        /// <summary>Step size for slider increments.</summary>
        public const float Step = 0.05f;
    }

    #endregion

    #region MaxTokens

    /// <summary>
    /// Constants for the Max Tokens parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Max Tokens limits the maximum length of generated responses.
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>64-512:</b> Short responses, quick answers</description></item>
    ///   <item><description><b>1024-2048:</b> Medium responses, detailed explanations</description></item>
    ///   <item><description><b>4096-8192:</b> Long responses, comprehensive content</description></item>
    /// </list>
    /// <para>
    /// Higher values use more memory and increase generation time.
    /// </para>
    /// </remarks>
    public static class MaxTokens
    {
        /// <summary>Minimum max tokens (short responses).</summary>
        public const int Min = 64;

        /// <summary>Maximum max tokens (very long responses).</summary>
        public const int Max = 8192;

        /// <summary>Default max tokens for medium-length responses.</summary>
        public const int Default = 2048;

        /// <summary>Step size for slider increments.</summary>
        public const int Step = 64;
    }

    #endregion

    #region ContextSize

    /// <summary>
    /// Constants for the Context Size parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Context Size determines the total number of tokens available for
    /// prompt plus response (the context window).
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>512-2048:</b> Small context, limited conversation history</description></item>
    ///   <item><description><b>4096-8192:</b> Medium context, good for most conversations</description></item>
    ///   <item><description><b>16384-32768:</b> Large context, extended conversations or documents</description></item>
    /// </list>
    /// <para>
    /// Must not exceed the model's maximum context length.
    /// Higher values use significantly more memory.
    /// </para>
    /// </remarks>
    public static class ContextSize
    {
        /// <summary>Minimum context size.</summary>
        public const uint Min = 512;

        /// <summary>Maximum context size.</summary>
        public const uint Max = 32768;

        /// <summary>Default context size for general use.</summary>
        public const uint Default = 4096;

        /// <summary>Step size for slider increments.</summary>
        public const uint Step = 512;
    }

    #endregion

    #region Seed

    /// <summary>
    /// Constants for the Seed parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The seed controls random number generation for reproducible outputs.
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>-1:</b> Use a random seed each generation (non-reproducible)</description></item>
    ///   <item><description><b>0+:</b> Use the specified seed for reproducible output</description></item>
    /// </list>
    /// <para>
    /// Using the same seed with identical parameters and prompts will produce
    /// the same output (assuming the same model and hardware).
    /// </para>
    /// </remarks>
    public static class Seed
    {
        /// <summary>Minimum seed value (-1 = random).</summary>
        public const int Min = -1;

        /// <summary>Maximum seed value.</summary>
        public const int Max = int.MaxValue;

        /// <summary>Default seed (-1 = random each time).</summary>
        public const int Default = -1;

        /// <summary>Special value indicating random seed should be used.</summary>
        public const int Random = -1;
    }

    #endregion
}
