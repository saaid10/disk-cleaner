using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class TreemapLayoutEngineTests
{
    [Fact]
    public void Layout_EmptyNodes_ReturnsEmpty()
    {
        Assert.Empty(TreemapLayoutEngine.Layout(Array.Empty<TreemapNode>(), 100, 100));
    }

    [Fact]
    public void Layout_RectanglesAreProportionalToSize()
    {
        var nodes = new[]
        {
            new TreemapNode("big", 75, FileTypeCategory.Videos),
            new TreemapNode("small", 25, FileTypeCategory.Documents),
        };

        var rects = TreemapLayoutEngine.Layout(nodes, width: 100, height: 50);

        var big = rects.Single(r => r.Node.Label == "big");
        var small = rects.Single(r => r.Node.Label == "small");
        Assert.Equal(75, big.Width, precision: 5);
        Assert.Equal(25, small.Width, precision: 5);
        Assert.Equal(50, big.Height);
        Assert.Equal(50, small.Height);
    }

    [Fact]
    public void Layout_RectanglesDoNotOverlap_AndFillTheAxis()
    {
        var nodes = new[]
        {
            new TreemapNode("a", 10, FileTypeCategory.Other),
            new TreemapNode("b", 20, FileTypeCategory.Other),
            new TreemapNode("c", 30, FileTypeCategory.Other),
        };

        var rects = TreemapLayoutEngine.Layout(nodes, width: 100, height: 40).OrderBy(r => r.X).ToList();

        for (var i = 1; i < rects.Count; i++)
        {
            Assert.Equal(rects[i - 1].X + rects[i - 1].Width, rects[i].X, precision: 5);
        }

        var last = rects[^1];
        Assert.Equal(100, last.X + last.Width, precision: 5);
    }

    [Fact]
    public void Layout_TallerThanWide_SlicesVertically()
    {
        var nodes = new[]
        {
            new TreemapNode("a", 1, FileTypeCategory.Other),
            new TreemapNode("b", 1, FileTypeCategory.Other),
        };

        var rects = TreemapLayoutEngine.Layout(nodes, width: 10, height: 100);

        Assert.All(rects, r => Assert.Equal(10, r.Width));
        Assert.All(rects, r => Assert.Equal(50, r.Height));
    }

    [Fact]
    public void Layout_ZeroTotalSize_ReturnsEmpty()
    {
        var nodes = new[] { new TreemapNode("a", 0, FileTypeCategory.Other) };
        Assert.Empty(TreemapLayoutEngine.Layout(nodes, 100, 100));
    }
}
