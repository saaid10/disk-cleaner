namespace DiskCleaner.Core.Services;

/// <summary>
/// Shared recursive file enumeration that skips inaccessible directories instead of
/// throwing (system/permission-locked folders are common when scanning whole drives).
/// Used by JunkScanner, DuplicateFileFinder, and LargeOldFileFinder.
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
}
