using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Backs the Dashboard's per-drive used/free overview (requirements.txt 3m).
/// Call <see cref="Refresh"/> after any cleanup action so free space reflects
/// reality immediately rather than waiting for the next full scan.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly DriveSpaceService _driveSpaceService;

    public DashboardViewModel(DriveSpaceService driveSpaceService)
    {
        _driveSpaceService = driveSpaceService;
        Refresh();
    }

    public ObservableCollection<DriveCardViewModel> Drives { get; } = new();

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
