namespace DiskCleaner.Core.Services;

/// <summary>
/// Shared Settings keys for scan scope/thresholds (requirements.txt 3f, 3g, 3h) -
/// centralized so the Settings screen and the background auto-clean pass agree on
/// the same keys and defaults.
/// </summary>
public static class ScanScopeSettingsKeys
{
    /// <summary>User-selected folders/drives in scope for dev-build/log/duplicate/large-file scans (3h).</summary>
    public const string ScanRoots = "ScanRoots";

    /// <summary>Removable drive names explicitly opted into scanning (3h) - excluded by default.</summary>
    public const string OptedInRemovableDrives = "OptedInRemovableDrives";

    public const string LargeFileMinSizeBytes = "LargeFileMinSizeBytes";
    public const long DefaultLargeFileMinSizeBytes = 100L * 1024 * 1024; // 100MB

    public const string LargeFileMinDaysSinceAccess = "LargeFileMinDaysSinceAccess";
    public const int DefaultLargeFileMinDaysSinceAccess = 90;

    public const string LogFileOlderThanDays = "LogFileOlderThanDays";
    public const int DefaultLogFileOlderThanDays = 30;
}
