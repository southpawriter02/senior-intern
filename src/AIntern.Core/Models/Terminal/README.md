# Terminal Models

This directory contains terminal-related model classes for the AIntern integrated terminal feature (v0.5.x).

## Planned Contents (v0.5.1b)

| File | Description |
|------|-------------|
| `TerminalSize.cs` | Terminal dimensions (columns × rows) |
| `TerminalColor.cs` | RGB/palette/default color representation |
| `TerminalAttributes.cs` | Text styling (bold, italic, underline, etc.) |
| `TerminalCell.cs` | Single cell with character and attributes |
| `TerminalLine.cs` | Row of cells with dirty tracking |
| `TerminalSelection.cs` | Text selection coordinates |
| `TerminalSessionState.cs` | Session lifecycle states |
| `TerminalSession.cs` | Active PTY session management |
| `TerminalBuffer.cs` | Screen buffer with scrollback |

## Namespace

```csharp
namespace AIntern.Core.Models.Terminal;
```

## Related

- Design: `docs/design/v0.5.x/v0.5.1-terminal-foundation.md`
- Parent: `docs/design/v0.5.x/v0.5.0-integrated-terminal.md`
