using DiskCleaner.Core.Data;

namespace DiskCleaner.Core.Tests.Data;

public class SettingsRepositoryTests : IClassFixture<AppDatabaseFixture>
{
    private readonly SettingsRepository _repo;

    public SettingsRepositoryTests(AppDatabaseFixture fixture) => _repo = new SettingsRepository(fixture.Database);

    [Fact]
    public void GetString_MissingKey_ReturnsNull() => Assert.Null(_repo.GetString("missing-key"));

    [Fact]
    public void SetString_ThenGet_RoundTrips()
    {
        _repo.SetString("theme", "dark");
        Assert.Equal("dark", _repo.GetString("theme"));
    }

    [Fact]
    public void SetString_Twice_Upserts()
    {
        _repo.SetString("interval", "7");
        _repo.SetString("interval", "14");
        Assert.Equal("14", _repo.GetString("interval"));
    }

    [Fact]
    public void GetInt_MissingKey_ReturnsDefault() => Assert.Equal(90, _repo.GetInt("age-threshold", 90));

    [Fact]
    public void SetInt_ThenGetInt_RoundTrips()
    {
        _repo.SetInt("large-file-mb", 100);
        Assert.Equal(100, _repo.GetInt("large-file-mb", 0));
    }

    [Fact]
    public void GetBool_MissingKey_ReturnsDefault() => Assert.True(_repo.GetBool("scheduling-enabled", true));

    [Fact]
    public void SetBool_ThenGetBool_RoundTrips()
    {
        _repo.SetBool("scheduling-enabled", false);
        Assert.False(_repo.GetBool("scheduling-enabled", true));
    }

    [Fact]
    public void StringSet_RoundTrips_CaseInsensitive()
    {
        _repo.SetStringSet("whitelist-categories", new[] { "BrowserCache", "WindowsTemp" });
        var result = _repo.GetStringSet("whitelist-categories");
        Assert.Contains("browsercache", result);
        Assert.Contains("WindowsTemp", result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void StringSet_MissingKey_ReturnsEmptySet() => Assert.Empty(_repo.GetStringSet("nonexistent"));
}
