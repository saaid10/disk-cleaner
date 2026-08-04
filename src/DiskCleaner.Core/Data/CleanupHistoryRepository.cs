using System.Globalization;
using DiskCleaner.Core.Models;
using Microsoft.Data.Sqlite;

namespace DiskCleaner.Core.Data;

/// <summary>
/// Before/after cleanup report data (requirements.txt 3j).
/// </summary>
public sealed class CleanupHistoryRepository
{
    private readonly AppDatabase _db;

    public CleanupHistoryRepository(AppDatabase db) => _db = db;

    public void Add(DateTime timestampUtc, JunkCategory category, long bytesReclaimed, int itemCount, string description)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CleanupHistory (TimestampUtc, Category, BytesReclaimed, ItemCount, Description)
            VALUES ($timestamp, $category, $bytes, $count, $description);
            """;
        command.Parameters.AddWithValue("$timestamp", timestampUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$category", category.ToString());
        command.Parameters.AddWithValue("$bytes", bytesReclaimed);
        command.Parameters.AddWithValue("$count", itemCount);
        command.Parameters.AddWithValue("$description", description);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<CleanupHistoryEntry> GetRecent(int limit = 50)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, TimestampUtc, Category, BytesReclaimed, ItemCount, Description
            FROM CleanupHistory
            ORDER BY TimestampUtc DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var results = new List<CleanupHistoryEntry>();
        while (reader.Read())
        {
            results.Add(new CleanupHistoryEntry(
                reader.GetInt64(0),
                DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Enum.Parse<JunkCategory>(reader.GetString(2)),
                reader.GetInt64(3),
                (int)reader.GetInt64(4),
                reader.GetString(5)));
        }

        return results;
    }
}
