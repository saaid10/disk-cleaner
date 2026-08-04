using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Desktop Organizer screen (requirements.txt 3d): on-demand sort of loose desktop
/// files into type/game-platform folders, with a one-step undo of the last run.
/// </summary>
public sealed partial class OrganizerViewModel : ObservableObject
{
    private readonly DesktopOrganizerService _organizer;
    private readonly string _desktopPath;
    private OrganizeBatch? _lastBatch;

    public OrganizerViewModel(DesktopOrganizerService organizer)
    {
        _organizer = organizer;
        _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    public string DesktopPath => _desktopPath;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private bool _canUndo;

    [RelayCommand]
    private void OrganizeNow()
    {
        var plan = _organizer.PlanOrganize(_desktopPath);
        if (plan.Count == 0)
        {
            StatusText = "Nothing to organize - no loose files on the Desktop.";
            return;
        }

        _lastBatch = _organizer.Execute(plan);
        CanUndo = _lastBatch.Actions.Count > 0;
        StatusText = $"Moved {_lastBatch.Actions.Count} file(s) into category folders.";
    }

    [RelayCommand]
    private void UndoLast()
    {
        if (_lastBatch is null)
        {
            return;
        }

        _organizer.Undo(_lastBatch);
        StatusText = $"Undid the last organize ({_lastBatch.Actions.Count} file(s) moved back).";
        _lastBatch = null;
        CanUndo = false;
    }
}
