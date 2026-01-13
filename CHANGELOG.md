# Changelog

All notable changes to the AIntern project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed release notes, see the [docs/changelog/](docs/changelog/) directory.

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
