using InstallerClean.Compatibility;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// What Windows Installer's records of where programs and patches were installed from
/// say about the folders earlier batches left in the app's own folder
/// (<see cref="ILeftInHand"/>).
/// </summary>
public interface ISourceRecords
{
    /// <summary>
    /// Which of <paramref name="batches"/> a source record names: the folder a program or
    /// a patch was installed from, or one its source list holds. A file in such a folder
    /// is that program's or patch's source where it lies, and is not moved.
    /// </summary>
    SourceRecordsRead NamedBatches(IReadOnlyList<LeftBatch> batches, CancellationToken cancellationToken);
}

/// <summary>The answer to <see cref="ISourceRecords.NamedBatches"/>.</summary>
/// <param name="Read">
/// Whether every source record was read. False is what the type's own zero carries: where
/// one was not read, no batch has been shown to be named by none, whatever
/// <paramref name="Named"/> holds.
/// </param>
/// <param name="Named">The names of the batches' folders a record names.</param>
public readonly record struct SourceRecordsRead(bool Read, IReadOnlyCollection<string>? Named = null)
{
    /// <summary>Whether a record names the batch's folder <paramref name="batch"/>.</summary>
    public bool Names(string batch) => Named is not null && Named.Contains(batch);
}

/// <summary>
/// The app's <see cref="ISourceRecords"/>, read from the registry, where Windows Installer
/// keeps these records. It opens no source folder, so it waits for no drive or share.
/// </summary>
/// <remarks>
/// THE RECORDS, each read for every program and every patch:
/// <list type="bullet">
///   <item>the source list, the <c>SourceList</c> key and every key under it, of each
///   program under <c>Products</c> and each patch under <c>Patches</c>, in each place
///   Windows Installer registers them: <c>HKLM\SOFTWARE\Classes\Installer</c> for the
///   whole PC, <c>HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed\&lt;SID&gt;\Installer</c>
///   for an account's managed programs, and <c>Software\Microsoft\Installer</c> in each
///   account's own hive for its others;</item>
///   <item>the <c>InstallProperties</c> key of each program under
///   <c>HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\&lt;SID&gt;\Products</c>,
///   whose <c>InstallSource</c> value is the folder the program was installed from.</item>
/// </list>
/// Every string value of those keys is read, whatever its name.
///
/// A VALUE NAMES A BATCH'S FOLDER where it holds the folder's name anywhere, or its short
/// name as one whole component of a path, case ignored, as stored or with this process's
/// environment variables expanded. A batch's folder is named with 32 random hexadecimal
/// characters, so text holding that name is a path to that folder or into it, however
/// the path reaches the Installer folder.
///
/// AN ACCOUNT'S OWN HIVE IS READ ONLY WHILE IT IS LOADED, which is while the account is
/// signed in. An account that is not, and that has a program or a patch under
/// <c>UserData</c> that its <c>Managed</c> key does not hold, has a source list nobody can
/// read, and the answer is then that the records were not all read. So is it for any key
/// here that is there and will not open or list.
/// </remarks>
internal sealed class RegistrySourceRecords : ISourceRecords
{
    private const string MachineClasses = @"SOFTWARE\Classes\Installer";
    private const string Managed = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed";
    private const string UserData = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData";
    private const string HiveInstaller = @"Software\Microsoft\Installer";

    // A source list holds its values and one level of keys under it. A key deeper than
    // this is not one Windows Installer writes, and is taken as not read.
    private const int DeepestKey = 8;

    private readonly IRegistryReader _registry;
    private readonly Func<string[]?> _loadedHives;

    /// <summary>Production constructor: the hives loaded under HKEY_USERS, as the records' watch lists them.</summary>
    public RegistrySourceRecords(IRegistryReader registry)
        : this(registry, RecordsWatchFactory.LoadedHiveNames) { }

    /// <summary>
    /// Test constructor: <paramref name="loadedHives"/> gives the names under HKEY_USERS,
    /// null where they could not all be read.
    /// </summary>
    internal RegistrySourceRecords(IRegistryReader registry, Func<string[]?> loadedHives)
    {
        _registry = registry;
        _loadedHives = loadedHives;
    }

