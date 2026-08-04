namespace DiskCleaner.Core.Models;

/// <summary>
/// Per-category totals within a <see cref="CleanupReport"/>.
/// </summary>
public sealed record CleanupCategorySummary(long BytesReclaimed, int ItemCount);

/// <summary>
/// Before/after cleanup summary (requirements.txt 3j): total space reclaimed,
/// category breakdown, and (for a just-completed action) the quarantined items with
/// their 7-day countdown visible via <see cref="QuarantineItem.TimeRemaining"/>.
/// </summary>
public sealed record CleanupReport(
    long TotalBytesReclaimed,
    int TotalItemCount,
    IReadOnlyDictionary<JunkCategory, CleanupCategorySummary> ByCategory,
    IReadOnlyList<QuarantineItem> QuarantinedItems);
