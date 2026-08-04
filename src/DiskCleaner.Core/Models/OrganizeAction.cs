namespace DiskCleaner.Core.Models;

/// <summary>
/// One planned or applied move from the Desktop Organizer (requirements.txt 3d).
/// </summary>
public sealed record OrganizeAction(string SourcePath, string DestinationPath);

/// <summary>
/// A completed organize run, kept so "Undo last organize" can reverse it.
/// </summary>
public sealed record OrganizeBatch(IReadOnlyList<OrganizeAction> Actions, DateTime TimestampUtc);
