using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class JunkScannerTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _root;
    private readonly PathProtectionService _protection;
    private readonly ProtectedFolderRepository _protectedFolders;
    private readonly JunkScanner _scanner;

    public JunkScannerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dc-scan-db-{Guid.NewGuid():N}.db");
        var db = new AppDatabase(_dbPath);
        _protectedFolders = new ProtectedFolderRepository(db);
        _protection = new PathProtectionService(_protectedFolders, new SettingsRepository(db));
        _scanner = new JunkScanner(_protection);

        _root = Path.Combine(Path.GetTempPath(), $"dc-scan-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    private string Combine(params string[] parts) => Path.Combine(new[] { _root }.Concat(parts).ToArray());

    [Fact]
    public void ScanKnownLocations_ExistingPathWithFiles_ReturnsAggregateResult()
    {
        var cacheDir = Combine("Cache");
        Directory.CreateDirectory(cacheDir);
        File.WriteAllText(Path.Combine(cacheDir, "a.bin"), "12345");
        File.WriteAllText(Path.Combine(cacheDir, "b.bin"), "1234567890");

        var results = _scanner.ScanKnownLocations(new[] { cacheDir }, JunkCategory.BrowserCache);

        Assert.Single(results);
        Assert.Equal(15, results[0].SizeBytes);
        Assert.Equal(JunkCategory.BrowserCache, results[0].Category);
        Assert.True(results[0].WhitelistEligible);
    }

    [Fact]
    public void ScanKnownLocations_NonExistentPath_IsSkipped()
    {
        var results = _scanner.ScanKnownLocations(new[] { Combine("DoesNotExist") }, JunkCategory.WindowsTemp);
        Assert.Empty(results);
    }

    [Fact]
    public void ScanKnownLocations_ProtectedPath_IsExcluded()
    {
        var tempDir = Combine("Temp");
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "x.tmp"), "data");
        _protectedFolders.Add(tempDir);

        var results = _scanner.ScanKnownLocations(new[] { tempDir }, JunkCategory.WindowsTemp);

        Assert.Empty(results);
    }

    [Fact]
    public void ScanDevBuildFolders_FindsNodeModulesAndObjFolders()
    {
        var projectDir = Combine("MyProject");
        var nodeModules = Path.Combine(projectDir, "node_modules");
        var objDir = Path.Combine(projectDir, "obj");
        Directory.CreateDirectory(nodeModules);
        Directory.CreateDirectory(objDir);
        File.WriteAllText(Path.Combine(nodeModules, "pkg.js"), "1234567890"); // 10 bytes

        var results = _scanner.ScanDevBuildFolders(new[] { _root });

        Assert.Contains(results, r => r.Path == nodeModules && r.SizeBytes == 10);
        Assert.Contains(results, r => r.Path == objDir);
        Assert.All(results, r => Assert.Equal(JunkCategory.DevBuildFolders, r.Category));
    }

    [Fact]
    public void ScanDevBuildFolders_DoesNotDescendIntoMatchedFolder()
    {
        // A pathological nested node_modules inside node_modules should only be
        // reported once (the outer one) - scanner must not descend further.
        var outer = Combine("proj", "node_modules");
        var nestedNodeModules = Path.Combine(outer, "some-pkg", "node_modules");
        Directory.CreateDirectory(nestedNodeModules);

        var results = _scanner.ScanDevBuildFolders(new[] { _root });

        Assert.Single(results, r => r.Path == outer);
        Assert.DoesNotContain(results, r => r.Path == nestedNodeModules);
    }

    [Fact]
    public void ScanDevBuildFolders_ProtectedProjectFolder_ExcludesEverythingInside()
    {
        var projectDir = Combine("Protected");
        var nodeModules = Path.Combine(projectDir, "node_modules");
        Directory.CreateDirectory(nodeModules);
        _protectedFolders.Add(projectDir);

        var results = _scanner.ScanDevBuildFolders(new[] { _root });

        Assert.DoesNotContain(results, r => r.Path == nodeModules);
    }

    [Fact]
    public void ScanLogFiles_OnlyReturnsFilesOlderThanThreshold()
    {
        var oldLog = Combine("old.log");
        var newLog = Combine("new.log");
        File.WriteAllText(oldLog, "old entry");
        File.WriteAllText(newLog, "new entry");

        var now = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(oldLog, now.AddDays(-40));
        File.SetLastWriteTimeUtc(newLog, now.AddDays(-1));

        var results = _scanner.ScanLogFiles(new[] { _root }, olderThanDays: 30, nowUtc: now);

        Assert.Contains(results, r => r.Path == oldLog);
        Assert.DoesNotContain(results, r => r.Path == newLog);
    }

    [Fact]
    public void ScanLogFiles_IgnoresNonLogFiles()
    {
        var txtFile = Combine("notes.txt");
        File.WriteAllText(txtFile, "not a log");
        File.SetLastWriteTimeUtc(txtFile, DateTime.UtcNow.AddDays(-100));

        var results = _scanner.ScanLogFiles(new[] { _root }, olderThanDays: 30);

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
