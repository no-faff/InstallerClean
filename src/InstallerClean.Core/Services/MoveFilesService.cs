using System.IO.Abstractions;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

public sealed class MoveFilesService : IMoveFilesService
{
    private readonly IFileSystem _fs;
    private readonly IMutexProbe _mutex;

    /// <summary>
    /// Re-reads the batch's patch claims once the installer mutex is held. Held
    /// by the service rather than called by the caller because the hold is taken
    /// here: a check that has to happen inside it cannot live outside it.
    /// </summary>
    private readonly IRemovableReverifier _reverifier;

    /// <summary>
    /// Test-only real-folder override for the containment guard's cache root
    /// (null in production, which uses the real <c>C:\Windows\Installer</c>).
    /// Lets the real-filesystem integration tests treat a %TEMP% sandbox as the
    /// cache so a legitimately in-bounds source is not refused, without ever
    /// touching the real cache. Mirrors <c>FileSystemScanService</c>'s own
    /// installer-folder override.
    /// </summary>
    private readonly string? _installerFolderOverride;

    /// <summary>
    /// Constructor. The DI container injects the registered
    /// <see cref="IFileSystem"/> and <see cref="IMutexProbe"/> singletons in
    /// production; the mutex is held for the batch so a msiexec starting
    /// mid-move waits instead of racing the cache.
    /// </summary>
    public MoveFilesService(IFileSystem fileSystem, IMutexProbe mutex, IRemovableReverifier reverifier)
        : this(fileSystem, mutex, null, reverifier) { }

    /// <summary>Test constructor. No mutex hold and no under-lease re-read (both are exercised via the seam constructor below).</summary>
    internal MoveFilesService(IFileSystem fileSystem)
        : this(fileSystem, NullMutexProbe.Instance, null, NullRemovableReverifier.Instance) { }

    /// <summary>Test constructor. Points the source containment guard at a real sandbox folder; no mutex hold.</summary>
    internal MoveFilesService(IFileSystem fileSystem, string? installerFolderOverride)
        : this(fileSystem, NullMutexProbe.Instance, installerFolderOverride, NullRemovableReverifier.Instance) { }

    /// <summary>Seam constructor: an injected <see cref="IMutexProbe"/> (real or fake) plus the sandbox override.</summary>
    internal MoveFilesService(IFileSystem fileSystem, IMutexProbe mutex, string? installerFolderOverride,
        IRemovableReverifier? reverifier = null)
    {
        _fs = fileSystem;
        _mutex = mutex;
        _installerFolderOverride = installerFolderOverride;
        _reverifier = reverifier ?? NullRemovableReverifier.Instance;
    }

