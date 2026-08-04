using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Settings screen (requirements.txt 3k): whitelist opt-ins, thresholds, scan scope,
/// auto-clean interval - all backed by SettingsRepository so the background
/// AutoCleanBackgroundService and every scan screen read the same values.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsRepository _settings;
    private readonly Func<string?> _browseForFolder;

    public SettingsViewModel(SettingsRepository settings, Func<string?> browseForFolder)
    {
        _settings = settings;
        _browseForFolder = browseForFolder;

        var whitelisted = _settings.GetStringSet(AutoCleanRunner.WhitelistCategoriesSettingKey);
        foreach (JunkCategory category in Enum.GetValues<JunkCategory>())
        {
            if (WhitelistPolicy.IsWhitelistEligible(category))
            {
                WhitelistOptions.Add(new WhitelistCategoryOptionViewModel(category, whitelisted.Contains(category.ToString())));
            }
        }

        foreach (var root in _settings.GetStringSet(ScanScopeSettingsKeys.ScanRoots))
        {
            ScanRoots.Add(root);
        }

        _autoCleanIntervalDays = _settings.GetInt(AutoCleanRunner.AutoCleanIntervalDaysSettingKey, AutoCleanRunner.DefaultIntervalDays);
        _largeFileMinSizeMb = _settings.GetInt(ScanScopeSettingsKeys.LargeFileMinSizeMb, ScanScopeSettingsKeys.DefaultLargeFileMinSizeMb);
        _largeFileMinDaysSinceAccess = _settings.GetInt(ScanScopeSettingsKeys.LargeFileMinDaysSinceAccess, ScanScopeSettingsKeys.DefaultLargeFileMinDaysSinceAccess);
        _logFileOlderThanDays = _settings.GetInt(ScanScopeSettingsKeys.LogFileOlderThanDays, ScanScopeSettingsKeys.DefaultLogFileOlderThanDays);
    }

    public ObservableCollection<WhitelistCategoryOptionViewModel> WhitelistOptions { get; } = new();
    public ObservableCollection<string> ScanRoots { get; } = new();

    [ObservableProperty]
    private int _autoCleanIntervalDays;

    [ObservableProperty]
    private int _largeFileMinSizeMb;

    [ObservableProperty]
    private int _largeFileMinDaysSinceAccess;

    [ObservableProperty]
    private int _logFileOlderThanDays;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [RelayCommand]
    private void AddScanRoot()
    {
        var path = _browseForFolder();
        if (!string.IsNullOrWhiteSpace(path) && !ScanRoots.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            ScanRoots.Add(path);
        }
    }

    [RelayCommand]
    private void RemoveScanRoot(string root) => ScanRoots.Remove(root);

    [RelayCommand]
    private void Save()
    {
        _settings.SetStringSet(
            AutoCleanRunner.WhitelistCategoriesSettingKey,
            WhitelistOptions.Where(o => o.IsEnabled).Select(o => o.Category.ToString()));
        _settings.SetStringSet(ScanScopeSettingsKeys.ScanRoots, ScanRoots);
        _settings.SetInt(AutoCleanRunner.AutoCleanIntervalDaysSettingKey, AutoCleanIntervalDays);
        _settings.SetInt(ScanScopeSettingsKeys.LargeFileMinSizeMb, LargeFileMinSizeMb);
        _settings.SetInt(ScanScopeSettingsKeys.LargeFileMinDaysSinceAccess, LargeFileMinDaysSinceAccess);
        _settings.SetInt(ScanScopeSettingsKeys.LogFileOlderThanDays, LogFileOlderThanDays);
        StatusText = "Settings saved.";
    }
}
