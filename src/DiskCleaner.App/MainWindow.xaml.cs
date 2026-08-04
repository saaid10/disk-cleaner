using System.Windows;
using System.Windows.Forms;
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
        DataContext = new ShellViewModel(app, BrowseForFolder);
    }

    private static string? BrowseForFolder()
    {
        using var dialog = new FolderBrowserDialog { ShowNewFolderButton = true };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }
}
