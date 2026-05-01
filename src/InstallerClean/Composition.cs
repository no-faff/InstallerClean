using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

/// <summary>
/// Composition root for the application's service graph. Wires every
/// interface to its concrete implementation in one place; <see cref="App"/>
/// builds the container, resolves <see cref="MainViewModel"/>, and
/// disposes the container on shutdown.
///
/// Every registration is Singleton. The services are stateless aside
/// from the file paths they read/write (SettingsService reads from
/// disk on every Load, no in-memory cache); the view-model graph
/// matches the single MainWindow which lives for the process's
/// lifetime; nothing here would benefit from per-call instantiation.
/// </summary>
internal static class Composition
{
    /// <summary>
    /// Builds and returns the DI container for the running application.
    /// Caller owns disposal: the GUI path holds it in a static field
    /// and disposes in <c>App.OnExit</c>; the CLI path uses a
    /// <c>using var</c> for the duration of the command.
    /// </summary>
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Stateless infrastructure.
        services.AddSingleton<IFileSystem, FileSystem>();

        // Win32 / registry / MSI-API wrappers.
        services.AddSingleton<IInstallerQueryService, InstallerQueryService>();
        services.AddSingleton<IPendingRebootService, PendingRebootService>();
        services.AddSingleton<IMsiFileInfoService, MsiFileInfoService>();

        // File-mutating services. Each takes the IFileSystem singleton
        // above plus its own constructor dependencies; the container
        // resolves them automatically.
        services.AddSingleton<IFileSystemScanService, FileSystemScanService>();
        services.AddSingleton<IMoveFilesService, MoveFilesService>();
        services.AddSingleton<IDeleteFilesService, DeleteFilesService>();

        // Persistence and user-interaction surfaces.
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IConfirmationService, ConfirmationService>();
        services.AddSingleton<IWindowService, WindowService>();

        // View-model graph. MainViewModel constructs the four child
        // VMs internally; the container only needs to know how to
        // resolve MainViewModel itself.
        services.AddSingleton<MainViewModel>();

        // validateScopes: true is defensive against a future scoped
        // registration accidentally being captured by a singleton.
        // Today every registration is Singleton so the validator has
        // nothing to flag, but the flag costs nothing and prevents a
        // class of bug if a maintainer ever adds a scoped service.
        return services.BuildServiceProvider(validateScopes: true);
    }
}
