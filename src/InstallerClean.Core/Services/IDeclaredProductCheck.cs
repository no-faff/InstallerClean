using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Asks a cached file what it declares itself to be, and puts that to Windows.
///
/// AN INSTALLATION PACKAGE declares the product it belongs to. The check puts that
/// product code to Windows and reports whether Windows still holds a record of it.
/// Where it does, the check reads the cached package each installation of that product
/// records, the original package each one's source list points at and the package in
/// the folder each one records as its <c>InstallSource</c>, and reports whether every
/// one of them is a different file from this one.
///
/// A PATCH declares its own patch code and the products it may be applied to. The
/// check finds the registrations Windows holds of that patch and reports whether there
/// are any. Where there are, the check reads the cached copy each registration records,
/// and reports whether every one of them is a different file from this one.
///
/// WHY IT EXISTS, AND IT IS ABOUT WHERE THE OTHER SOURCES START. Everything else
/// that decides whether a walked file is claimed begins at a REGISTRATION and works
/// towards a file. The path comparison asks whether any recorded <c>LocalPackage</c>
/// value is spelled the same as a walked file; the file-identity match asks whether
/// any recorded value NAMES the same file; and the registry fallback contributes
/// recorded values the API enumeration lost. Those are three ways of finding a claim,
/// and all three read the same recorded value. Where a product's or a patch's records
/// hold no value to read, none of them has anything to find, and the enumeration does
/// not notice: a <c>LocalPackage</c> that is present and zero-length merges no claim
/// AND records no gap, so the scan reports itself complete while short of a claim. The
/// cached file is then walked and matched against nothing, and the one view of it that
/// does not go through those records is the FILE, which is this.
///
/// AN INSTALLED PRODUCT DOES NOT ON ITS OWN MAKE THIS FILE THE ONE IT USES. Windows
/// Installer opens a product's cached package through the <c>LocalPackage</c> value
/// recorded for each installation of it, and its original package, when it needs that
/// rather than the cached copy, by looking for the package name in the folders on the
/// product's source list. The folder can hold further copies that declare the same
/// product code while nothing in either place names them, and those are not a package
/// any installation of the product opens. So the file is kept while some installation's
/// package cannot be seen: a value that will not read, that names nothing identifiable,
/// that names a file declaring another product, or that names this file under another
/// spelling. An installation whose value is empty records no cached package and opens
/// what its sources name, so it keeps the file only where a source cannot be ruled out,
/// and not at all where it is shown to have no source list either. It is kept too while
/// some source cannot be ruled out: one in the Installer folder itself, one naming this
/// file, one that cannot be read, one whose package does not answer within the check's
/// time limit, and one on a
/// drive or share the pass has stopped reading: because a read there did not answer
/// within that limit or failed only after a long wait, because the reads that failed
/// there took more time between them than the check allows, because all the reads there
/// took more time between them than the check allows one drive or share, whatever each
/// answered, or because the caller stopped waiting for it
/// (<see cref="SourceFolderWait.StopWaiting"/>). A source in a folder on the network, a
/// share or a network drive, is one of those only for a file whose name or short name
/// its package name could be, since Windows Installer looks in a source folder for that
/// name alone. While Windows is set to follow a symbolic link reached through a network
/// path to this PC or to another network path, it is one for every file. The folder an
/// installation records as its <c>InstallSource</c>, the one its package was installed
/// from, counts as a source whether or not the list still holds it. So it is while a
/// source list holds something the check does not compare, a URL, an entry naming an
/// environment variable, a media package path or a package name naming a folder, a
/// drive, a stream or a variable, or holding a null; while the source used last is not
/// a network source; and while the registry key holding the list does not
/// hold what the API returned for it, package name included, or holds that name as
/// anything but a REG_SZ. So it is while an installation's <c>InstallSource</c> will
/// not read, is held in the registry otherwise than the API answers it or as anything
/// but a REG_SZ, names an environment variable, or starts neither with a drive letter,
/// a ':' and a '\' nor with two '\'. An installation in a per-user-unmanaged context
/// keeps it as well, its source list not being read.
///
/// A REGISTERED PATCH DOES NOT ON ITS OWN MAKE THIS FILE A COPY OF IT WINDOWS OPENS
/// EITHER.
/// Windows Installer opens a registered patch's cached copy through the
/// <c>LocalPackage</c> value each registration of it records. Microsoft documents that
/// it needs the patch's source for an installation or a reinstallation when that
/// cached copy is missing. The folder can hold further copies that declare the same
/// patch code while no registration names them. So the file is kept while some
/// registration's copy cannot be seen: a value that is empty, that will not read, that
/// names nothing identifiable, that names a file that does not read as the same patch,
/// or that names this file under another spelling. A cached copy that is not there is
/// among them, so the patch's source list is not read.
///
/// A PATCH'S REGISTRATIONS ARE FOUND THREE WAYS, AND THE THREE ARE UNIONED. The
/// machine-wide patch enumeration lists the registrations it names, each with its
/// product, account and context. The keyed patch read puts the patch to every
/// installation of every product the patch's own Template names, which reaches a
/// registration of those products that the enumeration does not list. And the same
/// read is put to every installation the caller's enumeration listed, which reaches a
/// registration against a product the Template does not name, a second copy of a
/// program among them. Each can only add a registration, and so only add a reason to
/// keep the file. Where the registrations the first two found already keep every
/// copy, the third is not asked.
///
/// EVERY ANSWER ABOUT A PRODUCT IS HELD AGAINST THE CALLER'S OWN ENUMERATION. The
/// check asks Windows for the installations of one product code at a time, and the
/// caller's enumeration has already listed the installations of every product. An
/// answer about a code that leaves out an installation that enumeration listed
/// contradicts it, whether the answer is that the product is not installed or a list
/// short of that installation, and the check does not use it: an installation package
/// declaring the product is kept, and so is a patch naming it.
///
/// AN INSTALLATION ANSWERS FOR THE CODE ITS CACHED PACKAGE DECLARES, WHATEVER CODE IT IS
/// REGISTERED UNDER. A second copy of a program, installed under an instance transform,
/// is registered under the code the transform produced, while the original package it was
/// installed from declares the base code, and so can the package cached for it. So the
/// check reads the cached package of every installation the caller's enumeration listed,
/// and an installation package is put to every installation whose cached package
/// declares the code it declares, as well as to the installations of that code, each read
/// by the code it is registered under. Where an installation's cached package does not
/// say which product it declares, every installation package is kept unless that
/// installation's own record, read by this process, shows it to be an ordinary
/// installation, or it records no cached package and has no package to open at all. A
/// per-user installation keeps that record under its owner's account, and it is read only
/// where the owner is the account this process runs as. One that records no cached package
/// and has a source list opens what its sources name, so where every package they name can
/// be seen, every installation package is compared with those packages instead, as with
/// the installations below.
///
/// AND AN INSTALLATION THE CALLER COULD NOT RULE OUT AS A SECOND COPY ANSWERS FOR EVERY
/// FILE. Its cached package and its original package need not declare any code the check
/// can link it by, so every installation package the answer about its own product would
/// let through is compared by file identity with the packages each such installation
/// opens, its cached package and the packages its sources name, and one it opens as is
/// kept. Where those packages cannot all be seen, every such installation package is
/// kept, a package in a folder on the network that cannot be seen keeping only a file
/// whose name it could be.
///
/// IT ONLY EVER WITHHOLDS. No answer it can give puts a file on the list, clears
/// one another gate kept, or weakens anything upstream: a candidate it lets
/// through is decided by the rest of the scan exactly as if this check had not
/// run. For an installation package, a file it cannot read, a question it cannot
/// put, an answer that contradicts the caller's enumeration, an installation whose
/// cached package does not say which product it declares, unless its own record shows an
/// ordinary installation or it is shown to record no cached package and either have no
/// source list or have sources whose packages can all be seen, an installation not ruled
/// out as a second copy whose packages cannot all be seen, a source that answers off the
/// allowlist and a recorded package it cannot identify all keep the file. For a patch, a
/// file it cannot read, a registration it cannot list or ask about, an answer about a
/// product it names that contradicts the caller's enumeration and a recorded copy it
/// cannot identify all keep the file.
///
/// THE SUPERSEDED HALF OF THE OFFER IS NEVER PUT TO IT, AND THAT IS LOAD-BEARING. A
/// registered superseded patch's cached file is the very file its registrations
/// record, so this check would keep it: Windows holds a record of such a patch by
/// construction, that being what makes it superseded rather than unknown. The scan hands this check the walk's unclaimed candidates and
/// nothing else, and a superseded row reaches the offer from its own registration
/// without ever being one of them.
/// </summary>
public interface IDeclaredProductCheck
{
    /// <summary>
    /// Screens one scan's provisional candidates, in order, returning a verdict for each
    /// and every drive or share the pass gave up (<see cref="DeclaredProductScreening"/>).
    ///
    /// The whole pass is one call so that everything per-scan lives inside it.
    /// Several cached packages of one product declare one product code, so a
    /// folder holding six versions of the same program asks Windows once and not
    /// six times; the machine-wide patch enumeration is walked at most once, however
    /// many patches the list holds; and every such cache dies with the pass rather
    /// than outliving the machine state it describes.
    /// </summary>
    /// <param name="candidates">
    /// The files the path comparison and the file-identity match between them
    /// left unclaimed. The caller has already put every one through
    /// <see cref="CandidateGuard.CheckSafeToRemove"/>; see
    /// <see cref="IPackageIdentityReader.Read(string, bool, out string, out PackageReadRefusal)"/>
    /// for why that is a precondition and not a courtesy.
    /// </param>
    /// <param name="installations">
    /// Every installation the caller's own enumeration established,
    /// <see cref="InstallerQueryResult.Installations"/>. The check asks Windows for the
    /// installations of each product it puts a question about, and an answer leaving
    /// out one of these is an answer the check cannot use: the product half then gives
    /// <see cref="DeclaredProductOutcome.Unestablished"/>, and a patch naming that
    /// product gives <see cref="DeclaredProductOutcome.DeclaredPatchUnestablished"/>.
    /// The check also reads the cached package of each, once per pass, to find the
    /// installations registered under a code other than the one their package declares;
    /// and it reads the packages opened by each one marked
    /// <see cref="ListedInstallation.SecondCopyNotRuledOut"/>, once per pass, to compare
    /// every installation package with. An empty list compares nothing, which is right
    /// only for an enumeration that listed nothing.
    /// </param>
    /// <param name="recordRefusal">
    /// Where a reader refusal goes, given the exception to log and the reader's own
    /// short note on which refusal it was. Handed in by the scan that owns the crash
    /// log for the run rather than made here, so a test calling this pass directly
    /// has no run and writes nothing. A delegate rather than the log itself because
    /// this interface is public and that type is not.
    /// </param>
    /// <param name="namesAFileInInstallerFolder">
    /// Whether a path, once the kernel has expanded it, names a file directly in the
    /// Installer folder: true, false, or null where that was not established. Handed in
    /// by the scan, which has resolved that folder once for the run, as a delegate for
    /// the reason <paramref name="recordRefusal"/> is one. Without it no source can be
    /// ruled out, so every candidate whose declared product is installed is kept.
    /// </param>
    /// <param name="candidateReached">
    /// Told how many candidates the pass has reached, counting the one it is about to
    /// screen, once for each candidate in order, so the last call carries the length of
    /// the list. Handed in by the scan, which turns it into the progress it reports.
    /// </param>
    /// <param name="waitingOn">
    /// Told each wait a read of a source folder makes, once that read has waited about a
    /// second without answering, as a <see cref="SourceFolderWait"/> naming the drive or
    /// share it is waiting on, and told null when the wait ends, whether the read answered
    /// or its drive or share was given up. <see cref="SourceFolderWait.StopWaiting"/> gives
    /// that drive or share up for the rest of this pass. The end of a wait that cancelling
    /// the pass ends is not told. Handed in by the scan and by the check before a Move or
    /// Delete, which show the wait while it lasts.
    /// </param>
    DeclaredProductScreening Screen(
        IReadOnlyList<OrphanedFile> candidates,
        IReadOnlyList<ListedInstallation> installations,
        CancellationToken cancellationToken = default,
        Action<Exception, string>? recordRefusal = null,
        Func<string, bool?>? namesAFileInInstallerFolder = null,
        Action<int>? candidateReached = null,
        Action<SourceFolderWait?>? waitingOn = null);
}

