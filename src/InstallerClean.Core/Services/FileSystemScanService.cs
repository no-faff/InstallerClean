using System.IO.Abstractions;
using System.Security;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IFileSystemScanService"/> implementation. Pairs
/// the API output from <see cref="IInstallerQueryService"/> with a
/// directory walk of <c>C:\Windows\Installer</c> via the injected
/// <see cref="IFileSystem"/>.
/// </summary>
public sealed class FileSystemScanService : IFileSystemScanService
{
    private readonly IInstallerQueryService _queryService;
    private readonly IFileSystem _fs;
    private readonly IShortNameCreationProbe? _shortNames;
    private readonly IFileIdentityReader? _fileIds;
    private readonly IDeclaredProductCheck? _declaredProducts;
    private readonly IFileTimesReader? _fileTimes;
    private readonly TimeProvider _clock;
    private readonly IEnumerable<string>? _overrideFiles;
    private readonly string? _installerFolderOverride;

    // How often the walk and the classification loop report where they have
    // reached. Both run once per file in a folder whose size is the machine's, so
    // reporting per file would make the number of updates a property of the
    // machine: the walk reports on each multiple of the stride, and the
    // classification divides its own length so that a folder of any size produces
    // about the same number of updates. A host throttles again on its own
    // account; this is what keeps the work off a folder holding millions of
    // files.
    private const int WalkReportStride = 1_000;
    private const int ClassifyReportCount = 200;

