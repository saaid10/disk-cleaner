using System.Globalization;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Runs whitelisted-category auto-clean on a schedule (requirements.txt 3i), routing
/// every item through quarantine (3b) - never a direct delete. Only categories that
/// are BOTH policy-eligible (<see cref="WhitelistPolicy"/>) AND explicitly opted into
/// by the user are touched; everything else is left for manual review regardless of
/// scan results. Takes scan results as input rather than owning the scan itself, so
/// the caller composes which locations get scanned.
/// </summary>
public sealed class AutoCleanRunner
{
    public const string WhitelistCategoriesSettingKey = "WhitelistCategories";
    public const string LastAutoCleanRunUtcSettingKey = "LastAutoCleanRunUtc";
    public const string AutoCleanIntervalDaysSettingKey = "AutoCleanIntervalDays";
    public const int DefaultIntervalDays = 7;

    private readonly QuarantineService _quarantine;
    private readonly CleanupHistoryRepository _history;
    private readonly SettingsRepository _settings;

    public AutoCleanRunner(QuarantineService quarantine, CleanupHistoryRepository history, SettingsRepository settings)
    {
        _quarantine = quarantine;
        _history = history;
        _settings = settings;
    }

    public bool IsDue(DateTime nowUtc)
    {
        var lastRunRaw = _settings.GetString(LastAutoCleanRunUtcSettingKey);
        if (lastRunRaw is null)
        {
            return true;
        }

        var lastRun = DateTime.Parse(lastRunRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var intervalDays = _settings.GetInt(AutoCleanIntervalDaysSettingKey, DefaultIntervalDays);
        return nowUtc >= lastRun.AddDays(intervalDays);
    }

    public AutoCleanRunResult RunIfDue(IReadOnlyList<JunkScanResult> candidates, DateTime nowUtc)
    {
        if (!IsDue(nowUtc))
        {
            return AutoCleanRunResult.NotDue;
        }

        var whitelistedCategories = _settings.GetStringSet(WhitelistCategoriesSettingKey);
        var eligible = candidates
            .Where(r => WhitelistPolicy.IsWhitelistEligible(r.Category) && whitelistedCategories.Contains(r.Category.ToString()))
            .GroupBy(r => r.Category);

        var totalCount = 0;
        long totalBytes = 0;
        var quarantinedItems = new List<QuarantineItem>();

        foreach (var group in eligible)
        {
            var categoryCount = 0;
            long categoryBytes = 0;

            foreach (var result in group)
            {
                try
                {
                    quarantinedItems.Add(_quarantine.Quarantine(result.Path, result.Category));
                    categoryCount++;
                    categoryBytes += result.SizeBytes;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            if (categoryCount > 0)
            {
                _history.Add(nowUtc, group.Key, categoryBytes, categoryCount, $"Auto-clean: {group.Key}");
                totalCount += categoryCount;
                totalBytes += categoryBytes;
            }
        }

        _settings.SetString(LastAutoCleanRunUtcSettingKey, nowUtc.ToString("O", CultureInfo.InvariantCulture));
        return new AutoCleanRunResult(Ran: true, totalCount, totalBytes, quarantinedItems);
    }
}
