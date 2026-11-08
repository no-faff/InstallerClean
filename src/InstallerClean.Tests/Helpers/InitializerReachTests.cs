using System.Reflection;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Neither exe's module initializer reaches anything that fills in the runtime's AppContext
/// defaults. On .NET Framework the domain does not know its target framework while the
/// initializer runs, so the first such call there would fix the process's compatibility
/// defaults at those of .NET Framework 4.0 (legacy path handling, a 260-character limit).
/// Every member of another assembly the initializer reaches is read, through the app's own code
/// to any depth.
/// </summary>
public class InitializerReachTests
{
    public static TheoryData<Type> Initializers => new()
    {
        typeof(InstallerClean.ProcessDefaults),
        typeof(InstallerClean.Cli.LibrarySearch),
    };

    [Theory]
    [MemberData(nameof(Initializers))]
    public void The_module_initializer_reaches_nothing_that_fills_in_AppContext_defaults(Type initializer)
    {
        var reach = InitializerReach.From(SetBeforeMain(initializer));

        // A walk that read nothing, or met a method it could not read, has not shown anything.
        Assert.True(reach.Own.Count > 1, $"Only {reach.Own.Count} method of the app's own was read.");
        Assert.Empty(reach.Unfollowed);
        Assert.NotEmpty(reach.Outside);
        Assert.Empty(reach.Outside
            .Where(r => InitializerReach.FillsInAppContextDefaults(r.Member))
            .Select(r => $"{r.Member.DeclaringType}.{r.Member.Name} from {r.From.DeclaringType}.{r.From.Name}"));
    }

    [Fact]
    public void The_check_finds_such_a_call_two_levels_below_where_it_starts()
    {
        // CrashLog.TryWrite reaches System.IO through the app's own code, as the initializer's
        // crash-log write did when it was made there.
        var reach = InitializerReach.From(
            typeof(InstallerClean.Helpers.CrashLog).GetMethod(nameof(InstallerClean.Helpers.CrashLog.TryWrite))!);

        Assert.Contains(reach.Outside, r => InitializerReach.FillsInAppContextDefaults(r.Member));
    }

    [Fact]
    public void The_window_s_switches_are_pinned_from_App_s_type_initializer_not_its_module_initializer()
    {
        var pin = typeof(InstallerClean.FrameworkSwitches).GetMethod(
            nameof(InstallerClean.FrameworkSwitches.Pin), BindingFlags.Static | BindingFlags.NonPublic)!;

        Assert.DoesNotContain(pin, InitializerReach.From(SetBeforeMain(typeof(InstallerClean.ProcessDefaults))).Own);
        Assert.Contains(pin, InitializerReach.From(typeof(InstallerClean.App).TypeInitializer!).Own);
    }

    private static MethodInfo SetBeforeMain(Type initializer)
    {
        var method = initializer.GetMethod("SetBeforeMain", BindingFlags.Static | BindingFlags.NonPublic)!;
        // By name: on .NET Framework the attribute is PolySharp's, matched whichever copy a build takes.
        Assert.Contains(
            method.GetCustomAttributes(),
            a => a.GetType().FullName == "System.Runtime.CompilerServices.ModuleInitializerAttribute");
        return method;
    }
}
