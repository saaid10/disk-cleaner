using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Computes the immediate children of a folder (subfolders sized recursively, files
/// sized directly) for the treemap view (requirements.txt 3f) to lay out and let the
/// user drill into.
///
/// Getting this fast against a real drive root took a few iterations, live-tested
/// against the real filesystem each time:
///   1. Parallelize only across top-level sibling folders - still 30+ seconds,
///      because one huge folder (e.g. C:\Windows, thousands of nested directories)
///      dominates the total time on its own regardless of its siblings.
///   2. Nest a second parallel layer inside each sibling's own recursive walk -
///      no better (~39s) - two nested parallel operations compete for the same
///      finite thread pool without actually finishing faster.
///   3. One flat parallel pass across every leaf directory combined, but
///      accumulating each top-level folder's running total via a
///      ConcurrentDictionary keyed by that folder - WORSE (~63s). Nearly all of
///      C:\Windows's tens of thousands of leaf directories hammer the SAME
///      dictionary key concurrently; ConcurrentDictionary's compare-and-swap
///      retry loop under that much contention on one key is slower than no
///      sharing at all.
///   4. This version: (a) discover each top-level folder's full subtree in
///      parallel too (not sequentially before any work starts), and (b) accumulate
///      sizes with plain Interlocked operations on a pre-sized array (indexed by
///      top-level folder), which is a single atomic CPU instruction per update
///      instead of a hashed, retrying dictionary operation.
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

        // Discover each top-level folder's full subtree (directory names only, no
        // file stats) IN PARALLEL - doing this sequentially, one top-level folder at
        // a time, meant a huge folder's own discovery blocked starting work on any
        // of its siblings.
        var subtreesByTop = new IReadOnlyList<string>[topLevelDirs.Count];
        Parallel.For(0, topLevelDirs.Count, i =>
        {
            subtreesByTop[i] = FileSystemWalker.EnumerateDirectoriesRecursivelySafely(topLevelDirs[i]);
        });

        var workItems = new List<(int TopIndex, string Leaf)>();
        for (var i = 0; i < topLevelDirs.Count; i++)
        {
            foreach (var leaf in subtreesByTop[i])
            {
                workItems.Add((i, leaf));
            }
        }

        var totals = new long[topLevelDirs.Count];
        var remaining = new int[topLevelDirs.Count];
        foreach (var item in workItems)
        {
            remaining[item.TopIndex]++;
        }

        Parallel.ForEach(workItems, item =>
        {
            var size = FileSystemWalker.SumFilesInDirectorySafely(item.Leaf);
            if (size > 0)
            {
                Interlocked.Add(ref totals[item.TopIndex], size);
            }

            if (Interlocked.Decrement(ref remaining[item.TopIndex]) == 0)
            {
                var finalSize = Interlocked.Read(ref totals[item.TopIndex]);
                if (finalSize > 0)
                {
                    onSubfolderReady?.Invoke(new TreemapNode(Path.GetFileName(topLevelDirs[item.TopIndex]), finalSize, FileTypeCategory.Folder));
                }
            }
        });

        var results = new List<TreemapNode>();
        for (var i = 0; i < topLevelDirs.Count; i++)
        {
            if (totals[i] > 0)
            {
                results.Add(new TreemapNode(Path.GetFileName(topLevelDirs[i]), totals[i], FileTypeCategory.Folder));
            }
        }

        return results;
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
