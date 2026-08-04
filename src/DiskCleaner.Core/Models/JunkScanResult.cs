namespace DiskCleaner.Core.Models;

/// <summary>
/// One candidate for cleanup found by the junk scanner (requirements.txt 3g).
/// LastModifiedUtc is populated for per-file categories (e.g. Downloads) where the
/// user benefits from seeing how old a file is; left null for aggregate-location
/// categories (browser cache, Windows temp) where a single timestamp isn't meaningful.
/// IsAggregateLocation marks a result whose Path is a whole folder summed as one
/// number (ScanKnownLocations) rather than an individual file/subfolder - deleting
/// it must quarantine the folder's contents item-by-item
/// (QuarantineService.QuarantineFolderContents), not move the whole folder in one
/// atomic operation, since a single locked file inside would fail the entire move.
/// </summary>
public sealed record JunkScanResult(
    string Path,
    JunkCategory Category,
    long SizeBytes,
    DateTime? LastModifiedUtc = null,
    bool IsAggregateLocation = false)
{
    public bool WhitelistEligible => WhitelistPolicy.IsWhitelistEligible(Category);
}
