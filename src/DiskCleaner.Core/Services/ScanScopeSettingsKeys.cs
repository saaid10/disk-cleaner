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

    /// <summary>Stored in MB (not raw bytes) - keeps the value comfortably within int32 and matches the Settings UI's units.</summary>
    public const string LargeFileMinSizeMb = "LargeFileMinSizeMb";
    public const int DefaultLargeFileMinSizeMb = 100;

    public const string LargeFileMinDaysSinceAccess = "LargeFileMinDaysSinceAccess";
    public const int DefaultLargeFileMinDaysSinceAccess = 90;

    public const string LogFileOlderThanDays = "LogFileOlderThanDays";
    public const int DefaultLogFileOlderThanDays = 30;

    /// <summary>Treemap items smaller than this (MB) are bucketed into "Other (small items)" instead of each getting their own rectangle (3f).</summary>
    public const string TreemapMinSizeMb = "TreemapMinSizeMb";
    public const int DefaultTreemapMinSizeMb = 5;
}
