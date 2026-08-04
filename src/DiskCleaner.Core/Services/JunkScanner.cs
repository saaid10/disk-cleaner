using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Rule-based scanning for the junk categories in requirements.txt 3g. Protected
/// folders/Safe Haven (3c) are excluded from every scan, not just from deletion.
/// </summary>
public sealed class JunkScanner
{
    private static readonly HashSet<string> DevBuildFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", "obj", "bin", "dist", ".next", "target",
    };

    private readonly PathProtectionService _protection;

    public JunkScanner(PathProtectionService protection) => _protection = protection;

    /// <summary>
    /// Scans a fixed list of known paths (browser cache, Windows temp, Windows Update
    /// leftovers) as single aggregate results - each existing path becomes one result
    /// sized by everything under it.
    /// </summary>
    public IReadOnlyList<JunkScanResult> ScanKnownLocations(IEnumerable<string> paths, JunkCategory category)
    {
        var results = new List<JunkScanResult>();
        foreach (var path in paths)
        {
            if (_protection.IsProtected(path))
            {
                continue;
            }

            var size = ComputeSize(path);
            if (size > 0)
            {
                results.Add(new JunkScanResult(path, category, size, LastModifiedUtc: null, IsAggregateLocation: true));
            }
        }

        return results;
    }

    /// <summary>
    /// Finds node_modules/obj/bin/dist/.next/target folders under the given scan
    /// roots (requirements.txt 3h). Does not descend into a matched folder - no need
    /// to look for dev-build junk inside dev-build junk.
    /// </summary>
    public IReadOnlyList<JunkScanResult> ScanDevBuildFolders(IEnumerable<string> scanRoots)
    {
        var results = new List<JunkScanResult>();
        foreach (var root in scanRoots)
        {
            foreach (var match in FindMatchingFolders(root, DevBuildFolderNames))
            {
                results.Add(new JunkScanResult(match, JunkCategory.DevBuildFolders, ComputeSize(match)));
            }
        }

        return results;
    }

    /// <summary>
    /// Finds *.log files under the given scan roots older than <paramref name="olderThanDays"/>.
    /// </summary>
    public IReadOnlyList<JunkScanResult> ScanLogFiles(IEnumerable<string> scanRoots, int olderThanDays, DateTime? nowUtc = null)
    {
        var cutoff = (nowUtc ?? DateTime.UtcNow).AddDays(-olderThanDays);
        var results = new List<JunkScanResult>();

        foreach (var root in scanRoots)
        {
            foreach (var file in FileSystemWalker.EnumerateFilesSafely(root, "*.log"))
            {
                if (_protection.IsProtected(file))
                {
                    continue;
                }

                var info = new FileInfo(file);
                if (info.LastWriteTimeUtc <= cutoff)
                {
                    results.Add(new JunkScanResult(file, JunkCategory.LogFiles, info.Length));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Lists files sitting directly in the Downloads folder (not recursive - browsers
    /// rarely create subfolders there, and anything the user already organized into a
    /// subfolder is treated as intentional, same as the Desktop Organizer's rule for
    /// existing subfolders). Each file is its own result (not aggregated) with
    /// LastModifiedUtc populated, since Downloads review is inherently per-file and
    /// age is the main signal for "do I still need this."
    /// </summary>
    public IReadOnlyList<JunkScanResult> ScanDownloadsFolder(string downloadsPath)
    {
        var results = new List<JunkScanResult>();
        if (!Directory.Exists(downloadsPath) || _protection.IsProtected(downloadsPath))
        {
            return results;
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(downloadsPath);
        }
        catch (UnauthorizedAccessException)
        {
            return results;
        }

        foreach (var file in files)
        {
            if (_protection.IsProtected(file))
            {
                continue;
            }

            FileInfo info;
            try
            {
                info = new FileInfo(file);
            }
            catch (IOException)
            {
                continue;
            }

            results.Add(new JunkScanResult(file, JunkCategory.Downloads, info.Length, info.LastWriteTimeUtc));
        }

        return results;
    }

    private long ComputeSize(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).Length;
        }

        if (!Directory.Exists(path))
        {
            return 0;
        }

        return FileSystemWalker.EnumerateFilesSafely(path).Sum(f => new FileInfo(f).Length);
    }

    private IEnumerable<string> FindMatchingFolders(string root, HashSet<string> names)
    {
        if (!Directory.Exists(root) || _protection.IsProtected(root))
        {
            yield break;
        }

        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(current);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var child in children)
            {
                if (_protection.IsProtected(child))
                {
                    continue;
                }

                if (names.Contains(Path.GetFileName(child)))
                {
                    yield return child; // matched - don't descend further into it
                }
                else
                {
                    stack.Push(child);
                }
            }
        }
    }

}
