namespace DiskCleaner.Core.Models;

/// <summary>
/// One file found by the large/old file finder (requirements.txt 3f). Always
/// manual-review tier - only ever surfaced as a suggestion.
/// </summary>
public sealed record LargeFileResult(string Path, long SizeBytes, DateTime LastAccessedUtc);
