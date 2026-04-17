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
            // Preserve the unreadable file for manual recovery before starting fresh.
            try { File.Move(_settingsFile, _settingsFile + ".bad", overwrite: true); }
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
