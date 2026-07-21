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
    private readonly IEnumerable<string>? _overrideFiles;
    private readonly string? _installerFolderOverride;

    /// <summary>Production constructor. DI supplies both dependencies; the override fields stay null.</summary>
    /// <remarks>
    /// Microsoft.Extensions.DependencyInjection resolves the public ctor
    /// with the most resolvable parameters and ignores internal ctors.
    /// The test ctors below are <c>internal</c> so DI cannot select one
    /// at resolution time and pass defaults the production code never
    /// expects.
    /// </remarks>
    public FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem)
        : this(queryService, fileSystem, null, null) { }

    /// <summary>Test constructor. Injects a fake file list.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles)
        : this(queryService, new FileSystem(), overrideFiles, null) { }

    /// <summary>Test constructor. Points enumeration at a real directory.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles, string? installerFolderOverride)
        : this(queryService, new FileSystem(), overrideFiles, installerFolderOverride) { }

    /// <summary>
    /// Test constructor. Injects an <see cref="IFileSystem"/> so the
    /// scan-against-registered-set logic can be verified without
    /// touching <c>C:\Windows\Installer</c> on the host machine.
    /// </summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IFileSystem fileSystem,
        IEnumerable<string>? overrideFiles, string? installerFolderOverride)
    {
        _queryService = queryService;
        _fs = fileSystem;
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
        List<string> diskFiles;
        if (_overrideFiles is not null)
        {
            diskFiles = _overrideFiles as List<string> ?? _overrideFiles.ToList();
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

        foreach (var filePath in diskFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (registeredPaths.Contains(filePath))
                continue;

            var ext = _fs.Path.GetExtension(filePath);
            if (!ext.Equals(".msi", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".msp", StringComparison.OrdinalIgnoreCase))
                continue;

            // Containment guard at candidate creation. A walk file is normally
            // in-bounds (it came out of the folder root), but assert it, so a
            // reparse point that slipped the enumeration filter is dropped rather
            // than offered. A refused candidate is logged and skipped; an
            // unproven one is kept off the list under words that do not claim
            // more than the check showed, and a transient read failure
            // self-heals on the next scan.
            var walkSafety = CandidateGuard.CheckSafeToRemove(filePath, _installerFolderOverride);
            if (walkSafety != CandidateGuard.RemovalSafety.Safe)
            {
                CrashLog.Write(new InvalidOperationException(
                    walkSafety == CandidateGuard.RemovalSafety.Refused
                        ? $"Removal candidate refused (not directly in the Installer cache, or a reparse point): {filePath}"
                        : $"Removal candidate not offered (its symlink status or location could not be read): {filePath}"));
                continue;
            }

            long size = 0;
            // IOException covers locked / vanished files; UnauthorizedAccess
            // covers payload subfolders the elevated process still can't
            // read (deeply ACL'd MSI directories); SecurityException covers
            // the rare CAS-policy path. OOM and the like propagate.
            try { size = _fs.FileInfo.New(filePath).Length; }
            catch (IOException) { /* file vanished or locked */ }
            catch (UnauthorizedAccessException) { /* unreadable subfolder */ }
            catch (SecurityException) { /* CAS policy denies the FileInfo construction */ }

            removable.Add(new OrphanedFile(
                FullPath: filePath,
                SizeBytes: size,
                IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                IsRemovablePatch: false,
                IsObsoleted: false,
                Reason: Strings.Reason_Orphaned));
        }

        // Stat every registered package once here so the Details window
        // doesn't have to hit disk on the UI thread when it opens.
        long stillUsedBytes = 0;
        int missingNonRemovable = 0;
        int missingRemovable = 0;
        int nonRemovablePresent = 0;
        var sizedPackages = new List<RegisteredPackage>(registered.Count);
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
            // Same narrowed set as the orphan-file stat block above:
            // IOException for locked / vanished, UnauthorizedAccessException
            // for the deeply ACL'd payload subfolder case, SecurityException
            // for the rare CAS-policy path. OOM / SOH propagate.
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
                    ? CandidateGuard.CheckSafeToRemove(pkg.LocalPackagePath, _installerFolderOverride)
                    : CandidateGuard.RemovalSafety.Refused;
                if (exists && patchSafety == CandidateGuard.RemovalSafety.Safe)
                {
                    var ext = _fs.Path.GetExtension(pkg.LocalPackagePath);
                    // PatchState 2 = superseded by a newer patch.
                    // PatchState 4 = obsoleted (publisher-withdrawn);
                    // distinct API state, distinct Reason label, same
                    // user-visible outcome (the patch is removable).
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
                    CrashLog.Write(new InvalidOperationException(
                        patchSafety == CandidateGuard.RemovalSafety.Refused
                            ? $"Removable-patch candidate refused (not directly in the Installer cache, or a reparse point): {pkg.LocalPackagePath}"
                            : $"Removable-patch candidate not offered (its symlink status or location could not be read): {pkg.LocalPackagePath}"));
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
        var stillUsed = sizedPackages.Where(p => !p.IsRemovable).ToList().AsReadOnly();

        // Correlation sanity gate. On any real machine at least some registered
        // package's cached file is present on disk. If NOT ONE is (every
        // non-removable registered package looks missing) yet the walk still
        // yielded files to offer for removal, the two halves have not
        // correlated: a path-form mismatch between the API's LocalPackage values
        // and the walked paths, a collapsed enumeration, or the wrong folder all
        // produce exactly this signature (Windows referencing files that are all
        // "gone" while every file on disk is "orphaned"), and no healthy machine
        // does. A tool that genuinely wiped the cache would leave no files to be
        // orphans, so removable.Count > 0 rules that benign case out. Refuse the
        // scan rather than offer the whole cache for deletion on a broken
        // correlation. No absolute floor is used on purpose: Windows always has
        // many machine-context products, so "every one missing" is the collapse
        // signature at any real count, and a floor would only mask a smaller one.
        if (nonRemovablePresent == 0 && missingNonRemovable > 0 && removable.Count > 0)
            throw new LocalisedInvalidOperationException(Strings.Error_ScanCorrelationFailed);

        progress?.Report(new ScanProgressUpdate(string.Format(Strings.Status_FoundUnused,
            removable.Count, DisplayHelpers.PluraliseFile(removable.Count))));
        return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes, missingNonRemovable, missingRemovable,
            query.UnreadableProductCount);
    }

    /// <summary>
    /// Enumerates the walk into a list, checking the cancellation token per
    /// file. Runs inside a <c>Task.Run</c> so the directory walk stays off the
    /// caller's thread (the GUI's dispatcher).
    /// </summary>
    private List<string> MaterialiseInstallerFiles(string folder, CancellationToken cancellationToken)
    {
        var list = new List<string>();
        foreach (var filePath in GetInstallerFiles(folder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            list.Add(filePath);
        }
        return list;
    }

    private IEnumerable<string> GetInstallerFiles(string folder)
    {
        if (!_fs.Directory.Exists(folder))
            return Enumerable.Empty<string>();

        // Scan the folder ROOT only. A registered LocalPackage path only ever
        // sits at the root, so for any file in a subdirectory the API
        // correlation carries no signal at all: calling such a file orphaned
        // would be asking Windows about a file it was never told to track.
        // Root-only makes the candidate set "files at the root that no
        // registered package claims", which cannot acquire a new blind spot.
        // Recursing instead needs a denylist ($PatchCache$, the patch engine's
        // baseline payload copies), and a denylist can only ever exclude a
        // subtree after it has already bitten someone; root-only puts that whole
        // subtree out of scope to begin with.
        // Reparse points are skipped so a junction planted at the root cannot
        // redirect enumeration outside it; Hidden and System stay included
        // because real cache entries sometimes carry those attributes.
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
        };

        return _fs.Directory.EnumerateFiles(folder, "*.msi", options)
            .Concat(_fs.Directory.EnumerateFiles(folder, "*.msp", options));
    }
}
