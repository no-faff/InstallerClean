using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Threading;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The AppContext switches the window pins, read as .NET Framework reads them in a domain
/// targeting 4.8: each test runs in an AppDomain of its own, since a switch is read once per
/// domain and this test process loads the window's assembly, whose initializer pins them here
/// too. That an exe's own domain reports 4.8 to WPF rests on the exe's TargetFramework
/// attribute, checked here, and is read in the started window on Windows.
/// </summary>
public class FrameworkSwitchesTests
{
    private const string NetFramework48 = ".NETFramework,Version=v4.8";

    [Fact]
    public void A_4_8_target_reads_all_four_as_net_framework_sets_them_until_the_window_sets_them()
    {
        Assert.Equal(new[] { true, true, true, true }, InDomain(probe => probe.Read(pin: false)));
    }

    [Fact]
    public void The_window_s_switches_are_set_as_net_10_has_them()
    {
        Assert.Equal(new[] { false, false, false, false }, InDomain(probe => probe.Read(pin: true)));
    }

    [Fact]
    public void A_dispatcher_starts_without_an_error_once_the_window_s_switches_are_set()
    {
        Assert.Null(InDomain(probe => probe.DispatcherError()));
    }

    [Fact]
    public void The_window_s_module_initializer_sets_the_switches()
    {
        var initializer = typeof(InstallerClean.ProcessDefaults).GetMethod(
            "SetBeforeMain", BindingFlags.Static | BindingFlags.NonPublic)!;
        var pin = typeof(InstallerClean.FrameworkSwitches).GetMethod(
            nameof(InstallerClean.FrameworkSwitches.Pin), BindingFlags.Static | BindingFlags.NonPublic)!;

        // By name: on .NET Framework the attribute is PolySharp's, matched whichever copy a build takes.
        Assert.Contains(
            initializer.GetCustomAttributes(),
            a => a.GetType().FullName == "System.Runtime.CompilerServices.ModuleInitializerAttribute");
        Assert.Contains(pin, CallsIn(initializer));
    }

    // The methods a method body calls with the call opcode (0x28 and a metadata token).
    private static IEnumerable<MethodBase> CallsIn(MethodInfo method)
    {
        var il = method.GetMethodBody()!.GetILAsByteArray()!;
        for (var i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] != 0x28) continue;
            MethodBase? called = null;
            try
            {
                called = method.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1));
            }
            catch (ArgumentException)
            {
            }
            if (called is not null) yield return called;
        }
    }

    [Theory]
    [InlineData(typeof(InstallerClean.ProcessDefaults))]
    [InlineData(typeof(InstallerClean.Cli.LibrarySearch))]
    public void Each_exe_targets_net_framework_4_8(Type inExe)
    {
        Assert.Equal(NetFramework48, inExe.Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName);
    }

    private static T InDomain<T>(Func<Probe, T> run)
    {
        var setup = new AppDomainSetup
        {
            ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
            TargetFrameworkName = NetFramework48,
            // The test host's own file, for its binding redirects; it sets no switch.
            ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
        };
        var domain = AppDomain.CreateDomain("switches", null, setup);
        try
        {
            var probe = (Probe)domain.CreateInstanceAndUnwrap(
                typeof(Probe).Assembly.FullName!, typeof(Probe).FullName!);
            return run(probe);
        }
        finally
        {
            // A thread stuck in native code keeps the domain from unloading; the result above
            // still reaches the assertion.
            try
            {
                AppDomain.Unload(domain);
            }
            catch (CannotUnloadAppDomainException)
            {
            }
        }
    }

    /// <summary>Runs inside the test's own AppDomain and reads through WPF's own properties.</summary>
    public sealed class Probe : MarshalByRefObject
    {
        public bool[] Read(bool pin)
        {
            if (pin) InstallerClean.FrameworkSwitches.Pin();
            return
            [
                Switch(typeof(Dispatcher).Assembly, "System.Windows.AccessibilitySwitches", "UseNetFx48CompatibleAccessibilityFeatures"),
                Switch(typeof(Dispatcher).Assembly, "System.Windows.AccessibilitySwitches", "UseNetFx48aCompatibleAccessibilityFeatures"),
                Switch(typeof(Dispatcher).Assembly, "System.Windows.AccessibilitySwitches", "OptOutOfGridColumnResizeUsingKeyboard"),
                Switch(typeof(System.Windows.FrameworkElement).Assembly, "MS.Internal.FrameworkAppContextSwitches", "SelectorUpdatesSelectionPropertiesWhenDisconnected"),
            ];
        }

        /// <summary>
        /// Sets the window's switches, starts a Dispatcher on a thread of its own and runs it past
        /// Loaded, where .NET Framework raises a failed accessibility check. The name of what was
        /// raised, or null.
        /// </summary>
        public string? DispatcherError()
        {
            InstallerClean.FrameworkSwitches.Pin();

            string? raised = null;
            var thread = new Thread(() =>
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.UnhandledException += (_, e) =>
                {
                    raised ??= e.Exception.GetType().Name;
                    e.Handled = true;
                };
                var frame = new DispatcherFrame();
                dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
                dispatcher.InvokeShutdown();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(30)))
                return "the dispatcher did not stop";
            return raised;
        }

        private static bool Switch(Assembly assembly, string typeName, string property)
        {
            var type = assembly.GetType(typeName, throwOnError: true)!;
            var member = type.GetProperty(property, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new MissingMemberException(typeName, property);
            return (bool)member.GetValue(null)!;
        }
    }
}
