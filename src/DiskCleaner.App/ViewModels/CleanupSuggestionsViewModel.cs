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
/// queue. Every item requires manual confirm here - whitelisted auto-clean runs
/// separately in the background (3i).
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
    private string _statusText = "Click \"Scan\" to check for junk (browser cache, Windows temp, Windows Update leftovers, and - if scan roots are configured - dev-build folders and old logs).";

    [RelayCommand]
    private void Scan()
    {
        Results.Clear();

        var found = new List<JunkScanResult>();
        found.AddRange(_scanner.ScanKnownLocations(KnownJunkLocations.BrowserCachePaths(), JunkCategory.BrowserCache));
        found.AddRange(_scanner.ScanKnownLocations(KnownJunkLocations.WindowsTempPaths(), JunkCategory.WindowsTemp));
        found.AddRange(_scanner.ScanKnownLocations(KnownJunkLocations.WindowsUpdateLeftoverPaths(), JunkCategory.WindowsUpdateLeftovers));

        var scanRoots = _settings.GetStringSet(ScanScopeSettingsKeys.ScanRoots);
        if (scanRoots.Count > 0)
        {
            found.AddRange(_scanner.ScanDevBuildFolders(scanRoots));
            var logAgeDays = _settings.GetInt(ScanScopeSettingsKeys.LogFileOlderThanDays, ScanScopeSettingsKeys.DefaultLogFileOlderThanDays);
            found.AddRange(_scanner.ScanLogFiles(scanRoots, logAgeDays));
        }

        foreach (var result in found.OrderByDescending(r => r.SizeBytes))
        {
            Results.Add(new JunkResultRowViewModel(result, OnQuarantineRequested));
        }

        StatusText = scanRoots.Count > 0
            ? $"Found {Results.Count} candidates."
            : $"Found {Results.Count} candidates. Add scan roots in Settings to also check dev-build folders and log files.";
    }

    private void OnQuarantineRequested(JunkResultRowViewModel row)
    {
        try
        {
            _quarantine.Quarantine(row.Path, row.Category);
            _history.Add(DateTime.UtcNow, row.Category, row.Result.SizeBytes, 1, "Manual cleanup suggestion");
            StatusText = $"Quarantined {row.Path} ({FileSizeFormatter.Format(row.Result.SizeBytes)}).";
        }
        catch (IOException ex)
        {
            StatusText = $"Could not quarantine {row.Path}: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusText = $"Could not quarantine {row.Path}: {ex.Message}";
        }
    }
}
