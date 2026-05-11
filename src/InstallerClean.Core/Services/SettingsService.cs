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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Defence in depth against deeply-nested JSON parsed by the
        // elevated process. The schema is shallow; eight levels covers
        // the deepest expected nesting plus headroom.
        MaxDepth = 8,
    };

    private readonly string _settingsFile;

    public SettingsService() : this(DefaultSettingsFile) { }

    internal SettingsService(string settingsFile)
    {
        _settingsFile = settingsFile;
    }

    public AppSettings Load()
    {
        try
        {
            // OpenAtomic returns null if the file is missing or a
            // symlink; both cases fall back to defaults.
            using var handle = StorageHelpers.OpenAtomic(
                _settingsFile, FileAccess.Read, StorageHelpers.AtomicOpenMode.OpenExisting);
            if (handle is null)
                return new AppSettings();

            using var fs = new FileStream(handle, FileAccess.Read);
            if (fs.Length > MaxReadBytes)
                throw new InvalidDataException("settings.json exceeds the read cap");

            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex) when (
            ex is JsonException
                or InvalidDataException
                or IOException
                or UnauthorizedAccessException)
        {
            // Recovery is scoped to JSON / IO / access-control failures
            // because those are the only modes a settings.json should
            // produce. Other types (OutOfMemoryException, StackOverflow)
            // propagate; .bad-renaming on those would destroy a
            // recoverable settings file in response to a system-wide
            // problem.
            var badFile = _settingsFile + ".bad";
            try { File.Move(_settingsFile, badFile, overwrite: true); }
            catch { }
            return new AppSettings();
        }
    }

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
                if (handle is null) return false;
                using var fs = new FileStream(handle, FileAccess.Write);
                JsonSerializer.Serialize(fs, settings, JsonOptions);
            }

            File.Move(tempFile, _settingsFile, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            return false;
        }
    }
}
