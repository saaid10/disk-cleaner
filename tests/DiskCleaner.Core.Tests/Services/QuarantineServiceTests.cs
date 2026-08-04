using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

// Deliberately NOT an IClassFixture: each test needs its own isolated DB, since
// PurgeExpired's "now" comparison would otherwise pick up quarantine items left
// behind by other tests sharing the same database.
public class QuarantineServiceTests : IDisposable
{
    private readonly QuarantineRepository _repo;
    private readonly string _dbPath;
    private readonly string _workDir;
    private readonly string _quarantineRoot;
    private DateTime _now = DateTime.UtcNow;

    public QuarantineServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dc-qtest-db-{Guid.NewGuid():N}.db");
        _repo = new QuarantineRepository(new AppDatabase(_dbPath));
        _workDir = Path.Combine(Path.GetTempPath(), $"dc-qtest-work-{Guid.NewGuid():N}");
        _quarantineRoot = Path.Combine(Path.GetTempPath(), $"dc-qtest-quarantine-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
    }

    private QuarantineService CreateService() => new(_repo, _quarantineRoot, () => _now);

    private string CreateWorkFile(string name, string content = "junk")
    {
        var path = Path.Combine(_workDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Quarantine_MovesFileOutOfOriginalLocation()
    {
        var filePath = CreateWorkFile("a.tmp", "hello");
        var service = CreateService();

        var item = service.Quarantine(filePath, JunkCategory.WindowsTemp);

        Assert.False(File.Exists(filePath));
        Assert.True(File.Exists(item.QuarantinePath));
        Assert.Equal(5, item.SizeBytes);
    }

    [Fact]
    public void Quarantine_ThenRestore_MovesFileBack()
    {
        var filePath = CreateWorkFile("b.tmp", "restore-me");
        var service = CreateService();
        var item = service.Quarantine(filePath, JunkCategory.WindowsTemp);

        service.Restore(item.Id);

        Assert.True(File.Exists(filePath));
        Assert.Equal("restore-me", File.ReadAllText(filePath));
        Assert.DoesNotContain(service.GetActiveItems(), i => i.Id == item.Id);
    }

    [Fact]
    public void Restore_WhenOriginalPathOccupied_ThrowsAndLeavesFileInQuarantine()
    {
        var filePath = CreateWorkFile("c.tmp");
        var service = CreateService();
        var item = service.Quarantine(filePath, JunkCategory.WindowsTemp);

        // Something recreated a file at the original path after quarantining.
        File.WriteAllText(filePath, "someone else's file");

        Assert.Throws<QuarantineRestoreConflictException>(() => service.Restore(item.Id));
        Assert.True(File.Exists(item.QuarantinePath));
    }

    [Fact]
    public void PurgeExpired_DeletesOnlyItemsPastRetentionWindow()
    {
        var freshFile = CreateWorkFile("fresh.tmp");
        var oldFile = CreateWorkFile("old.tmp");
        var service = CreateService();

        var oldItem = service.Quarantine(oldFile, JunkCategory.WindowsTemp);
        var freshItem = service.Quarantine(freshFile, JunkCategory.WindowsTemp);

        _now = _now.AddDays(8); // past the 7-day retention window for both

        var purgedCount = service.PurgeExpired();

        Assert.Equal(2, purgedCount);
        Assert.False(File.Exists(oldItem.QuarantinePath));
        Assert.False(File.Exists(freshItem.QuarantinePath));
    }

    [Fact]
    public void PurgeExpired_DoesNotTouchItemsStillWithinRetentionWindow()
    {
        var filePath = CreateWorkFile("keep.tmp");
        var service = CreateService();
        var item = service.Quarantine(filePath, JunkCategory.WindowsTemp);

        _now = _now.AddDays(3); // still within the 7-day window

        var purgedCount = service.PurgeExpired();

        Assert.Equal(0, purgedCount);
        Assert.True(File.Exists(item.QuarantinePath));
        Assert.Contains(service.GetActiveItems(), i => i.Id == item.Id);
    }

    [Fact]
    public void Quarantine_Directory_SumsSizeOfAllContainedFiles()
    {
        var dirPath = Path.Combine(_workDir, "junkdir");
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Combine(dirPath, "one.txt"), "12345"); // 5 bytes
        File.WriteAllText(Path.Combine(dirPath, "two.txt"), "1234567890"); // 10 bytes

        var service = CreateService();
        var item = service.Quarantine(dirPath, JunkCategory.DevBuildFolders);

        Assert.False(Directory.Exists(dirPath));
        Assert.True(Directory.Exists(item.QuarantinePath));
        Assert.Equal(15, item.SizeBytes);
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
