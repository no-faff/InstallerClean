using System.Text.Json;
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
        if (!File.Exists(_settingsFile))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            // Back up the unreadable file so the user can recover it manually
            // if they want to, then start fresh.
            try
            {
                var badFile = _settingsFile + ".bad";
                File.Move(_settingsFile, badFile, overwrite: true);
            }
            catch
            {
                // Best effort; if we can't move it we still return defaults.
            }
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
            // Leave no debris if the atomic move failed partway. Best
            // effort; we don't care if Delete itself fails (disk full
            // scenarios often fail Delete too).
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            return false;
        }
    }
}
