using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Computes the immediate children of a folder (subfolders sized recursively, files
/// sized directly) for the treemap view (requirements.txt 3f) to lay out and let the
/// user drill into.
/// </summary>
public sealed class DirectoryUsageService
{
    private readonly PathProtectionService _protection;

    public DirectoryUsageService(PathProtectionService protection) => _protection = protection;

    public IReadOnlyList<TreemapNode> GetChildNodes(string folderPath)
    {
        var nodes = new List<TreemapNode>();
        if (!Directory.Exists(folderPath))
        {
            return nodes;
        }

        IEnumerable<string> subdirectories;
        try
        {
            subdirectories = Directory.EnumerateDirectories(folderPath);
        }
        catch (UnauthorizedAccessException)
        {
            subdirectories = Enumerable.Empty<string>();
        }

        foreach (var dir in subdirectories)
        {
            if (_protection.IsProtected(dir))
            {
                continue;
            }

            var size = FileSystemWalker.EnumerateFilesSafely(dir).Sum(SafeFileLength);
            if (size > 0)
            {
                nodes.Add(new TreemapNode(Path.GetFileName(dir), size, FileTypeCategory.Folder));
            }
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(folderPath);
        }
        catch (UnauthorizedAccessException)
        {
            files = Enumerable.Empty<string>();
        }

        foreach (var file in files)
        {
            if (_protection.IsProtected(file))
            {
                continue;
            }

            var size = SafeFileLength(file);
            if (size > 0)
            {
                nodes.Add(new TreemapNode(Path.GetFileName(file), size, FileTypeCategorizer.Categorize(file)));
            }
        }

        return nodes;
    }

    private static long SafeFileLength(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (IOException)
        {
            return 0;
        }
    }
}
