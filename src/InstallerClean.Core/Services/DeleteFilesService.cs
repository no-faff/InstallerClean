using System.IO.Abstractions;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class DeleteFilesService : IDeleteFilesService
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
    /// (null in production). See the matching field on <c>MoveFilesService</c>:
    /// it lets the real-filesystem integration tests treat a %TEMP% sandbox as
    /// the cache without touching the real one, and does not let a MockFileSystem
    /// bypass the gate.
    /// </summary>
    private readonly string? _installerFolderOverride;

    /// <summary>
    /// Constructor. The DI container injects the registered
    /// <see cref="IFileSystem"/> and <see cref="IMutexProbe"/> singletons in
    /// production; the mutex is held for the batch so a msiexec starting
    /// mid-delete waits instead of racing the cache.
    /// </summary>
    public DeleteFilesService(IFileSystem fileSystem, IMutexProbe mutex, IRemovableReverifier reverifier)
        : this(fileSystem, mutex, null, reverifier) { }

    /// <summary>Test constructor. No mutex hold and no under-lease re-read (both are exercised via the seam constructor below).</summary>
    internal DeleteFilesService(IFileSystem fileSystem)
        : this(fileSystem, NullMutexProbe.Instance, null, NullRemovableReverifier.Instance) { }

    /// <summary>Seam constructor: an injected <see cref="IMutexProbe"/> (real or fake) plus the sandbox override.</summary>
    internal DeleteFilesService(IFileSystem fileSystem, IMutexProbe mutex, string? installerFolderOverride,
        IRemovableReverifier? reverifier = null)
    {
        _fs = fileSystem;
        _mutex = mutex;
        _installerFolderOverride = installerFolderOverride;
        _reverifier = reverifier ?? NullRemovableReverifier.Instance;
    }

    public Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        IReadOnlyList<PatchClaim>? patchClaims = null)
    {
        return Task.Run(() =>
        {
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;
            if (total == 0)
                return new DeleteResult(0, Array.Empty<FileOperationError>());

            cancellationToken.ThrowIfCancellationRequested();

            // Hold Global\_MSIExecute for the batch on this worker thread so a
            // msiexec starting mid-delete waits on the mutex instead of racing the
            // cache. Acquired here and released in the finally on the SAME thread
            // (Win32 owner-thread rule); the body is synchronous, so no await hops
            // threads between acquire and release.
            //
            // Neither way of missing the hold proceeds, and the two are reported
            // separately because the caller can only account for one of them. Held
            // by a live transaction => the pending-reboot gate the caller re-runs
            // meets the same mutex and paints its banner. Refused with nobody
            // holding it (a DACL on the object, or any other non-fatal failure) =>
            // that gate comes back clean, so the result has to carry its own
            // sentence or the user is refused with no reason given.
            //
            // Refusing the second case is younger than the hold itself, and what
            // changed the sum was the delete becoming permanent rather than
            // anything about the mutex. Running on unserialised used to risk, at
            // worst, a file that had just become needed going to the Recycle Bin,
            // where the user could fetch it back; there is no bin to fetch it from.
            // MoveFilesService refuses on the same answer, for reasons it states at
            // its own acquire: its exposure to the hazard is this one's, and only
            // the recovery differs.
            //
            // What the hold costs, so nobody widens it and nobody removes it:
            // _MSIExecute is the machine-wide Windows Installer serialisation
            // mutex, so for as long as this batch runs, every installer on the
            // machine that wants it waits or fails with 1618. That is accepted
            // because the alternative is msiexec writing the cache in the middle
            // of a delete, which costs a needed file rather than a wait.
            //
            // What it is NOT is bounded by the batch's file count, and the two
            // things that break that bound are both already inside it. The
            // progress callback hands control to a consumer that can run for as
            // long as it likes, which is the property the destination re-check in
            // MoveFilesService is ordered around; in this host's command-line
            // sibling that consumer is a console write, and a console in QuickEdit
            // selection blocks one until the operator clears it. The prune below
            // is a full recursive enumeration of a folder this project has
            // measured at 6.4 million entries, materialised by OrderByDescending,
            // and its duration has nothing to do with how many files the batch
            // held. Both predate the range that wrote this block. Whether either
            // belongs inside the hold is an open behaviour question and is not
            // settled by anything here.
            var lease = _mutex.TryAcquire(PendingRebootService.MsiExecuteMutexName, out var heldByAnother);
            if (lease is null && heldByAnother)
                return new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true);
            if (lease is null)
            {
                // Recorded as well as refused. The refusal is what the user is
                // told; the crash log is the only place the machine's own
                // condition is written down, and an operator seeing this on every
                // run is looking at a DACL on the object rather than at a passing
                // race. Once per batch, so it costs nothing at any file count.
                Helpers.CrashLog.TryWrite(new InvalidOperationException(
                    "Delete refused: the Windows Installer mutex could not be acquired and was not held by another process."));
                return new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerLockUnavailable: true);
            }

            try
            {
            // The act-time re-read, and the reason it is HERE and not at the
            // caller. The caller's full re-verify runs before this method is
            // entered, so the mutex is taken after it and the batch acts on an
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
            // machine-wide installer lock on every run, which the note above
            // asks in as many words that nobody do, and it would buy little: with
            // the mutex held no orphan can acquire a NEW claim, because acquiring
            // one takes a Windows Installer transaction and a transaction takes
            // this mutex. What can still have moved is a verdict on a claim that
            // already existed, and those carry an identity to ask about.
            //
            // Synchronous on the acquiring thread by necessity, not by taste: the
            // lease is released by the thread that took it, so nothing between
            // the acquire and the release may await.
            var recheck = _reverifier.RecheckUnderLease(
                patchClaims ?? Array.Empty<PatchClaim>());
            var heldBack = recheck.HeldBack;
            if (heldBack.Count > 0)
            {
                var reclaimed = new HashSet<string>(heldBack, StringComparer.OrdinalIgnoreCase);
                pathList = pathList.Where(p => !reclaimed.Contains(p)).ToList();
                total = pathList.Count;
                if (total == 0)
                    return new DeleteResult(0, Array.Empty<FileOperationError>(), HeldBack: heldBack, HeldBackReasons: recheck.Reasons);
            }

            int deleted = 0;
            var errors = new List<FileOperationError>();
            var failureLog = new PerItemFailureLog("Delete",
                "The per-file list is on the completion screen.");
            // Resolved once for the batch; the guard resolves each SOURCE per
            // file against it (see InstallerCacheRoot).
            var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);
            bool cancelled = false;

            try
            {
            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var filePath = pathList[i];

                try
                {
                    // First statement inside the per-file try, and both halves of
                    // that are load-bearing. Before the skip check below, so a
                    // missing file still advances the visible counter, matching
                    // MoveFilesService. Inside the try, so a progress consumer
                    // that throws costs this one file an UnknownError instead of
                    // costing the batch: from outside the try the throw leaves the
                    // loop altogether, and the files already deleted are never
                    // reported, the result and the failure log's closing entry
                    // both being built past the loop.
                    progress?.Report(new OperationProgress(i + 1, total, _fs.Path.GetFileName(filePath)));

                    if (!_fs.File.Exists(filePath))
                    {
                        errors.Add(new MissingSourceFile(filePath));
                        continue;
                    }

                    // Refuse a reparse-point source, matching MoveFilesService:
                    // deleting a symlink removes the link, so following one out
                    // of the cache is refused. Real-FS check (MockFileSystem
                    // cannot bypass). Reparse first, then the containment check,
                    // so a symlink is reported as one. An attribute read that
                    // FAILS refuses the file as UnknownError rather than as a
                    // symlink, which it has not been shown to be.
                    var reparse = Helpers.StorageHelpers.CheckReparsePoint(filePath, out var reparseError);
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Yes)
                    {
                        errors.Add(new SourceIsReparsePoint(filePath));
                        continue;
                    }
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Unreadable)
                    {
                        failureLog.Record(reparseError!);
                        errors.Add(new UnknownError(filePath));
                        continue;
                    }

                    // Containment guard at the service boundary: never delete a
                    // file that does not resolve directly into
                    // C:\Windows\Installer, even if a corrupt candidate reached
                    // here. This is the source-side choke point matching the
                    // destination's. A path that could not be resolved at all is
                    // refused the same way and reported without the
                    // out-of-bounds claim; see the matching block in
                    // MoveFilesService.
                    // Inverted, so the delete below is reached only by an answer
                    // that positively said Safe, and the reason is switched on
                    // inside. Two positive equality tests naming Refused and
                    // Unproven fell through to File.Delete for anything they did
                    // not name, which made any RemovalSafety member added later a
                    // deletion until somebody remembered to come back here. The
                    // enum grew once already, splitting one refusal into Refused
                    // and Unproven, so that is a thing that happens rather than a
                    // thing that might.
                    //
                    // The default arm files the same UnknownError as Unproven but
                    // will not borrow its sentence, because a member nobody has
                    // written yet has not been shown to be a path that could not
                    // be resolved, and CandidateGuard's contract is that a caller
                    // must not name a cause it has not shown.
                    var safety = CandidateGuard.CheckSafeToRemove(filePath, cacheRoot);
                    if (safety != CandidateGuard.RemovalSafety.Safe)
                    {
                        switch (safety)
                        {
                            case CandidateGuard.RemovalSafety.Refused:
                                errors.Add(new CandidateOutsideCache(filePath));
                                break;
                            case CandidateGuard.RemovalSafety.Unproven:
                                failureLog.Record(new InvalidOperationException(
                                    $"Delete refused: {filePath} could not be resolved, so it could not be shown to be inside the Installer cache."));
                                errors.Add(new UnknownError(filePath));
                                break;
                            default:
                                failureLog.Record(new InvalidOperationException(
                                    $"Delete refused: the containment guard answered {safety} for {filePath}, which this service has no handling for."));
                                errors.Add(new UnknownError(filePath));
                                break;
                        }
                        continue;
                    }

                    _fs.File.Delete(filePath);
                    deleted++;
                }
                // Logged for the same reason as the matching block in
                // MoveFilesService: the framework exception's detail exists
                // nowhere else once the category has been filed.
                catch (UnauthorizedAccessException ex)
                {
                    // File.Delete throws this for a READ-ONLY file as well as
                    // for a permissions refusal, and the two are not the same
                    // problem. The shell delete this replaced cleared the
                    // attribute and carried on, so leaving it here would be a
                    // regression wearing the costume of a permissions error:
                    // the user is told Windows refused access, cannot tell a
                    // read-only bit from an ACL, and can act on neither.
                    //
                    // Safe HERE and nowhere else, which is why it is inline
                    // rather than a helper anything could call. By this line the
                    // file has passed the reparse refusal and the containment
                    // guard, both reading the real filesystem, so it is a real
                    // file inside C:\Windows\Installer; and the user has
                    // confirmed the deletion. Only the read-only attribute is
                    // cleared, only once, and any throw from clearing it or from
                    // the retry fails closed exactly as before.
                    if (TryClearReadOnly(filePath, failureLog))
                    {
                        try
                        {
                            _fs.File.Delete(filePath);
                            deleted++;
                            continue;
                        }
                        catch (Exception retry)
                        {
                            // Both are recorded: without the first the crash log
                            // would not show a retry was attempted, and without
                            // the second it would not show what beat it.
                            failureLog.Record(ex);
                            failureLog.Record(retry);
                            RestoreReadOnly(filePath, failureLog);
                            errors.Add(new AccessDenied(filePath));
                            continue;
                        }
                    }
                    failureLog.Record(ex);
                    errors.Add(new AccessDenied(filePath));
                }
                catch (IOException ex)
                {
                    failureLog.Record(ex);
                    // ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION as
                    // HRESULTs: another program holds the file open, which is
                    // the one IO failure here with a cause the user can act on
                    // and the one that is not a fault. Discriminated exactly as
                    // MoveFilesService does, off the same two codes, so both
                    // halves of the app name the same condition the same way.
                    errors.Add(ex.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021)
                        ? new FileInUse(filePath)
                        : new IOFailure(filePath));
                }
                catch (Exception ex)
                {
                    failureLog.Record(ex);
                    errors.Add(new UnknownError(filePath));
                }
            }
            }
            catch (OperationCanceledException)
            {
                // Return what was deleted before the cancel rather than
                // throwing the tally away. The loop's ThrowIfCancellationRequested
                // is the only cancellation source and sits outside the inner
                // per-file catch, so only a real cancel lands here.
                cancelled = true;
            }

            // Outside the cancel catch so a batch the user stopped still
            // accounts for what its failures cost the log.
            failureLog.WriteClosingEntry();

            // CancellationToken.None: best-effort cleanup. See the
            // matching comment in MoveFilesService for the rationale.
            InstallerCacheHelpers.PruneEmptySubdirectories(_fs, CancellationToken.None);
            return new DeleteResult(deleted, errors.AsReadOnly(), Cancelled: cancelled, HeldBack: heldBack, HeldBackReasons: recheck.Reasons);
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
    /// Clears the read-only attribute so the delete can be retried, and reports
    /// whether it is worth retrying. False for a file that was not read-only in
    /// the first place, which is the ordinary permissions refusal and has
    /// nothing here to fix, and false if the attributes cannot be read or
    /// written, which is a second refusal and is recorded rather than chased.
    /// </summary>
    private bool TryClearReadOnly(string filePath, PerItemFailureLog failureLog)
    {
        try
        {
            var attributes = _fs.File.GetAttributes(filePath);
            if (!attributes.HasFlag(FileAttributes.ReadOnly)) return false;
            _fs.File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
            return true;
        }
        catch (Exception ex)
        {
            failureLog.Record(ex);
            return false;
        }
    }

    /// <summary>
    /// Puts back the read-only attribute cleared for a retry that then failed.
    /// The file is the user's, it is still in C:\Windows\Installer, and the run
    /// has just told them it could not be deleted, which has to be true of the
    /// file's attributes as well as of the file.
    ///
    /// Reached only from inside the branch where
    /// <see cref="TryClearReadOnly"/> returned true, so there is always an
    /// attribute to put back. Guarded, unlike the clear: a throw in the tidying
    /// up must not replace the two errors already recorded for this file.
    /// </summary>
    private void RestoreReadOnly(string filePath, PerItemFailureLog failureLog)
    {
        try
        {
            _fs.File.SetAttributes(filePath,
                _fs.File.GetAttributes(filePath) | FileAttributes.ReadOnly);
        }
        catch (Exception ex)
        {
            failureLog.Record(new InvalidOperationException(
                $"The read-only attribute cleared from {filePath} could not be put back after the "
                + "retried delete failed.", ex));
        }
    }
}
