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
    /// production; the mutex is taken for the batch, and a batch refuses where a
    /// live install owns it.
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
        UnderLeaseClaims underLeaseClaims,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;
            if (total == 0)
                return new DeleteResult(0, Array.Empty<FileOperationError>());

            cancellationToken.ThrowIfCancellationRequested();

            // Take Global\_MSIExecute for the batch on this worker thread. The
            // acquire refuses the batch where a live install owns it, and for the
            // batch the object reads owned to anything that opens it. Acquired
            // here and released in the finally on the SAME thread
            // (Win32 owner-thread rule); the body is synchronous, so no await hops
            // threads between acquire and release.
            //
            // No way of missing the hold proceeds, and they are reported separately
            // because the caller owes the user a different sentence for each. Held
            // by a live transaction => the pending-reboot gate the caller re-runs
            // meets the same mutex and paints its banner, which says an install is
            // in progress, which it is. Refused because the object's security would
            // not let us open it => the app was not allowed to look, and the caller
            // says so in those words. The gate's probe asks for the rights this
            // acquire asks for, so a refusal standing when the gate ran stopped the
            // batch there, and one met here began after it. Refused any other way
            // with nothing shown to be holding it => the gate has no account of it,
            // its probe reading such a failure as not held, so this result carries
            // its own sentence.
            //
            // Both refusals stop the batch rather than letting it run without the
            // hold, and the delete being permanent is why: nothing deleted here can
            // be fetched back from a bin. MoveFilesService refuses on the same
            // answers, for reasons it states at its own acquire.
            //
            // What the hold costs, so nobody widens it and nobody removes it:
            // _MSIExecute is the machine-wide Windows Installer serialisation
            // mutex, which Windows Installer's client takes at the start of every
            // install, so for as long as this batch runs that client cannot take
            // it.
            //
            // What it is NOT is bounded by the batch's file count, and the one
            // thing inside the hold that breaks that bound is not a file
            // operation at all. The progress callback hands control to a consumer
            // that can run for as long as it likes, which is the property the
            // destination re-check in MoveFilesService is ordered around; in this
            // host's command-line sibling that consumer is a console write, and a
            // console in QuickEdit selection blocks one until the operator clears
            // it.
            var lease = _mutex.TryAcquire(PendingRebootService.MsiExecuteMutexName, out var outcome);
            if (lease is null && outcome == MutexAcquireOutcome.HeldByAnother)
                return new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true);
            if (lease is null && outcome == MutexAcquireOutcome.AccessRefused)
            {
                // The object is there and its security refused us the rights to
                // open it, so ownership was never sampled. Reported apart from the
                // arm below because the user is owed a different sentence: this one
                // is a setting somebody can go and look at, and saying the lock was
                // busy would name a cause nothing here established.
                //
                // Recorded as well as refused, on the same terms as the arm below.
                Helpers.CrashLog.TryWrite(new InvalidOperationException(
                    "Delete refused: access to the Windows Installer mutex was denied, so whether an installation was in progress could not be established."));
                return new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerLockAccessRefused: true);
            }
            if (lease is null)
            {
                // Every other non-fatal way the acquire can fail, with nothing
                // shown to be holding the object. Recorded as well as refused: the
                // refusal is what the user is told, and the crash log is the only
                // place the machine's own condition is written down. Once per
                // batch, so it costs nothing at any file count.
                //
                // An outcome added to MutexAcquireOutcome later arrives here and
                // is refused with this general wording, which is the safe
                // direction. One that owes the user something else takes an arm of
                // its own above, which is the failure worth having.
                Helpers.CrashLog.TryWrite(new InvalidOperationException(
                    "Delete refused: the Windows Installer mutex could not be acquired and nothing could be shown to be holding it."));
                return new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerLockUnavailable: true);
            }

            try
            {
            // The act-time re-read, and the reason it is HERE and not at the
            // caller. The caller's full re-verify runs before this method is
            // entered, so this is the read of the records made closest to the
            // first file operation: a verdict that moved while that enumeration
            // ran is read again here.
            //
            // Only the claims, never the enumeration. An enumeration is kept
            // outside the machine-wide installer lock, so what this re-reads is
            // the set of claims the caller's enumeration built, each asked about
            // by key.
            //
            // Synchronous on the acquiring thread by necessity, not by taste: the
            // lease is released by the thread that took it, so nothing between
            // the acquire and the release may await.
            var recheck = _reverifier.RecheckUnderLease(underLeaseClaims);
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
