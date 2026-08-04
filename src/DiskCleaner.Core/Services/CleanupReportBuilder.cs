using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Builds the before/after summary (requirements.txt 3j) shown after any cleanup
/// action - manual or scheduled auto-clean.
/// </summary>
public static class CleanupReportBuilder
{
    /// <summary>Report for an action that just ran, from the items it quarantined.</summary>
    public static CleanupReport FromQuarantinedItems(IReadOnlyList<QuarantineItem> items)
    {
        var byCategory = items
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => new CleanupCategorySummary(g.Sum(i => i.SizeBytes), g.Count()));

        return new CleanupReport(
            TotalBytesReclaimed: items.Sum(i => i.SizeBytes),
            TotalItemCount: items.Count,
            ByCategory: byCategory,
            QuarantinedItems: items);
    }

    /// <summary>Historical report aggregated from CleanupHistory rows (e.g. "all-time" or "last 30 days").</summary>
    public static CleanupReport FromHistory(IReadOnlyList<CleanupHistoryEntry> entries)
    {
        var byCategory = entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => new CleanupCategorySummary(g.Sum(e => e.BytesReclaimed), g.Sum(e => e.ItemCount)));

        return new CleanupReport(
            TotalBytesReclaimed: entries.Sum(e => e.BytesReclaimed),
            TotalItemCount: entries.Sum(e => e.ItemCount),
            ByCategory: byCategory,
            QuarantinedItems: Array.Empty<QuarantineItem>());
    }
}
