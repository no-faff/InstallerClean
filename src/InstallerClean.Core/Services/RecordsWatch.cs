using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// Arms the app's <see cref="IRecordsWatch"/>: a registry change notification
/// (<c>RegNotifyChangeKeyValue</c>) on each key Windows Installer keeps its records in.
/// </summary>
/// <remarks>
/// THE KEYS, and the list is kept in one method (<see cref="RecordKeys"/>):
/// <list type="bullet">
///   <item><c>HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer</c> and everything
///   under it, which holds every account's <c>UserData</c>, <c>Managed</c> and
///   <c>InProgress</c>;</item>
///   <item><c>HKLM\SOFTWARE\Classes\Installer</c> and everything under it;</item>
///   <item>for each registry hive loaded under <c>HKEY_USERS</c>:
///   <c>Software\Microsoft\Installer</c> and everything under it, where it is there, and
///   <c>Software\Microsoft</c> itself on a handle of its own, without what is under its
///   subkeys, so that <c>Installer</c> being made, or deleted and made again, is seen.</item>
/// </list>
/// A key of the first two that is not there is watched through the key above it in the
/// same way. A key watched without what is under its subkeys reports its own values and
/// the names of its subkeys.
///
/// WHAT THE NOTIFICATION REQUIRES, each of which a change here could break with nothing
/// to show for it. Every key is opened for the watch alone and stays open until the watch
/// is disposed of. The function is called once on each handle. The thread-agnostic flag is
/// set: without it the registration ends with the thread that made it, and the event is
/// set a moment after the write that caused it has returned, where with it the event is
/// set first, which is what lets a read answer without waiting. Keys are opened in the 64-bit
/// view by name, and the machine's classes at <c>HKLM\SOFTWARE\Classes</c>, never through
/// <c>HKEY_CLASSES_ROOT</c>, which merges the calling account's own classes in.
///
/// EVERY HANDLE CARRIES THE RIGHT TO BE ASKED AS WELL AS THE RIGHT TO BE TOLD
/// (KEY_QUERY_VALUE with KEY_NOTIFY). A hive that is unloaded takes its keys with it, and
/// a read asks each handle a question so that a handle whose key has gone answers. A key
/// that refuses either right leaves the watch unestablished: a handle that cannot be asked
/// is one that could stop reporting unnoticed. Both rights are part of KEY_READ, which the
/// scan's own reads of these keys ask for.
/// </remarks>
internal sealed class RecordsWatchFactory : IRecordsWatchFactory
{
    private const string MachineInstaller = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer";
    private const string MachineClassesInstaller = @"SOFTWARE\Classes\Installer";
    private const string HiveMicrosoft = @"Software\Microsoft";
    private const string HiveInstaller = @"Software\Microsoft\Installer";

    private readonly Func<IReadOnlyList<string>, IEnumerable<WatchedKey>> _keys;
    private readonly Func<string[]?> _loadedHives;

    /// <summary>Production constructor: the keys Windows Installer keeps its records in.</summary>
    public RecordsWatchFactory() : this(RecordKeys, LoadedHiveNames) { }

    /// <summary>
    /// Test constructor: the keys <paramref name="keys"/> gives for the hives
    /// <paramref name="loadedHives"/> lists, so a test watches a key of its own and says
    /// which hives are loaded. A null list is one that could not be read.
    /// </summary>
    internal RecordsWatchFactory(
        Func<IReadOnlyList<string>, IEnumerable<WatchedKey>> keys, Func<string[]?> loadedHives)
    {
        _keys = keys;
        _loadedHives = loadedHives;
    }

