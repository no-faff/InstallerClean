using System.IO.Abstractions;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

public sealed class MoveFilesService : IMoveFilesService
{
    private readonly IFileSystem _fs;
    private readonly IMutexProbe _mutex;

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
    public MoveFilesService(IFileSystem fileSystem, IMutexProbe mutex) : this(fileSystem, mutex, null) { }

    /// <summary>Test constructor. No mutex hold (the hold is exercised via the seam constructor below).</summary>
    internal MoveFilesService(IFileSystem fileSystem) : this(fileSystem, NullMutexProbe.Instance, null) { }

    /// <summary>Test constructor. Points the source containment guard at a real sandbox folder; no mutex hold.</summary>
    internal MoveFilesService(IFileSystem fileSystem, string? installerFolderOverride)
        : this(fileSystem, NullMutexProbe.Instance, installerFolderOverride) { }

    /// <summary>Seam constructor: an injected <see cref="IMutexProbe"/> (real or fake) plus the sandbox override.</summary>
    internal MoveFilesService(IFileSystem fileSystem, IMutexProbe mutex, string? installerFolderOverride)
    {
        _fs = fileSystem;
        _mutex = mutex;
        _installerFolderOverride = installerFolderOverride;
    }

    public Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
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

        return Task.Run(() =>
        {
            // Hold Global\_MSIExecute for the batch on this worker thread so a
            // msiexec starting mid-move waits on the mutex instead of racing the
            // cache. Acquired here (not on the dispatcher) and released in the
            // finally on the SAME thread (Win32 owner-thread rule); the body is
            // synchronous, so no await hops threads between acquire and release.
            // Held by a live transaction => refuse and touch nothing (the caller
            // re-checks the pending-reboot gate and shows its banner). A
            // DACL-refused acquire (lease null, not heldByAnother) proceeds
            // without the hold rather than refusing. This closes only the
            // sub-millisecond race after the host-side gate re-check has passed.
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
            // Delete runs its volume probe before its acquire, and this does NOT
            // match it: everything between here and the loop is the destination
            // work, and running it before the acquire would create the
            // destination folder even on the runs the busy check above refuses.
            // A refusal that has touched nothing is worth more than a shorter
            // hold.
            var lease = _mutex.TryAcquire(PendingRebootService.MsiExecuteMutexName, out var heldByAnother);
            if (lease is null && heldByAnother)
                return new MoveResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true);

            try
            {
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
            var canonicalDestination = InstallerCacheHelpers.ResolveFinalPath(destinationFolder)
                .TrimEnd(Path.DirectorySeparatorChar);

            ProbeDestinationWriteable(destinationFolder);

            int moved = 0;
            var errors = new List<FileOperationError>();
            var failureLog = new PerFileFailureLog("Move",
                "The per-file list is on the completion screen and in the result log.");
            // Resolved once for the batch; the guard resolves each SOURCE per
            // file against it (see InstallerCacheRoot). Separate from
            // canonicalDestination above, which guards the other end and is
            // re-resolved per iteration for a reason of its own.
            var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;
            bool cancelled = false;

            try
            {
            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = pathList[i];

                // Report progress before the skip checks so the
                // visible counter advances on missing / reparse-point
                // entries instead of jumping over them.
                progress?.Report(new OperationProgress(i + 1, total, _fs.Path.GetFileName(sourcePath)));

                // Re-resolve and compare to the canonical capture.
                var currentResolved = InstallerCacheHelpers.ResolveFinalPath(destinationFolder)
                    .TrimEnd(Path.DirectorySeparatorChar);
                if (!currentResolved.Equals(canonicalDestination, StringComparison.OrdinalIgnoreCase))
                    throw new LocalisedInvalidOperationException(
                        string.Format(Strings.Error_DestinationChangedMidBatch, destinationFolder));

                try
                {
                    if (!_fs.File.Exists(sourcePath))
                    {
                        errors.Add(new MissingSourceFile(sourcePath));
                        continue;
                    }

                    // Refuse a source that's a symlink or junction:
                    // moving the symlink would pull an OS file out of
                    // System32. Real-FS check (MockFileSystem cannot
                    // bypass). An attribute read that FAILS refuses the file
                    // too, but as UnknownError: SourceIsReparsePoint tells the
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
                    var safety = CandidateGuard.CheckSafeToRemove(sourcePath, cacheRoot);
                    if (safety == CandidateGuard.RemovalSafety.Refused)
                    {
                        errors.Add(new CandidateOutsideCache(sourcePath));
                        continue;
                    }
                    if (safety == CandidateGuard.RemovalSafety.Unproven)
                    {
                        failureLog.Record(new InvalidOperationException(
                            $"Move refused: {sourcePath} could not be resolved, so it could not be shown to be inside the Installer cache."));
                        errors.Add(new UnknownError(sourcePath));
                        continue;
                    }

                    var fileName = _fs.Path.GetFileName(sourcePath);
                    var destPath = GetUniqueDestPath(destinationFolder, fileName);
                    _fs.File.Move(sourcePath, destPath);
                    moved++;
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
                catch (UnauthorizedAccessException ex)
                {
                    failureLog.Record(ex);
                    errors.Add(new AccessDenied(sourcePath));
                }
                catch (IOException ex)
                {
                    failureLog.Record(ex);
                    // ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION as
                    // HRESULTs: another program holds the file open, which is
                    // the one IO failure here with a cause the user can act on
                    // and the one that is not a fault. The Delete path
                    // discriminates the same two codes off the shell's HRESULT
                    // (FileOperationError.RecycleFailed), so both halves of the
                    // app name the same condition the same way.
                    errors.Add(ex.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021)
                        ? new FileInUse(sourcePath)
                        : new IOFailure(sourcePath));
                }
                catch (Exception ex)
                {
                    failureLog.Record(ex);
                    errors.Add(new UnknownError(sourcePath));
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
            return new MoveResult(moved, errors.AsReadOnly(), cancelled);
            }
            finally
            {
                // Release on this same worker thread (Win32 owner-thread rule);
                // no-op when the acquire fell back (lease null).
                lease?.Dispose();
            }
        }, cancellationToken);
    }

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
