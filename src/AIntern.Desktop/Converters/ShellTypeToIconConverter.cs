// ============================================================================
// File: ShellTypeToIconConverter.cs
// Path: src/AIntern.Desktop/Converters/ShellTypeToIconConverter.cs
// Description: Converts ShellType to PathGeometry for icon display.
// Created: 2026-01-19
// AI Intern v0.5.5f - Terminal Settings Panel
// ============================================================================

namespace AIntern.Desktop.Converters;

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ ShellTypeToIconConverter (v0.5.5f)                                           │
// │ Converts ShellType enum to Geometry for PathIcon display.                   │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts <see cref="ShellType"/> to a PathGeometry for icon display.
/// </summary>
/// <remarks>
/// <para>
/// Used in shell profile lists to display appropriate icons for each shell type.
/// </para>
/// <para>Added in v0.5.5f.</para>
/// </remarks>
public class ShellTypeToIconConverter : IValueConverter
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Icon Geometries
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Bash shell icon (terminal with $).</summary>
    private static readonly Geometry BashIcon = Geometry.Parse(
        "M4 2h16a2 2 0 0 1 2 2v16a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2zm4 4v8l3-3 3 3V6H8z");

    /// <summary>Zsh shell icon.</summary>
    private static readonly Geometry ZshIcon = Geometry.Parse(
        "M4 2h16a2 2 0 0 1 2 2v16a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2zm3 5h10v2H7V7zm3 4h7v2h-7v-2zm-3 4h10v2H7v-2z");

    /// <summary>PowerShell icon (lightning bolt).</summary>
    private static readonly Geometry PowerShellIcon = Geometry.Parse(
        "M21.9 10.6L13.8 2.5C13.4 2.1 12.7 2.1 12.3 2.5L9.7 5.1C9.3 5.5 9.3 6.2 9.7 6.6L15.1 12L9.7 17.4C9.3 17.8 9.3 18.5 9.7 18.9L12.3 21.5C12.7 21.9 13.4 21.9 13.8 21.5L21.9 13.4C22.3 13 22.3 12.3 21.9 11.9V10.6zM4 19H2V5H4V19z");

    /// <summary>CMD icon (Windows command prompt).</summary>
    private static readonly Geometry CmdIcon = Geometry.Parse(
        "M2 4h20v16H2V4zm2 2v12h16V6H4zm2 2h12v2H6V8zm0 4h8v2H6v-2z");

    /// <summary>Fish shell icon.</summary>
    private static readonly Geometry FishIcon = Geometry.Parse(
        "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm-2-9c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm4 0c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm-2 5c-2.33 0-4.31-1.46-5.11-3.5h10.22c-.8 2.04-2.78 3.5-5.11 3.5z");

    /// <summary>Default terminal icon.</summary>
    private static readonly Geometry DefaultShellIcon = Geometry.Parse(
        "M4 4h16v16H4V4zm2 2v12h12V6H6zm2 2h8v2H8V8zm0 4l3 2-3 2v-4z");

    // ═══════════════════════════════════════════════════════════════════════════
    // IValueConverter Implementation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts a <see cref="ShellType"/> to a <see cref="Geometry"/>.
    /// </summary>
    /// <param name="value">The ShellType value.</param>
    /// <param name="targetType">Target type (ignored).</param>
    /// <param name="parameter">Optional parameter (ignored).</param>
    /// <param name="culture">Culture info (ignored).</param>
    /// <returns>Geometry for the shell type icon.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ShellType shellType)
        {
            return shellType switch
            {
                ShellType.Bash => BashIcon,
                ShellType.Zsh => ZshIcon,
                ShellType.PowerShell => PowerShellIcon,
                ShellType.Cmd => CmdIcon,
                ShellType.Fish => FishIcon,
                _ => DefaultShellIcon
            };
        }

        return DefaultShellIcon;
    }

    /// <summary>
    /// Not supported - one-way conversion only.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ShellTypeToIconConverter is one-way only");
    }
}
