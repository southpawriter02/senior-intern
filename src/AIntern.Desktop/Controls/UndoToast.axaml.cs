using Avalonia.Controls;

namespace AIntern.Desktop.Controls;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ UNDO TOAST (v0.4.3h)                                                     │
// │ Toast notification with undo option and countdown timer.                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Toast notification that appears after applying changes.
/// Shows message, countdown timer, and undo option.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3h.</para>
/// </remarks>
public partial class UndoToast : UserControl
{
    /// <summary>
    /// Initializes a new instance of the UndoToast control.
    /// </summary>
    public UndoToast()
    {
        InitializeComponent();
    }
}
