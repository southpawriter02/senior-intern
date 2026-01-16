using AIntern.Core.Models;

namespace AIntern.Core.Configuration;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SETTINGS DEFAULTS (v0.4.5a)                                             │
// │ Centralized default values for AppSettings.                             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Provides default values for all AppSettings properties.
/// Centralized location for defaults to ensure consistency.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5a.</para>
/// </remarks>
public static class SettingsDefaults
{
    // ═══════════════════════════════════════════════════════════════
    // Code Generation Defaults
    // ═══════════════════════════════════════════════════════════════

    public const bool AutoDetectCodeBlocks = true;
    public const bool ShowDiffPreviewDuringStreaming = true;
    public const bool AutoExpandCodeBlocks = true;
    public const bool ShowCodeBlockQuickActions = true;

    // ═══════════════════════════════════════════════════════════════
    // Diff Viewer Defaults
    // ═══════════════════════════════════════════════════════════════

    public const DiffViewMode DefaultDiffViewMode = DiffViewMode.SideBySide;
    public const bool ShowLineNumbersInDiff = true;
    public const bool HighlightWhitespaceChanges = false;
    public const int DiffContextLines = 3;
    public const bool SyntaxHighlightingInDiff = true;

    // ═══════════════════════════════════════════════════════════════
    // Backup & Undo Defaults
    // ═══════════════════════════════════════════════════════════════

    public const bool CreateBackupBeforeApply = true;
    public const int UndoWindowMinutes = 30;
    public const int DefaultBackupAgeDays = 7;
    public const string DefaultBackupSubdirectory = ".aintern/backups";

    // ═══════════════════════════════════════════════════════════════
    // Apply Behavior Defaults
    // ═══════════════════════════════════════════════════════════════

    public const bool AutoRefreshEditorAfterApply = true;
    public const bool ConfirmBeforeApply = true;
    public const bool ShowApplyProgressOverlay = true;
    public const bool AutoCloseProgressOnSuccess = true;

    // ═══════════════════════════════════════════════════════════════
    // History Defaults
    // ═══════════════════════════════════════════════════════════════

    public const int DefaultChangeHistoryItems = 50;
    public const bool TrackChangeHistory = true;
    public const bool PersistHistoryAcrossSessions = true;

    // ═══════════════════════════════════════════════════════════════
    // Constraint Ranges
    // ═══════════════════════════════════════════════════════════════

    public const int MinDiffContextLines = 0;
    public const int MaxDiffContextLines = 10;

    public const int MinUndoWindowMinutes = 5;
    public const int MaxUndoWindowMinutes = 1440; // 24 hours

    public const int MinBackupAgeDays = 1;
    public const int MaxBackupAgeDays = 90;

    public const int MinChangeHistoryItems = 10;
    public const int MaxChangeHistoryItems = 500;
}
