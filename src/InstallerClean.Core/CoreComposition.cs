using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace InstallerClean.Services;

/// <summary>
/// DI registration helper for the headless service surface in
/// <c>InstallerClean.Core</c>. Both the WPF host (InstallerClean) and
/// the console host (InstallerClean.Cli) call <see cref="AddInstallerCleanCore"/>
/// from their composition roots so the same service graph is wired
/// identically in both subsystems.
///
/// The GUI host then layers WPF-only services on top
/// (<c>IDialogService</c>, <c>IConfirmationService</c>, <c>IWindowService</c>,
/// <c>MainViewModel</c>); the CLI host doesn't need any of those.
/// </summary>
public static class CoreComposition
{
    /// <summary>
    /// Registers every headless service as a singleton on the given
    /// <see cref="IServiceCollection"/>. Lifetime rationale matches
    /// the GUI's <c>Composition</c> root: services are stateless aside
    /// from disk paths they read/write, so a single instance per
    /// process is the simplest correct choice.
    /// </summary>
    /// <remarks>
    /// Every registration here is Singleton. Both hosts build with
    /// <c>ValidateScopes</c> and <c>ValidateOnBuild</c> set, so a Scoped
    /// service captured by a Singleton and a constructor parameter with no
    /// registration behind it both fail at the container build rather than at
    /// first resolve.
    /// </remarks>
    public static IServiceCollection AddInstallerCleanCore(this IServiceCollection services)
    {
        // Stateless infrastructure.
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IRegistryReader, RegistryReader>();
        services.AddSingleton<IMutexProbe, MutexProbe>();
        services.AddSingleton<Interop.IMsiApi, Interop.MsiApi>();

        // Win32 / registry / MSI-API wrappers.
        services.AddSingleton<IInstallerQueryService, InstallerQueryService>();
        services.AddSingleton<IPendingRebootService, PendingRebootService>();
        services.AddSingleton<IMsiFileInfoService, MsiFileInfoService>();
        // Re-verifies removable candidates against the API at action time; the
        // GUI and CLI call it just before a Move/Delete batch.
        services.AddSingleton<IRemovableReverifier, RemovableReverifier>();

        // File-mutating services.
        services.AddSingleton<IFileSystemScanService, FileSystemScanService>();
        services.AddSingleton<IMoveFilesService, MoveFilesService>();
        services.AddSingleton<IDeleteFilesService, DeleteFilesService>();

        // Persistence.
        services.AddSingleton<ISettingsService, SettingsService>();

        // The two outbound-network services. Singleton keeps each one's
        // HttpClient connection pool reused for the life of the process.
        services.AddSingleton<IUpdateCheckService, UpdateCheckService>();
        services.AddSingleton<IResultLogService, ResultLogService>();

        return services;
    }
}
