namespace DiskCleaner.Core.Services;

/// <summary>
/// Minimal file-based error logger. Background passes (auto-clean, scheduled scans)
/// must never crash the app on failure, but silently swallowing the error would make
/// problems invisible - this gives them somewhere to go instead of a bare catch{}.
/// </summary>
public sealed class AppLogger
{
    private readonly string _logPath;

    public AppLogger(string? logPath = null)
    {
        _logPath = logPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiskCleaner",
            "app.log");
    }

    public void LogError(string context, Exception ex)
    {
        try
        {
            var directory = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(
                _logPath,
                $"{DateTime.UtcNow:O} ERROR [{context}] {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Logging must never itself take down the app - this is the one
            // deliberate empty catch in the codebase.
        }
    }
}