/// <summary>
/// What one pass of <see cref="IDeclaredProductCheck.Screen"/> settled.
/// </summary>
/// <param name="Outcomes">
/// A verdict for each candidate: entry <c>i</c> answers candidate <c>i</c> of the list the
/// pass was handed.
/// </param>
/// <param name="RootsGivenUp">
/// Every drive or share, or path of another form, the pass stopped reading, once each and
/// in the order it gave them up, with the route each was given up by and how many of
/// <paramref name="Outcomes"/> keep their file at it (<see cref="SourceRootGivenUp"/>).
/// Empty where the pass gave none up.
/// </param>
/// <param name="WaitCount">
/// How many waits the pass made: reads still waiting, with neither an answer nor a stop,
/// after <see cref="DeclaredProductCheck.SourceFolderWaitThreshold"/>, each told to the
/// caller through <see cref="IDeclaredProductCheck.Screen"/>'s <c>waitingOn</c>. A host
/// showing the waits puts its waiting line up once for each. The pass makes one read at a
/// time, so no two of them overlap.
/// </param>
/// <param name="CachedPackages">
/// What the pass found of the two conditions under which it holds back every installation
/// package it would otherwise let through (<see cref="CachedPackageCensus"/>).
/// </param>
public sealed record DeclaredProductScreening(
    IReadOnlyList<DeclaredProductOutcome> Outcomes,
    IReadOnlyList<SourceRootGivenUp> RootsGivenUp,
    int WaitCount,
    CachedPackageCensus? CachedPackages = null)
{
    /// <summary>Never null: a screening built without one reads as a pass that read no installation.</summary>
    public CachedPackageCensus CachedPackages { get; init; } = CachedPackages ?? CachedPackageCensus.None;
}

