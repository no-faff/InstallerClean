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
/// MainWindow, no dialogs, no DataContext bindings) shares the
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

        // WPF-only surfaces. These wrap Window types and therefore cannot
        // run without a WPF dispatcher.
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IConfirmationService, ConfirmationService>();
        services.AddSingleton<IWindowService, WindowService>();

        // View-model graph. MainViewModel constructs the four child
        // VMs internally; the container only needs to know how to
        // resolve MainViewModel itself.
        services.AddSingleton<MainViewModel>();

        // ValidateScopes catches a Scoped registration captured by a
        // Singleton. The current graph is Singleton-only; the check is
        // cheap insurance against a future Scoped addition.
        //
        // ValidateOnBuild is the one earning its keep today. It builds a
        // call site for every registration above without instantiating
        // any of them, so a constructor parameter added without its
        // registration fails here, naming the service it could not
        // resolve. Without it the first thing to notice is the
        // GetRequiredService<MainViewModel> a few lines later in
        // App.OnStartup, whose whole visible surface is "Failed to start"
        // and an exception type name: the app's worst failure mode
        // reached by its least visible mistake, and one no other gate
        // catches, because the solution still compiles, the tests build
        // their subjects by hand rather than through the container, and
        // publishing never runs the app. CompositionRootTests builds both
        // roots so it lands on CI instead of a user's machine.
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
    }
}
