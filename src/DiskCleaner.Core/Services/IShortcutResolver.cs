namespace DiskCleaner.Core.Services;

/// <summary>
/// Resolves a .lnk shortcut's target path. Real resolution needs Windows shell COM
/// interop, which lives in the App project (Windows-specific); Core only depends on
/// this interface so the organizer's rule logic stays unit-testable.
/// </summary>
public interface IShortcutResolver
{
    /// <returns>The shortcut's target path, or null if it can't be resolved.</returns>
    string? ResolveTargetPath(string shortcutPath);
}
