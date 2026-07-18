using System.Text;
using InstallerClean.Resources;

namespace InstallerClean.Helpers;

/// <summary>
/// Appends exceptions to a persistent log file so a failure can be
/// diagnosed after the fact. The callers are the unhandled-exception
/// handlers, catch blocks that recover and carry on, and paths that
/// synthesise an exception purely to record a breadcrumb.
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

            // Append-only handle, so the write lands at the end of the file as
            // one atomic step. Two writers really do collide here: unhandled
            // task exceptions surface off the dispatcher, the debounced
            // settings save writes from the thread pool, the post-operation
            // refresh writes from the dispatcher, and a CLI /s run (read-only,
            // so it skips the single-instance mutex by design) writes to this
            // same file from a second process. Opened for GENERIC_WRITE and
            // seeked to the end instead, both writers would resolve "the end"
            // to the same offset and the second would overwrite the first,
            // losing an entry that several code paths exist for no other
            // purpose than to write.
            //
            // Returns null if LogFile is a symlink; drop the entry rather than
            // append into the symlink's target.
            using var handle = StorageHelpers.OpenAtomicAppend(LogFile);
            if (handle is null) return (LogFile, false);

            // bufferSize 0: no buffering layer, so the single Write below is a
            // single write to the file. A StreamWriter over a buffered stream
            // flushes in chunks, and a second writer appending between two of
            // this entry's chunks would split it down the middle.
            using var fs = new FileStream(handle, FileAccess.Write, bufferSize: 0);

            // The first write to a fresh log file prepends a privacy
            // header. Under elevation, framework exception messages can
            // contain file paths from the running session including
            // other users' profiles, so anyone attaching this log to a
            // public report needs the disclosure before sharing.
            // Header lines start with # so log readers can skip them.
            var writeHeader = fs.Length == 0;

            // Redundant on an append-only handle (Win32 ignores the offset), and
            // kept as the floor: were this handle ever opened plainly writable
            // again, the write would still append rather than land on the head
            // of the file.
            fs.Seek(0, SeekOrigin.End);

            var entry = $"---- {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} ----{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
            // The BOM goes in with the header, on a fresh file only: written
            // again on an append it would sit in the log body as a stray
            // U+FEFF.
            ReadOnlySpan<byte> bom = writeHeader ? Encoding.UTF8.GetPreamble() : default;
            byte[] payload = [.. bom, .. Encoding.UTF8.GetBytes(writeHeader ? PrivacyHeader + entry : entry)];
            fs.Write(payload, 0, payload.Length);
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
