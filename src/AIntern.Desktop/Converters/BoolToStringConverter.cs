using System.Globalization;
using Avalonia.Data.Converters;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BOOL TO STRING CONVERTER (v0.4.3f)                                       │
// │ Converts a boolean to one of two string values.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts a boolean value to one of two strings.
/// Parameter format: "TrueValue|FalseValue"
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3f.</para>
/// </remarks>
public class BoolToStringConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean to a string based on the parameter.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string paramString)
        {
            return value?.ToString() ?? string.Empty;
        }

        var parts = paramString.Split('|');
        if (parts.Length != 2)
        {
            return value.ToString();
        }

        return boolValue ? parts[0] : parts[1];
    }

    /// <summary>
    /// Not implemented - one-way binding only.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
