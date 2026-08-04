namespace DiskCleaner.Core.Models;

/// <summary>
/// A laid-out rectangle for one <see cref="TreemapNode"/>, in the coordinate space
/// passed to <see cref="Services.TreemapLayoutEngine.Layout"/>.
/// </summary>
public sealed record TreemapRect(TreemapNode Node, double X, double Y, double Width, double Height);
