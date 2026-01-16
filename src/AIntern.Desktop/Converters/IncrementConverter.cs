using Avalonia.Data.Converters;
using System.Globalization;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INCREMENT CONVERTER (v0.4.2e)                                            │
// │ Converts 0-based index to 1-based display number.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts a 0-based index to a 1-based display number for user-friendly display.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2e.</para>
/// <para>
/// Used primarily in the diff viewer for displaying hunk position (e.g., "2/5" instead of "1/5").
/// </para>
/// </remarks>
public sealed class IncrementConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for XAML static resource usage.
    /// </summary>
    public static readonly IncrementConverter Instance = new();

    /// <summary>
    /// Converts a 0-based integer to 1-based.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue + 1;
        }
        return value;
    }

    /// <summary>
    /// Converts a 1-based integer back to 0-based.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue - 1;
        }
        return value;
    }
}
