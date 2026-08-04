using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Backs the Dashboard's per-drive used/free overview (requirements.txt 3m).
/// Auto-refreshes whenever QuarantineService reports a change (item quarantined,
/// restored, or purged - from anywhere in the app, including background
/// auto-clean), in addition to the manual Refresh button.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly DriveSpaceService _driveSpaceService;
    private readonly QuarantineService _quarantine;
    private readonly SettingsRepository _settings;

    public DashboardViewModel(DriveSpaceService driveSpaceService, QuarantineService quarantine, SettingsRepository settings)
    {
        _driveSpaceService = driveSpaceService;
        _quarantine = quarantine;
        _settings = settings;
        _quarantine.Changed += OnQuarantineChanged;
        Refresh();
    }

    public ObservableCollection<DriveCardViewModel> Drives { get; } = new();

    // QuarantineService.Changed can fire from a background thread (bulk cleanup runs
    // off the UI thread) - marshal back before touching the UI-bound collection.
    private void OnQuarantineChanged() =>
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(Refresh);

    [RelayCommand]
    private void Refresh()
    {
        var optedInRemovable = _settings.GetStringSet(ScanScopeSettingsKeys.OptedInRemovableDrives);
        Drives.Clear();
        foreach (var drive in _driveSpaceService.GetDrives(optedInRemovable))
        {
            Drives.Add(new DriveCardViewModel(drive));
        }
    }
}
