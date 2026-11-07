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
        // Stateless infrastructure. The app's own file system, whose File.Move moves as
        // .NET 10's does. BannedSymbols.txt does not see a type argument, so naming
        // System.IO.Abstractions' FileSystem here compiles: CompositionRootTests holds
        // this registration instead.
        services.AddSingleton<IFileSystem, AppFileSystem>();
        services.AddSingleton<IRegistryReader, RegistryReader>();
        services.AddSingleton<IMutexProbe, MutexProbe>();
        // Answers where a volume is mounted, for the pending-reboot gate alone. A
        // queued rename can name its volume rather than carrying a drive root, and
        // placing one of those against the cache folder is the only thing this does.
        services.AddSingleton<IVolumeMountProbe, VolumeMountProbe>();
        // Reads whether Windows Installer's in-progress file is in the cache folder,
        // for the pending-reboot gate alone.
        services.AddSingleton<IInstallerInProgressMarker, InstallerInProgressMarker>();
        services.AddSingleton<Interop.IMsiApi, Interop.MsiApi>();

        // Win32 / registry / MSI-API wrappers.
        services.AddSingleton<IInstallerQueryService, InstallerQueryService>();
        services.AddSingleton<IPendingRebootService, PendingRebootService>();
        services.AddSingleton<IMsiFileInfoService, MsiFileInfoService>();
        // Reads the machine's short-name policy for the opt-in report and for
        // nothing else. Registered beside the other registry readers rather than
        // inside the scan because it answers about the machine, not about the run.
        services.AddSingleton<IShortNameCreationProbe, ShortNameCreationProbe>();
        // Reads a cached package's own declared identity. Two consumers: the
        // enumeration's patch-target route asks a patch file which products it
        // names, and the declared-product screen below asks an installation package
        // which product it belongs to and a patch which patch it is.
        services.AddSingleton<IPackageIdentityReader, PackageIdentityReader>();
        // The scan's third source for cached files, and the only one that starts at
        // the file rather than at a registration. It can keep a candidate back and
        // can do nothing else; see IDeclaredProductCheck for why the superseded half
        // of the offer is never put to it.
        services.AddSingleton<IDeclaredProductCheck, DeclaredProductCheck>();
        // The account this process runs as, which the screen above holds a per-user
        // installation's account against.
        services.AddSingleton<IRunningAccount, RunningAccount>();
        // Answers which file a recorded path names, so the scan's path comparison
        // is not defeated by a registration written in a spelling the folder walk
        // does not produce. Stateless, and it holds nothing between scans.
        services.AddSingleton<IFileIdentityReader, FileIdentityReader>();
        // Reads a cached file's times and the file system that keeps them, for the
        // scan's age check. Stateless in the same way.
        services.AddSingleton<IFileTimesReader, FileTimesReader>();
        // The clock the age check judges against, read in the steps file times are
        // stamped in (FileStampClock). The scan and the check before a Move or Delete
        // are the only consumers of the registered TimeProvider.
        services.AddSingleton<TimeProvider, FileStampClock>();
        // Re-checks a batch just before a Move or Delete: the records again, and
        // the scan's own steps on every file no record names, which is why it
        // takes the scan's readers.
        services.AddSingleton<IRemovableReverifier, RemovableReverifier>();

        // File-mutating services.
        services.AddSingleton<IFileSystemScanService, FileSystemScanService>();
        services.AddSingleton<IDestinationGuard, HandleDestinationGuard>();
        services.AddSingleton<IMoveFilesService, MoveFilesService>();
        services.AddSingleton<IDeleteFilesService, DeleteFilesService>();

        // Persistence. Settings are per account; the first-run mark is one for the PC.
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFirstRunMark, FirstRunMark>();
        // The window's start check and the command line's record it reads. A singleton,
        // so the check started at launch and every later ask share one answer.
        services.AddSingleton<ICommandLineRunRecord, CommandLineRunRecord>();
        services.AddSingleton<IEarlierRunCheck, EarlierRunCheck>();
        // Which way the report box on the first finished card starts.
        services.AddSingleton<IWindowsRegion, WindowsRegion>();

        // The two outbound-network services. Singleton keeps each one's
        // HttpClient connection pool reused for the life of the process.
        services.AddSingleton<IUpdateCheckService, UpdateCheckService>();
        services.AddSingleton<IResultLogService, ResultLogService>();

        return services;
    }
}
