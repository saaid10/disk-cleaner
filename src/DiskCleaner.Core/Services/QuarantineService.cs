using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Generic move-to-quarantine/restore/purge service (requirements.txt 3b). Every
/// delete action in the app - whitelisted auto-clean, junk suggestions, duplicates,
/// large/old files - routes through here rather than deleting directly. Nothing is
/// permanently gone until <see cref="PurgeExpired"/> reaps it, 7 days after quarantine.
/// </summary>
public sealed class QuarantineService
{
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);

    private readonly QuarantineRepository _repo;
    private readonly string _quarantineRoot;
    private readonly Func<DateTime> _clock;

    public QuarantineService(QuarantineRepository repo, string quarantineRoot, Func<DateTime>? clock = null)
    {
        _repo = repo;
        _quarantineRoot = quarantineRoot;
        Directory.CreateDirectory(_quarantineRoot);
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    /// <summary>
    /// Fired once per Quarantine/QuarantineFolderContents/Restore/PurgeExpired call
    /// that actually changed something - lets Dashboard/Quarantine screens
    /// auto-refresh instead of requiring a manual Refresh click. May fire from a
    /// background thread (bulk quarantine runs off the UI thread) - subscribers must
    /// marshal back to the UI thread themselves.
    /// </summary>
    public event Action? Changed;

    /// <summary>Where quarantined files physically live - exposed so an elevated helper process can move directly into it.</summary>
    public string QuarantineRoot => _quarantineRoot;

    public IReadOnlyList<QuarantineItem> GetActiveItems() => _repo.GetActive();

    /// <summary>
    /// Records a quarantine entry for a file that was ALREADY moved into the
    /// quarantine root by something else - specifically, an elevated helper process
    /// (requirements.txt 2) that moved a Windows Update leftover a standard-user
    /// process couldn't touch. This service still owns the database bookkeeping;
    /// only the raw file move happened elsewhere.
    /// </summary>
    public QuarantineItem RecordExternallyMovedItem(string originalPath, string quarantinePath, JunkCategory category, long sizeBytes)
    {
        var now = _clock();
        var expiresAt = now.Add(RetentionPeriod);
        var id = _repo.Insert(originalPath, quarantinePath, category, sizeBytes, now, expiresAt);
        var item = new QuarantineItem(id, originalPath, quarantinePath, category, sizeBytes, now, expiresAt, Restored: false, Purged: false);
        Changed?.Invoke();
        return item;
    }

    public QuarantineItem Quarantine(string originalPath, JunkCategory category)
    {
        var item = QuarantineInternal(originalPath, category);
        Changed?.Invoke();
        return item;
    }

    /// <summary>
    /// Quarantines a folder's immediate contents (files and subfolders) one at a
    /// time, instead of moving the whole folder as a single atomic operation. A
    /// folder like Windows Temp or a browser cache almost always has at least one
    /// file locked by a running process - moving the whole folder in one shot fails
    /// entirely the moment any single item is locked, even though the rest is
    /// perfectly safe to quarantine. This skips locked items individually and
    /// quarantines everything else.
    /// </summary>
    public FolderQuarantineResult QuarantineFolderContents(string folderPath, JunkCategory category)
    {
        var succeededItems = new List<QuarantineItem>();
        var failedCount = 0;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(folderPath);
        }
        catch (IOException)
        {
            return new FolderQuarantineResult(0, 0, 1, succeededItems);
        }
        catch (UnauthorizedAccessException)
        {
            return new FolderQuarantineResult(0, 0, 1, succeededItems);
        }

        foreach (var entry in entries)
        {
            try
            {
                succeededItems.Add(QuarantineInternal(entry, category));
            }
            catch (IOException)
            {
                failedCount++; // locked/in-use - skip it, not fatal to the rest
            }
            catch (UnauthorizedAccessException)
            {
                failedCount++;
            }
        }

        if (succeededItems.Count > 0)
        {
            Changed?.Invoke(); // fire once for the whole batch, not once per file
        }

        return new FolderQuarantineResult(
            succeededItems.Count,
            succeededItems.Sum(i => i.SizeBytes),
            failedCount,
            succeededItems);
    }

    private QuarantineItem QuarantineInternal(string originalPath, JunkCategory category)
    {
        var size = GetSize(originalPath);
        var quarantinePath = Path.Combine(_quarantineRoot, $"{Guid.NewGuid():N}_{Path.GetFileName(originalPath)}");

        if (Directory.Exists(originalPath))
        {
            Directory.Move(originalPath, quarantinePath);
        }
        else
        {
            File.Move(originalPath, quarantinePath);
        }

        var now = _clock();
        var expiresAt = now.Add(RetentionPeriod);
        var id = _repo.Insert(originalPath, quarantinePath, category, size, now, expiresAt);
        return new QuarantineItem(id, originalPath, quarantinePath, category, size, now, expiresAt, Restored: false, Purged: false);
    }

    public void Restore(long id)
    {
        var item = _repo.GetActive().FirstOrDefault(i => i.Id == id)
            ?? throw new InvalidOperationException($"No active quarantine item with id {id}.");

        if (File.Exists(item.OriginalPath) || Directory.Exists(item.OriginalPath))
        {
            throw new QuarantineRestoreConflictException(item.OriginalPath);
        }

        var destinationDir = Path.GetDirectoryName(item.OriginalPath);
        if (!string.IsNullOrEmpty(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        if (Directory.Exists(item.QuarantinePath))
        {
            Directory.Move(item.QuarantinePath, item.OriginalPath);
        }
        else
        {
            File.Move(item.QuarantinePath, item.OriginalPath);
        }

        _repo.MarkRestored(id);
        Changed?.Invoke();
    }

    /// <summary>
    /// Permanently deletes quarantine items whose 7-day window has passed. Returns
    /// the number of items purged.
    /// </summary>
    public int PurgeExpired()
    {
        var now = _clock();
        var expired = _repo.GetExpiredNotPurged(now);

        foreach (var item in expired)
        {
            if (Directory.Exists(item.QuarantinePath))
            {
                Directory.Delete(item.QuarantinePath, recursive: true);
            }
            else if (File.Exists(item.QuarantinePath))
            {
                File.Delete(item.QuarantinePath);
            }

            _repo.MarkPurged(item.Id);
        }

        if (expired.Count > 0)
        {
            Changed?.Invoke();
        }

        return expired.Count;
    }

    private static long GetSize(string path)
    {
        if (Directory.Exists(path))
        {
            return new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }

        if (File.Exists(path))
        {
            return new FileInfo(path).Length;
        }

        throw new FileNotFoundException("Path does not exist.", path);
    }
}
