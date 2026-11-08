using System.Text;

namespace InstallerClean.Helpers;

/// <summary>
/// The app's own folder for the Windows account it runs as,
/// <c>%LOCALAPPDATA%\InstallerClean</c>, which holds settings.json, last-run.json and
/// crash.log. The first time a process asks for <see cref="Location"/>,
/// <c>%LOCALAPPDATA%\NoFaff\InstallerClean</c>, where earlier versions keep the same
/// files, is moved here, so every caller is answered with the folder its files are
/// actually in.
/// </summary>
/// <remarks>
/// The move happens inside the first ask rather than in either host's startup, so it
/// has happened before the first read or write whichever runs first: the window, the
/// command line, a read-only /s run that does not take the single-instance mutex, or a
/// crash handler. A static initialiser reading <see cref="CrashLog.LogPath"/> is one
/// such ask.
///
/// Where the move cannot be made, the answer is the earlier folder, so nothing the app
/// wrote is left behind in a folder it no longer reads, and the next process to start
/// tries again.
/// </remarks>
internal static class AppDataFolder
{
    private const string FolderName = "InstallerClean";
    private const string EarlierParentName = "NoFaff";

    // Twice CrashLog.MaxBytes, the size at which a write first moves the log aside to its
    // archive. An earlier log past this is not added to the kept one.
    private const long MaxMergedLogBytes = 2 * CrashLog.MaxBytes;

    private static readonly Lazy<Settled> First = new(
        () => Settle(KnownFolders.GetPath(Environment.SpecialFolder.LocalApplicationData)));

    private static int _failureRecorded;

    /// <summary>
    /// The folder the app's own files are in. The first call in a process settles it
    /// and moves the earlier folder across; every later call returns the same answer.
    /// </summary>
    internal static string Location
    {
        get
        {
            var settled = First.Value;

            // Written here rather than inside Settle, because CrashLog asks for this
            // folder and Lazy throws on an ask made while its value is being made. The
            // flag is set before the write, so the write's own ask returns at once.
            if (settled.Failure is { } failure && Interlocked.Exchange(ref _failureRecorded, 1) == 0)
                CrashLog.TryWrite(failure);

            return settled.Folder;
        }
    }

    /// <summary>
    /// Which folder the app's files are in, and what went wrong on the way if anything
    /// did. <paramref name="Failure"/> is for the crash log and changes nothing about
    /// the answer.
    /// </summary>
    internal readonly record struct Settled(string Folder, Exception? Failure);

    /// <summary>
    /// Moves <c>NoFaff\InstallerClean</c> under <paramref name="localAppData"/> to
    /// <c>InstallerClean</c> under it and answers with the folder the files are in.
    /// Never throws.
    /// </summary>
    /// <remarks>
    /// <para>The usual case is one rename of the whole folder. With no link in the way,
    /// both folders are on the volume holding <paramref name="localAppData"/>, so the
    /// rename either moves every file or moves none, and the emptied NoFaff folder is
    /// then removed unless something else is in it.</para>
    ///
    /// <para>Where both folders exist, as they do after an older version has been run
    /// again and made its folder anew, each file only in the earlier folder is renamed
    /// across. Where a name is in both, the copy in <c>InstallerClean</c> is kept and
    /// the earlier one deleted, except crash.log, whose entries are added to the end of
    /// the kept log. The kept settings.json and last-run.json are a pair: the settings
    /// record whether that report has been sent.</para>
    ///
    /// <para>Nothing is moved or deleted through a junction or a symbolic link. This
    /// process runs elevated and the folders are writable by the account's unelevated
    /// processes, so a link in either folder's place would point the moves and deletes
    /// somewhere else. Where NoFaff or either folder is a link, or its attributes cannot
    /// be read, nothing is touched and the answer is the earlier folder wherever there
    /// is one. A link or a subfolder inside the earlier folder stays where it is, and so
    /// does the earlier folder around it.</para>
    ///
    /// <para>Two processes can run this at once, and each step is a rename that only
    /// one of them can make. The loser of the whole-folder rename finds the earlier
    /// folder gone and answers with the new one, and a crash.log is renamed to a name
    /// of its own before its entries are added, so the entries are added once.</para>
    /// </remarks>
    internal static Settled Settle(string localAppData)
    {
        var current = PathText.Combine(localAppData, FolderName);

        // No profile folder to answer from, which KnownFolders reports as an empty
        // string. The relative path is returned as the callers would build it, and
        // nothing is moved relative to the working folder.
        if (string.IsNullOrEmpty(localAppData))
            return new Settled(current, null);

        var parent = PathText.Combine(localAppData, EarlierParentName);
        var earlier = PathText.Combine(parent, FolderName);
        var failures = new List<Exception>();

        try
        {
            if (!IsPlainFolder(parent, failures))
                return Answer(Directory.Exists(earlier) ? earlier : current, failures);

            if (!Directory.Exists(earlier))
            {
                RemoveIfEmpty(parent);
                return Answer(current, failures);
            }

            if (!IsPlainFolder(earlier, failures) || !IsPlainFolder(current, failures))
                return Answer(earlier, failures);

            if (!Directory.Exists(current))
            {
                try
                {
                    FrameworkDirectory.Move(earlier, current);
                    RemoveIfEmpty(parent);
                    return Answer(current, failures);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Another process may have renamed it first, or made the new folder
                    // in between. Where neither happened the rename was refused, by a
                    // file held open inside the folder for one, and every file is still
                    // where it was.
                    if (!Directory.Exists(earlier))
                        return Answer(current, failures);
                    if (!Directory.Exists(current))
                    {
                        failures.Add(ex);
                        return Answer(earlier, failures);
                    }
                    if (!IsPlainFolder(current, failures))
                        return Answer(earlier, failures);
                }
            }

            Merge(earlier, current, failures);
            RemoveIfEmpty(earlier);
            RemoveIfEmpty(parent);
            return Answer(current, failures);
        }
        catch (Exception ex)
        {
            // Whatever this was, the answer is the folder the files are in now.
            failures.Add(ex);
            string folder;
            try { folder = Directory.Exists(current) || !Directory.Exists(earlier) ? current : earlier; }
            catch { folder = earlier; }
            return Answer(folder, failures);
        }
    }

