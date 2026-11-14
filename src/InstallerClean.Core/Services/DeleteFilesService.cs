using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
    private readonly IFileSystem _fs;
    /// <summary>
    /// Acts on the batch inside Windows Installer transactions of the app's own, a group
    /// of files at a time, after comparing Windows Installer's records with the reading
    /// the batch was checked against. Held by the service rather than the caller because
    /// the act has to happen inside the hold.
    /// </summary>
    private readonly HoldRunner _runner;

    /// <summary>
    /// Test-only real-folder override for the containment guard's cache root
    /// (null in production). See the matching field on <c>MoveFilesService</c>:
    /// it lets the real-filesystem integration tests treat a %TEMP% sandbox as
    /// the cache without touching the real one, and does not let a MockFileSystem
    /// bypass the gate.
    /// </summary>
    private readonly string? _installerFolderOverride;

    /// <summary>Constructor. The DI container injects the registered singletons in production.</summary>
    public DeleteFilesService(IFileSystem fileSystem, HoldRunner runner)
        : this(fileSystem, runner, null) { }

    /// <summary>Test constructor. Acts through <see cref="HoldRunner.ForTests"/>, which takes no hold on the machine.</summary>
    internal DeleteFilesService(IFileSystem fileSystem)
        : this(fileSystem, HoldRunner.ForTests(), null) { }

    /// <summary>Seam constructor: an injected <see cref="HoldRunner"/> plus the sandbox override.</summary>
    internal DeleteFilesService(IFileSystem fileSystem, HoldRunner runner, string? installerFolderOverride)
    {
        _fs = fileSystem;
        _runner = runner;
        _installerFolderOverride = installerFolderOverride;
    }

    public Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        UnderLeaseClaims underLeaseClaims,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        Func<string, bool>? mayAct = null)
    {
        return Task.Run(() =>
        {
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;
            if (total == 0)
                return new DeleteResult(0, Array.Empty<FileOperationError>());

            cancellationToken.ThrowIfCancellationRequested();

            int deleted = 0;
            var errors = new List<FileOperationError>();
            var failureLog = new PerItemFailureLog("Delete",
                "The per-file list is on the completion screen.");
            // Resolved once for the batch; the guard resolves each SOURCE per
            // file against it (see InstallerCacheRoot).
            var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);
            bool cancelled = false;
            HoldRunResult? run = null;
            var tally = new HoldRunTally();

            // Called inside a hold, so everything in it reads or changes only the
            // local file. Every check that reads the file is in here, inside the same
            // hold as the delete, so nothing Windows Installer does can come between
            // the check and the act.
            void DeleteOne(string filePath)
            {
                try
                {
                    // The host's question is asked before the checks as well as last
                    // before the act, so a batch the host has stopped covering stops
                    // before this file is read.
                    if (mayAct is not null && !mayAct(filePath)) throw new BatchStoppedException();

                    if (!_fs.File.Exists(filePath))
                    {
                        errors.Add(new MissingSourceFile(filePath));
                        return;
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
                        return;
                    }
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Unreadable)
                    {
                        failureLog.Record(reparseError!);
                        errors.Add(new UnknownError(filePath));
                        return;
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
                        return;
                    }

                    // The last thing before the act, after every check on the file.
                    if (mayAct is not null && !mayAct(filePath)) throw new BatchStoppedException();
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
                    // The host's question is asked again before the attribute is cleared, the
                    // clear and the retried delete being acts on the file as the first was.
                    if (mayAct is not null && !mayAct(filePath)) throw new BatchStoppedException();
                    if (TryClearReadOnly(filePath, failureLog))
                    {
                        try
                        {
                            _fs.File.Delete(filePath);
                            deleted++;
                            return;
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
                            return;
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
                catch (Exception ex) when (!BatchStop.EndsBatch(ex, cancellationToken))
                {
                    failureLog.Record(ex);
                    errors.Add(new UnknownError(filePath));
                }
            }

            try
            {
                run = _runner.Run(pathList, underLeaseClaims.Print ?? InstallerRecordsPrint.None, DeleteOne,
                    (done, batch, filePath) => Report(progress, done + 1, batch, filePath, failureLog),
                    cancellationToken, tally);
            }
            catch (OperationCanceledException)
            {
                // Return what was deleted before the cancel rather than throwing the
                // tally away. The runner's cancellation checks and the host's mayAct
                // answering no are the only sources, and the per-file catch-all lets
                // only those through (BatchStop.EndsBatch).
                cancelled = true;
            }

            // Outside the cancel catch so a batch the user stopped still
            // accounts for what its failures cost the log.
            failureLog.WriteClosingEntry();

            return Result(deleted, errors, cancelled, run, tally);
        }, cancellationToken);
    }

    /// <summary>
    /// Reports one file outside the hold. A consumer that throws costs the batch nothing:
    /// the file has already been dealt with, and the throw is recorded once.
    /// </summary>
    private static void Report(IProgress<OperationProgress>? progress, int done, int total,
        string filePath, PerItemFailureLog failureLog)
    {
        try
        {
            progress?.Report(new OperationProgress(done, total, PathText.GetFileName(filePath), Finished: true));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            failureLog.Record(ex);
        }
    }

    /// <summary>
    /// The result of a run. A run that could not take a hold before its first file says
    /// so with the flag its hosts read as "nothing was deleted"; a run stopped later keeps
    /// its count, and the flag then sits beside files that were deleted.
    /// </summary>
    private static DeleteResult Result(int deleted, List<FileOperationError> errors, bool cancelled,
        HoldRunResult? run, HoldRunTally tally)
    {
        var heldBack = tally.HeldBack;
        var reasons = tally.Reasons;
        return run?.End switch
        {
            HoldRunEnd.InstallerBusy or HoldRunEnd.InstallerActivity =>
                new DeleteResult(deleted, errors.AsReadOnly(), Cancelled: cancelled, InstallerBusy: true,
                    HeldBack: heldBack, HeldBackReasons: reasons),
            HoldRunEnd.RollbackDisabled =>
                new DeleteResult(deleted, errors.AsReadOnly(), Cancelled: cancelled,
                    HeldBack: heldBack, HeldBackReasons: reasons) { InstallerRollbackDisabled = true },
            HoldRunEnd.HoldUnavailable or HoldRunEnd.EndFailed =>
                new DeleteResult(deleted, errors.AsReadOnly(), Cancelled: cancelled, InstallerLockUnavailable: true,
                    HeldBack: heldBack, HeldBackReasons: reasons),
            _ => new DeleteResult(deleted, errors.AsReadOnly(), Cancelled: cancelled,
                HeldBack: heldBack, HeldBackReasons: reasons),
        };
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
