using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Disk Scan / Treemap screen (requirements.txt 3f): visual drive explorer, drill
/// into folders by clicking, breadcrumb-style "up" navigation.
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
        LoadCurrentPath();
    }

    public ObservableCollection<TreemapRectViewModel> Rects { get; } = new();

    [ObservableProperty]
    private string _currentPath;

    [RelayCommand]
    private void Refresh() => LoadCurrentPath();

    [RelayCommand]
    private void NavigateUp()
    {
        var parent = Directory.GetParent(CurrentPath.TrimEnd('\\', '/'));
        if (parent is not null)
        {
            CurrentPath = parent.FullName;
            LoadCurrentPath();
        }
    }

    [RelayCommand]
    private void NavigateInto(TreemapRectViewModel? rect)
    {
        if (rect is null || !rect.IsFolder)
        {
            return;
        }

        CurrentPath = Path.Combine(CurrentPath, rect.Label);
        LoadCurrentPath();
    }

    private void LoadCurrentPath()
    {
        Rects.Clear();
        var nodes = _usage.GetChildNodes(CurrentPath);
        var layout = TreemapLayoutEngine.Layout(nodes, LayoutWidth, LayoutHeight);
        foreach (var rect in layout)
        {
            Rects.Add(new TreemapRectViewModel(rect));
        }
    }
}
