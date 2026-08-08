using InstallerClean.Models;

namespace InstallerClean.Services;

/// <inheritdoc />
internal sealed class ShortNameCreationProbe : IShortNameCreationProbe
{
    // The key and the four settings are Microsoft's, from the fsutil 8dot3name
    // reference (the `set <defaultvalue>` parameter, which names this value and
    // enumerates 0 to 3). That page does not state what an unconfigured machine
    // is at, so neither does this: an absent value is reported as absent and the
    // reports say what the population really holds.
    private const string FileSystemKey = @"SYSTEM\CurrentControlSet\Control\FileSystem";
    private const string ValueName = "NtfsDisable8dot3NameCreation";

    private readonly IRegistryReader _registry;

    public ShortNameCreationProbe(IRegistryReader registry) => _registry = registry;

    public string Read()
    {
        var read = _registry.LocalMachineDwordValue(FileSystemKey, ValueName);
        return read.State switch
        {
            RegistryDwordState.Absent => ShortNameCreationLabels.Unset,
            RegistryDwordState.WrongType => ShortNameCreationLabels.Unrecognised,
            RegistryDwordState.Unreadable => ShortNameCreationLabels.Unreadable,
            _ => FromSetting(read.Value),
        };
    }

    // The value disables creation, so the labels invert it: they say where short
    // names are still being made, which is the direction the question is asked in.
    // A number outside the documented four is a machine configured by something
    // that knew more than this does, and it is reported as unrecognised rather
    // than folded into the nearest neighbour.
    private static string FromSetting(int value) => value switch
    {
        0 => ShortNameCreationLabels.AllVolumes,
        1 => ShortNameCreationLabels.NoVolumes,
        2 => ShortNameCreationLabels.PerVolume,
        3 => ShortNameCreationLabels.SystemVolumeOnly,
        _ => ShortNameCreationLabels.Unrecognised,
    };
}
