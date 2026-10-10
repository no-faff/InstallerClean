using InstallerClean.Services;

namespace InstallerClean.Models;

/// <summary>
/// The files a scan kept back because the product each declares is installed
/// (<see cref="WithholdingSplit.DeclaredProductInstalledCount"/>), counted by why the
/// declared-product check kept each one (<see cref="DeclaredProductInstalledCause"/>): one member
/// for each cause, and one cause for each file. Counts, and nothing naming a file.
///
/// THE MEMBERS ADD UP TO THAT COUNT, every such file having one cause, where the check has its
/// file readers, its registry reader and the test for a file in the Installer folder, all of which
/// the scan gives it. A file the check gave no cause is counted in none of them.
/// </summary>
/// <param name="IsItsCachedPackage">The files kept for <see cref="DeclaredProductInstalledCause.IsItsCachedPackage"/>.</param>
/// <param name="IsAtItsSources">The files kept for <see cref="DeclaredProductInstalledCause.IsAtItsSources"/>.</param>
/// <param name="IsAnotherCachedPackage">The files kept for <see cref="DeclaredProductInstalledCause.IsAnotherCachedPackage"/>.</param>
/// <param name="IsAtAnotherInstallationsSources">The files kept for <see cref="DeclaredProductInstalledCause.IsAtAnotherInstallationsSources"/>.</param>
/// <param name="PathUnreadable">The files kept for <see cref="DeclaredProductInstalledCause.PathUnreadable"/>.</param>
/// <param name="NoneRecordedRegistryDisagrees">The files kept for <see cref="DeclaredProductInstalledCause.NoneRecordedRegistryDisagrees"/>.</param>
/// <param name="NotThere">The files kept for <see cref="DeclaredProductInstalledCause.NotThere"/>.</param>
/// <param name="WouldNotIdentify">The files kept for <see cref="DeclaredProductInstalledCause.WouldNotIdentify"/>.</param>
/// <param name="WouldNotRead">The files kept for <see cref="DeclaredProductInstalledCause.WouldNotRead"/>.</param>
/// <param name="NotThisProduct">The files kept for <see cref="DeclaredProductInstalledCause.NotThisProduct"/>.</param>
/// <param name="SourcesPerUserUnmanaged">The files kept for <see cref="DeclaredProductInstalledCause.SourcesPerUserUnmanaged"/>.</param>
/// <param name="SourcesGivenUp">The files kept for <see cref="DeclaredProductInstalledCause.SourcesGivenUp"/>.</param>
/// <param name="SourcesWouldNotRead">The files kept for <see cref="DeclaredProductInstalledCause.SourcesWouldNotRead"/>.</param>
/// <param name="SourcesRegistryDiffers">The files kept for <see cref="DeclaredProductInstalledCause.SourcesRegistryDiffers"/>.</param>
/// <param name="SourcesFormNotCompared">The files kept for <see cref="DeclaredProductInstalledCause.SourcesFormNotCompared"/>.</param>
/// <param name="SourcePackageNotRuledOut">The files kept for <see cref="DeclaredProductInstalledCause.SourcePackageNotRuledOut"/>.</param>
/// <param name="ByName">The files kept for <see cref="DeclaredProductInstalledCause.ByName"/>.</param>
public sealed record DeclaredProductInstalledCauses(
    int IsItsCachedPackage,
    int IsAtItsSources,
    int IsAnotherCachedPackage,
    int IsAtAnotherInstallationsSources,
    int PathUnreadable,
    int NoneRecordedRegistryDisagrees,
    int NotThere,
    int WouldNotIdentify,
    int WouldNotRead,
    int NotThisProduct,
    int SourcesPerUserUnmanaged,
    int SourcesGivenUp,
    int SourcesWouldNotRead,
    int SourcesRegistryDiffers,
    int SourcesFormNotCompared,
    int SourcePackageNotRuledOut,
    int ByName)
{
    /// <summary>A scan that kept no file for this reason.</summary>
    public static DeclaredProductInstalledCauses None { get; } =
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
