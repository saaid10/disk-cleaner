using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Sortable large/old file listing (requirements.txt 3f). Default suggestion filter
/// is size + staleness combined (>100MB AND not accessed in 90+ days), both
/// user-configurable - sorting itself is a UI concern over the returned list.
/// </summary>
public sealed class LargeOldFileFinder
{
    private readonly PathProtectionService _protection;

    public LargeOldFileFinder(PathProtectionService protection) => _protection = protection;

    /// <summary>
    /// Finds files at or above <paramref name="minSizeBytes"/>. If
    /// <paramref name="minDaysSinceAccess"/> is given, only files whose last-access
    /// time is at least that many days ago are included (the "old" half of the
    /// large+old filter) - omit it to get a plain large-file listing.
    /// Note: NTFS last-access-time tracking is often disabled by default for
    /// performance, so on some systems this can lag reality; it's still the closest
    /// signal available without a separate usage-tracking service.
    /// </summary>
    public IReadOnlyList<LargeFileResult> FindLargeFiles(
        IEnumerable<string> roots,
        long minSizeBytes,
        int? minDaysSinceAccess = null,
        DateTime? nowUtc = null)
    {
        DateTime? cutoff = minDaysSinceAccess.HasValue
            ? (nowUtc ?? DateTime.UtcNow).AddDays(-minDaysSinceAccess.Value)
            : null;

        var results = new List<LargeFileResult>();
        foreach (var root in roots)
        {
            foreach (var file in FileSystemWalker.EnumerateFilesSafely(root))
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

                if (info.Length < minSizeBytes)
                {
                    continue;
                }

                var lastAccessedUtc = info.LastAccessTimeUtc;
                if (cutoff.HasValue && lastAccessedUtc > cutoff.Value)
                {
                    continue;
                }

                results.Add(new LargeFileResult(file, info.Length, lastAccessedUtc));
            }
        }

        return results;
    }
}