/// <summary>
/// What the screen settled about one candidate. Some verdicts keep the file and the
/// rest let it through, and <see cref="DeclaredProductOutcomes.Withholds"/> is the only
/// place that says which.
/// </summary>
public enum DeclaredProductOutcome
{
    /// <summary>
    /// An installation package yielded no product code to ask about, or the code was
    /// read and the question could not be put or its answer could not be used. Kept
    /// back.
    ///
    /// IT IS FIRST SO THAT THE DEFAULT VALUE WITHHOLDS. A verdict nobody set is a
    /// verdict nobody established, and this enum's zero has to mean that rather
    /// than mean an answer.
    ///
    /// TWO INABILITIES UNDER ONE NAME, DELIBERATELY, AND THE NAME STATES NEITHER
    /// CAUSE. One is about the FILE: it would not open, it holds no Property
    /// table, its ProductCode row is absent or is not a GUID. The other is about
    /// the RECORDS: the keyed enumeration answered with something outside the
    /// returns that mean an answer, or with an answer that leaves out an installation
    /// of the product the caller's own enumeration listed. They are different things to
    /// have found out, which is exactly why they are not reported
    /// anywhere as one thing; what they share, and the whole of what this value claims,
    /// is that nothing was established. Nothing outside this pass reads which of the two
    /// it was.
    /// </summary>
    Unestablished,

