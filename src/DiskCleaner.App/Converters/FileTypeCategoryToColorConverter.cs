using System.Globalization;
using System.Windows.Data;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.Converters;

/// <summary>
/// Colors treemap rectangles by the shared file-type category (requirements.txt 3f).
/// </summary>
public sealed class FileTypeCategoryToColorConverter : IValueConverter
{
    private static readonly Dictionary<FileTypeCategory, System.Windows.Media.Brush> Colors = new()
    {
        [FileTypeCategory.Documents] = Brush(0x4C, 0x8B, 0xF5),
        [FileTypeCategory.Images] = Brush(0x34, 0xA8, 0x53),
        [FileTypeCategory.Archives] = Brush(0xF6, 0xBF, 0x26),
        [FileTypeCategory.Installers] = Brush(0xEA, 0x43, 0x35),
        [FileTypeCategory.Videos] = Brush(0x9C, 0x27, 0xB0),
        [FileTypeCategory.Audio] = Brush(0x00, 0xAC, 0xC1),
        [FileTypeCategory.Folder] = Brush(0x90, 0x90, 0x90),
        [FileTypeCategory.Other] = Brush(0xBD, 0xBD, 0xBD),
    };

    private static System.Windows.Media.Brush Brush(byte r, byte g, byte b) =>
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FileTypeCategory category && Colors.TryGetValue(category, out var brush)
            ? brush
            : Colors[FileTypeCategory.Other];

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
