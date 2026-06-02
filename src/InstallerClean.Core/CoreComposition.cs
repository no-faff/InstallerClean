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
    /// Every registration here is Singleton. A Scoped service captured
    /// by a Singleton throws at startup via the WPF host's
    /// <c>Composition.BuildServiceProvider(validateScopes: true)</c>
    /// flag.
    /// </remarks>
    public static IServiceCollection AddInstallerCleanCore(this IServiceCollection services)
    {
        // Stateless infrastructure.
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IRegistryReader, RegistryReader>();
        services.AddSingleton<IMutexProbe, MutexProbe>();

        // Win32 / registry / MSI-API wrappers.
        services.AddSingleton<IInstallerQueryService, InstallerQueryService>();
        services.AddSingleton<IPendingRebootService, PendingRebootService>();
        services.AddSingleton<IMsiFileInfoService, MsiFileInfoService>();

        // File-mutating services. The recycle engine owns the single
        // STA thread the IFileOperation delete path runs on; as a
        // singleton the container disposes it at shutdown, joining the
        // thread. DeleteFilesService depends on it.
        services.AddSingleton<IFileSystemScanService, FileSystemScanService>();
        services.AddSingleton<IMoveFilesService, MoveFilesService>();
        services.AddSingleton<IRecycleEngine, RecycleEngine>();
        services.AddSingleton<IDeleteFilesService, DeleteFilesService>();

        // Persistence.
        services.AddSingleton<ISettingsService, SettingsService>();

        // User-triggered network. Singleton keeps the HttpClient
        // connection pool reused across the button-click lifetime.
        services.AddSingleton<IUpdateCheckService, UpdateCheckService>();
        services.AddSingleton<IResultLogService, ResultLogService>();

        return services;
    }
}
