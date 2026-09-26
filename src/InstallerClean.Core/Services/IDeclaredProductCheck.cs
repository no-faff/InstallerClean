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
/// are any. Where there are, the check reads the cached copy each registration records
/// and the patch package the patch's source list points at in each registration's
/// account and context, and reports whether every one of them is a different file from
/// this one.
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
/// recorded for each installation of it, and its original package, when it needs
/// that rather than the cached copy, by looking for the package name in the folders
/// on the product's source list. The folder can hold further copies that declare the
/// same product code while nothing in either place names them, and those are not a
/// package any installation of the product opens. So the file is kept while some
/// installation's package cannot be seen: a value that is empty, that will not read,
/// that names nothing identifiable, that names a file declaring another product, or
/// that names this file under another spelling. It is kept too while some source
/// cannot be ruled out: one in the Installer folder itself, one naming this file, and
/// one that cannot be read. The folder an installation records as its
/// <c>InstallSource</c>, the one its package was installed from, counts as a source
/// whether or not the list still holds it. So it is while a source list holds
/// something the check does not compare, a URL, an entry naming an environment
/// variable, a media package path or a package name naming a folder, a drive or a
/// variable; while the source used last is not a network entry on the list; and while
/// the registry key holding the list does not hold what the API returned for it,
/// package name included, or holds that name as anything but a REG_SZ. So it is while
/// an installation's <c>InstallSource</c> will not read, is held in the registry
/// otherwise than the API answers it or as anything but a REG_SZ, names an environment
/// variable, or starts neither with a drive letter, a ':' and a '\' nor with two '\'.
/// An installation in a per-user-unmanaged context keeps it as well, its source list
/// not being read.
///
/// A REGISTERED PATCH DOES NOT ON ITS OWN MAKE THIS FILE A COPY OF IT WINDOWS OPENS
/// EITHER.
/// Windows Installer opens a registered patch's cached copy through the
/// <c>LocalPackage</c> value each registration of it records, and the patch has a
/// source list and a package name of its own, as a product has. The folder can hold
/// further copies that declare the same patch code while nothing in either place names
/// them. So the file is kept while some registration's copy cannot be seen: a value
/// that is empty, that will not read, that names nothing identifiable, that names a
/// file that does not read as the same patch, or that names this file under another
/// spelling. It is kept too while some source on the patch's list cannot be ruled
/// out: one in the Installer folder itself, one naming this file, and one that cannot
/// be read, and for everything else a product's source list keeps it for. A
/// registration in a per-user-unmanaged context keeps it as well, the patch's source
/// list there not being read.
///
/// A PATCH'S REGISTRATIONS ARE FOUND TWO WAYS, AND THE TWO ARE UNIONED. The
/// machine-wide patch enumeration lists the registrations it names, each with its
/// product, account and context. The keyed patch read puts the patch to every
/// installation of every product the patch's own Template names, which reaches a
/// registration of those products that the enumeration does not list. Either can
/// only add a registration, and so only add a reason to keep the file.
///
/// EVERY ANSWER ABOUT A PRODUCT IS HELD AGAINST THE CALLER'S OWN ENUMERATION. The
/// check asks Windows for the installations of one product code at a time, and the
/// caller's enumeration has already listed the installations of every product. An
/// answer about a code that leaves out an installation that enumeration listed
/// contradicts it, whether the answer is that the product is not installed or a list
/// short of that installation, and the check does not use it: an installation package
/// declaring the product is kept, and so is a patch naming it.
///
/// IT ONLY EVER WITHHOLDS. No answer it can give puts a file on the list, clears
/// one another gate kept, or weakens anything upstream: a candidate it lets
/// through is decided by the rest of the scan exactly as if this check had not
/// run. For an installation package, a file it cannot read, a question it cannot
/// put, an answer that contradicts the caller's enumeration, a source that answers
/// off the allowlist and a recorded package it cannot identify all keep the file. For
/// a patch, a file it cannot read, a registration it cannot list or ask about, an
/// answer about a product it names that contradicts the caller's enumeration, a source
/// that answers off the allowlist and a recorded copy it cannot identify all keep the
/// file.
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
    /// Screens one scan's provisional candidates, in order, returning a verdict
    /// for each: entry <c>i</c> answers candidate <c>i</c> of the list passed in.
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
    /// <see cref="IPackageIdentityReader.Read"/> for why that is a precondition
    /// and not a courtesy.
    /// </param>
    /// <param name="installations">
    /// Every installation the caller's own enumeration established,
    /// <see cref="InstallerQueryResult.Installations"/>. The check asks Windows for the
    /// installations of each product it puts a question about, and an answer leaving
    /// out one of these is an answer the check cannot use: the product half then gives
    /// <see cref="DeclaredProductOutcome.Unestablished"/>, and a patch naming that
    /// product gives <see cref="DeclaredProductOutcome.DeclaredPatchUnestablished"/>.
    /// An empty list compares nothing, which is right only for an enumeration that
    /// listed nothing.
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
    /// ruled out, so every candidate whose declared product is installed, and every
    /// candidate whose declared patch is registered, is kept.
    /// </param>
    IReadOnlyList<DeclaredProductOutcome> Screen(
        IReadOnlyList<OrphanedFile> candidates,
        IReadOnlyList<ListedInstallation> installations,
        CancellationToken cancellationToken = default,
        Action<Exception, string>? recordRefusal = null,
        Func<string, bool?>? namesAFileInInstallerFolder = null);
}

