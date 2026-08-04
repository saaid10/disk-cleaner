using System.IO;
using System.Windows;
using DiskCleaner.App.Services;
using DiskCleaner.Core.Data;
using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App;

/// <summary>
/// Composition root: builds the shared database/services once, runs a system tray
/// icon so scheduled auto-clean can operate without the main window open
/// (requirements.txt 2), and keeps the app alive in the tray when the window closes.
/// </summary>
public partial class App : System.Windows.Application
{
    private const string DbFileName = "diskcleaner.db";
    private const string QuarantineFolderName = "Quarantine";

    private AppDatabase _database = null!;
    private TrayIconService _trayIcon = null!;
    private AutoCleanBackgroundService _autoCleanBackground = null!;
    private MainWindow? _mainWindow;

    public SettingsRepository Settings { get; private set; } = null!;
    public ProtectedFolderRepository ProtectedFolders { get; private set; } = null!;
    public QuarantineRepository QuarantineRepository { get; private set; } = null!;
    public OrganizerRuleRepository OrganizerRules { get; private set; } = null!;
    public CleanupHistoryRepository CleanupHistory { get; private set; } = null!;
    public AppLogger Logger { get; private set; } = null!;

    public PathProtectionService Protection { get; private set; } = null!;
    public DriveSpaceService DriveSpace { get; private set; } = null!;
    public JunkScanner JunkScanner { get; private set; } = null!;
    public DuplicateFileFinder DuplicateFinder { get; private set; } = null!;
    public LargeOldFileFinder LargeOldFileFinder { get; private set; } = null!;
    public DirectoryUsageService DirectoryUsage { get; private set; } = null!;
    public DesktopOrganizerService Organizer { get; private set; } = null!;
    public QuarantineService Quarantine { get; private set; } = null!;
    public AutoCleanRunner AutoCleanRunner { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiskCleaner");
        Directory.CreateDirectory(appDataDir);

        _database = new AppDatabase(Path.Combine(appDataDir, DbFileName));
        Settings = new SettingsRepository(_database);
        ProtectedFolders = new ProtectedFolderRepository(_database);
        QuarantineRepository = new QuarantineRepository(_database);
        OrganizerRules = new OrganizerRuleRepository(_database);
        CleanupHistory = new CleanupHistoryRepository(_database);
        Logger = new AppLogger();

        Protection = new PathProtectionService(ProtectedFolders, Settings);
        DriveSpace = new DriveSpaceService();
        JunkScanner = new JunkScanner(Protection);
        DuplicateFinder = new DuplicateFileFinder(Protection);
        LargeOldFileFinder = new LargeOldFileFinder(Protection);
        DirectoryUsage = new DirectoryUsageService(Protection);
        Organizer = new DesktopOrganizerService(new WshShortcutResolver());
        Quarantine = new QuarantineService(QuarantineRepository, Path.Combine(appDataDir, QuarantineFolderName));
        AutoCleanRunner = new AutoCleanRunner(Quarantine, CleanupHistory, Settings);

        _trayIcon = new TrayIconService();
        _trayIcon.OpenRequested += (_, _) => ShowMainWindow();
        _trayIcon.ExitRequested += (_, _) => ExitApplication();

        _autoCleanBackground = new AutoCleanBackgroundService(
            AutoCleanRunner,
            JunkScanner,
            Settings,
            Logger,
            result => _trayIcon.ShowBalloon(
                "Disk Cleaner",
                $"Auto-clean reclaimed {FileSizeFormatter.Format(result.BytesQuarantined)}."));

        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            _mainWindow = new MainWindow(this);
            _mainWindow.Closing += (_, args) =>
            {
                // Keep running in the tray (req 2) instead of exiting on window close.
                args.Cancel = true;
                _mainWindow!.Hide();
            };
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApplication()
    {
        _autoCleanBackground.Dispose();
        _trayIcon.Dispose();
        Shutdown();
    }
}
