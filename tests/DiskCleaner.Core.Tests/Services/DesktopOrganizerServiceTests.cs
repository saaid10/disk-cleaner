using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class DesktopOrganizerServiceTests : IDisposable
{
    private readonly string _desktop;
    private readonly FakeShortcutResolver _shortcuts = new();
    private readonly DesktopOrganizerService _organizer;

    public DesktopOrganizerServiceTests()
    {
        _organizer = new DesktopOrganizerService(_shortcuts);
        _desktop = Path.Combine(Path.GetTempPath(), $"dc-desktop-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_desktop);
    }

    [Theory]
    [InlineData("report.pdf", "Documents")]
    [InlineData("photo.png", "Images")]
    [InlineData("archive.zip", "Archives")]
    [InlineData("movie.mp4", "Videos")]
    [InlineData("song.mp3", "Audio")]
    [InlineData("mystery.xyz", "Other")]
    public void DetermineTargetFolder_TypeBasedBaseLayer(string fileName, string expectedFolder)
    {
        Assert.Equal(expectedFolder, _organizer.DetermineTargetFolder(fileName));
    }

    [Fact]
    public void DetermineTargetFolder_PlainExe_GoesToPrograms()
    {
        Assert.Equal("Programs", _organizer.DetermineTargetFolder("random-tool.exe"));
    }

    [Fact]
    public void DetermineTargetFolder_KnownGameInstallerFilename_GoesToGames()
    {
        Assert.Equal("Games", _organizer.DetermineTargetFolder("SteamSetup.exe"));
        Assert.Equal("Games", _organizer.DetermineTargetFolder("GOG_Galaxy_2.0.exe"));
    }

    [Fact]
    public void DetermineTargetFolder_ShortcutToSteamGame_GoesToGames()
    {
        var shortcutPath = "Cyberpunk 2077.lnk";
        _shortcuts.SetTarget(shortcutPath, @"C:\Program Files (x86)\Steam\steamapps\common\Cyberpunk 2077\bin\x64\Cyberpunk2077.exe");

        Assert.Equal("Games", _organizer.DetermineTargetFolder(shortcutPath));
    }

    [Fact]
    public void DetermineTargetFolder_ShortcutToNonGameApp_GoesToGenericBucket()
    {
        var shortcutPath = "Notepad.lnk";
        _shortcuts.SetTarget(shortcutPath, @"C:\Windows\System32\notepad.exe");

        Assert.Equal("Other", _organizer.DetermineTargetFolder(shortcutPath));
    }

    [Fact]
    public void DetermineTargetFolder_UnresolvableShortcut_FallsBackToGenericBucket()
    {
        Assert.Equal("Other", _organizer.DetermineTargetFolder("broken.lnk"));
    }

    [Fact]
    public void PlanOrganize_OnlyIncludesLooseFiles_NotExistingSubfolders()
    {
        File.WriteAllText(Path.Combine(_desktop, "doc.pdf"), "x");
        Directory.CreateDirectory(Path.Combine(_desktop, "MyProjectFolder"));

        var plan = _organizer.PlanOrganize(_desktop);

        Assert.Single(plan);
        Assert.EndsWith("doc.pdf", plan[0].SourcePath);
    }

    [Fact]
    public void Execute_MovesFilesIntoCategoryFolders()
    {
        var pdfPath = Path.Combine(_desktop, "doc.pdf");
        File.WriteAllText(pdfPath, "content");
        var plan = _organizer.PlanOrganize(_desktop);

        var batch = _organizer.Execute(plan);

        Assert.False(File.Exists(pdfPath));
        var expectedDestination = Path.Combine(_desktop, "Documents", "doc.pdf");
        Assert.True(File.Exists(expectedDestination));
        Assert.Single(batch.Actions);
    }

    [Fact]
    public void Execute_DestinationAlreadyExists_SkipsWithoutOverwriting()
    {
        var pdfPath = Path.Combine(_desktop, "doc.pdf");
        File.WriteAllText(pdfPath, "new content");
        Directory.CreateDirectory(Path.Combine(_desktop, "Documents"));
        var existingDestination = Path.Combine(_desktop, "Documents", "doc.pdf");
        File.WriteAllText(existingDestination, "original content - must not be overwritten");

        var plan = _organizer.PlanOrganize(_desktop);
        var batch = _organizer.Execute(plan);

        Assert.Empty(batch.Actions);
        Assert.True(File.Exists(pdfPath)); // source untouched since the move was skipped
        Assert.Equal("original content - must not be overwritten", File.ReadAllText(existingDestination));
    }

    [Fact]
    public void Undo_MovesFilesBackToOriginalLocations()
    {
        var pdfPath = Path.Combine(_desktop, "doc.pdf");
        File.WriteAllText(pdfPath, "content");
        var plan = _organizer.PlanOrganize(_desktop);
        var batch = _organizer.Execute(plan);

        _organizer.Undo(batch);

        Assert.True(File.Exists(pdfPath));
        Assert.False(File.Exists(Path.Combine(_desktop, "Documents", "doc.pdf")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_desktop))
        {
            Directory.Delete(_desktop, recursive: true);
        }
    }
}
