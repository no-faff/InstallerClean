using Microsoft.Extensions.DependencyInjection;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

/// <summary>
/// Composition root for the WPF application's service graph. Wires
/// every interface to its concrete implementation in one place;
/// <see cref="App"/> builds the container, resolves
/// <see cref="MainViewModel"/>, and disposes the container on
/// shutdown.
///
/// The headless service surface (file operations, settings, MSI query,
/// pending-reboot) is registered via
/// <see cref="CoreComposition.AddInstallerCleanCore(IServiceCollection)"/>
/// from <c>InstallerClean.Core</c> so the CLI host (which has no
/// MainWindow, no MessageBox, no DataContext bindings) shares the
/// same registrations. The WPF host then layers in the surfaces it
/// uniquely needs.
///
/// Every registration is Singleton. The services are stateless aside
/// from the file paths they read/write; the view-model graph matches
/// the single MainWindow which lives for the process's lifetime;
/// nothing here would benefit from per-call instantiation.
/// </summary>
internal static class Composition
{
    /// <summary>
    /// Builds and returns the DI container for the running WPF
    /// application. Caller owns disposal: <see cref="App.OnStartup"/>
    /// holds the container in a static field and disposes in
    /// <see cref="App.OnExit"/>.
    /// </summary>
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Headless surface (Models, Services, Helpers, Interop) lives
        // in InstallerClean.Core and is registered via the extension.
        services.AddInstallerCleanCore();

        // WPF-only surfaces. These wrap MessageBox / Window types and
        // therefore cannot run without a WPF dispatcher.
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IConfirmationService, ConfirmationService>();
        services.AddSingleton<IWindowService, WindowService>();

        // View-model graph. MainViewModel constructs the four child
        // VMs internally; the container only needs to know how to
        // resolve MainViewModel itself.
        services.AddSingleton<MainViewModel>();

        // validateScopes: true catches a Scoped registration captured
        // by a Singleton; today every registration is Singleton.
        return services.BuildServiceProvider(validateScopes: true);
    }
}
