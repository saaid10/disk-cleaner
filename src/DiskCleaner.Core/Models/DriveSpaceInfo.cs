namespace DiskCleaner.Core.Models;

/// <summary>
/// Per-drive used/free breakdown shown on the Dashboard and Disk Scan screen
/// (requirements.txt 3m).
/// </summary>
public sealed record DriveSpaceInfo(
    string Name,
    string VolumeLabel,
    long TotalBytes,
    long FreeBytes)
{
    public long UsedBytes => TotalBytes - FreeBytes;

    public double UsedFraction => TotalBytes == 0 ? 0 : UsedBytes / (double)TotalBytes;
}
