# Changelog

All notable changes to the AIntern project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed release notes, see the [docs/changelog/](docs/changelog/) directory.

## [0.5.2e] - 2026-01-18

Terminal Panel. Complete terminal panel view with tab bar, session management, and content area.

### Added

- `TerminalPanel` UserControl integrating all v0.5.2 components
  - Tab bar with scrollable session tabs (icon, name, close button)
  - Panel actions: Maximize/Restore, Hide
  - Terminal content area with TerminalControl
  - Empty state display when no sessions
- Session attachment management via `Initialize(ITerminalService)`
- Focus transfer to terminal on session change
- 4 unit tests (100% pass rate)

## [0.5.2d] - 2026-01-18

ViewModels. MVVM ViewModel layer for terminal panel session management and tab navigation.

### Added

- `TerminalSessionViewModel` for individual terminal session (tab) representation
  - Observable properties: Name, IsActive, State, WorkingDirectory
  - Shell type detection for bash, zsh, fish, powershell, cmd, nushell
- `TerminalPanelViewModel` for terminal panel management
  - Sessions collection with automatic session lifecycle handling
  - Commands: NewSession, CloseSession, ActivateSession, TogglePanel, NextTab, PreviousTab
  - Circular tab navigation, adjacent session activation on close
- 60 unit tests (100% pass rate)

## [0.5.2c] - 2026-01-18

Terminal Control. Complete terminal control with input handling, selection, clipboard, and cursor animation.

### Added

- `TerminalControl` UserControl wrapping TerminalRenderer with full input handling
- Keyboard input with VT100/xterm escape sequence generation (all function keys, arrows, navigation, Ctrl+key)
- Mouse text selection (single, double, triple click for char/word/line selection)
- Clipboard integration (Ctrl+Shift+C/V, right-click paste)
- Cursor blink animation with theme-configurable interval
- Terminal size synchronization with PTY via `TerminalSizeChanged` event
- Scrollbar overlay for buffer navigation
- 42 unit tests for key sequence mapping and word character detection

## [0.5.2b] - 2026-01-17

Terminal Renderer. SkiaSharp-based hardware-accelerated terminal rendering with full text attribute support.

### Added

- `TerminalFontMetrics` class for font loading, measurement, and coordinate conversion
- `TerminalRenderer` Avalonia control using SkiaSharp for hardware-accelerated rendering
- Text attribute support: bold, dim, underline, strikethrough, inverse, hidden
- 256-color palette and true color (RGB) rendering via `TerminalTheme`
- Cursor rendering with Block, Underline, and Bar styles
- Focus-aware cursor display (solid when focused, outline when unfocused)
- Text selection highlighting with configurable opacity
- Wide character (CJK, emoji) support with proper cell spanning
- Cross-platform font fallback chain (Cascadia Mono, Consolas, Monaco, Courier New)
- 40 unit tests for font metrics functionality


## [0.5.2a] - 2026-01-17

Theme & Styles. Terminal theme model with 256-color palette, icons, and control themes.

### Added

- `TerminalTheme` model with 256-color palette (ANSI 16 + 6×6×6 color cube + grayscale)
- `CursorStyle` enum (Block, Underline, Bar)
- `TerminalThemeColor` enum for semantic color references
- Built-in theme presets: Dark, Light, Solarized Dark
- Terminal color brushes and control themes in `Dark.axaml`
- `IconPaths.axaml` with terminal icons (Terminal, Plus, Close, Maximize, etc.)
- 47 unit tests for theme and palette functionality

## [0.5.1f] - 2026-01-17

Integration & Testing. DI registration and comprehensive test verification for terminal subsystem.

### Added

- `TerminalServiceExtensions.AddTerminalServices()` extension method for DI registration
- Terminal service integration in `ServiceCollectionExtensions.AddAInternServices()`
- 8 unit tests for DI registration, singleton behavior, and dependency resolution
- Completes v0.5.1 Terminal Foundation series

## [0.5.1e] - 2026-01-17

Shell Detection Service. Enhanced shell detection with caching, version detection, and validation.

### Added

- Enhanced `IShellDetectionService` with `GetDefaultShellAsync` and `IsShellAvailableAsync` methods
- `ShellDetectionService` with full caching, version detection, and executable validation
- `Name` and `IsDefault` properties on `ShellInfo` record
- `Nushell` shell type for modern shell support
- Platform-specific detection for Windows, macOS, and Linux
- WSL and Git Bash detection on Windows
- 31 new unit tests for shell detection

## [0.5.1d] - 2026-01-17

Terminal Service. PTY-based terminal service for managing shell sessions.

### Added

- `IShellDetectionService` interface with `ShellInfo` record and `ShellType` enum
- `ITerminalService` interface with session lifecycle, I/O, and event management
- `TerminalSessionOptions` record and `TerminalSignal` enum
- `TerminalService` implementation using Pty.Net for cross-platform PTY support
- `DefaultShellDetectionService` for detecting available shells on Windows/macOS/Linux
- Event args classes for output, session, state, title, and bell events
- 28 unit tests for terminal service functionality

## [0.5.1c] - 2026-01-17

ANSI Parser. VT100/ANSI escape sequence parser for terminal emulation.

### Added

- `AnsiParserState` enum with 12 state machine states
- `AnsiParser` class with full VT100 escape sequence support
- C0 controls, CSI sequences, SGR colors (256-color, true color)
- OSC commands (title, working directory, hyperlinks)
- 10 new `TerminalBuffer` cursor/character operation methods
- 48 unit tests for parser functionality

## [0.5.1b] - 2026-01-17

Terminal Models. Core model classes for the integrated terminal subsystem.

### Added

- `TerminalSize`, `TerminalColor`, `TerminalAttributes`, `TerminalCell` value types
- `TerminalLine`, `TerminalSelection`, `TerminalSession`, `TerminalBuffer` classes
- `TerminalSessionState` enum for session lifecycle
- 44 unit tests for terminal models

## [0.5.1a] - 2026-01-17

Project Setup & Dependencies. Terminal infrastructure foundation for v0.5.x.

### Added

- `Pty.Net` 0.1.16-pre and `System.IO.Pipelines` 8.0.0 packages for terminal support
- `src/AIntern.Core/Models/Terminal/` directory for terminal model classes
- `src/AIntern.Core/Terminal/` directory for ANSI parser logic
- `src/AIntern.Services/Terminal/` directory for terminal service implementations

## [0.4.5i] - 2026-01-17

Status Bar Integration. Service/ViewModel architecture for pending changes and temperature display.

### Added

- `StatusBarSection`, `StatusColor`, `StatusBarItemChangeType` enums
- `StatusBarItem`, `StatusBarConfiguration` records with factory methods
- `IStatusBarService` interface, `StatusBarService` implementation
- `StatusBarItemViewModel`, extended `StatusBarViewModel`
- `StatusBarStyles.axaml`, `StatusColorConverter`

## [0.4.5h] - 2026-01-16


Change History Panel. Track, view, filter, and undo workspace file changes.

### Added

- `ChangeHistorySortOrder`, `HistoryGroupMode`, `ChangeHistoryExportFormat` enums
- `ChangeHistoryFilter`, `ChangeHistoryStats`, `ChangeHistoryGroup` records
- `IChangeHistoryService` interface for history query, grouping, and export
- `ChangeHistoryService` with in-memory cache and UndoManager integration
- `ChangeHistoryViewModel`, `ChangeHistoryItemViewModel`, `ChangeHistoryGroupViewModel`
- `ChangeHistoryPanel.axaml` view with filtering, grouping, and undo controls
- `ChangeHistoryStyles.axaml` for consistent panel styling
- Export formats: JSON, CSV, Markdown, Unified Diff

## [0.4.5g] - 2026-01-16

Quick Actions. Inline action buttons on code blocks for rapid one-click operations.

### Added

- `QuickActionType` enum with 8 action types (Apply, Copy, ShowDiff, etc.)
- `QuickActionResult` record with success/failure factory methods and DisplayMessage
- `QuickAction` record with factory methods for default actions
- `IQuickActionService` interface for action management and execution
- `QuickActionService` with event-based action lifecycle hooks
- `CodeBlockQuickActionsViewModel` with command execution and status feedback
- `CodeBlockQuickActionsView.axaml` UI with button bar and status indicator
- `QuickActionStyles.axaml` with hover, pressed, success, and error states
- 33 new unit tests (14 Core + 19 Desktop)

## [0.4.5f] - 2026-01-16

Keyboard Shortcuts. Enhanced keyboard shortcut system with context-aware dispatch.

### Added

- `KeyboardShortcut` struct with parsing, equality, and platform-aware display strings
- `ShortcutContext` enum for 10 UI contexts (Global, ChatInput, CodeBlock, DiffViewer, etc.)
- `ShortcutCategory` enum for settings UI grouping
- `ShortcutHandler` class with customization support
- `ShortcutActionRegistration` for action handler binding
- `IKeyboardShortcutService` interface with context-aware dispatch, customization, and persistence
- `KeyboardShortcutService` with 30+ default shortcuts for code generation workflow
- `ShortcutManager` for UI-level keyboard event handling and context detection
- Persistence of shortcut customizations via `AppSettings.CustomShortcuts`

### Changed

- `KeyboardShortcutService` constructor now requires `ISettingsService` for persistence
- Updated existing tests to use new v0.4.5f API

## [0.4.5e] - 2026-01-16

Snippet Options UI. See [detailed notes](docs/changelog/v0.4.5e.md).

### Added

