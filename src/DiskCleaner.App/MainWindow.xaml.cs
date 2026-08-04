using System.Windows;
using DiskCleaner.App.ViewModels;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel(new DriveSpaceService());
    }
}
