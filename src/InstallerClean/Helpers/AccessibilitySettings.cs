using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;

namespace InstallerClean.Helpers;

/// <summary>
/// Process-wide, observable view of the Windows accessibility settings
/// WPF does not honour on its own: "show animations"
/// (<see cref="ReduceMotion"/>) and "make text bigger"
/// (<see cref="TextScaleFactor"/>). Both refresh live when the user
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
    private double _textScaleFactor = 1.0;

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

    /// <summary>
    /// OS text-scale multiplier (1.0 = 100%). Font sizing multiplies by
    /// this to honour the "Make text bigger" accessibility slider, which
    /// WPF does not apply on its own.
    /// </summary>
    public double TextScaleFactor
    {
        get => _textScaleFactor;
        private set
        {
            if (_textScaleFactor == value) return;
            _textScaleFactor = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextScaleFactor)));
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
        TextScaleFactor = ReadTextScaleFactor();
    }

    private static double ReadTextScaleFactor()
    {
        // HKCU\SOFTWARE\Microsoft\Accessibility\TextScaleFactor holds the
        // percentage (100..225) behind Settings > Accessibility > Text
        // size. Absent means the slider was never moved, i.e. 100%.
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Accessibility");
            if (key?.GetValue("TextScaleFactor") is int percent && percent > 0)
                return percent / 100.0;
        }
        catch (Exception)
        {
            // A read failure (locked-down hive, transient denial) degrades
            // to no scaling rather than taking the app down for a cosmetic
            // setting.
        }
        return 1.0;
    }
}
