using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.App.Views;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

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

    [ObservableProperty]
    private bool _isBusy;

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
    private async Task DeleteSelected()
    {
        var selected = Results.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = "No items selected.";
            return;
        }

        var totalBytes = selected.Sum(r => r.Result.SizeBytes);
        var confirmed = MessageBox.Show(
            $"Quarantine {selected.Count} item(s), reclaiming up to {FileSizeFormatter.Format(totalBytes)}?\n\n"
            + "This is reversible for 7 days via the Quarantine screen.",
            "Confirm Cleanup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirmed != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        StatusText = $"Quarantining {selected.Count} item(s)...";

        // File I/O runs off the UI thread - a Delete Selected on Windows Temp can
        // move hundreds of individual items (via QuarantineFolderContents), which
        // would otherwise freeze the window the same way earlier synchronous scans
        // did before those were fixed.
        var (quarantinedItems, failures, processedRows) = await Task.Run(() => ProcessSelected(selected));

        foreach (var row in processedRows)
        {
            Results.Remove(row);
        }

        IsBusy = false;

        var quarantinedBytes = quarantinedItems.Sum(i => i.SizeBytes);
        StatusText = failures == 0
            ? $"Quarantined {quarantinedItems.Count} item(s), reclaiming {FileSizeFormatter.Format(quarantinedBytes)}."
            : $"Quarantined {quarantinedItems.Count} item(s) ({FileSizeFormatter.Format(quarantinedBytes)}); {failures} item(s) skipped (in use or access denied).";

        if (quarantinedItems.Count > 0)
        {
            var report = CleanupReportBuilder.FromQuarantinedItems(quarantinedItems);
            new CleanupReportWindow(report) { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog();
        }
    }

    private (List<QuarantineItem> QuarantinedItems, int Failures, List<JunkResultRowViewModel> ProcessedRows) ProcessSelected(
        List<JunkResultRowViewModel> selected)
    {
        var quarantinedItems = new List<QuarantineItem>();
        var failures = 0;
        var processedRows = new List<JunkResultRowViewModel>();

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
                    quarantinedItems.AddRange(folderResult.SucceededItems);
                }

                failures += folderResult.FailedCount;
                processedRows.Add(row);
                continue;
            }

            try
            {
                var item = _quarantine.Quarantine(row.Path, row.Category);
                _history.Add(DateTime.UtcNow, row.Category, row.Result.SizeBytes, 1, "Manual cleanup suggestion");
                quarantinedItems.Add(item);
                processedRows.Add(row);
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

        return (quarantinedItems, failures, processedRows);
    }
}
