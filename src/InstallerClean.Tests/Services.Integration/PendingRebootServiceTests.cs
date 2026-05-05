using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>Smoke coverage: PendingRebootService composes through DI and doesn't throw against the real registry and mutex.</summary>
public class PendingRebootServiceTests
{
    private static IPendingRebootService BuildFromDi()
    {
        var services = new ServiceCollection().AddInstallerCleanCore().BuildServiceProvider();
        return services.GetRequiredService<IPendingRebootService>();
    }

    [Fact]
    public void Check_does_not_throw_against_the_real_registry_and_mutex()
    {
        var svc = BuildFromDi();

        var exception = Record.Exception(() => svc.Check());

        Assert.Null(exception);
    }

    [Fact]
    public void Check_returns_consistent_result_on_repeated_calls()
    {
        var svc = BuildFromDi();

        var first = svc.Check();
        var second = svc.Check();

        // Real registry and mutex state is stable across two immediate calls (rare CI flap if
        // an MSI install starts or completes between them, or if PendingFileRenameOperations
        // is touched in the gap).
        Assert.Equal(first.Verdict, second.Verdict);
        Assert.Equal(first.Reason, second.Reason);
    }

    [Fact]
    public void Check_two_independent_instances_agree()
    {
        var svc1 = BuildFromDi();
        var svc2 = BuildFromDi();

        var r1 = svc1.Check();
        var r2 = svc2.Check();

        Assert.Equal(r1.Verdict, r2.Verdict);
        Assert.Equal(r1.Reason, r2.Reason);
    }
}
