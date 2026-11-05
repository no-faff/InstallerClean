namespace InstallerClean.Services;

/// <summary>
/// Why the screen kept one installation package as
/// <see cref="DeclaredProductOutcome.DeclaredProductInstalled"/>
/// (<see cref="DeclaredProductScreening.InstalledCauses"/>): the file is a package an
/// installation opens, or one installation's packages could not all be seen, and then the step
/// that could not see one, or the file is, or could be, the cached package an installation of its
/// own program records and is missing.
///
/// ONE CAUSE FOR EACH SUCH FILE, so the counts of the members other than <see cref="None"/> add
/// up to the files kept with that verdict. A file that is a package an installation opens has
/// that as its cause, and so does a file a package compared by name could be by its name,
/// except a file that is, or could be, a missing cached package of its own program and
/// opens as none of that program's packages, which has that as its cause. A file whose own
/// program's packages could not all be seen has the step that stopped the read of them, at the
/// first installation of the program that could not be seen, and every file declaring that
/// program has the same one.
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

    /// <summary>
    /// An installation's cached-package path names no file that is there, a folder included, and
    /// is not shown to name nothing: it is not a plain path on a drive Windows reports as local,
    /// the file identity read does not answer that it names nothing, or the installation's
    /// <c>InstallProperties</c> key does not hold the same path. Another account's per-user
    /// unmanaged installation whose cached-package path names no file that is there gives this as
    /// well.
    /// </summary>
    NotThereUnestablished,

    /// <summary>
    /// An installation's cached-package path is shown to name nothing, and the package code Windows
    /// Installer records for the installation was not read: through the API or from the product's
    /// key in the registry it does not read or is not a GUID, the two differ, or the installation is
    /// per user and not the account the check runs as.
    /// </summary>
    MissingPackageCodeUnread,

    /// <summary>An installation's cached package's volume and file ID would not read.</summary>
    WouldNotIdentify,

    /// <summary>An installation's cached package would not give up its product code.</summary>
    WouldNotRead,

    /// <summary>
    /// An installation's cached package declares no product code, one that is not a well-formed
    /// GUID, another product's code, or reads as a patch.
    /// </summary>
    NotThisProduct,

    /// <summary>
    /// An installation is in another account's per-user unmanaged context, whose source list is
    /// not read, and, where it records no cached package, its <c>InstallProperties</c> key records
    /// none as well. The source list of one in the account the check runs as is read like any
    /// other.
    /// One whose key does not show that gives <see cref="NoneRecordedRegistryDisagrees"/>.
    /// </summary>
    SourcesPerUserUnmanaged,

    /// <summary>
    /// A package at one of an installation's sources, read for every file declaring its program,
    /// is under a drive or share given up for the pass, or, for an installation recording no
    /// cached package and having no source list, the folder it was installed from is. A package
    /// read only for the file whose name it could be gives <see cref="ByName"/> instead.
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
    /// An installation's source list holds a URL other than an http or https address, or the
    /// source it used last is a URL source other than one. The first of the five forms the check
    /// does not compare, which, with <see cref="SourcesFormMedia"/>,
    /// <see cref="SourcesFormVariable"/>, <see cref="SourcesFormPackageName"/> and
    /// <see cref="SourcesFormOther"/>, take a file by the first such form the check met on its
    /// program's sources.
    /// </summary>
    SourcesFormUrl,

    /// <summary>
    /// An installation's source list names a media package path while Windows is set to follow a
    /// symbolic link reached through a network path to this PC or to another network path.
    /// </summary>
    SourcesFormMedia,

    /// <summary>
    /// An entry on an installation's source list, the source it used last or its
    /// <c>InstallSource</c> names a variable, holding a '%'.
    /// </summary>
    SourcesFormVariable,

    /// <summary>
    /// An installation's package name is empty, or names a folder, a drive, a stream or a variable,
    /// or holds a null.
    /// </summary>
    SourcesFormPackageName,

    /// <summary>
    /// An entry on an installation's source list, the source it used last or its
    /// <c>InstallSource</c> holds a null or is in a form not compared, or the source it used last is
    /// a network source with no folder or a source of any other kind.
    /// </summary>
    SourcesFormOther,

    /// <summary>
    /// A package at one of an installation's sources will not identify and is not shown to be on
    /// another volume than the Installer folder, or could be a file directly in the Installer
    /// folder or is not established not to be one; or an installation with no source list was
    /// installed from a folder that could be the Installer folder itself or is not established not
    /// to be it.
    /// </summary>
    SourcePackageNotRuledOut,

    /// <summary>
    /// An installation of the file's own program records a cached package that names nothing, and
    /// the file's own package code would not read, so the file could be that package.
    /// </summary>
    MissingSparePackageCodeUnread,

    /// <summary>
    /// The file carries the package code an installation of its own program records whose cached
    /// package names nothing: it is that installation's missing cached package.
    /// </summary>
    MissingSpareIsIt,

    /// <summary>
    /// A package compared by name that one of its own program's installations has as a source, and
    /// that the file could be by its name, could not be ruled out: one in a folder on the network,
    /// one under a drive letter at which Windows reports no drive to the check or on an optical or
    /// removable drive that answers that it is not ready or has no media in it, whose folder cannot
    /// be read, or the package the media search looks for on each drive it tries; a package under a
    /// drive or share given up for the pass among them.
    /// </summary>
    ByName,
}
