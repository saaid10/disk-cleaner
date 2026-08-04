using DiskCleaner.Core.Models;
using Microsoft.Data.Sqlite;

namespace DiskCleaner.Core.Data;

public sealed class ProtectedFolderRepository
{
    private readonly AppDatabase _db;

    public ProtectedFolderRepository(AppDatabase db) => _db = db;

    public IReadOnlyList<ProtectedFolder> GetAll()
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Path FROM ProtectedFolders ORDER BY Path;";

        var results = new List<ProtectedFolder>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new ProtectedFolder(reader.GetInt64(0), reader.GetString(1)));
        }

        return results;
    }

    public void Add(string path)
    {
        var normalized = Path.GetFullPath(path).TrimEnd('\\', '/');
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO ProtectedFolders (Path) VALUES ($path);";
        command.Parameters.AddWithValue("$path", normalized);
        command.ExecuteNonQuery();
    }

    public void Remove(long id)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ProtectedFolders WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }
}
