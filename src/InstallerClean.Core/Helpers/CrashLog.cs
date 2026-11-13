using System.Globalization;
using System.Text;
using InstallerClean.Models;
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
    /// <summary>The size at which a write first moves the log aside to its archive.</summary>
    internal const long MaxBytes = 512 * 1024;

    /// <summary>The log's file name, in the folder <see cref="LogPath"/> names.</summary>
    internal const string FileName = "crash.log";

    /// <summary>What the log's path takes on as it is moved aside to its archive.</summary>
    internal const string ArchiveSuffix = ".old";

    // The header Prepare built, which every write after it uses, so no write after
    // Prepare looks a string up. A write before Prepare builds its own and keeps none.
    private static volatile string? _preparedHeader;

    // resx stores the header as one multi-line block ending with a
    // single LF. Normalise to the host platform's line endings so
    // the file reads cleanly in Notepad / VS Code / less. The
    // trailing blank line separates the header from the first entry.
    private static string BuildPrivacyHeader() =>
        Strings.CrashLog_PrivacyHeader.Replace("\n", Environment.NewLine) + Environment.NewLine;

    // The header Prepare built where it has run, built here otherwise, and null where
    // its string will not load.
    private static string? PrivacyHeaderOrNull()
    {
        if (_preparedHeader is { } prepared) return prepared;
        try
        {
            return BuildPrivacyHeader();
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// The folder the test suite has the log written to for its whole run, so that no test
    /// writes into the log of the PC running it. Null everywhere else, which puts the log in
    /// the app's own folder, <see cref="AppDataFolder"/>. Nothing in the app sets it.
    /// </summary>
    internal static string? FolderForTests { get; set; }

    /// <summary>
    /// Drops the header <see cref="Prepare"/> kept, so a test that prepared the log in one
    /// language leaves the next writing headers of its own again. Nothing in the app calls it.
    /// </summary>
    internal static void ForgetPreparedHeader() => _preparedHeader = null;

    private static string LogFolder => FolderForTests ?? AppDataFolder.Location;

    /// <summary>
    /// Works out the folder the log is written to, which settles the app's folder on its
    /// first use, and builds the header and keeps it, so a write after this needs neither.
    /// The header is built in the calling thread's language, and every fresh log after
    /// this is headed in it, so this is called where that is the display language, and
    /// never inside a machine line's en-GB scope.
    /// Throws where either cannot be done, for the caller to log.
    /// </summary>
    internal static void Prepare()
    {
        _ = LogFolder;
        _preparedHeader = BuildPrivacyHeader();
    }

    /// <summary>
    /// Where the log lives, or an empty string where working the folder out throws,
    /// for the one caller that needs to recognise the path inside a sentence without
    /// writing anything: the drawing layer gives it its break opportunities, and a
    /// hardcoded spelling would match nothing on a machine whose profile sits elsewhere.
    /// That caller reads it in a static initialiser, so this never throws.
    /// </summary>
    public static string LogPath
    {
        get
        {
            try
            {
                return PathText.Combine(LogFolder, FileName);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Appends the exception to crash.log and reports where the log is and whether the
    /// entry was persisted, so dialog text doesn't claim "details written to X" when the
    /// write failed (symlinked log file, read-only profile). The path is empty where it
    /// could not be worked out. Swallows every failure: a crash handler must not throw.
    /// </summary>
    public static CrashLogEntry TryWrite(Exception ex)
    {
        var path = string.Empty;
        try
        {
            var folder = LogFolder;
            path = PathText.Combine(folder, FileName);
            FrameworkDirectory.CreateDirectory(folder);
            RotateIfNeeded(path, path + ArchiveSuffix);

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
            // Returns null where the log file is a symlink; drop the entry rather than
            // append into the symlink's target.
            using var handle = StorageHelpers.OpenAtomicAppend(path);
            if (handle is null) return new CrashLogEntry(path, false);

            // No buffering layer (StorageHelpers.UnbufferedWriter), so the single
            // Write below is a single write to the file. A StreamWriter over a
            // buffered stream flushes in chunks, and a second writer appending
            // between two of this entry's chunks would split it down the middle.
            using var fs = StorageHelpers.UnbufferedWriter(handle);

            // The first write to a fresh log file prepends a privacy
            // header. Under elevation, framework exception messages can
            // contain file paths from the running session including
            // other users' profiles, so anyone attaching this log to a
            // public report needs the disclosure before sharing.
            // Header lines start with # so log readers can skip them. A
            // fresh log is written only with its header: where the header
            // cannot be built, nothing is written and the log stays empty
            // for a later write.
            var writeHeader = fs.Length == 0;
            var header = writeHeader ? PrivacyHeaderOrNull() : string.Empty;
            if (header is null) return new CrashLogEntry(path, false);

            // Redundant on an append-only handle (Win32 ignores the offset), and
            // kept as the floor: were this handle ever opened plainly writable
            // again, the write would still append rather than land on the head
            // of the file.
            fs.Seek(0, SeekOrigin.End);

            // The invariant culture keeps the stamp in one shape on every machine: in the
            // user's culture the pattern's ':' is the culture's time separator, '.' in
            // Danish as ICU gives it, and the year is the culture's calendar's.
            var stamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
            var entry = $"---- {stamp} ----{Environment.NewLine}{TextOf(ex)}{Environment.NewLine}{Environment.NewLine}";
            // The BOM goes in with the header, on a fresh file only: written
            // again on an append it would sit in the log body as a stray
            // U+FEFF. Arrays rather than spans: a copy's start check can write here, and that
            // path keeps to the .NET Framework NetFrameworkCheck's remarks name.
            var bom = writeHeader ? Encoding.UTF8.GetPreamble() : new byte[0];
            var text = Encoding.UTF8.GetBytes(header + entry);
            var payload = new byte[bom.Length + text.Length];
            Buffer.BlockCopy(bom, 0, payload, 0, bom.Length);
            Buffer.BlockCopy(text, 0, payload, bom.Length, text.Length);
            fs.Write(payload, 0, payload.Length);
            return new CrashLogEntry(path, true);
        }
        catch
        {
            // Swallow: a crash handler must not itself throw.
            return new CrashLogEntry(path, false);
        }
    }

    /// <summary>
    /// The text crash.log records for <paramref name="ex"/>: what <c>ToString()</c> gives,
    /// type, message, inner exceptions and stack traces as .NET writes them, with the message
    /// of each exception in the chain that ends in a closing sentence
    /// (<see cref="IClosingSentenceMessage"/>) written without it.
    /// </summary>
    internal static string TextOf(Exception ex)
    {
        var text = ex.ToString();
        foreach (var each in Chain(ex))
        {
            if (each is IClosingSentenceMessage closing
                && closing.MessageWithoutClosingSentence.Length < closing.Message.Length)
                text = text.Replace(closing.Message, closing.MessageWithoutClosingSentence);
        }
        return text;
    }

    /// <summary>
    /// <paramref name="ex"/> and every exception <c>ToString()</c> writes beneath it: the
    /// inner exception, and each one an <see cref="AggregateException"/> holds.
    /// </summary>
    private static IEnumerable<Exception> Chain(Exception ex)
    {
        yield return ex;
        var inner = ex is AggregateException aggregate
            ? aggregate.InnerExceptions
            : ex.InnerException is { } one ? [one] : (IEnumerable<Exception>)[];
        foreach (var each in inner)
            foreach (var below in Chain(each))
                yield return below;
    }

    private static void RotateIfNeeded(string logFile, string archiveFile)
    {
        try
        {
            if (!FrameworkFile.Exists(logFile)) return;
            if (FrameworkFile.GetLength(logFile) < MaxBytes) return;
            // File.Move with overwrite uses MOVEFILE_REPLACE_EXISTING,
            // which replaces a symlink rather than following it.
            File.Move(logFile, archiveFile, overwrite: true);
        }
        catch
        {
            // Best-effort: a log that cannot be moved aside takes the entry
            // anyway, and the next write tries the move again.
        }
    }
}

/// <summary>
/// What <see cref="CrashLog.TryWrite"/> answers: where crash.log is, empty where that could
/// not be worked out, and whether the entry reached it.
/// </summary>
/// <remarks>
/// A type of its own rather than a tuple: <see cref="CrashLog.TryWrite"/> is on the path a copy runs
/// before its .NET Framework 4.8 check (<see cref="Services.NetFrameworkCheck"/>).
/// </remarks>
public readonly record struct CrashLogEntry(string Path, bool Written);
