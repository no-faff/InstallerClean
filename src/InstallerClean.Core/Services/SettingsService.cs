using System.Text.Json;
using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoFaff", "InstallerClean");

    private static readonly string DefaultSettingsFile = Path.Combine(SettingsFolder, "settings.json");

    // 64KB read cap. The schema is a flat object and the file is
    // normally a few hundred bytes. An oversize settings.json would
    // otherwise be loaded into a single managed string at startup
    // and could OOM the elevated WPF process before MainWindow opens;
    // the cap turns oversize into a clean InvalidDataException that
    // the catch block routes to the .bad-rename recovery path.
    private const int MaxReadBytes = 64 * 1024;

    // Internal for the config-pin test in SettingsServiceConfigTests.
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Defence in depth against deeply-nested JSON parsed by the
        // elevated process. The schema is shallow; eight levels covers
        // the deepest expected nesting plus headroom.
        MaxDepth = 8,
    };

    private readonly string _settingsFile;

    // Serialises Update's read-modify-write. SettingsService is a DI singleton
    // (CoreComposition), so one gate covers every settings writer in the
    // process: the debounced MoveDestination save on a thread-pool thread, and
    // the result-log lifetime lock and the language pick on the dispatcher.
    private readonly object _ioGate = new();

    public SettingsService() : this(DefaultSettingsFile) { }

    internal SettingsService(string settingsFile)
    {
        _settingsFile = settingsFile;
    }

    public AppSettings Load()
    {
        TryLoad(out var settings);
        return settings;
    }

    /// <summary>
    /// Loads the settings, and separately reports whether the file was actually
    /// read. False means the answer is defaults because the file could not be
    /// read, not because there was nothing in it to read, and a caller about to
    /// write must not write.
    ///
    /// The distinction is the difference between recovering a corrupt file and
    /// destroying a good one. A settings.json that is locked, momentarily
    /// unreadable, or on a profile a roaming or OneDrive-redirected setup has
    /// taken away throws IOException or UnauthorizedAccessException, which is
    /// not evidence of corruption; treating it as corruption loses the chosen
    /// language, the backup folder, the update opt-out and the record that a
    /// report has already been sent, and the last of those re-asks a user who
    /// has already answered. Only a file that parsed as something other than
    /// this schema is corrupt, and only that is renamed aside.
    /// </summary>
    private bool TryLoad(out AppSettings settings)
    {
        try
        {
            using var handle = StorageHelpers.OpenAtomic(
                _settingsFile, FileAccess.Read, StorageHelpers.AtomicOpenMode.OpenExisting);
            if (handle is null)
            {
                settings = new AppSettings();
                return !RefusedRatherThanAbsent(_settingsFile);
            }

            using var fs = new FileStream(handle, FileAccess.Read);
            if (fs.Length > MaxReadBytes)
                throw new InvalidDataException("settings.json exceeds the read cap");

            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            // Corrupt: the bytes are there and are not this schema, so keeping
            // them costs the user every future save. Renamed rather than
            // deleted so the evidence survives one round, and the run carries
            // on with defaults, which is the only thing left to carry on with.
            // Other types (OutOfMemoryException, StackOverflow) propagate:
            // renaming on those would destroy a recoverable file in response to
            // a system-wide problem.
            var badFile = _settingsFile + ".bad";
            try { File.Move(_settingsFile, badFile, overwrite: true); }
            catch { }
            settings = new AppSettings();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            settings = new AppSettings();
            return false;
        }
    }

    /// <summary>
    /// Whether an open that came back null was refused rather than answering
    /// that there is nothing there.
    ///
    /// <see cref="StorageHelpers.OpenAtomic"/> returns null for three states and
    /// only one of them is a read failure: the file is missing, so defaults ARE
    /// the settings; a reparse point sits at the name, which
    /// <see cref="TrySave"/> deliberately replaces rather than follows, so a
    /// save must still go ahead; or the open was refused, which is the
    /// transient case a save would write over. Attributes that cannot be read
    /// answer the same way as a refusal, a question that could not be put
    /// having no better answer than the cautious one.
    /// </summary>
    private static bool RefusedRatherThanAbsent(string path) =>
        File.Exists(path)
        && StorageHelpers.CheckReparsePoint(path, out _) != StorageHelpers.ReparseCheck.Yes;

    /// <summary>Persists settings via write-temp-then-rename. Returns true on
    /// success. Never throws (disk full / OneDrive lock / read-only profile
    /// all return false).</summary>
    public bool TrySave(AppSettings settings)
    {
        // Random temp name is belt-and-braces; the single-instance
        // mutex already prevents GUI and CLI /d|/m racing this file.
        var tempFile = _settingsFile + "." + Path.GetRandomFileName() + ".tmp";
        try
        {
            var folder = Path.GetDirectoryName(_settingsFile);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            // OpenAtomic + MoveFileEx(MOVEFILE_REPLACE_EXISTING) gives a
            // race-free save: atomic open at the temp file (refuses
            // symlinks), atomic rename onto the real file (replaces
            // symlinks rather than following them).
            using (var handle = StorageHelpers.OpenAtomic(
                       tempFile, FileAccess.Write, StorageHelpers.AtomicOpenMode.CreateAlways))
            {
                // Null means the open was refused after CREATE_ALWAYS had already
                // made the file (a reparse point at the name, or an attribute read
                // that failed). Returning straight out skips the catch below, so
                // this path has to clean up after itself.
                if (handle is null)
                {
                    StorageHelpers.TryDeleteTempFile(tempFile);
                    return false;
                }
                using var fs = new FileStream(handle, FileAccess.Write);
                JsonSerializer.Serialize(fs, settings, JsonOptions);
            }

            File.Move(tempFile, _settingsFile, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            StorageHelpers.TryDeleteTempFile(tempFile);
            return false;
        }
    }

    public bool Update(Action<AppSettings> mutate)
    {
        lock (_ioGate)
        {
            // A read that failed makes this a read-modify-write with no read in
            // it: every field but the one being set would be written back at its
            // default, which is the same loss as deleting the file and is why
            // the load's success is asked for here. Reporting the save as failed
            // is the truth and every caller already handles it.
            if (!TryLoad(out var settings)) return false;
            mutate(settings);
            return TrySave(settings);
        }
    }
}
