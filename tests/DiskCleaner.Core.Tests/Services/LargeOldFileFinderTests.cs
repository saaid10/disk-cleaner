using DiskCleaner.Core.Data;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class LargeOldFileFinderTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _root;
    private readonly ProtectedFolderRepository _protectedFolders;
    private readonly LargeOldFileFinder _finder;

    public LargeOldFileFinderTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dc-large-db-{Guid.NewGuid():N}.db");
        var db = new AppDatabase(_dbPath);
        _protectedFolders = new ProtectedFolderRepository(db);
        var protection = new PathProtectionService(_protectedFolders, new SettingsRepository(db));
        _finder = new LargeOldFileFinder(protection);

        _root = Path.Combine(Path.GetTempPath(), $"dc-large-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    private string WriteFile(string name, int sizeBytes)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, new byte[sizeBytes]);
        return path;
    }

    [Fact]
    public void FindLargeFiles_FiltersOutFilesBelowSizeThreshold()
    {
        var big = WriteFile("big.bin", 1000);
        WriteFile("small.bin", 10);

        var results = _finder.FindLargeFiles(new[] { _root }, minSizeBytes: 500);

        Assert.Single(results);
        Assert.Equal(big, results[0].Path);
    }

    [Fact]
    public void FindLargeFiles_NoAgeFilter_IncludesRecentlyAccessedFiles()
    {
        var path = WriteFile("recent.bin", 1000);
        File.SetLastAccessTimeUtc(path, DateTime.UtcNow);

        var results = _finder.FindLargeFiles(new[] { _root }, minSizeBytes: 500);

        Assert.Single(results);
    }

    [Fact]
    public void FindLargeFiles_WithAgeFilter_ExcludesRecentlyAccessedFiles()
    {
        var oldPath = WriteFile("old.bin", 1000);
        var recentPath = WriteFile("recent.bin", 1000);
        var now = DateTime.UtcNow;
        File.SetLastAccessTimeUtc(oldPath, now.AddDays(-120));
        File.SetLastAccessTimeUtc(recentPath, now.AddDays(-2));

        var results = _finder.FindLargeFiles(new[] { _root }, minSizeBytes: 500, minDaysSinceAccess: 90, nowUtc: now);

        Assert.Single(results);
        Assert.Equal(oldPath, results[0].Path);
    }

    [Fact]
    public void FindLargeFiles_ProtectedFolder_ExcludedFromResults()
    {
        var protectedDir = Path.Combine(_root, "Protected");
        Directory.CreateDirectory(protectedDir);
        File.WriteAllBytes(Path.Combine(protectedDir, "huge.bin"), new byte[10_000]);
        _protectedFolders.Add(protectedDir);

        var results = _finder.FindLargeFiles(new[] { _root }, minSizeBytes: 500);

        Assert.Empty(results);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
