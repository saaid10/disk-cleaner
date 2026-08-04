using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public DashboardViewModel(DriveSpaceService driveSpaceService, QuarantineService quarantine)
    {
        _driveSpaceService = driveSpaceService;
        _quarantine = quarantine;
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
        Drives.Clear();
        foreach (var drive in _driveSpaceService.GetDrives())
        {
            Drives.Add(new DriveCardViewModel(drive));
        }
    }
}
