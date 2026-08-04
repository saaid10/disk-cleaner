namespace DiskCleaner.Core.Models;

/// <summary>
/// Type-based buckets shared by the Desktop Organizer's base sorting layer (3d) and
/// the treemap's color-coding (3f), so both features speak the same visual language.
/// </summary>
public enum FileTypeCategory
{
    Documents,
    Images,
    Archives,
    Installers,
    Videos,
    Audio,
    Other,
    /// <summary>A folder node in the treemap - aggregates mixed content, not derived from an extension.</summary>
    Folder,
}
