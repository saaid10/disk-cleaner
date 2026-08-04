using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Tests;

public class WhitelistPolicyTests
{
    [Theory]
    [InlineData(JunkCategory.WindowsUpdateLeftovers, false)]
    [InlineData(JunkCategory.Duplicate, false)]
    [InlineData(JunkCategory.LargeOldFile, false)]
    [InlineData(JunkCategory.Downloads, false)]
    [InlineData(JunkCategory.BrowserCache, true)]
    [InlineData(JunkCategory.WindowsTemp, true)]
    [InlineData(JunkCategory.DevBuildFolders, true)]
    [InlineData(JunkCategory.LogFiles, true)]
    public void IsWhitelistEligible_MatchesRequirementsDoc(JunkCategory category, bool expected)
    {
        Assert.Equal(expected, WhitelistPolicy.IsWhitelistEligible(category));
    }
}
