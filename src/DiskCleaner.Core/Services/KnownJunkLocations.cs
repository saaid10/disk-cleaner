namespace DiskCleaner.Core.Services;

/// <summary>
/// Resolves the real OS paths for the fixed-location junk categories from
/// requirements.txt 3g (browser cache, Windows temp, Windows Update leftovers).
/// Kept separate from JunkScanner so the scanning/sizing logic can be unit tested
/// against arbitrary temp directories instead of the real system paths.
/// </summary>
public static class KnownJunkLocations
{
    public static IReadOnlyList<string> WindowsTempPaths()
    {
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
        return new[]
        {
            Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
            Path.Combine(systemRoot, "Temp"),
        };
    }

    public static IReadOnlyList<string> BrowserCachePaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var paths = new List<string>
        {
            Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache"),
            Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache"),
        };

        var firefoxProfiles = Path.Combine(roamingAppData, "Mozilla", "Firefox", "Profiles");
        if (Directory.Exists(firefoxProfiles))
        {
            foreach (var profile in Directory.EnumerateDirectories(firefoxProfiles))
            {
                paths.Add(Path.Combine(profile, "cache2"));
            }
        }

        return paths;
    }

    public static IReadOnlyList<string> WindowsUpdateLeftoverPaths()
    {
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
        var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
        return new[]
        {
            Path.Combine(systemDrive + Path.DirectorySeparatorChar, "Windows.old"),
            Path.Combine(systemRoot, "SoftwareDistribution", "Download"),
        };
    }

    /// <summary>
    /// The user's Downloads folder. Uses the default location
    /// (%USERPROFILE%\Downloads) - there's no managed .NET API for this folder
    /// (unlike Desktop/Documents), and resolving a user-relocated Downloads folder
    /// would need Windows Known Folder COM interop. Not worth the complexity for a
    /// personal tool; the default location covers the vast majority of setups.
    /// </summary>
    public static string DownloadsFolderPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads");
}