    /// <summary>Production constructor. DI supplies every dependency; the override fields stay null.</summary>
    /// <remarks>
    /// Microsoft.Extensions.DependencyInjection resolves the public ctor
    /// with the most resolvable parameters and ignores internal ctors.
    /// The test ctors below are <c>internal</c> so DI cannot select one
    /// at resolution time and pass defaults the production code never
    /// expects.
    ///
    /// The short-name probe is the one dependency that decides nothing, and it is
    /// here rather than sampled by a host so that both hosts report the same
    /// figure without either having to remember to ask.
    /// </remarks>
    public FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem,
        IShortNameCreationProbe shortNames, IFileIdentityReader fileIdentities,
        IDeclaredProductCheck declaredProducts, IFileTimesReader fileTimes, TimeProvider clock)
        : this(queryService, fileSystem, shortNames, null, null, fileIdentities, declaredProducts,
            fileTimes, clock) { }

    /// <summary>
    /// Test constructor. Injects a filesystem and nothing else, for the tests
    /// whose subject is the walk itself.
    /// </summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem)
        : this(queryService, fileSystem, shortNames: null, overrideFiles: null,
            installerFolderOverride: null, fileIdentities: null) { }

    /// <summary>Test constructor. Injects a fake file list.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles)
        : this(queryService, new FileSystem(), null, overrideFiles, null, null, null) { }

    /// <summary>Test constructor. Points enumeration at a real directory.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles, string? installerFolderOverride)
        : this(queryService, new FileSystem(), null, overrideFiles, installerFolderOverride, null, null) { }

    /// <summary>
    /// Test constructor. Injects an <see cref="IFileSystem"/> so the
    /// scan-against-registered-set logic can be verified without
    /// touching <c>C:\Windows\Installer</c> on the host machine.
    /// </summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem,
        IEnumerable<string>? overrideFiles, string? installerFolderOverride)
        : this(queryService, fileSystem, null, overrideFiles, installerFolderOverride, null, null) { }

    /// <summary>
    /// Test constructor carrying the short-name probe as well, for the tests
    /// whose subject is what the scan reports about the machine.
    /// </summary>
    /// <param name="shortNames">
    /// Null in every test that is not about this, which reports the setting as
    /// unreadable: a scan nobody sampled must not read as a machine whose policy
    /// is known, and the alternative of defaulting to a plausible setting would
    /// put a figure nobody measured into the one payload that exists to measure.
    /// </param>
    /// <param name="fileIdentities">
    /// Null in every test that is not about it, which leaves the path comparison
    /// exactly as it was before this existed: a string match and nothing more. A
    /// null reader cannot make a scan offer MORE than it would have, only the same,
    /// which is why the tests that pin the string classification go on pinning it.
    /// </param>
    /// <param name="declaredProducts">
    /// Null in every test that is not about it, on the same rule and for the same
    /// reason: the screen it performs can only ever keep a file back, so a scan
    /// built without one offers exactly what it offered before the screen existed
    /// and never more. The tests whose subject IS the screen inject one.
    ///
    /// DEFAULTED, WHICH IS THE ONE THING TO BE CAREFUL OF HERE. Every other seam on
    /// this constructor has to be spelled, so a test carries a null for each
    /// collaborator it is not about and the reader can see what was decided. This
    /// one may be left off, and the hazard that buys is narrow but real: a test
    /// asserting that the screen does NOT keep a file back passes just as well
    /// against a scan that has no screen at all. A test whose subject is this
    /// injects one for BOTH directions, and the pinning test for what the default
    /// itself means is in the suite beside them.
    /// </param>
    /// <param name="fileTimes">
    /// Null in every test that is not about the age check, which runs no age check at
    /// all, on the same terms as <paramref name="declaredProducts"/> and with the same
    /// hazard: a test asserting that the age check lets a file through passes just as
    /// well against a scan that has none. The tests whose subject is the age check
    /// inject a reader and show it holding a file back in the same fixture, and the
    /// pinning test for the default is beside them. Production always supplies one.
    /// </param>
    /// <param name="clock">
    /// The clock the age check judges against. Null means the system clock.
    /// </param>
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem,
        IShortNameCreationProbe? shortNames,
        IEnumerable<string>? overrideFiles, string? installerFolderOverride,
        IFileIdentityReader? fileIdentities,
        IDeclaredProductCheck? declaredProducts = null,
        IFileTimesReader? fileTimes = null,
        TimeProvider? clock = null)
    {
        _queryService = queryService;
        _fs = fileSystem;
        _shortNames = shortNames;
        _fileIds = fileIdentities;
        _declaredProducts = declaredProducts;
        _fileTimes = fileTimes;
        _clock = clock ?? TimeProvider.System;
        _overrideFiles = overrideFiles;
        _installerFolderOverride = installerFolderOverride;
    }

    /// <summary>
    /// Whether this scan runs the age check, for the test that holds the hosts' scan
    /// to running it. Every test constructor defaults the reader to null.
    /// </summary>
    internal bool ChecksAge => _fileTimes is not null;

    /// <summary>
    /// Whether this scan puts candidates to the declared-product screen, for the test
    /// that holds the hosts' scan to doing so. Every test constructor defaults the
    /// screen to null.
    /// </summary>
    internal bool ScreensDeclaredProducts => _declaredProducts is not null;

    public async Task<ScanResult> ScanAsync(
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // The age check's clock, read once and before the walk, so every candidate is
        // judged against the same instant and a file created or changed while the
        // scan runs is later than it.
        var scanClock = _clock.GetUtcNow();

        progress?.Report(new ScanProgressUpdate(Strings.Status_ScanningCache));

        // Walk the disk BEFORE querying the API, and materialise the walk here
        // rather than leaving it lazy. A package cached after the walk finishes
        // is then simply absent from the candidate set. A file the walk found
        // that is registered while the scan runs meets the age check, which keeps
        // a file created, written or changed within a day, and then the check
        // made just before a Move or Delete, which reads the records again and
        // re-runs this decision on it. Task.Run keeps
        // the walk off the calling thread: the GUI calls ScanAsync from the
        // dispatcher, and a synchronous directory walk here would freeze the very
        // window the scan keeps free.
        // ConfigureAwait(false): Core services do not bind to a caller's
        // SynchronizationContext.
        List<WalkedFile> diskFiles;
        if (_overrideFiles is not null)
        {
            // An injected list has no directory entry behind it, so the size is
            // asked for here. Test-only, which is why it is not worth a Task.Run.
            diskFiles = _overrideFiles.Select(p => new WalkedFile(p, StatSize(p))).ToList();
        }
        else
        {
            var folder = _installerFolderOverride ?? InstallerCacheHelpers.InstallerFolder;
            diskFiles = await Task.Run(() => MaterialiseInstallerFiles(folder, progress, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
        }

        progress?.Report(new ScanProgressUpdate(Strings.Status_QueryingApi));

        var query = await _queryService.GetRegisteredPackagesAsync(progress, cancellationToken)
            .ConfigureAwait(false);
        var registered = query.Packages;

        var registeredPaths = new HashSet<string>(
            registered.Select(p => p.LocalPackagePath),
            StringComparer.OrdinalIgnoreCase);

        var removable = new List<OrphanedFile>();

        // Candidates the scan declined to offer, so the left-alone line and the offer
        // between them still account for every file in the folder. Empty on every
        // machine whose registrations all spell a path, which is every machine anybody
        // has measured. See the decision below it for what fills it.
        var withheld = new List<OrphanedFile>();

        // Which decision put each of those there, kept apart because the report reads
        // them apart. It travels beside the list rather than being derived from it
        // afterwards: the list holds files and the files carry no verdict, so the only
        // place the question can be answered is where the decision is taken.
        var withheldBy = new WithholdingSplitTally();

        // Candidates no registration claims, in walk order. Four passes decide THIS
        // half of the offer: the path comparison, the file-identity match below it,
        // the declared-product screen and the age check. A survivor of all four is an
        // offered file.
        //
        // IT IS ONE OF TWO SOURCES OF OFFERED FILES AND THE OTHER IS NOT THE WALK AT
        // ALL. A superseded patch reaches the offer from the registered set, having
        // passed a condition about products rather than about paths. Keeping this list
        // separate is what lets the three sanity gates below be measured against what
        // the path comparison produced rather than against the whole offer, which is a
        // different quantity and would read a machine whose only offered files were
        // superseded patches as a machine whose comparison found nothing.
        var unclaimedByPath = new List<OrphanedFile>();

        // Budgeted, because a refusal is a per-file event on a loop whose length
        // is the size of C:\Windows\Installer. The guard's every input is a
        // machine-wide condition, so what refuses one candidate usually refuses
        // the lot: the cache root's own resolution degrading leaves every path
        // measured against an unexpanded root, and a filter driver refusing
        // CreateFile, or an attribute read failing across the folder, leaves
        // every verdict unproven. One full CrashLog.Write each would rotate
        // crash.log, which holds 512 KB with one archive, many times over in one
        // scan, and nothing that was in the file before the scan would survive.
        // The refusals are also the least informative entries possible, being
        // near-identical restatements of one condition.
        // Cause strings, rather than the bare exception, because both sites
        // synthesise an InvalidOperationException: all four kinds carry the same
        // type and HRESULT, so without them the budget's novel-cause escape
        // hatch would fire once and swallow the other three.
        var refusalLog = new PerItemFailureLog("Scan",
            "There is no other record of which files these were: a refused candidate is left off the "
            + "list offered for removal and nothing else about it is kept. Fewer files are offered, "
            + "never more.");

        // Resolved once for the scan; both guard sites below resolve their own
        // candidate per file against it (see InstallerCacheRoot).
        var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);

        // The folder the walk enumerated, in the spelling it enumerated it in, and
        // read by nothing but the three counts NamesFileDirectlyIn feeds: the two
        // correlation counts below, and missingInFolder, which is the other term in
        // the proportional clause that throws Error_ScanCorrelationFailed.
        // Deliberately NOT cacheRoot.Resolved: see NamesFileDirectlyIn.
        var walkedFolder = (_installerFolderOverride ?? InstallerCacheHelpers.InstallerFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        long stillUsedBytes = 0;
        // Registrations Windows reports superseded (2) and obsoleted (4), counted off
        // the machine rather than off the offer. See the increment site.
        int supersededRegistrations = 0;
        int obsoletedRegistrations = 0;
        int refusedCandidates = 0;
        int missingAffected = 0;
        int missingUnaffected = 0;
        // The correlation gate's inputs, counted here rather than derived from the
        // branches below because they answer a different question from the ones
        // those branches exist for. Every registered row is measured by ONE rule,
        // whether it is removable or not: a survivor count that tested the
        // non-removable half on File.Exists alone and the removable half only after
        // the containment guard is two rules in one sum, and the larger half then
        // proves nothing about the folder at all.
        //
        // ALL THREE ASK ABOUT ONE POPULATION, the registrations naming a file
        // directly in the folder this run walked, and they ask it of every row on
        // the same rule whatever state the row carries.
        //
        // The missing count is separate from the two the report speaks and does
        // not replace them. Those drive the missing-from-disk line on both hosts
        // and the result-log payload, and a registration naming a file somewhere
        // other than this folder that has gone is exactly as much of a problem, so
        // narrowing them would silence something that is right to say. What that
        // is not is evidence about whether the records and THIS folder describe the
        // same place, which is all the gate is asking.
        int registeredNamingFolder = 0;
        int registeredInFolderPresent = 0;
        int missingInFolder = 0;
        var sizedPackages = new List<RegisteredPackage>(registered.Count);

        // Declared out here for the same reason: the three gates below the loops
        // read it, and it is taken inside them. Zero is the honest starting value,
        // a scan that leaves before the walk finishes having produced no
        // candidates for anything to have judged.
        var candidatesFromWalk = 0;

        // Declared out here for the same reason again: it is decided inside the try
        // and read by the result built after it. FALSE is the honest starting value
        // in the same way zero is above, a scan that leaves before the branch is
        // reached having withheld nothing wholesale.
        var walkOfferWithheldWholesale = false;

        // And these for the same reason once more. A default tally is zero attempts,
        // which reads as a comparison that never ran rather than as one that ran
        // clean, and a scan leaving before the identity pass is exactly that.
        var registrationIdentityReads = default(FileIdentityReadTally);
        var candidateIdentityReads = default(FileIdentityReadTally);

        // The closing entry is owed on every exit, not just the clean one: a
        // cancel and the correlation gate both leave through here, and the gate
        // in particular fires on exactly the kind of broken machine that makes
        // the guard refuse wholesale, so its run is the one whose suppressed
        // count is worth having.
        try
        {
        // One update per this many files, so a folder of any size produces about
        // ClassifyReportCount of them. Floored at one, so a folder shorter than
        // that reports every file rather than none.
        var classifyStride = Math.Max(1, diskFiles.Count / ClassifyReportCount);
        var classified = 0;

        foreach (var walked in diskFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Counted before the three tests below rather than after them, so the
            // position measures how far through the folder the loop has reached
            // rather than how many files got past the tests. Every file in the
            // list reaches this line, and the last one is reported whatever the
            // stride, so the position always ends on the total.
            classified++;
            if (classified % classifyStride == 0 || classified == diskFiles.Count)
                progress?.Report(new ScanProgressUpdate(
                    DisplayHelpers.FormatCount(classified), IsMilestone: false,
                    Position: classified, Total: diskFiles.Count));

            var filePath = walked.FullPath;
            if (registeredPaths.Contains(filePath))
                continue;

            // Re-checked rather than trusted from the walk: the walk filters on
            // the same test, but an override file list does not go through it.
            var ext = _fs.Path.GetExtension(filePath);
            if (!IsCacheExtension(ext))
                continue;

            // Containment guard at candidate creation. A walk file is normally
            // in-bounds (it came out of the folder root), but assert it, so a
            // reparse point that slipped the enumeration filter is dropped rather
            // than offered. A refused candidate is logged and skipped; an
            // unproven one is kept off the list under words that do not claim
            // more than the check showed, and a transient read failure
            // self-heals on the next scan.
            var walkSafety = CandidateGuard.CheckSafeToRemove(filePath, cacheRoot);
            if (walkSafety != CandidateGuard.RemovalSafety.Safe)
            {
                refusedCandidates++;
                refusalLog.Record(new InvalidOperationException(
                    walkSafety == CandidateGuard.RemovalSafety.Refused
                        ? $"Removal candidate refused (not directly in the Installer cache, or a reparse point): {filePath}"
                        : $"Removal candidate not offered (its symlink status or location could not be read): {filePath}"),
                    cause: $"walk/{walkSafety}");
                continue;
            }

            unclaimedByPath.Add(new OrphanedFile(
                FullPath: filePath,
                SizeBytes: walked.SizeBytes,
                IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                IsRemovablePatch: false,
                IsObsoleted: false,
                Reason: Strings.Reason_Orphaned));
        }

        // THE SECOND HALF OF THE PATH COMPARISON, and it is part of that gate
        // rather than of the declared-product screen below. The loop above asked
        // whether any registration's recorded path is SPELLED the same as a walked
        // file. This
        // asks whether any registration's recorded path NAMES the same file, which
        // is the question that was always meant and which no comparison of strings
        // can settle.
        //
        // The two are separated by cost, not by principle. Asking the filesystem
        // costs a handle per file, and this folder reaches millions of them, so the
        // string comparison runs over the whole walk and this runs over what it
        // left: bounded by the candidate count on one side and the registration
        // count on the other, both small on every machine anybody has measured.
        //
        // A CLAIMED CANDIDATE IS NOT COUNTED ANYWHERE, and that is deliberate
        // rather than an omission. A file the string comparison matched has never
        // been counted either; it is simply claimed, and so is this one. Such a
        // file is not kept back at all: a registration names it, which is the
        // ordinary answer, and a count of it would invite a sentence about files
        // the app was unsure of that was false of every one of them.
        //
        // A CANDIDATE THIS PASS CANNOT IDENTIFY IS A DIFFERENT THING AND IS KEPT
        // BACK. It leaves the candidate list as well, so the two lists still
        // account for every file the walk found, but it goes onto the withheld
        // list rather than out of the reckoning. What was read on each side is
        // counted, in the two tallies this returns.
        //
        // THE THREE GATES BELOW ARE MEASURED BEFORE IT RUNS, and before the
        // declared-product screen and the age check too. Each of them reads an empty
        // candidate list as evidence that the comparison never worked, and a
        // candidate dropped here is the comparison WORKING: it found the registration
        // that names the file. Counting after the drop would let a machine whose every candidate
        // turned out to be a registered file under another spelling be refused as a
        // machine whose comparison was broken, and counting after the screen would
        // do the same to a machine whose candidates were all kept back.
        candidatesFromWalk = unclaimedByPath.Count;

        (registrationIdentityReads, candidateIdentityReads) =
            DropCandidatesRegisteredUnderAnotherSpelling(
                unclaimedByPath, withheld, withheldBy, registered, cancellationToken);

        // WHAT DECIDES THIS HALF OF THE OFFER: TWO COMPARISONS, ONE SCREEN AND ONE
        // AGE CHECK, and the difference between them is which end they start at. The
        // path comparison and the file-identity match above both start at a
        // REGISTRATION and ask whether it names this file. The screen a few lines
        // below starts at the FILE: it asks an installation package which product
        // it declares itself to belong to, puts that product code to Windows, and
        // keeps the file back where Windows still holds a record of it and some
        // installation of it opens a package, its cached copy or its original at a
        // source, not shown to be another file, or where the question could not be
        // settled. It asks a patch which patch it declares itself to be, finds the
        // registrations Windows holds of that patch, and keeps the file back where
        // some registration opens a copy of the patch, its cached copy or its original
        // at a source, not shown to be another file, or where the question could not
        // be settled; see IDeclaredProductCheck. The age check after it also starts at
        // the file, and asks it when it was last created, written or changed. Both can
        // subtract from the offer and do nothing else, so what survives all four is the
        // offer.
        //
        // THE CLASS WHERE A REGISTRATION EXISTS AND THE SCAN FAILED TO MATCH IT TO
        // ITS FILE is reached by four separate mechanisms besides, which is the
        // thing to know before anybody simplifies one of them away. A registration
        // written in a spelling the walk never produces is matched by volume serial
        // and file ID immediately above, whatever it was spelled as, for as long as
        // its recorded path can be opened. A product the API enumeration lost is
        // claimed by the registry fallback instead, which reads the same UserData
        // keys and contributes the same paths. When the fallback is ALSO failing
        // reads, the scan does not offer the file: it refuses outright, at
        // InstallerQueryService's records-unreadable gate, on one unreadable
        // product and any fallback failure. And a recorded path holding an
        // environment variable is expanded before it is resolved, so it is compared
        // as the path it names.
        //
        // ALL FOUR OF THOSE READ A REGISTRATION, WHICH IS WHY THE SCREEN IS NOT ONE
        // MORE OF THEM. Where a product's or a patch's records hold no path to
        // match, all four have nothing to work from and nothing records the gap: a
        // LocalPackage value that is present and zero-length merges no claim AND
        // sets no shortfall, so the enumeration reports itself complete while short
        // of a claim, and the cached file is walked and matched against nothing.
        // Asking the file is the only view of that which does not go through the
        // records, and the screen is what keeps such a file.
        //
        // A REGISTRATION WHOSE RECORDED PATH RESOLVES TO NOTHING, WHERE THAT IS NOT A
        // READ FAILURE, IS REACHED BY THE SCREEN. No counter fires and no gate
        // refuses for it, and the file it means can sit in the folder under a
        // spelling the identity match cannot reach, there being nothing to open. The
        // screen starts at that file and has no interest in the registration that
        // could not be resolved. Where the file is an installation package, the
        // screen either finds the declared product installed with a recorded package
        // that is not present, or fails to settle the question, and both keep the
        // file. Where the file is a patch, the screen keeps it where it finds that
        // registration with its recorded copy not present, and where it cannot settle
        // the patch's registrations.
        //
        // THE FOUR ABOVE ARE NOT BELT AND BRACES. Each is there for the case written
        // beside it, which none of the others reaches.
        //
        // AND THE FIFTH IS HERE: A CLAIM THIS SCAN COULD NOT SETTLE WITHHOLDS THIS
        // WHOLE HALF. Where any registration's recorded path could not be turned into
        // a path at all, or could be but the filesystem would not settle its spelling,
        // that claim is compared in a form that matches nothing the walk produces, so
        // any candidate in this list can be the cached file it names. WHICH ONE
        // cannot be established: the claim did not resolve, and the identity
        // match immediately above cannot be relied on to have reached it, since the
        // reasons a path will not resolve are largely the reasons a handle on it will
        // not open. Every candidate is therefore one that claim could have meant, so
        // none of them can be offered.
        //
        // IT CAN BE CAUSED BY AN ABSENCE OR BY A PERMISSION. Where the app can detect
        // that one of its own checks did not answer, it offers nothing that scan, and
        // no weighing of how often the condition arises enters it. So an unattached
        // drive, an unmapped share and a refused handle withhold alongside a value
        // that is not a path.
        //
        // A REGISTRATION WHOSE FILE IS SIMPLY GONE REACHES NONE OF THIS. The resolver
        // climbs to an existing ancestor and reattaches the missing suffix as text, so
        // a missing file resolves normally and this rule never sees it. What fires here
        // is a value that is not a path, or one the filesystem declined to settle.
        //
        // THE SUPERSEDED HALF OF THE OFFER IS NOT WITHHELD WITH IT. Those rows come
        // from the registered set rather than from this candidate list. They are
        // judged on products, through registry keys read by product code and patch
        // code, and every claim the Windows Installer enumeration returns is merged
        // into that set under its normalised recorded path, so a claim under a
        // superseded row's path that is not removable keeps that file off the offer,
        // whichever of the two was read first.
        //
        // AND IT IS NOT WITHHELD WITH THE SECOND-INSTANCE CONDITION EITHER. A second
        // copy is registered under its own product code and is therefore asked by the
        // per-product condition like any other product, whatever the cached patch's
        // own Template names, so a patch it still holds takes the offer away.
        //
        // THE QUESTION IS ASKED OF THE CENSUS RATHER THAN ASSEMBLED HERE. Naming the
        // members one by one at this line is correct and is one edit away from not
        // being: a cause added to the split is then a cause this rule silently does
        // not act on, with a green build, a counter still reporting it and nothing to
        // show for it but files still being offered. So the question is spelled once,
        // where the members are.
        // ASKED ONCE AND KEPT, rather than asked here and asked again where the
        // result is reported. The hosts need to know that this branch was taken, and
        // a second reading of the census further down would be a copy of this rule
        // able to answer differently from it after any edit to either.
        //
        // AND THE SIXTH, WHICH IS THE SAME ARGUMENT ABOUT THE OTHER HALF OF THE
        // COMPARISON. A recorded path can be settled and the file it names still not
        // be identifiable: the handle is refused, or the volume will not answer for
        // it. Such a registration claims nothing through the identity pass, so any
        // candidate in this list can be the cached file it names and WHICH ONE cannot
        // be established. It is the same conclusion for a different reason, which is
        // why the two are asked separately here and never added into one number
        // anywhere.
        //
        // TWO RECORDS, EACH ASKED ITS OWN QUESTION, AND NO MEMBER NAMED IN THIS
        // FILE. A cause added to either population is acted on because the question
        // is spelled where the members are declared, and this line only says that a
        // failure on either side withholds.
        //
        // AND THE THIRD, ARMED BY A POSITIVE FINDING AND BY A FAILURE TO ESTABLISH ONE.
        // What all three share is that something this scan established or could not
        // establish leaves it unable to say which cached files belong to which programs.
        // A product installed as a second instance of itself registers under a code the
        // instance transform produced while its cached package declares the base code,
        // and the per-file screen below reads a code out of a file and asks Windows
        // about it. On such a machine that screen can be told there is no record while
        // a live registration still needs the file, and no part of this scan can work
        // out WHICH cached file belongs to the second copy. So where the scan cannot
        // establish that no product is such a copy, the screen is not run and nothing
        // walk-derived is offered. The question is asked of the census, where its
        // members live, on the same rule as the other two.
        //
        // AND IT IS ONE CALL RATHER THAN THREE CONDITIONS SPELLED OUT HERE. A host
        // names which of these held, so the gate and that host read the same
        // expression: a condition added to WithholdingLeg is one this line acts on and
        // one that host prints. A condition written in beside this call instead,
        // which would withhold an offer the breakdown has nothing to say about, is
        // what FileSystemScanServiceWithholdingLegsTests holds this line against.
        var withholdWalkOfferWholesale =
            WithholdingLegs.Any(query.Census, registrationIdentityReads);

        if (withholdWalkOfferWholesale)
        {
            // Every candidate is already kept back on a fact about the machine, so
            // the per-file screen below could only reach the same answer at the cost
            // of opening every one of them. Skipped rather than run and thrown away.
            //
            // Candidates the identity pass already withheld one at a time are on the
            // withheld list and off this one, so nothing lands on it twice.
            withheld.AddRange(unclaimedByPath);
            withheldBy.Wholesale(unclaimedByPath.Count);

            // WHAT THE HOSTS ARE TOLD IS THAT THE WITHHOLDING CAUGHT SOMETHING, AND NOT
            // MERELY THAT THIS BRANCH WAS TAKEN. A walk that produced no unclaimed
            // candidates reaches here having held nothing back, and the screen that
            // reads this flag announces a count of files held back from the offer,
            // which at zero is both absurd and untrue; for that machine the all-clear
            // is right, nothing in the folder having gone unclaimed.
            //
            // IT IS DECIDED HERE BECAUSE THE HOST CANNOT DECIDE IT SAFELY. A host
            // counting ScanResult.WithheldFiles would be reading a list more than one
            // decision contributes to, so the moment any of their memberships changes
            // the screen's gate changes meaning with it and nothing fails. This
            // branch is the only thing that knows what THIS withholding took, so this is
            // where the question is answered.
            walkOfferWithheldWholesale = unclaimedByPath.Count > 0;
        }
        else
        {
            WithholdCandidatesByWhatTheyDeclare(
                unclaimedByPath, withheld, withheldBy, cacheRoot, cancellationToken,
                (ex, cause) => refusalLog.Record(ex, cause));

            // THE LAST DECISION ON THIS HALF, AND IT TAKES WHAT THE SCREEN LET THROUGH.
            // Run after the screen rather than before it, so the screen's own verdicts
            // are counted where they always were and this counts only what it kept
            // back from the offer.
            WithholdCandidatesNotShownADayOld(
                unclaimedByPath, withheld, withheldBy, scanClock, cancellationToken);

            removable.AddRange(unclaimedByPath);
        }

        // Stat every registered package once here so the Details window
        // doesn't have to hit disk on the UI thread when it opens.
        foreach (var pkg in registered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long size = 0;
            bool exists = false;
            try
            {
                if (_fs.File.Exists(pkg.LocalPackagePath))
                {
                    exists = true;
                    size = _fs.FileInfo.New(pkg.LocalPackagePath).Length;
                }
            }
            // Same narrowed set as SafeLength: IOException for locked /
            // vanished, UnauthorizedAccessException for the deeply ACL'd payload
            // subfolder case, SecurityException for the rare CAS-policy path.
            // OOM / SOH propagate.
            catch (IOException) { /* file vanished or locked between Exists and Length */ }
            catch (UnauthorizedAccessException) { /* unreadable payload subfolder */ }
            catch (SecurityException) { /* CAS policy denies the FileInfo construction */ }

            var sized = pkg with { FileSizeBytes = size, FileExists = exists };

            // The correlation measurement, taken on every registered row before
            // the branch below splits them by verdict. A row's verdict has
            // nothing to do with whether the two sides of the scan describe the
            // same folder, so measuring it inside the branches is what would let
            // two rules into one sum.
            var namesFileInFolder = NamesFileDirectlyIn(pkg.LocalPackagePath, walkedFolder);
            if (namesFileInFolder)
            {
                registeredNamingFolder++;
                if (exists) registeredInFolderPresent++;
            }

            // THE SCAN-TIME COUNTS, TAKEN OFF THE MACHINE AND NEVER OFF THE OFFER.
            // Every cached patch path whose merged row Windows reports superseded or
            // obsoleted is counted here, one per path however many programs register
            // the patch, whatever its removability, whether or not its file is on the
            // disk and whether or not anything is offered. A count derived from the
            // offer sees only the rows that reached it, so it cannot answer whether a
            // machine HAS any. Obsoleted patches are not offered at all, so this is
            // the only way that class is ever visible. The superseded count's
            // difference from the offered figure is a mixed set, several separate
            // conditions keeping a row off the offer, so no single cause is stated
            // for it.
            if (pkg.PatchState == 2) supersededRegistrations++;
            else if (pkg.PatchState == 4) obsoletedRegistrations++;

            // A SUPERSEDED PATCH THAT SURVIVED EVERY WITHHOLDING IS OFFERED, and this
            // is the branch that offers it. What it rests on is not this line: by the
            // time a row arrives here carrying IsRemovable it has passed a positively
            // read Superseded state, its own positively read Uninstallable, and the
            // per-product condition that asks whether anything on any product sharing
            // the patch could be uninstalled and roll back onto its file. Four later
            // passes can still take the verdict away and none can grant one.
            //
            // ON DISK ONLY. A removable row whose file has already gone has nothing to
            // offer and belongs in the missing counts below, which is where it goes.
            //
            // THE CONTAINMENT GUARD RUNS HERE TOO, exactly as it does on a walked
            // candidate, and for the same reason: a registered path is a string out of
            // the records rather than something this run enumerated, so it has not been
            // shown to sit directly in the cache folder and must not be offered on the
            // strength of the records alone.
            if (exists && sized.IsRemovable)
            {
                var safety = CandidateGuard.CheckSafeToRemove(pkg.LocalPackagePath, cacheRoot);
                if (safety == CandidateGuard.RemovalSafety.Safe)
                {
                    removable.Add(new OrphanedFile(
                        FullPath: pkg.LocalPackagePath,
                        SizeBytes: size,
                        IsPatch: true,
                        IsRemovablePatch: true,
                        IsObsoleted: false,
                        Reason: Strings.Reason_Superseded));
                    // AND IT COMES OUT OF THE KEPT LIST, which is the half that is
                    // easy to forget. The kept list drives the left-alone count, the
                    // left-alone bytes and the details window, so a row on the offer
                    // that stayed in it would be shown to the user twice and counted
                    // on both summary lines. Taking the row out here applies that rule
                    // at the one place that can see both destinations.
                    continue;
                }

                refusedCandidates++;
                refusalLog.Record(new InvalidOperationException(
                    safety == CandidateGuard.RemovalSafety.Refused
                        ? $"Registered removal candidate refused (not directly in the Installer cache, or a reparse point): {pkg.LocalPackagePath}"
                        : $"Registered removal candidate not offered (its symlink status or location could not be read): {pkg.LocalPackagePath}"),
                    cause: $"registered/{safety}");
            }

            sizedPackages.Add(sized);

            // THE BANNER FIRES WHEN SOMETHING COULD STILL REACH FOR A FILE THAT IS
            // GONE. That is the whole rule, and the split below is how it is computed.
            //
            // NEITHER BARE AXIS WOULD DO IT. Splitting on the patch STATE alone says a
            // missing superseded file is benign because Windows has marked the patch
            // replaced, and that claim is false: with the superseded files gone,
            // uninstalling the superseding patch discards both patches and goes to the
            // unpatched base, with Windows looking for the absent files. Splitting on
            // the app's own REMOVABLE verdict alone fires on every missing obsoleted
            // registration, because an obsoleted patch is not removable for a policy
            // reason rather than a dangerous one.
            //
            // So the benign half is the conjunction: the state is superseded or
            // obsoleted, AND the app has POSITIVELY established that nothing on any
            // product sharing the patch could be uninstalled and roll back onto it.
            // Everything else fires, including every case the app could not establish.
            //
            // THE UNESTABLISHED CASE FIRES, WHICH IS THE OPPOSITE DIRECTION FROM THE
            // OFFER, and the two are not inconsistent: both refuse to claim something
            // the app has not shown. On the offer, what is unshown is that the file is
            // spare, so it is kept. Here, what is unshown is that its absence is
            // harmless, so it is reported. The costs are not symmetrical either: an
            // alarm nobody needed costs somebody a repair they choose to run, and a
            // silence that should have been an alarm costs them a failure months later
            // with nothing pointing back at this.
            //
            // AND A WITHHELD ROW FIRES TOO, WITH ONE EXCEPTION, which turns on what the
            // flag means. It means the enumeration was short of a product, so the whole
            // class is withheld, and it ALSO means this product's patch set could not be
            // established. Such a row fires, and the second reading is why: where the
            // app cannot say that nothing could reach for the file, it has not
            // established that the absence is harmless, so the benign side is closed
            // to it.
            //
            // THE EXCEPTION IS A ROW WITHHELD ONLY BECAUSE ITS PATCH FILE COULD NOT BE
            // READ, and for a row that has reached this branch the file is GONE, so that
            // read is a read of the very file whose absence is the subject. It could not
            // have succeeded for anybody, and it fails identically whatever removed the
            // file, so treating it as a reason to warn had the app raise an alarm about a
            // file the same scan had positively established nothing could reach for.
            //
            // THAT EXCEPTION HOLDS ON A RUN THAT CAME UP SHORT ELSEWHERE. A scan-wide
            // withholding that cleared this marker on any run that lost a claim would
            // put the row back under the banner on the strength of a count whose terms
            // are all about OTHER products. The residual such a count reaches for is
            // real and is answered where answering still changes an outcome: such a run
            // removes no superseded patch at all.
            //
            // WHAT STILL FIRES FROM THE WITHHELD SIDE. A row whose patch set could not be
            // established carries an Unestablished verdict, so the state-and-verdict test
            // reports it without the flag being consulted. And a run whose machine-wide
            // patch enumeration did not answer downgrades every removable path with no
            // marker set, so such a row reaches here withheld and unmarked and is
            // reported. See MissingFilesReport.Affected, which owns the expression; this
            // comment explains the branch and must never grow a second copy of it.
            if (exists)
            {
                stillUsedBytes += size;
            }
            else
            {
                // Through the one named predicate rather than the expression written out
                // again, because the banner's population and the programs it names have
                // to be the same set and two copies of a conjunction drift.
                if (MissingFilesReport.Affected(sized)) missingAffected++;
                else missingUnaffected++;
                if (namesFileInFolder) missingInFolder++;
            }
        }
        }
        finally
        {
            refusalLog.WriteClosingEntry();
        }

        // A scan that could not put one file inside the folder it was scanning,
        // and whose empty list is indistinguishable from a machine with nothing
        // to clean. On a folder grown to tens of gigabytes, "nothing to clean
        // up" is the one answer a user has no way to question.
        //
        // Both halves are needed. An unproven root alone refuses nothing on a
        // machine with no reparse point in the path, the best-effort spelling
        // and the resolved one being the same string; refusals alone are a real
        // answer about real files, one candidate at a time. The pair is the
        // signature of a run whose comparison never worked.
        //
        // Refusing rather than reporting an empty list, because there is nothing
        // to report: no candidate was judged, so there is no shorter answer to
        // give.
        //
        // WHAT THE GATES BELOW ARE MEASURED AGAINST, AND IT IS NOT THE OFFER.
        // Each asks whether the COMPARISON worked and reads an empty result as its
        // evidence that it did not, so what they need is the count of files the
        // walk put in front of the comparison. On most runs that count and the offer
        // are equal, and they are still not the same question: the file-identity
        // match is why. It removes the candidates that turned out to be registered
        // files under another spelling, which are claims the comparison MADE rather
        // than files it failed to judge, so a machine whose every candidate resolved
        // that way has an empty offer and a comparison that worked perfectly.
        // Reading the offer here would refuse that machine.
        //
        // It is the walk's candidates and NOT the offer, and the difference is real
        // again now that a registered row can reach the offer without ever having been
        // a candidate. A machine whose only offered files are superseded patches has an
        // offer and no candidates, and the gates below must read that as a comparison
        // that found nothing rather than as a comparison that worked.
        var candidatesFromComparison = candidatesFromWalk;

        if (!cacheRoot.Proven && refusedCandidates > 0 && candidatesFromComparison == 0)
            throw new LocalisedInvalidOperationException(Strings.Error_ScanCacheRootUnresolved);

        // The registered rows this scan is keeping. A superseded patch that reached
        // the offer is not among them, having been left out at the loop above rather
        // than filtered out here.
        var stillUsed = sizedPackages.AsReadOnly();

        // The populations inside that list, counted off the list itself rather
        // than tallied through the loop above. That is the point rather than a
        // tidiness: what the counts have to partition is exactly the set the
        // window shows, and a counter incremented on a different pass can come
        // apart from it without anything noticing. The unjudged rows are the ones
        // carrying no claim, so a sentence about files being needed is true of the
        // claimed count and of nothing else.
        var registeredClaimed = stillUsed
            .Count(p => !p.RemovableWithheld && !p.VerdictUnreadable);
        var registeredClaimedBytes = stillUsed
            .Where(p => p.FileExists && !p.RemovableWithheld && !p.VerdictUnreadable)
            .Sum(p => p.FileSizeBytes);
        // TWO COUNTS OVER ONE FLAG, AND THEY ANSWER DIFFERENT QUESTIONS. The pair
        // below is not a duplication: one variable serving both gives one answer to
        // two questions.
        //
        // The partition member counts ROWS. It has to, because what the three counts
        // partition is exactly the kept list, a row whose file has already gone
        // included, and a member that left one out would leave a hole in it. The
        // registered-files window is built from that same list and decides its own
        // rows on its own terms, so it is not what settles this.
        //
        // The cost figure counts FILES, and only the ones that are there. It answers
        // what the withholding cost this run, and a row whose file is not on the disk
        // cost nothing: an absent file could never have been offered, the branch that
        // offers a superseded row being gated on its existence. Counting it inflated
        // the one instrument this project has for telling whether the withholding is
        // expensive, and inflating that invites relaxing the very condition the
        // release exists to add.
        //
        // The flag itself is right in both cases and is not narrowed here. It records
        // a true fact about the RECORDS, established in the enumeration, which cannot
        // know whether a file exists: existence is settled here, against the injected
        // filesystem. Clearing it later would move the row into the claimed count,
        // which asserts a live claim the app has not established.
        var registeredWithheld = stillUsed.Count(p => p.RemovableWithheld);
        var withheldCost = stillUsed.Count(p => p.RemovableWithheld && p.FileExists);
        var registeredUnjudged = stillUsed.Count(p => p.VerdictUnreadable);

        // SUPERSEDED AND OBSOLETED ROWS THIS SCAN IS KEEPING, which is a different
        // population from the one this pair counted before the offer came back. Every
        // superseded row that passed the per-product condition has left the kept list
        // for the offer, so what is counted here is the class the app declined to
        // offer: superseded rows some product could roll back onto or whose patch set
        // could not be established, plus every obsoleted row, which is not offered at
        // all. Files on disk only, a registration whose file has already gone having no
        // space to give back and belonging to the missing counts.
        //
        // A sub-count of the claimed rows rather than a fourth population (one shape
        // falls under the unjudged instead, a State that read 2 or 4 whose Uninstallable
        // read then failed), so the two are never added.
        var registeredSuperseded = stillUsed
            .Count(p => p.IsSupersededOrObsoleted && p.FileExists);
        var registeredSupersededBytes = stillUsed
            .Where(p => p.IsSupersededOrObsoleted && p.FileExists)
            .Sum(p => p.FileSizeBytes);

        // The first correlation question, and it is asked before the numeric one
        // because it is the one that can be answered outright. Of the rows
        // Windows holds, do ANY of them name a file sitting directly in the
        // folder this run walked? Existence is not part of it, and that is what
        // makes it work where a survivor count cannot: a machine whose cache
        // another tool emptied still has registrations naming in-folder files,
        // they are simply gone, where a machine whose two sides describe
        // different places has none and can have none. Counting how many exist
        // cannot separate those two; asking whether any point here does.
        //
        // A registered set that is empty is not an answer to it, so it is not
        // treated as one. Nothing can be asked of no rows, and the empty case has
        // its own gate upstream (InstallerQueryService's Error.InstallerDbEmpty),
        // which is where it belongs: this one would be reporting a mismatch it
        // never measured.
        //
        // Ordered before the numeric gate deliberately, and it is not a
        // precedence chain covering a false sentence: where both conditions hold,
        // both messages are true of the machine, and this one names what was
        // actually established rather than a proportion.
        if (registered.Count > 0
            && registeredNamingFolder == 0
            && candidatesFromComparison > 0)
            throw new LocalisedInvalidOperationException(Strings.Error_ScanNoRegisteredFileInFolder);

        // Correlation sanity gate. On any real machine some registered path
        // resolves to a file that is actually there. If next to none do, yet the
        // walk still yielded files to offer for removal, then what Windows says
        // it has and what the folder holds have not correlated, and no healthy
        // machine looks like that.
        //
        // ONE RULE FOR EVERY ROW: a survivor is a registered path that lexically
        // names a file directly in the walked folder AND is on disk. DO NOT COUNT
        // ANY ROW ON File.Exists ALONE. A handful of packages cached under a user
        // profile exist and are nowhere near the folder, and counting them holds the
        // count above the absolute bound, which disarms this gate on exactly the
        // machine whose correlation is broken.
        // Paths are normalised before they are claimed, in InstallerQueryService's
        // NormaliseLocalPackagePath, which is what makes a lexical test the right
        // one here.
        //
        // A tool that genuinely wiped the cache would leave no files to be
        // orphans, so the candidate clause rules that benign case out. Refuse the
        // scan rather than offer the whole cache for deletion on a broken
        // correlation.
        //
        // A survivor or two must not disarm it, which testing for a total
        // collapse did: a mismatch that spares one path in two hundred is the
        // same fault as one that spares none.
        //
        // Every registered file found on disk counts as one, superseded ones
        // included. What the gate is asking is whether any registered path named
        // a real file in the folder, and a superseded patch's does exactly as
        // much as an applied one's. One that exists in the folder and then fails
        // the containment guard counts too, and that is not an oversight: the
        // guard answers whether a file may be removed, which is a different
        // question from whether the records and the folder line up, and a row that
        // answered neither counter would fall out of this arithmetic entirely.
        //
        // BOTH SIDES OF THE PROPORTION ASK ABOUT THE SAME POPULATION, the
        // registrations naming a file directly in this folder, and the gate reads
        // them on that one rule. A missing side taken as the whole needed set
        // wherever its paths pointed would count a registration naming a file
        // somewhere else that has gone against a folder it says nothing about, with
        // that registration unable to answer back on the survivor side, and would
        // refuse a machine whose folder correlation is perfect on forty absent
        // registrations that were never in the folder to begin with.
        //
        // THE NARROWER READING IS THIS GATE'S ALONE and is not shared with the
        // missing-from-disk report, which still counts every registration whose file
        // has gone wherever it pointed: see the counters' own notes above for why the
        // two must not be the same number.
        //
        // SUPERSEDED ROWS ARE ON BOTH SIDES. The survivor side counts every
        // in-folder registration whose file is there, superseded ones included, and
        // the missing side counts the same rows: a superseded file having gone is
        // not read as its expected end state. So a machine whose absent in-folder
        // registrations are mostly superseded patches can reach the bound. That
        // needs at most two registered files present in the folder, twenty times
        // as many absent, and an offer with something in it, which is not a shape
        // a healthy machine takes.
        //
        // Two rather than a round number, because the absolute bound answers the
        // finding and no more; machines with most of their cache missing are
        // real, another tool having emptied the folder being exactly what the
        // missing-from-disk banner is for.
        //
        // The proportional clause is 19P < M, with P floored at one before it is
        // applied. Unfloored it is 0 < M at P = 0, so one missing row refused the
        // whole scan there: a machine with a single registered package whose file
        // has gone, which is what the missing-from-disk banner exists to report
        // and not the fault this gate was written for. Floored, a machine with no
        // survivor has to show the same twenty missing rows a machine with one
        // survivor already had to, and P = 1 and P = 2 are arithmetically
        // untouched. It is a fifth of the in-folder registrations rather than of
        // all of them, and the tests pin both sides at each P the absolute bound
        // admits.
        var presentRegistered = registeredInFolderPresent;
        var survivorsForBound = Math.Max(presentRegistered, 1);
        if (presentRegistered <= 2
            && survivorsForBound * 20 < survivorsForBound + missingInFolder
            && candidatesFromComparison > 0)
            throw new LocalisedInvalidOperationException(Strings.Error_ScanCorrelationFailed);

        progress?.Report(new ScanProgressUpdate(string.Format(Strings.Status_FoundUnused,
            DisplayHelpers.FormatCount(removable.Count),
            DisplayHelpers.PluraliseFile(removable.Count))));
        // THE PROGRESS LINE COUNTS WHAT IS OFFERED AND NAMES NOTHING KEPT BACK.
        // Files ARE kept back per file, by the declared-product screen, the age check
        // and the identity comparison, and they go on the withheld list rather
        // than into this sentence: what put them there differs between them, and one
        // running count over the lot could only be described by a cause false of some
        // of its members.
        //
        // WHAT THIS SCAN CAN ANSWER ABOUT ITSELF is in query.Census, which carries
        // the enumeration's own failures per product. That is a fact about the
        // records rather than about any file, and it is the shape a count of
        // withholding has to have: a count of files kept back is only ever
        // interesting alongside the reason, and the four reasons here have no honest
        // superordinate to report them under.
        return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes,
            missingAffected, missingUnaffected,
            // WITHHELD IS A REAL FIGURE AGAIN AND WAS A LITERAL ZERO IN THE COMMITS
            // BETWEEN. It counts what the withholding cost this run: superseded rows on
            // disk that the scan would have offered had it been able to establish that
            // nothing on any product sharing them could roll back onto the file.
            // Counted off the kept rows rather than tallied, on the same reasoning as
            // the three counts above it, and over the rows whose file is still there,
            // which is the half the partition member below deliberately does not share.
            query.UnaccountedProductCount, withheldCost,
            query.Census,
            // Read after the classification is settled, so a probe that threw
            // could not cost anybody a scan; it does not throw, and the ordering
            // is the guarantee rather than the interface's promise.
            _shortNames?.Read() ?? ShortNameCreationLabels.Unreadable,
            registeredClaimed,
            registeredClaimedBytes,
            registeredWithheld,
            registeredUnjudged,
            registeredSuperseded,
            registeredSupersededBytes,
            supersededRegistrations,
            obsoletedRegistrations,
            withheld.AsReadOnly(),
            // WHICH BRANCH WAS TAKEN, not what the offer ended up holding. A host
            // cannot recover this from the lists: an empty offer means either that
            // the folder held nothing to offer or that the scan could not establish
            // enough to offer anything, and those are opposite things to tell
            // somebody. Carried as a bool with no cause attached, because several
            // conditions reach that branch and a sentence naming one would be false
            // on the others.
            walkOfferWithheldWholesale,
            // What the identity comparison was told, per side. The registration
            // side's refusals are one of the conditions behind the bool above; the
            // candidate side's are the files it kept back one at a time.
            registrationIdentityReads,
            candidateIdentityReads,
            // Which decision took each file on the list two lines above. Read here
            // rather than derived, and held to that list's own length by a test:
            // nine counts that no longer sum to it mean a tenth arm has been
            // added and is reported by none of them.
            withheldBy.Taken(),
            withheldBy.DeclaredProductInstalledBytes,
            withheldBy.UnderADayOldBytes,
            withheldBy.DeclaredPatchRegisteredBytes);
    }

    /// <summary>
    /// One file the walk found, carrying the size its directory entry already
    /// held. Windows fills the size in as part of enumerating the folder, so
    /// asking for it again per candidate was a second metadata read of a figure
    /// already in hand: on an 800,000-entry cache folder, 776,000 of them.
    ///
    /// A directory entry's size can in principle lag a fresh read, for a file
    /// with a writer still holding it open. Nothing decides anything on this
    /// figure: it is the size column, the totals on the main screen and in the
    /// confirmation dialog, and the freed-bytes figure the completion screen and
    /// the opt-in result log carry. Which files are offered does not depend on
    /// it, and a file being written is one the walk-before-query ordering, the
    /// removable re-verify and the action-time gates already govern.
    /// </summary>
    private readonly record struct WalkedFile(string FullPath, long SizeBytes);

    /// <summary>
    /// Moves out of <paramref name="candidates"/> every file some registration's
    /// recorded path names, whatever either was spelled as, and moves into
    /// <paramref name="withheld"/> every candidate whose own identity would not read.
    /// Both lists keep walk order. The rule is <see cref="RegistrationIdentityMatch"/>,
    /// which the check made just before a Move or Delete runs again on the same half.
    ///
    /// A CLAIMED CANDIDATE IS NOT COUNTED ANYWHERE: a registration names it, which is
    /// the ordinary answer, and it simply leaves the list as a file the string
    /// comparison matched does.
    /// </summary>
    private (FileIdentityReadTally Registrations, FileIdentityReadTally Candidates)
        DropCandidatesRegisteredUnderAnotherSpelling(
            List<OrphanedFile> candidates,
            List<OrphanedFile> withheld,
            WithholdingSplitTally withheldBy,
            IReadOnlyList<RegisteredPackage> registered,
            CancellationToken cancellationToken)
    {
        // No reader, no comparison: the tallies leave at zero attempts.
        if (_fileIds is null) return (default, default);

        var comparison = RegistrationIdentityMatch.Compare(
            _fileIds, registered, candidates.Select(c => c.FullPath).ToList(), cancellationToken);

        var survivors = new List<OrphanedFile>(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
            switch (comparison.Answers[i].Verdict)
            {
                case CandidateIdentityVerdict.Unclaimed:
                    survivors.Add(candidates[i]);
                    break;
                case CandidateIdentityVerdict.Claimed:
                    break;
                case CandidateIdentityVerdict.Unestablished:
                    withheld.Add(candidates[i]);
                    withheldBy.IdentityUnestablished();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidates), comparison.Answers[i].Verdict,
                        "An identity verdict with no arm here. Name it in the same edit as the enum member.");
            }
        }

        candidates.Clear();
        candidates.AddRange(survivors);
        return (comparison.Registrations, comparison.Candidates);
    }

    /// <summary>
    /// The running split of why each candidate was kept back, folded into the
    /// immutable <see cref="WithholdingSplit"/> the result carries.
    ///
    /// EVERY METHOD SITS BESIDE THE ADD IT COUNTS, one statement after it, so that a
    /// file reaching the withheld list without being accounted for takes a deliberate
    /// edit rather than an oversight. WithholdingSplitTallyTests is what notices if one
    /// ever does: it walks the withholding verdicts the enum declares and holds each one
    /// to an arm of its own, so a verdict arriving without an arm is red by construction.
    /// </summary>
    internal sealed class WithholdingSplitTally
    {
        private int _identityUnestablished;
        private int _wholesale;
        private int _declaredProductInstalled;
        private long _declaredProductInstalledBytes;
        private int _declaredProductUnestablished;
        private int _screenUnanswered;
        private int _underADayOld;
        private long _underADayOldBytes;
        private int _ageUnestablished;
        private int _declaredPatchRegistered;
        private long _declaredPatchRegisteredBytes;
        private int _declaredPatchUnestablished;

        internal void IdentityUnestablished() => _identityUnestablished++;

        internal void UnderADayOld(long sizeBytes)
        {
            _underADayOld++;
            _underADayOldBytes += sizeBytes;
        }

        internal void AgeUnestablished() => _ageUnestablished++;

        /// <summary>
        /// The size of the files counted under the under-a-day-old arm, carried beside
        /// the split for the reason <see cref="DeclaredProductInstalledBytes"/> is.
        /// </summary>
        internal long UnderADayOldBytes => _underADayOldBytes;

        internal void Wholesale(int count) => _wholesale += count;

        internal void ScreenUnanswered(int count) => _screenUnanswered += count;

        /// <summary>
        /// The screen's own withholding verdicts, named one by one.
        ///
        /// NONE IS A CATCH-ALL, AND THAT IS THE POINT. <c>Withholds</c> is written
        /// as the complement of the verdicts that let a file through, so a member added
        /// to the enum withholds by default and would arrive here unnamed. Counting it
        /// under any of these would put a cause on it that nobody established, so an
        /// unnamed verdict counts nowhere and the split falls short of the list it
        /// splits. Such a member wants an arm of its own, and
        /// WithholdingSplitTallyTests walks the enum's withholding members against this
        /// switch, so adding one has to be a deliberate edit here as well as there.
        /// </summary>
        internal void Screened(DeclaredProductOutcome outcome, long sizeBytes)
        {
            switch (outcome)
            {
                case DeclaredProductOutcome.DeclaredProductInstalled:
                    _declaredProductInstalled++;
                    _declaredProductInstalledBytes += sizeBytes;
                    break;
                case DeclaredProductOutcome.Unestablished:
                    _declaredProductUnestablished++;
                    break;
                case DeclaredProductOutcome.DeclaredPatchRegistered:
                    _declaredPatchRegistered++;
                    _declaredPatchRegisteredBytes += sizeBytes;
                    break;
                case DeclaredProductOutcome.DeclaredPatchUnestablished:
                    _declaredPatchUnestablished++;
                    break;
            }
        }

        /// <summary>
        /// The size of the files counted under the declared-product-installed arm,
        /// which the result carries beside the split so the held-back sentences can
        /// give the size of the files they speak of and no others.
        /// </summary>
        internal long DeclaredProductInstalledBytes => _declaredProductInstalledBytes;

        /// <summary>
        /// The size of the files counted under the declared-patch-registered arm,
        /// carried beside the split for the reason <see cref="DeclaredProductInstalledBytes"/> is.
        /// </summary>
        internal long DeclaredPatchRegisteredBytes => _declaredPatchRegisteredBytes;

        internal WithholdingSplit Taken() => new(
            _identityUnestablished,
            _wholesale,
            _declaredProductInstalled,
            _declaredProductUnestablished,
            _screenUnanswered,
            _underADayOld,
            _ageUnestablished,
            _declaredPatchRegistered,
            _declaredPatchUnestablished);
    }

    /// <summary>
    /// Moves out of <paramref name="candidates"/> and into
    /// <paramref name="withheld"/> every installation package whose own declared
    /// product Windows still holds a record of, unless every package each
    /// installation of that product opens, cached or original, is shown to be a
    /// different file; every installation package this pass could not settle; every
    /// patch whose own declared patch Windows holds a registration of, unless every
    /// copy of the patch each registration opens, cached or original, is shown to be a
    /// different file; and every patch this pass could not settle. Both lists keep walk
    /// order.
    ///
    /// THE THIRD SOURCE, AND IT IS THE ONLY ONE THAT STARTS AT THE FILE. The two
    /// comparisons above it start at a registration and work towards a file, and
    /// both of them read the same recorded LocalPackage value, so a product or a
    /// patch whose records hold no value to read has nothing for either to find and
    /// nothing records the gap. See <see cref="IDeclaredProductCheck"/> for the
    /// mechanism in full; what it means here is that a cached file can be walked and
    /// matched against nothing while its product is installed or its patch
    /// registered, and this is where such a file is kept.
    ///
    /// IT CAN ONLY EVER SUBTRACT FROM THE OFFER. Nothing it returns adds a file or
    /// clears a withholding made anywhere else, so a scan with no screen injected
    /// offers what the rest of the scan decides.
    ///
    /// A WITHHELD CANDIDATE CARRIES NO CAUSE AND MUST NOT ACQUIRE ONE. It joins a
    /// list that already holds files kept back for a different reason entirely,
    /// and the surfaces that read that list say only that the app left these
    /// alone, which is true of both. Any sentence naming a cause over the whole
    /// list would be false of one half or the other. A cause is spoken per arm of
    /// the split instead, as the command line's reason lines speak it, and the line
    /// for an unestablished verdict is written as alternatives rather than as one
    /// cause.
    /// </summary>
    private void WithholdCandidatesByWhatTheyDeclare(
        List<OrphanedFile> candidates,
        List<OrphanedFile> withheld,
        WithholdingSplitTally withheldBy,
        InstallerCacheRoot cacheRoot,
        CancellationToken cancellationToken,
        Action<Exception, string>? recordRefusal = null)
    {
        if (_declaredProducts is null || candidates.Count == 0) return;

        // The folder a product's or a patch's source list is compared against is the
        // root this run resolved, the one every candidate was judged against.
        var outcomes = _declaredProducts.Screen(
            candidates, cancellationToken, recordRefusal,
            path => InstallerCacheHelpers.NamesAFileDirectlyInInstallerFolder(path, cacheRoot));

        // A screen that answered a different number of candidates than it was
        // given has not answered about these files, and reading it positionally
        // would attach one file's verdict to another. Every candidate is kept
        // rather than none, which is the direction this whole pass fails in.
        if (outcomes.Count != candidates.Count)
        {
            withheld.AddRange(candidates);
            withheldBy.ScreenUnanswered(candidates.Count);
            candidates.Clear();
            return;
        }

        // Partitioned forward into a second list rather than removed in place from
        // the back, so that BOTH sides come out in walk order without either of
        // them being reversed afterwards. Reversing would have been right only
        // while the withheld list was known to be empty on arrival, and it is
        // filled from another decision entirely a few lines up: a later edit that
        // let both fill would have quietly reordered the first one's files.
        var survivors = new List<OrphanedFile>(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (outcomes[i].Withholds())
            {
                withheld.Add(candidates[i]);
                withheldBy.Screened(outcomes[i], candidates[i].SizeBytes);
            }
            else survivors.Add(candidates[i]);
        }

        candidates.Clear();
        candidates.AddRange(survivors);
    }

    /// <summary>
    /// Moves out of <paramref name="candidates"/> and into
    /// <paramref name="withheld"/> every file not shown, by times read on a local NTFS
    /// volume, to have been created, written and changed at least a day before
    /// <paramref name="scanClock"/>. Both lists keep walk order.
    ///
    /// WHAT IT IS FOR. Windows Installer writes a package's new copy into this folder
    /// before any record names it, and until a record does, no comparison with the
    /// records can tell that copy from a spare: it declares its product like any other
    /// copy, and where the product is already installed its records name a different
    /// file that is present. So a file here is offered only once it was created,
    /// written and changed a day or more before the scan, which is read off the file
    /// rather than out of any record. See <see cref="CachedFileAge"/> for how the age
    /// is taken and why from three times.
    ///
    /// EVERY CANDIDATE, INSTALLATION PACKAGE AND PATCH ALIKE. A patch's cached copy
    /// arrives the same way, before any registration names it, and no comparison with
    /// the records can tell it from a spare either.
    ///
    /// ONLY THE WALK'S CANDIDATES. A superseded patch reaches the offer from its own
    /// registration, which names the file, so it is never on this list.
    ///
    /// IT CAN ONLY EVER SUBTRACT FROM THE OFFER. A scan built with no reader runs no
    /// age check, and nothing it returns adds a file or clears a withholding made
    /// anywhere else.
    /// </summary>
    private void WithholdCandidatesNotShownADayOld(
        List<OrphanedFile> candidates,
        List<OrphanedFile> withheld,
        WithholdingSplitTally withheldBy,
        DateTimeOffset scanClock,
        CancellationToken cancellationToken)
    {
        if (_fileTimes is null || candidates.Count == 0) return;

        // Partitioned forward into a second list for the reason the screen's pass
        // gives: both sides keep walk order without either being reversed.
        var survivors = new List<OrphanedFile>(candidates.Count);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outcome = _fileTimes.ReadOutcome(candidate.FullPath, out var times);
            var verdict = CachedFileAge.Judge(outcome, times, scanClock);
            if (verdict == CachedFileAgeVerdict.ShownADayOld)
            {
                survivors.Add(candidate);
                continue;
            }

            // Counted by which of the two keeping verdicts it was, because they are
            // told apart: a file under a day old is kept without a word, and a file
            // whose age was not established is among those the held-back sentence
            // counts.
            withheld.Add(candidate);
            if (verdict == CachedFileAgeVerdict.UnderADayOld)
                withheldBy.UnderADayOld(candidate.SizeBytes);
            else
                withheldBy.AgeUnestablished();
        }

        candidates.Clear();
        candidates.AddRange(survivors);
    }

    /// <summary>
    /// Whether a registered path names a file sitting DIRECTLY in the folder the
    /// walk enumerated, judged on the string alone.
    ///
    /// IT FEEDS THREE COUNTS, AND THROUGH ONE OF THEM IT CAN REFUSE THE WHOLE SCAN.
    /// <c>registeredNamingFolder</c> and <c>registeredInFolderPresent</c> are the two
    /// correlation counts. <c>missingInFolder</c> is the third: it is the other term
    /// in the proportional clause that throws <c>Error_ScanCorrelationFailed</c>, so
    /// an audit of what that gate rests on has to be able to reach it from the
    /// predicate the gate is built on. NO INDIVIDUAL FILE'S FATE TURNS ON ANY OF THE
    /// THREE.
    ///
    /// NOT A GATE, and the distance from <see cref="CandidateGuard.CheckSafeToRemove"/>
    /// is why it exists rather than borrowing that. The guard asks the kernel
    /// where a path really is, because a wrong answer there costs somebody a
    /// file. This asks whether the two sides of the scan describe the same place,
    /// and they meet as strings: orphanhood is decided by string equality between
    /// a registered path and a walked one, so a spelling the walk never produces
    /// is exactly what this has to be able to see. Resolving first would hide the
    /// thing it counts.
    ///
    /// Measured against the WALKED folder and not against the run's resolved
    /// <see cref="InstallerCacheRoot"/>, which is the same point from the other
    /// end. A junctioned or subst-mapped cache resolves to a spelling no
    /// registration carries, so a comparison against the resolved root would read
    /// an ordinary machine as one whose two sides disagree and refuse its scan.
    /// The walked spelling is the one the registrations have to match to be
    /// recognised at all, which is what the count is about.
    /// </summary>
    private static bool NamesFileDirectlyIn(string path, string folder)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var parent = Path.GetDirectoryName(
            path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return parent is not null
            && parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Equals(folder, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Enumerates the walk into a list, checking the cancellation token per
    /// file. Runs inside a <c>Task.Run</c> so the directory walk stays off the
    /// caller's thread (the GUI's dispatcher).
    ///
    /// Reports the running count and no total, because the walk is what
    /// establishes how many files there are: a host gets a number it can show and
    /// cannot place. A total reported from here is one that is still being
    /// counted, and a bar filled in proportion to it moves against a denominator
    /// that keeps growing.
    /// </summary>
    private List<WalkedFile> MaterialiseInstallerFiles(string folder,
        IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken)
    {
        var list = new List<WalkedFile>();
        foreach (var file in GetInstallerFiles(folder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            list.Add(file);
            if (list.Count % WalkReportStride == 0)
                progress?.Report(new ScanProgressUpdate(
                    DisplayHelpers.FormatCount(list.Count), IsMilestone: false, Position: list.Count));
        }
        return list;
    }

    /// <summary>
    /// The walk. One pass over the folder ROOT, yielding each cache file with
    /// the size its directory entry already carried.
    ///
    /// Root only. A registered LocalPackage path only ever sits at the root, so
    /// for any file in a subdirectory the API correlation carries no signal at
    /// all: calling such a file orphaned would be asking Windows about a file it
    /// was never told to track. Root-only makes the candidate set "files at the
    /// root that no registered package claims", which cannot acquire a new blind
    /// spot. Recursing instead needs a denylist ($PatchCache$, the patch
    /// engine's baseline payload copies), and a denylist can only ever exclude a
    /// subtree after it has already bitten someone; root-only puts that whole
    /// subtree out of scope to begin with.
    ///
    /// One pass, not a pass for "*.msi" concatenated with a pass for "*.msp":
    /// each pattern is a complete traversal of the folder's index, and the
    /// second bought only the tenth of the entries that are patches. Filtering
    /// here matches what .NET's own matcher does for those two patterns, which
    /// runs in managed code against the long name (so neither form ever matched
    /// an 8.3 short name), and it is the test the classification loop applies to
    /// every candidate anyway.
    ///
    /// The three things the enumeration options would otherwise carry are stated
    /// here instead, because the entry's own metadata is wanted and only the
    /// <see cref="IDirectoryInfo"/> form carries it, and that form rejects a
    /// changed AttributesToSkip under the test double (System.IO.Abstractions
    /// 22.2.0 raises NotSupportedException). SearchOption.TopDirectoryOnly maps
    /// to AttributesToSkip = 0 and IgnoreInaccessible = false, so:
    /// reparse points are skipped by the same test the option applied, keeping a
    /// junction planted at the root from redirecting the walk outside it, and
    /// now assertable against a MockFileSystem where the option never was;
    /// Hidden and System stay included, because real cache entries sometimes
    /// carry those attributes; and a folder the process cannot read yields
    /// nothing rather than throwing, which is what IgnoreInaccessible bought and
    /// is the only place this scan drops anything quietly. It drops in the safe
    /// direction: fewer files offered, never more.
    /// </summary>
    private IEnumerable<WalkedFile> GetInstallerFiles(string folder)
    {
        if (!_fs.Directory.Exists(folder))
            yield break;

        using var entries = _fs.DirectoryInfo.New(folder)
            .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .GetEnumerator();

        while (true)
        {
            IFileInfo entry;
            try
            {
                if (!entries.MoveNext()) yield break;
                entry = entries.Current;
            }
            // The enumerator opens the folder on the first move, so a DACL that
            // refuses the elevated process surfaces here. Access-denied only,
            // matching what IgnoreInaccessible itself continued past.
            catch (UnauthorizedAccessException) { yield break; }

            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) continue;
            if (!IsCacheExtension(entry.Extension)) continue;

            yield return new WalkedFile(entry.FullName, SafeLength(entry));
        }
    }

    private static bool IsCacheExtension(string extension) =>
        extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".msp", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The size off an enumerated entry. On Windows the directory read already
    /// returned it, so this costs nothing.
    ///
    /// IOException covers locked / vanished files; UnauthorizedAccess covers
    /// payload subfolders the elevated process still can't read (deeply ACL'd
    /// MSI directories); SecurityException covers the rare CAS-policy path. OOM
    /// and the like propagate. A size that could not be read is 0, leaving the
    /// file offered with a zero-byte row rather than dropped: what is offered
    /// must not turn on whether its size could be read. Written out rather than
    /// shared through a delegate, which would put a closure on the heap per
    /// file over a loop this change exists to take allocation out of.
    /// </summary>
    private static long SafeLength(IFileInfo file)
    {
        try { return file.Length; }
        catch (IOException) { return 0; }
        catch (UnauthorizedAccessException) { return 0; }
        catch (SecurityException) { return 0; }
    }

    /// <summary>
    /// The same figure for a path the walk did not produce, so it has to be
    /// asked for. The construction is inside the guard because that is what can
    /// fail on the CAS-policy path. See <see cref="SafeLength"/> for the rest.
    /// </summary>
    private long StatSize(string path)
    {
        try { return _fs.FileInfo.New(path).Length; }
        catch (IOException) { return 0; }
        catch (UnauthorizedAccessException) { return 0; }
        catch (SecurityException) { return 0; }
    }
}