    private static Settled Answer(string folder, List<Exception> failures) => new(
        folder,
        failures.Count switch
        {
            0 => null,
            1 => failures[0],
            _ => new AggregateException(
                "Moving the app's folder to " + folder + " met more than one failure.", failures),
        });

    /// <summary>
    /// True where <paramref name="path"/> is a real folder or names nothing. A link, or
    /// attributes that could not be read, is false, and the read's exception goes into
    /// <paramref name="failures"/>.
    /// </summary>
    private static bool IsPlainFolder(string path, List<Exception> failures)
    {
        var check = StorageHelpers.CheckReparsePoint(path, out var error);
        if (error is not null) failures.Add(error);
        return check == StorageHelpers.ReparseCheck.No;
    }

    private static void Merge(string earlier, string current, List<Exception> failures)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(earlier))
        {
            var name = PathText.GetFileName(entry);
            if (StorageHelpers.CheckReparsePoint(entry, out var error) != StorageHelpers.ReparseCheck.No)
            {
                if (error is not null) failures.Add(error);
                continue;
            }
            if (Directory.Exists(entry))
                continue;

            var target = PathText.Combine(current, name);
            try
            {
                File.Move(entry, target, overwrite: false);
                continue;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A file of this name already in the new folder is the clash handled
                // below; anything else leaves this one where it is for the next start.
                if (!File.Exists(target))
                {
                    if (File.Exists(entry)) failures.Add(ex);
                    continue;
                }
            }

            try
            {
                if (name.Equals(CrashLog.FileName, StringComparison.OrdinalIgnoreCase))
                    AppendEarlierLog(entry, target, failures);
                else
                    File.Delete(entry);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failures.Add(ex);
            }
        }
    }

    /// <summary>
    /// Adds the entries of the earlier folder's crash.log to the end of the kept one,
    /// without the earlier file's byte-order mark and the privacy header at its head,
    /// which the kept log already carries. The earlier log is first renamed to a name
    /// of its own, which only one process can do. Where adding them fails, that renamed
    /// file stays in the earlier folder and the next start moves it across as a file
    /// of its own.
    /// </summary>
    private static void AppendEarlierLog(string earlierLog, string log, List<Exception> failures)
    {
        var claimed = earlierLog + "." + Path.GetRandomFileName() + ".merging";
        try
        {
            File.Move(earlierLog, claimed, overwrite: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Another process claimed it, or it went; either way it is not this one's.
            if (File.Exists(earlierLog)) failures.Add(ex);
            return;
        }

        string text;
        using (var read = StorageHelpers.OpenAtomic(claimed, FileAccess.Read, StorageHelpers.AtomicOpenMode.OpenExisting))
        {
            if (read is null)
            {
                failures.Add(new IOException("The earlier crash log could not be opened to add to the kept one."));
                return;
            }
            using var fs = new FileStream(read, FileAccess.Read);
            if (fs.Length > MaxMergedLogBytes)
            {
                failures.Add(new InvalidDataException(
                    $"The earlier crash log exceeds {MaxMergedLogBytes} bytes and was not added to the kept one."));
                return;
            }
            using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            text = reader.ReadToEnd();
        }

        using (var write = StorageHelpers.OpenAtomicAppend(log))
        {
            if (write is null)
            {
                failures.Add(new IOException("The kept crash log could not be opened to add the earlier one's entries."));
                return;
            }
            using var fs = StorageHelpers.UnbufferedWriter(write);

            // An empty kept log takes the earlier one whole, header and mark included,
            // as its own first write would have.
            byte[] payload = fs.Length == 0
                ? [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)]
                : Encoding.UTF8.GetBytes(WithoutHeader(text));
            fs.Write(payload, 0, payload.Length);
        }

        File.Delete(claimed);
    }

    /// <summary>
    /// <paramref name="text"/> without the run of lines starting with '#' at its head
    /// and the blank lines after them, which is the privacy header CrashLog writes at
    /// the top of a fresh log.
    /// </summary>
    internal static string WithoutHeader(string text)
    {
        var start = 0;
        while (start < text.Length)
        {
            var end = text.IndexOf('\n', start);
            var lineEnd = end < 0 ? text.Length : end + 1;
            var line = text.AsSpan(start, lineEnd - start).TrimEnd("\r\n");
            if (!line.StartsWith("#") && !line.IsEmpty)
                break;
            start = lineEnd;
        }
        return text[start..];
    }

    /// <summary>
    /// Removes <paramref name="folder"/> where it is a real folder with nothing in it.
    /// The delete is not recursive, so a folder anything has been put in since stays.
    /// </summary>
    private static void RemoveIfEmpty(string folder)
    {
        try
        {
            if (StorageHelpers.CheckReparsePoint(folder, out _) != StorageHelpers.ReparseCheck.No)
                return;
            if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                Directory.Delete(folder, recursive: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left for the next start, which removes it once it is empty.
        }
    }
}
