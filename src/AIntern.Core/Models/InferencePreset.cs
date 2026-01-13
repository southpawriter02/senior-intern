using AIntern.Core.Entities;

namespace AIntern.Core.Models;

/// <summary>
/// Domain model representing a named configuration of inference options.
/// </summary>
/// <remarks>
/// <para>
/// Presets allow users to save and quickly switch between different inference
/// configurations. The system includes built-in presets for common use cases
/// and supports user-created custom presets.
/// </para>
/// <para>
/// <b>Built-in Presets:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Balanced:</b> Default preset for general use (Temperature: 0.7)</description></item>
///   <item><description><b>Precise:</b> Factual, deterministic responses (Temperature: 0.2)</description></item>
///   <item><description><b>Creative:</b> Brainstorming and creative writing (Temperature: 1.2)</description></item>
///   <item><description><b>Long-form:</b> Extended context for detailed work (Context: 16384)</description></item>
///   <item><description><b>Code Review:</b> Optimized for code analysis (Temperature: 0.3)</description></item>
/// </list>
/// <para>
/// <b>Key Features:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="FromOptions"/>: Factory method for creating presets</description></item>
///   <item><description><see cref="ToEntity"/>: Convert to database entity</description></item>
///   <item><description><see cref="FromEntity"/>: Create from database entity</description></item>
/// </list>
/// </remarks>
/// <example>
/// Creating a preset from options:
/// <code>
/// var options = new InferenceSettings { Temperature = 0.3f, MaxTokens = 4096 };
/// var preset = InferencePreset.FromOptions("My Custom Preset", options);
/// preset.Description = "Optimized for code generation";
/// preset.Category = "Code";
/// </code>
/// </example>
public sealed class InferencePreset
{
    #region Well-Known Preset IDs

    /// <summary>Well-known GUID for the Precise preset.</summary>
    public static readonly Guid PrecisePresetId = new("00000001-0000-0000-0000-000000000001");

    /// <summary>Well-known GUID for the Balanced preset (default).</summary>
    public static readonly Guid BalancedPresetId = new("00000001-0000-0000-0000-000000000002");

    /// <summary>Well-known GUID for the Creative preset.</summary>
    public static readonly Guid CreativePresetId = new("00000001-0000-0000-0000-000000000003");

    /// <summary>Well-known GUID for the Long-form preset.</summary>
    public static readonly Guid LongFormPresetId = new("00000001-0000-0000-0000-000000000004");

    /// <summary>Well-known GUID for the Code Review preset.</summary>
    public static readonly Guid CodeReviewPresetId = new("00000001-0000-0000-0000-000000000005");

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the unique identifier for the preset.
    /// </summary>
    /// <value>A GUID that uniquely identifies this preset.</value>
    /// <remarks>
    /// Generated automatically when creating a new preset.
    /// Built-in presets use well-known GUIDs for stable references.
    /// </remarks>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name for the preset.
    /// </summary>
    /// <value>A unique name for the preset. Required, max 100 characters.</value>
    /// <remarks>
    /// Names must be unique across all presets. Used in the UI for selection.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of the preset's use case.
    /// </summary>
    /// <value>A description explaining when to use this preset. Max 500 characters.</value>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category for grouping presets.
    /// </summary>
    /// <value>A category string (e.g., "General", "Code", "Creative"). Max 50 characters.</value>
    /// <remarks>
    /// Categories help organize presets in the UI.
    /// </remarks>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether this is a built-in preset.
    /// </summary>
    /// <value>True if this is a built-in preset; false for user-created presets.</value>
    /// <remarks>
    /// Built-in presets cannot be deleted but can be duplicated for customization.
    /// </remarks>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default preset.
    /// </summary>
    /// <value>True if this is the default preset for new conversations.</value>
    /// <remarks>
    /// Only one preset should have this set to true at a time.
    /// </remarks>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the number of times this preset has been used.
    /// </summary>
    /// <value>A non-negative integer tracking usage. Default is 0.</value>
    /// <remarks>
    /// Incremented each time the preset is selected for generation.
    /// Used for "most used" sorting and analytics.
    /// </remarks>
    public int UsageCount { get; set; }

