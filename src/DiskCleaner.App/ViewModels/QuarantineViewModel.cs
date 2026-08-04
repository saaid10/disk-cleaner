using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Quarantine screen (requirements.txt 3k/3b): active items pending permanent
/// deletion, with a per-item restore option and 7-day countdown.
/// </summary>
public sealed partial class QuarantineViewModel : ObservableObject
{
    private readonly QuarantineService _quarantine;

    public QuarantineViewModel(QuarantineService quarantine)
    {
        _quarantine = quarantine;
        Refresh();
    }

    public ObservableCollection<QuarantineRowViewModel> Items { get; } = new();

    [ObservableProperty]
    private string _statusText = string.Empty;

    [RelayCommand]
    private void Refresh()
    {
        Items.Clear();
        foreach (var item in _quarantine.GetActiveItems())
        {
            Items.Add(new QuarantineRowViewModel(item, OnRestoreRequested));
        }
    }

    [RelayCommand]
    private void PurgeExpired()
    {
        var purged = _quarantine.PurgeExpired();
        StatusText = $"Purged {purged} expired item(s).";
        Refresh();
    }

    private void OnRestoreRequested(QuarantineRowViewModel row)
    {
        try
        {
            _quarantine.Restore(row.Id);
            Items.Remove(row);
            StatusText = $"Restored {row.OriginalPath}.";
        }
        catch (QuarantineRestoreConflictException ex)
        {
            row.StatusText = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            row.StatusText = ex.Message;
        }
    }
}
