namespace InstallerClean.Helpers;

/// <summary>
/// Writes unhandled exceptions to a persistent log file so crashes can be
/// diagnosed after the fact.
/// </summary>
public static class CrashLog
{
    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoFaff", "InstallerClean");

    private static readonly string LogFile = Path.Combine(LogFolder, "crash.log");

    /// <summary>
    /// Appends the full exception detail (type, message, stack trace, inner
    /// exceptions) to crash.log and returns the log path so it can be shown
    /// to the user. Swallows any IO errors silently. A crash handler must
    /// never itself throw.
    /// </summary>
    public static string Write(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var entry = $"---- {DateTime.Now:yyyy-MM-dd HH:mm:ss} ----{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(LogFile, entry);
        }
        catch
        {
            // best effort
        }
        return LogFile;
    }
}
