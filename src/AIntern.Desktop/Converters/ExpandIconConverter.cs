// ============================================================================
// ExpandIconConverter.cs
// AIntern.Desktop - Inference Settings Panel (v0.2.3e)
// ============================================================================
// Converts a boolean expanded state to chevron icon geometry for the expand/
// collapse toggle button in the inference settings panel header.
// ============================================================================

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AIntern.Desktop.Converters;

/// <summary>
/// Converts a boolean expanded state to chevron icon <see cref="StreamGeometry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This converter is used in the inference settings panel header to show the
/// appropriate expand/collapse indicator:
/// </para>
/// <para>
/// <b>Conversion Logic:</b>
/// <list type="bullet">
///   <item><c>true</c> (expanded) → Chevron Down icon (▼)</item>
///   <item><c>false</c> (collapsed) → Chevron Right icon (▶)</item>
///   <item><c>null</c> or non-boolean → Chevron Right icon (collapsed state)</item>
/// </list>
/// </para>
/// <para>
/// <b>Icon Geometries:</b>
/// The converter uses 24x24 viewport chevron paths consistent with the icons
/// defined in Icons.axaml (ChevronDownIcon, ChevronRightIcon).
/// </para>
/// <para>
/// <b>XAML Usage:</b>
/// <code>
/// &lt;PathIcon Data="{Binding IsExpanded, Converter={x:Static converters:ExpandIconConverter.Instance}}" /&gt;
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="InferenceSettingsViewModel"/>
public sealed class ExpandIconConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="ExpandIconConverter"/>.
    /// </summary>
    /// <remarks>
    /// Using a singleton pattern avoids unnecessary allocations and is consistent
    /// with other converters in the application (e.g., <see cref="BoolToSelectedConverter"/>).
    /// </remarks>
    public static ExpandIconConverter Instance { get; } = new();

    /// <summary>
    /// Chevron down geometry (24x24 viewport) for expanded state.
    /// Points downward to indicate content is visible and can be collapsed.
    /// </summary>
    private static readonly StreamGeometry ExpandedIcon =
        StreamGeometry.Parse("M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z");

    /// <summary>
    /// Chevron right geometry (24x24 viewport) for collapsed state.
    /// Points right to indicate content is hidden and can be expanded.
    /// </summary>
    private static readonly StreamGeometry CollapsedIcon =
        StreamGeometry.Parse("M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6-1.41-1.41z");

    /// <summary>
    /// Converts a boolean expanded state to <see cref="StreamGeometry"/> chevron icon.
    /// </summary>
    /// <param name="value">The source value to convert (expected: <see cref="bool"/>).</param>
    /// <param name="targetType">The target type (expected: <see cref="StreamGeometry"/>).</param>
    /// <param name="parameter">Optional conversion parameter (not used).</param>
    /// <param name="culture">The culture to use for conversion (not used).</param>
    /// <returns>
    /// <see cref="ExpandedIcon"/> (chevron down) if <paramref name="value"/> is <c>true</c>;
    /// otherwise <see cref="CollapsedIcon"/> (chevron right).
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? ExpandedIcon : CollapsedIcon;
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    /// <param name="value">The target value to convert back.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">Optional conversion parameter.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>Never returns; always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ExpandIconConverter does not support ConvertBack.");
    }
}
