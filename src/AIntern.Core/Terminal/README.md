# Terminal Parsing

This directory contains ANSI/VT100 escape sequence parsing logic for the AIntern integrated terminal feature (v0.5.x).

## Planned Contents (v0.5.1c)

| File | Description |
|------|-------------|
| `AnsiParserState.cs` | Parser state machine states |
| `AnsiParser.cs` | VT100/ANSI sequence parser implementation |

## Features

The ANSI parser will support:
- **CSI sequences**: Cursor movement, erase, SGR (colors/styles)
- **OSC sequences**: Title changes, hyperlinks
- **Control characters**: BEL, BS, HT, LF, CR
- **SGR codes**: 16 colors, 256 colors, true color (24-bit RGB)
- **Text attributes**: Bold, italic, underline, inverse, strikethrough

## Namespace

```csharp
namespace AIntern.Core.Terminal;
```

## Related

- Design: `docs/design/v0.5.x/v0.5.1-terminal-foundation.md`
