namespace DiskCleaner.Core.Models;

/// <summary>
/// Outcome of quarantining a folder's contents item-by-item (requirements.txt 3g) -
/// partial success is expected and fine: locked/in-use files (common in Windows
/// Temp/browser cache) are skipped individually instead of failing the whole folder.
/// </summary>
public sealed record FolderQuarantineResult(int SucceededCount, long SucceededBytes, int FailedCount);
