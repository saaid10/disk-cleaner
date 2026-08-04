namespace DiskCleaner.Core.Services;

/// <summary>
/// Shared recursive file enumeration that skips inaccessible directories instead of
/// throwing (system/permission-locked folders are common when scanning whole drives).
/// Used by JunkScanner, DuplicateFileFinder, LargeOldFileFinder, and
/// DirectoryUsageService.
/// </summary>
internal static class FileSystemWalker
{
    public static IEnumerable<string> EnumerateFilesSafely(string root, string searchPattern = "*")
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

    /// <summary>
    /// Lists every directory under (and including) root, skipping subtrees it can't
    /// access instead of aborting - unlike Directory.EnumerateDirectories(root, "*",
    /// SearchOption.AllDirectories), which throws on the FIRST inaccessible directory
    /// anywhere in the whole tree (guaranteed to happen somewhere under a real
    /// C:\Windows). This is deliberately cheap - it lists directory names only, never
    /// touches file sizes, so it's fast even for trees with tens of thousands of dirs.
    /// </summary>
    public static IReadOnlyList<string> EnumerateDirectoriesRecursivelySafely(string root)
    {
        var result = new List<string> { root };
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
                result.Add(child);
                stack.Push(child);
            }
        }

        return result;
    }

    /// <summary>Total size of files directly inside dir (not recursive) - skips what it can't read instead of throwing.</summary>
    public static long SumFilesInDirectorySafely(string dir)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(dir);
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
        catch (IOException)
        {
            return 0;
        }

        long sum = 0;
        foreach (var file in files)
        {
            try
            {
                sum += new FileInfo(file).Length;
            }
            catch (IOException)
            {
            }
        }

        return sum;
    }
}
