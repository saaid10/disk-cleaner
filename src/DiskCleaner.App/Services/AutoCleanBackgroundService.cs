using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.Services;

/// <summary>
/// Ticks hourly and runs the whitelisted auto-clean pass when it's due
/// (requirements.txt 3i). Lives in the App project because it owns a real
/// System.Threading.Timer; the actual due-check/quarantine logic is
/// AutoCleanRunner (Core, unit-tested).
/// </summary>
public sealed class AutoCleanBackgroundService : IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly System.Threading.Timer _timer;
    private readonly AutoCleanRunner _runner;
    private readonly JunkScanner _scanner;
    private readonly SettingsRepository _settings;
    private readonly AppLogger _logger;
    private readonly Action<AutoCleanRunResult>? _onRun;

    public AutoCleanBackgroundService(
        AutoCleanRunner runner,
        JunkScanner scanner,
        SettingsRepository settings,
        AppLogger logger,
        Action<AutoCleanRunResult>? onRun = null)
    {
        _runner = runner;
        _scanner = scanner;
        _settings = settings;
        _logger = logger;
        _onRun = onRun;
        _timer = new System.Threading.Timer(_ => CheckAndRun(), null, TimeSpan.FromMinutes(1), CheckInterval);
    }

    private void CheckAndRun()
    {
        try
        {
            var now = DateTime.UtcNow;
            if (!_runner.IsDue(now))
            {
                return;
            }

            var candidates = new List<JunkScanResult>();
            candidates.AddRange(_scanner.ScanKnownLocations(KnownJunkLocations.BrowserCachePaths(), JunkCategory.BrowserCache));
            candidates.AddRange(_scanner.ScanKnownLocations(KnownJunkLocations.WindowsTempPaths(), JunkCategory.WindowsTemp));

            var scanRoots = _settings.GetStringSet(ScanScopeSettingsKeys.ScanRoots);
            if (scanRoots.Count > 0)
            {
                candidates.AddRange(_scanner.ScanDevBuildFolders(scanRoots));
                var logAgeDays = _settings.GetInt(ScanScopeSettingsKeys.LogFileOlderThanDays, ScanScopeSettingsKeys.DefaultLogFileOlderThanDays);
                candidates.AddRange(_scanner.ScanLogFiles(scanRoots, logAgeDays, now));
            }

            var result = _runner.RunIfDue(candidates, now);
            if (result.Ran)
            {
                _onRun?.Invoke(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(nameof(AutoCleanBackgroundService), ex);
        }
    }

    public void Dispose() => _timer.Dispose();
}
