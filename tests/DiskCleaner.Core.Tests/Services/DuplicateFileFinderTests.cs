using DiskCleaner.Core.Data;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class DuplicateFileFinderTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _root;
    private readonly ProtectedFolderRepository _protectedFolders;
    private readonly DuplicateFileFinder _finder;

    public DuplicateFileFinderTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dc-dup-db-{Guid.NewGuid():N}.db");
        var db = new AppDatabase(_dbPath);
        _protectedFolders = new ProtectedFolderRepository(db);
        var protection = new PathProtectionService(_protectedFolders, new SettingsRepository(db));
        _finder = new DuplicateFileFinder(protection);

        _root = Path.Combine(Path.GetTempPath(), $"dc-dup-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    private string WriteFile(string name, string content)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void FindDuplicates_IdenticalContent_GroupsTogether()
    {
        var a = WriteFile("a.txt", "same content here");
        var b = WriteFile("b.txt", "same content here");
        WriteFile("c.txt", "different content"); // different content, not a dup

        var groups = _finder.FindDuplicates(new[] { _root });

        var group = Assert.Single(groups);
        Assert.Equal(2, group.FilePaths.Count);
        Assert.Contains(a, group.FilePaths);
        Assert.Contains(b, group.FilePaths);
    }

    [Fact]
    public void FindDuplicates_SameSizeDifferentContent_NotGroupedTogether()
    {
        // Same length, different bytes - the size pre-filter must not produce a false
        // positive; only the hash comparison decides.
        WriteFile("x.txt", "AAAAAAAAAA");
        WriteFile("y.txt", "BBBBBBBBBB");

        var groups = _finder.FindDuplicates(new[] { _root });

        Assert.Empty(groups);
    }

    [Fact]
    public void FindDuplicates_UniqueFile_ReturnsNoGroups()
    {
        WriteFile("only.txt", "nothing matches this");

        var groups = _finder.FindDuplicates(new[] { _root });

        Assert.Empty(groups);
    }

    [Fact]
    public void FindDuplicates_EmptyFiles_AreIgnored()
    {
        WriteFile("empty1.txt", "");
        WriteFile("empty2.txt", "");

        var groups = _finder.FindDuplicates(new[] { _root });

        Assert.Empty(groups);
    }

    [Fact]
    public void FindDuplicates_ThreeIdenticalFiles_OneGroupWithAllThree()
    {
        var a = WriteFile("a.txt", "triplet");
        var b = WriteFile("b.txt", "triplet");
        var c = WriteFile("c.txt", "triplet");

        var groups = _finder.FindDuplicates(new[] { _root });

        var group = Assert.Single(groups);
        Assert.Equal(3, group.FilePaths.Count);
        Assert.Equal(new[] { a, b, c }.OrderBy(p => p), group.FilePaths.OrderBy(p => p));
        Assert.Equal(group.SizeBytesEach * 2, group.WastedBytes);
    }

    [Fact]
    public void FindDuplicates_ProtectedFolder_ExcludedFromResults()
    {
        var protectedDir = Path.Combine(_root, "Protected");
        Directory.CreateDirectory(protectedDir);
        File.WriteAllText(Path.Combine(protectedDir, "a.txt"), "shared content");
        WriteFile("b.txt", "shared content");
        _protectedFolders.Add(protectedDir);

        var groups = _finder.FindDuplicates(new[] { _root });

        // Only one candidate remains (b.txt) since the protected copy is excluded -
        // can't form a duplicate group with just one file.
        Assert.Empty(groups);
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
