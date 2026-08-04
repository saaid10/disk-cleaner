using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

public sealed partial class JunkResultRowViewModel : ObservableObject
{
    private readonly JunkScanResult _result;
    private readonly Action<JunkResultRowViewModel> _onQuarantine;

    public JunkResultRowViewModel(JunkScanResult result, Action<JunkResultRowViewModel> onQuarantine)
    {
        _result = result;
        _onQuarantine = onQuarantine;
    }

    public JunkScanResult Result => _result;
    public string Path => _result.Path;
    public JunkCategory Category => _result.Category;
    public string SizeDisplay => FileSizeFormatter.Format(_result.SizeBytes);
    public bool WhitelistEligible => _result.WhitelistEligible;

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
