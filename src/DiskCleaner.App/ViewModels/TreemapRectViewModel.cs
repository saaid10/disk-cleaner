using DiskCleaner.Core.Formatting;
using DiskCleaner.Core.Models;

namespace DiskCleaner.App.ViewModels;

public sealed class TreemapRectViewModel
{
    public TreemapRectViewModel(TreemapRect rect)
    {
        Label = rect.Node.Label;
        Category = rect.Node.Category;
        SizeDisplay = FileSizeFormatter.Format(rect.Node.SizeBytes);
        X = rect.X;
        Y = rect.Y;
        Width = rect.Width;
        Height = rect.Height;
    }

    public string Label { get; }
    public FileTypeCategory Category { get; }
    public string SizeDisplay { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public bool IsFolder => Category == FileTypeCategory.Folder;
}
