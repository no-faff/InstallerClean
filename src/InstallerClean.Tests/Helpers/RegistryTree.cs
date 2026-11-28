using InstallerClean.Services;
using Microsoft.Win32;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A registry held in memory, as keys a test writes by their full paths: a path under HKLM
/// as it is asked for, and a path in an account's hive as <c>HKU\&lt;sid&gt;\...</c>
/// (<see cref="InHive"/>). A key written has every key above it. A key no test wrote is not
/// there, and one marked with <see cref="WillNotRead"/> answers every read as unreadable.
/// Every key asked about is recorded in <see cref="Reads"/>.
/// </summary>
internal sealed class RegistryTree : IRegistryReader
{
    private readonly Dictionary<string, List<RegistryValue>> _keys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unreadable = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _hives = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every key path asked about, in order, a hive's as <see cref="InHive"/> spells it.</summary>
    public List<string> Reads { get; } = [];

    /// <summary>The spelling of <paramref name="keyPath"/> in the hive loaded as <paramref name="sid"/>.</summary>
    public static string InHive(string sid, string keyPath) => $@"HKU\{sid}\{keyPath}";

    /// <summary>The hives loaded under HKEY_USERS, as a test's own list of them.</summary>
    public string[] LoadedHives => _hives.ToArray();

    /// <summary>A hive is loaded as <paramref name="sid"/>.</summary>
    public RegistryTree Hive(string sid)
    {
        _hives.Add(sid);
        return this;
    }

    /// <summary>The hive loaded as <paramref name="sid"/> is unloaded.</summary>
    public RegistryTree Unload(string sid)
    {
        _hives.Remove(sid);
        foreach (var key in _keys.Keys.Where(k => k.StartsWith($@"HKU\{sid}\", StringComparison.OrdinalIgnoreCase)).ToList())
            _keys.Remove(key);
        return this;
    }

    /// <summary>The key at <paramref name="path"/> is there, with these string values, each a name and its text.</summary>
    public RegistryTree Key(string path, params (string Name, string Text)[] values) =>
        Key(path, values.Select(v => new RegistryValue(v.Name, RegistryValueKind.String, v.Text)).ToArray());

    /// <summary>The key at <paramref name="path"/> is there, with these values.</summary>
    public RegistryTree Key(string path, RegistryValue[] values)
    {
        for (var above = path; above.Length > 0; above = Above(above))
            if (!_keys.ContainsKey(above)) _keys[above] = [];
        _keys[path].AddRange(values);
        return this;
    }

    /// <summary>The key at <paramref name="path"/> is there and answers every read as unreadable.</summary>
    public RegistryTree WillNotRead(string path)
    {
        Key(path);
        _unreadable.Add(path);
        return this;
    }

    private static string Above(string path)
    {
        var cut = path.LastIndexOf('\\');
        return cut < 0 ? string.Empty : path[..cut];
    }

    private RegistryKeyPresence PresenceOf(string path)
    {
        Reads.Add(path);
        if (_unreadable.Contains(path)) return RegistryKeyPresence.Unreadable;
        return _keys.ContainsKey(path) ? RegistryKeyPresence.Present : RegistryKeyPresence.Absent;
    }

    private RegistryKeyValues ValuesAt(string path) => PresenceOf(path) switch
    {
        RegistryKeyPresence.Present => new RegistryKeyValues(RegistryKeyPresence.Present, _keys[path].ToArray()),
        var other => new RegistryKeyValues(other),
    };

    private RegistrySubKeyNames SubKeysAt(string path) => PresenceOf(path) switch
    {
        RegistryKeyPresence.Present => new RegistrySubKeyNames(
            RegistryKeyPresence.Present,
            _keys.Keys.Where(k => string.Equals(Above(k), path, StringComparison.OrdinalIgnoreCase))
                .Select(k => k[(path.Length + 1)..]).ToArray()),
        var other => new RegistrySubKeyNames(other),
    };

    public RegistryKeyPresence LocalMachineKeyPresence(string relativePath) => PresenceOf(relativePath);

    public RegistryKeyValues LocalMachineValues(string keyPath, CancellationToken cancellationToken) => ValuesAt(keyPath);

    public RegistryKeyValues UserValues(string sid, string keyPath, CancellationToken cancellationToken) =>
        ValuesAt(InHive(sid, keyPath));

    public RegistrySubKeyNames LocalMachineSubKeys(string keyPath, CancellationToken cancellationToken) => SubKeysAt(keyPath);

    public RegistrySubKeyNames UserSubKeys(string sid, string keyPath, CancellationToken cancellationToken) =>
        SubKeysAt(InHive(sid, keyPath));

    public RegistryKeyPresence UserHivePresence(string sid)
    {
        Reads.Add($@"HKU\{sid}");
        return _hives.Contains(sid) ? RegistryKeyPresence.Present : RegistryKeyPresence.Absent;
    }

    public RegistryMultiStringRead LocalMachineMultiStringValue(string keyPath, string valueName) =>
        throw new InvalidOperationException($"No string array is scripted here, and {keyPath} {valueName} was asked for.");

    public RegistryDwordRead LocalMachineDwordValue(string keyPath, string valueName) =>
        throw new InvalidOperationException($"No number is scripted here, and {keyPath} {valueName} was asked for.");
}
