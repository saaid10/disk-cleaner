using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Display-ready wrapper around <see cref="DriveSpaceInfo"/> for the Dashboard drive
/// cards (requirements.txt 3m): total/used/free formatted via the shared size
/// formatter (3l), plus the used fraction for the proportion bar.
/// </summary>
public sealed class DriveCardViewModel
{
    public DriveCardViewModel(DriveSpaceInfo info)
    {
        Name = info.Name;
        VolumeLabel = info.VolumeLabel;
        TotalDisplay = FileSizeFormatter.Format(info.TotalBytes);
        UsedDisplay = FileSizeFormatter.Format(info.UsedBytes);
        FreeDisplay = FileSizeFormatter.Format(info.FreeBytes);
        UsedFraction = info.UsedFraction;
        FreeFraction = 1 - info.UsedFraction;
    }

    public string Name { get; }
    public string VolumeLabel { get; }
    public string TotalDisplay { get; }
    public string UsedDisplay { get; }
    public string FreeDisplay { get; }
    public double UsedFraction { get; }
    public double FreeFraction { get; }
}
