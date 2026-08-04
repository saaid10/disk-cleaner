using DiskCleaner.Core.Data;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Folder-based exclusion + Safe Haven (requirements.txt 3c). Used by every scanner
/// so protected content is excluded from scanning entirely, not just from deletion.
/// Folder protection is recursive by default.
/// </summary>
public sealed class PathProtectionService
{
    public const string SafeHavenPathSettingKey = "SafeHavenPath";

    private readonly ProtectedFolderRepository _protectedFolders;
    private readonly SettingsRepository _settings;

    public PathProtectionService(ProtectedFolderRepository protectedFolders, SettingsRepository settings)
    {
        _protectedFolders = protectedFolders;
        _settings = settings;
    }

    public bool IsProtected(string path)
    {
        var normalizedPath = Normalize(path);

        var safeHaven = _settings.GetString(SafeHavenPathSettingKey);
        if (!string.IsNullOrEmpty(safeHaven) && IsUnderOrEqual(normalizedPath, Normalize(safeHaven)))
        {
            return true;
        }

        foreach (var folder in _protectedFolders.GetAll())
        {
            if (IsUnderOrEqual(normalizedPath, Normalize(folder.Path)))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static bool IsUnderOrEqual(string candidate, string ancestor)
    {
        if (string.Equals(candidate, ancestor, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var ancestorWithSeparator = ancestor + Path.DirectorySeparatorChar;
        return candidate.StartsWith(ancestorWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}
