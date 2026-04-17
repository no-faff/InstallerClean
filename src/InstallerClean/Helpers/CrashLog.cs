namespace InstallerClean.Helpers;

/// <summary>
/// Writes unhandled exceptions to a persistent log file so crashes can be
/// diagnosed after the fact.
/// </summary>
public static class CrashLog
{
    private const long MaxBytes = 512 * 1024; // 512 KB

    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoFaff", "InstallerClean");

    private static readonly string LogFile = Path.Combine(LogFolder, "crash.log");
    private static readonly string ArchiveFile = Path.Combine(LogFolder, "crash.log.old");

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
            RotateIfNeeded();
            // Offset-aware timestamp so logs from users in different
            // timezones remain unambiguous when shared for diagnosis.
            var entry = $"---- {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} ----{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(LogFile, entry);
        }
        catch
        {
            // best effort
        }
        return LogFile;
    }

    private static void RotateIfNeeded()
    {
        try
        {
            if (!File.Exists(LogFile)) return;
            var info = new FileInfo(LogFile);
            if (info.Length < MaxBytes) return;
            File.Move(LogFile, ArchiveFile, overwrite: true);
        }
        catch
        {
            // If rotation fails, the next Write will still try to append.
            // Worst case the log grows a bit larger than MaxBytes before
            // rotation succeeds.
        }
    }
}
