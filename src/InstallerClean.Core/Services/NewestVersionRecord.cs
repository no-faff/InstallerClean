using System.Text;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// The newest version of InstallerClean run in this Windows account, kept in
/// <c>%LOCALAPPDATA%\InstallerClean\newest-version-run.txt</c> as its three numbers
/// ("3.8.0"). The window records it as it starts and says once, from what it found, that
/// InstallerClean has been updated; the command line records it only on an account's first
/// run (<see cref="RecordFirstRun"/>). It is a file of its own, which
/// earlier versions never touch. Kept in settings.json it would be lost each time an
/// earlier version saved that file, which it rewrites whole from a schema without it, and
/// the command line writes the record without taking the mutex that the window's writes to
/// settings.json rely on.
/// </summary>
internal sealed class NewestVersionRecord
{
    internal const string FileName = "newest-version-run.txt";

    // The file holds three numbers and the dots between them. Anything longer is not a
    // version this class wrote, and is read as no version.
    private const int MaxReadBytes = 64;

    private readonly string _file;
    private readonly IReadOnlyList<string> _earlierRunFiles;

    /// <summary>
    /// The record in this account's InstallerClean folder. An earlier run in the account
    /// shows by settings.json or last-run.json beside it, the files earlier versions keep
    /// there.
    /// </summary>
    public NewestVersionRecord()
        : this(PathText.Combine(AppDataFolder.Location, FileName),
            [PathText.Combine(AppDataFolder.Location, SettingsService.FileName),
             PathText.Combine(AppDataFolder.Location, ResultLogService.FileName)])
    {
    }

    internal NewestVersionRecord(string file, IReadOnlyList<string> earlierRunFiles)
    {
        _file = file;
        _earlierRunFiles = earlierRunFiles;
    }

    /// <summary>This build's version, as major, minor and build.</summary>
    public static Version Running() =>
        UpdateCheckService.NormaliseToBuild(
            typeof(NewestVersionRecord).Assembly.GetName().Version ?? new Version(0, 0, 0));

    /// <summary>
    /// Reads the record and whether an earlier version ran in this account, for
    /// <see cref="Record"/> to act on. Read before anything a start does writes
    /// settings.json or last-run.json, since their presence is what shows an earlier run
    /// where there is no record. Never throws.
    /// </summary>
    public Reading Read(Version running)
    {
        running = UpdateCheckService.NormaliseToBuild(running);
        try
        {
            var found = Read(out var recorded);
            return new Reading(found, recorded, found == Found.Absent && EarlierRunInAccount(), running);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return new Reading(Found.Unreadable, null, false, running);
        }
    }

    /// <summary>
    /// Records the version <paramref name="reading"/> was taken for where it is newer than
    /// the record, and says whether the window is to say that InstallerClean has been
    /// updated to it. It says so only once the record holds that version, so a record that
    /// cannot be written never brings the line back at the next start. Never throws.
    /// </summary>
    public bool Record(Reading reading)
    {
        if (!Records(reading.Found, reading.Recorded, reading.Running))
            return false;
        return Write(reading.Running)
            && SaysUpdated(reading.Found, reading.Recorded, reading.EarlierRunInAccount, reading.Running);
    }

    /// <summary>
    /// <see cref="Read(Version)"/> and <see cref="Record"/> together, for a caller with
    /// nothing to do between them.
    /// </summary>
    public bool RecordAndSayIfUpdated(Version running) => Record(Read(running));

    /// <summary>
    /// Records <paramref name="running"/> only where this is the account's first run: no
    /// record and no earlier run. That is what the command line writes, so the window
    /// opening later as the same version does not take settings.json or last-run.json, which
    /// the command line may write, for an earlier version's. A record it finds, or an
    /// earlier run, it leaves for the window to move up as it says InstallerClean has been
    /// updated. Never throws.
    /// </summary>
    public void RecordFirstRun(Version running)
    {
        var reading = Read(running);
        if (reading.Found == Found.Absent && !reading.EarlierRunInAccount)
            Write(reading.Running);
    }

    /// <summary>What <see cref="Read(Version)"/> found, and the version it was taken for.</summary>
    internal readonly record struct Reading(Found Found, string? Recorded, bool EarlierRunInAccount, Version Running);

