using System.Security.Cryptography;
using System.Text;
using InstallerClean.Helpers;
using InstallerClean.Interop;
using InstallerClean.Interop.Native;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// A digest of everything Windows Installer records about what is installed, taken so
/// that two readings can be compared: one when a batch's files are checked, and one
/// inside each <see cref="InstallerHold"/> immediately before any file is touched. Equal
/// readings mean no registration, patch, source list or pending file rename that the
/// check relied on has changed in between.
///
/// It reads three things and digests them in a fixed order:
/// <list type="bullet">
/// <item>Windows Installer's own answers: every patch registration on the machine with
/// its state, removability and cached package, and every product with its cached
/// package.</item>
/// <item>Windows Installer's registry trees: every key's name and last-write time. A key
/// that cannot be opened is recorded by name as unreadable, and its parent's listing
/// still shows a subkey arriving or going under it.</item>
/// <item>The list of loaded user hives and the raw PendingFileRenameOperations entries.</item>
/// </list>
/// The registry half sees a change that is undone again between two readings, because
/// the undo moves a last-write time; the API half sees what Windows Installer reports
/// from keys this process cannot open.
/// </summary>
public interface IInstallerRecordsFingerprint
{
    /// <summary>Takes a reading. Never throws for a read that fails: the reading is then incomplete.</summary>
    InstallerRecordsPrint Take();
}

/// <summary>
/// One reading. An incomplete reading matches nothing, itself included, so a failure
/// to read is never taken as "unchanged".
/// </summary>
public sealed class InstallerRecordsPrint
{
    public InstallerRecordsPrint(string digest, string? failure)
    {
        Digest = digest;
        Failure = failure;
    }

    /// <summary>SHA-256 of the reading, as hex.</summary>
    public string Digest { get; }

    /// <summary>Why the reading is incomplete, or null where every part was read.</summary>
    public string? Failure { get; }

    public bool Complete => Failure is null;

    /// <summary>A reading that was never taken, which matches nothing.</summary>
    public static InstallerRecordsPrint None { get; } = new(string.Empty, "no reading was taken");

    public bool Matches(InstallerRecordsPrint? other) =>
        other is not null && Complete && other.Complete
        && string.Equals(Digest, other.Digest, StringComparison.Ordinal);
}

/// <summary>The registry half of a reading, behind an interface so tests can supply their own trees.</summary>
public interface IInstallerRegistrySnapshot
{
    /// <summary>
    /// Writes one line per key and per item to <paramref name="sink"/>, in a fixed
    /// order. Returns null, or why the reading is incomplete.
    /// </summary>
    string? Write(Action<string> sink);
}

/// <summary>The production <see cref="IInstallerRecordsFingerprint"/>.</summary>
public sealed class InstallerRecordsFingerprint : IInstallerRecordsFingerprint
{
    private const string AllUsersSid = "S-1-1-0";

    private static readonly string[] PatchProperties = { "State", "Uninstallable", "LocalPackage" };

    /// <summary>
    /// A walk still answering at this index makes the reading incomplete, so a list that
    /// never ends cannot hold every installer on the machine. The scan stops at the same place.
    /// </summary>
    internal const uint MaxIndex = 10_000;

    private readonly IMsiApi _msi;
    private readonly IInstallerRegistrySnapshot _registry;

    public InstallerRecordsFingerprint(IMsiApi msi, IInstallerRegistrySnapshot registry)
    {
        _msi = msi;
        _registry = registry;
    }

