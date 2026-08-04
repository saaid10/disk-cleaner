namespace DiskCleaner.Core.Models;

/// <summary>
/// Junk categories from requirements.txt 3g, plus the judgment-call categories from
/// 3e/3f. Whitelist-eligibility (which of these can be opted into auto-clean) is
/// enforced in code, not just by convention - see WhitelistPolicy.
/// </summary>
public enum JunkCategory
{
    BrowserCache,
    WindowsTemp,
    DevBuildFolders,
    LogFiles,
    WindowsUpdateLeftovers,
    Duplicate,
    LargeOldFile,
    /// <summary>Files sitting in the Downloads folder - could be anything, so always manual-review.</summary>
    Downloads,
}
