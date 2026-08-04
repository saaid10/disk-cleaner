using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Extension-based type mapping - the Desktop Organizer's base sorting layer
/// (requirements.txt 3d) before the game-platform override, and the treemap's
/// color-coding source (3f).
/// </summary>
public static class FileTypeCategorizer
{
    private static readonly Dictionary<string, FileTypeCategory> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = FileTypeCategory.Documents,
        [".doc"] = FileTypeCategory.Documents,
        [".docx"] = FileTypeCategory.Documents,
        [".xls"] = FileTypeCategory.Documents,
        [".xlsx"] = FileTypeCategory.Documents,
        [".ppt"] = FileTypeCategory.Documents,
        [".pptx"] = FileTypeCategory.Documents,
        [".txt"] = FileTypeCategory.Documents,
        [".md"] = FileTypeCategory.Documents,

        [".png"] = FileTypeCategory.Images,
        [".jpg"] = FileTypeCategory.Images,
        [".jpeg"] = FileTypeCategory.Images,
        [".gif"] = FileTypeCategory.Images,
        [".bmp"] = FileTypeCategory.Images,
        [".svg"] = FileTypeCategory.Images,
        [".webp"] = FileTypeCategory.Images,

        [".zip"] = FileTypeCategory.Archives,
        [".rar"] = FileTypeCategory.Archives,
        [".7z"] = FileTypeCategory.Archives,
        [".tar"] = FileTypeCategory.Archives,
        [".gz"] = FileTypeCategory.Archives,

        [".exe"] = FileTypeCategory.Installers,
        [".msi"] = FileTypeCategory.Installers,

        [".mp4"] = FileTypeCategory.Videos,
        [".mkv"] = FileTypeCategory.Videos,
        [".mov"] = FileTypeCategory.Videos,
        [".avi"] = FileTypeCategory.Videos,

        [".mp3"] = FileTypeCategory.Audio,
        [".wav"] = FileTypeCategory.Audio,
        [".flac"] = FileTypeCategory.Audio,
    };

    public static FileTypeCategory Categorize(string fileNameOrPath)
    {
        var extension = Path.GetExtension(fileNameOrPath);
        return ExtensionMap.TryGetValue(extension, out var category) ? category : FileTypeCategory.Other;
    }
}
