# Data Layer Specification

The `AIntern.Data` project contains the data persistence layer including Entity Framework Core configuration, cross-platform path resolution, and database management utilities.

## Overview

This layer depends on `AIntern.Core` and provides:
- **Path Resolution**: Cross-platform database and app data paths
- **Entity Framework Core**: SQLite database configuration (coming in v0.2.1b)
- **Repositories**: Data access abstractions (coming in v0.2.1b)

---

## DatabasePathResolver

Sealed class providing cross-platform file system path resolution for the SQLite database and application data. Follows platform conventions and the XDG Base Directory Specification on Linux.

### Platform Paths

| Platform | App Data Directory |
|----------|-------------------|
| Windows | `%APPDATA%\AIntern\` |
| macOS | `~/Library/Application Support/AIntern/` |
| Linux | `$XDG_DATA_HOME/AIntern/` or `~/.local/share/AIntern/` |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `AppDataDirectory` | `string` | Platform-specific application data directory |
| `DatabasePath` | `string` | Full path to `aintern.db` |
| `LogsDirectory` | `string` | Path to logs subdirectory |
| `BackupsDirectory` | `string` | Path to backups subdirectory |
| `ConnectionString` | `string` | SQLite connection string (`Data Source={path}`) |
| `DatabaseExists` | `bool` | Whether the database file exists |

### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetBackupPath()` | `string` | Generates timestamped backup path |
| `GetLogFilePath(DateTime?)` | `string` | Generates date-based log file path |

### Directory Structure

The resolver automatically creates the following structure:

```
{AppDataDirectory}/
├── aintern.db          # SQLite database
├── logs/               # Application log files
│   └── aintern_{date}.log
└── backups/            # Database backups
    └── aintern_backup_{timestamp}.db
```

### Usage Example

```csharp
// Create resolver (automatically creates directories)
var resolver = new DatabasePathResolver();

// Use with Entity Framework Core
services.AddDbContext<AInternDbContext>(options =>
    options.UseSqlite(resolver.ConnectionString));

// Generate backup path
if (resolver.DatabaseExists)
{
    var backupPath = resolver.GetBackupPath();
    File.Copy(resolver.DatabasePath, backupPath);
}

// Get log file for specific date
var logPath = resolver.GetLogFilePath(DateTime.Today);
```

---

## XDG Base Directory Specification (Linux)

On Linux, the resolver follows the [XDG Base Directory Specification](https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html):

1. **Check `$XDG_DATA_HOME`**: If set, uses `$XDG_DATA_HOME/AIntern`
2. **Fallback to default**: If not set, uses `~/.local/share/AIntern`

This ensures compatibility with standard Linux desktop environments and user expectations.

---

## Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `AppName` | `"AIntern"` | Application folder name |
| `DatabaseFileName` | `"aintern.db"` | SQLite database filename |
| `LogsDirectoryName` | `"logs"` | Logs subdirectory name |
| `BackupsDirectoryName` | `"backups"` | Backups subdirectory name |

---

## Exceptions

| Exception | When Thrown |
|-----------|-------------|
| `PlatformNotSupportedException` | Running on unsupported OS (not Windows, macOS, or Linux) |
| `UnauthorizedAccessException` | No permission to create data directory |
| `IOException` | I/O error creating directories |

---

## Logging

DatabasePathResolver supports comprehensive logging via `Microsoft.Extensions.Logging`:

### Constructors

| Constructor | Description |
|-------------|-------------|
| `DatabasePathResolver()` | Uses `NullLogger` (no logging output) |
| `DatabasePathResolver(ILogger<DatabasePathResolver>)` | Uses provided logger for diagnostic output |

### Log Levels

| Level | Events |
|-------|--------|
| Debug | Platform detection, path resolution details, directory existence checks |
| Information | Resolved paths, directory creation, initialization success |
| Error | Permission denied, I/O errors, unsupported platform |

### Usage with DI

```csharp
// Register with logging
services.AddSingleton<DatabasePathResolver>(sp =>
    new DatabasePathResolver(sp.GetRequiredService<ILogger<DatabasePathResolver>>()));

// Or use parameterless constructor (NullLogger)
services.AddSingleton<DatabasePathResolver>();
```

### Sample Log Output

```
[DBG] Initializing DatabasePathResolver
[DBG] Detected macOS platform
[DBG] macOS data directory resolved using ApplicationData special folder. Base: /Users/user/Library/Application Support, Full: /Users/user/Library/Application Support/AIntern
[INF] Resolved application data directory: /Users/user/Library/Application Support/AIntern
[DBG] Database path: /Users/user/Library/Application Support/AIntern/aintern.db
[DBG] Logs directory: /Users/user/Library/Application Support/AIntern/logs
[DBG] Backups directory: /Users/user/Library/Application Support/AIntern/backups
[DBG] Ensuring required directories exist
[DBG] Application data directory already exists: /Users/user/Library/Application Support/AIntern
[INF] DatabasePathResolver initialized successfully. Database exists: False
```

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore` | 8.0.11 | ORM framework |
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.11 | SQLite database provider |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.11 | Design-time tools for migrations |
| `Microsoft.Extensions.Logging.Abstractions` | 8.0.2 | Logging abstractions |

---

## Coming in Future Releases

### v0.2.1b - Database Configuration

- `AInternDbContext`: Entity Framework Core DbContext
- Entity configurations for `Conversation` and `ChatMessage`
- Database migrations

### v0.2.1c - Repository Pattern

- `IConversationRepository`: Conversation data access
- `IMessageRepository`: Message data access
- Unit of Work pattern implementation
