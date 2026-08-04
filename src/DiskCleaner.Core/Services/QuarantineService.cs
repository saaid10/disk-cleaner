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

    public IReadOnlyList<QuarantineItem> GetActiveItems() => _repo.GetActive();

    public QuarantineItem Quarantine(string originalPath, JunkCategory category)
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
