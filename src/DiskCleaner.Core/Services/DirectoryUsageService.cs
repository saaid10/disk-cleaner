using System.Collections.Concurrent;
using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Computes the immediate children of a folder (subfolders sized recursively, files
/// sized directly) for the treemap view (requirements.txt 3f) to lay out and let the
/// user drill into.
///
/// Sizing every subfolder's full recursive tree runs as ONE FLAT parallel operation
/// across every leaf directory of every subfolder combined - not one parallel
/// operation per subfolder nested inside another. A single huge subfolder (e.g.
/// C:\Windows, with thousands of nested directories) needs its own work spread across
/// every core just as much as sibling folders do; nesting two levels of parallelism
/// (parallelize subfolders, and separately parallelize inside each one) causes thread-
/// pool contention without actually finishing any faster, since the pool is a shared,
/// finite resource regardless of how the work is structured above it.
/// </summary>
public sealed class DirectoryUsageService
{
    private readonly PathProtectionService _protection;

    public DirectoryUsageService(PathProtectionService protection) => _protection = protection;

    /// <summary>
    /// If <paramref name="minSizeBytes"/> is greater than zero, nodes smaller than it
    /// are collapsed into a single "Other (small items)" node instead of each getting
    /// its own rectangle - keeps a folder with hundreds of small children readable.
    /// If <paramref name="onSubfolderReady"/> is given, it's invoked (from whatever
    /// background thread completed that subfolder) the moment each subfolder's size
    /// is fully known - well before the whole call returns - so the UI can show
    /// results incrementally instead of an all-or-nothing wait.
    /// </summary>
    public IReadOnlyList<TreemapNode> GetChildNodes(
        string folderPath,
        long minSizeBytes = 0,
        Action<TreemapNode>? onSubfolderReady = null)
    {
        if (!Directory.Exists(folderPath))
        {
            return Array.Empty<TreemapNode>();
        }

        var subdirNodes = GetSubdirectoryNodes(folderPath, onSubfolderReady);
        var fileNodes = GetFileNodes(folderPath);
        var allNodes = subdirNodes.Concat(fileNodes).ToList();

        if (minSizeBytes <= 0)
        {
            return allNodes;
        }

        var kept = allNodes.Where(n => n.SizeBytes >= minSizeBytes).ToList();
        var smallTotal = allNodes.Where(n => n.SizeBytes < minSizeBytes).Sum(n => n.SizeBytes);
        if (smallTotal > 0)
        {
            // Category.Other keeps this non-navigable (IsFolder is Folder-category-only) -
            // it's a summary, not a real path to drill into.
            kept.Add(new TreemapNode("Other (small items)", smallTotal, FileTypeCategory.Other));
        }

        return kept;
    }

    private List<TreemapNode> GetSubdirectoryNodes(string folderPath, Action<TreemapNode>? onSubfolderReady)
    {
        IEnumerable<string> subdirectories;
        try
        {
            subdirectories = Directory.EnumerateDirectories(folderPath);
        }
        catch (UnauthorizedAccessException)
        {
            subdirectories = Enumerable.Empty<string>();
        }

        var topLevelDirs = subdirectories.Where(dir => !_protection.IsProtected(dir)).ToList();
        if (topLevelDirs.Count == 0)
        {
            return new List<TreemapNode>();
        }

        // Cheap pass: list every nested directory under each top-level folder (names
        // only, no file stats yet) so the expensive part below can be one flat
        // parallel pass across the whole tree.
        var workItems = new List<(string Top, string Leaf)>();
        foreach (var top in topLevelDirs)
        {
            foreach (var leaf in FileSystemWalker.EnumerateDirectoriesRecursivelySafely(top))
            {
                workItems.Add((top, leaf));
            }
        }

        var totals = new ConcurrentDictionary<string, long>(topLevelDirs.Select(d => new KeyValuePair<string, long>(d, 0L)));
        var remaining = new ConcurrentDictionary<string, int>(
            workItems.GroupBy(w => w.Top).Select(g => new KeyValuePair<string, int>(g.Key, g.Count())));

        Parallel.ForEach(workItems, item =>
        {
            var size = FileSystemWalker.SumFilesInDirectorySafely(item.Leaf);
            totals.AddOrUpdate(item.Top, size, (_, existing) => existing + size);

            var stillRemaining = remaining.AddOrUpdate(item.Top, 0, (_, count) => count - 1);
            if (stillRemaining == 0)
            {
                var finalSize = totals[item.Top];
                if (finalSize > 0)
                {
                    onSubfolderReady?.Invoke(new TreemapNode(Path.GetFileName(item.Top), finalSize, FileTypeCategory.Folder));
                }
            }
        });

        return topLevelDirs
            .Select(top => new TreemapNode(Path.GetFileName(top), totals[top], FileTypeCategory.Folder))
            .Where(n => n.SizeBytes > 0)
            .ToList();
    }

    private List<TreemapNode> GetFileNodes(string folderPath)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(folderPath);
        }
        catch (UnauthorizedAccessException)
        {
            files = Enumerable.Empty<string>();
        }

        var nodes = new List<TreemapNode>();
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
