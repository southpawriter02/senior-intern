using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Represents the save state of a conversation.
/// </summary>
public enum SaveStatus
{
    Saved,
    Unsaved,
    Saving
}

/// <summary>
/// Converts SaveStatus to display text with icons.
/// </summary>
public sealed class SaveStatusConverter : IValueConverter
{
    public static readonly SaveStatusConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            SaveStatus.Saved => "✓ Saved",
            SaveStatus.Unsaved => "● Unsaved",
            SaveStatus.Saving => "⟳ Saving...",
            _ => string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts SaveStatus to a color brush.
/// Saved = green, Unsaved = yellow/orange, Saving = accent blue.
/// </summary>
public sealed class SaveStatusColorConverter : IValueConverter
{
    public static readonly SaveStatusColorConverter Instance = new();

    private static readonly IBrush SavedBrush = new SolidColorBrush(Color.Parse("#4caf50"));
    private static readonly IBrush UnsavedBrush = new SolidColorBrush(Color.Parse("#ffc107"));
    private static readonly IBrush SavingBrush = new SolidColorBrush(Color.Parse("#00d9ff"));
    private static readonly IBrush DefaultBrush = Brushes.Gray;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            SaveStatus.Saved => SavedBrush,
            SaveStatus.Unsaved => UnsavedBrush,
            SaveStatus.Saving => SavingBrush,
            _ => DefaultBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
