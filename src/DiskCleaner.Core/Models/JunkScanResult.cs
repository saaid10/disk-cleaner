namespace DiskCleaner.Core.Models;

/// <summary>
/// One candidate for cleanup found by the junk scanner (requirements.txt 3g).
/// </summary>
public sealed record JunkScanResult(string Path, JunkCategory Category, long SizeBytes)
{
    public bool WhitelistEligible => WhitelistPolicy.IsWhitelistEligible(Category);
}
