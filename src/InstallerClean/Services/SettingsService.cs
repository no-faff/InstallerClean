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

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile)!);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var tempFile = _settingsFile + ".tmp";
        File.WriteAllText(tempFile, json);
        File.Move(tempFile, _settingsFile, overwrite: true);
    }
}
