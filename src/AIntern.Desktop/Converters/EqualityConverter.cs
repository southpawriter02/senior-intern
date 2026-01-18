// -----------------------------------------------------------------------
// <copyright file="EqualityConverter.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Desktop.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ EQUALITY CONVERTER (v0.5.4f)                                            │
// │ Returns true if value equals parameter (case-insensitive).              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converter that returns true if the value equals the parameter.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4f.</para>
/// <para>
/// Used for dynamic class binding based on status values.
/// </para>
/// <example>
/// <code>
/// Classes.success="{Binding StatusClass, 
///     Converter={StaticResource EqualityConverter}, 
///     ConverterParameter=success}"
/// </code>
/// </example>
/// </remarks>
public sealed class EqualityConverter : IValueConverter
{
    /// <summary>
    /// Converts a value by comparing it to the parameter.
    /// </summary>
    /// <param name="value">The value to compare.</param>
    /// <param name="targetType">Target type (ignored).</param>
    /// <param name="parameter">The comparison value.</param>
    /// <param name="culture">Culture info (ignored).</param>
    /// <returns>True if value equals parameter (case-insensitive).</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Both null means equal
        if (value == null && parameter == null)
            return true;

        // One null means not equal
        if (value == null || parameter == null)
            return false;

        // Case-insensitive string comparison
        return string.Equals(
            value.ToString(),
            parameter.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Not supported - this is a one-way converter.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("EqualityConverter does not support ConvertBack.");
    }
}
