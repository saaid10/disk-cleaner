namespace DiskCleaner.Core.Models;

public enum OrganizerMatchKind
{
    Extension,
    FilenameKeyword,
    ShortcutTargetPathContains,
}

/// <summary>
/// A Desktop Organizer sorting rule (requirements.txt 3d) - either a built-in preset
/// (game-platform detection) or a user-added custom rule, both stored the same way.
/// </summary>
public sealed record OrganizerRule(long Id, OrganizerMatchKind MatchKind, string Pattern, string TargetFolder);
