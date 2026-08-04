using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

public sealed partial class QuarantineRowViewModel : ObservableObject
{
    private readonly Action<QuarantineRowViewModel> _onRestore;

    public QuarantineRowViewModel(QuarantineItem item, Action<QuarantineRowViewModel> onRestore)
    {
        Item = item;
        _onRestore = onRestore;
    }

    public QuarantineItem Item { get; }
    public long Id => Item.Id;
    public string OriginalPath => Item.OriginalPath;
    public JunkCategory Category => Item.Category;
    public string SizeDisplay => FileSizeFormatter.Format(Item.SizeBytes);

    public string TimeRemainingDisplay
    {
        get
        {
            var remaining = Item.TimeRemaining(DateTime.UtcNow);
            return remaining <= TimeSpan.Zero ? "Expiring soon" : $"{remaining.Days}d {remaining.Hours}h remaining";
        }
    }

    [ObservableProperty]
    private string _statusText = string.Empty;

    [RelayCommand]
    private void Restore() => _onRestore(this);
}