    public SourceRecordsRead NamedBatches(IReadOnlyList<LeftBatch> batches, CancellationToken cancellationToken)
    {
        var walk = new Walk(_registry, batches, cancellationToken);

        // The hives are listed before any is read, and each one read is asked for again
        // at the end: one unloaded meanwhile reads as keys that are not there.
        var loaded = _loadedHives();
        if (loaded is null) return default;

        if (!walk.Registrations(Key.Machine(MachineClasses))) return default;

        var managed = _registry.LocalMachineSubKeys(Managed, cancellationToken);
        if (managed.Presence == RegistryKeyPresence.Unreadable) return default;
        foreach (var sid in managed.Names ?? [])
            if (!walk.Registrations(Key.Machine(Managed + @"\" + sid + @"\Installer"))) return default;

        foreach (var hive in loaded)
            if (!walk.Registrations(Key.User(hive, HiveInstaller))) return default;

        var accounts = _registry.LocalMachineSubKeys(UserData, cancellationToken);
        if (accounts.Presence == RegistryKeyPresence.Unreadable) return default;
        foreach (var sid in accounts.Names ?? [])
        {
            var account = UserData + @"\" + sid;
            var products = _registry.LocalMachineSubKeys(account + @"\Products", cancellationToken);
            if (products.Presence == RegistryKeyPresence.Unreadable) return default;
            foreach (var product in products.Names ?? [])
                if (!walk.Values(Key.Machine(account + @"\Products\" + product + @"\InstallProperties")))
                    return default;

            // The whole PC's programs have their source lists under HKLM, read above, and
            // so has an account whose hive was.
            if (string.Equals(sid, DirectoryGuard.SystemSid, StringComparison.OrdinalIgnoreCase)
                || loaded.Contains(sid, StringComparer.OrdinalIgnoreCase))
                continue;

            var patches = _registry.LocalMachineSubKeys(account + @"\Patches", cancellationToken);
            if (patches.Presence == RegistryKeyPresence.Unreadable) return default;
            var itsManaged = Managed + @"\" + sid + @"\Installer";
            if (!EachIsManaged(itsManaged + @"\Products", products.Names)
                || !EachIsManaged(itsManaged + @"\Patches", patches.Names))
                return default;
        }

        foreach (var hive in loaded)
            if (_registry.UserHivePresence(hive) != RegistryKeyPresence.Present) return default;

        return new SourceRecordsRead(true, walk.Named);
    }

    /// <summary>
    /// Whether each of <paramref name="codes"/> has a key under <paramref name="managedKey"/>,
    /// so that its source list is one read from HKLM. False where one has none, or the
    /// question does not answer.
    /// </summary>
    private bool EachIsManaged(string managedKey, IReadOnlyList<string>? codes)
    {
        foreach (var code in codes ?? [])
            if (_registry.LocalMachineKeyPresence(managedKey + @"\" + code) != RegistryKeyPresence.Present)
                return false;
        return true;
    }

    /// <summary>A key under HKLM, or in the hive loaded under HKEY_USERS as <see cref="Hive"/>.</summary>
    private readonly record struct Key(string? Hive, string Path)
    {
        public static Key Machine(string path) => new(null, path);
        public static Key User(string hive, string path) => new(hive, path);
        public Key Under(string name) => this with { Path = Path + @"\" + name };
    }

    private sealed class Walk(IRegistryReader registry, IReadOnlyList<LeftBatch> batches, CancellationToken cancellationToken)
    {
        private static readonly char[] Separators = ['\\', '/'];

        public HashSet<string> Named { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Reads the source list of every program and patch registered under
        /// <paramref name="root"/>. False where any key that is there did not read.
        /// </summary>
        public bool Registrations(Key root)
        {
            foreach (var kind in new[] { "Products", "Patches" })
            {
                var codes = SubKeys(root.Under(kind));
                if (codes.Presence == RegistryKeyPresence.Unreadable) return false;
                foreach (var code in codes.Names ?? [])
                    if (!Tree(root.Under(kind).Under(code).Under("SourceList"), 0))
                        return false;
            }
            return true;
        }

        private bool Tree(Key key, int depth)
        {
            if (depth > DeepestKey) return false;
            if (!Values(key)) return false;

            var under = SubKeys(key);
            if (under.Presence == RegistryKeyPresence.Unreadable) return false;
            foreach (var name in under.Names ?? [])
                if (!Tree(key.Under(name), depth + 1))
                    return false;
            return true;
        }

        /// <summary>
        /// Reads every string value of <paramref name="key"/>. False where the key is
        /// there and did not read; a key that is not there holds nothing.
        /// </summary>
        public bool Values(Key key)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = key.Hive is null
                ? registry.LocalMachineValues(key.Path, cancellationToken)
                : registry.UserValues(key.Hive, key.Path, cancellationToken);
            if (values.Presence == RegistryKeyPresence.Unreadable) return false;

            foreach (var value in values.Values ?? [])
            {
                if (value.Text is not { Length: > 0 } text) continue;
                Note(text);
                if (text.IndexOf('%') >= 0) Note(Environment.ExpandEnvironmentVariables(text));
            }
            return true;
        }

        private RegistrySubKeyNames SubKeys(Key key) => key.Hive is null
            ? registry.LocalMachineSubKeys(key.Path, cancellationToken)
            : registry.UserSubKeys(key.Hive, key.Path, cancellationToken);

        private void Note(string text)
        {
            string[]? components = null;
            foreach (var batch in batches)
            {
                if (text.IndexOfOrdinal(batch.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Named.Add(batch.Name);
                    continue;
                }

                if (batch.ShortName is not { Length: > 0 } shortName) continue;
                components ??= text.Split(Separators);
                foreach (var component in components)
                {
                    if (!string.Equals(component.Trim(' ', '"'), shortName, StringComparison.OrdinalIgnoreCase)) continue;
                    Named.Add(batch.Name);
                    break;
                }
            }
        }
    }
}
