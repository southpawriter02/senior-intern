namespace AIntern.Core.Entities;

/// <summary>
/// Represents a saved configuration of inference parameters.
/// Presets allow users to quickly switch between different generation settings.
/// </summary>
/// <remarks>
/// <para>Inference parameters control how the language model generates text,
/// affecting creativity, consistency, and output length.</para>
/// <para>Key features:</para>
/// <list type="bullet">
///   <item><description>Built-in presets for common use cases</description></item>
///   <item><description>User-created custom presets</description></item>
///   <item><description>Quick switching during conversations</description></item>
/// </list>
/// <para>
/// This entity is configured for Entity Framework Core in v0.2.1c.
/// Built-in presets are seeded during database initialization in v0.2.1e.
/// </para>
/// </remarks>
/// <example>
/// Creating a creative writing preset:
/// <code>
/// var preset = new InferencePresetEntity
/// {
///     Id = Guid.NewGuid(),
///     Name = "Creative",
///     Description = "Higher temperature for creative writing",
///     Temperature = 1.2f,
///     TopP = 0.95f,
///     MaxTokens = 4096,
///     CreatedAt = DateTime.UtcNow,
///     UpdatedAt = DateTime.UtcNow
/// };
/// </code>
/// </example>
public sealed class InferencePresetEntity
{
    #region Primary Key

    /// <summary>
    /// Gets or sets the unique identifier for the preset.
    /// </summary>
    /// <remarks>
    /// Generated as a new GUID when the preset is created.
    /// Used as the primary key in the database.
    /// </remarks>
    public Guid Id { get; set; }

    #endregion

    #region Identity

    /// <summary>
    /// Gets or sets the display name for the preset.
    /// </summary>
    /// <remarks>
    /// <para>Must be unique across all presets.</para>
    /// <para>Maximum length: 100 characters (enforced by EF Core config in v0.2.1c).</para>
    /// <para>Examples: "Balanced", "Creative", "Precise"</para>
    /// <para>Default: empty string</para>
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of the preset's use case.
    /// </summary>
    /// <remarks>
    /// <para>Maximum length: 500 characters (enforced by EF Core config in v0.2.1c).</para>
    /// <para>Helps users understand when to use this preset.</para>
    /// </remarks>
    public string? Description { get; set; }

    #endregion

    #region Sampling Parameters

    /// <summary>
    /// Gets or sets the temperature parameter controlling randomness.
    /// </summary>
    /// <remarks>
    /// <para>Valid range: 0.0 to 2.0</para>
    /// <list type="bullet">
    ///   <item><description>Lower values (0.1-0.3): More focused, deterministic outputs</description></item>
    ///   <item><description>Medium values (0.7-0.9): Balanced creativity and coherence</description></item>
    ///   <item><description>Higher values (1.0-2.0): More creative, potentially chaotic</description></item>
    /// </list>
    /// <para>Default: 0.7</para>
    /// </remarks>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets the Top-P (nucleus sampling) parameter.
    /// </summary>
    /// <remarks>
    /// <para>Valid range: 0.0 to 1.0</para>
    /// <para>Controls diversity by sampling from the smallest set of tokens
    /// whose cumulative probability exceeds P.</para>
    /// <list type="bullet">
    ///   <item><description>0.9: Considers tokens in the top 90% probability mass</description></item>
    ///   <item><description>1.0: Considers all tokens (disabled)</description></item>
    /// </list>
    /// <para>Default: 0.9</para>
    /// </remarks>
    public float TopP { get; set; } = 0.9f;

    /// <summary>
    /// Gets or sets the Top-K parameter limiting token selection.
    /// </summary>
    /// <remarks>
    /// <para>Valid range: 1 to 100 (typically)</para>
    /// <para>Limits selection to the K most likely next tokens.</para>
    /// <list type="bullet">
    ///   <item><description>Lower values: More focused outputs</description></item>
    ///   <item><description>Higher values: More diversity</description></item>
    /// </list>
    /// <para>Default: 40</para>
    /// </remarks>
    public int TopK { get; set; } = 40;

