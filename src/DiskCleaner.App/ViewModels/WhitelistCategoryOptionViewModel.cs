using CommunityToolkit.Mvvm.ComponentModel;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

public sealed partial class WhitelistCategoryOptionViewModel : ObservableObject
{
    public WhitelistCategoryOptionViewModel(JunkCategory category, bool isEnabled)
    {
        Category = category;
        _isEnabled = isEnabled;
    }

    public JunkCategory Category { get; }
    public string DisplayName => Category.ToString();

    [ObservableProperty]
    private bool _isEnabled;
}
