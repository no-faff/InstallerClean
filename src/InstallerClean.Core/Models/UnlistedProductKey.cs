namespace InstallerClean.Models;

/// <summary>
/// One product key under <c>UserData</c> that no installation an enumeration listed
/// accounts for, by account subtree and code. Everything here comes off the registry. No walk
/// lists it, so no pass in a scan asks it anything. A program installed per user by SYSTEM
/// reads this way, and so does a key an uninstall left behind.
///
/// THE REGISTRY KEEPS A FILE AND NEVER RELEASES ONE. The declared-product check reads these
/// to hold a file, and no value here shows that a file may go.
/// </summary>
/// <param name="Account">
/// The name of the account subtree the key sits under, as the registry spells it.
/// </param>
/// <param name="Code">The product code unpacked out of the key's name.</param>
/// <param name="PatchCodes">
/// The patch codes the key's own <c>Patches</c> key lists, empty where it has no such key,
/// and null where that listing was not established.
/// </param>
/// <param name="CachedPackages">
/// The path each value of its <c>InstallProperties</c> key records a cached package under,
/// empty where no value holds a path.
/// </param>
/// <param name="HasInstallProperties">
/// Whether the key has an <c>InstallProperties</c> key, which decides what the declared-product
/// check keeps for it (<see cref="Services.IDeclaredProductCheck"/>).
/// </param>
public sealed record UnlistedProductKey(
    string Account,
    string Code,
    IReadOnlyCollection<string>? PatchCodes,
    IReadOnlyList<string> CachedPackages,
    bool HasInstallProperties);
