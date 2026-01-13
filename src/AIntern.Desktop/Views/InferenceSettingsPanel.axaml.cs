// ============================================================================
// InferenceSettingsPanel.axaml.cs
// AIntern.Desktop - Inference Settings Panel (v0.2.3e)
// ============================================================================
// Code-behind for the inference settings panel UserControl. Contains only
// constructor initialization as all logic is handled by the ViewModel.
// ============================================================================

using Avalonia.Controls;

namespace AIntern.Desktop.Views;

/// <summary>
/// User control for inference settings with preset management and parameter sliders.
/// </summary>
/// <remarks>
/// <para>
/// This panel displays in the sidebar below the model selector and provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Preset dropdown:</b> Built-in and custom presets with category badges</description></item>
///   <item><description><b>Parameter sliders:</b> Temperature, TopP, MaxTokens, ContextSize</description></item>
///   <item><description><b>Advanced section:</b> Collapsible section with RepetitionPenalty and TopK</description></item>
///   <item><description><b>Save as preset:</b> Dialog for saving current settings as a custom preset</description></item>
///   <item><description><b>Reset button:</b> Restore default preset settings</description></item>
/// </list>
/// <para>
/// <b>Visual Layout:</b>
/// </para>
/// <code>
/// ┌────────────────────────────────────────────────────────────┐
/// │ [v] Inference Settings                         [R] [S]    │ Header
/// ├────────────────────────────────────────────────────────────┤
/// │ [ComboBox: Balanced v]                      [Modified]    │ Preset
/// ├────────────────────────────────────────────────────────────┤
/// │ Temperature                                    [ 0.7 ]    │
/// │ ═══════════════════●════════════════════════════════════  │
/// │ Balanced creativity and consistency                       │
/// │                                                           │
/// │ (More sliders...)                                         │
/// │                                                           │
/// │ ▶ Advanced                                                │ Expander
/// │   └─ RepetitionPenalty, TopK sliders                      │
/// └────────────────────────────────────────────────────────────┘
/// </code>
/// </remarks>
/// <seealso cref="ViewModels.InferenceSettingsViewModel"/>
/// <seealso cref="Controls.ParameterSlider"/>
public partial class InferenceSettingsPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceSettingsPanel"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All business logic is handled by <see cref="ViewModels.InferenceSettingsViewModel"/>
    /// which is bound via <c>DataContext</c>. The code-behind only initializes the
    /// component from XAML.
    /// </para>
    /// </remarks>
    public InferenceSettingsPanel()
    {
        InitializeComponent();
    }
}
