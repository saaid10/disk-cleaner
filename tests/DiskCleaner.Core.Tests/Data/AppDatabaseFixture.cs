using DiskCleaner.Core.Data;

namespace DiskCleaner.Core.Tests.Data;

/// <summary>
/// Creates a throwaway SQLite file per test so tests never touch the real app DB.
/// </summary>
public sealed class AppDatabaseFixture : IDisposable
{
    public AppDatabase Database { get; }
    private readonly string _dbPath;

    public AppDatabaseFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"disk-cleaner-tests-{Guid.NewGuid():N}.db");
        Database = new AppDatabase(_dbPath);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
