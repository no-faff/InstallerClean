using System.Text;
using InstallerClean.Resources;

namespace InstallerClean.Helpers;

/// <summary>
/// Writes unhandled exceptions to a persistent log file so crashes can
/// be diagnosed after the fact.
/// </summary>
public static class CrashLog
{
    private const long MaxBytes = 512 * 1024;

    // resx stores the header as one multi-line block ending with a
    // single LF. Normalise to the host platform's line endings so
    // the file reads cleanly in Notepad / VS Code / less. The
    // trailing blank line separates the header from the first entry.
    private static readonly string PrivacyHeader =
        Strings.CrashLog_PrivacyHeader.Replace("\n", Environment.NewLine) + Environment.NewLine;

    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoFaff", "InstallerClean");

    private static readonly string LogFile = Path.Combine(LogFolder, "crash.log");
    private static readonly string ArchiveFile = Path.Combine(LogFolder, "crash.log.old");

    /// <summary>
    /// Appends the exception to crash.log and returns the log path.
    /// Swallows IO errors (a crash handler must never throw); use
    /// <see cref="TryWrite"/> to also learn whether the write
    /// succeeded.
    /// </summary>
    public static string Write(Exception ex)
    {
        TryWrite(ex);
        return LogFile;
    }

    /// <summary>
    /// Like <see cref="Write"/> but also reports whether the entry was
    /// persisted, so dialog text doesn't claim "details written to X"
    /// when the write failed (symlinked log file, read-only profile).
    /// </summary>
    public static (string Path, bool Written) TryWrite(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            RotateIfNeeded();

            // OpenAtomic returns null if LogFile is a symlink; drop the
            // entry rather than append into the symlink's target.
            using var handle = StorageHelpers.OpenAtomic(
                LogFile, FileAccess.Write, StorageHelpers.AtomicOpenMode.OpenAlways);
            if (handle is null) return (LogFile, false);

            using var fs = new FileStream(handle, FileAccess.Write);
            var writeHeader = fs.Length == 0;
            fs.Seek(0, SeekOrigin.End);

            // The first write to a fresh log file prepends a privacy
            // header. Under elevation, framework exception messages can
            // contain file paths from the running session including
            // other users' profiles, so anyone attaching this log to a
            // public report needs the disclosure before sharing.
            // Header lines start with # so log readers can skip them.
            using var writer = new StreamWriter(fs, Encoding.UTF8, leaveOpen: false);
            if (writeHeader)
                writer.Write(PrivacyHeader);
            var entry = $"---- {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} ----{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
            writer.Write(entry);
            return (LogFile, true);
        }
        catch
        {
            // Swallow: a crash handler must never itself throw.
            return (LogFile, false);
        }
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