    /// <summary>
    /// Gets or sets when the preset was created.
    /// </summary>
    /// <value>UTC timestamp of creation.</value>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the preset was last modified.
    /// </summary>
    /// <value>UTC timestamp of last modification.</value>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the inference options for this preset.
    /// </summary>
    /// <value>
    /// The inference parameters. Never null; initialized to defaults.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property contains all the actual inference parameters
    /// (Temperature, TopP, MaxTokens, etc.) that will be applied
    /// when using this preset.
    /// </para>
    /// <para>
    /// When setting this property, consider using <see cref="InferenceSettings.Clone"/>
    /// to prevent unintended sharing of the options instance.
    /// </para>
    /// </remarks>
    public InferenceSettings Options { get; set; } = new();

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new preset from inference options.
    /// </summary>
    /// <param name="name">The display name for the preset.</param>
    /// <param name="options">The inference options to use. Will be cloned.</param>
    /// <returns>A new <see cref="InferencePreset"/> with the given name and cloned options.</returns>
    /// <remarks>
    /// <para>
    /// The options are cloned to prevent external modification of the preset's
    /// parameters after creation. The original options instance can be safely
    /// modified without affecting the preset.
    /// </para>
    /// <para>
    /// The preset will have:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>A new unique ID</description></item>
    ///   <item><description>IsBuiltIn = false</description></item>
    ///   <item><description>IsDefault = false</description></item>
    ///   <item><description>UsageCount = 0</description></item>
    ///   <item><description>Current UTC timestamps</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new InferenceSettings { Temperature = 0.3f };
    /// var preset = InferencePreset.FromOptions("Code Preset", options);
    ///
    /// // Modifying original options doesn't affect the preset
    /// options.Temperature = 1.0f;
    /// // preset.Options.Temperature is still 0.3f
    /// </code>
    /// </example>
    public static InferencePreset FromOptions(string name, InferenceSettings options)
    {
        return new InferencePreset
        {
            Id = Guid.NewGuid(),
            Name = name,
            Options = options.Clone(),
            IsBuiltIn = false,
            IsDefault = false,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Entity Mapping

    /// <summary>
    /// Converts this domain model to a database entity.
    /// </summary>
    /// <returns>
    /// An <see cref="InferencePresetEntity"/> with properties copied from this preset.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The entity flattens the nested <see cref="Options"/> into individual properties
    /// for database storage. This avoids complex JSON serialization in the database.
    /// </para>
    /// <para>
    /// All properties are copied, including the Id, so the same entity can be used
    /// for both inserts and updates.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var preset = InferencePreset.FromOptions("Test", new InferenceSettings());
    /// var entity = preset.ToEntity();
    /// await _context.InferencePresets.AddAsync(entity);
    /// </code>
    /// </example>
    public InferencePresetEntity ToEntity()
    {
        return new InferencePresetEntity
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Category = Category,
            Temperature = Options.Temperature,
            TopP = Options.TopP,
            TopK = Options.TopK,
            RepeatPenalty = Options.RepetitionPenalty,
            MaxTokens = Options.MaxTokens,
            ContextSize = (int)Options.ContextSize,
            Seed = Options.Seed,
            IsBuiltIn = IsBuiltIn,
            IsDefault = IsDefault,
            UsageCount = UsageCount,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    /// <summary>
    /// Creates a domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity to convert.</param>
    /// <returns>
    /// A new <see cref="InferencePreset"/> with properties copied from the entity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method reconstructs the nested <see cref="InferenceSettings"/> from
    /// the entity's flattened properties.
    /// </para>
    /// <para>
    /// Note: The entity's <see cref="InferencePresetEntity.RepeatPenalty"/>
    /// maps to <see cref="InferenceSettings.RepetitionPenalty"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entity"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// var entity = await _context.InferencePresets.FindAsync(id);
    /// if (entity != null)
    /// {
    ///     var preset = InferencePreset.FromEntity(entity);
    /// }
    /// </code>
    /// </example>
    public static InferencePreset FromEntity(InferencePresetEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new InferencePreset
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Category = entity.Category,
            IsBuiltIn = entity.IsBuiltIn,
            IsDefault = entity.IsDefault,
            UsageCount = entity.UsageCount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Options = new InferenceSettings
            {
                Temperature = entity.Temperature,
                TopP = entity.TopP,
                TopK = entity.TopK,
                RepetitionPenalty = entity.RepeatPenalty,
                MaxTokens = entity.MaxTokens,
                ContextSize = (uint)entity.ContextSize,
                Seed = entity.Seed
            }
        };
    }

    #endregion
}
