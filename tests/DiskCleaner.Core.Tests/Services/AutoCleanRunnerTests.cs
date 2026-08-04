using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class AutoCleanRunnerTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _workDir;
    private readonly string _quarantineRoot;
    private readonly SettingsRepository _settings;
    private readonly CleanupHistoryRepository _history;
    private readonly AutoCleanRunner _runner;
    private DateTime _now = DateTime.UtcNow;

    public AutoCleanRunnerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dc-auto-db-{Guid.NewGuid():N}.db");
        var db = new AppDatabase(_dbPath);
        _settings = new SettingsRepository(db);
        _history = new CleanupHistoryRepository(db);

        _workDir = Path.Combine(Path.GetTempPath(), $"dc-auto-work-{Guid.NewGuid():N}");
        _quarantineRoot = Path.Combine(Path.GetTempPath(), $"dc-auto-quarantine-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);

        var quarantine = new QuarantineService(new QuarantineRepository(db), _quarantineRoot, () => _now);
        _runner = new AutoCleanRunner(quarantine, _history, _settings);
    }

    private string CreateFile(string name)
    {
        var path = Path.Combine(_workDir, name);
        File.WriteAllText(path, "junk");
        return path;
    }

    [Fact]
    public void IsDue_NoPreviousRun_ReturnsTrue() => Assert.True(_runner.IsDue(_now));

    [Fact]
    public void RunIfDue_NotDueYet_ReturnsNotDueWithoutTouchingFiles()
    {
        _settings.SetString(AutoCleanRunner.LastAutoCleanRunUtcSettingKey, _now.ToString("O"));
        var filePath = CreateFile("a.tmp");
        var candidates = new[] { new JunkScanResult(filePath, JunkCategory.WindowsTemp, 4) };

        var result = _runner.RunIfDue(candidates, _now.AddDays(1)); // interval defaults to 7 days

        Assert.False(result.Ran);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void RunIfDue_WhitelistedCategoryOptedIn_QuarantinesMatchingResults()
    {
        _settings.SetStringSet(AutoCleanRunner.WhitelistCategoriesSettingKey, new[] { "WindowsTemp" });
        var filePath = CreateFile("a.tmp");
        var candidates = new[] { new JunkScanResult(filePath, JunkCategory.WindowsTemp, 4) };

        var result = _runner.RunIfDue(candidates, _now);

        Assert.True(result.Ran);
        Assert.Equal(1, result.ItemsQuarantined);
        Assert.Equal(4, result.BytesQuarantined);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void RunIfDue_CategoryNotOptedIn_LeavesFileAlone()
    {
        // WindowsTemp is whitelist-ELIGIBLE, but the user never opted it in via Settings.
        var filePath = CreateFile("a.tmp");
        var candidates = new[] { new JunkScanResult(filePath, JunkCategory.WindowsTemp, 4) };

        var result = _runner.RunIfDue(candidates, _now);

        Assert.True(result.Ran);
        Assert.Equal(0, result.ItemsQuarantined);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void RunIfDue_NeverWhitelistEligibleCategory_NeverQuarantinedEvenIfOptedIn()
    {
        // Even if somehow "opted in", WindowsUpdateLeftovers must never be touched by
        // auto-clean - this is the safety guarantee from WhitelistPolicy (task 4).
        _settings.SetStringSet(AutoCleanRunner.WhitelistCategoriesSettingKey, new[] { "WindowsUpdateLeftovers" });
        var filePath = CreateFile("Windows.old-stub");
        var candidates = new[] { new JunkScanResult(filePath, JunkCategory.WindowsUpdateLeftovers, 4) };

        var result = _runner.RunIfDue(candidates, _now);

        Assert.Equal(0, result.ItemsQuarantined);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void RunIfDue_UpdatesLastRunTimestamp_SoSecondCallSameDayIsNotDue()
    {
        _runner.RunIfDue(Array.Empty<JunkScanResult>(), _now);

        Assert.False(_runner.IsDue(_now.AddHours(1)));
    }

    [Fact]
    public void RunIfDue_RecordsCleanupHistoryPerCategory()
    {
        _settings.SetStringSet(AutoCleanRunner.WhitelistCategoriesSettingKey, new[] { "WindowsTemp" });
        var filePath = CreateFile("a.tmp");
        var candidates = new[] { new JunkScanResult(filePath, JunkCategory.WindowsTemp, 4) };

        _runner.RunIfDue(candidates, _now);

        var recent = _history.GetRecent();
        Assert.Contains(recent, e => e.Category == JunkCategory.WindowsTemp && e.ItemCount == 1 && e.BytesReclaimed == 4);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workDir))
        {
            Directory.Delete(_workDir, recursive: true);
        }

        if (Directory.Exists(_quarantineRoot))
        {
            Directory.Delete(_quarantineRoot, recursive: true);
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