    /// <summary>
    /// The file declared a product code, Windows positively answered that no such
    /// product is installed, in any account and any context, the caller's own
    /// enumeration listed no installation of it, and no cached package the check read,
    /// of the installations it listed, declares it, every one whose cached package did
    /// not read being shown by its own record to be an ordinary installation, or shown to
    /// record no cached package and have no source list, so that it opens none, or to record
    /// none and open only packages its sources name that the check saw. Every installation
    /// the caller could not rule out as a second copy opens packages the check saw, and this
    /// file is shown to be a different file from all of those packages. The candidate goes
    /// on being decided by everything else.
    ///
    /// A POSITIVE ANSWER AND NOT AN ABSENCE OF ONE, which is the distinction the
    /// whole check turns on. Only a return documented to mean the product is not
    /// there reaches this; anything else is <see cref="Unestablished"/>, and so is
    /// that return for a product the caller's enumeration listed.
    /// </summary>
    DeclaredProductNotInstalled,

    /// <summary>
    /// Windows still holds a record of the product this file declares it belongs
    /// to, or of an installation whose cached package declares that product, and for at
    /// least one such installation the check cannot show that every package it opens is
    /// a different file; or an installation the caller could not rule out as a second copy
    /// of a program opens this file as its package. Kept back.
    ///
    /// That covers a recorded <c>LocalPackage</c> value that will not read, one naming a
    /// folder or a file that is absent or cannot be identified, one naming a file that
    /// declares another product, and one naming this very file under another spelling. An
    /// empty value is covered through the installation's sources alone, below, and not at all
    /// for an installation that has no source list either.
    /// It covers a source list or package name that will not read, a source whose package
    /// is this file, will not identify or does not answer within the check's time limit, a
    /// source on a drive or share the check has stopped reading for the pass (a read there
    /// did not answer within that limit or failed only after a long wait, the reads that
    /// failed there took more time between them than the check allows, or all the reads
    /// there took more time between them than the check allows one drive or share, whatever
    /// each answered), a source not on a local drive that is the Installer folder itself or
    /// cannot be resolved, and an installation in a per-user-unmanaged context, whose
    /// source list is not read. It covers a source list holding a URL, an entry naming an
    /// environment variable or starting neither with a drive letter, a ':' and a '\' nor
    /// with two '\', a media package path or a package name naming a folder, a drive, a
    /// stream or a variable, or holding a null, one whose source used last is not a network
    /// source, and one whose registry key does not hold what the API returned for it,
    /// package name included, or holds that name as anything but a REG_SZ. It covers an
    /// <c>InstallSource</c> whose package is this file, will not identify or does not
    /// answer within the time limit, one on a drive or share the check has stopped reading,
    /// one not on a local drive that is the Installer folder itself, and one that will not
    /// read, that the registry holds otherwise than the API answers it or as anything but a
    /// REG_SZ, that names an environment variable, or that starts neither with a drive
    /// letter, a ':' and a '\' nor with two '\'. A source or an <c>InstallSource</c> in a
    /// folder on the network covers this only where its package name could be this file's
    /// name or short name, or where Windows is set to follow a symbolic link reached
    /// through a network path to this PC or to another network path. In each of them this
    /// file is, or could be, the package that installation opens. A check constructed
    /// without its two file readers or its registry reader, or screening without the
    /// Installer folder to compare against, answers this for every installed product,
    /// having no way to look.
    ///
    /// Where every package was seen and this file's own identity does not read, the
    /// answer is <see cref="CandidateIdentityUnestablished"/> instead.
    ///
    /// WHAT IT DOES NOT ESTABLISH, so no copy may be built on it: that a program
    /// would break without this particular copy.
    /// </summary>
    DeclaredProductInstalled,

