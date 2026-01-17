// -----------------------------------------------------------------------
// <copyright file="ConfidenceToColorConverter.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Converts confidence values to color brushes.
//     Added in v0.4.5e.
// </summary>
// -----------------------------------------------------------------------

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a confidence value (0.0 to 1.0) to a color brush.
/// </summary>
/// <remarks>
/// <para>
/// Thresholds:
/// <list type="bullet">
///   <item><description>High confidence (≥0.8): Green</description></item>
///   <item><description>Medium confidence (≥0.5): Orange</description></item>
///   <item><description>Low confidence (&lt;0.5): Gray</description></item>
/// </list>
/// </para>
/// <para>Added in v0.4.5e.</para>
/// </remarks>
public sealed class ConfidenceToColorConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static ConfidenceToColorConverter Instance { get; } = new();

    /// <summary>
    /// Brush for high confidence values (≥0.8).
    /// </summary>
    public IBrush HighConfidenceBrush { get; set; } = new SolidColorBrush(Color.FromRgb(34, 197, 94));

    /// <summary>
    /// Brush for medium confidence values (≥0.5).
    /// </summary>
    public IBrush MediumConfidenceBrush { get; set; } = new SolidColorBrush(Color.FromRgb(249, 115, 22));

    /// <summary>
    /// Brush for low confidence values (&lt;0.5).
    /// </summary>
    public IBrush LowConfidenceBrush { get; set; } = new SolidColorBrush(Color.FromRgb(156, 163, 175));

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var confidence = value switch
        {
            double d => d,
            float f => f,
            int i => i / 100.0,
            _ => 0.0
        };

        return confidence switch
        {
            >= 0.8 => HighConfidenceBrush,
            >= 0.5 => MediumConfidenceBrush,
            _ => LowConfidenceBrush
        };
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ConfidenceToColorConverter only supports one-way binding.");
    }
}
