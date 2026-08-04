using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Cleanup Suggestions screen (requirements.txt 3k): categorized junk-scan review
/// queue, including the Downloads folder (per-file, always manual - Downloads can
/// contain anything so it's never whitelist-eligible). Checkbox multi-select: the
/// user checks whichever rows they want, then confirms with a single bulk
/// "Delete Selected" action - nothing is quarantined on a per-row click.
/// </summary>
public sealed partial class CleanupSuggestionsViewModel : ObservableObject
{
    private readonly JunkScanner _scanner;
    private readonly QuarantineService _quarantine;
    private readonly SettingsRepository _settings;
    private readonly CleanupHistoryRepository _history;

    public CleanupSuggestionsViewModel(
        JunkScanner scanner,
        QuarantineService quarantine,
        SettingsRepository settings,
        CleanupHistoryRepository history)
    {
        _scanner = scanner;
        _quarantine = quarantine;
        _settings = settings;
        _history = history;
    }

    public ObservableCollection<JunkResultRowViewModel> Results { get; } = new();

    [ObservableProperty]
    private string _statusText = "Click \"Scan\" to check for junk (browser cache, Windows temp, Downloads, Windows Update leftovers, and - if scan roots are configured - dev-build folders and old logs).";

    [RelayCommand]
    private void Scan()
    {
        Results.Clear();

        var found = new List<JunkScanResult>();
        found.AddRange(_scanner.ScanKnownLocations(KnownJunkLocations.BrowserCachePaths(), JunkCategory.BrowserCache));
        found.AddRange(_scanner.ScanKnownLocations(KnownJunkLocations.WindowsTempPaths(), JunkCategory.WindowsTemp));
        found.AddRange(_scanner.ScanKnownLocations(KnownJunkLocations.WindowsUpdateLeftoverPaths(), JunkCategory.WindowsUpdateLeftovers));
        found.AddRange(_scanner.ScanDownloadsFolder(KnownJunkLocations.DownloadsFolderPath()));

        var scanRoots = _settings.GetStringSet(ScanScopeSettingsKeys.ScanRoots);
        if (scanRoots.Count > 0)
        {
            found.AddRange(_scanner.ScanDevBuildFolders(scanRoots));
            var logAgeDays = _settings.GetInt(ScanScopeSettingsKeys.LogFileOlderThanDays, ScanScopeSettingsKeys.DefaultLogFileOlderThanDays);
            found.AddRange(_scanner.ScanLogFiles(scanRoots, logAgeDays));
        }

        foreach (var result in found.OrderByDescending(r => r.SizeBytes))
        {
            Results.Add(new JunkResultRowViewModel(result));
        }

        StatusText = scanRoots.Count > 0
            ? $"Found {Results.Count} candidates."
            : $"Found {Results.Count} candidates. Add scan roots in Settings to also check dev-build folders and log files.";
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var row in Results)
        {
            row.IsSelected = true;
        }
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var row in Results)
        {
            row.IsSelected = false;
        }
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        var selected = Results.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = "No items selected.";
            return;
        }

        var quarantinedCount = 0;
        long quarantinedBytes = 0;
        var failures = 0;

        foreach (var row in selected)
        {
            if (row.Result.IsAggregateLocation)
            {
                // Whole-folder categories (browser cache, Windows temp, Windows
                // Update leftovers): quarantine contents item-by-item so a single
                // locked file (very common in Temp/cache folders) only skips itself
                // instead of failing the entire folder.
                var folderResult = _quarantine.QuarantineFolderContents(row.Path, row.Category);
                if (folderResult.SucceededCount > 0)
                {
                    _history.Add(
                        DateTime.UtcNow, row.Category, folderResult.SucceededBytes, folderResult.SucceededCount,
                        "Manual cleanup suggestion (folder contents)");
                }

                quarantinedCount += folderResult.SucceededCount;
                quarantinedBytes += folderResult.SucceededBytes;
                failures += folderResult.FailedCount;
                Results.Remove(row);
                continue;
            }

            try
            {
                _quarantine.Quarantine(row.Path, row.Category);
                _history.Add(DateTime.UtcNow, row.Category, row.Result.SizeBytes, 1, "Manual cleanup suggestion");
                quarantinedCount++;
                quarantinedBytes += row.Result.SizeBytes;
                Results.Remove(row);
            }
            catch (IOException)
            {
                failures++;
            }
            catch (UnauthorizedAccessException)
            {
                failures++;
            }
        }

        StatusText = failures == 0
            ? $"Quarantined {quarantinedCount} item(s), reclaiming {FileSizeFormatter.Format(quarantinedBytes)}."
            : $"Quarantined {quarantinedCount} item(s) ({FileSizeFormatter.Format(quarantinedBytes)}); {failures} item(s) skipped (in use or access denied).";
    }
}
