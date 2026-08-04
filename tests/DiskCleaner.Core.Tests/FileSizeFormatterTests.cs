using DiskCleaner.Core.Formatting;

namespace DiskCleaner.Core.Tests;

public class FileSizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(480, "480 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1024 * 312, "312 KB")]
    [InlineData(1024 * 1024, "1.00 MB")]
    [InlineData((long)(1024 * 1024 * 487.1), "487.10 MB")]
    [InlineData(1024L * 1024 * 1024, "1.00 GB")]
    [InlineData((long)(1024L * 1024 * 1024 * 2.34), "2.34 GB")]
    public void Format_PicksLargestUnitThatKeepsValueAtLeastOne(long bytes, string expected)
    {
        Assert.Equal(expected, FileSizeFormatter.Format(bytes));
    }

    [Fact]
    public void Format_NegativeBytes_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FileSizeFormatter.Format(-1));
    }
}
