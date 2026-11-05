using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace InstallerClean.Helpers;

/// <summary>
/// Runs the stripe across a busy progress bar. Set <c>IsRunning</c> on the
/// stripe, an element whose parent is the bar's track and whose left edge sits
/// on the track's left edge, and it crosses the track by the
/// <see cref="SweepTravel"/> figures, over and over, until the property is
/// cleared.
///
/// The ends of the run are read from the track when the run starts, because a
/// storyboard in a control template is frozen and its From and To cannot follow
/// the width the bar is drawn at. A change in the track's width starts the run
/// again from the left with the new ends.
///
/// The run goes only while the stripe can be seen: loaded and visible, which an
/// ancestor collapsed, such as the scanning card once a scan ends, makes it not.
/// Clearing the property, the stripe leaving the window or the stripe stopping
/// being visible removes the animation and the size handler, so nothing keeps
/// the render loop running or holds the bar while it cannot be seen. Collapsing
/// an ancestor raises no Unloaded, which is why visibility is followed as well. A
/// stripe that is seen again with the property still set starts again.
/// </summary>
public static class IndeterminateSweep
{
    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.RegisterAttached(
            "IsRunning", typeof(bool), typeof(IndeterminateSweep),
            new PropertyMetadata(false, OnIsRunningChanged));

    public static bool GetIsRunning(DependencyObject element) =>
        (bool)element.GetValue(IsRunningProperty);

    public static void SetIsRunning(DependencyObject element, bool value) =>
        element.SetValue(IsRunningProperty, value);

    // The track a running stripe listens to, set on the stripe, and the stripe a
    // track's size handler restarts, set on the track. Held both ways so the run
    // can be taken down from either end.
    private static readonly DependencyProperty TrackProperty =
        DependencyProperty.RegisterAttached(
            "Track", typeof(FrameworkElement), typeof(IndeterminateSweep));

    private static readonly DependencyProperty StripeProperty =
        DependencyProperty.RegisterAttached(
            "Stripe", typeof(FrameworkElement), typeof(IndeterminateSweep));

    private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement stripe) return;

        if ((bool)e.NewValue)
        {
            stripe.Loaded += OnStripeLoaded;
            stripe.Unloaded += OnStripeUnloaded;
            stripe.IsVisibleChanged += OnStripeIsVisibleChanged;
            // A stripe in a template that is applied before the window shows is
            // not loaded yet, and its Loaded starts the run; one under a
            // collapsed card starts when the card shows.
            if (CanBeSeen(stripe)) Attach(stripe);
        }
        else
        {
            stripe.Loaded -= OnStripeLoaded;
            stripe.Unloaded -= OnStripeUnloaded;
            stripe.IsVisibleChanged -= OnStripeIsVisibleChanged;
            Detach(stripe);
        }
    }

    private static bool CanBeSeen(FrameworkElement stripe) => stripe.IsLoaded && stripe.IsVisible;

    private static void OnStripeLoaded(object sender, RoutedEventArgs e)
    {
        var stripe = (FrameworkElement)sender;
        if (CanBeSeen(stripe)) Attach(stripe);
    }

    private static void OnStripeIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var stripe = (FrameworkElement)sender;
        if (CanBeSeen(stripe)) Attach(stripe);
        else Detach(stripe);
    }

    private static void OnStripeUnloaded(object sender, RoutedEventArgs e) =>
        Detach((FrameworkElement)sender);

    private static void Attach(FrameworkElement stripe)
    {
        Detach(stripe);
        if (VisualTreeHelper.GetParent(stripe) is not FrameworkElement track) return;

        stripe.SetValue(TrackProperty, track);
        track.SetValue(StripeProperty, stripe);
        track.SizeChanged += OnTrackSizeChanged;
        Run(stripe, track);
    }

    private static void Detach(FrameworkElement stripe)
    {
        if (stripe.GetValue(TrackProperty) is FrameworkElement track)
        {
            track.SizeChanged -= OnTrackSizeChanged;
            track.ClearValue(StripeProperty);
            stripe.ClearValue(TrackProperty);
        }
        (stripe.RenderTransform as TranslateTransform)?.BeginAnimation(TranslateTransform.XProperty, null);
    }

    private static void OnTrackSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged) return;
        var track = (FrameworkElement)sender;
        if (track.GetValue(StripeProperty) is FrameworkElement stripe)
            Run(stripe, track);
    }

    private static void Run(FrameworkElement stripe, FrameworkElement track)
    {
        // A track not laid out yet has no width to cross, and its first layout
        // raises the size change that starts the run.
        if (track.ActualWidth <= 0) return;

        if (stripe.RenderTransform is not TranslateTransform transform || transform.IsFrozen)
        {
            transform = new TranslateTransform();
            stripe.RenderTransform = transform;
        }

        // The stripe's own Width, where it has one, because the trigger that
        // starts the run shows the stripe in the same pass and a stripe still
        // collapsed has no ActualWidth.
        var stripeWidth = double.IsNaN(stripe.Width) ? stripe.ActualWidth : stripe.Width;
        var (from, to, duration) = SweepTravel.For(stripeWidth, track.ActualWidth);

        // BeginAnimation replaces whatever animation X already has, so a run
        // started again takes over from the one before it.
        transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(from, to, duration)
        {
            RepeatBehavior = RepeatBehavior.Forever,
        });
    }
}
