using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DiskCleaner.Core.Data;

/// <summary>
/// Typed key-value settings store (requirements.txt 2): whitelist opt-ins, thresholds,
/// Safe Haven path, rolling-window/scan-schedule config, etc.
/// </summary>
public sealed class SettingsRepository
{
    // ASCII Unit Separator (0x1F) - won't appear in real file paths, safe join/split delimiter.
    private const char ListSeparator = (char)0x1F;

    private readonly AppDatabase _db;

    public SettingsRepository(AppDatabase db) => _db = db;

    public string? GetString(string key)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Settings WHERE Key = $key;";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() as string;
    }

    public void SetString(string key, string value)
    {
        using var connection = _db.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Settings (Key, Value) VALUES ($key, $value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    public int GetInt(string key, int defaultValue)
    {
        var raw = GetString(key);
        return raw is not null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    public void SetInt(string key, int value) => SetString(key, value.ToString(CultureInfo.InvariantCulture));

    public bool GetBool(string key, bool defaultValue)
    {
        var raw = GetString(key);
        return raw switch
        {
            "1" => true,
            "0" => false,
            _ => defaultValue,
        };
    }

    public void SetBool(string key, bool value) => SetString(key, value ? "1" : "0");

    public IReadOnlySet<string> GetStringSet(string key)
    {
        var raw = GetString(key);
        if (string.IsNullOrEmpty(raw))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return new HashSet<string>(
            raw.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    public void SetStringSet(string key, IEnumerable<string> values) =>
        SetString(key, string.Join(ListSeparator, values));
}
