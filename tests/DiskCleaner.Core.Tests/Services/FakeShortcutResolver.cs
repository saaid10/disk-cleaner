using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

internal sealed class FakeShortcutResolver : IShortcutResolver
{
    private readonly Dictionary<string, string> _targets = new(StringComparer.OrdinalIgnoreCase);

    public void SetTarget(string shortcutPath, string targetPath) => _targets[shortcutPath] = targetPath;

    public string? ResolveTargetPath(string shortcutPath) =>
        _targets.TryGetValue(shortcutPath, out var target) ? target : null;
}
