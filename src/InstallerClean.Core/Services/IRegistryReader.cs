namespace InstallerClean.Services;

/// <summary>Registry-read abstraction. All reads target HKLM in the 64-bit (Registry64) view; the keys checked are unwowed.</summary>
public interface IRegistryReader
{
    /// <summary>
    /// Whether the relative HKLM path resolves to an existing subkey, three-state
    /// because a key that is not on the machine and a key nobody could look at are
    /// two different things to have found out. Absent is an answer; unreadable is
    /// the absence of one, and a caller that acts on what the key's presence means
    /// has to be able to tell them apart.
    /// </summary>
    RegistryKeyPresence LocalMachineKeyPresence(string relativePath);

    /// <summary>
    /// A REG_MULTI_SZ value, four-state for the same reason
    /// <see cref="LocalMachineDwordValue"/> is: the three ways of having no array
    /// are three different things to have found out. Absent is an answer, a wrong
    /// type is a machine configured in a way nothing here anticipated, and a failed
    /// read is no answer at all.
    /// </summary>
    RegistryMultiStringRead LocalMachineMultiStringValue(string keyPath, string valueName);

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

/// <summary>Whether an HKLM subkey is there. See <see cref="IRegistryReader.LocalMachineKeyPresence"/>.</summary>
public enum RegistryKeyPresence
{
    /// <summary>
    /// The read itself failed, so nothing at all was established.
    ///
    /// FIRST MEMBER SO THAT IT IS WHAT THE TYPE'S OWN ZERO CARRIES. A value nobody
    /// set has found nothing out, and this is the reading that says so; ordering it
    /// anywhere else would make a default the most confident answer of the three.
    /// </summary>
    Unreadable,

    /// <summary>The key is there.</summary>
    Present,

    /// <summary>The key is not there. Not a failure: it is how a machine without it reads.</summary>
    Absent,
}

/// <summary>How a REG_MULTI_SZ read turned out. See <see cref="RegistryMultiStringRead"/>.</summary>
public enum RegistryMultiStringState
{
    /// <summary>
    /// The read itself failed, so nothing at all was established. First member for
    /// the reason <see cref="RegistryKeyPresence.Unreadable"/> is: it is the state a
    /// <see cref="RegistryMultiStringRead"/> carries when nobody has set one, and
    /// the array is null there as well, so it is also the only state the zero can
    /// honestly describe.
    /// </summary>
    Unreadable,

    /// <summary>The value was there and was a string array, which is in <see cref="RegistryMultiStringRead.Entries"/>.</summary>
    Read,

    /// <summary>
    /// The value is not there. An answer rather than a failure, and it covers an
    /// absent key as well as an absent value: the question was what this value
    /// holds, and "nothing" is true of both.
    /// </summary>
    Absent,

    /// <summary>The value is there and is not a string array, so there is nothing to read but the fact.</summary>
    WrongType,
}

/// <summary>
/// One REG_MULTI_SZ read. <see cref="Entries"/> is meaningful only when
/// <see cref="State"/> is <see cref="RegistryMultiStringState.Read"/> and is null
/// in every other state, so a caller that reads the array without the state cannot
/// tell a value that holds nothing from a value nobody could read.
/// </summary>
public readonly record struct RegistryMultiStringRead(
    RegistryMultiStringState State, string[]? Entries = null);

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
