# Changelog

All notable changes to the AIntern project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed release notes, see the [docs/changelog/](docs/changelog/) directory.

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
