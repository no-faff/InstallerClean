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
}
