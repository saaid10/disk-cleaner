namespace DiskCleaner.Core.Models;

/// <summary>
/// A set of two or more files with identical content (requirements.txt 3e) - same
/// size and same SHA-256 hash. Always manual-review tier; never whitelist-eligible.
/// </summary>
public sealed record DuplicateFileGroup(string ContentHash, long SizeBytesEach, IReadOnlyList<string> FilePaths)
{
    public long WastedBytes => SizeBytesEach * (FilePaths.Count - 1);
}
