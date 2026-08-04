using System.Security.Cryptography;
using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Two-stage duplicate detection (requirements.txt 3e): group by file size first
/// (cheap), then SHA-256 hash only files that share a size with at least one other
/// file - avoids hashing every file on the drive. On-demand, user-selected roots.
/// Always manual-review tier - never routed through whitelist auto-clean.
/// </summary>
public sealed class DuplicateFileFinder
{
    private readonly PathProtectionService _protection;

    public DuplicateFileFinder(PathProtectionService protection) => _protection = protection;

    public IReadOnlyList<DuplicateFileGroup> FindDuplicates(IEnumerable<string> roots)
    {
        var bySize = new Dictionary<long, List<string>>();

        foreach (var root in roots)
        {
            foreach (var file in FileSystemWalker.EnumerateFilesSafely(root))
            {
                if (_protection.IsProtected(file))
                {
                    continue;
                }

                long size;
                try
                {
                    size = new FileInfo(file).Length;
                }
                catch (IOException)
                {
                    continue;
                }

                if (size == 0)
                {
                    continue; // empty files aren't meaningful "duplicates"
                }

                if (!bySize.TryGetValue(size, out var list))
                {
                    list = new List<string>();
                    bySize[size] = list;
                }

                list.Add(file);
            }
        }

        var results = new List<DuplicateFileGroup>();
        foreach (var (size, candidates) in bySize)
        {
            if (candidates.Count < 2)
            {
                continue; // only one file at this size - cannot be a duplicate
            }

            var byHash = new Dictionary<string, List<string>>();
            foreach (var file in candidates)
            {
                string hash;
                try
                {
                    hash = ComputeHash(file);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                if (!byHash.TryGetValue(hash, out var list))
                {
                    list = new List<string>();
                    byHash[hash] = list;
                }

                list.Add(file);
            }

            foreach (var (hash, files) in byHash)
            {
                if (files.Count >= 2)
                {
                    results.Add(new DuplicateFileGroup(hash, size, files));
                }
            }
        }

        return results;
    }

    private static string ComputeHash(string path)
    {
        using var stream = File.OpenRead(path);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes);
    }
}
