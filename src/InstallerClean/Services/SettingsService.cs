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
        // Refuse to read through a redirected path; same threat model as
        // TrySave: an attacker who controlled %LOCALAPPDATA% could plant a
        // symlink to read a sensitive file as us, or redirect the .bad
        // recovery rename below into a sensitive location.
        if (StorageHelpers.IsRedirected(_settingsFile))
            return new AppSettings();

        if (!File.Exists(_settingsFile))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            // Preserve the unreadable file for manual recovery before
            // starting fresh. Re-check the .bad target path in case an
            // attacker planted a file-level symlink there specifically.
            var badFile = _settingsFile + ".bad";
            try
            {
                if (!StorageHelpers.IsRedirected(badFile))
                    File.Move(_settingsFile, badFile, overwrite: true);
            }
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
        var tempFile = _settingsFile + ".tmp";
        try
        {
            // Refuse to write through a redirected path. Both the temp file
            // AND the final settings file are checked; either being a
            // junction or symlink would let an attacker who controlled
            // %LOCALAPPDATA% redirect the write into a sensitive location.
            if (StorageHelpers.IsRedirected(_settingsFile) ||
                StorageHelpers.IsRedirected(tempFile))
                return false;

            var folder = Path.GetDirectoryName(_settingsFile);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(tempFile, json);
            File.Move(tempFile, _settingsFile, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            // Clean up the temp file so a disk-full save leaves no debris.
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            return false;
        }
    }
}
