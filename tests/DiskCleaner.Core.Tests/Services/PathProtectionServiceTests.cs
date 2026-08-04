using DiskCleaner.Core.Data;
using DiskCleaner.Core.Services;
using DiskCleaner.Core.Tests.Data;

namespace DiskCleaner.Core.Tests.Services;

public class PathProtectionServiceTests : IClassFixture<AppDatabaseFixture>
{
    private readonly PathProtectionService _service;
    private readonly ProtectedFolderRepository _protectedFolders;
    private readonly SettingsRepository _settings;

    public PathProtectionServiceTests(AppDatabaseFixture fixture)
    {
        _protectedFolders = new ProtectedFolderRepository(fixture.Database);
        _settings = new SettingsRepository(fixture.Database);
        _service = new PathProtectionService(_protectedFolders, _settings);
    }

    [Fact]
    public void IsProtected_PathNotUnderAnyProtectedFolder_ReturnsFalse()
    {
        Assert.False(_service.IsProtected(@"C:\Users\test\Downloads\file.exe"));
    }

    [Fact]
    public void IsProtected_ExactProtectedFolder_ReturnsTrue()
    {
        _protectedFolders.Add(@"C:\Users\test\Projects");
        Assert.True(_service.IsProtected(@"C:\Users\test\Projects"));
    }

    [Fact]
    public void IsProtected_FileInsideProtectedFolder_ReturnsTrue_Recursive()
    {
        _protectedFolders.Add(@"C:\Users\test\Projects");
        Assert.True(_service.IsProtected(@"C:\Users\test\Projects\sub\deep\file.txt"));
    }

    [Fact]
    public void IsProtected_SiblingFolderWithSharedPrefix_ReturnsFalse()
    {
        _protectedFolders.Add(@"C:\Users\test\Projects");
        // "ProjectsArchive" starts with "Projects" as a string but is NOT a subfolder -
        // must not be treated as protected.
        Assert.False(_service.IsProtected(@"C:\Users\test\ProjectsArchive\file.txt"));
    }

    [Fact]
    public void IsProtected_UnderSafeHaven_ReturnsTrue()
    {
        _settings.SetString(PathProtectionService.SafeHavenPathSettingKey, @"C:\Users\test\SafeHaven");
        Assert.True(_service.IsProtected(@"C:\Users\test\SafeHaven\loose-file.pdf"));
    }

    [Fact]
    public void IsProtected_CaseInsensitive_ReturnsTrue()
    {
        _protectedFolders.Add(@"C:\Users\test\Projects");
        Assert.True(_service.IsProtected(@"c:\users\TEST\PROJECTS\file.txt"));
    }
}
