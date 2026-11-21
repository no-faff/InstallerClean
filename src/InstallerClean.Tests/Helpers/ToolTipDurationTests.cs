using System.Windows;
using System.Windows.Controls;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// How long a tooltip stays open, read as WPF reads it in a domain targeting .NET Framework 4.8
/// with <c>OptOutOfWCAG21ToolTipBehavior</c> on, the switch under which WPF's own default is five
/// seconds. Each test runs in an AppDomain of its own, since the switch is read once per domain
/// and OverrideMetadata is refused once the property has been used on the type.
/// </summary>
public class ToolTipDurationTests
{
    private const string OptOutOfWCAG21ToolTipBehavior =
        "Switch.System.Windows.Controls.ToolTip.OptOutOfWCAG21ToolTipBehavior";

    [Fact]
    public void With_the_switch_on_wpf_s_own_default_is_five_seconds()
    {
        Assert.Equal(new[] { 5000, 5000, 5000 },
            FrameworkSwitchesTests.InDomain((DefaultProbe probe) => probe.Read()));
    }

    [Fact]
    public void With_the_switch_on_a_tooltip_stays_open_while_the_pointer_rests_on_any_element_the_app_hangs_one_off()
    {
        Assert.Equal(new[] { int.MaxValue, int.MaxValue, int.MaxValue },
            FrameworkSwitchesTests.InDomain((AppProbe probe) => probe.Read()));
    }

    private static int[] ShowDurations() =>
    [
        (int)ToolTipService.ShowDurationProperty.GetMetadata(typeof(Button)).DefaultValue,
        (int)ToolTipService.ShowDurationProperty.GetMetadata(typeof(TextBlock)).DefaultValue,
        (int)ToolTipService.ShowDurationProperty.GetMetadata(typeof(Border)).DefaultValue,
    ];

    /// <summary>Turns the switch on and reads WPF's own default, the app's code untouched.</summary>
    public sealed class DefaultProbe : MarshalByRefObject
    {
        public int[] Read()
        {
            AppContext.SetSwitch(OptOutOfWCAG21ToolTipBehavior, true);
            return ShowDurations();
        }
    }

    /// <summary>Turns the switch on, sets the duration as the window's start does, and reads it.</summary>
    public sealed class AppProbe : MarshalByRefObject
    {
        public int[] Read()
        {
            AppContext.SetSwitch(OptOutOfWCAG21ToolTipBehavior, true);
            InstallerClean.ToolTipDuration.KeepOpenWhileThePointerRests();
            return ShowDurations();
        }
    }
}
