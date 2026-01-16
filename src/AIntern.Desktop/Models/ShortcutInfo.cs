namespace AIntern.Desktop.Models;

using Avalonia.Input;

/// <summary>
/// Information about a registered keyboard shortcut (v0.3.5g).
/// </summary>
public sealed class ShortcutInfo
{
    /// <summary>
    /// The key for this shortcut.
    /// </summary>
    public Key Key { get; init; }

    /// <summary>
    /// Key modifiers (Ctrl, Shift, Alt).
    /// </summary>
    public KeyModifiers Modifiers { get; init; }

    /// <summary>
    /// Unique command identifier (e.g., "file.save").
    /// </summary>
    public string CommandId { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Category for grouping (e.g., "Editor", "File").
    /// </summary>
    public string Category { get; init; } = "General";

    /// <summary>
    /// Formatted gesture for display (e.g., "Ctrl+S").
    /// </summary>
    public string DisplayGesture => FormatGesture();

    /// <summary>
    /// Formats the key and modifiers as a display string.
    /// </summary>
    private string FormatGesture()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(KeyModifiers.Control))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(KeyModifiers.Shift))
            parts.Add("Shift");
        if (Modifiers.HasFlag(KeyModifiers.Alt))
            parts.Add("Alt");

        // Format special keys more nicely
        var keyName = Key switch
        {
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            _ => Key.ToString()
        };

        parts.Add(keyName);

        return string.Join("+", parts);
    }
}
