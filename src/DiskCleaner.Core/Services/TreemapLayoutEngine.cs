using DiskCleaner.Core.Models;

namespace DiskCleaner.Core.Services;

/// <summary>
/// Lays out treemap rectangles proportional to size (requirements.txt 3f), using the
/// standard "squarified" treemap algorithm (Bruls, Huizing, van Wijk 1999): items are
/// grouped into rows/columns chosen to keep each rectangle's aspect ratio as close to
/// square as possible, rather than slicing everything along a single axis. A folder
/// with dozens of children (e.g. a drive root) previously got squeezed into a single
/// strip where all but the first few items were unreadable slivers - squarifying
/// keeps most items legible regardless of how many there are.
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

        // Squarify operates in area-space: scale byte sizes so their sum equals the
        // available pixel area.
        var scale = (width * height) / totalSize;
        var areas = ordered.Select(n => n.SizeBytes * scale).ToList();

        var result = new List<TreemapRect>();
        Squarify(ordered, areas, x: 0, y: 0, width, height, result);
        return result;
    }

    private static void Squarify(
        IReadOnlyList<TreemapNode> nodes,
        IReadOnlyList<double> areas,
        double x, double y, double width, double height,
        List<TreemapRect> output)
    {
        var rx = x;
        var ry = y;
        var rw = width;
        var rh = height;
        var i = 0;

        while (i < areas.Count)
        {
            var shortSide = Math.Min(rw, rh);
            var rowEnd = i + 1;
            var rowSum = areas[i];

            // Grow the row while adding the next item doesn't worsen the row's worst
            // aspect ratio - once it would, stop and place what we have.
            while (rowEnd < areas.Count)
            {
                var candidateSum = rowSum + areas[rowEnd];
                var currentWorst = WorstAspectRatio(areas, i, rowEnd, rowSum, shortSide);
                var candidateWorst = WorstAspectRatio(areas, i, rowEnd + 1, candidateSum, shortSide);
                if (candidateWorst > currentWorst)
                {
                    break;
                }

                rowSum = candidateSum;
                rowEnd++;
            }

            if (rw >= rh)
            {
                var stripWidth = rowSum / rh;
                var itemY = ry;
                for (var k = i; k < rowEnd; k++)
                {
                    var itemHeight = areas[k] / stripWidth;
                    output.Add(new TreemapRect(nodes[k], rx, itemY, stripWidth, itemHeight));
                    itemY += itemHeight;
                }

                rx += stripWidth;
                rw -= stripWidth;
            }
            else
            {
                var stripHeight = rowSum / rw;
                var itemX = rx;
                for (var k = i; k < rowEnd; k++)
                {
                    var itemWidth = areas[k] / stripHeight;
                    output.Add(new TreemapRect(nodes[k], itemX, ry, itemWidth, stripHeight));
                    itemX += itemWidth;
                }

                ry += stripHeight;
                rh -= stripHeight;
            }

            i = rowEnd;
        }
    }

    /// <summary>
    /// The worst (largest) width:height ratio among rectangles in areas[start..end) if
    /// laid out as a strip of the given short side - lower is more square/readable.
    /// </summary>
    private static double WorstAspectRatio(IReadOnlyList<double> areas, int start, int end, double sum, double shortSide)
    {
        if (end <= start || sum <= 0 || shortSide <= 0)
        {
            return double.MaxValue;
        }

        var max = double.MinValue;
        var min = double.MaxValue;
        for (var i = start; i < end; i++)
        {
            if (areas[i] > max)
            {
                max = areas[i];
            }

            if (areas[i] < min)
            {
                min = areas[i];
            }
        }

        var shortSideSquared = shortSide * shortSide;
        var sumSquared = sum * sum;
        return Math.Max(shortSideSquared * max / sumSquared, sumSquared / (shortSideSquared * min));
    }
}
