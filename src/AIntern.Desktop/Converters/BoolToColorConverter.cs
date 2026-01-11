using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a boolean value to an accent or muted color.
/// True returns accent color (#00d9ff), false returns muted gray (#888888).
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#00d9ff"));
    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#888888"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? AccentBrush : MutedBrush;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
