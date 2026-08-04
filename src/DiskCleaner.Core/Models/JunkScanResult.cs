namespace DiskCleaner.Core.Models;

/// <summary>
/// One candidate for cleanup found by the junk scanner (requirements.txt 3g).
/// LastModifiedUtc is populated for per-file categories (e.g. Downloads) where the
/// user benefits from seeing how old a file is; left null for aggregate-location
/// categories (browser cache, Windows temp) where a single timestamp isn't meaningful.
/// </summary>
public sealed record JunkScanResult(string Path, JunkCategory Category, long SizeBytes, DateTime? LastModifiedUtc = null)
{
    public bool WhitelistEligible => WhitelistPolicy.IsWhitelistEligible(Category);
}
