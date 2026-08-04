using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DiskCleaner.App.ViewModels;

/// <summary>
/// Navigation shell (requirements.txt 3k): sidebar switching between screens. Each
/// screen ViewModel is created lazily and cached so switching tabs doesn't re-scan.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly App _app;
    private readonly Func<string?> _browseForFolder;

    private DashboardViewModel? _dashboard;
    private TreemapViewModel? _treemap;
    private CleanupSuggestionsViewModel? _cleanupSuggestions;
    private DuplicatesViewModel? _duplicates;
    private LargeOldFilesViewModel? _largeOldFiles;
    private QuarantineViewModel? _quarantine;
    private OrganizerViewModel? _organizer;
    private ProtectedFoldersViewModel? _protectedFolders;
    private SettingsViewModel? _settings;

    public ShellViewModel(App app, Func<string?> browseForFolder)
    {
        _app = app;
        _browseForFolder = browseForFolder;
        CurrentView = Dashboard;
    }

    [ObservableProperty]
    private object? _currentView;

    private DashboardViewModel Dashboard => _dashboard ??= new DashboardViewModel(_app.DriveSpace, _app.Quarantine, _app.Settings);

    private TreemapViewModel Treemap => _treemap ??= new TreemapViewModel(_app.DirectoryUsage, _app.DriveSpace);

    private CleanupSuggestionsViewModel CleanupSuggestions => _cleanupSuggestions ??= new CleanupSuggestionsViewModel(
        _app.JunkScanner, _app.Quarantine, _app.Settings, _app.CleanupHistory);

    private DuplicatesViewModel Duplicates => _duplicates ??= new DuplicatesViewModel(
        _app.DuplicateFinder, _app.Quarantine, _app.Settings, _app.CleanupHistory);

    private LargeOldFilesViewModel LargeOldFiles => _largeOldFiles ??= new LargeOldFilesViewModel(
        _app.LargeOldFileFinder, _app.Quarantine, _app.Settings, _app.CleanupHistory);

    private QuarantineViewModel Quarantine => _quarantine ??= new QuarantineViewModel(_app.Quarantine);

    private OrganizerViewModel Organizer => _organizer ??= new OrganizerViewModel(_app.Organizer);

    private ProtectedFoldersViewModel ProtectedFolders => _protectedFolders ??= new ProtectedFoldersViewModel(
        _app.ProtectedFolders, _app.Settings, _browseForFolder);

    private SettingsViewModel Settings => _settings ??= new SettingsViewModel(_app.Settings, _browseForFolder, _app.DriveSpace);

    [RelayCommand]
    private void ShowDashboard() => CurrentView = Dashboard;

    [RelayCommand]
    private void ShowTreemap() => CurrentView = Treemap;

    [RelayCommand]
    private void ShowCleanupSuggestions() => CurrentView = CleanupSuggestions;

    [RelayCommand]
    private void ShowDuplicates() => CurrentView = Duplicates;

    [RelayCommand]
    private void ShowLargeOldFiles() => CurrentView = LargeOldFiles;

    [RelayCommand]
    private void ShowQuarantine() => CurrentView = Quarantine;

    [RelayCommand]
    private void ShowOrganizer() => CurrentView = Organizer;

    [RelayCommand]
    private void ShowProtectedFolders() => CurrentView = ProtectedFolders;

    [RelayCommand]
    private void ShowSettings() => CurrentView = Settings;
}
