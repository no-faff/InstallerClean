namespace InstallerClean.Services;

/// <summary>Registry-read abstraction. All reads target HKLM in the 64-bit (Registry64) view; the keys checked are unwowed.</summary>
public interface IRegistryReader
{
    /// <summary>True if the relative HKLM path resolves to an existing subkey; false on absent or read failure.</summary>
    bool LocalMachineKeyExists(string relativePath);

    /// <summary>REG_MULTI_SZ value as a string array, or null if absent, wrong type, or read fails.</summary>
    string[]? LocalMachineMultiStringValue(string keyPath, string valueName);
}