- `SnippetApplyOptionsViewModel` with 6 insert modes and two-way binding properties
- `InsertModeDescriptor` record for insert mode metadata
- `SnippetApplyOptionsPopup` view and code-behind with RadioButton selection
- `SnippetOptionsPopup.axaml` popup styles
- `IPopupHostService` interface and `PopupHostService` for popup management
- `ConfidenceToColorConverter` for confidence-to-brush mapping
- `FileSizeConverter` for human-readable file size display
- `BrushConverter` for dynamic resource lookup
- 42 new unit tests

## [0.4.5d] - 2026-01-16

Snippet Apply Service. See [detailed notes](docs/changelog/v0.4.5d.md).

### Added

- `ISnippetApplyService` interface with 5 methods
- `SnippetApplyService` implementation with pattern matching
- `SnippetApplyResult`, `SnippetApplyPreview`, `SnippetLocationSuggestion` records
- `SnippetOptionsValidation` types for validation
- `SnippetApplyException` for error handling
- 6 new unit tests

## [0.4.5c] - 2026-01-16

Partial Snippet Models. See [detailed notes](docs/changelog/v0.4.5c.md).

### Added

- Extended `LineRange` with transformation and conversion methods
- `ColumnRange` struct for character-level operations
- `SnippetInsertMode` enum (Replace, InsertBefore, InsertAfter, Append, Prepend, ReplaceFile)
- `SnippetAnchorType` and `SnippetAnchor` for text-based location
- `AnchorMatchResult` for anchor search results
- `IndentationStyle` record for code formatting
- `SnippetApplyOptions` for snippet application configuration
- `SnippetContext` for file context analysis
- 17 new unit tests

## [0.4.5b] - 2026-01-16

Streaming Diff Preview. See [detailed notes](docs/changelog/v0.4.5b.md).

### Added

- `DiffComputationStatus` enum and `DiffComputationState` model
- `IStreamingDiffCoordinator` interface
- `StreamingDiffCoordinator` with debouncing and content hashing
- `DiffComputationQueue` for prioritized request management
- `StreamingDiffViewModel` for UI binding
- 6 new unit tests

## [0.4.5a] - 2026-01-16

Settings Models. See [detailed notes](docs/changelog/v0.4.5a.md).

### Added

- `DiffViewMode` enum for diff viewer display options
- `AppSettingsValidator` and `SettingsValidationResult` for settings validation
- `SettingChangedEventArgs` for change notifications
- `SettingsDefaults` for centralized defaults and constraints
- 20 new settings properties in `AppSettings` (Code Gen, Diff, Backup, Apply, History)
- 11 new unit tests

## [0.4.4h] - 2026-01-16

Chat Integration. See [detailed notes](docs/changelog/v0.4.4h.md).

### Added

- `IProposalDetectionService` and `ProposalDetectionService` for multi-file proposal detection
- `ChatProposalCoordinator` for workflow orchestration
- `ProgressReporterAdapter` and batch undo event args
- 8 new unit tests

## [0.4.4g] - 2026-01-16

Progress Overlay. See [detailed notes](docs/changelog/v0.4.4g.md).

### Added

- `ApplyProgressOverlay` control with progress bar and phase icons
- `ApplyProgressViewModel` for progress state and cancellation
- `PhaseToIconConverter`, `PhaseToTitleConverter`, `PhaseToBrushConverter`
- 24 new unit tests

## [0.4.4f] - 2026-01-16

Batch Preview. See [detailed notes](docs/changelog/v0.4.4f.md).

### Added

- `BatchPreviewDialog` for previewing all changes before applying
- `BatchPreviewDialogViewModel` for dialog state and navigation
- `DiffPreviewViewModel` for individual file diff display
- `ReferenceEqualsConverter` for selection state binding
- 18 new unit tests

## [0.4.4e] - 2026-01-16

Proposal Panel. See [detailed notes](docs/changelog/v0.4.4e.md).

### Added

- `FileTreeProposalPanel` control for multi-file proposal display
- `FileTreeItemControl` for tree item display with checkboxes/badges
- `IconNameConverter` for icon name to StreamGeometry
- `SelectionStateToCheckStateConverter` for tri-state checkbox
- `ValidationSeverityToBrushConverter` for severity colors
- `ProposalPanelStyles.axaml` with icons and color resources

## [0.4.4d] - 2026-01-16

Tree ViewModels. See [detailed notes](docs/changelog/v0.4.4d.md).

### Added

- `SelectionState` enum for directory tri-state checkboxes
- `TreeNodeSelectionBehavior` enum for selection propagation
- `TreeBuildingOptions` configuration record
- `ITreeBuildingService` and `TreeBuildingService` for tree building
- `TreeNode` data model for hierarchy
- `FileOperationItemViewModel` for tree item display
- `FileTreeProposalViewModel` for proposal management
- 48 unit tests

## [0.4.4c] - 2026-01-16

Proposal Service. See [detailed notes](docs/changelog/v0.4.4c.md).

### Added

- `ProposalServiceOptions` record for service configuration
- `OperationCompletedEventArgs` for operation completion events
- `IFileTreeProposalService` interface for proposal management
- `FileTreeProposalService` with validation, apply, preview, and undo
- `ApplyContext` for state tracking during batch apply
- `RollbackManager` for automatic rollback support
- 40 unit tests

## [0.4.4b] - 2026-01-16

File Tree Parser. See [detailed notes](docs/changelog/v0.4.4b.md).

### Added

- `TreeFormat` enum for tree format types
- `TreeParseResult` record for parsing results
- `ParsedTreeNode` class for tree node structure
- `FileTreeParserOptions` for parser configuration
- `IFileTreeParser` interface for parsing contracts
- `FileTreeParser` service with regex-based parsing
- 55 unit tests

## [0.4.4a] - 2026-01-16

Multi-File Core Models. See [detailed notes](docs/changelog/v0.4.4a.md).

### Added

- `FileOperationType`, `FileOperationStatus`, `FileTreeProposalStatus` enums
- `BatchApplyPhase`, `ValidationSeverity`, `ValidationIssueType` enums
- `FileOperation` model with factory methods and computed properties
- `FileTreeProposal` model with selection and query methods
- `BatchApplyResult` and `BatchApplyProgress` for batch operations
- `ValidationIssue` and `ProposalValidationResult` for validation
- 83 unit tests

## [0.4.3i] - 2026-01-16

Editor Integration. See [detailed notes](docs/changelog/v0.4.3i.md).

### Added

- `RefreshReason` enum for editor refresh types
- `EditorRefreshEventArgs` for refresh event data
- `IEditorRefreshService` interface for coordinating editor refreshes
- `EditorRefreshService` with event coalescing and suspension support
- 22 unit tests


## [0.4.3h] - 2026-01-16

Undo Toast. See [detailed notes](docs/changelog/v0.4.3h.md).

### Added

- `UndoToastViewModel` with live countdown and undo commands
- `UndoToast.axaml` UserControl with animations
- Auto-hide behavior (10 second default)
- Pulse animation when expiring soon
- 18 unit tests


## [0.4.3g] - 2026-01-16

Conflict Dialog. See [detailed notes](docs/changelog/v0.4.3g.md).

### Added

- `ConflictResolution` enum (Cancel, RefreshDiff, ForceApply)
- `ConflictWarningDialogViewModel` with resolution commands
- `ConflictWarningDialog.axaml` with warning UI and file info
- Static `ShowAsync` factory methods
- Keyboard shortcuts (Enter/Escape)
- 14 unit tests for conflict dialog ViewModel

## [0.4.3f] - 2026-01-16

Apply Dialog. See [detailed notes](docs/changelog/v0.4.3f.md).

### Added

- `ApplyChangesDialogViewModel` with MVVM pattern
- `ApplyChangesDialog.axaml` with diff preview layout
- `ApplyChangesDialog.axaml.cs` with `ShowAsync` factory method
- `BoolToStringConverter` utility
- Keyboard shortcuts (Enter/Escape)
- 10 unit tests for dialog ViewModel

## [0.4.3e] - 2026-01-16

Conflict Detection. See [detailed notes](docs/changelog/v0.4.3e.md).

### Added

- `ConflictReason` enum and `ConflictInfo` model with factory methods
- `IConflictDetector` interface with snapshot operations
- `ConflictDetector` with SHA-256 content hashing and parallel checking
- Detection for: content modified, file created, file deleted, permissions
- 17 unit tests for conflict detector

## [0.4.3d] - 2026-01-16

Undo System. See [detailed notes](docs/changelog/v0.4.3d.md).

### Added

- `UndoState`, `UndoOptions`, `UndoEvents` models
- `IUndoManager` interface with undo, query, timer operations
- `UndoManager` with time-windowed expiration and events
- Pause/resume/extend countdown functionality
- 23 unit tests for undo manager

## [0.4.3c] - 2026-01-16

Backup System. See [detailed notes](docs/changelog/v0.4.3c.md).

### Added

- `BackupInfo`, `BackupOptions`, `BackupStorageInfo` models
- `BackupService` with metadata, integrity verification, cleanup
- Naming convention: `{filename}_{timestamp}_{pathHash}{ext}.backup`
- SHA-256 content hashing and incremental backups
- 19 unit tests for backup service

## [0.4.3b] - 2026-01-16

File Change Service. See [detailed notes](docs/changelog/v0.4.3b.md).

### Added

- `IBackupService` interface for backup management
- `IFileChangeService` interface for apply operations
- `FileChangeService` implementation with apply, undo, history, and conflict detection
- Thread-safe operations with semaphore locking
- 18 unit tests for file change service

## [0.4.3a] - 2026-01-16

Core Models (Apply Changes). See [detailed notes](docs/changelog/v0.4.3a.md).

### Added

- `ApplyOptions` configuration record with presets (Default, Silent, Batch)
- `ApplyResult` class with factory methods for success/failure states
- `FileChangeRecord` for undo tracking with time calculations
- Event args for file change notifications
- `LineEndingStyle` enum with detection support
- 41 unit tests for apply models

