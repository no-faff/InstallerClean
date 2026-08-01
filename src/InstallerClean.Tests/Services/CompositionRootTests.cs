using Microsoft.Extensions.DependencyInjection;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Both composition roots, built the way the hosts build them. Nothing here
/// resolves a service: <c>ValidateOnBuild</c> creates a call site for every
/// registration and instantiates none of them, so this needs no dispatcher, no
/// STA thread and no WPF object graph, and it fails only for the reason it
/// exists to catch.
///
/// That reason: a constructor parameter added to a service or a view model
/// without its registration passes every other gate. The solution compiles,
/// the suite passes (subjects are assembled by hand with NSubstitute rather
/// than through the container), the publishes succeed because publishing never
/// runs the app, and the installer compiles. The first thing to notice would be
/// a user's machine, where the single <c>GetRequiredService&lt;MainViewModel&gt;</c>
/// throws and the startup catch paints a failure with only an exception type
/// name in it.
///
/// The GUI root is <c>internal</c>; <c>InternalsVisibleTo</c> on the WPF
/// assembly is what lets this reach it.
/// </summary>
public class CompositionRootTests
{
    [Fact]
    public void The_wpf_root_resolves_every_registration_it_declares()
    {
        var exception = Record.Exception(() =>
        {
            using var services = Composition.BuildServiceProvider();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void The_console_root_resolves_every_registration_it_declares()
    {
        // Built exactly as InstallerClean.Cli's Program.RunWorkAsync builds it,
        // options included: the headless surface alone, with none of the WPF
        // services layered on, so a Core service that had come to depend on one
        // of those would fail here and not in the GUI test above.
        var exception = Record.Exception(() =>
        {
            using var services = new ServiceCollection()
                .AddInstallerCleanCore()
                .BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true,
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Every_headless_registration_is_a_singleton()
    {
        // ValidateScopes catches only the Scoped-inside-Singleton case, so a
        // Transient registration would slip past both flags while breaking what
        // CoreComposition's own remarks promise. It matters most for the two
        // services holding a pooled HttpClient: one instance per resolve opens a
        // fresh connection pool each time and exhausts sockets under a retry.
        var services = new ServiceCollection().AddInstallerCleanCore();

        Assert.All(services, descriptor =>
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime));
    }
}
