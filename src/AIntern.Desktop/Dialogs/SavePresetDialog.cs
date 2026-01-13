// ============================================================================
// SavePresetDialog.cs
// AIntern.Desktop - Inference Settings Panel (v0.2.3e)
// ============================================================================
// Static dialog helper for saving custom inference presets. Prompts the user
// to enter a name (required) and optional description for the new preset.
// Follows the same pattern as DeleteConfirmationDialog for consistency.
// ============================================================================

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Dialogs;

/// <summary>
/// Dialog for saving current inference settings as a custom preset.
/// </summary>
/// <remarks>
/// <para>
/// This static helper class creates and shows a modal dialog when the user clicks
/// the "Save" button in the inference settings panel. The dialog collects:
/// <list type="bullet">
///   <item><description><b>Name</b> (required): A unique name for the preset</description></item>
///   <item><description><b>Description</b> (optional): A brief description of the preset's purpose</description></item>
/// </list>
/// </para>
/// <para>
/// The dialog follows the same programmatic UI construction pattern as
/// <see cref="DeleteConfirmationDialog"/> for consistency across the application.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var (success, name, description) = await SavePresetDialog.ShowAsync(ownerWindow, logger);
/// if (success)
/// {
///     await SavePresetAsync(name, description);
/// }
/// </code>
/// </example>
/// <seealso cref="DeleteConfirmationDialog"/>
/// <seealso cref="InferenceSettingsViewModel"/>
public static class SavePresetDialog
{
    #region Constants

    /// <summary>
    /// Maximum length for preset name input.
    /// Names longer than this are automatically truncated.
    /// </summary>
    private const int NameMaxLength = 50;

    /// <summary>
    /// Maximum length for preset description input.
    /// Descriptions longer than this are automatically truncated.
    /// </summary>
    private const int DescriptionMaxLength = 200;

    /// <summary>
    /// Dialog window width in pixels.
    /// </summary>
    private const int DialogWidth = 400;

    /// <summary>
    /// Dialog window height in pixels.
    /// </summary>
    private const int DialogHeight = 280;

    #endregion

    #region Public Methods

    /// <summary>
    /// Shows the save preset dialog and returns the user's input.
    /// </summary>
    /// <param name="owner">The owner window for modal positioning.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><c>success</c>: <c>true</c> if the user confirmed; <c>false</c> if cancelled</item>
    ///   <item><c>name</c>: The preset name (empty string if cancelled)</item>
    ///   <item><c>description</c>: The optional preset description (null if not provided)</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// The dialog is displayed as a modal window centered over the owner.
    /// The Name TextBox receives initial focus for immediate input.
    /// </para>
    /// <para>
    /// <b>Validation:</b> The Save button is disabled until a valid name is entered.
    /// Names cannot be empty or contain only whitespace.
    /// </para>
    /// </remarks>
    public static async Task<(bool success, string name, string? description)> ShowAsync(
        Window owner,
        ILogger? logger = null)
    {
        var stopwatch = Stopwatch.StartNew();
        logger?.LogDebug("[ENTER] SavePresetDialog.ShowAsync");

        // Create result tracking variables.
        var success = false;
        var resultName = string.Empty;
        string? resultDescription = null;

        // Create the dialog window.
        var dialog = new Window
        {
            Title = "Save Preset",
            Width = DialogWidth,
            Height = DialogHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.BorderOnly,
            Background = Application.Current?.FindResource("WindowBackground") as IBrush
                ?? new SolidColorBrush(Color.Parse("#1E1E1E"))
        };

        logger?.LogDebug("[INFO] SavePresetDialog - Creating dialog UI");

        // Create the content layout.
        var mainPanel = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16
        };

        // Dialog title.
        var titleText = new TextBlock
        {
            Text = "Save as Preset",
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = Application.Current?.FindResource("TextPrimary") as IBrush
                ?? Brushes.White
        };