## [0.4.2h] - 2026-01-16

Theming & Polish. See [detailed notes](docs/changelog/v0.4.2h.md).

### Added

- `DiffColors.axaml` with 30+ color/brush resources for diff viewer
- `DiffStyles.axaml` with 25+ reusable styles for diff components
- Complete dark theme palette (panel, line states, inline, hunk, gutter, buttons, stats)

## [0.4.2g] - 2026-01-16

Diff Navigation. See [detailed notes](docs/changelog/v0.4.2g.md).

### Added

- `DiffNavigationService` for hunk position tracking
- Keyboard shortcuts: Ctrl+↑/↓ (nav), Ctrl+Enter (apply), Ctrl+Home/End (first/last), Ctrl+I/W (toggles), Escape (reject)
- Focus management for DiffViewerPanel
- 19 unit tests for navigation service

## [0.4.2f] - 2026-01-16

Diff Line Rendering. See [detailed notes](docs/changelog/v0.4.2f.md).

### Added

- `DiffHunkControl` with header stats and side-aware line rendering
- `DiffLineControl` with gutter, content, and inline segment highlighting
- `InlineSegmentConverter` for inline change styling
- CSS-class-based conditional styling (`.added`, `.removed`, `.modified`, `.placeholder`)
- Additional color resources for hunk headers and gutters

## [0.4.2e] - 2026-01-16

Side-by-Side Diff View. See [detailed notes](docs/changelog/v0.4.2e.md).

### Added

- `DiffViewerPanel` with side-by-side synchronized scrolling
- Header with file info, stats, navigation controls, and toolbar
- Footer with Apply/Reject action buttons
- Loading and error overlay states
- `IncrementConverter` for 1-based hunk position display
- 6 new icons (ChevronUp/Down, Highlight, WrapText, Link, FileCode)
- 14 new diff color resources in dark theme

## [0.4.2d] - 2026-01-16

Diff ViewModels for Diff Engine. See [detailed notes](docs/changelog/v0.4.2d.md).

### Added

- `DiffViewerViewModel` main coordinator with navigation and action commands
- `DiffHunkViewModel` with side-by-side line alignment
- `DiffLineViewModel` with computed properties and factory methods
- Navigation commands (Next/Previous/First/Last/GoTo hunk)
- Toggle commands (inline changes, word wrap, sync scroll, line numbers)
- Action commands (apply, reject, copy)
- Comprehensive unit tests (63)

## [0.4.2c] - 2026-01-16

Inline Diff Service for Diff Engine. See [detailed notes](docs/changelog/v0.4.2c.md).

### Added

- `IInlineDiffService` interface for character-level diff computation
- `InlineSegment` rendering model for text segments
- `InlineDiffService` implementation using DiffPlex character diffs
- Levenshtein distance similarity calculation
- DiffService integration for inline diff computation
- Comprehensive unit tests (20+)

## [0.4.2b] - 2026-01-16

Diff Service for Diff Engine. See [detailed notes](docs/changelog/v0.4.2b.md).

### Added

- `IDiffService` interface for diff computation
- `DiffOptions` configuration model with presets
- `DiffService` implementation using DiffPlex library
- Synchronous and async diff methods
- Support for CodeBlock-to-diff conversion
- Comprehensive unit tests (29)

## [0.4.2a] - 2026-01-16

Diff Models for Diff Engine. See [detailed notes](docs/changelog/v0.4.2a.md).

### Added

- `DiffResult` top-level container for diff computation
- `DiffHunk` contiguous change group with unified diff headers
- `DiffLine` individual line with `DiffLineType`, `DiffSide` enums
- `InlineChange` character-level change with `InlineChangeType` enum
- `DiffStats` summary statistics record with computed properties
- Static factory methods for NoChanges, NewFile, DeleteFile, BinaryFile
- Comprehensive unit tests (61)

## [0.4.1h] - 2026-01-16

UI Rendering for Code Block Extraction. See [detailed notes](docs/changelog/v0.4.1h.md).

### Added

- `CodeBlockControl` user control for individual code block rendering
- Code block styles: `.code-block`, `.language-badge`, `.status-badge`, etc.
- Value converters: `EnumEqualsConverter`, `GreaterThanOneConverter`, etc.
- Icon resources: DiffIcon, CodeIcon, CheckIcon
- Code block color palette in Dark.axaml
- ChatMessageControl code blocks section with summary panel
- Comprehensive unit tests (38)

## [0.4.1g] - 2026-01-16

ViewModel Integration for Code Block Extraction. See [detailed notes](docs/changelog/v0.4.1g.md).

### Added

- `IClipboardService` interface and `ClipboardService` Avalonia implementation
- `CodeBlockViewModel` with observable properties, commands, and streaming support
- `CodeProposalViewModel` for block collections with aggregate statistics
- `CodeBlockMessages` messaging types for inter-ViewModel communication
- `Reviewing` and `Applying` statuses added to `CodeBlockStatus`
- ChatMessageViewModel enhancements: code block collection, streaming methods
- Comprehensive unit tests (50)

## [0.4.1f] - 2026-01-16

Streaming Parser for Code Block Extraction. See [detailed notes](docs/changelog/v0.4.1f.md).

### Added

- `IStreamingCodeBlockParser` interface for incremental parsing
- `StreamingCodeBlockParser` state machine implementation
- `PartialCodeBlock` model for in-progress blocks
- `StreamingParserState` enum (Text, FenceOpening, CodeContent, FenceClosing)
- `FenceType` enum (Backtick, Tilde)
- Streaming events (BlockStarted, ContentAdded, BlockCompleted)
- `IStreamingParserFactory` and `StreamingParserFactory`
- Support for lang:path fence syntax
- Support for extended fences (4+ backticks) and tilde fences
- Comprehensive unit tests (30)

## [0.4.1e] - 2026-01-16

File Path Inference Service. See [detailed notes](docs/changelog/v0.4.1e.md).

### Added

- `IFilePathInferenceService` interface with 6-strategy cascade
- `FilePathInferenceService` for inferring target paths
- `PathInferenceResult` model with confidence scoring
- `InferenceStrategy` enum (ExplicitPath, SingleContext, LanguageMatch, TypeNameMatch, ContentSimilarity, GeneratedNew)
- `TypeNameExtractor` helper with patterns for 6 languages
- Path inference events (PathInferred, InferenceAmbiguous, InferenceFailed)
- Comprehensive unit tests (91)

## [0.4.1d] - 2026-01-16

Block Classification Service. See [detailed notes](docs/changelog/v0.4.1d.md).

### Added

- `IBlockClassificationService` interface
- `BlockClassificationService` with multi-factor classification
- 16 example indicators, 17 apply indicators, 9 output indicators
- Language-specific complete file structure detection
- Classification confidence scoring (0.70 - 0.95)
- DI registration for ILanguageDetectionService and IBlockClassificationService
- Comprehensive unit tests (43)

## [0.4.1c] - 2026-01-15

Language Detection Service. See [detailed notes](docs/changelog/v0.4.1c.md).

### Added

- `ILanguageDetectionService` interface
- `LanguageDetectionService` with 50+ language definitions
- Alias normalization (cs → csharp, js → javascript, etc.)
- File extension mapping
- Content-based detection heuristics
- Shell/config language classification
- Comprehensive unit tests (30+)

## [0.4.1b] - 2026-01-15

Parser Service for Code Block Extraction. See [detailed notes](docs/changelog/v0.4.1b.md).

### Added

- `ICodeBlockParserService` interface for parsing operations
- `CodeBlockParserService` with regex-based fence parsing
- `CodeBlockEvents.cs` with event argument classes
- Support for `lang:path` fence syntax and file path comments
- Language detection and normalization (20+ languages)
- Block type classification (CompleteFile, Snippet, Command, Config)
- Confidence score calculation
- Comprehensive unit tests (20+)

## [0.4.1a] - 2026-01-15

Core Models for Code Block Extraction. See [detailed notes](docs/changelog/v0.4.1a.md).

### Added

- `CodeBlock` model for extracted code blocks
- `CodeBlockType` enum (CompleteFile, Snippet, Example, Command, Output, Config)
- `CodeBlockStatus` enum (Pending, Applied, Rejected, Skipped, Conflict, Error)
- `TextRange` record struct for character positions
- `LineRange` record struct for line ranges
- `CodeProposal` model for grouping related blocks
- `ProposalStatus` enum for tracking proposal states
- Comprehensive unit tests (20+)

## [0.3.5h] - 2026-01-15

Startup Integration. See [detailed notes](docs/changelog/v0.3.5h.md).

### Added

- `WorkspaceState` and `OpenFileState` models for persisting session state
- `WelcomeViewModel` and `WelcomeView` for welcome screen
- `ShowWelcome` property on MainWindowViewModel
- Welcome screen with recent workspaces and keyboard hints
- Comprehensive unit tests

## [0.3.5g] - 2026-01-15

Keyboard Shortcuts Manager. See [detailed notes](docs/changelog/v0.3.5g.md).

### Added

- `IKeyboardShortcutService` interface and `KeyboardShortcutService` implementation
- `ShortcutInfo` model with display formatting
- 18 default shortcuts across 7 categories
- `ExecuteCommandAsync` in MainWindowViewModel for command routing
- Comprehensive unit tests

## [0.3.5f] - 2026-01-15

Global Drag-Drop. See [detailed notes](docs/changelog/v0.3.5f.md).

### Added

- Drag-drop file/folder support on MainWindow
- Drop overlay with contextual messages
- DropIcon for overlay visual
- FileExplorer.OpenFolderByPath and RequestOpenFile helpers

## [0.3.5e] - 2026-01-15

