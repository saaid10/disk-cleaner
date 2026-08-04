using DiskCleaner.Core.Services;

namespace DiskCleaner.Core.Tests.Services;

public class AppLoggerTests : IDisposable
{
    private readonly string _logPath;

    public AppLoggerTests() =>
        _logPath = Path.Combine(Path.GetTempPath(), $"dc-log-{Guid.NewGuid():N}", "app.log");

    [Fact]
    public void LogError_CreatesFileAndWritesMessage()
    {
        var logger = new AppLogger(_logPath);

        logger.LogError("AutoClean", new InvalidOperationException("boom"));

        Assert.True(File.Exists(_logPath));
        var content = File.ReadAllText(_logPath);
        Assert.Contains("AutoClean", content);
        Assert.Contains("boom", content);
    }

    [Fact]
    public void LogError_CalledTwice_Appends()
    {
        var logger = new AppLogger(_logPath);

        logger.LogError("First", new Exception("one"));
        logger.LogError("Second", new Exception("two"));

        var lines = File.ReadAllLines(_logPath);
        Assert.Equal(2, lines.Length);
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_logPath);
        if (dir is not null && Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
