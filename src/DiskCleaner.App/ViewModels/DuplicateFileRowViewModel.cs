using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

public sealed partial class DuplicateFileRowViewModel : ObservableObject
{
    private readonly Action<DuplicateFileRowViewModel> _onQuarantine;

    public DuplicateFileRowViewModel(string path, long sizeBytes, Action<DuplicateFileRowViewModel> onQuarantine)
    {
        Path = path;
        SizeBytes = sizeBytes;
        _onQuarantine = onQuarantine;
    }

    public string Path { get; }
    public long SizeBytes { get; }

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

public sealed class DuplicateGroupRowViewModel
{
    public DuplicateGroupRowViewModel(DuplicateFileGroup group, Action<DuplicateFileRowViewModel> onQuarantine)
    {
        SizeEachDisplay = Core.Formatting.FileSizeFormatter.Format(group.SizeBytesEach);
        WastedDisplay = Core.Formatting.FileSizeFormatter.Format(group.WastedBytes);
        Files = group.FilePaths
            .Select(p => new DuplicateFileRowViewModel(p, group.SizeBytesEach, onQuarantine))
            .ToList();
    }

    public string SizeEachDisplay { get; }
    public string WastedDisplay { get; }
    public IReadOnlyList<DuplicateFileRowViewModel> Files { get; }
}
