using System.Windows;
using DiskCleaner.App.ViewModels;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.Views;

public partial class CleanupReportWindow : Window
{
    public CleanupReportWindow(CleanupReport report)
    {
        InitializeComponent();
        DataContext = new CleanupReportViewModel(report);
    }

    private void OnOkClicked(object sender, RoutedEventArgs e) => Close();
}
