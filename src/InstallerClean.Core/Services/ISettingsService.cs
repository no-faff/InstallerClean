using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Reads and writes the user's <see cref="AppSettings"/>. Persistence
/// uses an atomic write-temp-then-rename so a crash mid-save can
/// never leave a half-written settings.json. A corrupt file detected
/// during <see cref="Load"/> is renamed to <c>settings.json.bad</c>
/// for manual recovery before the loader returns defaults.
/// </summary>
/// <remarks>
/// Failed writes never throw out of <see cref="Save"/>; callers that
/// need to know whether the save succeeded use <see cref="TrySave"/>.
/// Both Load and Save refuse to operate through reparse-point parents
/// or symlinked target files (see <c>StorageHelpers.IsRedirected</c>):
/// the process runs elevated, so a junction at <c>%LOCALAPPDATA%</c>
/// would otherwise let an attacker who controlled that path redirect
/// the read or write into a sensitive location.
/// </remarks>
public interface ISettingsService
{
    /// <summary>
    /// Read settings.json. Returns defaults on any failure (missing
    /// file, parse error, redirected path); never throws. A parse
    /// error is renamed aside as <c>.bad</c> for manual recovery.
    /// </summary>
    AppSettings Load();

    /// <summary>Best-effort save. Never throws. See <see cref="TrySave"/>.</summary>
    void Save(AppSettings settings);

    /// <summary>Save that returns whether the write succeeded.</summary>
    bool TrySave(AppSettings settings);
}