    /// <summary>What a read of the record found.</summary>
    internal enum Found
    {
        /// <summary>No record: no version that records one has run in this account.</summary>
        Absent,

        /// <summary>The record was read; its text may still not be a version.</summary>
        Read,

        /// <summary>A record is there and could not be read, so nothing is known of it.</summary>
        Unreadable,
    }

    /// <summary>
    /// Whether a start that found <paramref name="found"/> and <paramref name="recorded"/>
    /// says InstallerClean has been updated to <paramref name="running"/>: a record of an
    /// older version, or no record where an earlier version ran in this account
    /// (<paramref name="earlierRunInAccount"/>), which recorded none. No record and no
    /// earlier run is the account's first run. A record that will not read, or holds no
    /// version, says nothing about what ran before.
    /// </summary>
    internal static bool SaysUpdated(Found found, string? recorded, bool earlierRunInAccount, Version running) =>
        found switch
        {
            Found.Absent => earlierRunInAccount,
            Found.Read => TryParse(recorded, out var version) && version < UpdateCheckService.NormaliseToBuild(running),
            _ => false,
        };

    /// <summary>
    /// Whether <paramref name="running"/> is to be written: wherever the record is absent,
    /// holds no version, or holds an older one. Never over a record that could not be read.
    /// </summary>
    internal static bool Records(Found found, string? recorded, Version running) =>
        found != Found.Unreadable
        && (!TryParse(recorded, out var version) || version < UpdateCheckService.NormaliseToBuild(running));

    private bool EarlierRunInAccount() => _earlierRunFiles.Any(FrameworkFile.Exists);

    /// <summary>
    /// Reads the record. A reparse point at its name reads as no record, which the write
    /// then replaces rather than follows.
    /// </summary>
    private Found Read(out string? recorded)
    {
        recorded = null;
        try
        {
            using var handle = StorageHelpers.OpenAtomic(
                _file, FileAccess.Read, StorageHelpers.AtomicOpenMode.OpenExisting);
            if (handle is null)
            {
                return FrameworkFile.Exists(_file)
                    && StorageHelpers.CheckReparsePoint(_file, out _) != StorageHelpers.ReparseCheck.Yes
                    ? Found.Unreadable
                    : Found.Absent;
            }
            using var fs = new FileStream(handle, FileAccess.Read);
            if (fs.Length <= MaxReadBytes)
            {
                using var reader = new StreamReader(fs, Encoding.UTF8);
                recorded = reader.ReadToEnd().Trim();
            }
            return Found.Read;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Found.Unreadable;
        }
    }

    /// <summary>
    /// Writes <paramref name="version"/> as the record: a temporary file opened with
    /// <see cref="StorageHelpers.OpenAtomic"/>, which refuses a link at its name, then a
    /// rename that replaces whatever is at the record's name rather than following it.
    /// </summary>
    private bool Write(Version version)
    {
        var tempFile = _file + "." + Path.GetRandomFileName() + ".tmp";
        try
        {
            var folder = PathText.GetDirectoryName(_file);
            if (folder is { Length: > 0 }) FrameworkDirectory.CreateDirectory(folder);

            using (var handle = StorageHelpers.OpenAtomic(
                       tempFile, FileAccess.Write, StorageHelpers.AtomicOpenMode.CreateAlways))
            {
                // Null means the open was refused after CREATE_ALWAYS had made the file.
                if (handle is null)
                {
                    StorageHelpers.TryDeleteTempFile(tempFile);
                    return false;
                }
                using var fs = new FileStream(handle, FileAccess.Write);
                var bytes = Encoding.ASCII.GetBytes(version.ToString(3));
                fs.Write(bytes, 0, bytes.Length);
            }

            File.Move(tempFile, _file, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            // A refused rename or open is what a record that cannot be written gives at
            // every start, so it is not logged; anything else is.
            if (ex is not (IOException or UnauthorizedAccessException))
                CrashLog.TryWrite(ex);
            StorageHelpers.TryDeleteTempFile(tempFile);
            return false;
        }
    }

    private static bool TryParse(string? text, out Version version)
    {
        if (Version.TryParse(text, out var parsed))
        {
            version = UpdateCheckService.NormaliseToBuild(parsed);
            return true;
        }
        version = new Version(0, 0, 0);
        return false;
    }
}
