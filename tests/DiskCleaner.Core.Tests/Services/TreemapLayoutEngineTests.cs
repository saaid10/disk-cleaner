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

    [Fact]
    public void Layout_TotalAreaIsConserved()
    {
        var nodes = new[]
        {
            new TreemapNode("a", 40, FileTypeCategory.Other),
            new TreemapNode("b", 25, FileTypeCategory.Other),
            new TreemapNode("c", 15, FileTypeCategory.Other),
            new TreemapNode("d", 10, FileTypeCategory.Other),
            new TreemapNode("e", 10, FileTypeCategory.Other),
        };

        var rects = TreemapLayoutEngine.Layout(nodes, width: 760, height: 460);

        var totalArea = rects.Sum(r => r.Width * r.Height);
        Assert.Equal(760 * 460, totalArea, precision: 1);
    }

    [Fact]
    public void Layout_AllRectanglesStayWithinBounds()
    {
        var nodes = Enumerable.Range(0, 15)
            .Select(i => new TreemapNode($"item{i}", i + 1, FileTypeCategory.Other))
            .ToArray();

        var rects = TreemapLayoutEngine.Layout(nodes, width: 760, height: 460);

        Assert.All(rects, r =>
        {
            Assert.True(r.X >= -0.01);
            Assert.True(r.Y >= -0.01);
            Assert.True(r.X + r.Width <= 760.01);
            Assert.True(r.Y + r.Height <= 460.01);
        });
    }

    [Fact]
    public void Layout_ManySimilarSizedItems_AvoidsSliverRectangles()
    {
        // Regression test: the previous single-axis slice layout squeezed anything
        // past the first few items into unreadable slivers ("only 4 things visible,
        // the 4th is a letter") once a folder had more than a handful of children -
        // this is exactly the C:\ root scenario (dozens of top-level folders).
        // Squarify must keep every rectangle's aspect ratio within a sane bound.
        var nodes = Enumerable.Range(0, 25)
            .Select(i => new TreemapNode($"folder{i}", 100, FileTypeCategory.Folder))
            .ToArray();

        var rects = TreemapLayoutEngine.Layout(nodes, width: 760, height: 460);

        Assert.Equal(25, rects.Count);
        Assert.All(rects, r =>
        {
            var longSide = Math.Max(r.Width, r.Height);
            var shortSide = Math.Min(r.Width, r.Height);
            var aspectRatio = longSide / shortSide;
            Assert.True(aspectRatio < 6, $"'{r.Node.Label}' has an unreadable aspect ratio {aspectRatio:0.0}:1 ({r.Width:0.0}x{r.Height:0.0})");
        });
    }
}
