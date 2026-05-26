using System.IO.Abstractions;
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
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(Strings.Status_QueryingApi);

        // ConfigureAwait(false): Core services do not bind to a caller's
        // SynchronizationContext. The continuation under a WPF host
        // would otherwise run on the dispatcher and the directory walk
        // plus per-file stat would block the UI thread for the scan
        // duration.
        var registered = await _queryService.GetRegisteredPackagesAsync(progress, cancellationToken)
            .ConfigureAwait(false);

        var registeredPaths = new HashSet<string>(
            registered.Select(p => p.LocalPackagePath),
            StringComparer.OrdinalIgnoreCase);

        progress?.Report(Strings.Status_ScanningCache);

        var diskFiles = _overrideFiles ?? GetInstallerFiles(_installerFolderOverride ?? InstallerCacheHelpers.InstallerFolder);
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

            long size = 0;
            // IOException covers locked / vanished files; UnauthorizedAccess
            // covers payload subfolders the elevated process still can't
            // read (deeply ACL'd MSI directories). OOM and the like are
            // genuine bugs and propagate.
            try { size = _fs.FileInfo.New(filePath).Length; }
            catch (IOException) { /* file vanished or locked */ }
            catch (UnauthorizedAccessException) { /* unreadable subfolder */ }

            removable.Add(new OrphanedFile(
                FullPath: filePath,
                SizeBytes: size,
                IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                IsSuperseded: false,
                Reason: Strings.Reason_Orphaned));
        }

        // Stat every registered package once here so the Details window
        // doesn't have to hit disk on the UI thread when it opens.
        long stillUsedBytes = 0;
        int missingNonRemovable = 0;
        int missingRemovable = 0;
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
            catch (Exception)
            {
                // Inaccessible package: treat as missing-from-disk
                // (size=0, exists=false) so the count surfaces but
                // doesn't break the scan.
            }

            sizedPackages.Add(pkg with { FileSizeBytes = size, FileExists = exists });

            // Non-removable + missing is the load-bearing banner signal:
            // Windows still claims the file but it is gone from disk, so
            // a future install / uninstall / patch will fail. Removable
            // + missing is benign (Windows considers the patch already
            // removed; the file having gone is the expected end state)
            // and counts separately so the banner does not fire on it.
            if (pkg.IsRemovable)
            {
                if (exists)
                {
                    var ext = _fs.Path.GetExtension(pkg.LocalPackagePath);
                    // PatchState 2 = superseded by a newer patch.
                    // PatchState 4 = obsoleted (publisher-withdrawn);
                    // distinct API state, distinct Reason label, same
                    // user-visible outcome (the patch is removable).
                    var reason = pkg.PatchState == 4
                        ? Strings.Reason_Obsoleted
                        : Strings.Reason_Superseded;
                    removable.Add(new OrphanedFile(
                        FullPath: pkg.LocalPackagePath,
                        SizeBytes: size,
                        IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                        IsSuperseded: true,
                        Reason: reason));
                }
                else
                {
                    missingRemovable++;
                }
            }
            else
            {
                if (exists)
                {
                    stillUsedBytes += size;
                }
                else
                {
                    missingNonRemovable++;
                }
            }
        }
        var stillUsed = sizedPackages.Where(p => !p.IsRemovable).ToList().AsReadOnly();

        progress?.Report(string.Format(Strings.Status_FoundUnused,
            removable.Count, DisplayHelpers.PluraliseFile(removable.Count)));
        return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes, missingNonRemovable, missingRemovable);
    }

    private IEnumerable<string> GetInstallerFiles(string folder)
    {
        if (!_fs.Directory.Exists(folder))
            return Enumerable.Empty<string>();

        // Reparse points are skipped so a junction planted inside the
        // Installer folder cannot redirect enumeration outside it; Hidden
        // and System stay included because real installer-cache entries
        // sometimes carry those attributes.
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
        };

        return _fs.Directory.EnumerateFiles(folder, "*.msi", options)
            .Concat(_fs.Directory.EnumerateFiles(folder, "*.msp", options));
    }
}
