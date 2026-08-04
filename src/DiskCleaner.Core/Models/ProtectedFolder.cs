namespace DiskCleaner.Core.Models;

/// <summary>
/// A user-marked protected folder (requirements.txt 3c). Recursive by default -
/// everything under it is excluded from scanning and deletion.
/// </summary>
public sealed record ProtectedFolder(long Id, string Path);
