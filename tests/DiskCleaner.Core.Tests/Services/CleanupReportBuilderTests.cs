using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class CleanupReportBuilderTests
{
    [Fact]
    public void FromQuarantinedItems_AggregatesTotalsAndByCategory()
    {
        var now = DateTime.UtcNow;
        var items = new[]
        {
            new QuarantineItem(1, @"C:\a.tmp", @"Q:\a.tmp", JunkCategory.WindowsTemp, 100, now, now.AddDays(7), false, false),
            new QuarantineItem(2, @"C:\b.tmp", @"Q:\b.tmp", JunkCategory.WindowsTemp, 50, now, now.AddDays(7), false, false),
            new QuarantineItem(3, @"C:\cache", @"Q:\cache", JunkCategory.BrowserCache, 200, now, now.AddDays(7), false, false),
        };

        var report = CleanupReportBuilder.FromQuarantinedItems(items);

        Assert.Equal(350, report.TotalBytesReclaimed);
        Assert.Equal(3, report.TotalItemCount);
        Assert.Equal(new CleanupCategorySummary(150, 2), report.ByCategory[JunkCategory.WindowsTemp]);
        Assert.Equal(new CleanupCategorySummary(200, 1), report.ByCategory[JunkCategory.BrowserCache]);
        Assert.Equal(3, report.QuarantinedItems.Count);
    }

    [Fact]
    public void FromQuarantinedItems_Empty_ReturnsZeroedReport()
    {
        var report = CleanupReportBuilder.FromQuarantinedItems(Array.Empty<QuarantineItem>());

        Assert.Equal(0, report.TotalBytesReclaimed);
        Assert.Equal(0, report.TotalItemCount);
        Assert.Empty(report.ByCategory);
    }

    [Fact]
    public void FromHistory_AggregatesAcrossMultipleEntries()
    {
        var now = DateTime.UtcNow;
        var entries = new[]
        {
            new CleanupHistoryEntry(1, now.AddDays(-1), JunkCategory.WindowsTemp, 1000, 4, "Auto-clean"),
            new CleanupHistoryEntry(2, now, JunkCategory.WindowsTemp, 500, 2, "Auto-clean"),
            new CleanupHistoryEntry(3, now, JunkCategory.LogFiles, 300, 1, "Auto-clean"),
        };

        var report = CleanupReportBuilder.FromHistory(entries);

        Assert.Equal(1800, report.TotalBytesReclaimed);
        Assert.Equal(7, report.TotalItemCount);
        Assert.Equal(new CleanupCategorySummary(1500, 6), report.ByCategory[JunkCategory.WindowsTemp]);
        Assert.Equal(new CleanupCategorySummary(300, 1), report.ByCategory[JunkCategory.LogFiles]);
        Assert.Empty(report.QuarantinedItems);
    }
}
