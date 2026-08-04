namespace DiskCleaner.Core.Models;

/// <summary>
/// One row of the before/after cleanup report (requirements.txt 3j).
/// </summary>
public sealed record CleanupHistoryEntry(
    long Id,
    DateTime TimestampUtc,
    JunkCategory Category,
    long BytesReclaimed,
    int ItemCount,
    string Description);
