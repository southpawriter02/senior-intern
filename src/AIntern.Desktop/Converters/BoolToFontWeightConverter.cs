// ============================================================================
// BoolToFontWeightConverter.cs
// AIntern.Desktop - Inference Settings Panel (v0.2.3e)
// ============================================================================
// Converts a boolean value to FontWeight for emphasizing default presets in
// the preset dropdown. Default presets display in Bold, others in Normal.
// ============================================================================

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a boolean to <see cref="FontWeight"/> for emphasizing default presets.
/// </summary>
/// <remarks>
/// <para>
/// This converter is used in the inference settings panel to visually distinguish
/// the default preset from user-created presets in the ComboBox dropdown.
/// </para>
/// <para>
/// <b>Conversion Logic:</b>
/// <list type="bullet">
///   <item><c>true</c> → <see cref="FontWeight.Bold"/> (default preset)</item>
///   <item><c>false</c> → <see cref="FontWeight.Normal"/> (custom preset)</item>
///   <item><c>null</c> or non-boolean → <see cref="FontWeight.Normal"/></item>
/// </list>
/// </para>
/// <para>
/// <b>XAML Usage:</b>
/// <code>
/// &lt;TextBlock FontWeight="{Binding IsDefault, Converter={x:Static converters:BoolToFontWeightConverter.Instance}}" /&gt;
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="InferencePresetViewModel"/>
public sealed class BoolToFontWeightConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="BoolToFontWeightConverter"/>.
    /// </summary>
    /// <remarks>
    /// Using a singleton pattern avoids unnecessary allocations and is consistent
    /// with other converters in the application (e.g., <see cref="BoolToSelectedConverter"/>).
    /// </remarks>
    public static BoolToFontWeightConverter Instance { get; } = new();

    /// <summary>
    /// Converts a boolean value to <see cref="FontWeight"/>.
    /// </summary>
    /// <param name="value">The source value to convert (expected: <see cref="bool"/>).</param>
    /// <param name="targetType">The target type (expected: <see cref="FontWeight"/>).</param>
    /// <param name="parameter">Optional conversion parameter (not used).</param>
    /// <param name="culture">The culture to use for conversion (not used).</param>
    /// <returns>
    /// <see cref="FontWeight.Bold"/> if <paramref name="value"/> is <c>true</c>;
    /// otherwise <see cref="FontWeight.Normal"/>.
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? FontWeight.Bold : FontWeight.Normal;
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    /// <param name="value">The target value to convert back.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">Optional conversion parameter.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>Never returns; always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BoolToFontWeightConverter does not support ConvertBack.");
    }
}
