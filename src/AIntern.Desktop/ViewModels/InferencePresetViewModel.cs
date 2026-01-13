using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for displaying an inference preset in dropdown menus and lists.
/// </summary>
/// <remarks>
/// <para>
/// This lightweight ViewModel wraps <see cref="InferencePreset"/> domain model data
/// for display in the UI. It provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Display Properties:</b> Name, Description, Category for user identification</description></item>
///   <item><description><b>State Flags:</b> IsBuiltIn, IsDefault, IsSelected for visual indicators</description></item>
///   <item><description><b>Computed Text:</b> ParameterSummary and TypeIndicator for quick preview</description></item>
/// </list>
/// <para>
/// <b>Usage Pattern:</b>
/// </para>
/// <code>
/// // In InferenceSettingsViewModel:
/// var presets = await _settingsService.GetPresetsAsync();
/// Presets.Clear();
/// foreach (var preset in presets)
/// {
///     Presets.Add(new InferencePresetViewModel(preset));
/// }
/// </code>
/// <para>
/// <b>Thread Safety:</b> This ViewModel is designed for UI thread usage only.
/// Property changes should be made on the UI thread.
/// </para>
/// </remarks>
/// <seealso cref="InferencePreset"/>
/// <seealso cref="InferenceSettingsViewModel"/>
public partial class InferencePresetViewModel : ViewModelBase
{
    #region Properties

    /// <summary>
    /// Gets the unique identifier for this preset.
    /// </summary>
    /// <value>
    /// A <see cref="Guid"/> that uniquely identifies this preset in the repository.
    /// </value>
    /// <remarks>
    /// <para>
    /// This value is immutable after construction and matches
    /// <see cref="InferencePreset.Id"/>.
    /// </para>
    /// </remarks>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the display name of the preset.
    /// </summary>
    /// <value>
    /// A user-friendly name like "Balanced", "Creative", or "My Custom Preset".
    /// </value>
    /// <remarks>
    /// <para>
    /// The name is the primary identification shown in dropdown menus.
    /// For user presets, this is editable; for built-in presets, it's fixed.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the optional description explaining the preset's purpose.
    /// </summary>
    /// <value>
    /// Descriptive text like "Good for creative writing with high variation",
    /// or <c>null</c> if no description is provided.
    /// </value>
    /// <remarks>
    /// <para>
    /// Displayed as a tooltip or subtitle in the UI to help users
    /// understand what each preset is optimized for.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string? _description;

    /// <summary>
    /// Gets or sets the category for grouping related presets.
    /// </summary>
    /// <value>
    /// Category text like "General", "Code", "Creative", or <c>null</c> if uncategorized.
    /// </value>
    /// <remarks>
    /// <para>
    /// Used for visual grouping in dropdown menus. Built-in presets
    /// typically use "General", while user presets may be uncategorized.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string? _category;

    /// <summary>
    /// Gets or sets whether this is a built-in (system) preset.
    /// </summary>
    /// <value>
    /// <c>true</c> for presets like "Balanced", "Precise", "Creative";
    /// <c>false</c> for user-created presets.
    /// </value>
    /// <remarks>
    /// <para>
    /// Built-in presets cannot be modified or deleted. The UI uses this
    /// to disable edit/delete buttons for built-in presets.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isBuiltIn;

    /// <summary>
    /// Gets or sets whether this preset is the system default.
    /// </summary>
    /// <value>
    /// <c>true</c> if this preset is used when creating new conversations
    /// or resetting to defaults; otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Only one preset can be the default at a time. The UI may show
    /// a special indicator (star icon, badge) for the default preset.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isDefault;

    /// <summary>
    /// Gets or sets whether this preset is currently selected in the UI.
    /// </summary>
    /// <value>
    /// <c>true</c> if this preset matches the currently active settings;
    /// <c>false</c> otherwise.
    /// </value>
    /// <remarks>
    /// <para>
    /// This is a UI state property, not persisted. It's updated by
    /// <see cref="InferenceSettingsViewModel"/> when the active preset changes.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets a formatted summary of key parameter values.
    /// </summary>
    /// <value>
    /// A compact string like "Temp: 0.7, TopP: 0.90, Max: 2048"
    /// showing the most important settings at a glance.
    /// </value>
    /// <remarks>
    /// <para>
    /// Displayed in dropdown item tooltips or detail views to help
    /// users compare presets without opening the full settings panel.
    /// </para>
    /// <para>
    /// This value is computed once at construction and does not update
    /// if the underlying preset is modified.
    /// </para>
    /// </remarks>
    public string ParameterSummary { get; init; } = string.Empty;

    /// <summary>
    /// Gets a human-readable indicator of the preset type.
    /// </summary>
    /// <value>
    /// "Built-in" for system presets; "Custom" for user-created presets.
    /// </value>
    /// <remarks>
    /// <para>
    /// Used as a badge or label in the UI to distinguish preset origins.
    /// Dynamically computed from <see cref="IsBuiltIn"/>.
    /// </para>
    /// </remarks>
    public string TypeIndicator => IsBuiltIn ? "Built-in" : "Custom";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="InferencePresetViewModel"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parameterless constructor required for design-time data and XAML instantiation.
    /// Use <see cref="InferencePresetViewModel(InferencePreset)"/> for runtime construction.
    /// </para>
    /// </remarks>
    public InferencePresetViewModel()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InferencePresetViewModel"/> class
    /// from a domain model preset.
    /// </summary>
    /// <param name="preset">The <see cref="InferencePreset"/> to wrap for display.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="preset"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Maps all relevant properties from the domain model to observable properties
    /// for UI binding. The <see cref="ParameterSummary"/> is computed once at construction.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var preset = await settingsService.GetPresetsAsync().FirstAsync();
    /// var vm = new InferencePresetViewModel(preset);
    /// Console.WriteLine($"Loaded preset: {vm.Name} ({vm.TypeIndicator})");
    /// </code>
    /// </example>
    public InferencePresetViewModel(InferencePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        Id = preset.Id;
        Name = preset.Name;
        Description = preset.Description;
        Category = preset.Category;
        IsBuiltIn = preset.IsBuiltIn;
        IsDefault = preset.IsDefault;

        // Compute a compact parameter summary for quick preview
        // Format: "Temp: X.X, TopP: X.XX, Max: XXXX"
        ParameterSummary = $"Temp: {preset.Options.Temperature:F1}, " +
                          $"TopP: {preset.Options.TopP:F2}, " +
                          $"Max: {preset.Options.MaxTokens}";
    }

    #endregion
}
