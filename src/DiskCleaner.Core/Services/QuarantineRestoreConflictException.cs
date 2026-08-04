namespace DiskCleaner.Core.Services;

/// <summary>
/// Thrown when restoring a quarantined item would overwrite something already
/// sitting at the original path (e.g. the user or another process recreated it).
/// Restore must never silently clobber it.
/// </summary>
public sealed class QuarantineRestoreConflictException : Exception
{
    public QuarantineRestoreConflictException(string path)
        : base($"Cannot restore - a file or folder already exists at '{path}'.")
    {
    }
}
