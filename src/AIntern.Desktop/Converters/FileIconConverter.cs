using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Desktop.Utilities;
using System.Globalization;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts an icon key to a StreamGeometry for PathIcon.
/// </summary>
public class FileIconConverter : IValueConverter
{
    /// <summary>Singleton instance for XAML usage.</summary>
    public static FileIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var iconKey = value as string ?? "file";
        var pathData = FileIconProvider.GetIconPath(iconKey);
        return StreamGeometry.Parse(pathData);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
