using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class DirectoryUsageServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _root;
    private readonly ProtectedFolderRepository _protectedFolders;
    private readonly DirectoryUsageService _service;

    public DirectoryUsageServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dc-dirusage-db-{Guid.NewGuid():N}.db");
        var db = new AppDatabase(_dbPath);
        _protectedFolders = new ProtectedFolderRepository(db);
        var protection = new PathProtectionService(_protectedFolders, new SettingsRepository(db));
        _service = new DirectoryUsageService(protection);

        _root = Path.Combine(Path.GetTempPath(), $"dc-dirusage-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void GetChildNodes_TopLevelFile_ReturnsFileNodeWithCategory()
    {
        File.WriteAllBytes(Path.Combine(_root, "photo.png"), new byte[100]);

        var nodes = _service.GetChildNodes(_root);

        var node = Assert.Single(nodes);
        Assert.Equal("photo.png", node.Label);
        Assert.Equal(100, node.SizeBytes);
        Assert.Equal(FileTypeCategory.Images, node.Category);
    }

    [Fact]
    public void GetChildNodes_Subfolder_SizedRecursivelyAsFolderCategory()
    {
        var subDir = Path.Combine(_root, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllBytes(Path.Combine(subDir, "a.txt"), new byte[30]);
        Directory.CreateDirectory(Path.Combine(subDir, "nested"));
        File.WriteAllBytes(Path.Combine(subDir, "nested", "b.txt"), new byte[20]);

        var nodes = _service.GetChildNodes(_root);

        var node = Assert.Single(nodes);
        Assert.Equal("sub", node.Label);
        Assert.Equal(50, node.SizeBytes);
        Assert.Equal(FileTypeCategory.Folder, node.Category);
    }

    [Fact]
    public void GetChildNodes_DoesNotDescendPastImmediateChildren_ForFiles()
    {
        var subDir = Path.Combine(_root, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllBytes(Path.Combine(subDir, "deep.txt"), new byte[10]);

        var nodes = _service.GetChildNodes(_root);

        Assert.DoesNotContain(nodes, n => n.Label == "deep.txt");
    }

    [Fact]
    public void GetChildNodes_ProtectedSubfolder_Excluded()
    {
        var protectedDir = Path.Combine(_root, "Protected");
        Directory.CreateDirectory(protectedDir);
        File.WriteAllBytes(Path.Combine(protectedDir, "secret.txt"), new byte[10]);
        _protectedFolders.Add(protectedDir);

        var nodes = _service.GetChildNodes(_root);

        Assert.DoesNotContain(nodes, n => n.Label == "Protected");
    }

    [Fact]
    public void GetChildNodes_EmptyFolder_ExcludedFromResults()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Empty"));

        var nodes = _service.GetChildNodes(_root);

        Assert.Empty(nodes);
    }

    [Fact]
    public void GetChildNodes_MinSizeFilter_BucketsSmallItemsTogether()
    {
        File.WriteAllBytes(Path.Combine(_root, "big.bin"), new byte[1000]);
        File.WriteAllBytes(Path.Combine(_root, "small1.bin"), new byte[10]);
        File.WriteAllBytes(Path.Combine(_root, "small2.bin"), new byte[20]);

        var nodes = _service.GetChildNodes(_root, minSizeBytes: 500);

        Assert.Contains(nodes, n => n.Label == "big.bin");
        Assert.DoesNotContain(nodes, n => n.Label == "small1.bin");
        Assert.DoesNotContain(nodes, n => n.Label == "small2.bin");
        var bucket = Assert.Single(nodes, n => n.Label == "Other (small items)");
        Assert.Equal(30, bucket.SizeBytes);
        Assert.Equal(FileTypeCategory.Other, bucket.Category);
    }

    [Fact]
    public void GetChildNodes_MinSizeFilter_NoSmallItems_NoBucketAdded()
    {
        File.WriteAllBytes(Path.Combine(_root, "big.bin"), new byte[1000]);

        var nodes = _service.GetChildNodes(_root, minSizeBytes: 500);

        Assert.DoesNotContain(nodes, n => n.Label == "Other (small items)");
    }

    [Fact]
    public void GetChildNodes_MinSizeZero_ReturnsAllItemsUnbucketed()
    {
        File.WriteAllBytes(Path.Combine(_root, "tiny.bin"), new byte[1]);

        var nodes = _service.GetChildNodes(_root, minSizeBytes: 0);

        Assert.Contains(nodes, n => n.Label == "tiny.bin");
        Assert.DoesNotContain(nodes, n => n.Label == "Other (small items)");
    }

    [Fact]
    public void GetChildNodes_ParallelSubfolderSizing_ProducesCorrectResultsForManyFolders()
    {
        for (var i = 0; i < 12; i++)
        {
            var dir = Path.Combine(_root, $"folder{i}");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "f.bin"), new byte[i + 1]);
        }

        var nodes = _service.GetChildNodes(_root);

        Assert.Equal(12, nodes.Count);
        for (var i = 0; i < 12; i++)
        {
            Assert.Contains(nodes, n => n.Label == $"folder{i}" && n.SizeBytes == i + 1);
        }
    }

    [Fact]
    public void GetChildNodes_DeepNestedTree_SumsCorrectlyAcrossMultipleLevels()
    {
        // Verifies the deep-parallel size computation (FileSystemWalker.
        // ComputeTotalSizeParallel) correctly sums a multi-level tree, not just
        // immediate children - this is the fix for C:\Windows-style folders where
        // one subfolder dominates because it alone has thousands of nested dirs.
        var level1 = Path.Combine(_root, "big", "level1");
        var level2 = Path.Combine(level1, "level2");
        var level3 = Path.Combine(level2, "level3");
        Directory.CreateDirectory(level3);
        File.WriteAllBytes(Path.Combine(_root, "big", "a.bin"), new byte[10]);
        File.WriteAllBytes(Path.Combine(level1, "b.bin"), new byte[20]);
        File.WriteAllBytes(Path.Combine(level2, "c.bin"), new byte[30]);
        File.WriteAllBytes(Path.Combine(level3, "d.bin"), new byte[40]);

        var nodes = _service.GetChildNodes(_root);

        var node = Assert.Single(nodes);
        Assert.Equal("big", node.Label);
        Assert.Equal(100, node.SizeBytes); // 10 + 20 + 30 + 40 across all 4 levels
    }

    [Fact]
    public void GetChildNodes_OnSubfolderReadyCallback_FiresOncePerSubfolderWithCorrectSize()
    {
        Directory.CreateDirectory(Path.Combine(_root, "a"));
        Directory.CreateDirectory(Path.Combine(_root, "b"));
        File.WriteAllBytes(Path.Combine(_root, "a", "x.bin"), new byte[15]);
        File.WriteAllBytes(Path.Combine(_root, "b", "y.bin"), new byte[25]);

        var received = new System.Collections.Concurrent.ConcurrentBag<TreemapNode>();
        _service.GetChildNodes(_root, onSubfolderReady: node => received.Add(node));

        Assert.Equal(2, received.Count);
        Assert.Contains(received, n => n.Label == "a" && n.SizeBytes == 15);
        Assert.Contains(received, n => n.Label == "b" && n.SizeBytes == 25);
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
