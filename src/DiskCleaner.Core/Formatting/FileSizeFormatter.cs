using System.Globalization;

namespace DiskCleaner.Core.Formatting;

/// <summary>
/// Shared adaptive-unit size formatter (requirements.txt 3l): GB, falling back to
/// MB, then KB, then bytes - largest unit that keeps the displayed number >= 1.
/// </summary>
public static class FileSizeFormatter
{
    private const long Kb = 1024L;
    private const long Mb = Kb * 1024L;
    private const long Gb = Mb * 1024L;

    public static string Format(long bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "Size cannot be negative.");
        }

        if (bytes >= Gb)
        {
            return (bytes / (double)Gb).ToString("0.00", CultureInfo.InvariantCulture) + " GB";
        }

        if (bytes >= Mb)
        {
            return (bytes / (double)Mb).ToString("0.00", CultureInfo.InvariantCulture) + " MB";
        }

        if (bytes >= Kb)
        {
            return (bytes / (double)Kb).ToString("0", CultureInfo.InvariantCulture) + " KB";
        }

        return $"{bytes} B";
    }
}
