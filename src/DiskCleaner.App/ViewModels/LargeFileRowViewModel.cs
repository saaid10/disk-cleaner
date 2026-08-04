using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

public sealed partial class LargeFileRowViewModel : ObservableObject
{
    private readonly LargeFileResult _result;
    private readonly Action<LargeFileRowViewModel> _onQuarantine;

    public LargeFileRowViewModel(LargeFileResult result, Action<LargeFileRowViewModel> onQuarantine)
    {
        _result = result;
        _onQuarantine = onQuarantine;
    }

    public string Path => _result.Path;
    public long SizeBytes => _result.SizeBytes;
    public string SizeDisplay => FileSizeFormatter.Format(_result.SizeBytes);
    public DateTime LastAccessedUtc => _result.LastAccessedUtc;

    [ObservableProperty]
    private bool _isQuarantined;

    [RelayCommand]
    private void Quarantine()
    {
        if (IsQuarantined)
        {
            return;
        }

        _onQuarantine(this);
        IsQuarantined = true;
    }
}
