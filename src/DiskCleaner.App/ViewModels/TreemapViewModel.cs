using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Disk Scan / Treemap screen (requirements.txt 3f): visual drive explorer, drill
/// into folders by clicking, breadcrumb-style "up" navigation. Loading is async -
/// summing folder sizes recursively (DirectoryUsageService) can take real time
/// against a full drive root, and blocking the UI thread there froze the whole app
/// during navigation (caught by the final verification pass).
/// </summary>
public sealed partial class TreemapViewModel : ObservableObject
{
    private const double LayoutWidth = 760;
    private const double LayoutHeight = 460;

    private readonly DirectoryUsageService _usage;

    public TreemapViewModel(DirectoryUsageService usage, DriveSpaceService driveSpace)
    {
        _usage = usage;
        var firstDrive = driveSpace.GetDrives().FirstOrDefault();
        _currentPath = firstDrive?.Name ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _ = LoadCurrentPathAsync();
    }

    public ObservableCollection<TreemapRectViewModel> Rects { get; } = new();

    [ObservableProperty]
    private string _currentPath;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private Task Refresh() => LoadCurrentPathAsync();

    [RelayCommand]
    private Task NavigateUp()
    {
        var parent = Directory.GetParent(CurrentPath.TrimEnd('\\', '/'));
        if (parent is null)
        {
            return Task.CompletedTask;
        }

        CurrentPath = parent.FullName;
        return LoadCurrentPathAsync();
    }

    [RelayCommand]
    private Task NavigateInto(TreemapRectViewModel? rect)
    {
        if (rect is null || !rect.IsFolder)
        {
            return Task.CompletedTask;
        }

        CurrentPath = Path.Combine(CurrentPath, rect.Label);
        return LoadCurrentPathAsync();
    }

    private async Task LoadCurrentPathAsync()
    {
        IsLoading = true;
        var requestedPath = CurrentPath;

        var nodes = await Task.Run(() => _usage.GetChildNodes(requestedPath));

        if (requestedPath != CurrentPath)
        {
            return; // user navigated elsewhere while this load was in flight - discard stale result
        }

        var layout = TreemapLayoutEngine.Layout(nodes, LayoutWidth, LayoutHeight);
        Rects.Clear();
        foreach (var rect in layout)
        {
            Rects.Add(new TreemapRectViewModel(rect));
        }

        IsLoading = false;
    }
}
