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
                results.Add(new JunkScanResult(path, category, size));
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
            foreach (var file in EnumerateFilesSafely(root, "*.log"))
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

        return EnumerateFilesSafely(path, "*").Sum(f => new FileInfo(f).Length);
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

    private static IEnumerable<string> EnumerateFilesSafely(string root, string searchPattern)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(current, searchPattern);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            IEnumerable<string> subdirs;
            try
            {
                subdirs = Directory.EnumerateDirectories(current);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var dir in subdirs)
            {
                stack.Push(dir);
            }
        }
    }
}
