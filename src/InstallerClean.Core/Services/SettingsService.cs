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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            // Rename the unreadable file to .bad for manual recovery.
            var badFile = _settingsFile + ".bad";
            try { File.Move(_settingsFile, badFile, overwrite: true); }
            catch { }
            return new AppSettings();
        }
    }

    /// <summary>
    /// Persists settings using a write-temp-then-rename for atomicity.
    /// Swallows IO errors (disk full, OneDrive lock, read-only profile)
    /// so a failed save can never crash an operation that triggered it.
    /// Callers that need to know whether the save succeeded should use
    /// <see cref="TrySave"/> instead.
    /// </summary>
    public void Save(AppSettings settings) => TrySave(settings);

    /// <summary>Persists settings. Returns true on success.</summary>
    public bool TrySave(AppSettings settings)
    {
        // Random temp filename so two writers (e.g. future concurrent
        // CLI /s instances that skip the single-instance mutex) don't
        // collide on the same .tmp path.
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
