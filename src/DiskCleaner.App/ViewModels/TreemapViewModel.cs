using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Disk Scan / Treemap screen (requirements.txt 3f): visual drive explorer, drill
/// into folders by clicking, breadcrumb-style "up" navigation. Loading streams in -
/// DirectoryUsageService reports each subfolder's size as soon as it's known (a
/// single huge folder like C:\Windows can still take real time even fully
/// parallelized, so waiting for ALL subfolders before showing anything made a drive
/// root feel frozen). IsCalculating stays true until every subfolder is done, but
/// Rects fills in progressively rather than gating on it.
/// </summary>
public sealed partial class TreemapViewModel : ObservableObject
{
    private const double LayoutWidth = 760;
    private const double LayoutHeight = 460;

    private readonly DirectoryUsageService _usage;
    private readonly DriveSpaceService _driveSpace;
    private readonly SettingsRepository _settings;
    private readonly List<TreemapNode> _provisionalNodes = new();

    public TreemapViewModel(DirectoryUsageService usage, DriveSpaceService driveSpace, SettingsRepository settings)
    {
        _usage = usage;
        _driveSpace = driveSpace;
        _settings = settings;
        var firstDrive = driveSpace.GetDrives().FirstOrDefault();
        _currentPath = firstDrive?.Name ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        RefreshDriveSpaceSummary();
        _ = LoadCurrentPathAsync();
    }

    public ObservableCollection<TreemapRectViewModel> Rects { get; } = new();

    [ObservableProperty]
    private string _currentPath;

    /// <summary>True while any subfolder is still being sized - Rects may already have content.</summary>
    [ObservableProperty]
    private bool _isCalculating;

    [ObservableProperty]
    private string _driveSpaceSummary = string.Empty;

    partial void OnCurrentPathChanged(string value) => RefreshDriveSpaceSummary();

    private void RefreshDriveSpaceSummary()
    {
        var root = Path.GetPathRoot(CurrentPath);
        var drive = string.IsNullOrEmpty(root)
            ? null
            : _driveSpace.GetDrives().FirstOrDefault(d => string.Equals(d.Name, root, StringComparison.OrdinalIgnoreCase));

        DriveSpaceSummary = drive is null
            ? string.Empty
            : $"{drive.VolumeLabel} - Used: {FileSizeFormatter.Format(drive.UsedBytes)} | Free: {FileSizeFormatter.Format(drive.FreeBytes)} | Total: {FileSizeFormatter.Format(drive.TotalBytes)}";
    }

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
        IsCalculating = true;
        var requestedPath = CurrentPath;
        var minSizeMb = _settings.GetInt(ScanScopeSettingsKeys.TreemapMinSizeMb, ScanScopeSettingsKeys.DefaultTreemapMinSizeMb);
        var minSizeBytes = minSizeMb * 1024L * 1024L;

        lock (_provisionalNodes)
        {
            _provisionalNodes.Clear();
        }

        Rects.Clear();

        // onSubfolderReady fires from background worker threads (Parallel.ForEach) -
        // marshal to the UI thread before touching Rects/_provisionalNodes.
        void OnSubfolderReady(TreemapNode node)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                if (requestedPath != CurrentPath)
                {
                    return; // stale - user navigated elsewhere while this load was in flight
                }

                _provisionalNodes.Add(node);
                RenderNodes(_provisionalNodes);
            });
        }

        var nodes = await Task.Run(() => _usage.GetChildNodes(requestedPath, minSizeBytes, OnSubfolderReady));

        if (requestedPath != CurrentPath)
        {
            return; // discard stale final result too
        }

        RenderNodes(nodes); // authoritative final list (includes file nodes + small-item bucketing)
        IsCalculating = false;
    }

    private void RenderNodes(IReadOnlyList<TreemapNode> nodes)
    {
        var layout = TreemapLayoutEngine.Layout(nodes, LayoutWidth, LayoutHeight);
        Rects.Clear();
        foreach (var rect in layout)
        {
            Rects.Add(new TreemapRectViewModel(rect));
        }
    }
}
