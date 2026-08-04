using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Protected Folders / Safe Haven screen (requirements.txt 3c). Folder-based
/// exclusion is recursive and primary; Safe Haven is a single catch-all folder.
/// Both are excluded from scanning entirely, not just from deletion.
/// </summary>
public sealed partial class ProtectedFoldersViewModel : ObservableObject
{
    private readonly ProtectedFolderRepository _protectedFolders;
    private readonly SettingsRepository _settings;
    private readonly Func<string?> _browseForFolder;

    public ProtectedFoldersViewModel(
        ProtectedFolderRepository protectedFolders,
        SettingsRepository settings,
        Func<string?> browseForFolder)
    {
        _protectedFolders = protectedFolders;
        _settings = settings;
        _browseForFolder = browseForFolder;
        SafeHavenPath = _settings.GetString(PathProtectionService.SafeHavenPathSettingKey) ?? string.Empty;
        Refresh();
    }

    public ObservableCollection<ProtectedFolder> Folders { get; } = new();

    [ObservableProperty]
    private string _safeHavenPath;

    [RelayCommand]
    private void Refresh()
    {
        Folders.Clear();
        foreach (var folder in _protectedFolders.GetAll())
        {
            Folders.Add(folder);
        }
    }

    [RelayCommand]
    private void AddFolder()
    {
        var path = _browseForFolder();
        if (!string.IsNullOrWhiteSpace(path))
        {
            _protectedFolders.Add(path);
            Refresh();
        }
    }

    [RelayCommand]
    private void RemoveFolder(ProtectedFolder folder)
    {
        _protectedFolders.Remove(folder.Id);
        Refresh();
    }

    [RelayCommand]
    private void BrowseSafeHaven()
    {
        var path = _browseForFolder();
        if (!string.IsNullOrWhiteSpace(path))
        {
            SafeHavenPath = path;
            _settings.SetString(PathProtectionService.SafeHavenPathSettingKey, path);
        }
    }
}
