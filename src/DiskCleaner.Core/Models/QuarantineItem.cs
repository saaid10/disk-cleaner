namespace DiskCleaner.Core.Models;

/// <summary>
/// A file moved to quarantine (requirements.txt 3b): recoverable for 7 days before
/// permanent deletion.
/// </summary>
public sealed record QuarantineItem(
    long Id,
    string OriginalPath,
    string QuarantinePath,
    JunkCategory Category,
    long SizeBytes,
    DateTime DeletedAtUtc,
    DateTime ExpiresAtUtc,
    bool Restored,
    bool Purged)
{
    public bool IsExpired(DateTime nowUtc) => !Restored && !Purged && nowUtc >= ExpiresAtUtc;

    public TimeSpan TimeRemaining(DateTime nowUtc) =>
        ExpiresAtUtc > nowUtc ? ExpiresAtUtc - nowUtc : TimeSpan.Zero;
}
