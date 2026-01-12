using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a language identifier to an icon geometry.
/// </summary>
public class LanguageToIconConverter : IValueConverter
{
    public static LanguageToIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var language = value as string;

        // Return icon key based on language
        // Uses FileIcon as default fallback
        return GetGeometry(language?.ToLowerInvariant() switch
        {
            "csharp" => "FileIcon",
            "javascript" or "javascriptreact" => "FileIcon",
            "typescript" or "typescriptreact" => "FileIcon",
            "python" => "FileIcon",
            "html" => "FileIcon",
            "css" or "scss" or "less" => "FileIcon",
            "json" or "jsonc" => "FileIcon",
            "markdown" => "FileIcon",
            "xml" => "FileIcon",
            "yaml" or "toml" => "FileIcon",
            "rust" => "FileIcon",
            "go" => "FileIcon",
            "java" or "kotlin" => "FileIcon",
            "shellscript" or "bash" or "powershell" => "FileIcon",
            "dockerfile" => "FileIcon",
            _ => "FileIcon"
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static Geometry? GetGeometry(string resourceKey)
    {
        if (Application.Current?.Resources.TryGetResource(resourceKey, null, out var resource) == true)
            return resource as Geometry;
        return null;
    }
}
