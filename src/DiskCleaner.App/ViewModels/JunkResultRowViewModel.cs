using CommunityToolkit.Mvvm.ComponentModel;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// One row in the Cleanup Suggestions checkbox list. Selection doesn't delete
/// anything by itself - the user checks the rows they want, then confirms via the
/// screen's single "Delete Selected" bulk action.
/// </summary>
public sealed partial class JunkResultRowViewModel : ObservableObject
{
    public JunkResultRowViewModel(JunkScanResult result)
    {
        Result = result;
    }

    public JunkScanResult Result { get; }
    public string Path => Result.Path;
    public JunkCategory Category => Result.Category;
    public string SizeDisplay => FileSizeFormatter.Format(Result.SizeBytes);
    public bool WhitelistEligible => Result.WhitelistEligible;
    public string LastModifiedDisplay => Result.LastModifiedUtc?.ToLocalTime().ToString("yyyy-MM-dd") ?? string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
