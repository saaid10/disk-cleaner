using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Enumerates drives for the Dashboard/Disk Scan overview (requirements.txt 3h, 3m).
/// Fixed drives are included by default; removable drives are excluded unless the
/// caller explicitly opts a drive name in.
/// </summary>
public sealed class DriveSpaceService
{
    public IReadOnlyList<DriveSpaceInfo> GetDrives(IReadOnlySet<string>? optedInRemovableDrives = null)
    {
        optedInRemovableDrives ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Where(d => d.DriveType == DriveType.Fixed
                        || (d.DriveType == DriveType.Removable && optedInRemovableDrives.Contains(d.Name)))
            .Select(d => new DriveSpaceInfo(
                Name: d.Name,
                VolumeLabel: SafeVolumeLabel(d),
                TotalBytes: d.TotalSize,
                FreeBytes: d.TotalFreeSpace))
            .ToList();
    }

    /// <summary>
    /// Removable drives currently attached (requirements.txt 3h) - lets the Settings
    /// screen offer them as opt-in checkboxes rather than requiring the user to know
    /// a drive letter to type in.
    /// </summary>
    public IReadOnlyList<DriveSpaceInfo> GetAvailableRemovableDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Removable)
            .Select(d => new DriveSpaceInfo(d.Name, SafeVolumeLabel(d), d.TotalSize, d.TotalFreeSpace))
            .ToList();
    }

    private static string SafeVolumeLabel(DriveInfo drive)
    {
        try
        {
            return string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : drive.VolumeLabel;
        }
        catch (IOException)
        {
            return drive.Name;
        }
    }
}
