using System.Runtime.InteropServices;

namespace DiskCleaner.App.Services;

/// <summary>
/// Recycle Bin size reporting + empty action (requirements.txt 3g). Not routed
/// through QuarantineService - the Recycle Bin already IS a quarantine, so emptying
/// it is a direct action, not something that needs a second safety net.
/// </summary>
public static class RecycleBinService
{
    private const uint ShellErrbNoConfirmation = 0x00000001;
    private const uint ShellErrbNoProgressUi = 0x00000002;
    private const uint ShellErrbNoSound = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShQueryRbInfo
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref ShQueryRbInfo pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    /// <summary>Total size and item count across the Recycle Bin on all drives.</summary>
    public static (long SizeBytes, long ItemCount) QuerySize()
    {
        var info = new ShQueryRbInfo { cbSize = Marshal.SizeOf<ShQueryRbInfo>() };
        var result = SHQueryRecycleBin(null, ref info);
        return result == 0 ? (info.i64Size, info.i64NumItems) : (0, 0);
    }

    /// <returns>True if the Recycle Bin was emptied successfully.</returns>
    public static bool Empty()
    {
        // Confirmation is handled by our own MessageBox before calling this, so
        // suppress the shell's own confirmation/progress UI to avoid a double prompt.
        var flags = ShellErrbNoConfirmation | ShellErrbNoProgressUi | ShellErrbNoSound;
        return SHEmptyRecycleBin(IntPtr.Zero, null, flags) == 0;
    }
}
