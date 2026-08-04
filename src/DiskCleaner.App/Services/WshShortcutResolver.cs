using System.IO;
using DiskCleaner.Core.Services;

namespace DiskCleaner.App.Services;

/// <summary>
/// Resolves .lnk shortcut targets via the WScript.Shell COM object (shipped with
/// Windows). Uses late-bound dynamic COM instead of an IWshRuntimeLibrary reference
/// so no extra COM reference/package is needed in the project.
/// </summary>
public sealed class WshShortcutResolver : IShortcutResolver
{
    public string? ResolveTargetPath(string shortcutPath)
    {
        if (!File.Exists(shortcutPath))
        {
            return null;
        }

        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            return null;
        }

        dynamic? shell = null;
        try
        {
            shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell!.CreateShortcut(shortcutPath);
            string targetPath = shortcut.TargetPath;
            return string.IsNullOrWhiteSpace(targetPath) ? null : targetPath;
        }
        catch (Exception) when (shell is not null)
        {
            // Malformed/inaccessible shortcuts shouldn't crash a whole organize run -
            // treat as unresolved and let the caller fall back to the generic bucket.
            return null;
        }
        finally
        {
            if (shell is not null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
            }
        }
    }
}
