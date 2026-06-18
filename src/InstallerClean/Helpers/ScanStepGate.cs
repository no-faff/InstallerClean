using System.Threading.Tasks;
using System.Windows.Threading;
using InstallerClean.Models;

namespace InstallerClean.Helpers;

/// <summary>
/// Wraps the startup-scan progress reporter so each milestone pauses for a
/// screenshot when <see cref="ScanStepConsole.IsEnabled"/> (a developer
/// aid). Every update is forwarded to the real reporter first, so the
/// window paints the phase, then a console Enter is awaited at milestones
/// only; the per-product ticker (<see cref="ScanProgressUpdate.IsMilestone"/>
/// false, hundreds per scan) passes straight through. When stepping is
/// off, <see cref="Wrap"/> hands back the inner reporter unchanged.
/// </summary>
internal sealed class ScanStepGate : IProgress<ScanProgressUpdate>
{
    private readonly IProgress<ScanProgressUpdate> _inner;
    private readonly Dispatcher _dispatcher;
    private int _step;

    private ScanStepGate(IProgress<ScanProgressUpdate> inner, Dispatcher dispatcher)
    {
        _inner = inner;
        _dispatcher = dispatcher;
        ScanStepConsole.EnsureConsole();
    }

    public static IProgress<ScanProgressUpdate> Wrap(IProgress<ScanProgressUpdate> inner, Dispatcher dispatcher) =>
        ScanStepConsole.IsEnabled ? new ScanStepGate(inner, dispatcher) : inner;

    public void Report(ScanProgressUpdate value)
    {
        _inner.Report(value);
        if (!value.IsMilestone) return;

        if (_dispatcher.CheckAccess())
        {
            // The first milestone is reported on the UI thread, before the
            // scan's first await. Blocking here would freeze the splash, so
            // pump the queue (keeping it painting) while a background read
            // waits for Enter, and end the pump once Enter is pressed.
            var frame = new DispatcherFrame();
            int step = ++_step;
            string message = value.Message;
            Task.Run(() =>
            {
                ScanStepConsole.Pause(step, message);
                _dispatcher.BeginInvoke(() => frame.Continue = false);
            });
            Dispatcher.PushFrame(frame);
        }
        else
        {
            // Reported on a background thread (every later milestone): the
            // UI thread is free to paint. A Background-priority round-trip
            // runs after the forwarded update and the render pass, so the
            // phase is on screen before the prompt; then block until Enter.
            _dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            ScanStepConsole.Pause(++_step, value.Message);
        }
    }
}
