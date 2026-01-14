namespace AIntern.Core.Models;

/// <summary>
/// Represents persistent application settings that are saved between sessions.
/// Includes model configuration, inference parameters, and UI preferences.
/// </summary>
public sealed class AppSettings
{
    #region Model Settings

    /// <summary>
    /// Gets or sets the file path of the last loaded model.
    /// Used to restore the previous model on application startup.
    /// </summary>
    public string? LastModelPath { get; set; }

    /// <summary>
    /// Gets or sets the default context window size in tokens.
    /// Larger values allow for longer conversations but require more memory.
    /// </summary>
    public uint DefaultContextSize { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the number of model layers to offload to GPU.
    /// Set to -1 for automatic detection based on available VRAM.
    /// </summary>
    public int DefaultGpuLayers { get; set; } = -1;

    /// <summary>
    /// Gets or sets the batch size for token processing.
    /// Larger values may improve throughput at the cost of latency.
    /// </summary>
    public uint DefaultBatchSize { get; set; } = 512;

    #endregion

    #region Inference Settings

    /// <summary>
    /// Gets or sets the temperature for response generation.
    /// Higher values (0.8-1.0) produce more creative responses;
    /// lower values (0.1-0.5) produce more focused, deterministic responses.
    /// </summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets the top-p (nucleus) sampling threshold.
    /// Limits token selection to the smallest set whose cumulative probability exceeds this value.
    /// </summary>
    public float TopP { get; set; } = 0.9f;

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate per response.
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the ID of the last active inference preset.
    /// </summary>
    /// <value>
    /// The GUID of the active preset, or <c>null</c> if using default settings.
    /// </value>
    /// <remarks>
    /// <para>
    /// Persisted to settings.json to restore the user's last-used preset on startup.
    /// Set by <see cref="Interfaces.IInferenceSettingsService.ApplyPresetAsync"/> when
    /// a preset is applied.
    /// </para>
    /// <para>
    /// If this is null or the referenced preset no longer exists,
    /// <see cref="Interfaces.IInferenceSettingsService.InitializeAsync"/> falls back
    /// to the default preset (Balanced).
    /// </para>
    /// </remarks>
    public Guid? ActivePresetId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the currently selected system prompt.
    /// </summary>
    /// <value>
    /// The GUID of the selected system prompt, or <c>null</c> if using the default.
    /// </value>
    /// <remarks>
    /// <para>
    /// Persisted to settings.json to restore the user's last-selected prompt on startup.
    /// Set by <see cref="Interfaces.ISystemPromptService.SetCurrentPromptAsync"/> when
    /// a prompt is selected.
    /// </para>
    /// <para>
    /// If this is null or the referenced prompt no longer exists,
    /// <see cref="Interfaces.ISystemPromptService.InitializeAsync"/> falls back
    /// to the default system prompt.
    /// </para>
    /// <para>Added in v0.2.4b.</para>
    /// </remarks>
    public Guid? CurrentSystemPromptId { get; set; }

    #endregion

    #region UI Settings

    /// <summary>
    /// Gets or sets the application color theme ("Dark" or "Light").
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// Gets or sets the width of the conversation sidebar in pixels.
    /// </summary>
    public double SidebarWidth { get; set; } = 280;

    /// <summary>
    /// Gets or sets whether to restore the last workspace on startup.
    /// </summary>
    /// <remarks>Added in v0.3.1e.</remarks>
    public bool RestoreLastWorkspace { get; set; } = true;

    #endregion

    #region Window State

    /// <summary>
    /// Gets or sets the main window width in pixels.
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// Gets or sets the main window height in pixels.
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// Gets or sets the main window X position, or null if not previously set.
    /// </summary>
    public double? WindowX { get; set; }

    /// <summary>
    /// Gets or sets the main window Y position, or null if not previously set.
    /// </summary>
    public double? WindowY { get; set; }

    #endregion
}
