using System.Globalization;
using Avalonia.Data.Converters;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts token count integers to formatted strings with thousands separators.
/// </summary>
public sealed class TokenCountConverter : IValueConverter
{
    public static TokenCountConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int count => count.ToString("N0", culture),
            long longCount => longCount.ToString("N0", culture),
            _ => value?.ToString() ?? "0"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            // Remove thousands separators and parse
            var cleanedStr = str.Replace(",", "").Replace(" ", "");
            if (int.TryParse(cleanedStr, NumberStyles.Integer, culture, out var result))
            {
                return result;
            }
        }
        return 0;
    }
}
