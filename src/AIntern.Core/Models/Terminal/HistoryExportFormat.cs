// ============================================================================
// File: HistoryExportFormat.cs
// Path: src/AIntern.Core/Models/Terminal/HistoryExportFormat.cs
// Description: Enumeration defining the supported export formats for terminal
//              command history data.
// Created: 2026-01-19
// AI Intern v0.5.5i - History Management
// ============================================================================

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Export format options for terminal command history.
/// </summary>
/// <remarks>
/// <para>
/// Defines the supported formats when exporting terminal command history
/// via <c>ITerminalHistoryService.ExportHistoryAsync</c>.
/// </para>
/// <para>
/// <b>Format Comparison:</b>
/// <list type="table">
///   <listheader>
///     <term>Format</term>
///     <description>Use Case</description>
///   </listheader>
///   <item>
///     <term><see cref="Json"/></term>
///     <description>Full metadata, programmatic processing, backup/restore</description>
///   </item>
///   <item>
///     <term><see cref="Csv"/></term>
///     <description>Spreadsheet import, data analysis, reporting</description>
///   </item>
///   <item>
///     <term><see cref="Text"/></term>
///     <description>Quick review, bash history compatibility, scripts</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Typical Usage:</b>
/// <code>
/// await historyService.ExportHistoryAsync(
///     filePath: "/home/user/history.json",
///     format: HistoryExportFormat.Json);
/// </code>
/// </para>
/// <para>Added in v0.5.5i.</para>
/// </remarks>
public enum HistoryExportFormat
{
    /// <summary>
    /// JSON format with full metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exports all history entries as a JSON array with full metadata:
    /// Id, Command, ExecutedAt, WorkingDirectory, ExitCode, Duration, ProfileId.
    /// </para>
    /// <para>
    /// The output is formatted with indentation for readability.
    /// Property names use camelCase convention.
    /// </para>
    /// <para>
    /// <b>Example output:</b>
    /// <code>
    /// [
    ///   {
    ///     "id": "550e8400-e29b-41d4-a716-446655440000",
    ///     "command": "git status",
    ///     "executedAt": "2026-01-19T14:30:00Z",
    ///     "workingDirectory": "/home/user/project",
    ///     "exitCode": 0,
    ///     "durationMs": 150.5,
    ///     "profileId": "bash-default"
    ///   }
    /// ]
    /// </code>
    /// </para>
    /// </remarks>
    Json,

    /// <summary>
    /// CSV format for spreadsheet import.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exports as comma-separated values with a header row.
    /// Suitable for import into Excel, Google Sheets, or data analysis tools.
    /// </para>
    /// <para>
    /// <b>Columns:</b> Command, ExecutedAt, WorkingDirectory, ExitCode, DurationMs
    /// </para>
    /// <para>
    /// Commands containing commas or quotes are properly escaped:
    /// <list type="bullet">
    ///   <item>Values containing commas are wrapped in double quotes</item>
    ///   <item>Double quotes within values are escaped by doubling them</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Example output:</b>
    /// <code>
    /// Command,ExecutedAt,WorkingDirectory,ExitCode,DurationMs
    /// "git status",2026-01-19T14:30:00Z,"/home/user/project",0,150
    /// "echo ""hello""",2026-01-19T14:31:00Z,"/home/user",0,5
    /// </code>
    /// </para>
    /// </remarks>
    Csv,

    /// <summary>
    /// Plain text with one command per line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exports commands only, one per line, without metadata.
    /// Compatible with standard shell history formats.
    /// </para>
    /// <para>
    /// Useful for:
    /// <list type="bullet">
    ///   <item>Quick review of command history</item>
    ///   <item>Piping to shell scripts</item>
    ///   <item>Compatibility with bash/zsh history files</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Example output:</b>
    /// <code>
    /// git status
    /// npm install
    /// dotnet build
    /// </code>
    /// </para>
    /// </remarks>
    Text
}