        // Name label and input.
        var nameLabel = new TextBlock
        {
            Text = "Name *",
            FontSize = 12,
            Foreground = Application.Current?.FindResource("TextPrimary") as IBrush
                ?? Brushes.White,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var nameTextBox = new TextBox
        {
            MaxLength = NameMaxLength,
            Watermark = "Enter preset name...",
            Background = Application.Current?.FindResource("InputBackground") as IBrush
                ?? new SolidColorBrush(Color.Parse("#3C3C3C")),
            Foreground = Application.Current?.FindResource("TextPrimary") as IBrush
                ?? Brushes.White,
            BorderBrush = Application.Current?.FindResource("BorderBrush") as IBrush
                ?? new SolidColorBrush(Color.Parse("#3C3C3C")),
            Padding = new Thickness(8, 6)
        };

        // Error message (initially hidden).
        var errorBorder = new Border
        {
            Background = Application.Current?.FindResource("ErrorBackground") as IBrush
                ?? new SolidColorBrush(Color.Parse("#5A1D1D")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4),
            IsVisible = false,
            Margin = new Thickness(0, -8, 0, 0)
        };

        var errorText = new TextBlock
        {
            Text = "Name is required",
            FontSize = 11,
            Foreground = Application.Current?.FindResource("ErrorForeground") as IBrush
                ?? new SolidColorBrush(Color.Parse("#F48771"))
        };
        errorBorder.Child = errorText;

        // Description label and input.
        var descriptionLabel = new TextBlock
        {
            Text = "Description (optional)",
            FontSize = 12,
            Foreground = Application.Current?.FindResource("TextPrimary") as IBrush
                ?? Brushes.White,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var descriptionTextBox = new TextBox
        {
            MaxLength = DescriptionMaxLength,
            Watermark = "Brief description of this preset...",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 60,
            Background = Application.Current?.FindResource("InputBackground") as IBrush
                ?? new SolidColorBrush(Color.Parse("#3C3C3C")),
            Foreground = Application.Current?.FindResource("TextPrimary") as IBrush
                ?? Brushes.White,
            BorderBrush = Application.Current?.FindResource("BorderBrush") as IBrush
                ?? new SolidColorBrush(Color.Parse("#3C3C3C")),
            Padding = new Thickness(8, 6)
        };

        // Button panel.
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // Cancel button.
        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(16, 8)
        };
        cancelButton.Click += (_, _) =>
        {
            logger?.LogDebug("[INFO] SavePresetDialog - User clicked Cancel");
            success = false;
            dialog.Close();
        };

        // Save button (initially disabled until valid name entered).
        var saveButton = new Button
        {
            Content = "Save",
            Padding = new Thickness(16, 8),
            IsEnabled = false
        };

        // Apply accent theme from resources.
        if (Application.Current?.FindResource("AccentButton") is ControlTheme accentTheme)
        {
            saveButton.Theme = accentTheme;
        }
        else
        {
            // Fallback styling if theme not found.
            saveButton.Background = new SolidColorBrush(Color.Parse("#007ACC"));
            saveButton.Foreground = Brushes.White;
        }

        saveButton.Click += (_, _) =>
        {
            var name = nameTextBox.Text?.Trim() ?? string.Empty;

            // Validate name is not empty.
            if (string.IsNullOrWhiteSpace(name))
            {
                logger?.LogDebug("[INFO] SavePresetDialog - Validation failed: empty name");
                errorBorder.IsVisible = true;
                nameTextBox.Focus();
                return;
            }

            logger?.LogDebug(
                "[INFO] SavePresetDialog - User clicked Save - Name: {Name}, HasDescription: {HasDesc}",
                name, !string.IsNullOrWhiteSpace(descriptionTextBox.Text));

            success = true;
            resultName = name;
            resultDescription = string.IsNullOrWhiteSpace(descriptionTextBox.Text)
                ? null
                : descriptionTextBox.Text.Trim();
            dialog.Close();
        };

        // Enable/disable save button based on name input.
        nameTextBox.TextChanged += (_, _) =>
        {
            var hasName = !string.IsNullOrWhiteSpace(nameTextBox.Text);
            saveButton.IsEnabled = hasName;

            // Hide error when user starts typing.
            if (hasName && errorBorder.IsVisible)
            {
                errorBorder.IsVisible = false;
            }
        };

        // Add buttons to panel (Cancel, Save).
        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(saveButton);

        // Assemble the layout.
        mainPanel.Children.Add(titleText);
        mainPanel.Children.Add(nameLabel);
        mainPanel.Children.Add(nameTextBox);
        mainPanel.Children.Add(errorBorder);
        mainPanel.Children.Add(descriptionLabel);
        mainPanel.Children.Add(descriptionTextBox);
        mainPanel.Children.Add(buttonPanel);

        dialog.Content = mainPanel;

        // Focus name TextBox when dialog opens.
        dialog.Opened += (_, _) =>
        {
            logger?.LogDebug("[INFO] SavePresetDialog - Dialog opened, focusing Name TextBox");
            nameTextBox.Focus();
        };

        // Show the dialog.
        logger?.LogInformation("[INFO] SavePresetDialog - Showing dialog");

        await dialog.ShowDialog(owner);

        stopwatch.Stop();
        logger?.LogDebug(
            "[EXIT] SavePresetDialog.ShowAsync - Success: {Success}, Name: {Name}, Duration: {ElapsedMs}ms",
            success, resultName, stopwatch.ElapsedMilliseconds);

        return (success, resultName, resultDescription);
    }

    #endregion
}
