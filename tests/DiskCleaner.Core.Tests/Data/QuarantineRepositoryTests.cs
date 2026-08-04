using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Tests.Data;

public class QuarantineRepositoryTests : IClassFixture<AppDatabaseFixture>
{
    private readonly QuarantineRepository _repo;

    public QuarantineRepositoryTests(AppDatabaseFixture fixture) => _repo = new QuarantineRepository(fixture.Database);

    [Fact]
    public void Insert_ThenGetActive_ReturnsItem()
    {
        var now = DateTime.UtcNow;
        _repo.Insert(@"C:\Temp\a.tmp", @"C:\Quarantine\a.tmp", JunkCategory.WindowsTemp, 1024, now, now.AddDays(7));

        var active = _repo.GetActive();
        Assert.Single(active);
        Assert.Equal(@"C:\Temp\a.tmp", active[0].OriginalPath);
        Assert.Equal(JunkCategory.WindowsTemp, active[0].Category);
    }

    [Fact]
    public void MarkRestored_RemovesFromActiveList()
    {
        var now = DateTime.UtcNow;
        var id = _repo.Insert(@"C:\Temp\b.tmp", @"C:\Quarantine\b.tmp", JunkCategory.WindowsTemp, 2048, now, now.AddDays(7));

        _repo.MarkRestored(id);

        Assert.DoesNotContain(_repo.GetActive(), i => i.Id == id);
    }

    [Fact]
    public void GetExpiredNotPurged_OnlyReturnsPastExpiry()
    {
        var now = DateTime.UtcNow;
        var expiredId = _repo.Insert(@"C:\Temp\old.tmp", @"C:\Quarantine\old.tmp", JunkCategory.WindowsTemp, 512, now.AddDays(-8), now.AddDays(-1));
        var freshId = _repo.Insert(@"C:\Temp\new.tmp", @"C:\Quarantine\new.tmp", JunkCategory.WindowsTemp, 512, now, now.AddDays(7));

        var expired = _repo.GetExpiredNotPurged(now);

        Assert.Contains(expired, i => i.Id == expiredId);
        Assert.DoesNotContain(expired, i => i.Id == freshId);
    }

    [Fact]
    public void MarkPurged_RemovesFromActiveAndExpiredLists()
    {
        var now = DateTime.UtcNow;
        var id = _repo.Insert(@"C:\Temp\gone.tmp", @"C:\Quarantine\gone.tmp", JunkCategory.WindowsTemp, 512, now.AddDays(-8), now.AddDays(-1));

        _repo.MarkPurged(id);

        Assert.DoesNotContain(_repo.GetActive(), i => i.Id == id);
        Assert.DoesNotContain(_repo.GetExpiredNotPurged(now), i => i.Id == id);
    }
}