Recent Workspaces Menu. See [detailed notes](docs/changelog/v0.3.5e.md).

### Added

- RecentWorkspaceItemViewModel with path shortening (~) and relative time formatting
- RecentWorkspacesViewModel with load, pin, remove, clear commands
- RecentWorkspacesMenu.axaml popup menu with workspace items
- Menu colors and icon-button-small styles

## [0.3.5d] - 2026-01-15

Enhanced Status Bar. See [detailed notes](docs/changelog/v0.3.5d.md).

### Added

- StatusBarViewModel with 10 segments (workspace, file, language, cursor, encoding, line-ending, unsaved, watcher, model)
- StatusBar.axaml with interactive buttons and tooltips
- BoolToOpacityConverter for file watcher indicator
- SyncIcon, BrainIcon, FileCodeIcon
- StatusBar theme colors and status-item styles

## [0.3.5c] - 2026-01-15

Quick Open Dialog. See [detailed notes](docs/changelog/v0.3.5c.md).

### Added

- IFileIndexService interface and FileIndexService with fuzzy search
- QuickOpenViewModel with debounced search
- QuickOpenDialog with keyboard navigation (↑↓ Enter Esc)
- Recent files tracking
- FileSearchResult model

## [0.3.5b] - 2026-01-15

Workspace Settings Panel UI. See [detailed notes](docs/changelog/v0.3.5b.md).

### Added

- WorkspaceSettingsPanel.axaml with 3 organized sections
- File Explorer section (6 controls)
- Editor section (9 controls)
- Context Attachment section (6 controls)
- Footer with Reset/Cancel/Save buttons
- Primary button style and theme resources

## [0.3.5a] - 2026-01-15

Workspace Settings ViewModel. See [detailed notes](docs/changelog/v0.3.5a.md).

### Added

- WorkspaceSettingsViewModel with 21 setting properties
- File Explorer settings (6): restore workspace, hidden files, gitignore, etc.
- Editor settings (9): font, size, tabs, line numbers, themes, etc.
- Context settings (6): token limits, file size, warnings
- Validation logic for all numeric ranges
- Change tracking with HasChanges property
- Save, Cancel, ResetToDefaults commands
- 4 new properties in AppSettings

### Unit Tests

- 18 tests for WorkspaceSettingsViewModel

## [0.3.4h] - 2026-01-15

Message History Integration. See [detailed notes](docs/changelog/v0.3.4h.md).

### Added

- AttachedContexts property to ChatMessage model
- Context display properties to ChatMessageViewModel (HasAttachedContexts, AttachedContextCount, TotalAttachedTokens)
- ToggleContextExpandedCommand for expand/collapse in message bubbles
- AttachedContextsJson property to MessageEntity for database persistence
- JSON serialization/deserialization in DatabaseConversationService

### Unit Tests

- 7 tests for ChatMessage and ChatMessageViewModel context functionality

## [0.3.4g] - 2026-01-15

File Explorer Integration. See [detailed notes](docs/changelog/v0.3.4g.md).

### Added

- Drop zone visual indicator in ChatView for drag-drop feedback
- Drag-drop handlers (DragEnter, DragOver, DragLeave, Drop) in ChatView
- AttachFileAsync method in MainWindowViewModel for file attachment
- Updated OnFileAttachRequested to use AttachFileAsync

## [0.3.4f] - 2026-01-15

Editor Integration. See [detailed notes](docs/changelog/v0.3.4f.md).

### Added

- SelectionInfo model for selection data
- SelectionAttachmentEventArgs for event handling
- AttachSelectionRequested event on EditorPanel
- GetCurrentSelection(), AttachSelection(), AttachCurrentFile() methods
- Keyboard shortcuts: Ctrl+Shift+A, Ctrl+Shift+F
- HasSelection property on EditorPanelViewModel

### Unit Tests

- 7 tests for SelectionInfo and SelectionAttachmentEventArgs

## [0.3.4e] - 2026-01-15

Context Preview. See [detailed notes](docs/changelog/v0.3.4e.md).

### Added

- ContextPreviewPopup user control with header, content, footer
- ContextPreviewViewModel for preview state management
- Popup theme resources (background, border, header, footer)
- Commands: ShowPreview, HidePreview, OpenInEditor, Remove, Copy
- Escape key and backdrop click to close

### Unit Tests

- 10 tests for ContextPreviewViewModel

## [0.3.4d] - 2026-01-15

Chat Context Bar UI. See [detailed notes](docs/changelog/v0.3.4d.md).

### Added

- ChatContextBar user control with context pill layout
- ChatContextBarViewModel with token tracking and commands
- TokenCountConverter for formatting token counts
- Context bar theme resources (pills, badges, warnings)
- Icons: AttachmentIcon, CloseIcon, SelectionIcon, ClipboardIcon
- Token limit warning and error states

### Unit Tests

- 21 tests for TokenCountConverter and ChatContextBarViewModel

## [0.3.4c] - 2026-01-15

File Context ViewModel. See [detailed notes](docs/changelog/v0.3.4c.md).

### Added

- ContextAttachmentType enum (File, Selection, Clipboard)
- FileContextViewModel with observable and computed properties
- Factory methods (FromFile, FromSelection, FromClipboard)
- Display properties (DisplayLabel, ShortLabel, PreviewContent, Badge, IconKey)
- ToFileContext and FromFileContext conversions

### Unit Tests

- 33 tests for FileContextViewModel

## [0.3.4b] - 2026-01-15

Context Formatter. See [detailed notes](docs/changelog/v0.3.4b.md).

### Added

- IContextFormatter interface for context formatting
- ContextFormatter with prompt, display, and storage formatting
- ContextPromptTemplates for standardized templates
- Support for code block syntax highlighting hints
- Truncated preview for collapsed display mode

### Unit Tests

- 21 tests for context formatting

## [0.3.4a] - 2026-01-15

Token Estimation Service. See [detailed notes](docs/changelog/v0.3.4a.md).

### Added

- ITokenEstimationService interface for token estimation
- TokenEstimationService with three algorithms (CharacterBased, WordBased, BpeApproximate)
- TokenEstimationMethod enum
- TokenUsageBreakdown model with usage tracking
- ContextLimitsConfig for configurable limits
- Content truncation with word-boundary awareness

### Unit Tests

- 36 tests for token estimation and usage breakdown

## [0.3.3g] - 2026-01-14

Dialogs & Integration. See [detailed notes](docs/changelog/v0.3.3g.md).

### Added

- IDialogService interface for platform dialogs
- DialogService implementation using Avalonia storage providers
- GoToLineDialog with input validation
- MessageDialog with configurable buttons and icons
- Dialog icons (InfoIcon, WarningIcon, ErrorIcon, QuestionIcon)
- Semantic color brushes (DangerBrush, WarningBrush, SuccessBrush)

### Unit Tests

- 12 new tests for DialogService and MessageDialogIcon

## [0.3.3f] - 2026-01-14

Find & Replace. See [detailed notes](docs/changelog/v0.3.3f.md).

### Added

- EditorSearchManager static utility class
- OpenFind/OpenReplace with selection-based auto-fill
- FindNext/FindPrevious navigation methods
- F3 and Shift+F3 keyboard shortcuts
- Escape to close search panel

### Unit Tests

- 15 new tests for EditorSearchManager

## [0.3.3e] - 2026-01-14

Editor Panel UI. See [detailed notes](docs/changelog/v0.3.3e.md).

### Added

- EditorPanel.axaml and EditorPanel.axaml.cs view implementation
- LanguageToIconConverter for mapping languages to icons
- Tab bar with tab switching, close, and context menu
- Empty state display with "No file open" message
- Keyboard shortcuts (Ctrl+S, Ctrl+W, Ctrl+G, Ctrl+F, Ctrl+H, Ctrl+Tab)
- TextEditor integration with syntax highlighting and caret tracking
- FileCodeIcon and TerminalIcon geometry resources
- Tab bar styles in App.axaml

### Unit Tests

- 40 new tests for LanguageToIconConverter

## [0.3.3d] - 2026-01-14

Editor Configuration. See [detailed notes](docs/changelog/v0.3.3d.md).

### Added

- EditorConfiguration static utility class
- ApplySettings, ApplyDefaults, BindToSettings methods
- ConvertTabsToSpaces and RulerColumn properties to AppSettings

### Unit Tests

- 21 new tests for EditorConfiguration

## [0.3.3c] - 2026-01-14

Syntax Highlighting Service. See [detailed notes](docs/changelog/v0.3.3c.md).

### Added

- SyntaxHighlightingService with TextMate integration
- Support for 46+ programming languages
- 7 built-in themes (DarkPlus, LightPlus, Monokai, Solarized, HighContrast)
- AvaloniaEdit.TextMate package dependency

### Unit Tests

- 46 new tests for SyntaxHighlightingService

## [0.3.3b] - 2026-01-14

Editor Panel ViewModel. See [detailed notes](docs/changelog/v0.3.3b.md).

### Added

