using System.IO;
using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class FileSystemScanService : IFileSystemScanService
{
    private readonly IInstallerQueryService _queryService;
    private readonly IEnumerable<string>? _overrideFiles;
    private readonly string? _installerFolderOverride;

    /// <summary>Production constructor.</summary>
    public FileSystemScanService(IInstallerQueryService queryService)
        : this(queryService, null, null) { }

    /// <summary>Test constructor. Injects a fake file list.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles)
        : this(queryService, overrideFiles, null) { }

    /// <summary>Test constructor. Points enumeration at a real directory.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles, string? installerFolderOverride)
    {
        _queryService = queryService;
        _overrideFiles = overrideFiles;
        _installerFolderOverride = installerFolderOverride;
    }

    public async Task<ScanResult> ScanAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Querying Windows Installer API...");

        var registered = await _queryService.GetRegisteredPackagesAsync(progress, cancellationToken);

        var registeredPaths = new HashSet<string>(
            registered.Select(p => p.LocalPackagePath),
            StringComparer.OrdinalIgnoreCase);

        progress?.Report("Scanning installer cache folder...");

        var diskFiles = _overrideFiles ?? GetInstallerFiles(_installerFolderOverride ?? InstallerCacheHelpers.InstallerFolder);
        var removable = new List<OrphanedFile>();

        foreach (var filePath in diskFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (registeredPaths.Contains(filePath))
                continue;

            var ext = Path.GetExtension(filePath);
            if (!ext.Equals(".msi", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".msp", StringComparison.OrdinalIgnoreCase))
                continue;

            long size = 0;
            try { size = new FileInfo(filePath).Length; } catch (Exception) { /* skip inaccessible files */ }

            removable.Add(new OrphanedFile(
                FullPath: filePath,
                SizeBytes: size,
                IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase)));
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
                if (File.Exists(pkg.LocalPackagePath))
                {
                    exists = true;
                    size = new FileInfo(pkg.LocalPackagePath).Length;
                }
            }
            catch (Exception) { }

            if (!exists) missingFromDisk++;

            sizedPackages.Add(pkg with { FileSizeBytes = size, FileExists = exists });

            if (pkg.IsRemovable)
            {
                var ext = Path.GetExtension(pkg.LocalPackagePath);
                removable.Add(new OrphanedFile(
                    FullPath: pkg.LocalPackagePath,
                    SizeBytes: size,
                    IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                    Reason: "Superseded"));
            }
            else
            {
                stillUsedBytes += size;
            }
        }
        var stillUsed = sizedPackages.Where(p => !p.IsRemovable).ToList().AsReadOnly();

        progress?.Report($"Found {removable.Count} {DisplayHelpers.Pluralise(removable.Count, "file", "files")} to clean up.");
        return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes, missingFromDisk);
    }

    private static IEnumerable<string> GetInstallerFiles(string folder)
    {
        if (!Directory.Exists(folder))
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

        return Directory.EnumerateFiles(folder, "*.msi", options)
            .Concat(Directory.EnumerateFiles(folder, "*.msp", options));
    }
}
