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
    private readonly IEnumerable<string>? _overrideFiles;
    private readonly string? _installerFolderOverride;

    /// <summary>Production constructor. DI supplies all four dependencies; the override fields stay null.</summary>
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
        IDeclaredProductCheck declaredProducts)
        : this(queryService, fileSystem, shortNames, null, null, fileIdentities, declaredProducts) { }

    /// <summary>
    /// Test constructor. Injects a filesystem and nothing else, for the tests
    /// whose subject is the walk itself.
    /// </summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem)
        : this(queryService, fileSystem, null, null, null, null, null) { }

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
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem,
        IShortNameCreationProbe? shortNames,
        IEnumerable<string>? overrideFiles, string? installerFolderOverride,
        IFileIdentityReader? fileIdentities,
        IDeclaredProductCheck? declaredProducts = null)
    {
        _queryService = queryService;
        _fs = fileSystem;
        _shortNames = shortNames;
        _fileIds = fileIdentities;
        _declaredProducts = declaredProducts;
        _overrideFiles = overrideFiles;
        _installerFolderOverride = installerFolderOverride;
    }

    public async Task<ScanResult> ScanAsync(
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new ScanProgressUpdate(Strings.Status_ScanningCache));

        // Walk the disk BEFORE querying the API, and materialise the walk here
        // rather than leaving it lazy. A package cached after the walk finishes
        // is then simply absent from the candidate set, so a fast install
        // completing during the scan cannot land its freshly cached, still-needed
        // file in the orphan list. It is not a guarantee for every interleaving
        // (a registration that completes in the gap between the query passing its
        // position and the post-scan reboot probe can still slip through); the
        // action-time gates close that sliver, being the pending-reboot re-check,
        // the removable re-verify and the Global\_MSIExecute hold. Task.Run keeps
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
            diskFiles = await Task.Run(() => MaterialiseInstallerFiles(folder, cancellationToken), cancellationToken)
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

        // Candidates no registration claims, in walk order. The path comparison and
        // the file-identity match below it are the whole of what decides THIS half of
        // the offer, and there is no second screening pass over it, so a survivor of
        // those two is an offered file.
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
        // every verdict unproven. One full CrashLog.Write each is then a
        // self-inflicted denial of the log: driven at 100,000 refusals it wrote
        // 19 MB across 37 rotations, and crash.log holds 512 KB with one
        // archive, so nothing that was in the file before the scan survived. The
        // refusals are also the least informative entries possible, being
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
        // whether it is removable or not: the survivor count used to be a
        // non-removable half counted on File.Exists alone plus a removable half
        // counted only after the containment guard passed, which is two rules in
        // one sum, and the larger half proved nothing about the folder at all.
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
        foreach (var walked in diskFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
        // declared-product screen too. Each of them reads an empty candidate list
        // as evidence that the comparison never worked, and a candidate dropped
        // here is the comparison WORKING: it found the registration that names the
        // file. Counting after the drop would let a machine whose every candidate
        // turned out to be a registered file under another spelling be refused as a
        // machine whose comparison was broken, and counting after the screen would
        // do the same to a machine whose candidates were all kept back.
        candidatesFromWalk = unclaimedByPath.Count;

        (registrationIdentityReads, candidateIdentityReads) =
            DropCandidatesRegisteredUnderAnotherSpelling(
                unclaimedByPath, withheld, withheldBy, registered, cancellationToken);

        // WHAT DECIDES THIS HALF OF THE OFFER: TWO COMPARISONS AND ONE SCREEN, and
        // the difference between them is which end they start at. The path
        // comparison and the file-identity match above both start at a
        // REGISTRATION and ask whether it names this file. The screen a few lines
        // below starts at the FILE: it asks an installation package which product
        // it declares itself to belong to, puts that product code to Windows, and
        // keeps the file back where Windows still holds a record of it or where
        // the question could not be settled. It can subtract from the offer and do
        // nothing else, so what survives all three is the offer. Product packages
        // only, for a reason that is load-bearing rather than incidental; see
        // IDeclaredProductCheck.
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
        // environment variable now resolves, that having been the one live hole in
        // the set.
        //
        // ALL FOUR OF THOSE READ A REGISTRATION, WHICH IS WHY THE SCREEN IS NOT ONE
        // MORE OF THEM. Where a product's records hold no path to match, all four
        // have nothing to work from and nothing records the gap: a LocalPackage
        // value that is present and zero-length merges no claim AND sets no
        // shortfall, so the enumeration reports itself complete while short of a
        // claim, and the product's cached package is walked, matched against
        // nothing and offered while the product is installed. Asking the file is
        // the only view of that which does not go through the records.
        //
        // WHAT IS LEFT UNGUARDED, stated so a reader can find it rather than
        // rediscover it: a registration whose recorded path resolves to nothing
        // where that is not a read failure, so no counter fires and no gate
        // refuses, while the file it means sits in the folder under a spelling the
        // identity match cannot reach because there is nothing to open. Never
        // observed, on any machine. It stands open and unguarded rather than
        // closed, and saying so here is the point of this note: two designs to
        // close it have been proposed and both were withdrawn, the second by its
        // own author.
        //
        // THE SCREEN NARROWS THAT ONE WITHOUT CLOSING IT, and the half it leaves is
        // the half to remember. Where such a candidate is an installation package
        // the screen reaches it, having no interest in the registration that could
        // not be resolved: it either finds the declared product installed or fails
        // to settle the question, and both keep the file. Where the candidate is a
        // PATCH the screen does not run at all, so that half stands exactly as the
        // paragraph above describes it.
        //
        // SO THE FOUR ABOVE ARE NOT BELT AND BRACES. This release alone fixed six
        // separate faults in that class, four of them live in every shipped
        // version, so treating any one of them as redundant cover for the others is
        // the mistake this comment exists to prevent.
        //
        // AND THE FIFTH IS HERE: A CLAIM THIS SCAN COULD NOT SETTLE WITHHOLDS THIS
        // WHOLE HALF. Where any registration's recorded path could not be turned into
        // a path at all, or could be but the filesystem would not settle its spelling,
        // that claim is compared in a form that matches nothing the walk produces, so
        // the cached file it names is sitting in this candidate list right now,
        // unclaimed. WHICH ONE cannot be established: the claim did not resolve, and
        // the identity match immediately above cannot be relied on to have reached it,
        // since the reasons a path will not resolve are largely the reasons a handle on
        // it will not open. Every candidate is therefore one that claim could have
        // meant, so none of them can be offered.
        //
        // THE WHOLE SET, WHICH IS THE COST AND IS NOT AN ARGUMENT AGAINST IT. Holding
        // a file back is this app working. The alternative is offering a file it
        // cannot say is spare, and the two are not comparable.
        //
        // IT CAN NOW BE CAUSED BY AN ABSENCE OR BY A PERMISSION, AND THAT IS THE
        // CHANGE 3.0.0 MAKES. This comment used to say the opposite, and said it as
        // the thing separating this rule from two designs that were withdrawn: those
        // acted on an unattached drive, and emptying a machine's whole offer because a
        // USB stick was unplugged was judged too much to pay. The owner has since ruled
        // that trade-off away. Where the app can detect that one of its own checks did
        // not answer, it offers nothing that scan, and no weighing of how often the
        // condition arises enters it. So an unattached drive, an unmapped share and a
        // refused handle now withhold alongside a value that is not a path.
        //
        // A REGISTRATION WHOSE FILE IS SIMPLY GONE STILL REACHES NONE OF THIS, which is
        // the first objection anybody raises and remains the answer to it. The resolver
        // climbs to an existing ancestor and reattaches the missing suffix as text, so
        // a missing file resolves normally and this rule never sees it. What fires here
        // is a value that is not a path, or one the filesystem declined to settle.
        //
        // THE SUPERSEDED HALF OF THE OFFER IS NOT WITHHELD WITH IT. Those rows are
        // judged on products, through registry keys read by product code and patch
        // code, and nothing on that path reads a cached-package path at all; measured
        // with a planted unspellable value against an ordinary-value control, the
        // sibling patch's offer did not move. What is unobserved rather than ruled out
        // is an unsettled registration naming the very same file an offered
        // superseded row names, which would be a second claim on that path that the
        // merge cannot see.
        //
        // AND IT IS NOT WITHHELD WITH THE SECOND-INSTANCE CONDITION EITHER, which was
        // put to fixtures rather than argued because the blunter rule was the obvious
        // one to write. A second copy is registered under its own product code and is
        // therefore asked by the per-product condition like any other product, so a
        // patch it still holds takes the offer away; measured with the cached patch's
        // own Template naming ONLY the base code, which is the instance transform's
        // whole peculiarity, and the offer still went, against a twin in which the same
        // second copy is finished with the patch and the file is offered. The one shape
        // in which such a file IS wrongly offered is a holder neither the enumeration
        // nor the registry can name, and on that machine the condition is undetectable,
        // so a blanket refusal would not have saved the file either: measured on a pair
        // that differs only in whether an unrelated product reads as a second instance,
        // where the same file is wrongly offered both times.
        //
        // THE QUESTION IS ASKED OF THE CENSUS RATHER THAN ASSEMBLED HERE. This line
        // used to name the members one by one, which was correct and was one edit
        // away from not being: a cause added to the split is a cause this rule would
        // silently not act on, with a green build, a counter still reporting it and
        // nothing to show for it but files still being offered. The release that
        // added a fourth cause is the release that would have walked into it, and the
        // release that added a second POPULATION is this one. So the question is
        // spelled once, where the members are.
        // ASKED ONCE AND KEPT, rather than asked here and asked again where the
        // result is reported. The hosts need to know that this branch was taken, and
        // a second reading of the census further down would be a copy of this rule
        // able to answer differently from it after any edit to either.
        //
        // AND THE SIXTH, WHICH IS THE SAME ARGUMENT ABOUT THE OTHER HALF OF THE
        // COMPARISON. A recorded path can be settled and the file it names still not
        // be identifiable: the handle is refused, or the volume will not answer for
        // it. Such a registration claims nothing through the identity pass either,
        // so once again a cached file that is needed is sitting in this candidate
        // list and WHICH ONE cannot be established. It is the same conclusion for a
        // different reason, which is why the two are asked separately here and never
        // added into one number anywhere.
        //
        // TWO RECORDS, EACH ASKED ITS OWN QUESTION, AND NO MEMBER NAMED IN THIS
        // FILE. That is the rule the paragraph above earned: a cause added to either
        // population is acted on because the question is spelled where the members
        // are declared, and this line only says that a failure on either side
        // withholds.
        //
        // AND THE THIRD, WHICH IS A POSITIVE FINDING RATHER THAN A FAILURE TO READ, so
        // the sentence above is now true of two of the three and the superordinate over
        // all of them is one step wider: something this scan established or could not
        // establish leaves it unable to say which cached files belong to which programs.
        // A product installed as a second instance of itself registers under a code the
        // instance transform produced while its cached package declares the base code,
        // and the per-file screen below is the one pass that reads a code out of a file
        // and asks Windows about it. On such a machine that screen can be told there is
        // no record while a live registration still needs the file, and no part of this
        // scan can work out WHICH cached file belongs to the second copy. So the screen
        // is not run and nothing walk-derived is offered. The question is asked of the
        // census, where its two members live, on the same rule as the other two.
        //
        // AND IT IS ONE CALL RATHER THAN THREE CONDITIONS SPELLED OUT HERE, which is
        // the paragraph above arriving where it was always going. A host now names
        // which of these held rather than only that the branch was taken, so the gate
        // and that host read the same expression: a condition added to WithholdingLeg
        // is one this line acts on and one that host prints. A condition written in
        // beside this call instead, which would withhold an offer the breakdown has
        // nothing to say about, is what
        // FileSystemScanServiceWithholdingLegsTests holds this line against.
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
            WithholdCandidatesTheirOwnProductStillClaims(
                unclaimedByPath, withheld, withheldBy, cancellationToken,
                (ex, cause) => refusalLog.Record(ex, cause));
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
            // same folder, so measuring it inside the branches is what let two
            // rules into one sum.
            var namesFileInFolder = NamesFileDirectlyIn(pkg.LocalPackagePath, walkedFolder);
            if (namesFileInFolder)
            {
                registeredNamingFolder++;
                if (exists) registeredInFolderPresent++;
            }

            // THE SCAN-TIME COUNTS, TAKEN OFF THE MACHINE AND NEVER OFF THE OFFER.
            // Every registration Windows reports superseded or obsoleted is counted
            // here whatever its removability and whether or not anything is offered,
            // which is the whole point of them: a count derived from the offer can
            // only ever see the registrations that passed the removability condition,
            // so it cannot answer whether a machine HAS any. Obsoleted patches are not
            // offered at all, so this is the only way that class is ever visible; and
            // the superseded pair differing from the offer-derived figure is itself
            // the finding, being the size of the class the condition excludes, which
            // nobody has measured.
            if (pkg.PatchState == 2) supersededRegistrations++;
            else if (pkg.PatchState == 4) obsoletedRegistrations++;

            // A SUPERSEDED PATCH THAT SURVIVED EVERY WITHHOLDING IS OFFERED, and this
            // is the branch 3.0.0 puts back. What it rests on is not this line: by the
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
                    // on both summary lines. v2.3.0 filtered these out of that list
                    // after the fact; taking the row out here is the same rule at the
                    // one place that can see both destinations.
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
            // NEITHER BARE AXIS WOULD DO IT, and both were tried. Splitting on the patch
            // STATE alone says a missing superseded file is benign because Windows has
            // marked the patch replaced, and that claim was measured false: with the
            // superseded files gone, uninstalling the superseding patch discarded both
            // patches and went to the unpatched base, with Windows demonstrably looking
            // for the absent files. Splitting on the app's own REMOVABLE verdict alone
            // fires on every missing obsoleted registration, because an obsoleted patch
            // is not removable in 3.0.0 for a policy reason rather than a dangerous
            // one. And that is precisely an alarm at past users about files THIS APP
            // removed, which is the scenario the whole reversal exists to avoid.
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
            // AND A WITHHELD ROW FIRES TOO, WITH ONE EXCEPTION, though v2.3.0 put every
            // withheld row on the benign side. What changed is what the flag means.
            // There it meant one thing: the enumeration was short of a product, so the
            // whole class was withheld. From 3.0.0 it ALSO means this product's patch
            // set could not be established, which is exactly the case where the app
            // cannot say that nothing could reach for the file, so putting it on the
            // benign side would call an absence harmless in the one case where the app
            // explicitly failed to establish harmlessness.
            //
            // THE EXCEPTION IS A ROW WITHHELD ONLY BECAUSE ITS PATCH FILE COULD NOT BE
            // READ, and for a row that has reached this branch the file is GONE, so that
            // read is a read of the very file whose absence is the subject. It could not
            // have succeeded for anybody, and it fails identically whatever removed the
            // file, so treating it as a reason to warn had the app raise an alarm about a
            // file the same scan had positively established nothing could reach for.
            //
            // THAT EXCEPTION HOLDS ON A RUN THAT CAME UP SHORT ELSEWHERE, WHICH IS THE
            // WHOLE OF WHAT 3.0.0 SETTLED HERE. The scan-wide withholding used to clear
            // that marker on any run that lost a claim, which put the row back under the
            // banner on the strength of a count whose terms are all about OTHER products.
            // The residual it was reaching for is real and is answered where answering
            // still changes an outcome: such a run removes no superseded patch at all.
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
        // walk put in front of the comparison. That was a separate quantity from
        // the offer while a per-candidate screen sat between the two; the screen has
        // gone and on most runs the two numbers are now equal, but they are still
        // not the same question, and the file-identity match is why. It removes the
        // candidates that turned out to be registered files under another spelling,
        // which are claims the comparison MADE rather than files it failed to
        // judge, so a machine whose every candidate resolved that way has an empty
        // offer and a comparison that worked perfectly. Reading the offer here
        // would refuse that machine.
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
        // TWO COUNTS OVER ONE FLAG, AND THEY ANSWER DIFFERENT QUESTIONS. Both were
        // one variable until 3.0.0, and the pair below is the fix rather than a
        // duplication.
        //
        // The partition member counts ROWS. It has to, because what the three counts
        // partition is exactly the set the registered-files window lists, and that
        // window lists a row whose file has already gone like any other.
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
        // ONE RULE FOR EVERY ROW, which it was not: a survivor is a registered
        // path that lexically names a file directly in the walked folder AND is
        // on disk. It used to be the non-removable rows counted on File.Exists
        // alone, with no containment test of any kind, plus the removable ones
        // counted only after the containment guard passed. A handful of packages
        // cached under a user profile, which exist and are nowhere near the
        // folder, then held the count above the absolute bound and disarmed this
        // gate permanently on a machine whose correlation was wholly broken.
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
        // question from whether the records and the folder line up, and a row
        // answering neither counter used to fall out of this arithmetic entirely.
        //
        // BOTH SIDES OF THE PROPORTION ASK ABOUT THE SAME POPULATION, the
        // registrations naming a file directly in this folder. The missing side
        // was the whole needed set wherever its paths pointed, so a registration
        // naming a file somewhere else that had gone counted against a folder it
        // says nothing about, and could never answer back on the survivor side.
        // A machine whose folder correlation was perfect could be refused on
        // forty absent registrations that were never in the folder to begin with.
        // Its narrower reading is not shared with the missing-from-disk report,
        // which still counts every registration whose file has gone wherever it
        // pointed: see the counters' own notes above for why the two must not be
        // the same number.
        //
        // AND IT DID NOT ASK ABOUT ONE POPULATION UNTIL 3.0.0, the sentence above
        // notwithstanding. The survivor side counted every in-folder registration
        // whose file was there, superseded ones included, while the missing side
        // took only the rows that carried no superseded or obsoleted state, on the
        // reading that such a file having gone was its expected end state. That
        // reading is what this release removes, so the exclusion goes with it and
        // the two sides now measure the same rows. It widens the gate: a machine
        // whose absent in-folder registrations are mostly superseded patches can
        // now reach the bound where it could not before. That needs at most two
        // registered files present in the folder, twenty times as many absent, and
        // an offer with something in it, which is not a shape a healthy machine
        // takes.
        //
        // Two rather than a round number, because the absolute bound answers the
        // finding and no more; machines with most of their cache missing are
        // real, another tool having emptied the folder being exactly what the
        // missing-from-disk banner is for.
        //
        // THE 92-RUN MEASUREMENT NO LONGER DESCRIBES THIS CODE and is recorded
        // here as history rather than as a receipt. Of the 92 result-log runs
        // that could reach this gate at all, none would have been refused by
        // these bounds, taking each run at the worst reading its figures allow;
        // that was measured against a survivor count including registered files
        // anywhere on disk and a missing count doing the same, and both now read
        // the folder only, so neither number it was taken on is the number the
        // code computes. Nothing published anywhere may cite it for the present
        // shape.
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
            removable.Count, DisplayHelpers.PluraliseFile(removable.Count))));
        // THE PROGRESS LINE COUNTS WHAT IS OFFERED AND NAMES NOTHING KEPT BACK, and
        // the three identity counts that used to travel here went with the pass that
        // produced them. Files ARE kept back per file, by the declared-product screen
        // and by the identity comparison, and they go on the withheld list rather
        // than into this sentence: what put them there differs between them, and one
        // running count over the lot could only be described by a cause false of some
        // of its members.
        //
        // WHAT REPLACED THEM AS THE QUESTION THIS SCAN CAN ANSWER ABOUT ITSELF is
        // in query.Census, which carries the enumeration's own failures per
        // product. That is a fact about the records rather than about any file, and
        // it is the shape the removed counts should have had: a count of files kept
        // back is only ever interesting alongside the reason, and the four reasons
        // had no honest superordinate to report them under.
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
            // five counts that no longer sum to it mean a sixth arm has been
            // added and is reported by none of them.
            withheldBy.Taken());
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
    /// Removes from <paramref name="candidates"/> every file that some
    /// registration's recorded path actually names, whatever that path was spelled
    /// as. Mutates the list in place, keeping the walk order of the survivors.
    ///
    /// The registration side is walked first and the candidate side second, so a
    /// machine whose registrations all resolve to files the walk already matched
    /// costs one handle per registration and nothing more. Nothing is opened at all
    /// where there are no candidates, or where no registration yielded an identity.
    ///
    /// A FAILED READ IS A WITHHOLDING GIVEN UP, AND UNTIL 3.0.0 THIS NOTE SAID THE
    /// OPPOSITE. It read "every failure leaves the candidate where it was": a
    /// registration path that will not open contributes no identity and claims
    /// nothing extra, a candidate that will not open is compared against nothing and
    /// carries on being judged by everything downstream. That is sound about a pass
    /// that only subtracts and false about the machine. The registration this pass
    /// could not identify is one whose cached file is sitting in the candidate list
    /// unclaimed, and it was being offered. Both reads are now counted, and both
    /// answers are acted on.
    ///
    /// THE TWO SIDES ACT DIFFERENTLY AND THE ASYMMETRY IS THE WHOLE DESIGN. A
    /// registration nobody could identify might name ANY candidate in the list, and
    /// which one cannot be established, so its tally withholds the walk-derived
    /// offer entire, at the branch below the caller. A candidate nobody could
    /// identify is one file: every other candidate was compared against the
    /// registrations by a read that answered, so this one is moved to
    /// <paramref name="withheld"/> and the rest stand. Emptying an offer over one
    /// unidentifiable file would cost a machine everything for a fact about one of
    /// its files.
    ///
    /// A FILE THAT IS NOT THERE IS NOT A FAILURE OF EITHER KIND. See
    /// <c>FileIdentityRead.NamesNothing</c>: a registration whose cached file has
    /// gone claims none of the walked files, and reading that as a give-up would
    /// empty the offer on most machines that have ever uninstalled anything.
    ///
    /// EVERY REGISTRATION IS READ, INCLUDING THE ONES THE TEXT COMPARISON ALREADY
    /// MATCHED, so a single registration this cannot identify costs the scan its
    /// whole walk-derived offer on an otherwise ordinary machine. That is the cost
    /// and it is deliberate. The obvious narrowing is to read only the registrations
    /// whose recorded path failed to match the walk by text, on the reasoning that a
    /// matched one has already claimed its file and can claim nothing extra.
    ///
    /// WHAT ACTUALLY REFUSES IS NOT A FILE SOMETHING ELSE HAS OPEN, and getting that
    /// wrong makes the cost sound both commoner and smaller than it is. The read
    /// asks for no access bits, so there is nothing for another opener's share mode
    /// to exclude. Once absence is carved out, what is left is an ACL refusing an
    /// already-elevated process, a call that threw, and a volume or Windows build
    /// that will not answer <c>FileIdInfo</c>.
    ///
    /// THAT LAST ONE IS A PROPERTY OF THE VOLUME RATHER THAN OF A FILE, so it does
    /// not cost such a machine one scan. Every registration fails on it, every time,
    /// and that machine is offered nothing from the folder walk until something
    /// about it changes. A whole class of machines told sorry is a defensible thing
    /// to say and a different thing from one unlucky file, so say the one that is
    /// true.
    ///
    /// THE COUNTER-EXAMPLE THAT KILLS IT, recorded here because it is not
    /// reconstructible from anything else: take registration R whose recorded path
    /// matches walked file W by text, so W is off the candidate list and R looks
    /// harmless. If R's path is a reparse point resolving to candidate C, then a
    /// successful identity read would have returned C's identity and dropped C. A
    /// failed read leaves C on the offer, and C is the data behind a registered
    /// package. The narrow version therefore leaves a route to a needed file open,
    /// and holding files back is this app working.
    ///
    /// A HARD LINK IS DROPPED AND THAT IS THE RIGHT ANSWER HERE, though it is a
    /// stricter one than strictly necessary. Two names for one file share an
    /// identity, so a candidate hard-linked to a registered package is treated as
    /// that package. Removing one link would in fact leave the data reachable
    /// through the other, so the file could safely have been offered; withholding
    /// it costs an offer and claims nothing untrue, and no machine measured up to
    /// 2026-08-11 held one (every cached file's link count read 1). The date is part
    /// of the reading: it says what had been looked at, not what every machine holds.
    /// </summary>
    private (FileIdentityReadTally Registrations, FileIdentityReadTally Candidates)
        DropCandidatesRegisteredUnderAnotherSpelling(
            List<OrphanedFile> candidates,
            List<OrphanedFile> withheld,
            WithholdingSplitTally withheldBy,
            IReadOnlyList<RegisteredPackage> registered,
            CancellationToken cancellationToken)
    {
        var registrations = new IdentityReadTally();
        var candidateReads = new IdentityReadTally();

        // Nothing was asked, so nothing was given up: the tallies leave here at zero
        // attempts, which is what tells a report the pass was skipped rather than
        // that it answered cleanly.
        if (_fileIds is null || candidates.Count == 0 || registered.Count == 0)
            return (registrations.Taken(), candidateReads.Taken());

        var registeredIds = new HashSet<FileIdentity>();
        foreach (var pkg in registered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (registrations.Record(_fileIds.ReadOutcome(pkg.LocalPackagePath, out var id))
                == FileIdentityRead.Read)
                registeredIds.Add(id);
        }

        // No identity to compare against, so the candidate side is not asked and its
        // tally says so. A give-up on the registration side has already been counted
        // and is about to withhold the whole offer whichever way this returns.
        if (registeredIds.Count == 0) return (registrations.Taken(), candidateReads.Taken());

        candidates.RemoveAll(c =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = candidateReads.Record(_fileIds.ReadOutcome(c.FullPath, out var id));

            // Claimed: a registration names this file, whatever either of them was
            // spelled as. Dropped rather than withheld, and not counted anywhere,
            // because the pass working is not a file anybody was unsure about.
            if (outcome == FileIdentityRead.Read) return registeredIds.Contains(id);

            // Kept back rather than left on the offer, which is the fix. Removed
            // from the candidate list in the same step so the two lists still
            // account for every file the walk found.
            if (!outcome.GivesUpAWithholding()) return false;
            withheld.Add(c);
            withheldBy.IdentityUnestablished();
            return true;
        });

        return (registrations.Taken(), candidateReads.Taken());
    }

    /// <summary>
    /// One side's running count of what the identity reader answered, folded into
    /// the immutable <see cref="FileIdentityReadTally"/> the result carries.
    ///
    /// <see cref="Record"/> hands the outcome straight back so that counting it and
    /// acting on it are one expression at both call sites, rather than two
    /// statements a later edit can separate. A give-up nothing counted is the fault
    /// this whole pass exists to close.
    /// </summary>
    internal sealed class IdentityReadTally
    {
        private int _attempts;
        private int _namesNothing;
        private int _notAPath;
        private int _openRefused;
        private int _identityUnavailable;
        private int _faulted;

        internal FileIdentityRead Record(FileIdentityRead outcome)
        {
            _attempts++;
            switch (outcome)
            {
                case FileIdentityRead.NamesNothing: _namesNothing++; break;
                case FileIdentityRead.NotAPath: _notAPath++; break;
                case FileIdentityRead.OpenRefused: _openRefused++; break;
                case FileIdentityRead.IdentityUnavailable: _identityUnavailable++; break;
                case FileIdentityRead.Faulted: _faulted++; break;
                    // Read is not counted: it is the attempts less the five, and a
                    // stored copy could disagree with them.
            }

            return outcome;
        }

        internal FileIdentityReadTally Taken() => new(
            _attempts, _namesNothing, _notAPath, _openRefused, _identityUnavailable, _faulted);
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
        private int _declaredProductUnestablished;
        private int _screenUnanswered;

        internal void IdentityUnestablished() => _identityUnestablished++;

        internal void Wholesale(int count) => _wholesale += count;

        internal void ScreenUnanswered(int count) => _screenUnanswered += count;

        /// <summary>
        /// The screen's own two withholding verdicts, named one by one.
        ///
        /// NEITHER IS A CATCH-ALL, AND THAT IS THE POINT. <c>Withholds</c> is written
        /// as the complement of the two verdicts that keep a file, so a member added
        /// to the enum withholds by default and would arrive here unnamed. Counting it
        /// under either of these would put a cause on it that nobody established, so an
        /// unnamed verdict counts nowhere and the split falls short of the list it
        /// splits. Such a member wants an arm of its own, and
        /// WithholdingSplitTallyTests walks the enum's withholding members against this
        /// switch, so adding one has to be a deliberate edit here as well as there.
        /// </summary>
        internal void Screened(DeclaredProductOutcome outcome)
        {
            switch (outcome)
            {
                case DeclaredProductOutcome.DeclaredProductInstalled:
                    _declaredProductInstalled++;
                    break;
                case DeclaredProductOutcome.Unestablished:
                    _declaredProductUnestablished++;
                    break;
            }
        }

        internal WithholdingSplit Taken() => new(
            _identityUnestablished,
            _wholesale,
            _declaredProductInstalled,
            _declaredProductUnestablished,
            _screenUnanswered);
    }

    /// <summary>
    /// Moves out of <paramref name="candidates"/> and into
    /// <paramref name="withheld"/> every installation package whose own declared
    /// product Windows still holds a record of, and every one this pass could not
    /// settle. Both lists keep walk order.
    ///
    /// THE THIRD SOURCE, AND IT IS THE ONLY ONE THAT STARTS AT THE FILE. The two
    /// comparisons above it start at a registration and work towards a file, and
    /// both of them read the same recorded LocalPackage value, so a product whose
    /// records hold no value to read has nothing for either to find and nothing
    /// records the gap. See <see cref="IDeclaredProductCheck"/> for the mechanism
    /// in full; what it means here is that a product package can be walked,
    /// matched against nothing and offered while its product is installed.
    ///
    /// IT CAN ONLY EVER SUBTRACT FROM THE OFFER. Nothing it returns adds a file,
    /// clears a withholding made anywhere else, or reaches a patch at all. So a
    /// scan with no screen injected offers exactly what it offered before the
    /// screen existed, and a fault inside the screen costs offers rather than
    /// files.
    ///
    /// A WITHHELD CANDIDATE CARRIES NO CAUSE AND MUST NOT ACQUIRE ONE. It joins a
    /// list that already holds files kept back for a different reason entirely,
    /// and the surfaces that read that list say only that the app left these
    /// alone, which is true of both. Any sentence naming a cause over the whole
    /// list would be false of one half or the other, and the two inabilities
    /// behind an unestablished verdict have no honest superordinate between them
    /// either.
    /// </summary>
    private void WithholdCandidatesTheirOwnProductStillClaims(
        List<OrphanedFile> candidates,
        List<OrphanedFile> withheld,
        WithholdingSplitTally withheldBy,
        CancellationToken cancellationToken,
        Action<Exception, string>? recordRefusal = null)
    {
        if (_declaredProducts is null || candidates.Count == 0) return;

        var outcomes = _declaredProducts.Screen(candidates, cancellationToken, recordRefusal);

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
                withheldBy.Screened(outcomes[i]);
            }
            else survivors.Add(candidates[i]);
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
    /// correlation counts. <c>missingInFolder</c> is the third and this note left it
    /// out: it is the other term in the proportional clause that throws
    /// <c>Error_ScanCorrelationFailed</c>, so an audit of what that gate rests on has
    /// to be able to reach it from the predicate the gate is built on. No individual
    /// file's fate turns on any of the three, which is what "decides nothing about any
    /// file" was reaching for and is worth keeping in those narrower words.
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
    /// </summary>
    private List<WalkedFile> MaterialiseInstallerFiles(string folder, CancellationToken cancellationToken)
    {
        var list = new List<WalkedFile>();
        foreach (var file in GetInstallerFiles(folder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            list.Add(file);
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
    /// The three things the enumeration options used to say, said here instead,
    /// because the entry's own metadata is wanted and only the
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