- EditorPanelViewModel with tab management and file operations
- IDialogService interface for platform-independent dialogs
- Tab navigation (next/prev with wrap-around)
- Editor commands (undo, redo, find, replace, go-to-line)
- Unsaved changes prompt flow (Save/Don't Save/Cancel)

### Unit Tests

- 31 new tests for EditorPanelViewModel

## [0.3.3a] - 2026-01-14

Editor Tab ViewModel. See [detailed notes](docs/changelog/v0.3.3a.md).

### Added

- EditorTabViewModel with factory methods (FromFile, CreateNew)
- Hash-based dirty state detection
- Caret position and selection tracking
- Line ending detection (LF, CRLF, CR)
- Avalonia.AvaloniaEdit v11.3.0 package

### Unit Tests

- 31 new tests for EditorTabViewModel

## [0.3.2g] - 2026-01-14

Main window integration. See [detailed notes](docs/changelog/v0.3.2g.md).

### Added

- FileExplorerViewModel integration with MainWindowViewModel
- HasOpenWorkspace property for EditorPanel visibility
- WorkspaceChanged event handling
- FileOpenRequested and FileAttachRequested event wiring
- Sidebar and TreeView theme colors in Dark.axaml
- FileIcons.axaml resource inclusion in App.axaml
- FileExplorerViewModel DI registration

### Unit Tests

- 8 new tests for workspace integration

## [0.3.2f] - 2026-01-14

Keyboard & interactions documentation. See [detailed notes](docs/changelog/v0.3.2f.md).

### Documentation

- Complete keyboard shortcut reference table
- Inline rename flow with validation rules
- Delete confirmation flow diagrams
- Focus management patterns

### Shortcuts

| Key | Action |
|-----|--------|
| Enter | Open file / Toggle folder |
| F2 | Start rename |
| Delete | Delete with confirmation |
| Ctrl+C | Copy absolute path |
| Ctrl+Shift+C | Copy relative path |
| Ctrl+N | New file |
| Ctrl+Shift+N | New folder |
| Ctrl+R | Refresh tree |

## [0.3.2e] - 2026-01-14

Context menus & icon colors. See [detailed notes](docs/changelog/v0.3.2e.md).

### Added

- **IconColorConverter** - IMultiValueConverter for language-specific icon colors

### Features

- 17 language-specific colors (C#, JS, TS, Python, HTML, CSS, etc.)
- Folder gold color (#E8A838) for all directories
- Default gray (#8B8B8B) for unknown file types
- GetColorForIconKey() and GetAllColors() helper methods

## [0.3.2d] - 2026-01-14

Tree view UI. See [detailed notes](docs/changelog/v0.3.2d.md).

### Added

- **FileExplorerView.axaml** - Main file explorer UI with TreeView
- **FileExplorerView.axaml.cs** - Code-behind with keyboard handlers

### Features

- Header with workspace name and action buttons
- Filter box with search icon and clear button
- TreeView with TreeDataTemplate for file/folder hierarchy
- Inline rename mode (F2, Enter/Escape)
- Keyboard navigation (Enter, Delete, Arrow keys, Ctrl+C/N/R)
- Context menu with file operations
- Empty state for no workspace
- Error message display

## [0.3.2c] - 2026-01-14

File icon system. See [detailed notes](docs/changelog/v0.3.2c.md).

### Added

- **SVG IconPaths** - 25+ file type icons with 24x24 SVG path data
- **FileIconConverter** - IValueConverter for XAML PathIcon binding
- **FileIcons.axaml** - Resource dictionary with colors and UI icons

### Features

- GetIconPath() returns SVG data for icon keys
- GetAllIconKeys() lists all available icons
- 11 language-specific color brushes
- 14 static UI geometry icons

## [0.3.2b] - 2026-01-14

File explorer ViewModel. See [detailed notes](docs/changelog/v0.3.2b.md).

### Added

- **FileExplorerViewModel** - Workspace management, file operations, debounced filter
- **FileOpenRequestedEventArgs** - Event args for file open requests
- **FileAttachRequestedEventArgs** - Event args for context attachment
- **DeleteConfirmationEventArgs** - Event args for delete confirmation

### Features

- Open workspace via folder picker, close, refresh commands
- New file/folder with inline rename
- Copy path, copy relative path, reveal in finder
- 200ms debounced filter with auto-expand
- Attach file to chat context

## [0.3.2a] - 2026-01-14

File tree item ViewModel. See [detailed notes](docs/changelog/v0.3.2a.md).

### Added

- **FileTreeItemViewModel** - Observable properties, lazy loading, inline rename, filtering
- **IFileTreeItemParent** - Interface for parent callbacks
- **FileIconProvider** - Extension-based icon key resolution (50+ mappings)
- **Icon Features** - Special folder icons (src, test, docs), special file icons (README, package.json)

### Technical Details

- 30 new unit tests covering factory, properties, rename, filtering, icons
- Recursive filter application with auto-expand for matches

## [0.3.1f] - 2026-01-14

Repository layer enhancements. See [detailed notes](docs/changelog/v0.3.1f.md).

### Added

- **Max Recent Enforcement** - Limit of 20 workspaces, oldest non-pinned auto-removed
- **EnforceMaxRecentAsync** - Private method in WorkspaceRepository
- **AppSettings Extensions**:
  - Workspace: ShowHiddenFiles, UseGitIgnore, MaxRecentWorkspaces, CustomIgnorePatterns
  - Editor: WordWrap, TabSize, ShowLineNumbers, HighlightCurrentLine, FontFamily, FontSize
- **DI Registration** - IFileSystemService, IWorkspaceService, IWorkspaceRepository

### Technical Details

- 10 new repository unit tests (43 total Workspace tests)
- Pinned workspaces preserved during max enforcement

## [0.3.1e] - 2026-01-14

Workspace lifecycle management service. See [detailed notes](docs/changelog/v0.3.1e.md).

### Added

- **IWorkspaceService Interface** - Workspace lifecycle management
  - Open/close workspace operations with event notifications
  - Recent workspaces management (pin, rename, remove)
  - State persistence (open files, expanded folders)
  - 30-second auto-save timer
  - Workspace restoration on startup
- **WorkspaceEvents** - Event args and enums for workspace changes
- **IWorkspaceRepository + WorkspaceRepository** - EF Core persistence
- **RestoreLastWorkspace** setting in AppSettings

### Technical Details

- 18 unit tests in WorkspaceServiceTests
- Thread-safe operations with SemaphoreSlim
- Integrates with IFileSystemService for file watching and .gitignore

## [0.3.1d] - 2026-01-14

File system service for workspace operations. See [detailed notes](docs/changelog/v0.3.1d.md).

### Added

- **IFileSystemService Interface** - Workspace-aware file operations
  - Directory operations: list, create, delete (recursive)
  - File operations: read, write, create, delete, rename, copy, move
  - Debounced file watching (200ms window)
  - Binary file detection via signatures (PNG, JPEG, PDF, etc.)
  - .gitignore pattern matching with negation support
- **FileSystemChangeEvent** - Change notification data
- **FileSystemChangeType** - Created, Modified, Deleted, Renamed enum

### Technical Details

- 27 unit tests (FileSystemServiceTests + GitIgnorePatternTests)
- Last-match-wins semantics for gitignore patterns

## [0.3.1c] - 2026-01-14

Enhanced language detection utility. See [detailed notes](docs/changelog/v0.3.1c.md).


### Added

- **LanguageDetector Enhancements** - Complete rewrite
  - 83 file extension mappings (expanded from ~60)
  - 35 special file names (Dockerfile, package.json, tsconfig.json, etc.)
  - `DetectByPath()` - Detect from full file paths
  - `GetDisplayName()` - Human-readable names (C#, F#, TypeScript)
  - `GetIconName()` - Icon identifiers for UI
  - `GetAllLanguages()` - Unique language list
  - `GetExtensionsForLanguage()` - Reverse lookup
  - `IsLikelyTextFile()` - Text file heuristic

### Technical Details

- 71 unit tests for LanguageDetector
- VSCode/TextMate language identifier conventions

## [0.3.1b] - 2026-01-14

Database entities for workspace persistence. See [detailed notes](docs/changelog/v0.3.1b.md).


### Added

- **RecentWorkspaceEntity** - Workspace state persistence
  - JSON serialization for OpenFiles, ExpandedFolders
  - ToWorkspace/FromWorkspace mapping
  - UpdateFrom for preserving Id

- **FileContextEntity** - File context history
  - FK to Conversations and Messages (CASCADE delete)
  - FromFileContext/ToFileContextStub mapping
  - FileContextStub for lightweight queries

- **EF Core Configurations** - Table schemas
  - RecentWorkspaces: Unique RootPath, DESC LastAccessedAt index
  - FileContextHistory: FK indexes, AttachedAt index

- **DbContext Updates** - New DbSets
  - RecentWorkspaces, FileContextHistory

### Technical Details

- 16 new unit tests for entity mapping and JSON handling
- Configurations auto-discovered via ApplyConfigurationsFromAssembly

## [0.3.1a] - 2026-01-14

Core models for workspace awareness. See [detailed notes](docs/changelog/v0.3.1a.md).


### Added

- **Workspace Model** - Project folder representation
  - Properties: Id, Name, RootPath, OpenedAt, LastAccessedAt, OpenFiles, ActiveFilePath
  - UI state: ExpandedFolders, IsPinned, GitIgnorePatterns
  - Path utilities: GetAbsolutePath, GetRelativePath, ContainsPath, Touch
  - Computed: DisplayName (custom name or folder), Exists

- **FileSystemItem Model** - File/directory representation
  - Core: Path, Name, Type, Size, ModifiedAt, AccessedAt, CreatedAt
  - Metadata: IsHidden, IsReadOnly, HasChildren, IsExpanded, Children
  - Factory methods: FromFileInfo, FromDirectoryInfo
  - Computed: Extension, Language, FormattedSize, ParentPath

- **FileContext Model** - Attached file content for chat
  - Content: FilePath, Content, Language, LineCount, EstimatedTokens
  - Partial support: StartLine, EndLine, IsPartialContent
  - Factory methods: FromFile, FromSelection, FormatForLlmContext
  - Change detection: ContentHash (SHA256 prefix)

- **TokenEstimator Utility** - LLM context budget estimation
  - Estimate(content) - ~3.5 chars/token baseline
  - Estimate(content, language) - language-specific multipliers
  - MaxContentLength, ExceedsBudget helpers

- **LanguageDetector Utility** - File extension to language mapping
  - 50+ extensions mapped (csharp, python, typescript, etc.)
  - Special files: Dockerfile, Makefile, Gemfile, etc.
  - GetAllSupportedExtensions, IsSupported helpers

### Technical Details

- New Utilities/ folder in AIntern.Core
- 40 new unit tests (Workspace: 10, FileSystemItem: 12, FileContext: 14, TokenEstimator: 15)
- Full XML documentation with remarks and examples

## [0.2.5g] - 2026-01-14


Status bar enhancements with interactive elements and keyboard shortcuts. See [detailed notes](docs/changelog/v0.2.5g.md).

### Added

- **Interactive Status Bar** - Clickable elements in status bar
  - Clickable model name that opens sidebar (shows model selector)
  - Model name color indicates load state: accent (#00d9ff) loaded, muted (#888888) unloaded
  - Clickable temperature that expands inference settings panel
  - Color-coded save status: green (saved), yellow (unsaved), accent (saving)
  - SaveState observable property tracking IConversationService.SaveStateChanged
  - IsModelLoaded observable property for status bar color binding

- **New Converters** - Value converters for status bar display
  - BoolToAccentColorConverter: bool to accent/muted IBrush for model name
  - SaveStatusTextConverter: SaveStateChangedEventArgs to "Saving..."/"Unsaved"/"Saved"
  - SaveStatusColorConverter: SaveStateChangedEventArgs to color-coded IBrush
  - Comprehensive unit tests (24 test cases for all converters)

- **New Keyboard Shortcuts** - Additional global shortcuts
  - Ctrl+, toggles inference settings panel expansion
  - Ctrl+P opens system prompt editor window
  - Ctrl+Shift+S forces save of current conversation

### Technical Details

- IConversationService.SaveStateChanged event subscription in MainWindowViewModel
- ToggleSettingsPanelCommand auto-shows sidebar when expanding settings
- Static cached brushes in converters for memory efficiency
- Singleton pattern for all converters (Instance property)
- 14 new MainWindowViewModel tests for v0.2.5g functionality

## [0.2.5f] - 2026-01-14

Export UI components with format selection and live preview. See [detailed notes](docs/changelog/v0.2.5f.md).

### Added

- **Export Dialog** - Export conversation UI (Ctrl+E)
  - ExportViewModel with format selection, options, and live preview
  - ExportDialog with 2-row grid layout (550x500)
  - Format selection via RadioButtons (Markdown, JSON, PlainText, HTML)
  - Option checkboxes (timestamps, system prompt, metadata, token counts)
  - Live preview that updates when format or options change
  - Native file save dialog via IStorageProvider
  - Cancel button and Escape key to close without export
  - Error handling with inline error message display
  - IExportService integration for content generation
  - Comprehensive unit tests (37 test cases)
  - Full XML documentation and logging with timing

- **EnumBoolConverter** - New converter for RadioButton ↔ enum binding
  - Singleton pattern with Instance property
  - Case-insensitive enum comparison
  - Returns DoNothing when RadioButton unchecked
  - Unit tests (14 test cases)

### Technical Details

- Manual ViewModel creation (requires runtime conversationId parameter)
- CancellationTokenSource for preview cancellation
- HasActiveConversation CanExecute check for OpenExportCommand
- IDisposable pattern for CTS cleanup
- Ctrl+E keybinding in MainWindow.axaml

## [0.2.5e] - 2026-01-14

Search UI components with spotlight-style dialog. See [detailed notes](docs/changelog/v0.2.5e.md).

### Added

- **Search Dialog** - Spotlight-style search UI (Ctrl+K)
  - SearchViewModel with 300ms debounced search
  - SearchDialog with borderless window styling (600x500)
  - Filter tabs (All, Conversations, Messages)
  - Keyboard navigation (Up/Down arrows with wrap-around)
  - Enter to select result, Escape to close
  - Grouped results by type (conversations then messages)
  - Status bar showing result count and search timing
  - ISearchService integration for FTS5 search
  - Comprehensive unit tests (31 test cases)
  - Full XML documentation and logging with timing

### Technical Details

- 300ms debounce using System.Timers.Timer
- CancellationTokenSource for search cancellation
- ObservableCollection for grouped results
- Transient ViewModel (fresh per dialog instance)
- IDisposable pattern for timer cleanup
- Ctrl+K keybinding in MainWindow.axaml

## [0.2.5d] - 2026-01-14

Migration service for version upgrades. See [detailed notes](docs/changelog/v0.2.5d.md).

### Added

- **Migration Service** - Automatic migration from v0.1.0 to v0.2.0
  - IMigrationService interface with MigrateIfNeededAsync, GetCurrentVersionAsync, IsMigrationRequiredAsync
  - MigrationService implementation with automatic detection and backup
  - MigrationResult record with Success, FromVersion, ToVersion, MigrationSteps, ErrorMessage
  - LegacySettings model for v0.1.0 settings.json format
  - AppVersionEntity for persistent version tracking in database
  - AppVersionConfiguration with descending index on MigratedAt
  - Automatic backup of settings.json to settings.v1.json.bak before migration
  - Version stamping in AppVersions table after successful migration
  - Startup integration in App.axaml.cs (runs after DatabaseInitializer)
  - Comprehensive unit tests (26 test cases)
  - Full XML documentation and logging with timing

### Technical Details

- Singleton service with scoped dependencies (factory pattern for DbContext)
- CurrentVersion (0.2.0) and LegacyVersion (0.1.0) constants
- Idempotent migration (safe to run multiple times)
- Graceful error handling (app continues even if migration fails)

## [0.2.5c] - 2026-01-14

Export service layer for conversation export. See [detailed notes](docs/changelog/v0.2.5c.md).

### Added

- **Export Service** - Service layer for conversation export operations
  - IExportService interface with ExportAsync, GeneratePreviewAsync, GetFileExtension, GetMimeType
  - ExportService implementation with 4 format exporters
  - ExportFormat enum (Markdown, Json, PlainText, Html)
  - ExportOptions model with configurable options (timestamps, system prompt, metadata, token counts)
  - ExportResult model with success/content/filename/mime type
  - Markdown export with headers, bold roles, horizontal separators
  - JSON export with structured messages array
  - PlainText export with title underline and [HH:mm] timestamps
  - HTML export with embedded dark theme CSS (responsive)
  - SanitizeFileName utility with [GeneratedRegex] for safe cross-platform filenames
  - GeneratePreviewAsync for truncated UI previews
  - Comprehensive unit tests (34 test cases)
  - Full XML documentation and logging with timing

### Technical Details

- Factory pattern for DI registration (singleton with scoped repository)
- Source-generated regex for filename sanitization performance
- Dark theme HTML with color-coded message roles (cyan/purple/gold)
- ExportOptions static factories: Default (full) and Minimal (content only)

## [0.2.5b] - 2026-01-14

Search service layer for full-text search. See [detailed notes](docs/changelog/v0.2.5b.md).

### Added

- **Search Service** - Service layer for full-text search operations
  - ISearchService interface with SearchAsync, RebuildIndexAsync, GetSuggestionsAsync
  - SearchService implementation wrapping AInternDbContext FTS5 operations
  - Recent search tracking for autocomplete suggestions (max 20)
  - Thread-safe in-memory suggestion cache
  - Case-insensitive prefix matching for suggestions
  - Comprehensive unit tests (25 test cases)
  - Full XML documentation and logging with timing

### Technical Details

- Thin wrapper pattern over existing DbContext FTS5 methods
- Factory pattern for DI registration (singleton with scoped DbContext)
- In-memory recent search cache for session-scoped suggestions
- Duplicate detection moves searches to most recent position

## [0.2.5a] - 2026-01-13

FTS5 search infrastructure for full-text search. See [detailed notes](docs/changelog/v0.2.5a.md).

### Added

- **FTS5 Search Infrastructure** - Full-text search for conversations and messages
  - SearchResultType enum (Conversation, Message) for categorizing search results
  - SearchResult record with Id, ResultType, Title, Preview, Rank, Timestamp, ConversationId, MessageId
  - SearchQuery record with QueryText, MaxResults, IncludeConversations, IncludeMessages, MinRank
  - SearchResults record with Results, TotalCount, Query, SearchDuration
  - AInternDbContext.EnsureFts5TablesAsync() - Creates FTS5 virtual tables and triggers
  - AInternDbContext.RebuildFts5IndexesAsync() - Rebuilds FTS indexes from source data
  - AInternDbContext.SearchAsync(SearchQuery) - Executes full-text search with BM25 ranking
  - ConversationsFts virtual table indexing conversation titles
  - MessagesFts virtual table indexing message content
  - 6 synchronization triggers for automatic FTS index maintenance

### Technical Details

- Uses SQLite FTS5 with external content pattern (no data duplication)
- BM25 ranking algorithm for relevance scoring (lower = better match)
- snippet() function for highlighted search previews
- Trigger-based synchronization for INSERT/UPDATE/DELETE operations

## [0.2.4e] - 2026-01-13

Chat integration for system prompt feature. See [detailed notes](docs/changelog/v0.2.4e.md).

### Added

- SystemPromptSelector.axaml: Chat header dropdown control (139 lines)
  - ComboBox dropdown with all available prompts including "(No prompt)" option
  - Default prompt indicator (star icon)
  - Category display for template prompts
  - Edit button that raises routed event to open editor window
- SystemPromptSelector.axaml.cs: Code-behind with routed event (183 lines)
  - EditButtonClickEvent with Bubble routing strategy
  - Event handler wiring for Edit button
  - Exhaustive logging with [ENTER]/[EXIT]/[INFO]/[INIT] markers
- ChatView.axaml: 4-row Grid layout (expanded from 2-row)
  - Row 0: SystemPromptSelector header with dropdown
  - Row 1: System prompt content Expander (collapsible, shows name and content)
  - Row 2: Messages list (existing)
  - Row 3: Input area (existing)
- ChatViewModel.cs: System prompt integration (~150 lines)
  - SystemPromptSelectorViewModel property for header dropdown binding
  - ShowSystemPromptMessage, SystemPromptContent, SystemPromptName properties
  - BuildContextWithSystemPrompt() method prepends system message to LLM context
  - OnCurrentPromptChanged event handler for UI synchronization
- MainWindowViewModel.cs: Editor command and window reference (~80 lines)
  - SetMainWindow(Window) method for dialog display
  - OpenSystemPromptEditorCommand for opening editor as modal dialog
  - InitializeAsync updates to initialize SystemPromptSelectorViewModel
- SystemPromptBackgroundColor in Dark.axaml: Subtle blue background (#1A2D3D) for prompt expander

### Changed

- ChatView layout expanded to 4 rows for system prompt header and expander
- ChatViewModel constructor now takes ISystemPromptService and SystemPromptSelectorViewModel
- MainWindowViewModel constructor now takes ISystemPromptService and IDispatcher

## [0.2.4d] - 2026-01-13

System prompt editor window UI. See [detailed notes](docs/changelog/v0.2.4d.md).

### Added

- SystemPromptEditorWindow.axaml: Full editor dialog (428 lines)
  - Split-pane layout: 260px prompt list sidebar + editor panel
  - User prompts section with default star indicator and usage count
  - Templates section with category badges and descriptions
  - Name, description, and content editing with validation
  - Character count and estimated token count display
  - "Modified" indicator for unsaved changes
  - Read-only mode for built-in templates with "Create Copy to Edit" action
  - Loading overlay and error banner
- SystemPromptEditorWindow.axaml.cs: Code-behind with lifecycle management (377 lines)
  - OnOpened: Initializes ViewModel
  - OnClosing: Unsaved changes handling with UnsavedChangesDialog
  - OnKeyDown: Keyboard shortcuts (Ctrl+S, Ctrl+N, Escape)
  - IDisposable for ViewModel cleanup
  - Exhaustive logging with [ENTER]/[EXIT]/[INFO]/[SKIP]/[ERROR]/[INIT]/[DISPOSE] markers
- Keyboard shortcuts: Ctrl+S (save), Ctrl+N (new), Escape (discard/close)
- Editor styles: Window.Styles (prompt-item, template-item), Dark.axaml (CodeEditorTextBox, LinkButton)
- Icons in Icons.axaml: StarIcon, CopyIcon, DefaultIcon, PromptIcon
- ClearErrorMessageCommand in SystemPromptEditorViewModel for error banner dismiss

## [0.2.4c] - 2026-01-13

Editor ViewModels for system prompt management. See [detailed notes](docs/changelog/v0.2.4c.md).

### Added

- SystemPromptViewModel: List item ViewModel for system prompts (~320 lines)
  - Observable properties: Name, Content, Description, Category, IsBuiltIn, IsDefault, IsSelected, UsageCount
  - Computed properties: CharacterCount, EstimatedTokenCount, ContentPreview, TypeLabel, CategoryIcon
  - Constructor mapping from SystemPrompt domain model
  - Property change handlers for computed property notifications
- SystemPromptEditorViewModel: Full editor ViewModel with CRUD operations (~550 lines)
  - Prompt lists: UserPrompts, Templates (ObservableCollection<SystemPromptViewModel>)
  - Editor state: PromptName, PromptDescription, PromptCategory, EditorContent
  - State flags: IsDirty, IsEditing, IsNewPrompt, IsLoading
  - Computed properties: CharacterCount, EstimatedTokenCount, HasContent, HasValidName, CanSave, CanEdit, CanDelete, CanSetDefault
  - Commands: Initialize, LoadPrompts, CreateNewPrompt, SavePrompt, DeletePrompt, DuplicatePrompt, SetAsDefault, CreateFromTemplate, DiscardChanges
  - Dirty tracking via _originalPrompt comparison
  - Event subscription to ISystemPromptService.PromptListChanged
- SystemPromptSelectorViewModel: Quick selector for chat header dropdown (~200 lines)
  - Properties: AvailablePrompts (with "No prompt" option), SelectedPrompt, IsLoading
  - Computed: HasPromptSelected, DisplayText, ContentPreview, SelectedCategory, IsBuiltInSelected
  - Commands: Initialize, SelectPrompt, RefreshPrompts
  - Event subscriptions: PromptListChanged, CurrentPromptChanged

### Changed

- ServiceCollectionExtensions: Added DI registrations
  - SystemPromptEditorViewModel (Transient - per editor window)
  - SystemPromptSelectorViewModel (Singleton - shared state)
  - Updated XML documentation

## [0.2.4b] - 2026-01-13

System prompt service layer with CRUD operations and event notifications. See [detailed notes](docs/changelog/v0.2.4b.md).

### Added

- ISystemPromptService: Service interface for system prompt management
  - CurrentPrompt property for the selected prompt
  - Query methods: GetAllPromptsAsync, GetUserPromptsAsync, GetTemplatesAsync, GetByIdAsync, GetDefaultPromptAsync, SearchPromptsAsync
  - Mutation methods: CreatePromptAsync, CreateFromTemplateAsync, UpdatePromptAsync, DeletePromptAsync, DuplicatePromptAsync, SetAsDefaultAsync, SetCurrentPromptAsync
  - Utility: FormatPromptForContext, InitializeAsync
  - Events: PromptListChanged, CurrentPromptChanged
- SystemPromptService: Full implementation (~750 lines)
  - Thread-safe with SemaphoreSlim (same pattern as InferenceSettingsService)
  - Exhaustive logging with [ENTER]/[INFO]/[SKIP]/[EXIT]/[EVENT] markers
  - Auto-increment duplicate names for CreateFromTemplateAsync/DuplicatePromptAsync
  - Automatic fallback to default when current prompt is deleted
- PromptListChangeType: Enum for list change categorization (PromptCreated, PromptUpdated, PromptDeleted, DefaultChanged, ListRefreshed)
- PromptListChangedEventArgs: Event args with ChangeType, AffectedPromptId, AffectedPromptName
- CurrentPromptChangedEventArgs: Event args with NewPrompt, PreviousPrompt

### Changed

- AppSettings: Added CurrentSystemPromptId property for persisting prompt selection
- ServiceCollectionExtensions: Added ISystemPromptService DI registration as singleton with scoped repository factory

## [0.2.4a] - 2026-01-13

System prompt domain model and repository implementation. See [detailed notes](docs/changelog/v0.2.4a.md).

### Added

- SystemPrompt: Rich domain model for system prompts
  - 12 properties with validation constants (NameMaxLength, ContentMaxLength, etc.)
  - Validate() method returning ValidationResult with all constraint violations
  - Duplicate(newName?) for creating user copies of templates
  - Computed properties: CharacterCount, EstimatedTokenCount (~4 chars/token)
  - FromEntity()/ToEntity() mapping methods for EF Core integration
- SystemPromptTemplates: 8 built-in prompt templates with well-known GUIDs
  - Default Assistant (General, IsDefault=true)
  - The Senior Intern (Creative, snarky coding assistant)
  - Code Expert, Rubber Duck, Code Reviewer, Debugger (Code category)
  - Technical Writer, Socratic Tutor (Technical/General)
- ISystemPromptRepository: Complete repository interface (16 methods)
  - Read: GetByIdAsync, GetDefaultAsync, GetAllActiveAsync, GetByCategoryAsync, GetCategoriesAsync, SearchAsync, NameExistsAsync, GetByNameAsync, GetUserPromptsAsync, GetBuiltInPromptsAsync
  - Write: CreateAsync, UpdateAsync, DeleteAsync (soft), HardDeleteAsync (with built-in protection)
  - Actions: SetAsDefaultAsync (atomic), IncrementUsageCountAsync, RestoreAsync
- SystemPromptRepository: Full implementation with soft-delete and built-in protection
- SystemPromptConfiguration: EF Core configuration with 6 indexes

### Changed

- SystemPromptEntity: Added TagsJson (JSON-serialized tags array) and Conversations navigation property
- ValidationResult: Enhanced with FirstError property and GetAllErrors(separator) method

## [0.2.3e] - 2026-01-13

Settings panel UI with preset selector and MainWindow integration. See [detailed notes](docs/changelog/v0.2.3e.md).

### Added

- InferenceSettingsPanel: Complete settings panel UserControl
  - Collapsible header with expand toggle, reset, and save buttons
  - Preset ComboBox with custom item template (name, summary, badges)
  - 4 primary ParameterSlider controls (Temperature, TopP, MaxTokens, ContextSize)
  - Advanced Expander with RepetitionPenalty and TopK sliders
  - Error message banner with conditional visibility
- SavePresetDialog: Static dialog helper for saving custom presets
  - Programmatic UI construction following existing dialog pattern
  - Name validation (required) and optional description
  - Exhaustive logging with [ENTER]/[INFO]/[EXIT] markers
- BoolToFontWeightConverter: Singleton converter for default preset emphasis
- ExpandIconConverter: Singleton converter for expand/collapse chevron icons
- Theme resources: PanelBackgroundColor, WarningBackground, WarningForeground
- Icons: ResetIcon, SaveIcon, SettingsIcon

### Changed

- MainWindow.axaml: Added InferenceSettingsPanel to sidebar, updated status bar with preset/temperature indicators
- MainWindowViewModel: Added InferenceSettingsViewModel property and initialization
- LlmService: Uses IInferenceSettingsService.CurrentSettings for inference parameters (Temperature, TopP, TopK, RepetitionPenalty)
- ServiceCollectionExtensions: Updated LlmService DI registration with IInferenceSettingsService

## [0.2.3d] - 2026-01-13

Parameter slider control for inference settings UI. See [detailed notes](docs/changelog/v0.2.3d.md).

### Added

- ParameterSlider: Custom templated control for inference parameters
  - 10 styled properties (Label, Value, Min, Max, Step, Description, ValueFormat, Unit, ShowDescription, IsInteger)
  - FormattedValue computed property with format string and unit suffix support
  - Value coercion clamping to Min/Max range
  - Keyboard navigation (Arrow±Step, Shift+Arrow±Step×10, Home/End)
  - PART_Slider template part for inner slider access
  - 3-row Grid layout (Label+Badge, Slider, Description)
- ParameterSlider.axaml: Control theme with dynamic resource bindings
  - Monospace font for value badge (prevents layout shift)

### Changed

- App.axaml: Added ResourceInclude for ParameterSlider.axaml

## [0.2.3c] - 2026-01-13

ViewModels for inference settings panel. See [detailed notes](docs/changelog/v0.2.3c.md).

### Added

- InferenceSettingsViewModel: Full ViewModel for parameter sliders with:
  - 7 parameter properties (Temperature, TopP, TopK, MaxTokens, ContextSize, RepetitionPenalty, Seed)
  - 7 computed description properties with context-aware text
  - 200ms debounce timer for batched service updates
  - Feedback loop prevention via `_isUpdatingFromService` flag
  - 10 commands for preset and UI operations
  - IDisposable for proper event cleanup
- InferencePresetViewModel: Lightweight wrapper for preset display with:
  - All preset properties (Id, Name, Description, Category, IsBuiltIn, IsDefault)
  - Computed ParameterSummary and TypeIndicator
- DI registration for InferenceSettingsViewModel (singleton)

## [0.2.3b] - 2026-01-13

Inference settings service with event-driven architecture. See [detailed notes](docs/changelog/v0.2.3b.md).

### Added

- IInferenceSettingsService: Interface for centralized settings management with preset support
- InferenceSettingsChangedEventArgs: Event args with ChangeType (ParameterChanged, PresetApplied, ResetToDefaults, AllChanged) and ChangedParameter
- PresetChangedEventArgs: Event args for preset CRUD operations (Applied, Created, Updated, Deleted, DefaultChanged)
- InferenceSettingsService: Full implementation (~750 lines) with:
  - Parameter updates with clamping and change detection
  - Preset operations (apply, save, update, delete, reset, set default)
  - Thread-safe async operations via SemaphoreSlim
  - Float epsilon comparison (0.001) for change detection
  - HasUnsavedChanges property for tracking modifications
  - InitializeAsync for startup preset restoration
- DI registration for IInferenceSettingsService (singleton with scoped repository factory)

### Changed

- AppSettings: Added ActivePresetId (Guid?) for preset persistence across sessions

## [0.2.3a] - 2026-01-13

Inference parameter models and repository enhancements. See [detailed notes](docs/changelog/v0.2.3a.md).

### Added

- InferenceSettings: Value object with 7 parameters (Temperature, TopP, TopK, RepetitionPenalty, MaxTokens, ContextSize, Seed), Clone(), Validate()
- InferencePreset: Domain model with well-known preset IDs, Options property, ToEntity()/FromEntity() mapping
- ParameterConstants: Static Min/Max/Default/Step for all 7 parameters
- ValidationResult: Record type for validation results with Success/Failure factory methods
- GetByCategoryAsync: Repository method for filtering presets by category
- IncrementUsageAsync: Repository method for usage tracking
- SeedBuiltInPresetsAsync: Repository method for idempotent preset seeding
- "Code Review" preset: 5th built-in preset optimized for code analysis

### Changed

- InferencePresetEntity: Added Seed, Category, UsageCount properties
- InferencePresetConfiguration: Added EF Core configuration for new columns and Category index
- DatabaseInitializer: All presets now use well-known GUIDs and include Category assignments
- InferencePresetRepository.DuplicateAsync: Now copies Category and Seed, resets UsageCount to 0

## [0.2.2e] - 2026-01-13

Polish & edge cases. See [detailed notes](docs/changelog/v0.2.2e.md).

### Added

- UnsavedChangesDialog: Save/Don't Save/Cancel prompt before close or navigation
- DeleteConfirmationDialog: Confirmation before conversation deletion
- Keyboard shortcuts: Ctrl+S (save), F2 (rename selected)
- Lazy loading: LoadConversationLazyAsync, LoadMoreMessagesAsync for large conversations
- GetMessagesPagedAsync: 1-indexed pagination for message retrieval
- Destructive button style (.destructive class) in Dark.axaml

### Changed

- MainWindow.axaml.cs: OnClosing handler for unsaved changes, OnKeyDown for F2
- ConversationListViewModel: Delete confirmation dialog integration
- ChatViewModel: SaveCommand, ClearUnsavedChangesFlag method
- Conversation.cs: HasMoreMessages, TotalMessageCount, PrependMessages

## [0.2.2d] - 2026-01-13

Chat integration with conversation persistence. See [detailed notes](docs/changelog/v0.2.2d.md).

### Changed

- ChatViewModel: Event subscriptions to IConversationService, RefreshFromConversation, IDisposable
- ChatMessageViewModel: Performance stats (TokenCount, GenerationTime, TokensPerSecond)
- MainWindowViewModel: ConversationListViewModel composition, InitializeAsync, sidebar toggle
- MainWindow.axaml: SplitView layout replacing Grid, ConversationListView integration
- MainWindow.axaml.cs: OnOpened initialization for async startup

### Added

- Icons.axaml: MenuIcon for sidebar toggle button
- Keyboard shortcuts: Ctrl+N (new), Ctrl+B (toggle sidebar), Ctrl+F (search focus)
- Save status indicator in status bar

## [0.2.2c] - 2026-01-13

Conversation list UI implementation. See [detailed notes](docs/changelog/v0.2.2c.md).

### Added

- ConversationListView.axaml with sidebar layout (header, search, grouped list)
- ConversationListView.axaml.cs code-behind with keyboard/pointer handlers
- PinTextConverter for dynamic "Pin"/"Unpin" context menu text
- BoolToSelectedConverter for selection styling via Tag attribute
- Icons.axaml resource dictionary with 10 StreamGeometry icons
- Dark.axaml additions: 7 color brushes, 6 new ControlThemes

## [0.2.2b] - 2026-01-12

ViewModel layer for conversation persistence. See [detailed notes](docs/changelog/v0.2.2b.md).

### Added

- IDispatcher interface for testable UI thread dispatching
- AvaloniaDispatcher implementation with exhaustive logging
- ConversationSummaryViewModel with RelativeTime formatting
- ConversationGroupViewModel with date-based grouping
- ConversationListViewModel with 10 commands and 300ms debounced search
- DI registration for dispatcher and ViewModels

## [0.2.2a] - 2026-01-12

Service layer for conversation persistence. See [detailed notes](docs/changelog/v0.2.2a.md).

### Added

- DatabaseConversationService with auto-save (500ms debounce)
- Domain model persistence tracking (IsPersisted, HasUnsavedChanges)
- ConversationSummary and DateGroup for sidebar display
- Event-driven UI synchronization (3 events)
- Message SequenceNumber for ordering

### Removed

- ConversationService (replaced by DatabaseConversationService)

## [0.2.1e] - 2026-01-12

Database initialization and seeding. See [detailed notes](docs/changelog/v0.2.1e.md).

### Added

- DatabaseInitializer for migrations and seeding
- Built-in system prompts (3): Default, Code, Writing Assistant
- Built-in inference presets (4): Balanced, Creative, Precise, Fast
- Automatic backup before migrations
- DataServiceCollectionExtensions for DI registration

## [0.2.1d] - 2026-01-12

Repository layer implementation. See [detailed notes](docs/changelog/v0.2.1d.md).

### Added

- IConversationRepository with message-level operations
- ISystemPromptRepository with category filtering
- IInferencePresetRepository with default management
- Repository implementations with exhaustive logging
- 29 unit tests for repository layer

## [0.2.1c] - 2026-01-12

DbContext and Entity Framework Core configurations. See [detailed notes](docs/changelog/v0.2.1c.md).

### Added

- AInternDbContext with 4 DbSet properties and automatic timestamp management
- Entity configurations for Conversation, Message, SystemPrompt, InferencePreset
- 19 database indexes for query optimization
- Unit tests for DbContext and configurations (22 tests)
- Feature documentation (docs/features/dbcontext.md)


## [0.2.1b] - 2026-01-12

Entity classes for database persistence. See [detailed notes](docs/changelog/v0.2.1b.md).

### Added

- Entity classes: ConversationEntity, MessageEntity, SystemPromptEntity, InferencePresetEntity
- Unit tests for entities (26 tests)
- Feature documentation (docs/features/entities.md)

## [0.2.1a] - 2026-01-12

### Added

- AIntern.Data project with Entity Framework Core SQLite
- DatabasePathResolver for cross-platform path resolution (Windows, macOS, Linux)
- XDG Base Directory Specification compliance on Linux
- AIntern.Data.Tests project with 33 unit tests

### Changed

- Updated target framework from .NET 8.0 to .NET 9.0
- Added AIntern.Data reference to Services and Desktop projects
- Added Entity Framework Core packages to central package management

## [0.1.0] - 2026-01-11

### Added

- Initial project setup with Avalonia UI desktop application
- AIntern.Core library with base models (ChatMessage, Conversation)
- AIntern.Services library with LLamaSharp integration
- Basic MVVM architecture with CommunityToolkit.Mvvm
- Serilog logging infrastructure
- Central package management via Directory.Packages.props
- Unit test projects for Core and Services
