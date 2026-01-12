using System.Text.Json;
using AIntern.Core.Models;

namespace AIntern.Core.Entities;

/// <summary>
/// Entity class for persisting recent workspace data to the database.
/// </summary>
public sealed class RecentWorkspaceEntity
{
    public Guid Id { get; set; }

    /// <summary>Optional custom name for the workspace.</summary>
    public string? Name { get; set; }

    /// <summary>Absolute path to the workspace root directory.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>When the workspace was last accessed.</summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>JSON-serialized list of open file paths (relative to RootPath).</summary>
    public string? OpenFilesJson { get; set; }

    /// <summary>Currently active/focused file path (relative to RootPath).</summary>
    public string? ActiveFilePath { get; set; }

    /// <summary>JSON-serialized list of expanded folder paths (relative to RootPath).</summary>
    public string? ExpandedFoldersJson { get; set; }

    /// <summary>Whether this workspace is pinned in the recent list.</summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Converts this entity to a Workspace model.
    /// </summary>
    public Workspace ToWorkspace()
    {
        return new Workspace
        {
            Id = Id,
            Name = Name ?? string.Empty,
            RootPath = RootPath,
            LastAccessedAt = LastAccessedAt,
            OpenFiles = DeserializeStringList(OpenFilesJson),
            ActiveFilePath = ActiveFilePath,
            ExpandedFolders = DeserializeStringList(ExpandedFoldersJson),
            IsPinned = IsPinned
        };
    }

    /// <summary>
    /// Creates a new entity from a Workspace model.
    /// </summary>
    public static RecentWorkspaceEntity FromWorkspace(Workspace workspace)
    {
        return new RecentWorkspaceEntity
        {
            Id = workspace.Id,
            Name = string.IsNullOrWhiteSpace(workspace.Name) ? null : workspace.Name,
            RootPath = workspace.RootPath,
            LastAccessedAt = workspace.LastAccessedAt,
            OpenFilesJson = SerializeStringList(workspace.OpenFiles),
            ActiveFilePath = workspace.ActiveFilePath,
            ExpandedFoldersJson = SerializeStringList(workspace.ExpandedFolders),
            IsPinned = workspace.IsPinned
        };
    }

    /// <summary>
    /// Updates this entity from a Workspace model.
    /// </summary>
    public void UpdateFrom(Workspace workspace)
    {
        Name = string.IsNullOrWhiteSpace(workspace.Name) ? null : workspace.Name;
        LastAccessedAt = workspace.LastAccessedAt;
        OpenFilesJson = SerializeStringList(workspace.OpenFiles);
        ActiveFilePath = workspace.ActiveFilePath;
        ExpandedFoldersJson = SerializeStringList(workspace.ExpandedFolders);
        IsPinned = workspace.IsPinned;
    }

    private static string? SerializeStringList(IReadOnlyList<string>? list)
    {
        if (list is null || list.Count == 0)
            return null;

        return JsonSerializer.Serialize(list);
    }

    private static IReadOnlyList<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
