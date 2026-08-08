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
    private readonly IIdentityVeto _identityVeto;
    private readonly IShortNameCreationProbe? _shortNames;
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
        IIdentityVeto identityVeto, IShortNameCreationProbe shortNames)
        : this(queryService, fileSystem, identityVeto, shortNames, null, null) { }

    /// <summary>
    /// Test constructor. Injects a filesystem and nothing else, for the tests
    /// whose subject is the walk itself.
    /// </summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem)
        : this(queryService, fileSystem, PermissiveIdentityVeto.Instance, null, null, null) { }

    /// <summary>Test constructor. Injects a fake file list.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles)
        : this(queryService, new FileSystem(), PermissiveIdentityVeto.Instance, null, overrideFiles, null) { }

    /// <summary>Test constructor. Points enumeration at a real directory.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles, string? installerFolderOverride)
        : this(queryService, new FileSystem(), PermissiveIdentityVeto.Instance, null, overrideFiles, installerFolderOverride) { }

    /// <summary>
    /// Test constructor. Injects an <see cref="IFileSystem"/> so the
    /// scan-against-registered-set logic can be verified without
    /// touching <c>C:\Windows\Installer</c> on the host machine.
    /// </summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem,
        IEnumerable<string>? overrideFiles, string? installerFolderOverride)
        : this(queryService, fileSystem, PermissiveIdentityVeto.Instance, null, overrideFiles, installerFolderOverride) { }

    /// <summary>
    /// Test constructor carrying the identity veto as well, for the tests whose
    /// subject IS the veto rather than the path classification.
    /// </summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem,
        IIdentityVeto identityVeto, IEnumerable<string>? overrideFiles, string? installerFolderOverride)
        : this(queryService, fileSystem, identityVeto, null, overrideFiles, installerFolderOverride) { }

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
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem,
        IIdentityVeto identityVeto, IShortNameCreationProbe? shortNames,
        IEnumerable<string>? overrideFiles, string? installerFolderOverride)
    {
        _queryService = queryService;
        _fs = fileSystem;
        _identityVeto = identityVeto;
        _shortNames = shortNames;
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

        // Candidates the PATH comparison did not find a claim on, held here
        // rather than offered, because the path comparison no longer decides what
        // is offered: it decides what is CONSIDERED, and the identity pass below
        // decides the rest. The list is kept in walk order and the survivors are
        // appended to the offer in that order, so what the user sees is ordered
        // exactly as it was before the pass existed.
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

        long stillUsedBytes = 0;
        int refusedCandidates = 0;
        int withheld = 0;
        int missingNonRemovable = 0;
        int missingRemovable = 0;
        int nonRemovablePresent = 0;
        int removablePresent = 0;
        var sizedPackages = new List<RegisteredPackage>(registered.Count);

        // Declared out here because the two sanity gates below the loops read its
        // counts. An empty pass is the honest starting value: a scan that leaves
        // before the pass runs has kept nothing back by identity, and reporting
        // that it had would put a cause in front of the user that never occurred.
        var screened = new IdentityPassResult(Array.Empty<CandidateIdentityOutcome>(), 0, 0);

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

        // The identity pass. Nothing above it established that a candidate
        // belongs to nothing; it established that no registration names its path,
        // which is a different sentence and is the one that goes quiet when a
        // location is spelled a way the walk does not produce. Here each
        // candidate is asked what it is and Windows is asked about that, and only
        // a candidate every source answered for, with none of them claiming it,
        // is offered.
        //
        // It runs on the orphan branch and on nothing else. A superseded or
        // obsoleted patch is offered BECAUSE Windows positively said the patch is
        // superseded and no longer uninstallable, so it is a file Windows knows
        // by construction; putting it through a check whose keeping condition is
        // "Windows knows this identity" would withhold that whole class on every
        // machine, for ever, and would be the check misreading its own question.
        screened = _identityVeto.Screen(
            unclaimedByPath.ConvertAll(f => new IdentityCandidate(f.FullPath, f.IsPatch)),
            progress,
            cancellationToken);

        for (var i = 0; i < unclaimedByPath.Count; i++)
        {
            if (screened.Outcomes[i] == CandidateIdentityOutcome.Unclaimed)
                removable.Add(unclaimedByPath[i]);
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

            sizedPackages.Add(pkg with { FileSizeBytes = size, FileExists = exists });

            // Non-removable + missing is the load-bearing banner signal:
            // Windows still claims the file but it is gone from disk, so
            // a future install / uninstall / patch will fail. Removable
            // + missing is benign (Windows considers the patch already
            // removed; the file having gone is the expected end state)
            // and counts separately so the banner does not fire on it.
            // A withheld row is removable-in-fact on a scan that could not
            // prove it, so it takes the benign side too: only this scan's
            // verdict was withheld, and the API's own reading of the file
            // did not change.
            if (pkg.IsRemovable)
            {
                // Containment guard at candidate creation. Unlike the orphan
                // walk, a superseded/obsoleted candidate's path comes from an
                // API or registry LocalPackage value, which a corrupt
                // registration could point anywhere on disk. Drop one that does
                // not resolve to a file directly in the cache root (or is a
                // reparse point) rather than offer it; a genuine cache patch
                // always passes, its LocalPackage naming a file at that root.
                // Below the root is out of scope for the same reason the walk
                // never descends. An unproven answer drops it too, logged under
                // words that do not claim more than the check showed.
                var patchSafety = exists
                    ? CandidateGuard.CheckSafeToRemove(pkg.LocalPackagePath, cacheRoot)
                    : CandidateGuard.RemovalSafety.Refused;
                if (exists && patchSafety == CandidateGuard.RemovalSafety.Safe)
                {
                    var ext = _fs.Path.GetExtension(pkg.LocalPackagePath);
                    // PatchState 2 = superseded by a newer patch.
                    // PatchState 4 = obsoleted (publisher-withdrawn);
                    // distinct API state, distinct Reason label, same
                    // user-visible outcome (the patch is removable).
                    removablePresent++;
                    var isObsoleted = pkg.PatchState == 4;
                    var reason = isObsoleted
                        ? Strings.Reason_Obsoleted
                        : Strings.Reason_Superseded;
                    removable.Add(new OrphanedFile(
                        FullPath: pkg.LocalPackagePath,
                        SizeBytes: size,
                        IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                        IsRemovablePatch: true,
                        IsObsoleted: isObsoleted,
                        Reason: reason));
                }
                else if (exists)
                {
                    // In-bounds check failed on an existing removable file: drop
                    // it (do not offer, do not count as missing) and log.
                    refusedCandidates++;
                    refusalLog.Record(new InvalidOperationException(
                        patchSafety == CandidateGuard.RemovalSafety.Refused
                            ? $"Removable-patch candidate refused (not directly in the Installer cache, or a reparse point): {pkg.LocalPackagePath}"
                            : $"Removable-patch candidate not offered (its symlink status or location could not be read): {pkg.LocalPackagePath}"),
                        cause: $"removable-patch/{patchSafety}");
                }
                else
                {
                    missingRemovable++;
                }
            }
            else if (exists)
            {
                stillUsedBytes += size;
                nonRemovablePresent++;
                // Counted only where the file is on disk, because the count is
                // what the withholding COST this run: a withheld row whose file
                // is already gone had nothing to offer either way.
                if (pkg.RemovableWithheld) withheld++;
            }
            else if (pkg.RemovableWithheld)
            {
                missingRemovable++;
            }
            else
            {
                missingNonRemovable++;
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
        // BOTH GATES BELOW ARE MEASURED BEFORE THE IDENTITY PASS, and that is not
        // a detail. Each of them asks whether the COMPARISON worked, and reads an
        // empty offer as its evidence that it did not. A scan whose comparison
        // worked perfectly and whose identity pass then kept every candidate back
        // also has an empty offer, and refusing that scan would turn the safest
        // possible outcome into an error. This count is what the offer was before
        // the pass ran, so both gates go on answering the question they were
        // written to answer.
        var candidatesBeforeIdentity = unclaimedByPath.Count + removablePresent;

        if (!cacheRoot.Proven && refusedCandidates > 0 && candidatesBeforeIdentity == 0)
            throw new LocalisedInvalidOperationException(Strings.Error_ScanCacheRootUnresolved);

        var stillUsed = sizedPackages.Where(p => !p.IsRemovable).ToList().AsReadOnly();

        // Correlation sanity gate. On any real machine some registered path
        // resolves to a file that is actually there. If next to none do, yet the
        // walk still yielded files to offer for removal, then what Windows says
        // it has and what the folder holds have not correlated, and no healthy
        // machine looks like that.
        //
        // Present means File.Exists against the registered path, which for a
        // superseded row also has to pass the containment guard to be counted
        // (see the removable branch above). Both are normalised before they are
        // claimed, in InstallerQueryService's NormaliseLocalPackagePath.
        //
        // A tool that genuinely wiped the cache would leave no files to be
        // orphans, so removable.Count > 0 rules that benign case out. Refuse the
        // scan rather than offer the whole cache for deletion on a broken
        // correlation.
        //
        // A survivor or two must not disarm it, which testing for a total
        // collapse did: a mismatch that spares one path in two hundred is the
        // same fault as one that spares none.
        //
        // Every registered file found on disk counts as one, superseded ones
        // included. What the gate is asking is whether any registered path
        // resolved to a real file, and a superseded patch's does exactly as much
        // as a needed package's; leaving those out would refuse a machine that
        // had ten of them sitting in the folder about to be offered.
        //
        // Two rather than a round number, because the absolute bound answers the
        // finding and no more; machines with most of their cache missing are
        // real, another tool having emptied the folder being exactly what the
        // missing-from-disk banner is for.
        // Measured against the result logs rather than judged: of the 92 runs
        // that could reach this gate at all, not one would have been refused by
        // these bounds, taking each run at the worst reading its figures allow.
        //
        // The proportional clause is 19P < M, with P floored at one before it is
        // applied. Unfloored it is 0 < M at P = 0, so one missing row refused the
        // whole scan there: a machine with a single registered package whose file
        // has gone, which is what the missing-from-disk banner exists to report
        // and not the fault this gate was written for. Floored, a machine with no
        // survivor has to show the same twenty missing rows a machine with one
        // survivor already had to, and P = 1 and P = 2 are arithmetically
        // untouched, which is why the measurement above stands as it was taken.
        // Its denominator excludes the superseded rows whose file Windows has
        // already removed, so it is not a fifth of the registrations, and the
        // tests pin both sides at each P the absolute bound admits.
        var presentRegistered = nonRemovablePresent + removablePresent;
        var survivorsForBound = Math.Max(presentRegistered, 1);
        if (presentRegistered <= 2
            && survivorsForBound * 20 < survivorsForBound + missingNonRemovable
            && candidatesBeforeIdentity > 0)
            throw new LocalisedInvalidOperationException(Strings.Error_ScanCorrelationFailed);

        progress?.Report(new ScanProgressUpdate(string.Format(Strings.Status_FoundUnused,
            removable.Count, DisplayHelpers.PluraliseFile(removable.Count))));
        // THE THREE IDENTITY COUNTS ARE READ BY NO HOST, AND THAT IS THE DECISION
        // RATHER THAN AN OMISSION. The scan says nothing about a file it kept
        // back, because the app exists to list the files it is certain about and
        // does not claim to find them all: two of the three causes are the
        // ordinary case of declining to be sure about one file, and a line saying
        // there may be three more tells somebody something they can neither act on
        // nor check.
        //
        // The sibling notice is not a precedent for one here, though it looks like
        // the same case. Summary.RecordsNotMatched fires when a whole CLASS is
        // withheld because the records could not be matched up, which reports that
        // a scan ran worse than usual and that running it again may genuinely fix
        // it. Neither is true of these.
        //
        // What they are for is the question nothing else can answer: whether any
        // of this fires on a machine that is not the one it was all measured on.
        // They are exact per-file counts of exactly that, and are named inputs to
        // the outstanding reports schema. A schema designed without them is a
        // departure somebody has to argue for.
        return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes, missingNonRemovable, missingRemovable,
            query.UnaccountedProductCount, withheld,
            screened.ClaimedCount, screened.IdentityUnreadableCount, screened.RecordsUnaskableCount,
            query.Census,
            // Read after the classification is settled, so a probe that threw
            // could not cost anybody a scan; it does not throw, and the ordering
            // is the guarantee rather than the interface's promise.
            _shortNames?.Read() ?? ShortNameCreationLabels.Unreadable);
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
