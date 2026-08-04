using DiskCleaner.Core.Models;
using Microsoft.Data.Sqlite;

namespace DiskCleaner.Core.Data;

public sealed class OrganizerRuleRepository
{
    private readonly AppDatabase _db;

    public OrganizerRuleRepository(AppDatabase db) => _db = db;

    public IReadOnlyList<OrganizerRule> GetAll()
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, MatchKind, Pattern, TargetFolder FROM OrganizerRules ORDER BY Id;";

        using var reader = command.ExecuteReader();
        var results = new List<OrganizerRule>();
        while (reader.Read())
        {
            results.Add(new OrganizerRule(
                reader.GetInt64(0),
                Enum.Parse<OrganizerMatchKind>(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3)));
        }

        return results;
    }

    public long Add(OrganizerMatchKind matchKind, string pattern, string targetFolder)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO OrganizerRules (MatchKind, Pattern, TargetFolder)
            VALUES ($matchKind, $pattern, $targetFolder);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$matchKind", matchKind.ToString());
        command.Parameters.AddWithValue("$pattern", pattern);
        command.Parameters.AddWithValue("$targetFolder", targetFolder);
        return (long)command.ExecuteScalar()!;
    }

    public void Remove(long id)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM OrganizerRules WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }
}
