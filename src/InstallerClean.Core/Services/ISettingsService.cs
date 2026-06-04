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

    /// <summary>
    /// Serialises a read-modify-write under a private lock so concurrent
    /// writers cannot lose each other's change to the last-writer-wins rename:
    /// the debounced destination save runs on a thread-pool thread while the
    /// window-size and lifetime-lock persists run on the dispatcher. Loads,
    /// applies <paramref name="mutate"/>, saves. Returns the TrySave result;
    /// never throws.
    /// </summary>
    bool Update(Action<AppSettings> mutate);
}
