namespace AIntern.Core.Entities;

using AIntern.Core.Models;
using System.Text.Json;

/// <summary>
/// Database entity for storing recent workspace information.
/// Persists workspace UI state including open files and folder expansion.
/// </summary>
/// <remarks>
/// <para>
/// This entity stores workspace state for session restoration. When a user
/// reopens a workspace, their previous UI state (open files, expanded folders)
/// can be restored from this entity.
/// </para>
/// <para>
/// List properties (OpenFiles, ExpandedFolders) are serialized as JSON strings
/// to avoid additional join tables while maintaining flexibility.
/// </para>
/// </remarks>
public sealed class RecentWorkspaceEntity
{
    #region Primary Key

    /// <summary>
    /// Gets or sets the unique identifier for the workspace.
    /// </summary>
    public Guid Id { get; set; }

    #endregion

    #region Core Properties

    /// <summary>
    /// Gets or sets an optional custom name for the workspace.
    /// </summary>
    /// <remarks>
    /// If null or empty, the UI should display the folder name from RootPath.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the absolute path to the workspace root directory.
    /// </summary>
    /// <remarks>
    /// This must be unique - a workspace cannot be opened twice.
    /// </remarks>
    public required string RootPath { get; set; }

    /// <summary>
    /// Gets or sets when the workspace was last accessed.
    /// </summary>
    /// <remarks>
    /// Used for sorting recent workspaces by recency.
    /// </remarks>
    public DateTime LastAccessedAt { get; set; }

    #endregion

    #region UI State (JSON Serialized)

    /// <summary>
    /// Gets or sets the JSON-serialized array of open file paths.
    /// </summary>
    /// <remarks>
    /// Paths are relative to RootPath. Example: ["src/file1.cs", "src/file2.cs"]
    /// </remarks>
    public string? OpenFilesJson { get; set; }

    /// <summary>
    /// Gets or sets the currently active file path (relative to RootPath).
    /// </summary>
    public string? ActiveFilePath { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized array of expanded folder paths.
    /// </summary>
    /// <remarks>
    /// Paths are relative to RootPath. Used to restore tree expansion state.
    /// </remarks>
    public string? ExpandedFoldersJson { get; set; }

    #endregion

    #region Flags

    /// <summary>
    /// Gets or sets whether this workspace is pinned in the recent list.
    /// </summary>
    /// <remarks>
    /// Pinned workspaces are not removed when the recent list is trimmed.
    /// </remarks>
    public bool IsPinned { get; set; }

    #endregion

    #region Mapping Methods

    /// <summary>
    /// Converts this entity to a domain model.
    /// </summary>
    /// <returns>A Workspace domain model with deserialized lists.</returns>
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
    /// Creates an entity from a domain model.
    /// </summary>
    /// <param name="workspace">The workspace to convert.</param>
    /// <returns>A new entity with serialized JSON lists.</returns>
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
    /// Updates this entity from a domain model, preserving the Id.
    /// </summary>
    /// <param name="workspace">The workspace with updated values.</param>
    public void UpdateFrom(Workspace workspace)
    {
        Name = string.IsNullOrWhiteSpace(workspace.Name) ? null : workspace.Name;
        LastAccessedAt = workspace.LastAccessedAt;
        OpenFilesJson = SerializeStringList(workspace.OpenFiles);
        ActiveFilePath = workspace.ActiveFilePath;
        ExpandedFoldersJson = SerializeStringList(workspace.ExpandedFolders);
        IsPinned = workspace.IsPinned;
    }

    #endregion

    #region JSON Helpers

    /// <summary>
    /// Serializes a string list to JSON.
    /// </summary>
    private static string? SerializeStringList(IReadOnlyList<string> list)
    {
        if (list.Count == 0) return null;
        return JsonSerializer.Serialize(list);
    }

    /// <summary>
    /// Deserializes a JSON string to a string list.
    /// </summary>
    private static IReadOnlyList<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            // Silently handle corrupted JSON - return empty list
            return [];
        }
    }

    #endregion
}