    /// <summary>
    /// Windows still holds a record of the product this file declares, or of an
    /// installation whose cached package declares it, every such installation records a
    /// cached package that is present, is a different file, and itself declares the
    /// same product, or records none, no installation is in a per-user-unmanaged context,
    /// and no package in a folder on an installation's source list, in the folder it used last as a
    /// source or in its <c>InstallSource</c> is this file or, on a root other than a local
    /// drive, would be a file directly in the Installer folder, a package in a folder on
    /// the network counting only where its package name could be this file's name. Every
    /// installation's source list is held in the registry as the API returns it, with its
    /// package name as a REG_SZ, and so is its <c>InstallSource</c> where there is one,
    /// which starts with a drive letter, a ':' and a '\', or with two '\'. Every source
    /// list holds no URL and no entry naming an environment variable, names no media
    /// package path, has a package name naming a file alone, and was last used from a
    /// network source or not at all. Every installation the caller could not rule out as a
    /// second copy opens packages the check saw, and this file is a different file from
    /// all of them too. The candidate goes on being decided by everything else.
    ///
    /// Windows Installer opens a product's cached package through the
    /// <c>LocalPackage</c> value recorded for each installation, and its original
    /// package through the package name in the folders on the source list, so a copy
    /// that none of those names is not a package any installation of the product
    /// opens.
    ///
    /// AN INSTALLATION RECORDING NO CACHED PACKAGE OPENS WHAT ITS SOURCES NAME, so its sources
    /// are read as above and no cached package is asked of it.
    ///
    /// AN INSTALLATION WITH NEITHER IS LEFT OUT OF ALL OF THIS. A per-machine installation
    /// that records no cached package and has no source list, by the API's answer and in
    /// the registry alike, opens no package, so there is none for this file to be. Every
    /// other installation of the product is read as above.
    ///
    /// EVERY INSTALLATION, NOT ONE. One code can name a per-machine installation and
    /// per-user installations under several accounts, each recording its own
    /// package, and a single installation whose package cannot be seen gives
    /// <see cref="DeclaredProductInstalled"/> instead. Every installation the caller's
    /// own enumeration listed is among them, an answer without one of those giving
    /// <see cref="Unestablished"/>.
    ///
    /// DIFFERENT IS DECIDED BY FILE IDENTITY, NOT BY SPELLING. A recorded value can
    /// reach this file through a short name, a long-path prefix or a link, so the
    /// comparison is between the volume and file ID each path opens, and a recorded
    /// package that opens as this file keeps it.
    /// </summary>
    DeclaredProductCachedAsAnotherFile,

