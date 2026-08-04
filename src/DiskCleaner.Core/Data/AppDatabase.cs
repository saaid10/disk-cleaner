using Microsoft.Data.Sqlite;

namespace DiskCleaner.Core.Data;

/// <summary>
/// Opens the app's local SQLite database (requirements.txt 2) and ensures the schema
/// exists. Settings, protected folders, quarantine metadata, organizer rules, and
/// cleanup history all live in this one file - single-machine, single-user tool.
/// </summary>
public sealed class AppDatabase
{
    public string DbPath { get; }

    public AppDatabase(string dbPath)
    {
        DbPath = dbPath;
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        EnsureCreated();
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }

        return connection;
    }

    private void EnsureCreated()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ProtectedFolders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Path TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS QuarantineItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OriginalPath TEXT NOT NULL,
                QuarantinePath TEXT NOT NULL,
                Category TEXT NOT NULL,
                SizeBytes INTEGER NOT NULL,
                DeletedAtUtc TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                Restored INTEGER NOT NULL DEFAULT 0,
                Purged INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS OrganizerRules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MatchKind TEXT NOT NULL,
                Pattern TEXT NOT NULL,
                TargetFolder TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CleanupHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TimestampUtc TEXT NOT NULL,
                Category TEXT NOT NULL,
                BytesReclaimed INTEGER NOT NULL,
                ItemCount INTEGER NOT NULL,
                Description TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
