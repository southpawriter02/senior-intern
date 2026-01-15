namespace AIntern.Desktop.Views.Dialogs;

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

/// <summary>
/// Dialog for navigating to a specific line number.
/// </summary>
/// <remarks>
/// <para>
/// Features:
/// </para>
/// <list type="bullet">
///   <item><description>Validates line number range (1 to MaxLine)</description></item>
///   <item><description>Pre-fills current line number</description></item>
///   <item><description>Enter key submits</description></item>
///   <item><description>Auto-focus and select on open</description></item>
/// </list>
/// <para>Added in v0.3.3g.</para>
/// </remarks>
public partial class GoToLineDialog : Window
{
    #region Fields

    private int _maxLine = 1;
    private int _currentLine = 1;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="GoToLineDialog"/>.
    /// </summary>
    public GoToLineDialog()
    {
        InitializeComponent();
        LineNumberInput.KeyDown += OnInputKeyDown;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the maximum line number allowed.
    /// </summary>
    public int MaxLine
    {
        get => _maxLine;
        set
        {
            _maxLine = Math.Max(1, value);
            PromptText.Text = $"Go to line (1 - {_maxLine}):";
        }
    }

    /// <summary>
    /// Gets or sets the current line number (pre-filled in input).
    /// </summary>
    public int CurrentLine
    {
        get => _currentLine;
        set
        {
            _currentLine = value;
            LineNumberInput.Text = value.ToString();
        }
    }

    #endregion

    #region Event Handlers

    /// <inheritdoc />
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        LineNumberInput.Focus();
        LineNumberInput.SelectAll();
    }

    /// <summary>
    /// Handles Enter key to submit.
    /// </summary>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryGoToLine();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles Go button click.
    /// </summary>
    private void OnGoClick(object? sender, RoutedEventArgs e)
    {
        TryGoToLine();
    }

    /// <summary>
    /// Handles Cancel button click.
    /// </summary>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates input and closes with result if valid.
    /// </summary>
    private void TryGoToLine()
    {
        if (int.TryParse(LineNumberInput.Text, out var line))
        {
            if (line >= 1 && line <= _maxLine)
            {
                Close(line);
                return;
            }
        }

        // Invalid input - refocus and select
        LineNumberInput.Focus();
        LineNumberInput.SelectAll();
    }

    #endregion
}
