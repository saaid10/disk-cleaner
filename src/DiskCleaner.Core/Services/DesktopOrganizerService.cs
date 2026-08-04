using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Desktop Organizer (requirements.txt 3d): on-demand, two-layer sorting.
/// Base layer is extension-based type mapping (shared FileTypeCategorizer); the
/// override layer detects game-platform .exe/.lnk before falling back to the
/// generic bucket. Only loose files directly in the folder are touched - existing
/// subfolders are left alone.
/// </summary>
public sealed class DesktopOrganizerService
{
    private static readonly HashSet<string> GameInstallerKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "steamsetup",
        "epicgameslauncherinstaller",
        "epicinstaller",
        "gog_galaxy",
        "goggalaxysetup",
        "battlenetsetup",
        "originsetup",
        "originthinsetup",
    };

    private static readonly string[] GamePlatformPathMarkers =
    {
        @"\Steam\steamapps\common\",
        @"\Epic Games\",
        @"\GOG Games\",
        @"\Battle.net\",
        @"\Origin Games\",
    };

    private const string GamesFolder = "Games";
    private const string ProgramsFolder = "Programs";
    private const string OtherFolder = "Other";

    private readonly IShortcutResolver _shortcutResolver;

    public DesktopOrganizerService(IShortcutResolver shortcutResolver) => _shortcutResolver = shortcutResolver;

    /// <returns>The target subfolder name this file should be sorted into.</returns>
    public string DetermineTargetFolder(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        if (string.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            var target = _shortcutResolver.ResolveTargetPath(filePath);
            var isGameShortcut = target is not null
                && GamePlatformPathMarkers.Any(marker => target.Contains(marker, StringComparison.OrdinalIgnoreCase));
            return isGameShortcut ? GamesFolder : OtherFolder;
        }

        if (string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var isGameInstaller = GameInstallerKeywords.Any(
                keyword => nameWithoutExtension.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            return isGameInstaller ? GamesFolder : ProgramsFolder;
        }

        return FileTypeCategorizer.Categorize(filePath) switch
        {
            FileTypeCategory.Documents => "Documents",
            FileTypeCategory.Images => "Images",
            FileTypeCategory.Archives => "Archives",
            FileTypeCategory.Videos => "Videos",
            FileTypeCategory.Audio => "Audio",
            FileTypeCategory.Installers => ProgramsFolder,
            _ => OtherFolder,
        };
    }

    /// <summary>
    /// Plans moves for every loose file directly inside <paramref name="folderPath"/>.
    /// Existing subfolders are never descended into or touched.
    /// </summary>
    public IReadOnlyList<OrganizeAction> PlanOrganize(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return Array.Empty<OrganizeAction>();
        }

        var actions = new List<OrganizeAction>();
        foreach (var file in Directory.EnumerateFiles(folderPath))
        {
            var targetFolderName = DetermineTargetFolder(file);
            var destination = Path.Combine(folderPath, targetFolderName, Path.GetFileName(file));
            actions.Add(new OrganizeAction(file, destination));
        }

        return actions;
    }

    /// <summary>
    /// Applies a plan. Skips (does not overwrite) any destination that already
    /// exists - conflicts are left in place rather than silently clobbered.
    /// </summary>
    public OrganizeBatch Execute(IReadOnlyList<OrganizeAction> actions, DateTime? nowUtc = null)
    {
        var applied = new List<OrganizeAction>();
        foreach (var action in actions)
        {
            if (File.Exists(action.DestinationPath))
            {
                continue;
            }

            var targetDir = Path.GetDirectoryName(action.DestinationPath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Move(action.SourcePath, action.DestinationPath);
            applied.Add(action);
        }

        return new OrganizeBatch(applied, nowUtc ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Reverses a batch - moves everything back to where it came from. Skips any
    /// action whose destination no longer exists or whose original spot is occupied.
    /// </summary>
    public void Undo(OrganizeBatch batch)
    {
        foreach (var action in batch.Actions.Reverse())
        {
            if (!File.Exists(action.DestinationPath) || File.Exists(action.SourcePath))
            {
                continue;
            }

            File.Move(action.DestinationPath, action.SourcePath);
        }
    }
}
