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
/// <param name="NotThereUnestablished">The files kept for <see cref="DeclaredProductInstalledCause.NotThereUnestablished"/>.</param>
/// <param name="MissingPackageCodeUnread">The files kept for <see cref="DeclaredProductInstalledCause.MissingPackageCodeUnread"/>.</param>
/// <param name="WouldNotIdentify">The files kept for <see cref="DeclaredProductInstalledCause.WouldNotIdentify"/>.</param>
/// <param name="WouldNotRead">The files kept for <see cref="DeclaredProductInstalledCause.WouldNotRead"/>.</param>
/// <param name="NotThisProduct">The files kept for <see cref="DeclaredProductInstalledCause.NotThisProduct"/>.</param>
/// <param name="SourcesPerUserUnmanaged">The files kept for <see cref="DeclaredProductInstalledCause.SourcesPerUserUnmanaged"/>.</param>
/// <param name="SourcesGivenUp">The files kept for <see cref="DeclaredProductInstalledCause.SourcesGivenUp"/>.</param>
/// <param name="SourcesWouldNotRead">The files kept for <see cref="DeclaredProductInstalledCause.SourcesWouldNotRead"/>.</param>
/// <param name="SourcesRegistryDiffers">The files kept for <see cref="DeclaredProductInstalledCause.SourcesRegistryDiffers"/>.</param>
/// <param name="SourcesFormUrl">The files kept for <see cref="DeclaredProductInstalledCause.SourcesFormUrl"/>.</param>
/// <param name="SourcesFormMedia">The files kept for <see cref="DeclaredProductInstalledCause.SourcesFormMedia"/>.</param>
/// <param name="SourcesFormVariable">The files kept for <see cref="DeclaredProductInstalledCause.SourcesFormVariable"/>.</param>
/// <param name="SourcesFormPackageName">The files kept for <see cref="DeclaredProductInstalledCause.SourcesFormPackageName"/>.</param>
/// <param name="SourcesFormOther">The files kept for <see cref="DeclaredProductInstalledCause.SourcesFormOther"/>.</param>
/// <param name="SourcePackageNotRuledOut">The files kept for <see cref="DeclaredProductInstalledCause.SourcePackageNotRuledOut"/>.</param>
/// <param name="MissingSparePackageCodeUnread">The files kept for <see cref="DeclaredProductInstalledCause.MissingSparePackageCodeUnread"/>.</param>
/// <param name="MissingSpareIsIt">The files kept for <see cref="DeclaredProductInstalledCause.MissingSpareIsIt"/>.</param>
/// <param name="ByName">The files kept for <see cref="DeclaredProductInstalledCause.ByName"/>.</param>
/// <param name="UndeclaredPackageCodeIsIt">The files kept for <see cref="DeclaredProductInstalledCause.UndeclaredPackageCodeIsIt"/>.</param>
/// <param name="UndeclaredPackageCodeUnread">The files kept for <see cref="DeclaredProductInstalledCause.UndeclaredPackageCodeUnread"/>.</param>
public sealed record DeclaredProductInstalledCauses(
    int IsItsCachedPackage,
    int IsAtItsSources,
    int IsAnotherCachedPackage,
    int IsAtAnotherInstallationsSources,
    int PathUnreadable,
    int NoneRecordedRegistryDisagrees,
    int NotThereUnestablished,
    int MissingPackageCodeUnread,
    int WouldNotIdentify,
    int WouldNotRead,
    int NotThisProduct,
    int SourcesPerUserUnmanaged,
    int SourcesGivenUp,
    int SourcesWouldNotRead,
    int SourcesRegistryDiffers,
    int SourcesFormUrl,
    int SourcesFormMedia,
    int SourcesFormVariable,
    int SourcesFormPackageName,
    int SourcesFormOther,
    int SourcePackageNotRuledOut,
    int MissingSparePackageCodeUnread,
    int MissingSpareIsIt,
    int ByName,
    int UndeclaredPackageCodeIsIt,
    int UndeclaredPackageCodeUnread)
{
    /// <summary>A scan that kept no file for this reason.</summary>
    public static DeclaredProductInstalledCauses None { get; } =
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// The files kept because their program's sources hold a form the check does not compare,
    /// whichever form it was: the sum of <see cref="SourcesFormUrl"/>,
    /// <see cref="SourcesFormMedia"/>, <see cref="SourcesFormVariable"/>,
    /// <see cref="SourcesFormPackageName"/> and <see cref="SourcesFormOther"/>.
    /// </summary>
    public int SourcesFormNotCompared =>
        SourcesFormUrl + SourcesFormMedia + SourcesFormVariable + SourcesFormPackageName + SourcesFormOther;
}
