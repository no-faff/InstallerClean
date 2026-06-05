using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;

namespace InstallerClean.Helpers;

/// <summary>
/// Process-wide, observable view of the Windows accessibility settings
/// WPF does not honour on its own. Today that is "show animations"
/// (<see cref="ReduceMotion"/>); the value refreshes live when the user
/// changes the setting while the app is running.
/// </summary>
/// <remarks>
/// One instance (<see cref="Current"/>) so XAML can bind through an
/// <c>x:Static</c> source against a single source of truth.
/// <see cref="PropertyChanged"/> always raises on the dispatcher thread
/// so bindings update where WPF expects them.
/// </remarks>
public sealed class AccessibilitySettings : INotifyPropertyChanged
{
    /// <summary>Shared instance for XAML binding and startup wiring.</summary>
    public static AccessibilitySettings Current { get; } = new();

    private bool _reduceMotion;

    private AccessibilitySettings()
    {
        // First read runs on the dispatcher thread (first access is from
        // startup or a XAML binding), so no marshal is needed here.
        ReadFromSystem();

        // WM_SETTINGCHANGE reaches managed code two ways and which one
        // carries a given toggle is not contractual: SystemParameters
        // invalidates its cached system values and raises
        // StaticPropertyChanged on the dispatcher thread, while
        // SystemEvents.UserPreferenceChanged covers the broader set and
        // can arrive on its own thread. Re-reading on either signal keeps
        // the result correct without depending on that routing.
        SystemParameters.StaticPropertyChanged += (_, _) => Refresh();
        SystemEvents.UserPreferenceChanged += (_, _) => Refresh();
    }

    /// <summary>
    /// True when Windows' "show animations" is off, i.e. animation should
    /// be suppressed. The inverse of
    /// <see cref="SystemParameters.ClientAreaAnimation"/>.
    /// </summary>
    public bool ReduceMotion
    {
        get => _reduceMotion;
        private set
        {
            if (_reduceMotion == value) return;
            _reduceMotion = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReduceMotion)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Refresh()
    {
        // SystemEvents can raise off the dispatcher thread; marshal so
        // PropertyChanged consumers (live bindings) update where WPF
        // expects them.
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.BeginInvoke((Action)ReadFromSystem);
        else
            ReadFromSystem();
    }

    private void ReadFromSystem()
    {
        ReduceMotion = !SystemParameters.ClientAreaAnimation;
    }
}
