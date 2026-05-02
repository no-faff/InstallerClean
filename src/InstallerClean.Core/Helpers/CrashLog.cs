using System.Text;

namespace InstallerClean.Helpers;

/// <summary>
/// Writes unhandled exceptions to a persistent log file so crashes can
/// be diagnosed after the fact.
/// </summary>
public static class CrashLog
{
    private const long MaxBytes = 512 * 1024;

    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoFaff", "InstallerClean");

    private static readonly string LogFile = Path.Combine(LogFolder, "crash.log");
    private static readonly string ArchiveFile = Path.Combine(LogFolder, "crash.log.old");

    /// <summary>
    /// Appends the full exception detail (type, message, stack trace,
    /// inner exceptions) to crash.log and returns the log path so it
    /// can be shown to the user. Swallows IO errors silently: a crash
    /// handler must never itself throw.
    /// </summary>
    public static string Write(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            RotateIfNeeded();

            // OpenAtomic returns null if LogFile is a symlink; drop the
            // entry rather than append into the symlink's target.
            using var handle = StorageHelpers.OpenAtomic(
                LogFile, FileAccess.Write, StorageHelpers.AtomicOpenMode.OpenAlways);
            if (handle is null) return LogFile;

            using var fs = new FileStream(handle, FileAccess.Write);
            fs.Seek(0, SeekOrigin.End);

            var entry = $"---- {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} ----{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
            using var writer = new StreamWriter(fs, Encoding.UTF8, leaveOpen: false);
            writer.Write(entry);
        }
        catch
        {
            // Swallow: a crash handler must never itself throw.
        }
        return LogFile;
    }

    private static void RotateIfNeeded()
    {
        try
        {
            if (!File.Exists(LogFile)) return;
            if (new FileInfo(LogFile).Length < MaxBytes) return;
            // File.Move with overwrite uses MOVEFILE_REPLACE_EXISTING,
            // which replaces a symlink rather than following it.
            File.Move(LogFile, ArchiveFile, overwrite: true);
        }
        catch
        {
            // Best-effort: next Write retries; worst case the log
            // briefly exceeds MaxBytes.
        }
    }
}
