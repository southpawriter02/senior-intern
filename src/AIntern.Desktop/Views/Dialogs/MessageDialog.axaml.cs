namespace AIntern.Desktop.Views.Dialogs;

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

/// <summary>
/// Icon types for message dialogs.
/// </summary>
public enum MessageDialogIcon
{
    /// <summary>No icon.</summary>
    None,

    /// <summary>Information icon (i).</summary>
    Information,

    /// <summary>Warning icon (⚠).</summary>
    Warning,

    /// <summary>Error icon (✗).</summary>
    Error,

    /// <summary>Question icon (?).</summary>
    Question
}

/// <summary>
/// A configurable message dialog with icons and buttons.
/// </summary>
/// <remarks>
/// <para>
/// Features:
/// </para>
/// <list type="bullet">
///   <item><description>Icon support (Info, Warning, Error, Question)</description></item>
///   <item><description>Configurable button labels</description></item>
///   <item><description>Returns clicked button text as result</description></item>
/// </list>
/// <para>Added in v0.3.3g.</para>
/// </remarks>
public partial class MessageDialog : Window
{
    #region Fields

    private MessageDialogIcon _icon = MessageDialogIcon.None;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="MessageDialog"/>.
    /// </summary>
    public MessageDialog()
    {
        InitializeComponent();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the message text.
    /// </summary>
    public string Message
    {
        get => MessageText.Text ?? string.Empty;
        set => MessageText.Text = value;
    }

    /// <summary>
    /// Gets or sets the dialog icon type.
    /// </summary>
    public new MessageDialogIcon Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            UpdateIcon();
        }
    }

    /// <summary>
    /// Sets the button labels for the dialog.
    /// </summary>
    /// <remarks>
    /// The first button is styled as primary and appears rightmost.
    /// </remarks>
    public IEnumerable<string> Buttons
    {
        set => CreateButtons(value);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates the icon display based on the Icon property.
    /// </summary>
    private void UpdateIcon()
    {
        var iconKey = _icon switch
        {
            MessageDialogIcon.Information => "InfoIcon",
            MessageDialogIcon.Warning => "WarningIcon",
            MessageDialogIcon.Error => "ErrorIcon",
            MessageDialogIcon.Question => "QuestionIcon",
            _ => null
        };

        if (iconKey != null && Application.Current?.TryGetResource(iconKey, null, out var resource) == true
            && resource is Geometry geometry)
        {
            DialogIcon.Data = geometry;
            DialogIcon.IsVisible = true;

            // Set icon color based on type
            var foregroundKey = _icon switch
            {
                MessageDialogIcon.Error => "DangerBrush",
                MessageDialogIcon.Warning => "WarningBrush",
                MessageDialogIcon.Information => "SuccessBrush",
                _ => "TextSecondary"
            };

            if (Application.Current?.TryGetResource(foregroundKey, null, out var brush) == true
                && brush is IBrush iconBrush)
            {
                DialogIcon.Foreground = iconBrush;
            }
        }
        else
        {
            DialogIcon.IsVisible = false;
        }
    }

    /// <summary>
    /// Creates button controls from labels.
    /// </summary>
    private void CreateButtons(IEnumerable<string> buttonLabels)
    {
        var labelsArray = buttonLabels.ToArray();
        var buttons = labelsArray.Select((label, index) =>
        {
            var button = new Button
            {
                Content = label,
                MinWidth = 80
            };

            // First button is primary
            if (index == 0)
            {
                button.Classes.Add("primary");
            }

            button.Click += (s, e) => Close(label);
            return button;
        });

        // Reverse so primary is on the right
        ButtonsPanel.ItemsSource = buttons.Reverse();
    }

    #endregion
}
