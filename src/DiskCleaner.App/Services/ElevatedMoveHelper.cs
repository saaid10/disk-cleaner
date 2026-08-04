using System.Diagnostics;
using System.IO;

namespace DiskCleaner.App.Services;

/// <summary>
/// Handles the "needs admin rights" case for Windows Update leftovers
/// (requirements.txt 2/3b): a standard-user process can't move
/// Windows.old/SoftwareDistribution contents. Relaunches this same exe with a
/// special command-line mode (--elevated-move-folder) via a UAC prompt (Verb=runas);
/// that headless instance does ONLY the raw file moves and writes results to a temp
/// file, then exits. The main process stays unelevated and owns all database
/// bookkeeping (via QuarantineService.RecordExternallyMovedItem) - elevation is
/// scoped to the minimum needed (file I/O), not the whole app.
/// </summary>
public static class ElevatedMoveHelper
{
    public const string ElevatedModeArg = "--elevated-move-folder";

    public sealed record MovedEntry(string OriginalPath, string QuarantinePath, long SizeBytes);

    /// <summary>
    /// Runs from the main (unelevated) process. Launches an elevated copy of this
    /// exe to move <paramref name="sourceFolder"/>'s contents into the quarantine
    /// root, waits for it to finish, and returns whatever it reports it moved.
    /// Returns an empty list if the user cancels the UAC prompt or elevation fails.
    /// </summary>
    public static IReadOnlyList<MovedEntry> RetryFolderElevated(string sourceFolder)
    {
        var resultFilePath = Path.Combine(Path.GetTempPath(), $"dc-elevated-result-{Guid.NewGuid():N}.txt");
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
        {
            return Array.Empty<MovedEntry>();
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"{ElevatedModeArg} \"{sourceFolder}\" \"{resultFilePath}\"",
            UseShellExecute = true,
            Verb = "runas",
        };

        try
        {
            using var process = Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User clicked "No" on the UAC prompt, or elevation was otherwise refused.
            return Array.Empty<MovedEntry>();
        }

        if (!File.Exists(resultFilePath))
        {
            return Array.Empty<MovedEntry>();
        }

        var entries = ParseResultFile(File.ReadAllLines(resultFilePath));

        try
        {
            File.Delete(resultFilePath);
        }
        catch (IOException)
        {
        }

        return entries;
    }

    internal static IReadOnlyList<MovedEntry> ParseResultFile(IEnumerable<string> lines)
    {
        var entries = new List<MovedEntry>();
        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length == 3 && long.TryParse(parts[2], out var size))
            {
                entries.Add(new MovedEntry(parts[0], parts[1], size));
            }
        }

        return entries;
    }

    /// <summary>
    /// Runs INSIDE the elevated child process (headless - no window, no tray icon).
    /// Moves every immediate entry of <paramref name="sourceFolder"/> into the
    /// quarantine root and writes "original\tquarantine\tsize" lines to
    /// <paramref name="resultFilePath"/> for the main process to read back.
    /// </summary>
    public static void RunElevatedMoveFolder(string sourceFolder, string resultFilePath)
    {
        var quarantineRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiskCleaner",
            "Quarantine");
        Directory.CreateDirectory(quarantineRoot);

        var resultLines = new List<string>();

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(sourceFolder);
        }
        catch (IOException)
        {
            entries = Array.Empty<string>();
        }
        catch (UnauthorizedAccessException)
        {
            entries = Array.Empty<string>();
        }

        foreach (var entry in entries)
        {
            try
            {
                var isDirectory = Directory.Exists(entry);
                var size = isDirectory
                    ? new DirectoryInfo(entry).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length)
                    : new FileInfo(entry).Length;

                var destination = Path.Combine(quarantineRoot, $"{Guid.NewGuid():N}_{Path.GetFileName(entry)}");

                if (isDirectory)
                {
                    Directory.Move(entry, destination);
                }
                else
                {
                    File.Move(entry, destination);
                }

                resultLines.Add($"{entry}\t{destination}\t{size}");
            }
            catch (IOException)
            {
                // Still couldn't move it even elevated (e.g. genuinely locked by the
                // OS itself) - skip, don't fail the rest.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        File.WriteAllLines(resultFilePath, resultLines);
    }
}
