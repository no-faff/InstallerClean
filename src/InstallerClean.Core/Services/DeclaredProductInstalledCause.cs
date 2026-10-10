namespace InstallerClean.Services;

/// <summary>
/// Why the screen kept one installation package as
/// <see cref="DeclaredProductOutcome.DeclaredProductInstalled"/>
/// (<see cref="DeclaredProductScreening.InstalledCauses"/>): the file is a package an
/// installation opens, or one installation's packages could not all be seen, and then the step
/// that could not see one.
///
/// ONE CAUSE FOR EACH SUCH FILE, so the counts of the members other than <see cref="None"/> add
/// up to the files kept with that verdict. A file that is a package an installation opens has
/// that as its cause, and so does a file a package in a folder on the network could be by its
/// name. A file whose own program's packages could not all be seen has the step that stopped the
/// read of them, at the first installation of the program that could not be seen, and every file
/// declaring that program has the same one.
///
/// Every step that keeps a file with that verdict names exactly one member. A step added without
/// one leaves its files counted nowhere, and the counts then fall short of the files kept.
/// </summary>
public enum DeclaredProductInstalledCause
{
    /// <summary>
    /// No cause: the verdict is not <see cref="DeclaredProductOutcome.DeclaredProductInstalled"/>,
    /// or the check had no way to look, being built without its file readers or its registry
    /// reader, or given no test for a file in the Installer folder.
    /// </summary>
    None,

    /// <summary>
    /// The file opens as the cached package an installation of its own program records, under
    /// another path.
    /// </summary>
    IsItsCachedPackage,

    /// <summary>The file opens as the package at a folder one of its own program's installations has as a source.</summary>
    IsAtItsSources,

    /// <summary>
    /// The file opens as the cached package an installation not ruled out as a second copy of a
    /// program records.
    /// </summary>
    IsAnotherCachedPackage,

    /// <summary>
    /// The file opens as the package at a folder an installation not ruled out as a second copy
    /// has as a source, or one an installation recording no cached package, released by its
    /// sources, has.
    /// </summary>
    IsAtAnotherInstallationsSources,

    /// <summary>An installation's cached-package path would not read.</summary>
    PathUnreadable,

    /// <summary>
    /// An installation records no cached package, and its <c>InstallProperties</c> key holds one,
    /// will not read, or has a path that will not make.
    /// </summary>
    NoneRecordedRegistryDisagrees,

    /// <summary>An installation's cached-package path names no file that is there, a folder included.</summary>
    NotThere,

    /// <summary>An installation's cached package's volume and file ID would not read.</summary>
    WouldNotIdentify,

    /// <summary>An installation's cached package would not give up its product code.</summary>
    WouldNotRead,

    /// <summary>
    /// An installation's cached package declares no product code, one that is not a well-formed
    /// GUID, another product's code, or reads as a patch.
    /// </summary>
    NotThisProduct,

    /// <summary>An installation is per user and unmanaged, a context whose source list is not read.</summary>
    SourcesPerUserUnmanaged,

    /// <summary>
    /// A package at one of an installation's sources, read for every file declaring its program,
    /// is under a drive or share given up for the pass. A package read only for the file whose
    /// name it could be gives <see cref="ByName"/> instead.
    /// </summary>
    SourcesGivenUp,

    /// <summary>
    /// An installation's package name, its source list, a property of the list, a registry key
    /// holding them or its <c>InstallSource</c> would not read, or a key's path would not make.
    /// </summary>
    SourcesWouldNotRead,

    /// <summary>
    /// The registry holds an installation's source list, its package name, its media package
    /// path, the source it used last or its <c>InstallSource</c> otherwise than Windows Installer
    /// answers for it, or holds none of the key Windows Installer answers from.
    /// </summary>
    SourcesRegistryDiffers,

    /// <summary>
    /// An installation's sources hold something the check does not compare: a URL, a media
    /// package path, an entry, a package name, a source used last or an <c>InstallSource</c>
    /// naming a variable, holding a null or in a form not compared, or a source used last that is
    /// not a network source.
    /// </summary>
    SourcesFormNotCompared,

    /// <summary>
    /// A package at one of an installation's sources will not identify, or could be a file
    /// directly in the Installer folder or is not established not to be one.
    /// </summary>
    SourcePackageNotRuledOut,

    /// <summary>
    /// A package in a folder on the network that one of its own program's installations has as a
    /// source, and that the file could be by its name, could not be ruled out, a package under a
    /// drive or share given up for the pass among them.
    /// </summary>
    ByName,
}
