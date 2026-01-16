namespace AIntern.Core.Events;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SETTING CHANGED EVENT ARGS (v0.4.5a)                                    │
// │ Event arguments for when a setting value changes.                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event arguments for when a setting value changes.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5a.</para>
/// </remarks>
public sealed class SettingChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates new setting changed event args.
    /// </summary>
    /// <param name="propertyName">Name of the changed property.</param>
    /// <param name="oldValue">Previous value (may be null).</param>
    /// <param name="newValue">New value (may be null).</param>
    public SettingChangedEventArgs(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        OldValue = oldValue;
        NewValue = newValue;
        ChangedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Name of the property that changed.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Previous value of the setting.
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// New value of the setting.
    /// </summary>
    public object? NewValue { get; }

    /// <summary>
    /// Timestamp when the change occurred.
    /// </summary>
    public DateTime ChangedAt { get; }

    /// <summary>
    /// Checks if the changed property matches the given name.
    /// </summary>
    public bool IsProperty(string name) =>
        string.Equals(PropertyName, name, StringComparison.Ordinal);

    /// <summary>
    /// Gets the new value as the specified type.
    /// </summary>
    public T? GetNewValue<T>() => NewValue is T value ? value : default;

    /// <summary>
    /// Gets the old value as the specified type.
    /// </summary>
    public T? GetOldValue<T>() => OldValue is T value ? value : default;
}
