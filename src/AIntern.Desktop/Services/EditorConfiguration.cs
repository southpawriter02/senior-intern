using System.ComponentModel;
using Avalonia.Media;
using AvaloniaEdit;
using AIntern.Core.Models;

namespace AIntern.Desktop.Services;

/// <summary>
/// Configures TextEditor instances based on application settings.
/// </summary>
public static class EditorConfiguration
{
    #region Constants

    /// <summary>Default editor font family.</summary>
    public const string DefaultFontFamily = "Cascadia Code, Consolas, Monaco, monospace";

    /// <summary>Default editor font size.</summary>
    public const int DefaultFontSize = 14;

    /// <summary>Default tab size.</summary>
    public const int DefaultTabSize = 4;

    #endregion

    #region Apply Methods

    /// <summary>
    /// Applies all settings to an editor instance.
    /// </summary>
    /// <param name="editor">The TextEditor to configure.</param>
    /// <param name="settings">Application settings.</param>
    public static void ApplySettings(TextEditor editor, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(settings);

        ApplyFontSettings(editor, settings);
        ApplyDisplaySettings(editor, settings);
        ApplyEditingSettings(editor, settings);
        ApplyBehaviorSettings(editor, settings);
    }

    /// <summary>
    /// Applies font-related settings.
    /// </summary>
    public static void ApplyFontSettings(TextEditor editor, AppSettings settings)
    {
        editor.FontFamily = new FontFamily(
            string.IsNullOrEmpty(settings.EditorFontFamily)
                ? DefaultFontFamily
                : settings.EditorFontFamily);

        editor.FontSize = settings.EditorFontSize > 0
            ? settings.EditorFontSize
            : DefaultFontSize;
    }

    /// <summary>
    /// Applies display settings (line numbers, highlighting, etc.).
    /// </summary>
    public static void ApplyDisplaySettings(TextEditor editor, AppSettings settings)
    {
        editor.ShowLineNumbers = settings.ShowLineNumbers;
        editor.WordWrap = settings.WordWrap;
        editor.Options.HighlightCurrentLine = settings.HighlightCurrentLine;

        // Hardcoded display options
        editor.Options.ShowEndOfLine = false;
        editor.Options.ShowSpaces = false;
        editor.Options.ShowTabs = false;
        editor.Options.EnableHyperlinks = true;
        editor.Options.RequireControlModifierForHyperlinkClick = true;
    }

    /// <summary>
    /// Applies editing settings (tabs, indentation).
    /// </summary>
    public static void ApplyEditingSettings(TextEditor editor, AppSettings settings)
    {
        editor.Options.ConvertTabsToSpaces = settings.ConvertTabsToSpaces;
        editor.Options.IndentationSize = settings.TabSize > 0
            ? settings.TabSize
            : DefaultTabSize;

        // Smart editing options
        editor.Options.EnableTextDragDrop = true;
        editor.Options.CutCopyWholeLine = true;
    }

    /// <summary>
    /// Applies behavior settings.
    /// </summary>
    public static void ApplyBehaviorSettings(TextEditor editor, AppSettings settings)
    {
        // Selection and scrolling
        editor.Options.EnableVirtualSpace = false;
        editor.Options.EnableRectangularSelection = true;
        editor.Options.AllowScrollBelowDocument = true;

        // Note: Column ruler (ShowColumnRuler, ColumnRulerPosition)
        // is not available in AvaloniaEdit's TextEditorOptions API
    }

    #endregion

    #region Defaults

    /// <summary>
    /// Applies sensible defaults to an editor (without AppSettings).
    /// </summary>
    public static void ApplyDefaults(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        // Font
        editor.FontFamily = new FontFamily(DefaultFontFamily);
        editor.FontSize = DefaultFontSize;

        // Display
        editor.ShowLineNumbers = true;
        editor.WordWrap = false;
        editor.Options.HighlightCurrentLine = true;

        // Editing
        editor.Options.ConvertTabsToSpaces = true;
        editor.Options.IndentationSize = DefaultTabSize;

        // Behavior
        editor.Options.EnableTextDragDrop = true;
        editor.Options.CutCopyWholeLine = true;
        editor.Options.EnableRectangularSelection = true;
        editor.Options.AllowScrollBelowDocument = true;

        // Visual
        editor.Options.ShowEndOfLine = false;
        editor.Options.ShowSpaces = false;
        editor.Options.ShowTabs = false;
        editor.Options.EnableHyperlinks = true;
    }

    #endregion

    #region Live Binding

    /// <summary>
    /// Creates bindings between editor and settings for live updates.
    /// </summary>
    /// <param name="editor">The TextEditor to bind.</param>
    /// <param name="settings">Application settings.</param>
    /// <returns>Disposable subscription (dispose to unbind).</returns>
    public static IDisposable BindToSettings(TextEditor editor, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(settings);

        return new SettingsBindingSubscription(editor, settings);
    }

    private sealed class SettingsBindingSubscription : IDisposable
    {
        private readonly TextEditor _editor;
        private readonly AppSettings _settings;
        private readonly INotifyPropertyChanged? _notifiable;
        private bool _disposed;

        public SettingsBindingSubscription(TextEditor editor, AppSettings settings)
        {
            _editor = editor;
            _settings = settings;

            // Subscribe to settings property changes if supported
            // Cast to object first to bypass sealed class pattern matching restrictions
            if ((object)_settings is INotifyPropertyChanged notifiable)
            {
                _notifiable = notifiable;
                _notifiable.PropertyChanged += OnSettingsPropertyChanged;
            }
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_disposed) return;

            switch (e.PropertyName)
            {
                case nameof(AppSettings.EditorFontFamily):
                case nameof(AppSettings.EditorFontSize):
                    ApplyFontSettings(_editor, _settings);
                    break;

                case nameof(AppSettings.ShowLineNumbers):
                case nameof(AppSettings.WordWrap):
                case nameof(AppSettings.HighlightCurrentLine):
                    ApplyDisplaySettings(_editor, _settings);
                    break;

                case nameof(AppSettings.TabSize):
                case nameof(AppSettings.ConvertTabsToSpaces):
                    ApplyEditingSettings(_editor, _settings);
                    break;

                case nameof(AppSettings.RulerColumn):
                    ApplyBehaviorSettings(_editor, _settings);
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_notifiable != null)
            {
                _notifiable.PropertyChanged -= OnSettingsPropertyChanged;
            }
        }
    }

    #endregion
}
