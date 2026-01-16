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

    /// <summary>
    /// Gets or sets whether to show hidden files in the file explorer.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public bool ShowHiddenFiles { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to respect .gitignore patterns when displaying files.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public bool UseGitIgnore { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of recent workspaces to display.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public int MaxRecentWorkspaces { get; set; } = 10;

    /// <summary>
    /// Gets or sets custom ignore patterns (in addition to .gitignore).
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public IReadOnlyList<string> CustomIgnorePatterns { get; set; } = [];

    #endregion

    #region Editor Settings (v0.3.1f)

    /// <summary>
    /// Gets or sets whether to enable word wrap in the code editor.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public bool WordWrap { get; set; } = false;

    /// <summary>
    /// Gets or sets the tab size in spaces.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public int TabSize { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether to show line numbers in the code editor.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to highlight the current line.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public bool HighlightCurrentLine { get; set; } = true;

    /// <summary>
    /// Gets or sets the font family for the code editor.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public string EditorFontFamily { get; set; } = "Cascadia Code, Consolas, monospace";

    /// <summary>
    /// Gets or sets the font size for the code editor.
    /// </summary>
    /// <remarks>Added in v0.3.1f.</remarks>
    public int EditorFontSize { get; set; } = 14;

    /// <summary>
    /// Gets or sets whether to convert tabs to spaces when typing.
    /// </summary>
    /// <remarks>Added in v0.3.3d.</remarks>
    public bool ConvertTabsToSpaces { get; set; } = true;

    /// <summary>
    /// Gets or sets the column position for the column ruler (0 = disabled).
    /// </summary>
    /// <remarks>Added in v0.3.3d.</remarks>
    public int RulerColumn { get; set; } = 0;

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

    #region Context Attachment Settings (v0.3.4a)

    /// <summary>
    /// Gets or sets the context attachment limits configuration.
    /// </summary>
    /// <remarks>Added in v0.3.4a.</remarks>
    public ContextLimitsConfig ContextLimits { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to show token count in the context bar.
    /// </summary>
    /// <remarks>Added in v0.3.5a.</remarks>
    public bool ShowTokenCount { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to warn when approaching token limit.
    /// </summary>
    /// <remarks>Added in v0.3.5a.</remarks>
    public bool WarnOnTokenLimit { get; set; } = true;

    #endregion

    #region Additional Settings (v0.3.5a)

    /// <summary>
    /// Gets or sets whether to auto-refresh when external file changes are detected.
    /// </summary>
    /// <remarks>Added in v0.3.5a.</remarks>
    public bool AutoRefreshOnExternalChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets the editor syntax highlighting theme.
    /// </summary>
    /// <remarks>Added in v0.3.5a.</remarks>
    public string EditorTheme { get; set; } = "DarkPlus";

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // Code Generation Settings (v0.4.5a)
    // ═══════════════════════════════════════════════════════════════

    #region Code Generation Settings (v0.4.5a)

    /// <summary>
    /// Automatically detect and parse code blocks from LLM responses.
    /// When enabled, code fences (```) are parsed into CodeBlock models.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool AutoDetectCodeBlocks { get; set; } = true;

    /// <summary>
    /// Show diff preview while LLM is still streaming response.
    /// Enables real-time diff computation during generation.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool ShowDiffPreviewDuringStreaming { get; set; } = true;

    /// <summary>
    /// Auto-expand code blocks in chat messages.
    /// When false, code blocks show collapsed by default.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool AutoExpandCodeBlocks { get; set; } = true;

    /// <summary>
    /// Show quick action buttons (Copy, Apply, Preview) on code blocks.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool ShowCodeBlockQuickActions { get; set; } = true;

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // Diff Viewer Settings (v0.4.5a)
    // ═══════════════════════════════════════════════════════════════

    #region Diff Viewer Settings (v0.4.5a)

    /// <summary>
    /// Default view mode for diff viewer.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public DiffViewMode DefaultDiffViewMode { get; set; } = DiffViewMode.SideBySide;

    /// <summary>
    /// Show line numbers in diff viewer.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool ShowLineNumbersInDiff { get; set; } = true;

    /// <summary>
    /// Highlight whitespace changes (spaces, tabs, line endings) in diff viewer.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool HighlightWhitespaceChanges { get; set; } = false;

    /// <summary>
    /// Number of context lines to show around changes in diff viewer.
    /// Valid range: 0-10.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public int DiffContextLines { get; set; } = 3;

    /// <summary>
    /// Enable syntax highlighting in diff viewer.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool SyntaxHighlightingInDiff { get; set; } = true;

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // Backup & Undo Settings (v0.4.5a)
    // ═══════════════════════════════════════════════════════════════

    #region Backup & Undo Settings (v0.4.5a)

    /// <summary>
    /// Create backup file before applying changes.
    /// Enables undo functionality for applied changes.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool CreateBackupBeforeApply { get; set; } = true;

    /// <summary>
    /// Time window in minutes during which undo is available.
    /// Valid range: 5-1440 (5 minutes to 24 hours).
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public int UndoWindowMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum age in days for backup files before automatic cleanup.
    /// Valid range: 1-90 days.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public int MaxBackupAgeDays { get; set; } = 7;

    /// <summary>
    /// Custom directory for storing backup files.
    /// Null uses default: ".aintern/backups" in workspace root.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public string? BackupDirectory { get; set; }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // Apply Behavior Settings (v0.4.5a)
    // ═══════════════════════════════════════════════════════════════

    #region Apply Behavior Settings (v0.4.5a)

    /// <summary>
    /// Automatically refresh/reload file in editor after applying changes.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool AutoRefreshEditorAfterApply { get; set; } = true;

    /// <summary>
    /// Show confirmation dialog before applying changes.
    /// When false, changes apply immediately on click.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool ConfirmBeforeApply { get; set; } = true;

    /// <summary>
    /// Show modal progress overlay during batch apply operations.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool ShowApplyProgressOverlay { get; set; } = true;

    /// <summary>
    /// Automatically close progress overlay when operation succeeds.
    /// When false, overlay stays open until user dismisses it.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool AutoCloseProgressOnSuccess { get; set; } = true;

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // History Settings (v0.4.5a)
    // ═══════════════════════════════════════════════════════════════

    #region History Settings (v0.4.5a)

    /// <summary>
    /// Maximum number of recent changes to track in history panel.
    /// Valid range: 10-500.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public int MaxChangeHistoryItems { get; set; } = 50;

    /// <summary>
    /// Enable change history tracking.
    /// When disabled, no history is recorded.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool TrackChangeHistory { get; set; } = true;

    /// <summary>
    /// Persist change history across application sessions.
    /// When false, history is cleared on restart.
    /// </summary>
    /// <remarks>Added in v0.4.5a.</remarks>
    public bool PersistHistoryAcrossSessions { get; set; } = true;

    #endregion
}
