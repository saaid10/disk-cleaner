using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Large/Old Files screen (requirements.txt 3f). Default filter is size + staleness
/// combined (>100MB, not accessed in 90+ days), both configurable in Settings.
/// Always manual-review - never routed through whitelist auto-clean.
/// </summary>
public sealed partial class LargeOldFilesViewModel : ObservableObject
{
    private readonly LargeOldFileFinder _finder;
    private readonly QuarantineService _quarantine;
    private readonly SettingsRepository _settings;
    private readonly CleanupHistoryRepository _history;

    public LargeOldFilesViewModel(
        LargeOldFileFinder finder,
        QuarantineService quarantine,
        SettingsRepository settings,
        CleanupHistoryRepository history)
    {
        _finder = finder;
        _quarantine = quarantine;
        _settings = settings;
        _history = history;
    }

    public ObservableCollection<LargeFileRowViewModel> Files { get; } = new();

    [ObservableProperty]
    private string _statusText = "Click \"Scan\" to search your configured scan roots (see Settings).";

    [RelayCommand]
    private void Scan()
    {
        Files.Clear();
        var scanRoots = _settings.GetStringSet(ScanScopeSettingsKeys.ScanRoots);
        if (scanRoots.Count == 0)
        {
            StatusText = "No scan roots configured. Add folders to scan in Settings first.";
            return;
        }

        var minSizeMb = _settings.GetInt(ScanScopeSettingsKeys.LargeFileMinSizeMb, ScanScopeSettingsKeys.DefaultLargeFileMinSizeMb);
        var minSize = minSizeMb * 1024L * 1024L;
        var minDays = _settings.GetInt(ScanScopeSettingsKeys.LargeFileMinDaysSinceAccess, ScanScopeSettingsKeys.DefaultLargeFileMinDaysSinceAccess);

        var results = _finder.FindLargeFiles(scanRoots, minSize, minDays);
        foreach (var result in results.OrderByDescending(r => r.SizeBytes))
        {
            Files.Add(new LargeFileRowViewModel(result, OnQuarantineRequested));
        }

        StatusText = $"Found {Files.Count} file(s).";
    }

    private void OnQuarantineRequested(LargeFileRowViewModel row)
    {
        try
        {
            _quarantine.Quarantine(row.Path, JunkCategory.LargeOldFile);
            _history.Add(DateTime.UtcNow, JunkCategory.LargeOldFile, row.SizeBytes, 1, "Manual large/old file removal");
            StatusText = $"Quarantined {row.Path}.";
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