    /// <summary>
    /// The names of the subkeys of HKEY_USERS, one for each loaded hive, or null where
    /// they could not all be read.
    /// </summary>
    internal static string[]? LoadedHiveNames()
    {
        const int ERROR_SUCCESS = 0;
        const int ERROR_NO_MORE_ITEMS = 259;

        // A registry key name runs to 255 characters, and the terminator.
        var buffer = new char[256];
        using var users = Predefined(RegistryHive.Users);
        var names = new List<string>();
        for (uint index = 0; ; index++)
        {
            var length = (uint)buffer.Length;
            var code = Advapi32.RegEnumKeyEx(
                users, index, buffer, ref length,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (code == ERROR_NO_MORE_ITEMS) return names.ToArray();
            if (code != ERROR_SUCCESS) return null;
            names.Add(new string(buffer, 0, (int)length));
        }
    }

    /// <summary>
    /// A hive's predefined handle, which nothing closes. A base key's own Handle on .NET
    /// Framework opens the hive's root again at every read of it.
    /// </summary>
    private static SafeRegistryHandle Predefined(RegistryHive hive) =>
        new(new IntPtr((int)hive), ownsHandle: false);

    /// <summary>
    /// One key to watch: with everything under it where <paramref name="Subtree"/> is
    /// true, and otherwise its own values and the names of its subkeys. Where the key is
    /// not there and <paramref name="ParentWhereAbsent"/> is true, the names of the
    /// subkeys of the key above it are watched in its place, so the key being made is
    /// seen; where that is false a key that is not there is left out.
    /// </summary>
    internal readonly record struct WatchedKey(RegistryHive Root, string Path, bool Subtree, bool ParentWhereAbsent);

    /// <summary>
    /// The keys of the class remarks, for the hives in <paramref name="loadedHives"/>.
    /// Within a hive <c>Software\Microsoft</c> comes before <c>Installer</c> under it, so
    /// an <c>Installer</c> key made between the two is seen by the first.
    /// </summary>
    internal static IEnumerable<WatchedKey> RecordKeys(IReadOnlyList<string> loadedHives)
    {
        yield return new WatchedKey(RegistryHive.LocalMachine, MachineInstaller, Subtree: true, ParentWhereAbsent: true);
        yield return new WatchedKey(RegistryHive.LocalMachine, MachineClassesInstaller, Subtree: true, ParentWhereAbsent: true);

        // A hive with no Software\Microsoft is not an account's own hive, where Windows
        // keeps that account's settings under that key from the account's first sign-in.
        // The classes hive loaded beside each account's is one such, and both of its keys
        // here are simply not there.
        foreach (var hive in loadedHives)
        {
            yield return new WatchedKey(RegistryHive.Users, hive + @"\" + HiveMicrosoft, Subtree: false, ParentWhereAbsent: false);
            yield return new WatchedKey(RegistryHive.Users, hive + @"\" + HiveInstaller, Subtree: true, ParentWhereAbsent: false);
        }
    }

    public IRecordsWatch Arm()
    {
        var watch = new RecordsWatch(_loadedHives);
        try
        {
            // The loaded hives are listed before any key is opened, so a hive loaded while
            // the keys are being armed shows as a difference at the first read. One
            // unloaded meanwhile has keys that are not there, and shows the same way.
            if (!watch.RecordLoadedHives()) return watch.Unestablished();

            foreach (var key in _keys(watch.LoadedHives))
            {
                using var root = Predefined(key.Root);

                var added = watch.Add(root, key.Path, key.Subtree);
                if (added == Added.NotThere && key.ParentWhereAbsent)
                {
                    // The key above is armed first and the key itself is then tried
                    // again: made before the key above was armed, it opens now, and made
                    // after, it sets the key above's event.
                    added = watch.Add(root, key.Path.Substring(0, key.Path.LastIndexOf('\\')), subtree: false);
                    if (added == Added.NotThere) return watch.Unestablished();
                    if (added == Added.Watched && watch.Add(root, key.Path, key.Subtree) == Added.Refused)
                        added = Added.Refused;
                }

                // A key that is not there, and asks for nothing in its place, is left out.
                if (added == Added.Refused) return watch.Unestablished();
            }

            if (watch.Count == 0) return watch.Unestablished();
            watch.Armed();
            return watch;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            CrashLog.TryWrite(ex);
            return watch.Unestablished();
        }
    }

    private enum Added
    {
        Refused,
        NotThere,
        Watched,
    }

    private sealed class RecordsWatch : IRecordsWatch
    {
        private const int ERROR_SUCCESS = 0;
        private const int ERROR_FILE_NOT_FOUND = 2;

        private const uint Filter = Advapi32.REG_NOTIFY_CHANGE_NAME
            | Advapi32.REG_NOTIFY_CHANGE_LAST_SET
            | Advapi32.REG_NOTIFY_THREAD_AGNOSTIC;

        private readonly Func<string[]?> _loadedHives;
        private readonly List<(SafeRegistryHandle Key, ManualResetEvent Changed)> _watched = new();
        private readonly object _gate = new();
        private bool _armed;
        private bool _changed;
        private bool _disposed;

        public RecordsWatch(Func<string[]?> loadedHives) => _loadedHives = loadedHives;

        /// <summary>The names under HKEY_USERS when the watch was armed.</summary>
        public string[] LoadedHives { get; private set; } = [];

        public bool RecordLoadedHives()
        {
            var names = _loadedHives();
            if (names is null) return false;
            LoadedHives = names;
            return true;
        }

        public void Armed() => _armed = true;

        /// <summary>This watch with every handle closed, reading Unestablished from now on.</summary>
        public RecordsWatch Unestablished()
        {
            Release();
            _armed = false;
            return this;
        }

        /// <summary>How many keys are watched.</summary>
        public int Count => _watched.Count;

        /// <summary>
        /// Opens one key for the watch and arms the notification on it, its subtree too
        /// where <paramref name="subtree"/> is true and otherwise the key's own values and
        /// the names of its subkeys.
        /// </summary>
        public Added Add(SafeRegistryHandle root, string path, bool subtree)
        {
            var code = Advapi32.RegOpenKeyEx(
                root, path, 0,
                Advapi32.KEY_NOTIFY | Advapi32.KEY_QUERY_VALUE | Advapi32.KEY_WOW64_64KEY,
                out var opened);
            if (code == ERROR_FILE_NOT_FOUND) return Added.NotThere;
            if (code != ERROR_SUCCESS) return Added.Refused;

            var key = new SafeRegistryHandle(opened, ownsHandle: true);
            var changed = new ManualResetEvent(false);
            _watched.Add((key, changed));

            return Advapi32.RegNotifyChangeKeyValue(
                key, subtree, Filter, changed.SafeWaitHandle, fAsynchronous: true) == ERROR_SUCCESS
                ? Added.Watched
                : Added.Refused;
        }

        public RecordsWatchReading Read()
        {
            lock (_gate)
            {
                if (_disposed || !_armed) return RecordsWatchReading.Unestablished;
                if (_changed) return RecordsWatchReading.Changed;

                try
                {
                    foreach (var (key, changed) in _watched)
                    {
                        // Armed with the thread-agnostic flag, the event is set before the
                        // write that caused it returns to its caller, so a test of it that
                        // does not wait is the whole of the question.
                        if (HandleWait.One(changed.SafeWaitHandle, TimeSpan.Zero) != HandleWaitResult.TimedOut)
                            return Latch();

                        // A key that has gone with its hive answers this with an error,
                        // whether or not its event was set on the way.
                        if (Advapi32.RegQueryInfoKey(
                                key, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out _, IntPtr.Zero, IntPtr.Zero,
                                out _, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != ERROR_SUCCESS)
                            return Latch();
                    }

                    // A hive loaded since the watch was armed is under no watched key, and
                    // one unloaded and loaded again is a different set of keys under the
                    // same name, which the question above has already answered for.
                    var now = _loadedHives();
                    if (now is null || !SameNames(LoadedHives, now)) return Latch();

                    return RecordsWatchReading.Unchanged;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    // A read that could not be made has not shown the records unchanged.
                    CrashLog.TryWrite(ex);
                    return Latch();
                }
            }
        }

        // Changed is kept: the events are never reset, and a later read must not answer
        // Unchanged because a hive that came back made the names match again.
        private RecordsWatchReading Latch()
        {
            _changed = true;
            return RecordsWatchReading.Changed;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                Release();
            }
        }

        // The key first and its event after it: closing the key ends the registration, so
        // nothing is left to set an event that has been closed.
        private void Release()
        {
            foreach (var (key, changed) in _watched)
            {
                key.Dispose();
                changed.Dispose();
            }

            _watched.Clear();
        }

        private static bool SameNames(string[] armed, string[] now)
        {
            if (armed.Length != now.Length) return false;
            var remaining = new HashSet<string>(armed, StringComparer.OrdinalIgnoreCase);
            foreach (var name in now)
                if (!remaining.Remove(name)) return false;
            return remaining.Count == 0;
        }
    }
}
