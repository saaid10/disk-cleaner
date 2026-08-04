using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Display wrapper for CleanupReportWindow (requirements.txt 3j): the before/after
/// summary shown after a bulk "Delete Selected" action completes.
/// </summary>
public sealed class CleanupReportViewModel
{
    public CleanupReportViewModel(CleanupReport report)
    {
        TotalReclaimedDisplay = $"Reclaimed {FileSizeFormatter.Format(report.TotalBytesReclaimed)} across {report.TotalItemCount} item(s).";

        CategoryRows = report.ByCategory
            .OrderByDescending(kv => kv.Value.BytesReclaimed)
            .Select(kv => new CategoryReportRow(kv.Key.ToString(), kv.Value.ItemCount, FileSizeFormatter.Format(kv.Value.BytesReclaimed)))
            .ToList();

        ItemRows = report.QuarantinedItems
            .OrderByDescending(i => i.SizeBytes)
            .Select(i => new ItemReportRow(i.OriginalPath, FileSizeFormatter.Format(i.SizeBytes)))
            .ToList();
    }

    public string TotalReclaimedDisplay { get; }
    public IReadOnlyList<CategoryReportRow> CategoryRows { get; }
    public IReadOnlyList<ItemReportRow> ItemRows { get; }
}

public sealed record CategoryReportRow(string CategoryName, int ItemCount, string BytesDisplay);

public sealed record ItemReportRow(string Path, string SizeDisplay);
