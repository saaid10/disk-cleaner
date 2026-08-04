namespace DiskCleaner.Core.Models;

/// <summary>
/// One item to lay out on the treemap (requirements.txt 3f) - a file or folder,
/// colored by its file-type category (shared with the Desktop Organizer, 3d).
/// </summary>
public sealed record TreemapNode(string Label, long SizeBytes, FileTypeCategory Category);
