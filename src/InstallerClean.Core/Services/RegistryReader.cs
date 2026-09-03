using System.Security;
using Microsoft.Win32;

namespace InstallerClean.Services;

/// <summary>Production IRegistryReader: opens HKLM Registry64 and reports the
/// documented failure modes (SecurityException, IOException,
/// UnauthorizedAccessException, ObjectDisposedException) as the state that says
/// the read did not answer, so a caller acting on the answer can tell that apart
/// from a key or a value that is simply not there.
/// OutOfMemoryException and StackOverflowException propagate so a real
/// memory-pressure failure isn't silently downgraded to "no signal" by
/// PendingRebootService.Check.</summary>
internal sealed class RegistryReader : IRegistryReader
{
    public RegistryKeyPresence LocalMachineKeyPresence(string relativePath)
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hive.OpenSubKey(relativePath);
            return key is not null ? RegistryKeyPresence.Present : RegistryKeyPresence.Absent;
        }
        catch (SecurityException) { return RegistryKeyPresence.Unreadable; }
        catch (IOException) { return RegistryKeyPresence.Unreadable; }
        catch (UnauthorizedAccessException) { return RegistryKeyPresence.Unreadable; }
        catch (ObjectDisposedException) { return RegistryKeyPresence.Unreadable; }
    }

    public RegistryMultiStringRead LocalMachineMultiStringValue(string keyPath, string valueName)
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hive.OpenSubKey(keyPath);

            // A key that is not there and a value that is not there answer alike,
            // because the question is what the value holds and "nothing" is true of
            // both. Neither is the same as a key the read could not open, which
            // leaves the caller with no answer at all and comes out of the catches.
            if (key is null) return new RegistryMultiStringRead(RegistryMultiStringState.Absent);

            var raw = key.GetValue(valueName);
            if (raw is null) return new RegistryMultiStringRead(RegistryMultiStringState.Absent);

            // A REG_MULTI_SZ comes back as string[]. Anything else is a value
            // written in a form this does not read, and it is reported as that
            // rather than as an absence: the caller draws opposite conclusions from
            // "there is nothing here" and "there is something here I cannot read".
            return raw is string[] entries
                ? new RegistryMultiStringRead(RegistryMultiStringState.Read, entries)
                : new RegistryMultiStringRead(RegistryMultiStringState.WrongType);
        }
        catch (SecurityException) { return new RegistryMultiStringRead(RegistryMultiStringState.Unreadable); }
        catch (IOException) { return new RegistryMultiStringRead(RegistryMultiStringState.Unreadable); }
        catch (UnauthorizedAccessException) { return new RegistryMultiStringRead(RegistryMultiStringState.Unreadable); }
        catch (ObjectDisposedException) { return new RegistryMultiStringRead(RegistryMultiStringState.Unreadable); }
    }

    public RegistryDwordRead LocalMachineDwordValue(string keyPath, string valueName)
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hive.OpenSubKey(keyPath);
            if (key is null) return new RegistryDwordRead(RegistryDwordState.Absent);

            // DoNotExpandEnvironmentNames is inert on a REG_DWORD and is passed
            // so that a value stored as REG_EXPAND_SZ answers as the string it
            // is rather than as whatever the expansion produces, which would be
            // one more shape to tell apart from a number for no gain.
            var raw = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (raw is null) return new RegistryDwordRead(RegistryDwordState.Absent);

            // A REG_DWORD comes back as int; REG_QWORD comes back as long and is
            // not what this asks for. Neither is silently coerced: the caller is
            // measuring how machines are configured, and a coercion would hide
            // exactly the configuration worth hearing about.
            return raw is int value
                ? new RegistryDwordRead(RegistryDwordState.Read, value)
                : new RegistryDwordRead(RegistryDwordState.WrongType);
        }
        catch (SecurityException) { return new RegistryDwordRead(RegistryDwordState.Unreadable); }
        catch (IOException) { return new RegistryDwordRead(RegistryDwordState.Unreadable); }
        catch (UnauthorizedAccessException) { return new RegistryDwordRead(RegistryDwordState.Unreadable); }
        catch (ObjectDisposedException) { return new RegistryDwordRead(RegistryDwordState.Unreadable); }
    }
}
