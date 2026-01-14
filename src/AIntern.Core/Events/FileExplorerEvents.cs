namespace AIntern.Core.Events;

/// <summary>
/// Event arguments for file open requests.
/// </summary>
/// <remarks>Added in v0.3.2b.</remarks>
public class FileOpenRequestedEventArgs : EventArgs
{
    /// <summary>Absolute path to the file to open.</summary>
    public string FilePath { get; }

    public FileOpenRequestedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Event arguments for file attach requests (add to chat context).
/// </summary>
/// <remarks>Added in v0.3.2b.</remarks>
public class FileAttachRequestedEventArgs : EventArgs
{
    /// <summary>Absolute path to the file to attach.</summary>
    public string FilePath { get; }

    public FileAttachRequestedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Event arguments for delete confirmation requests.
/// </summary>
/// <remarks>Added in v0.3.2b.</remarks>
public class DeleteConfirmationEventArgs : EventArgs
{
    /// <summary>Path of item to delete.</summary>
    public string Path { get; }

    /// <summary>Whether the item is a directory.</summary>
    public bool IsDirectory { get; }

    /// <summary>Set to true if user confirms deletion.</summary>
    public bool Confirmed { get; set; }

    public DeleteConfirmationEventArgs(string path, bool isDirectory)
    {
        Path = path;
        IsDirectory = isDirectory;
    }
}
