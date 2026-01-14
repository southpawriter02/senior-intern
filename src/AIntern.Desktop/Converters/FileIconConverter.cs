namespace AIntern.Desktop.Converters;

using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Desktop.Utilities;
using System.Globalization;

/// <summary>
/// Converts an icon key to a StreamGeometry for PathIcon rendering.
/// </summary>
/// <remarks>Added in v0.3.2c.</remarks>
public class FileIconConverter : IValueConverter
{
    /// <summary>Singleton instance for XAML usage.</summary>
    public static FileIconConverter Instance { get; } = new();

    /// <summary>
    /// Converts an icon key string to a StreamGeometry.
    /// </summary>
    /// <param name="value">Icon key (e.g., "file-csharp").</param>
    /// <param name="targetType">Target type (StreamGeometry).</param>
    /// <param name="parameter">Optional parameter (unused).</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>Parsed StreamGeometry from SVG path data.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var iconKey = value as string ?? "file";
        var pathData = FileIconProvider.GetIconPath(iconKey);
        return StreamGeometry.Parse(pathData);
    }

    /// <summary>Not supported.</summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("FileIconConverter only supports one-way conversion.");
}