/// <summary>
/// What one candidate's own declaration settled. Four of the eight keep the file,
/// and <see cref="Withholds"/> is the only place that says which.
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
    /// of the product the caller's own enumeration listed. They are different things
    /// to have found out, which is exactly why they are not reported anywhere as one
    /// thing; what they share, and the whole of what this value claims, is that
    /// nothing was established. Nothing outside this pass reads which of the two it
    /// was.
    /// </summary>
    Unestablished,

    /// <summary>
    /// The file declared a product code, Windows positively answered that no such
    /// product is installed, in any account and any context, and the caller's own
    /// enumeration listed no installation of it. The candidate goes on being decided by
    /// everything else.
    ///
    /// A POSITIVE ANSWER AND NOT AN ABSENCE OF ONE, which is the distinction the
    /// whole check turns on. Only a return documented to mean the product is not
    /// there reaches this; anything else is <see cref="Unestablished"/>, and so is
    /// that return for a product the caller's enumeration listed.
    /// </summary>
    DeclaredProductNotInstalled,

    /// <summary>
    /// Windows still holds a record of the product this file declares it belongs
    /// to, and for at least one installation of that product the check cannot show
    /// that every package it opens is a different file. Kept back.
    ///
    /// That covers a recorded <c>LocalPackage</c> value that is empty or will not
    /// read, one naming a folder or a file that is absent or cannot be identified,
    /// one naming a file that declares another product, and one naming this very
    /// file under another spelling. It covers a source list or package name that will
    /// not read, a source in the Installer folder itself, a source whose package is
    /// this file, and a source that cannot be resolved or whose package will not
    /// identify, and an installation in a per-user-unmanaged context, whose source list
    /// is not read. It covers a source list holding a URL, an entry naming an
    /// environment variable, a media package path or a package name naming a folder, a
    /// drive or a variable, one whose source used last is not a network entry on it,
    /// and one whose registry key does not hold what the API returned for it, package
    /// name included, or holds that name as anything but a REG_SZ. It covers an
    /// <c>InstallSource</c> that is the Installer folder itself, whose package is this
    /// file or will not identify, that will not read, that the registry holds otherwise
    /// than the API answers it or as anything but a REG_SZ, that names an environment
    /// variable, or that starts neither with a drive letter, a ':' and a '\' nor with two
    /// '\'. In each of them the check cannot see which package that installation opens,
    /// so this file could be it. A check constructed without its two file readers or its
    /// registry reader, or screening without the Installer folder to compare against,
    /// answers this for every installed product, having no way to look.
    ///
    /// WHAT IT DOES NOT ESTABLISH, so no copy may be built on it: that a program
    /// would break without this particular copy.
    /// </summary>
    DeclaredProductInstalled,

    /// <summary>
    /// Windows still holds a record of the product this file declares, every
    /// installation of that product records a cached package that is present, is a
    /// different file, and itself declares the same product, no installation is in a
    /// per-user-unmanaged context, and no installation's source list or
    /// <c>InstallSource</c> points at the Installer folder or at this file. Every
    /// installation's source list is held in the registry as the API returns it, with
    /// its package name as a REG_SZ, and so is its <c>InstallSource</c> where there is
    /// one, which starts with a drive letter, a ':' and a '\', or with two '\'. Every
    /// source list holds network entries only, none of them naming an environment
    /// variable, names no media package path, has a package name naming a file alone,
    /// and was last used from one of its own network entries or not at all. The
    /// candidate goes on being decided by everything else.
    ///
    /// Windows Installer opens a product's cached package through the
    /// <c>LocalPackage</c> value recorded for each installation, and its original
    /// package through the package name in the folders on the source list, so a copy
    /// that none of those names is not a package any installation of the product
    /// opens.
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
    /// enumeration listed, or an installation of one would not answer the keyed patch
    /// read. That last includes an installation answering that its product is not
    /// installed, which contradicts the keyed product enumeration that listed it
    /// moments earlier. What they share, and the whole of what this value claims, is
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
    /// ran to its end and listed none, and every installation of every product the
    /// patch names, every one the caller's own enumeration listed among them, answered
    /// that it holds no record of the patch, or no such product is installed and that
    /// enumeration listed no installation of it. The candidate goes on being decided by
    /// everything else.
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
    /// one registration the check cannot show that every copy of the patch it opens is
    /// a different file. Kept back.
    ///
    /// That covers a recorded <c>LocalPackage</c> value that is empty or will not
    /// read, one naming a folder or a file that is absent or cannot be identified, one
    /// naming a file that does not read as the same patch, and one naming this very
    /// file under another spelling. It covers a source list or package name of the
    /// patch that will not read, a source in the Installer folder itself, a source
    /// whose package is this file, and a source that cannot be resolved or whose
    /// package will not identify, and a registration in a per-user-unmanaged context,
    /// where the patch's source list is not read. It covers the patch's source list
    /// keeping the file for any of the reasons a product's does at
    /// <see cref="DeclaredProductInstalled"/>. A check constructed without its two file
    /// readers or its registry reader, or screening without the Installer folder to
    /// compare against, answers this for every registered patch, having no way to look.
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
    /// Windows holds a registration of the patch this file declares, every
    /// registration records a cached copy that is present, is a different file, and
    /// itself declares the same patch, no registration is in a per-user-unmanaged
    /// context, and no source list of the patch, in the account and context of any
    /// registration, points at the Installer folder or at this file. Each of those
    /// source lists passes what a product's has to at
    /// <see cref="DeclaredProductCachedAsAnotherFile"/>.
    /// The candidate goes on being decided by everything else.
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
