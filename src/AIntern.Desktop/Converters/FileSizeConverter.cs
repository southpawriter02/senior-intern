// -----------------------------------------------------------------------
// <copyright file="FileSizeConverter.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Converts byte counts to human-readable file sizes.
//     Added in v0.4.5e.
// </summary>
// -----------------------------------------------------------------------

using System.Globalization;
using Avalonia.Data.Converters;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts byte counts to human-readable file size strings.
/// </summary>
/// <remarks>
/// <para>
/// Formats:
/// <list type="bullet">
///   <item><description>Bytes: "512 B"</description></item>
///   <item><description>Kilobytes: "1.5 KB"</description></item>
///   <item><description>Megabytes: "2.3 MB"</description></item>
///   <item><description>Gigabytes: "1.0 GB"</description></item>
/// </list>
/// </para>
/// <para>Added in v0.4.5e.</para>
/// </remarks>
public sealed class FileSizeConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static FileSizeConverter Instance { get; } = new();

    private static readonly string[] SizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var bytes = value switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            _ => 0L
        };

        if (bytes <= 0)
        {
            return "0 B";
        }

        var index = 0;
        var size = (double)bytes;

        while (size >= 1024 && index < SizeSuffixes.Length - 1)
        {
            size /= 1024;
            index++;
        }

        // Use no decimal for bytes, 1 decimal for larger units
        return index == 0
            ? $"{size:N0} {SizeSuffixes[index]}"
            : $"{size:N1} {SizeSuffixes[index]}";
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("FileSizeConverter only supports one-way binding.");
    }
}