    /// <summary>
    /// Gets or sets the repeat penalty to reduce repetition.
    /// </summary>
    /// <remarks>
    /// <para>Valid range: 1.0 to 2.0</para>
    /// <list type="bullet">
    ///   <item><description>1.0: No penalty (disabled)</description></item>
    ///   <item><description>1.1: Light penalty (recommended)</description></item>
    ///   <item><description>1.5+: Strong penalty (may affect coherence)</description></item>
    /// </list>
    /// <para>Default: 1.1</para>
    /// </remarks>
    public float RepeatPenalty { get; set; } = 1.1f;

    /// <summary>
    /// Gets or sets the random seed for reproducible generation.
    /// </summary>
    /// <remarks>
    /// <para>Valid values:</para>
    /// <list type="bullet">
    ///   <item><description>-1: Use a random seed each generation (non-reproducible)</description></item>
    ///   <item><description>0+: Use the specified seed for reproducible output</description></item>
    /// </list>
    /// <para>Default: -1 (random)</para>
    /// </remarks>
    public int Seed { get; set; } = -1;

    #endregion

    #region Length Parameters

    /// <summary>
    /// Gets or sets the maximum tokens to generate in response.
    /// </summary>
    /// <remarks>
    /// <para>Limits the length of generated responses.</para>
    /// <para>Common values: 512, 1024, 2048, 4096, 8192</para>
    /// <para>Higher values allow longer responses but use more memory.</para>
    /// <para>Default: 2048</para>
    /// </remarks>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the context window size in tokens.
    /// </summary>
    /// <remarks>
    /// <para>Total tokens available for prompt + response.</para>
    /// <para>Must not exceed model's maximum context length.</para>
    /// <para>Common values: 2048, 4096, 8192, 16384, 32768</para>
    /// <para>Higher values allow longer conversations but use more memory.</para>
    /// <para>Default: 4096</para>
    /// </remarks>
    public int ContextSize { get; set; } = 4096;

    #endregion

    #region Flags

    /// <summary>
    /// Gets or sets whether this is the default preset.
    /// </summary>
    /// <remarks>
    /// <para>Only one preset should have this set to true.</para>
    /// <para>The default preset is used for new conversations.</para>
    /// <para>Default: false</para>
    /// </remarks>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this is a built-in preset.
    /// </summary>
    /// <remarks>
    /// <para>Built-in presets are created during database seeding.</para>
    /// <para>They cannot be deleted but can be modified.</para>
    /// <para>Default: false</para>
    /// </remarks>
    public bool IsBuiltIn { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// Gets or sets the category for grouping presets.
    /// </summary>
    /// <remarks>
    /// <para>Categories help organize presets in the UI.</para>
    /// <para>Common categories: "General", "Code", "Creative", "Technical"</para>
    /// <para>Maximum length: 50 characters (enforced by EF Core config in v0.2.3a).</para>
    /// <para>Null indicates no category assignment.</para>
    /// </remarks>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the number of times this preset has been used.
    /// </summary>
    /// <remarks>
    /// <para>Incremented each time the preset is selected for a new message.</para>
    /// <para>Used for analytics and "most used" sorting in the UI.</para>
    /// <para>Default: 0</para>
    /// </remarks>
    public int UsageCount { get; set; }

    #endregion

    #region Timestamps

    /// <summary>
    /// Gets or sets when the preset was created.
    /// </summary>
    /// <remarks>
    /// Set automatically by the DbContext when the entity is added.
    /// Stored as UTC time.
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the preset was last modified.
    /// </summary>
    /// <remarks>
    /// Updated automatically when preset parameters change.
    /// Stored as UTC time.
    /// </remarks>
    public DateTime UpdatedAt { get; set; }

    #endregion
}
