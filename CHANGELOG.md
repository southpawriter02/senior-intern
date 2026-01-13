# Changelog

All notable changes to the AIntern project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed release notes, see the [docs/changelog/](docs/changelog/) directory.

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
