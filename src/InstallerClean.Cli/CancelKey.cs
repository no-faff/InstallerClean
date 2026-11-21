using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Cli;

/// <summary>
/// The command line's one handler for Ctrl+C, hooked on the first line of
/// <see cref="Program.Main"/> and kept for the whole process. Windows ends a console
/// process that has no handler for Ctrl+C at once, with no summary entry and the
/// console's code page not handed back, so every Ctrl+C is taken here and keeps the
/// process running. The first one cancels <see cref="Pressed"/>, as does the event
/// <see cref="Watch"/> is given, and a run answers it
/// through <see cref="Answer"/>, which calls back at once where the Ctrl+C came first. A
/// run that ends without answering, such as one printing its help, finishes as it would
/// have.
/// <para>
/// A console closed under the run, or a shutdown, ends it the same way: it cancels
/// <see cref="Pressed"/>, and the thread Windows gives the event is held for
/// <see cref="CloseHold"/> so the run can end at its next file and write its entry before
/// Windows ends the process, which it does once the handler returns or its time is up. A
/// logoff is taken and the run carries on, since the event does not say whose session is
/// ending.
/// </para>
/// </summary>
internal sealed class CancelKey
{
    private readonly CancellationTokenSource _pressed = new();
    private readonly Func<string, WaitHandle?> _openEvent;
    private readonly Action<TimeSpan> _hold;
    private int _hooked;
    private int _closing;
    // What cancelled Pressed first: 0 nothing yet; 1 Ctrl+C, or the starting version's
    // cancel event (Watch), which that version sets for its own Ctrl+C or close; 2 a close
    // or shutdown of this process's console, recorded before Closing is set.
    private int _firstPress;

    /// <summary>
    /// How long a close or shutdown event holds the process: a second inside the five
    /// seconds Windows allows a console close by default.
    /// </summary>
    internal static readonly TimeSpan CloseHold = Durations.FromSeconds(4);

    public CancelKey() : this(OpenEvent, Thread.Sleep) { }

    /// <summary>Test seam: how <see cref="Watch"/> opens the event a name names.</summary>
    internal CancelKey(Func<string, WaitHandle?> openEvent) : this(openEvent, Thread.Sleep) { }

    /// <summary>Test seam: also how a close or shutdown event holds its thread.</summary>
    internal CancelKey(Func<string, WaitHandle?> openEvent, Action<TimeSpan> hold)
    {
        _openEvent = openEvent;
        _hold = hold;
    }

    /// <summary>
    /// Whether the console is being closed or Windows is shutting down. A run then prints
    /// no "Cancelling" line and writes its entry before its last lines, since Windows ends
    /// the process within seconds.
    /// </summary>
    public bool Closing => Volatile.Read(ref _closing) == 1;

    /// <summary>
    /// Whether a close or shutdown, rather than Ctrl+C, is what cancelled
    /// <see cref="Pressed"/>. Settled before <see cref="Closing"/> is set and before the
    /// cancellation it names.
    /// </summary>
    public bool ClosedFirst => Volatile.Read(ref _firstPress) == 2;

    /// <summary>The handler the command line hooks.</summary>
    public static CancelKey Process { get; } = new();

    /// <summary>Cancelled by the first Ctrl+C; later ones change nothing.</summary>
    public CancellationToken Pressed => _pressed.Token;

    /// <summary>
    /// Hooks the handler. A second call does nothing, since <see cref="Program.Main"/>
    /// can run more than once in one process. Never throws.
    /// </summary>
    public void Hook()
    {
        if (Interlocked.Exchange(ref _hooked, 1) == 1) return;
        try
        {
            Console.CancelKeyPress += Take;
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
        }
        // After CancelKeyPress, so this handler is asked first and passes Ctrl+C and
        // Ctrl+Break on to the one CancelKeyPress registers.
        ConsoleControl.TryRegister(Control);
    }

    /// <summary>
    /// Answers a console control event, on the thread Windows gives it, and answers whether
    /// it was taken. Ctrl+C and Ctrl+Break are not, and reach <see cref="Take"/>. A close
    /// or shutdown cancels <see cref="Pressed"/> and holds this thread for
    /// <see cref="CloseHold"/>; the process ends as the run returns, or as Windows ends it.
    /// Never throws.
    /// </summary>
    internal bool Control(uint ctrlType)
    {
        switch (ctrlType)
        {
            case Kernel32.CTRL_LOGOFF_EVENT:
                return true;
            case Kernel32.CTRL_CLOSE_EVENT or Kernel32.CTRL_SHUTDOWN_EVENT:
                Interlocked.CompareExchange(ref _firstPress, 2, 0);
                Volatile.Write(ref _closing, 1);
                Press();
                try
                {
                    _hold(CloseHold);
                }
                catch (Exception ex)
                {
                    Helpers.CrashLog.TryWrite(ex);
                }
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Runs <paramref name="answer"/> once, on the first Ctrl+C, or at once where it has
    /// already come. Disposing what this returns releases it without waiting for a call
    /// already running, so a run can return while its answer is held up on a console
    /// write.
    /// </summary>
    public Registration Answer(Action answer) => new(Pressed.Register(answer));

    /// <summary>A run's answer to Ctrl+C, released on <see cref="Dispose"/>.</summary>
    public readonly struct Registration(CancellationTokenRegistration registration) : IDisposable
    {
        public void Dispose() => registration.Unregister();
    }

    /// <summary>
    /// What Ctrl+C does: keeps the process running and cancels <see cref="Pressed"/>,
    /// running the answer registered on it. The runtime runs it on a thread-pool thread
    /// while the console's control-handler thread waits for it, and a throw there is
    /// unhandled, so it never throws.
    /// </summary>
    internal void Take(object? sender, ConsoleCancelEventArgs cancelArgs)
    {
        cancelArgs.Cancel = true;
        Press();
    }

    /// <summary>
    /// Also cancels <see cref="Pressed"/> once the event <paramref name="id"/> stands for
    /// is set. The version that started this one creates it and sets it on Ctrl+C, so a
    /// Ctrl+C the console gave that version before this process was connected to it still
    /// stops this one, and one set before this process opened it is seen as it opens it.
    /// The event is waited on by a thread of its own for the life of the process. An id
    /// naming no event this process can open is written to crash.log, and Ctrl+C alone
    /// stops the run. Never throws.
    /// </summary>
    public void Watch(string id)
    {
        try
        {
            if (!CliContract.TryReadCancelEvent(id, out var name))
                throw new FormatException("The value passed with the cancel-event switch is not an event's id.");
            var watched = _openEvent(name)
                ?? throw new InvalidOperationException("The event passed with the cancel-event switch does not exist.");
            // One set already is answered here, before the run reads Pressed.
            if (watched.WaitOne(0))
            {
                Press();
                return;
            }
            new Thread(() =>
            {
                try
                {
                    if (watched.WaitOne()) Press();
                }
                catch (Exception ex)
                {
                    Helpers.CrashLog.TryWrite(ex);
                }
            }) { IsBackground = true, Name = "Ctrl+C from the starting version" }.Start();
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
        }
    }

    /// <summary>Cancels <see cref="Pressed"/>. Never throws.</summary>
    private void Press()
    {
        Interlocked.CompareExchange(ref _firstPress, 1, 0);
        try
        {
            _pressed.Cancel();
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
        }
    }

    /// <summary>Opens the event <paramref name="name"/> names, or answers null where there is none.</summary>
    private static WaitHandle? OpenEvent(string name) =>
        EventWaitHandle.TryOpenExisting(name, out var opened) ? opened : null;
}
