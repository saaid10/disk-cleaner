using CommunityToolkit.Mvvm.ComponentModel;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// One currently-attached removable drive, offered as an opt-in checkbox in
/// Settings (requirements.txt 3h) - excluded from the Dashboard by default to
/// avoid accidentally scanning a plugged-in USB backup drive.
/// </summary>
public sealed partial class RemovableDriveOptionViewModel : ObservableObject
{
    public RemovableDriveOptionViewModel(DriveSpaceInfo drive, bool isEnabled)
    {
        Name = drive.Name;
        DisplayLabel = $"{drive.VolumeLabel} ({drive.Name}) - {FileSizeFormatter.Format(drive.TotalBytes)}";
        _isEnabled = isEnabled;
    }

    public string Name { get; }
    public string DisplayLabel { get; }

    [ObservableProperty]
    private bool _isEnabled;
}
