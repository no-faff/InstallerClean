using InstallerClean.Models;

namespace InstallerClean.Services;

public interface ISettingsService
{
    AppSettings Load();

    /// <summary>Best-effort save. Never throws. See <see cref="TrySave"/>.</summary>
    void Save(AppSettings settings);

    /// <summary>Save that returns whether the write succeeded.</summary>
    bool TrySave(AppSettings settings);
}
