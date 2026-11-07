using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows.Threading;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The AppContext switches the window pins, read as .NET Framework reads them in a process
/// targeting 4.8 whose .config sets no switch: each test runs in an AppDomain of its own, since a switch is
/// read once per domain and this test process loads the window's assembly, which pins them here
/// too.
/// </summary>
public class FrameworkSwitchesTests
{
    private const string NetFramework48 = ".NETFramework,Version=v4.8";

    [Fact]
    public void Without_the_window_s_initializer_a_4_8_target_reads_all_four_as_net_framework_sets_them()
    {
        var read = InDomain(probe => probe.Read(pin: false));

        Assert.Equal(NetFramework48, read.TargetFrameworkName);
        Assert.Equal(new[] { true, true, true, true }, read.Switches);
    }

    [Fact]
    public void The_window_s_initializer_sets_all_four_as_net_10_has_them()
    {
        var read = InDomain(probe => probe.Read(pin: true));

        Assert.Equal(NetFramework48, read.TargetFrameworkName);
        Assert.Equal(new[] { false, false, false, false }, read.Switches);
    }

    [Fact]
    public void A_dispatcher_starts_without_an_error_once_the_window_s_switches_are_set()
    {
        Assert.Null(InDomain(probe => probe.DispatcherError(pinWindow: true, columnResizeOnly: false)));
    }

    [Fact]
    public void The_column_resize_set_alone_fails_the_accessibility_check_at_dispatcher_start()
    {
        Assert.Equal(
            nameof(NotSupportedException),
            InDomain(probe => probe.DispatcherError(pinWindow: false, columnResizeOnly: true)));
    }

    [Theory]
    [InlineData(typeof(InstallerClean.LibrarySearch))]
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
            AppDomain.Unload(domain);
        }
    }

    [Serializable]
    public sealed class SwitchRead
    {
        public string? TargetFrameworkName { get; set; }

        public bool[] Switches { get; set; } = [];
    }

    /// <summary>Runs inside the test's own AppDomain and reads through WPF's own properties.</summary>
    public sealed class Probe : MarshalByRefObject
    {
        public SwitchRead Read(bool pin)
        {
            if (pin) RunWindowInitializer();
            return new SwitchRead
            {
                TargetFrameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName,
                Switches =
                [
                    Switch(typeof(Dispatcher).Assembly, "System.Windows.AccessibilitySwitches", "UseNetFx48CompatibleAccessibilityFeatures"),
                    Switch(typeof(Dispatcher).Assembly, "System.Windows.AccessibilitySwitches", "UseNetFx48aCompatibleAccessibilityFeatures"),
                    Switch(typeof(Dispatcher).Assembly, "System.Windows.AccessibilitySwitches", "OptOutOfGridColumnResizeUsingKeyboard"),
                    Switch(typeof(System.Windows.FrameworkElement).Assembly, "MS.Internal.FrameworkAppContextSwitches", "SelectorUpdatesSelectionPropertiesWhenDisconnected"),
                ],
            };
        }

        /// <summary>
        /// Starts a Dispatcher on a thread of its own and runs it past Loaded, where .NET
        /// Framework raises a failed accessibility check. The name of what was raised, or null.
        /// </summary>
        public string? DispatcherError(bool pinWindow, bool columnResizeOnly)
        {
            if (pinWindow) RunWindowInitializer();
            if (columnResizeOnly)
                AppContext.SetSwitch("Switch.System.Windows.Controls.OptOutOfGridColumnResizeUsingKeyboard", false);

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

        private static void RunWindowInitializer() =>
            RuntimeHelpers.RunModuleConstructor(typeof(InstallerClean.LibrarySearch).Module.ModuleHandle);

        private static bool Switch(Assembly assembly, string typeName, string property)
        {
            var type = assembly.GetType(typeName, throwOnError: true)!;
            var member = type.GetProperty(property, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new MissingMemberException(typeName, property);
            return (bool)member.GetValue(null)!;
        }
    }
}
