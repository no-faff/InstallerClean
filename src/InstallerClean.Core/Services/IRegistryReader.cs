namespace InstallerClean.Services;

/// <summary>Registry-read abstraction. All reads target HKLM in the 64-bit (Registry64) view; the keys checked are unwowed.</summary>
public interface IRegistryReader
{
    /// <summary>True if the relative HKLM path resolves to an existing subkey; false on absent or read failure.</summary>
    bool LocalMachineKeyExists(string relativePath);

    /// <summary>REG_MULTI_SZ value as a string array, or null if absent, wrong type, or read fails.</summary>
    string[]? LocalMachineMultiStringValue(string keyPath, string valueName);

    /// <summary>
    /// The immediate subkey names under an HKLM path, or null where the key is
    /// absent or the read fails.
    ///
    /// NULL AND EMPTY MEAN DIFFERENT THINGS AND A CALLER THAT CONFLATES THEM IS
    /// WRONG. Empty is an answer: the key is there and holds nothing. Null is the
    /// absence of an answer, and the caller that reads the installed accounts off
    /// this treats it as a question it could not put, because a list it could not
    /// read is not a list of nobody.
    /// </summary>
    string[]? LocalMachineSubKeyNames(string relativePath);

    /// <summary>
    /// A REG_DWORD value, four-state because the three ways of having no number
    /// are three different things to have found out and a caller reporting them
    /// would otherwise have to pick one sentence for all three. Absent is an
    /// answer (the setting is at its default), a wrong type is a machine
    /// configured in a way nothing here anticipated, and a failed read is no
    /// answer at all.
    /// </summary>
    RegistryDwordRead LocalMachineDwordValue(string keyPath, string valueName);
}

/// <summary>How a REG_DWORD read turned out. See <see cref="RegistryDwordRead"/>.</summary>
public enum RegistryDwordState
{
    /// <summary>The value was there and was a number, which is in <see cref="RegistryDwordRead.Value"/>.</summary>
    Read,

    /// <summary>The key or the value is not there. Not a failure: it is how a setting left at its default reads.</summary>
    Absent,

    /// <summary>The value is there and is not a number, so there is nothing to report but the fact.</summary>
    WrongType,

    /// <summary>The read itself failed, so nothing at all was established.</summary>
    Unreadable,
}

/// <summary>
/// One REG_DWORD read. <see cref="Value"/> is meaningful only when
/// <see cref="State"/> is <see cref="RegistryDwordState.Read"/>; it is zero in
/// every other state, and zero is a legitimate setting value, so a caller that
/// reads the number without the state cannot tell a machine set to 0 from a
/// machine that answered nothing.
/// </summary>
public readonly record struct RegistryDwordRead(RegistryDwordState State, int Value = 0);
