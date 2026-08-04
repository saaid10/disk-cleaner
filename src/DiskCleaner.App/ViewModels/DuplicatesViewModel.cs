using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Duplicate finder screen (requirements.txt 3e). On-demand, scoped to the user's
/// configured scan roots. Always manual-review - quarantining one copy is a
/// per-file, user-initiated decision.
/// </summary>
public sealed partial class DuplicatesViewModel : ObservableObject
{
    private readonly DuplicateFileFinder _finder;
    private readonly QuarantineService _quarantine;
    private readonly SettingsRepository _settings;
    private readonly CleanupHistoryRepository _history;

    public DuplicatesViewModel(
        DuplicateFileFinder finder,
        QuarantineService quarantine,
        SettingsRepository settings,
        CleanupHistoryRepository history)
    {
        _finder = finder;
        _quarantine = quarantine;
        _settings = settings;
        _history = history;
    }

    public ObservableCollection<DuplicateGroupRowViewModel> Groups { get; } = new();

    [ObservableProperty]
    private string _statusText = "Click \"Scan for Duplicates\" to search your configured scan roots (see Settings).";

    [RelayCommand]
    private void Scan()
    {
        Groups.Clear();
        var scanRoots = _settings.GetStringSet(ScanScopeSettingsKeys.ScanRoots);
        if (scanRoots.Count == 0)
        {
            StatusText = "No scan roots configured. Add folders to scan in Settings first.";
            return;
        }

        var duplicateGroups = _finder.FindDuplicates(scanRoots);
        foreach (var group in duplicateGroups)
        {
            Groups.Add(new DuplicateGroupRowViewModel(group, OnQuarantineRequested));
        }

        StatusText = $"Found {Groups.Count} duplicate group(s).";
    }

    private void OnQuarantineRequested(DuplicateFileRowViewModel row)
    {
        try
        {
            _quarantine.Quarantine(row.Path, JunkCategory.Duplicate);
            _history.Add(DateTime.UtcNow, JunkCategory.Duplicate, row.SizeBytes, 1, "Manual duplicate removal");
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
