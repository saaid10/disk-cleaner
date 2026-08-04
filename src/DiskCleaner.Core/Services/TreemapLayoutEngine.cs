using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Lays out treemap rectangles proportional to size (requirements.txt 3f). Simple
/// single-axis slice layout (sliced along whichever dimension is longer, largest
/// node first) rather than a full squarified algorithm - enough for a personal
/// cleanup tool's "see what's big" view without the extra complexity.
/// </summary>
public static class TreemapLayoutEngine
{
    public static IReadOnlyList<TreemapRect> Layout(IReadOnlyList<TreemapNode> nodes, double width, double height)
    {
        if (nodes.Count == 0 || width <= 0 || height <= 0)
        {
            return Array.Empty<TreemapRect>();
        }

        var totalSize = nodes.Sum(n => n.SizeBytes);
        if (totalSize <= 0)
        {
            return Array.Empty<TreemapRect>();
        }

        var ordered = nodes.OrderByDescending(n => n.SizeBytes).ToList();
        var horizontal = width >= height;
        var results = new List<TreemapRect>(ordered.Count);
        double offset = 0;

        foreach (var node in ordered)
        {
            var fraction = node.SizeBytes / (double)totalSize;
            if (horizontal)
            {
                var w = width * fraction;
                results.Add(new TreemapRect(node, offset, 0, w, height));
                offset += w;
            }
            else
            {
                var h = height * fraction;
                results.Add(new TreemapRect(node, 0, offset, width, h));
                offset += h;
            }
        }

        return results;
    }
}