    public Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        UnderLeaseClaims? underLeaseClaims = null)
    {
        return Task.Run(() =>
        {
            // The three destination gates are inside the worker, not on the
            // caller's thread, which for the GUI is the dispatcher: two of them
            // resolve a user-chosen path through CreateFile and
            // GetFinalPathNameByHandle, twice over, and a mapped drive or a UNC
            // share that has gone away stalls each one for the SMB timeout with
            // the operating overlay already up and its Cancel button
            // unpressable.

            // Reject relative destinations: Path.GetFullPath would otherwise
            // resolve them against the process CWD, and the CLI host's CWD
            // is whatever the caller invoked it from.
            if (!Path.IsPathFullyQualified(destinationFolder))
                throw new LocalisedInvalidOperationException(
                    string.Format(Strings.Error_DestinationNotFullyQualified, destinationFolder));

            // Destination must not resolve inside C:\Windows\Installer;
            // ResolveFinalPath expands junctions so a reparse-point
            // destination cannot smuggle the batch into the cache folder.
            if (InstallerCacheHelpers.IsInstallerFolderOrChild(destinationFolder))
                throw new LocalisedInvalidOperationException(
                    string.Format(Strings.Error_MoveIntoInstaller, destinationFolder));

            // System-folder gate at the service boundary, not just at the
            // call sites: %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)%
            // and %ProgramData% sit on the Win32 DLL search path and the
            // SxS resolution path. Anchoring the check inside MoveFilesAsync
            // means any future caller (integration test, new automation
            // entry point) that bypasses the GUI/CLI validation block still
            // hits the same gate.
            if (InstallerCacheHelpers.IsSystemFolderOrChild(destinationFolder))
                throw new LocalisedInvalidOperationException(
                    string.Format(Strings.Error_DestinationInSystemFolder, destinationFolder));

            // Hold Global\_MSIExecute for the batch on this worker thread so a
            // msiexec starting mid-move waits on the mutex instead of racing the
            // cache. Acquired here (not on the dispatcher) and released in the
            // finally on the SAME thread (Win32 owner-thread rule); the body is
            // synchronous, so no await hops threads between acquire and release.
            //
            // Neither way of missing the hold proceeds, and the two are reported
            // separately because the caller can account for only one of them. Held
            // by a live transaction => the pending-reboot gate the caller re-runs
            // meets the same mutex and paints its banner, which says an install is
            // in progress, which it is. Refused with nothing shown to be holding it
            // (a DACL on the object, or any other non-fatal failure) => that gate
            // is no account of the condition at all, whichever way it answers.
            // IsHeld asks through a different call requesting different rights, so
            // it can come back clean, leaving a refusal with nothing on screen
            // explaining it; and on a DACL it returns held (its own catch says so),
            // which would paint a banner asserting an install nothing has shown. So
            // this result carries its own sentence rather than deferring to the
            // gate. Holding the mutex closes only the sub-millisecond race after
            // the host-side gate re-check has passed.
            //
            // Refusing the second case rather than running on unheld, and the three
            // things that decide it. A false there does not mean nothing is
            // installing, it means this process could not find out, which is what
            // the flag is named for and what the branch reading it must not forget.
            // The object is not permanent, so being refused it is evidence rather
            // than noise:
            // Windows Installer creates _MSIExecute when an install begins and drops
            // it when the install ends, so between installs the create-or-open path
            // below makes the object and succeeds, and the only object that can
            // refuse this process is one something else has already made. That is
            // the documented lifetime plus MutexProbe's account of the
            // create-or-open; it has not been measured on a live machine. And a
            // move's exposure to the hazard is the delete's: a moved file is as
            // absent from the cache as a deleted one, so a transaction that starts
            // mid-batch fails to find it either way. Only the recovery differs, and
            // running on here bought a recovery property at the price of a safety
            // one.
            //
            // The counter-argument this rejects: MutexProbe's DACL comment reasons
            // that the plausible cause of a refusal is a non-elevated per-user
            // install, which does not write the machine cache, so the hazard would
            // not apply. It does not survive this branch being unable to see which
            // cause it has. The same null-with-nothing-shown answer comes back
            // from that probe's catch-all for any other non-fatal failure, so acting
            // on the benign reading is choosing a behaviour for a mixed set on the
            // strength of one member of it.
            //
            // What the hold costs, so nobody widens it and nobody removes it:
            // _MSIExecute is the machine-wide Windows Installer serialisation
            // mutex, so for as long as this batch runs, every installer on the
            // machine that wants it waits or fails with 1618. A single file
            // operation that hangs (a stalled network destination) therefore
            // holds the machine's installer lock until this process is killed.
            // That is accepted because the alternative is msiexec writing the
            // cache in the middle of a move, which costs a needed file rather
            // than a wait.
            //
            // Two more things inside the hold are unbounded by the batch, and
            // neither is a file operation. The progress callback below hands
            // control to a consumer that can run for as long as it likes, which
            // is the property the destination re-check is ordered around; in the
            // command-line host that consumer is a console write, and a console
            // in QuickEdit selection blocks one until the operator clears it. The
            // prune past the loop is a full recursive enumeration of a folder this
            // project has measured at 6.4 million entries, materialised by
            // OrderByDescending, and its duration has nothing to do with how many
            // files the batch held. Both predate the range that wrote this block.
            // Whether either belongs inside the hold is an open behaviour question
            // and is not settled by anything here.
            //
            // Delete acquires immediately, having nothing to set up first, and
            // this cannot: everything between here and the loop is the
            // destination work, and running it before the acquire would create
            // the destination folder even on the runs the checks below refuse. A
            // refusal that has touched nothing is worth more than a shorter hold,
            // and both refusals below are reached before any of that work.
            var lease = _mutex.TryAcquire(PendingRebootService.MsiExecuteMutexName, out var shownHeldByAnother);
            if (lease is null && shownHeldByAnother)
                return new MoveResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true);
            if (lease is null)
            {
                // Recorded as well as refused. The refusal is what the user is
                // told; the crash log is the only place the machine's own
                // condition is written down, and an operator seeing this on every
                // run is looking at a DACL on the object rather than at a passing
                // race. Once per batch, so it costs nothing at any file count.
                Helpers.CrashLog.TryWrite(new InvalidOperationException(
                    "Move refused: the Windows Installer mutex could not be acquired and nothing could be shown to be holding it."));
                return new MoveResult(0, Array.Empty<FileOperationError>(), InstallerLockUnavailable: true);
            }

            try
            {
            // The act-time re-read, before any destination work so a batch it
            // empties leaves no folder behind. It is HERE and not at the caller
            // because the caller's full re-verify runs before this method is
            // entered: the mutex is taken after it, so the batch acts on an
            // answer read outside the hold, across the whole of that
            // enumeration's duration rather than the instant after it. Windows
            // writes a patch's registration while it processes the install
            // script, and _MSIExecute is documented as set only while the
            // execute-sequence tables are being processed, so the write falls
            // inside the phase this mutex covers.
            //
            // What is NOT established, and must not be written here as though it
            // were: whether an info API can return a registration its own
            // transaction has written and not yet committed. That answer decides
            // how WIDE the window this closes really was; it does not decide
            // whether the re-read is worth taking, which is why this was built
            // without it.
            //
            // Only the claims, never the enumeration. Re-walking the whole
            // registered set here would put an API enumeration inside a
            // machine-wide installer lock on every run, which the note above asks
            // in as many words that nobody do, and it would buy little: with the
            // mutex held no orphan can acquire a NEW claim, because acquiring one
            // takes a Windows Installer transaction and a transaction takes this
            // mutex. What can still have moved is a verdict on a claim that
            // already existed, and those carry an identity to ask about.
            //
            // Synchronous on the acquiring thread by necessity, not by taste: the
            // lease is released by the thread that took it, so nothing between the
            // acquire and the release may await.
            var recheck = _reverifier.RecheckUnderLease(
                underLeaseClaims ?? UnderLeaseClaims.None);
            var heldBack = recheck.HeldBack;
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            if (heldBack.Count > 0)
            {
                var reclaimed = new HashSet<string>(heldBack, StringComparer.OrdinalIgnoreCase);
                pathList = pathList.Where(p => !reclaimed.Contains(p)).ToList();
                // Only when the re-read is what emptied it. An empty batch handed
                // in still falls through and creates the destination, which the
                // command line relies on not changing.
                if (pathList.Count == 0)
                    return new MoveResult(0, Array.Empty<FileOperationError>(), HeldBack: heldBack, HeldBackReasons: recheck.Reasons);
            }

            CreateDestinationFolder(destinationFolder);

            // Re-check after CreateDirectory closes the TOCTOU window
            // where a junction could be swapped into the leaf.
            if (InstallerCacheHelpers.IsInstallerFolderOrChild(destinationFolder))
                throw new LocalisedInvalidOperationException(
                    string.Format(Strings.Error_MoveIntoInstaller, destinationFolder));
            if (InstallerCacheHelpers.IsSystemFolderOrChild(destinationFolder))
                throw new LocalisedInvalidOperationException(
                    string.Format(Strings.Error_DestinationInSystemFolder, destinationFolder));

            // Capture the canonical destination once, then re-resolve
            // per iteration. The per-iteration check catches a junction
            // swap on the destination's parent folder during the loop:
            // without it, a relabelled leaf folder would silently route
            // the remaining files into the junction's target. The
            // pre-loop IsInstallerFolderOrChild check covers the
            // CreateDirectory point; the loop-body check covers each
            // per-file move.
            // The bool is kept, not discarded: it says whether the kernel really
            // expanded this path, and the per-iteration check below compares
            // proof states as well as strings.
            var canonicalProven = InstallerCacheHelpers.TryResolveFinalPath(
                destinationFolder, out var canonicalRaw);
            var canonicalDestination = canonicalRaw.TrimEnd(Path.DirectorySeparatorChar);

            ProbeDestinationWriteable(destinationFolder);

            int moved = 0;
            var errors = new List<FileOperationError>();
            var failureLog = new PerItemFailureLog("Move",
                "The per-file list is on the completion screen.");
            // Resolved once for the batch; the guard resolves each SOURCE per
            // file against it (see InstallerCacheRoot). Separate from
            // canonicalDestination above, which guards the other end and is
            // re-resolved per iteration for a reason of its own.
            var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);
            var total = pathList.Count;
            bool cancelled = false;
            MoveAbortReason? abortReason = null;

            try
            {
            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = pathList[i];

                try
                {
                    // First statement inside the per-file try, and both halves of
                    // that are load-bearing. Before the skip checks below, so the
                    // visible counter advances on missing and reparse-point
                    // entries instead of jumping over them. Inside the try, so a
                    // progress consumer that throws costs this one file a
                    // categorised error instead of costing the batch: from outside
                    // the try the throw leaves the loop altogether, and the files
                    // already moved are never reported, the result and the failure
                    // log's closing entry both being built past the loop.
                    progress?.Report(new OperationProgress(i + 1, total, _fs.Path.GetFileName(sourcePath)));

                    // Re-resolve and compare to the canonical capture, AFTER the
                    // report and not before it. The order is what the guard rests
                    // on: this is a time-of-check-to-time-of-use guard, and the
                    // report hands control to a consumer that can run for as long as
                    // it likes, so a check taken before it leaves that consumer's
                    // whole duration between the check and the move. On the last
                    // file of a batch there is no next iteration to catch the swap
                    // at all.
                    // Every statement between here and File.Move is this service's
                    // own, which is the claim that holds and the one worth having:
                    // no consumer gets control again before the move. It is NOT the
                    // last position the check could occupy. The source Exists test,
                    // the reparse read, the containment guard's real-filesystem
                    // resolution and GetUniqueDestPath all sit after it, and the
                    // last of those runs a collision loop bounded at 10,000 probes,
                    // which on a network destination is 10,000 round trips inside
                    // the machine-wide mutex. Bounded, and not small.
                    //
                    // The ordering is not the reach, and the reach has a hole worth
                    // naming rather than a guarantee. A resolve that DEGRADED is
                    // exactly a path whose reparse points went UNexpanded, which is
                    // the one thing a containment check exists to see through, so a
                    // re-resolve that has lost the proof the capture had counts as a
                    // change rather than being compared as a string. What it still
                    // cannot see is a leaf missing at this instant: resolution then
                    // runs on the nearest existing ancestor and reattaches the leaf
                    // name as text, so a leaf deleted and replaced inside the window
                    // compares equal. That hole is open, and it is named here rather
                    // than described as closed.
                    var resolveProven = InstallerCacheHelpers.TryResolveFinalPath(
                        destinationFolder, out var currentRaw);
                    var currentResolved = currentRaw.TrimEnd(Path.DirectorySeparatorChar);
                    abortReason = ClassifyAbort(
                        canonicalProven, resolveProven, canonicalDestination, currentResolved);
                    if (abortReason is not null)
                    {
                        // The only record of which condition fired. Nothing else
                        // keeps it: the host is told the batch stopped and where
                        // the files are, which is the same either way, so a report
                        // of one of these could not otherwise be told from a report
                        // of the other. Once per batch, on a path that ends it.
                        Helpers.CrashLog.TryWrite(new InvalidOperationException(
                            $"Move stopped by the destination guard ({abortReason}). Captured "
                            + $"'{canonicalDestination}' (resolved: {canonicalProven}); re-resolved "
                            + $"'{currentResolved}' (resolved: {resolveProven}). "
                            + $"{moved} of {total} files had moved."));

                        // Out through the one exit below rather than straight up the
                        // stack: everything a stopped batch still owes is under this
                        // frame. Files have already left C:\Windows\Installer, so the
                        // count, the size and the line telling the user they can put
                        // them back are all real, and the failure log's closing entry
                        // accounts for what the batch's failures cost it.
                        //
                        // A break is not an exception, so sitting inside the per-file
                        // try does not hand the abort to the catch arms below: it
                        // leaves the loop exactly as it did from outside them.
                        break;
                    }

                    if (!_fs.File.Exists(sourcePath))
                    {
                        errors.Add(new MissingSourceFile(sourcePath));
                        continue;
                    }

                    // Refuse a source that's a symlink or junction: a move can
                    // read through one and copy the target's contents into the
                    // user's folder, the receipts being with the check itself
                    // (StorageHelpers.CheckReparsePoint). Real-FS check
                    // (MockFileSystem cannot bypass). An attribute read that
                    // FAILS refuses the file too, but as UnknownError:
                    // SourceIsReparsePoint tells the
                    // user the file is a symlink, and a read that could not be
                    // made has not shown that.
                    var reparse = Helpers.StorageHelpers.CheckReparsePoint(sourcePath, out var reparseError);
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Yes)
                    {
                        errors.Add(new SourceIsReparsePoint(sourcePath));
                        continue;
                    }
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Unreadable)
                    {
                        failureLog.Record(reparseError!);
                        errors.Add(new UnknownError(sourcePath));
                        continue;
                    }

                    // Containment guard at the service boundary, matching the
                    // IsSystemFolderOrChild anchoring above: never move a source
                    // that does not resolve to a file directly in
                    // C:\Windows\Installer. The scan already filters candidates,
                    // but a corrupt LocalPackage value or a future caller could
                    // reach here with an out-of-bounds path; this is the choke
                    // point that refuses it. Reparse is handled just above, so a
                    // Refused here is an out-of-bounds path and
                    // CandidateOutsideCache says so; an Unproven one is a path
                    // that could not be resolved at all, which is the same
                    // refusal without the same claim.
                    // Inverted, so the move below is reached only by an answer that
                    // positively said Safe, and the reason is switched on inside.
                    // Two positive equality tests naming Refused and Unproven fell
                    // through to File.Move for anything they did not name, which
                    // made any RemovalSafety member added later a move out of the
                    // cache until somebody remembered to come back here. The enum
                    // grew once already, splitting one refusal into Refused and
                    // Unproven, so that is a thing that happens rather than a thing
                    // that might. Matches the Delete path's own guard site.
                    //
                    // The default arm files the same UnknownError as Unproven but
                    // will not borrow its sentence, because a member nobody has
                    // written yet has not been shown to be a path that could not be
                    // resolved, and CandidateGuard's contract is that a caller must
                    // not name a cause it has not shown.
                    var safety = CandidateGuard.CheckSafeToRemove(sourcePath, cacheRoot);
                    if (safety != CandidateGuard.RemovalSafety.Safe)
                    {
                        switch (safety)
                        {
                            case CandidateGuard.RemovalSafety.Refused:
                                errors.Add(new CandidateOutsideCache(sourcePath));
                                break;
                            case CandidateGuard.RemovalSafety.Unproven:
                                failureLog.Record(new InvalidOperationException(
                                    $"Move refused: {sourcePath} could not be resolved, so it could not be shown to be inside the Installer cache."));
                                errors.Add(new UnknownError(sourcePath));
                                break;
                            default:
                                failureLog.Record(new InvalidOperationException(
                                    $"Move refused: the containment guard answered {safety} for {sourcePath}, which this service has no handling for."));
                                errors.Add(new UnknownError(sourcePath));
                                break;
                        }
                        continue;
                    }

                    var fileName = _fs.Path.GetFileName(sourcePath);
                    var destPath = GetUniqueDestPath(destinationFolder, fileName);
                    _fs.File.Move(sourcePath, destPath);

                    // A move that returned without throwing has not necessarily
                    // happened: see ReconcileMove.
                    var halfMove = ReconcileMove(sourcePath, destPath, failureLog);
                    if (halfMove is null)
                        moved++;
                    else
                        errors.Add(halfMove);
                }
                // DestinationCollisionException alone is not logged: it is this
                // class's own control flow, thrown from one place, and its
                // error entry already states the whole cause. The rest are
                // framework exceptions whose detail exists nowhere else once
                // the category has been filed, so without this a move that
                // failed for an unforeseen reason leaves no trace at all.
                catch (DestinationCollisionException ex)
                {
                    errors.Add(new DestinationCollision(sourcePath, ex.FileName));
                }
                catch (Exception ex)
                {
                    failureLog.Record(ex);
                    errors.Add(Categorise(ex, sourcePath));
                }
            }
            }
            catch (OperationCanceledException)
            {
                // Return what moved before the cancel rather than throwing the
                // tally away. The per-file loop's ThrowIfCancellationRequested is
                // the only cancellation source and sits outside the inner
                // per-file catch, so only a real cancel lands here; a genuine
                // mid-batch error (a destination junction swap) still propagates.
                cancelled = true;
            }

            // Outside the cancel catch so a batch the user stopped still
            // accounts for what its failures cost the log.
            failureLog.WriteClosingEntry();

            // Pass CancellationToken.None: the prune is best-effort
            // post-operation cleanup. If the user pressed Cancel during
            // the prune (after all moves completed), propagating their
            // token would throw OperationCanceledException out of a
            // batch that actually succeeded - the caller would re-label
            // the run as "Move cancelled" even though every file moved.
            InstallerCacheHelpers.PruneEmptySubdirectories(_fs, CancellationToken.None);

            var result = new MoveResult(moved, errors.AsReadOnly(), cancelled, HeldBack: heldBack, HeldBackReasons: recheck.Reasons);

            // A guard that trips mid-flight throws, like this service's other
            // guards and unlike a cancel, which is the user's own choice. What it
            // must not do is take the batch's account of itself with it, which is
            // why the result is built first and travels on the exception.
            //
            // The message names the folder the CALLER asked for, because it asks
            // the reader to go and check that folder and that is the one they
            // configured. Where the files ended up is a different question and
            // travels separately.
            if (abortReason is not null)
                throw new MoveAbortedException(
                    string.Format(Strings.Error_DestinationChangedMidBatch, destinationFolder),
                    result, DisplayPath(canonicalDestination), abortReason.Value);

            return result;
            }
            finally
            {
                // Release on this same worker thread (Win32 owner-thread rule).
                // Non-null by the time this try is entered: both ways of failing
                // to acquire return above, so a batch that reaches here holds the
                // mutex and a batch that does not never started.
                lease.Dispose();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Whether the destination guard stops the batch, and on which of its two
    /// conditions, from the capture and the re-resolve. Null means carry on.
    ///
    /// The two answers exist even though the outcome is identical, and the reason
    /// is entirely about what may be SAID. OR them into one bool and the only
    /// account of a stopped batch is that its destination changed, which is false
    /// of a share that dropped or an ACL that closed: nothing replaced or
    /// redirected anything, and the folder is exactly where the user put it. One
    /// bool cannot answer WHY without answering it wrongly for half the batches
    /// that reach it.
    ///
    /// Separated out because the loop cannot be driven into the second condition
    /// by any test the suite can write: the resolve deliberately bypasses the
    /// injected <see cref="System.IO.Abstractions.IFileSystem"/> and asks the real
    /// kernel, and on a local disk the walk always finds an existing ancestor to
    /// open, so a real-filesystem test can produce a changed target and cannot
    /// produce a lost one. As a function of the four inputs it is pinned at every
    /// combination. What that leaves unpinned is which inputs the kernel really
    /// hands back on a dropped share, and no test here has ever answered that.
    /// </summary>
    internal static MoveAbortReason? ClassifyAbort(
        bool canonicalProven, bool resolveProven,
        string canonicalDestination, string currentResolved)
    {
        // Order matters and is not a precedence chain over one condition: a
        // degraded re-resolve returns a best-effort string that may or may not
        // compare equal, so asking the comparison first would report some lost
        // destinations as changed ones and the rest not at all.
        if (canonicalProven && !resolveProven) return MoveAbortReason.StoppedResolving;
        if (!currentResolved.Equals(canonicalDestination, StringComparison.OrdinalIgnoreCase))
            return MoveAbortReason.ResolvesElsewhere;
        return null;
    }

    /// <summary>
    /// A resolved folder path in the form a person should read it in.
    ///
    /// The guard trims the trailing separator so two resolutions of one folder
    /// compare equal whichever way the kernel spelled them, and that is right for
    /// a comparison and wrong for a drive root: it turns <c>D:\</c> into
    /// <c>D:</c>, which names the process's current directory on that drive
    /// rather than the root of it.
    /// </summary>
    private static string DisplayPath(string resolved) =>
        resolved.EndsWith(':') ? resolved + Path.DirectorySeparatorChar : resolved;

    /// <summary>
    /// Settles what actually happened to a file <c>File.Move</c> reported as
    /// moved, and returns null when the file really has gone or the error to
    /// file when it has not.
    ///
    /// A move that returns without throwing is not proof the source is gone.
    /// .NET calls MoveFileEx with MOVEFILE_COPY_ALLOWED (dotnet/runtime,
    /// Interop.MoveFileEx.cs), and Win32 documents that flag as simulating a
    /// cross-volume move with CopyFile plus DeleteFile, then says outright: "If
    /// the file is successfully copied to a different volume and the original
    /// file is unable to be deleted, the function succeeds leaving the source
    /// file intact." A read-only source is exactly that case, DeleteFile being
    /// documented to fail with ERROR_ACCESS_DENIED on one, and a file another
    /// program holds open without FILE_SHARE_DELETE is a second. Same-drive
    /// moves never reach this state: a rename either happens or throws.
    ///
    /// Checked after the fact rather than pre-empted, because pre-clearing the
    /// attribute would mutate sources that never needed it and would need a
    /// same-volume test that cannot be had: a mount point inside one drive
    /// letter falls back to copy-and-delete like any other volume boundary.
    ///
    /// Clearing the read-only bit is safe here for the reasons the delete path's
    /// own comment sets out: the same two gates passed, the same confirmed
    /// intent.
    /// </summary>
    private FileOperationError? ReconcileMove(string sourcePath, string destPath, PerItemFailureLog failureLog)
    {
        if (!_fs.File.Exists(sourcePath)) return null;

        var clearedReadOnly = false;
        try
        {
            clearedReadOnly = ClearReadOnly(sourcePath);
            _fs.File.Delete(sourcePath);
            return null;
        }
        catch (Exception ex)
        {
            // An antivirus quarantining the file it has just watched being
            // copied takes the source out between the Exists above and this
            // throw. Source gone with the copy in place is the end state a
            // completed move produces, so that is a success; discarding the
            // copy would leave the user with neither.
            if (!_fs.File.Exists(sourcePath)) return null;

            failureLog.Record(ex);
            RestoreReadOnly(sourcePath, clearedReadOnly, failureLog);
            DiscardDestinationCopy(destPath, failureLog);
            return Categorise(ex, sourcePath);
        }
    }

    /// <summary>
    /// Removes the copy a half-completed move left in the destination, so a
    /// file reported as failed is a file the user still has exactly one of.
    ///
    /// A failure here is the one state with nothing to say for itself: the
    /// error the caller files says the file was left in place, which is true,
    /// and does not mention the copy. It is recorded rather than surfaced
    /// because it needs a destination that accepts a create and refuses a
    /// delete, which the write probe has already had to pass.
    ///
    /// CopyFile carries the source's attributes across, so the copy of a
    /// read-only source is read-only and the attribute has to come off before
    /// the delete. Safe on this file above any other: this process wrote it
    /// seconds ago at a name it chose. Nothing puts it back on a failure here,
    /// unlike the source's, because the copy is not a file the user has: the
    /// record already says it exists in both places, and a read-only leftover is
    /// only harder for them to remove by hand.
    ///
    /// No check that what sits at destPath is still that file, deliberately:
    /// any identity check is itself racy and the window is milliseconds.
    /// </summary>
    private void DiscardDestinationCopy(string destPath, PerItemFailureLog failureLog)
    {
        try
        {
            ClearReadOnly(destPath);
            _fs.File.Delete(destPath);
        }
        catch (Exception ex)
        {
            failureLog.Record(new InvalidOperationException(
                $"A copy of {destPath} could not be removed after the move failed to remove its source, "
                + "so that file now exists in both places.", ex));
        }
    }

    /// <summary>
    /// Clears the read-only attribute, reporting whether there was one to clear
    /// so a caller that then fails can put it back.
    ///
    /// Throws are the caller's: this is one step of an operation that fails
    /// closed as a whole.
    /// </summary>
    private bool ClearReadOnly(string filePath)
    {
        var attributes = _fs.File.GetAttributes(filePath);
        if (!attributes.HasFlag(FileAttributes.ReadOnly)) return false;
        _fs.File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
        return true;
    }

    /// <summary>
    /// Puts back a read-only attribute cleared from a source the delete then
    /// refused. What this service says about a file it reports as failed is that
    /// the user still has exactly one of it and exactly as it was, and a source
    /// left writable is one step of that operation committed after the operation
    /// gave up.
    ///
    /// Guarded where <see cref="ClearReadOnly"/> is not, because this runs inside
    /// the failure arm: a throw here would replace the error the caller is about
    /// to file with one about the tidying up after it.
    /// </summary>
    private void RestoreReadOnly(string filePath, bool wasCleared, PerItemFailureLog failureLog)
    {
        if (!wasCleared) return;
        try
        {
            _fs.File.SetAttributes(filePath,
                _fs.File.GetAttributes(filePath) | FileAttributes.ReadOnly);
        }
        catch (Exception ex)
        {
            failureLog.Record(new InvalidOperationException(
                $"The read-only attribute cleared from {filePath} could not be put back after the "
                + "move failed to remove it, so that file is no longer read-only.", ex));
        }
    }

    /// <summary>
    /// Shared so the loop's own failures and the reconciliation's are named the
    /// same way.
    ///
    /// ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION as HRESULTs: another
    /// program holds the file open, which is the one IO failure here with a
    /// cause the user can act on and the one that is not a fault. The Delete
    /// path discriminates the same two codes off the HRESULT of the IOException
    /// its own File.Delete raises, so both halves of the app name the same
    /// condition the same way.
    /// </summary>
    private static FileOperationError Categorise(Exception ex, string sourcePath) => ex switch
    {
        UnauthorizedAccessException => new AccessDenied(sourcePath),
        IOException io when io.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021)
            => new FileInUse(sourcePath),
        IOException => new IOFailure(sourcePath),
        _ => new UnknownError(sourcePath),
    };

    /// <summary>
    /// Wraps <c>Directory.CreateDirectory</c> so framework-thrown
    /// UnauthorizedAccessException and IOException are remapped to the
    /// same UnauthorizedAccessException-with-localised-message that
    /// <see cref="ProbeDestinationWriteable"/> produces. The caller's
    /// catch block sees one consistent contract; the framework's
    /// path-bearing message is preserved on InnerException for crash
    /// log consumers but never reaches the displayed UI.
    /// </summary>
    private void CreateDestinationFolder(string folder)
    {
        try
        {
            _fs.Directory.CreateDirectory(folder);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new LocalisedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, folder), ex);
        }
        catch (IOException ex)
        {
            throw new LocalisedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, folder), ex);
        }
    }

    private void ProbeDestinationWriteable(string folder)
    {
        // Fail fast with one clean error rather than collecting per-file
        // errors for every source when the destination is read-only.
        //
        // On a destination whose ACL grants create but not delete, the write
        // lands and the delete is what throws, so the probe file stays behind.
        // Nothing here can clear it: a second delete is the operation that was
        // just refused, and any probe that proves a folder accepts a file has
        // to create one. The move is refused, which is the outcome that
        // matters, and the residue is one empty random-named file on a
        // destination Windows will not let this process tidy.
        var probe = _fs.Path.Combine(folder, _fs.Path.GetRandomFileName());
        try
        {
            _fs.File.WriteAllBytes(probe, Array.Empty<byte>());
            _fs.File.Delete(probe);
        }
        catch (Exception ex)
        {
            // ex.Message stays out of the thrown message (path-leak risk
            // under elevation); the inner exception preserves it for
            // crash-log consumers via .InnerException.
            throw new LocalisedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, folder), ex);
        }
    }

    // File.Move with overwrite=true follows a reparse point planted
    // at destPath during the unique-name race. The non-overwriting
    // form refuses existing targets, ending the race in a per-file
    // error rather than a symlink follow-through to a sensitive
    // location. Overwrite=true would require a reparse-point check on
    // destPath immediately before the move.
    private string GetUniqueDestPath(string folder, string fileName)
    {
        var candidate = _fs.Path.Combine(folder, fileName);
        if (!_fs.File.Exists(candidate)) return candidate;

        var nameWithout = _fs.Path.GetFileNameWithoutExtension(fileName);
        var ext = _fs.Path.GetExtension(fileName);

        for (int i = 1; i <= 10_000; i++)
        {
            candidate = _fs.Path.Combine(folder, $"{nameWithout} ({i}){ext}");
            if (!_fs.File.Exists(candidate)) return candidate;
        }

        throw new DestinationCollisionException(fileName);
    }

    /// <summary>
    /// Thrown by <see cref="GetUniqueDestPath"/> when 10,000 unique-
    /// suffix attempts all collide. The MoveFilesAsync loop catches
    /// it one frame up and folds it into the result as a
    /// <see cref="DestinationCollision"/> entry so the rest of the
    /// batch continues; nothing outside this class observes the
    /// exception type, so the sealed-private scope keeps it from
    /// leaking into the public surface.
    /// </summary>
    private sealed class DestinationCollisionException : Exception
    {
        public string FileName { get; }
        public DestinationCollisionException(string fileName) =>
            FileName = fileName;
    }
}
