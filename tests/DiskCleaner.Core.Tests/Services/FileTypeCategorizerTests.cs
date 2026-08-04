using DiskCleaner.Core.Models;
using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class FileTypeCategorizerTests
{
    [Theory]
    [InlineData("report.pdf", FileTypeCategory.Documents)]
    [InlineData("photo.PNG", FileTypeCategory.Images)]
    [InlineData("backup.zip", FileTypeCategory.Archives)]
    [InlineData("Setup.exe", FileTypeCategory.Installers)]
    [InlineData("movie.mp4", FileTypeCategory.Videos)]
    [InlineData("song.mp3", FileTypeCategory.Audio)]
    [InlineData("weird.xyz123", FileTypeCategory.Other)]
    [InlineData("no-extension", FileTypeCategory.Other)]
    public void Categorize_MapsExtensionToExpectedCategory(string fileName, FileTypeCategory expected)
    {
        Assert.Equal(expected, FileTypeCategorizer.Categorize(fileName));
    }

    [Fact]
    public void Categorize_WorksWithFullPath()
    {
        Assert.Equal(FileTypeCategory.Documents, FileTypeCategorizer.Categorize(@"C:\Users\me\Desktop\invoice.docx"));
    }
}