    /// <summary>
    /// The file is a patch, and either it yielded no patch code and target products to
    /// ask about, or the registrations of the patch it declares could not all be found.
    /// Kept back.
    ///
    /// SEVERAL INABILITIES UNDER ONE NAME, AS FOR <see cref="Unestablished"/>, AND THE
    /// NAME STATES NONE OF THEM. One is about the FILE: its summary stream would not
    /// open, its patch code is absent or is not a GUID, or its Template is absent, is not
    /// a list of GUIDs or names no product. The others are about the RECORDS: the
    /// machine-wide patch enumeration did not run to its end, a product the patch names
    /// would not list its installations or listed them without one the caller's own
    /// enumeration listed, or an installation of one, or one the caller's enumeration
    /// listed, would not answer the keyed patch read. That last includes an installation
    /// answering that its product is not installed, which contradicts the enumeration
    /// that listed it. What they share, and the whole of what this value claims, is
    /// that nothing was established.
    ///
    /// A PATCH HAS A VERDICT OF ITS OWN FOR THIS RATHER THAN SHARING
    /// <see cref="Unestablished"/>, so that everything counting the two can tell a
    /// patch copy from an installation package.
    /// </summary>
    DeclaredPatchUnestablished,

    /// <summary>
    /// The file is a patch, and Windows positively answered that it holds no
    /// registration of the patch the file declares: the machine-wide patch enumeration
    /// ran to its end and listed none, every installation of every product the patch
    /// names answered that it holds no record of the patch, or no such product is
    /// installed and the caller's own enumeration listed no installation of it, and every
    /// installation that enumeration listed answered the same. The candidate goes on
    /// being decided by everything else.
    ///
    /// A POSITIVE ANSWER AND NOT AN ABSENCE OF ONE, as for
    /// <see cref="DeclaredProductNotInstalled"/>. Only ERROR_UNKNOWN_PATCH from the keyed
    /// read, the return of an installation that holds no record of the patch, counts as
    /// an installation answering that way. Any other return of that read is either a
    /// registration or, where it gave no answer that can be used,
    /// <see cref="DeclaredPatchUnestablished"/>,
    /// and an enumeration that did not reach its end gives
    /// <see cref="DeclaredPatchUnestablished"/> too.
    /// </summary>
    DeclaredPatchNotRegistered,

    /// <summary>
    /// Windows holds a registration of the patch this file declares, and for at least
    /// one registration the check cannot show that the cached copy it records is a
    /// different file. Kept back.
    ///
    /// That covers a recorded <c>LocalPackage</c> value that is empty or will not
    /// read, one naming a folder or a file that is absent or cannot be identified, one
    /// naming a file that does not read as the same patch, and one naming this very
    /// file under another spelling. A check constructed without its two file readers
    /// answers this for every registered patch, having no way to look. Where every copy
    /// was seen and this file's own identity does not read, the answer is
    /// <see cref="CandidateIdentityUnestablished"/> instead.
    ///
    /// A REGISTRATION IS ANY RECORD WINDOWS HOLDS OF THE PATCH AGAINST AN INSTALLATION
    /// OF A PRODUCT, whatever state the patch is in there, so it is wider than
    /// <see cref="Interop.MsiPatchFilter.Registered"/>, which is one of those states.
    ///
    /// WHAT IT DOES NOT ESTABLISH, so no copy may be built on it: that a program
    /// would break without this particular copy.
    /// </summary>
    DeclaredPatchRegistered,

