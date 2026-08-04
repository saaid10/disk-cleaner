using System.Windows;
using DiskCleaner.App.ViewModels;

namespace DiskCleaner.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(App app)
    {
        InitializeComponent();
        DataContext = new DashboardViewModel(app.DriveSpace);
    }
}
