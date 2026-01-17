// -----------------------------------------------------------------------
// <copyright file="BrushConverter.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Converts dynamic resource key strings to brushes.
//     Added in v0.4.5e.
// </summary>
// -----------------------------------------------------------------------

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a resource key string to an <see cref="IBrush"/>.
/// </summary>
/// <remarks>
/// <para>
/// Looks up the brush from application resources by key name.
/// Returns a fallback brush if the key is not found.
/// </para>
/// <para>Added in v0.4.5e.</para>
/// </remarks>
public sealed class BrushConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance.  
    /// </summary>
    public static BrushConverter Instance { get; } = new();

    /// <summary>
    /// Fallback brush when resource is not found.
    /// </summary>
    public IBrush FallbackBrush { get; set; } = new SolidColorBrush(Color.FromRgb(156, 163, 175));

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string resourceKey || string.IsNullOrEmpty(resourceKey))
        {
            return FallbackBrush;
        }

        if (Application.Current?.TryGetResource(resourceKey, Application.Current.ActualThemeVariant, out var resource) == true)
        {
            return resource as IBrush ?? FallbackBrush;
        }

        return FallbackBrush;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BrushConverter only supports one-way binding.");
    }
}
