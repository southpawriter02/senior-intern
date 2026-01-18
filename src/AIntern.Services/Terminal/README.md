# Terminal Services

This directory contains terminal service implementations for the AIntern integrated terminal feature (v0.5.x).

## Planned Contents

| File | Version | Description |
|------|---------|-------------|
| `TerminalService.cs` | v0.5.1d | PTY session management via Pty.Net |
| `ShellDetectionService.cs` | v0.5.1e | Cross-platform shell detection |

## Dependencies

- **Pty.Net**: Cross-platform pseudo-terminal library
- **System.IO.Pipelines**: High-performance stream handling

## Namespace

```csharp
namespace AIntern.Services.Terminal;
```

## Related

- Interface: `src/AIntern.Core/Interfaces/ITerminalService.cs` (v0.5.1d)
- Design: `docs/design/v0.5.x/v0.5.1-terminal-foundation.md`