    /// <summary>
    /// Windows holds a registration of the patch this file declares, and every
    /// registration records a cached copy that is present, is a different file, and
    /// itself declares the same patch. The candidate goes on being decided by
    /// everything else.
    ///
    /// EVERY REGISTRATION, NOT ONE. A patch can be registered against several products
    /// and under several accounts, each registration recording its own value, and a
    /// single registration whose copy cannot be seen gives
    /// <see cref="DeclaredPatchRegistered"/> instead.
    ///
    /// DIFFERENT IS DECIDED BY FILE IDENTITY, NOT BY SPELLING, for the reason given at
    /// <see cref="DeclaredProductCachedAsAnotherFile"/>.
    /// </summary>
    DeclaredPatchCachedAsAnotherFile,

    /// <summary>
    /// Windows holds a record of the product this installation package declares, or a
    /// registration of the patch this patch copy declares, the check saw every package
    /// or copy each installation or registration opens that this file could be, and this
    /// file's own identity did not read, so it could not be compared with any of them.
    /// Kept back.
    ///
    /// IT IS ABOUT THE FILE AND NOT ABOUT THE RECORDS. Everything the records name that
    /// this file could be was read and identified; what the comparison lacks is this
    /// file's own volume and file ID. So both halves give this one verdict, and the scan
    /// counts it with the files the identity comparison could not identify.
    ///
    /// A FILE GONE BY THE TIME ITS IDENTITY IS READ IS GIVEN IT TOO. It declared its
    /// product or patch earlier in the same pass, and nothing at its path shows it to be
    /// a different file from any the records name.
    /// </summary>
    CandidateIdentityUnestablished,

    /// <summary>
    /// The answer about the product this installation package declares would let it
    /// through, and either an installation the caller could not rule out as a second copy
    /// of a program opens packages the check could not all see, or an installation's
    /// cached package does not say which product it declares, its own record does not
    /// show it to be an ordinary installation, and it is not shown to record no cached
    /// package and either have no source list or have sources whose packages the check saw.
    /// Kept back.
    ///
    /// IT IS ABOUT ANOTHER INSTALLATION AND NOT ABOUT THIS FILE. Such an installation
    /// could be a second copy of a program installed under an instance transform, whose
    /// packages need not declare any code this file could be linked to it by, so any
    /// installation package could be one it opens. So every candidate the answer would
    /// let through is given this verdict, on the same pass, whatever it declares. A
    /// package in a folder on the network that cannot be seen gives it only to a file
    /// whose name that package's name could be.
    ///
    /// Where a single installation's packages cannot all be seen it covers what
    /// <see cref="DeclaredProductInstalled"/> covers for one: a cached package that will
    /// not read, names nothing identifiable or yields no product code, and every source the
    /// check cannot rule out, a per-user-unmanaged context and a source in the Installer
    /// folder among them, an installation recording no cached package being read by its
    /// sources alone. A check constructed without its two file
    /// readers or its registry reader, or screening without the Installer folder to
    /// compare against, answers this beside every such installation, having no way to
    /// look.
    /// </summary>
    SecondCopyUnestablished,
}

/// <summary>Reading a <see cref="DeclaredProductOutcome"/>.</summary>
public static class DeclaredProductOutcomes
{
    /// <summary>
    /// Whether this outcome keeps the file back.
    ///
    /// STATED AS "ANYTHING BUT THE FOUR THAT LET A FILE THROUGH" RATHER THAN BY
    /// NAMING THE WITHHOLDING MEMBERS, and that is the safety property rather than a
    /// style. Named positively, a member added later would silently not withhold: a
    /// green build, a verdict the pass sets, and files going on being offered. Named
    /// this way an unconsidered member keeps the file, which is the direction a
    /// mistake here has to fail in.
    /// </summary>
    public static bool Withholds(this DeclaredProductOutcome outcome) =>
        outcome is not (DeclaredProductOutcome.DeclaredProductNotInstalled
            or DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile
            or DeclaredProductOutcome.DeclaredPatchNotRegistered
            or DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile);
}
