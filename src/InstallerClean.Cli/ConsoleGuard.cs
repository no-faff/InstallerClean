using System.Text;
using InstallerClean.Helpers;

namespace InstallerClean.Cli;

/// <summary>
/// Standard output for the whole run. Every write goes to the writer the console gave the
/// run, and a write that fails, such as one to an output redirected to a disk that is full,
/// loses what it was writing and nothing more: the next write is tried as if none had
/// failed, and goes through once the disk has room, which a Delete on that disk can give it.
/// Every write reaches the console's writer as one call, whichever overload made it, and
/// is guarded as one.
///
/// Nothing the run prints can throw, so no failed write ends a run or reaches a catch-all. A
/// Delete that deleted files is logged and exits as that Delete, and a run refused for
/// something that clears by itself keeps the code that tells a scheduler to come back. A run
/// whose output is what it is asked for, a scan-only run, --help or --version, reads
/// <see cref="FirstFailure"/> and exits 1 where a write failed.
///
/// The first failure is recorded in crash.log and the later ones are not. One cause, such
/// as a full disk, can refuse many writes in a row, and an entry each would spend
/// crash.log's rotation on copies of it.
///
/// A FAILED WRITE CAN STOP PART-WAY THROUGH A LINE. The console's writer sends a line longer
/// than its buffer out in pieces, so the pieces ahead of the failure are out and the rest of
/// the write, its line break included, is not. So after a failure
/// the guard puts a line break ahead of the next write, tried as part of that write: the
/// next text out starts a line of its own, and where the failure lost a whole line a blank
/// line stands in its place. Where the line break is refused too, the write it leads is not
/// made, since its text would join the cut line, and the next write tries again.
///
/// <see cref="Program"/>'s Main installs it once, after it sets the code page.
/// Setting <see cref="Console.OutputEncoding"/> replaces a writer the console made for
/// itself and keeps one installed through <see cref="Console.SetOut"/>, so with the code
/// page set first this guard wraps a writer that already writes UTF-8, and putting the
/// code page back at the end leaves it in place.
/// </summary>
internal sealed class ConsoleGuard(TextWriter inner) : TextWriter
{
    /// <summary>
    /// Whether the next write owes a line break: a write has failed and no line break has
    /// gone through since.
    /// </summary>
    private bool _lineCut;

    /// <summary>The first write that failed, or null where every write has gone through.</summary>
    internal OutputFailure? FirstFailure { get; private set; }

    /// <summary>Whether any write has failed.</summary>
    internal bool Failed => FirstFailure is not null;

    public override Encoding Encoding => inner.Encoding;

    public override IFormatProvider FormatProvider => inner.FormatProvider;

    public override void Write(char value) => Guard(value, static (w, v) => w.Write(v));

    public override void Write(char[] buffer, int index, int count) =>
        Guard((buffer, index, count), static (w, v) => w.Write(v.buffer, v.index, v.count));

    public override void Write(string? value) => Guard(value, static (w, v) => w.Write(v));

    public override void WriteLine() => Guard(0, static (w, _) => w.WriteLine());

    public override void WriteLine(string? value) => Guard(value, static (w, v) => w.WriteLine(v));

    // TextWriter writes each of the overloads below as more than one write: a line as its
    // text and then a line break. Each is overridden so it reaches the console's writer as
    // one call, inside one guard, and a failure part-way through it ends that write: no
    // second line break follows the one the failure leaves owing. A string builder reaches
    // TextWriter's Write and WriteLine of an object, which write its text as one string
    // through the string overrides above.

    public override void WriteLine(char value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void WriteLine(char[]? buffer) => Guard(buffer, static (w, v) => w.WriteLine(v));

    public override void WriteLine(char[] buffer, int index, int count) =>
        Guard((buffer, index, count), static (w, v) => w.WriteLine(v.buffer, v.index, v.count));

    public override void WriteLine(bool value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void WriteLine(int value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void WriteLine(uint value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void WriteLine(long value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void WriteLine(ulong value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void WriteLine(float value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void WriteLine(double value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void WriteLine(decimal value) => Guard(value, static (w, v) => w.WriteLine(v));

    public override void Flush()
    {
        try
        {
            inner.Flush();
        }
        catch (Exception ex)
        {
            Record(ex);
        }
    }

    /// <summary>
    /// Makes one write, with the line break a failure leaves owing ahead of it. Takes the
    /// value and a static write rather than a closure, so no write allocates.
    /// </summary>
    private void Guard<T>(T value, Action<TextWriter, T> write)
    {
        try
        {
            if (_lineCut)
            {
                inner.WriteLine();
                _lineCut = false;
            }
            write(inner, value);
        }
        catch (Exception ex)
        {
            Record(ex);
        }
    }

    private void Record(Exception ex)
    {
        _lineCut = true;
        if (Failed) return;
        FirstFailure = new OutputFailure(ex, CrashLog.TryWrite(ex));
    }
}

/// <summary>
/// A write to the run's output that failed: the exception it threw, and where crash.log
/// recorded it, if it could.
/// </summary>
internal sealed record OutputFailure(Exception Exception, (string Path, bool Written) CrashLog);
