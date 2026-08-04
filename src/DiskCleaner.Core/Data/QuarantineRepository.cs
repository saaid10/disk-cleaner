using System.Globalization;
using DiskCleaner.Core.Models;
using Microsoft.Data.Sqlite;

namespace DiskCleaner.Core.Data;

public sealed class QuarantineRepository
{
    private readonly AppDatabase _db;

    public QuarantineRepository(AppDatabase db) => _db = db;

    public long Insert(string originalPath, string quarantinePath, JunkCategory category, long sizeBytes,
        DateTime deletedAtUtc, DateTime expiresAtUtc)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO QuarantineItems
                (OriginalPath, QuarantinePath, Category, SizeBytes, DeletedAtUtc, ExpiresAtUtc, Restored, Purged)
            VALUES
                ($originalPath, $quarantinePath, $category, $sizeBytes, $deletedAtUtc, $expiresAtUtc, 0, 0);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$originalPath", originalPath);
        command.Parameters.AddWithValue("$quarantinePath", quarantinePath);
        command.Parameters.AddWithValue("$category", category.ToString());
        command.Parameters.AddWithValue("$sizeBytes", sizeBytes);
        command.Parameters.AddWithValue("$deletedAtUtc", deletedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$expiresAtUtc", expiresAtUtc.ToString("O", CultureInfo.InvariantCulture));

        return (long)command.ExecuteScalar()!;
    }

    public IReadOnlyList<QuarantineItem> GetActive()
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, OriginalPath, QuarantinePath, Category, SizeBytes, DeletedAtUtc, ExpiresAtUtc, Restored, Purged
            FROM QuarantineItems
            WHERE Restored = 0 AND Purged = 0
            ORDER BY DeletedAtUtc DESC;
            """;

        using var reader = command.ExecuteReader();
        var results = new List<QuarantineItem>();
        while (reader.Read())
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public IReadOnlyList<QuarantineItem> GetExpiredNotPurged(DateTime nowUtc)
    {
        return GetActive().Where(i => i.IsExpired(nowUtc)).ToList();
    }

    public void MarkRestored(long id)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE QuarantineItems SET Restored = 1 WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void MarkPurged(long id)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE QuarantineItems SET Purged = 1 WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static QuarantineItem Map(SqliteDataReader reader) => new(
        Id: reader.GetInt64(0),
        OriginalPath: reader.GetString(1),
        QuarantinePath: reader.GetString(2),
        Category: Enum.Parse<JunkCategory>(reader.GetString(3)),
        SizeBytes: reader.GetInt64(4),
        DeletedAtUtc: DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        ExpiresAtUtc: DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        Restored: reader.GetInt64(7) != 0,
        Purged: reader.GetInt64(8) != 0);
}
