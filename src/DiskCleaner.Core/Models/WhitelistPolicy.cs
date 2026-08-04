namespace DiskCleaner.Core.Models;

/// <summary>
/// Which categories can ever be opted into whitelist auto-clean (requirements.txt 3b,
/// 3g). Windows Update leftovers, duplicates, large/old files, and Downloads are
/// hard-coded as never-eligible - this is a safety guarantee, not a default the user
/// can override via Settings, since getting this wrong risks OS rollback or destroying
/// files that needed a human judgment call. Downloads specifically can contain
/// anything - installers not yet run, documents, unextracted archives - so it can
/// never be a "safe to auto-delete" category the way browser cache or temp files are.
/// </summary>
public static class WhitelistPolicy
{
    private static readonly HashSet<JunkCategory> NeverWhitelistEligible = new()
    {
        JunkCategory.WindowsUpdateLeftovers,
        JunkCategory.Duplicate,
        JunkCategory.LargeOldFile,
        JunkCategory.Downloads,
    };

    public static bool IsWhitelistEligible(JunkCategory category) => !NeverWhitelistEligible.Contains(category);
}
