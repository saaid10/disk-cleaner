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
