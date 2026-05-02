using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Reads and writes <see cref="AppSettings"/>. Persistence uses
/// write-temp-then-rename so a crash mid-save can't leave a half-
/// written settings.json. A corrupt file detected during
/// <see cref="Load"/> is renamed to <c>settings.json.bad</c> before
/// the loader returns defaults. Both Load and TrySave open via
/// <c>StorageHelpers.OpenAtomic</c> so a symlink at the settings
/// file path can't redirect the read or write under elevation.
/// </summary>
public interface ISettingsService
{
    /// <summary>Read settings.json. Returns defaults on failure; never throws.</summary>
    AppSettings Load();

    /// <summary>Persist settings. Returns true on success; never throws.</summary>
    bool TrySave(AppSettings settings);
}
