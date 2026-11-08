using System.Reflection;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// No module initializer in the assemblies shipped beside the exes reaches anything that fills
/// in the runtime's AppContext defaults. On .NET Framework the domain does not know its target
/// framework while an exe's initializer runs, so the first such call there would fix the
/// process's compatibility defaults at those of .NET Framework 4.0 (legacy path handling, a
/// 260-character limit). Every member of another assembly an initializer reaches, through the
/// app's own code to any depth, must be on a short list known not to reach AppContext.
/// </summary>
public class InitializerReachTests
{
    private static readonly Assembly Window = typeof(InstallerClean.ProcessDefaults).Assembly;
    private static readonly Assembly CommandLine = typeof(InstallerClean.Cli.LibrarySearch).Assembly;

    [Fact]
    public void No_module_initializer_shipped_with_the_exes_reaches_anything_that_fills_in_AppContext_defaults()
    {
        var shipped = InitializerReach.ShippedClosure(Window, CommandLine);
        var initializers = shipped.SelectMany(InitializerReach.ModuleTypeInitializers).ToList();

        // Each exe has one, so fewer than two means the search lost a source.
        Assert.True(initializers.Count >= 2,
            $"Module type initializers found: {Describe(initializers)}, in {string.Join(", ", shipped.Select(a => a.GetName().Name))}.");
        Assert.Contains(initializers, m => m.Module.Assembly == Window);
        Assert.Contains(initializers, m => m.Module.Assembly == CommandLine);

        // Another assembly's initializer runs when that assembly is first touched, which can come
        // before Main, and its code is not followed by the walk below.
        Assert.Empty(initializers.Where(m => !InitializerReach.IsOwn(m)).Select(m => m.Module.Assembly.GetName().Name));

        var reaches = initializers.Select(InitializerReach.From).ToList();

        // A method marked as a module initializer that no module type initializer calls has not
        // been read.
        Assert.Empty(shipped.SelectMany(InitializerReach.MarkedModuleInitializers)
            .Where(marked => !reaches.Any(r => r.Own.Contains(marked)))
            .Select(m => $"{m.DeclaringType}.{m.Name}"));

        foreach (var (initializer, reach) in initializers.Zip(reaches, (i, r) => (i, r)))
        {
            var where = $"the module initializer of {initializer.Module.Assembly.GetName().Name}";
            // A walk that read nothing, or met a method it could not read, has not shown anything.
            Assert.True(reach.Own.Count >= 2, $"Only {reach.Own.Count} method of the app's own was read from {where}.");
            Assert.Empty(reach.Unfollowed);
            Assert.NotEmpty(reach.Outside);
            Assert.Empty(reach.Outside
                .Where(r => !InitializerReach.KnownNotToReachAppContext(r.Member))
                .Select(r => $"{r.Member.DeclaringType}.{r.Member.Name} from {r.From.DeclaringType}.{r.From.Name}, in {where}"));
        }
    }

    [Fact]
    public void The_search_finds_each_module_initializer_of_an_assembly_that_is_not_an_exe()
    {
        // Each of this assembly's module initializers, an internal one among them, is run by its
        // one module type initializer.
        var tests = typeof(InitializerReachTests).Assembly;
        var initializer = Assert.Single(InitializerReach.ModuleTypeInitializers(tests));
        var marked = InitializerReach.MarkedModuleInitializers(tests);
        var reach = InitializerReach.From(initializer);

        Assert.Contains(marked, m => m.DeclaringType == typeof(TestCulture) && m.Name == nameof(TestCulture.Pin));
        Assert.All(marked, m => Assert.Contains(m, reach.Own));
    }

    [Fact]
    public void The_check_finds_such_a_call_two_levels_below_where_it_starts()
    {
        // CrashLog.TryWrite reaches System.IO through the app's own code, as the initializer's
        // crash-log write did when it was made there.
        var reach = InitializerReach.From(
            typeof(InstallerClean.Helpers.CrashLog).GetMethod(nameof(InstallerClean.Helpers.CrashLog.TryWrite))!);

        Assert.Contains(reach.Outside, r => !InitializerReach.KnownNotToReachAppContext(r.Member));
    }

    [Fact]
    public void Only_the_exception_constructors_that_take_their_message_as_given_are_on_the_list()
    {
        Assert.True(InitializerReach.KnownNotToReachAppContext(
            typeof(InvalidOperationException).GetConstructor([typeof(string)])!));
        Assert.True(InitializerReach.KnownNotToReachAppContext(
            typeof(System.ComponentModel.Win32Exception).GetConstructor([typeof(int), typeof(string)])!));

        Assert.False(InitializerReach.KnownNotToReachAppContext(
            typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes)!));
        Assert.False(InitializerReach.KnownNotToReachAppContext(
            typeof(System.ComponentModel.Win32Exception).GetConstructor([typeof(int)])!));
    }

    [Fact]
    public void The_window_s_switches_are_pinned_from_App_s_type_initializer_not_its_module_initializer()
    {
        var pin = typeof(InstallerClean.FrameworkSwitches).GetMethod(
            nameof(InstallerClean.FrameworkSwitches.Pin), BindingFlags.Static | BindingFlags.NonPublic)!;
        var windowInitializer = Assert.Single(InitializerReach.ModuleTypeInitializers(Window));

        Assert.DoesNotContain(pin, InitializerReach.From(windowInitializer).Own);
        Assert.Contains(pin, InitializerReach.From(typeof(InstallerClean.App).TypeInitializer!).Own);
    }

    private static string Describe(IEnumerable<MethodBase> methods) =>
        string.Join(", ", methods.Select(m => $"{m.Module.Assembly.GetName().Name} {m.Name}"));
}