    public InstallerRecordsPrint Take()
    {
        using var sha = SHA256.Create();
        var failure = (string?)null;
        void Sink(string line)
        {
            var bytes = Encoding.UTF8.GetBytes(line + "\n");
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        try
        {
            failure = WritePatches(Sink) ?? WriteProducts(Sink) ?? _registry.Write(Sink);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            failure = ex.GetType().Name + ": " + ex.Message;
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var digest = BitConverter.ToString(sha.Hash!).Replace("-", string.Empty);
        return new InstallerRecordsPrint(digest, failure);
    }

    private string? WritePatches(Action<string> sink)
    {
        var patch = new char[Msi.GuidBufferLength];
        var product = new char[Msi.GuidBufferLength];
        for (uint index = 0; index < MaxIndex; index++)
        {
            Array.Clear(patch, 0, patch.Length);
            Array.Clear(product, 0, product.Length);
            var sid = new char[256];
            var sidLength = (uint)sid.Length;
            var rc = _msi.EnumPatches(null, AllUsersSid, MsiInstallContext.All, MsiPatchFilter.All, index,
                patch, product, out var context, sid, ref sidLength);
            if (rc == MsiError.MoreData)
            {
                sid = new char[sidLength + 1];
                sidLength = (uint)sid.Length;
                rc = _msi.EnumPatches(null, AllUsersSid, MsiInstallContext.All, MsiPatchFilter.All, index,
                    patch, product, out context, sid, ref sidLength);
            }
            if (rc == MsiError.NoMoreItems)
                return null;
            if (rc != MsiError.Success)
                return $"MsiEnumPatchesEx returned {rc} at index {index}";

            var patchCode = Text(patch);
            var productCode = Text(product);
            if (patchCode.Length == 0 || productCode.Length == 0)
                return $"MsiEnumPatchesEx named no patch or product at index {index}";
            var userSid = context == MsiInstallContext.Machine ? null : SidText(sid, sidLength);
            var line = new StringBuilder($"patch|{patchCode}|{productCode}|{(uint)context}|{userSid}");
            foreach (var property in PatchProperties)
                line.Append('|').Append(ReadPatch(patchCode, productCode, userSid, context, property));
            sink(line.ToString());
        }
        return $"MsiEnumPatchesEx was still answering at index {MaxIndex}";
    }

    private string? WriteProducts(Action<string> sink)
    {
        var product = new char[Msi.GuidBufferLength];
        for (uint index = 0; index < MaxIndex; index++)
        {
            Array.Clear(product, 0, product.Length);
            var sid = new char[256];
            var sidLength = (uint)sid.Length;
            var rc = _msi.EnumProducts(null, AllUsersSid, MsiInstallContext.All, index,
                product, out var context, sid, ref sidLength);
            if (rc == MsiError.MoreData)
            {
                sid = new char[sidLength + 1];
                sidLength = (uint)sid.Length;
                rc = _msi.EnumProducts(null, AllUsersSid, MsiInstallContext.All, index,
                    product, out context, sid, ref sidLength);
            }
            if (rc == MsiError.NoMoreItems)
                return null;
            if (rc != MsiError.Success)
                return $"MsiEnumProductsEx returned {rc} at index {index}";

            var productCode = Text(product);
            if (productCode.Length == 0)
                return $"MsiEnumProductsEx named no product at index {index}";
            var userSid = context == MsiInstallContext.Machine ? null : SidText(sid, sidLength);
            sink($"product|{productCode}|{(uint)context}|{userSid}|{ReadProduct(productCode, userSid, context)}");
        }
        return $"MsiEnumProductsEx was still answering at index {MaxIndex}";
    }

    /// <summary>
    /// One property, as "=value", or as "!code" where the read failed. A failed read is
    /// part of the reading rather than a reason to abandon it: the same failure twice
    /// digests the same, and a property that starts or stops reading digests differently.
    /// </summary>
    private string ReadPatch(string patch, string product, string? sid, MsiInstallContext context, string property)
    {
        var value = new char[512];
        var length = (uint)value.Length;
        var rc = _msi.GetPatchInfo(patch, product, sid, context, property, value, ref length);
        if (rc == MsiError.MoreData)
        {
            value = new char[length + 1];
            length = (uint)value.Length;
            rc = _msi.GetPatchInfo(patch, product, sid, context, property, value, ref length);
        }
        return rc == MsiError.Success ? "=" + new string(value, 0, (int)length) : "!" + rc;
    }

    private string ReadProduct(string product, string? sid, MsiInstallContext context)
    {
        var value = new char[512];
        var length = (uint)value.Length;
        var rc = _msi.GetProductInfo(product, sid, context, "LocalPackage", value, ref length);
        if (rc == MsiError.MoreData)
        {
            value = new char[length + 1];
            length = (uint)value.Length;
            rc = _msi.GetProductInfo(product, sid, context, "LocalPackage", value, ref length);
        }
        return rc == MsiError.Success ? "=" + new string(value, 0, (int)length) : "!" + rc;
    }

    /// <summary>The SID the walk wrote, or null for a per-machine row, which carries none.</summary>
    private static string? SidText(char[] buffer, uint length)
    {
        var text = Text(buffer);
        if (length < text.Length)
            text = text.Substring(0, (int)length);
        return text.Length == 0 ? null : text;
    }

    private static string Text(char[] buffer)
    {
        var end = Array.IndexOf(buffer, '\0');
        return new string(buffer, 0, end < 0 ? buffer.Length : end);
    }
}

/// <summary>
/// The production <see cref="IInstallerRegistrySnapshot"/>: Windows Installer's trees in
/// the 64-bit view of HKEY_LOCAL_MACHINE, the Installer key of every loaded user hive,
/// the names of the loaded hives, and the PendingFileRenameOperations value.
///
/// Each key is listed once with RegEnumKeyEx, which gives every subkey's name and
/// last-write time in the same call, so a value set or deleted anywhere in a walked key
/// shows as that key's time, and a subkey added, removed or recreated shows in its
/// parent's list. Nothing here waits or tries again: a listing that fails part-way
/// makes the reading incomplete, and the comparison it is part of then fails.
///
/// Two kinds of key are recorded but not walked. A key named Components holds one
/// subkey per component on the machine, which changes only with an installation that
/// also writes the product keys walked here. A key named Usage records when an
/// advertised program was last started, which changes with no installation at all.
///
/// A key that will not open is recorded as unreadable, and two readings that both find
/// it so compare equal. The check a reading is compared against could not read that key
/// either, and Windows Installer's own answers, which the service gives from keys this
/// process cannot open, are the other half of the reading.
/// </summary>
public sealed class InstallerRegistrySnapshot : IInstallerRegistrySnapshot
{
    private static readonly string[] MachineRoots =
    {
        @"SOFTWARE\Classes\Installer",
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer",
    };

    private const string UserInstallerKey = @"Software\Microsoft\Installer";

    // KEY_READ with KEY_WOW64_64KEY, so the 64-bit view is read from a 32-bit process too.
    private const int KeyRead64 = 0x20019 | 0x0100;
    private const int ErrorSuccess = 0;
    private const int ErrorFileNotFound = 2;
    private const int ErrorNoMoreItems = 259;

    /// <summary>
    /// More keys than this in one reading makes it incomplete rather than letting a
    /// listing that never ends hold every installer on the machine.
    /// </summary>
    internal const int MaxKeys = 500_000;

    public string? Write(Action<string> sink)
    {
        var walk = new Walk(sink);
        using (var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        {
            foreach (var root in MachineRoots)
                walk.From(machine.Handle, root, "HKLM\\" + root);
            if (walk.Failure is null)
                WritePendingRenames(machine, sink);
        }
        if (walk.Failure is not null)
            return walk.Failure;

        using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        var hives = walk.List(users.Handle, "HKU");
        if (hives is null)
            return walk.Failure;
        foreach (var hive in hives)
            walk.From(users.Handle, hive.Name + "\\" + UserInstallerKey, "HKU\\" + hive.Name + "\\" + UserInstallerKey);
        return walk.Failure;
    }

    private sealed class Walk
    {
        private readonly Action<string> _sink;
        private readonly char[] _name = new char[256];
        private int _keys;

        public Walk(Action<string> sink) => _sink = sink;

        public string? Failure { get; private set; }

        /// <summary>Opens <paramref name="relative"/> under <paramref name="parent"/> and walks it.</summary>
        public void From(SafeRegistryHandle parent, string relative, string label)
        {
            if (Failure is not null)
                return;
            var rc = Advapi32.RegOpenKeyEx(parent, relative, 0, KeyRead64, out var raw);
            if (rc == ErrorFileNotFound)
            {
                _sink("absent|" + label);
                return;
            }
            if (rc != ErrorSuccess)
            {
                _sink("unreadable|" + label + "|" + rc);
                return;
            }
            using var key = new SafeRegistryHandle(raw, ownsHandle: true);
            _sink("open|" + label + "|" + OwnLastWrite(key));
            Below(key, label);
        }

        /// <summary>
        /// The key's own last-write time, which a walk root has no parent listing to give,
        /// or "!" and the error.
        /// </summary>
        private static string OwnLastWrite(SafeRegistryHandle key)
        {
            var time = Marshal.AllocHGlobal(sizeof(long));
            try
            {
                var rc = Advapi32.RegQueryInfoKey(key, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    out _, IntPtr.Zero, IntPtr.Zero, out _, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, time);
                return rc == ErrorSuccess ? Marshal.ReadInt64(time).ToString() : "!" + rc;
            }
            finally
            {
                Marshal.FreeHGlobal(time);
            }
        }

        /// <summary>Lists a key's subkeys with their last-write times, or null where the listing failed.</summary>
        public List<(string Name, long LastWrite)>? List(SafeRegistryHandle key, string label)
        {
            var subkeys = new List<(string, long)>();
            var time = Marshal.AllocHGlobal(sizeof(long));
            try
            {
                for (uint index = 0; ; index++)
                {
                    if (++_keys > MaxKeys)
                    {
                        Failure = $"more than {MaxKeys} registry keys";
                        return null;
                    }
                    var length = (uint)_name.Length;
                    var rc = Advapi32.RegEnumKeyEx(key, index, _name, ref length,
                        IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, time);
                    if (rc == ErrorNoMoreItems)
                        break;
                    if (rc != ErrorSuccess)
                    {
                        Failure = $"RegEnumKeyEx returned {rc} under {label}";
                        return null;
                    }
                    var name = new string(_name, 0, (int)length);
                    var lastWrite = Marshal.ReadInt64(time);
                    subkeys.Add((name, lastWrite));
                    _sink("sub|" + label + "\\" + name + "|" + lastWrite);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(time);
            }
            return subkeys;
        }

        private void Below(SafeRegistryHandle key, string label)
        {
            var subkeys = List(key, label);
            if (subkeys is null)
                return;
            foreach (var (name, _) in subkeys)
            {
                if (string.Equals(name, "Components", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "Usage", StringComparison.OrdinalIgnoreCase))
                    continue;
                From(key, name, label + "\\" + name);
                if (Failure is not null)
                    return;
            }
        }
    }

    private static void WritePendingRenames(RegistryKey machine, Action<string> sink)
    {
        using var sessionManager = machine.OpenSubKeyEarned(PendingRebootService.SessionManagerKey);
        var value = sessionManager is null
            ? null
            : RegistryData.ValueOf(sessionManager, PendingRebootService.PendingFileRenameOperationsValue, doNotExpand: true);
        sink(value switch
        {
            null => "renames|absent",
            string[] entries => "renames|" + string.Join("\u0001", entries),
            byte[] bytes => "renames|bytes|" + BitConverter.ToString(bytes),
            _ => "renames|" + value.GetType().Name + "|" + value,
        });
    }
}
