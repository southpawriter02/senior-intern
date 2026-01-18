// -----------------------------------------------------------------------
// <copyright file="BoolToTextConverter.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Desktop.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BOOL TO TEXT CONVERTER (v0.5.4f)                                        │
// │ Converts boolean to one of two text values.                             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts a boolean to one of two text values.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4f.</para>
/// <para>
/// Parameter format: "TrueText|FalseText"
/// </para>
/// <example>
/// <code>
/// Text="{Binding IsExecuting, 
///     Converter={StaticResource BoolToTextConverter}, 
///     ConverterParameter=Running...|Run}"
/// </code>
/// </example>
/// </remarks>
public sealed class BoolToTextConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean to text based on the parameter.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">Target type (ignored).</param>
    /// <param name="parameter">Format: "TrueText|FalseText".</param>
    /// <param name="culture">Culture info (ignored).</param>
    /// <returns>TrueText if value is true, FalseText if false.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Require boolean input
        if (value is not bool boolValue)
            return null;

        // Parse parameter as "TrueText|FalseText"
        var texts = parameter?.ToString()?.Split('|') ?? Array.Empty<string>();
        if (texts.Length != 2)
            return value.ToString();

        return boolValue ? texts[0] : texts[1];
    }

    /// <summary>
    /// Not supported - this is a one-way converter.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BoolToTextConverter does not support ConvertBack.");
    }
}
