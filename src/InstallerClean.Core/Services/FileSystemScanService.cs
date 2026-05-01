using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IFileSystemScanService"/> implementation. Pairs the
/// API output from <see cref="IInstallerQueryService"/> with a directory
/// walk of <c>C:\Windows\Installer</c> via the injected
/// <see cref="IFileSystem"/>, so tests can substitute a mock filesystem
/// and a mock query service to drive every code path without touching
/// the real Installer folder.
/// </summary>
public sealed class FileSystemScanService : IFileSystemScanService
{
    private readonly IInstallerQueryService _queryService;
    private readonly IFileSystem _fs;
    private readonly IEnumerable<string>? _overrideFiles;
    private readonly string? _installerFolderOverride;

    /// <summary>
    /// Production constructor. The DI container injects both
    /// dependencies; the override fields stay null so production
    /// enumeration walks the real Installer folder via the injected
    /// <see cref="IFileSystem"/>.
    /// </summary>
    /// <remarks>
    /// DI uses this constructor: it is the only public ctor on the
    /// class and Microsoft.Extensions.DependencyInjection picks the
    /// public ctor with the most resolvable parameters. Keep the
    /// test ctors below <c>internal</c> so they cannot be picked up
    /// accidentally if a future change widens DI's resolver.
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
    /// Test constructor. Inject an <see cref="IFileSystem"/> so unit
    /// tests can verify the scan-against-registered-set logic without
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

        // ConfigureAwait(false): the rest of this method does a directory
        // walk and per-file stat across every file in C:\Windows\Installer.
        // Without ConfigureAwait(false), the await continuation runs on
        // the caller's SynchronizationContext - which is the WPF dispatcher
        // when called from the splash startup path or the user-driven
        // scan command - and the heavy I/O blocks the UI thread for the
        // entire scan duration. Core services should never assume a UI
        // thread; ConfigureAwait(false) is the contract.
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
            try { size = _fs.FileInfo.New(filePath).Length; } catch (Exception) { /* skip inaccessible files */ }

            removable.Add(new OrphanedFile(
                FullPath: filePath,
                SizeBytes: size,
                IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                Reason: Strings.Reason_Orphaned));
        }

        // Stat every registered package once here so the Details window
        // doesn't have to hit disk on the UI thread when it opens.
        long stillUsedBytes = 0;
        int missingFromDisk = 0;
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
            catch (Exception) { }

            if (!exists) missingFromDisk++;

            sizedPackages.Add(pkg with { FileSizeBytes = size, FileExists = exists });

            if (pkg.IsRemovable)
            {
                var ext = _fs.Path.GetExtension(pkg.LocalPackagePath);
                removable.Add(new OrphanedFile(
                    FullPath: pkg.LocalPackagePath,
                    SizeBytes: size,
                    IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                    Reason: Strings.Reason_Superseded));
            }
            else
            {
                stillUsedBytes += size;
            }
        }
        var stillUsed = sizedPackages.Where(p => !p.IsRemovable).ToList().AsReadOnly();

        progress?.Report(string.Format(Strings.Status_FoundOrphans,
            removable.Count, DisplayHelpers.PluraliseFile(removable.Count)));
        return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes, missingFromDisk);
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
