using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Hand fake for <see cref="IRecordsWatchFactory"/>, so a test says what each watch
/// reads and when, with no registry behind it. Every watch it arms is kept in
/// <see cref="Armed"/>, in order, so a test can see how many were armed, how often each
/// was read and that each was disposed of.
/// </summary>
internal sealed class FakeRecordsWatchFactory : IRecordsWatchFactory
{
    private readonly Func<int, FakeRecordsWatch> _make;

    /// <summary>Every watch reads <see cref="RecordsWatchReading.Unchanged"/>.</summary>
    public FakeRecordsWatchFactory() : this(_ => new FakeRecordsWatch()) { }

    /// <summary>
    /// <paramref name="make"/> builds each watch, given how many were armed before it:
    /// 0 for the first.
    /// </summary>
    public FakeRecordsWatchFactory(Func<int, FakeRecordsWatch> make) => _make = make;

    public List<FakeRecordsWatch> Armed { get; } = [];

    public IRecordsWatch Arm()
    {
        var watch = _make(Armed.Count);
        Armed.Add(watch);
        return watch;
    }
}

/// <summary>
/// A watch whose reading a test sets. <see cref="Reading"/> is what the next read
/// answers; <see cref="OnRead"/> runs at the start of each read with the number of that
/// read, 1 for the first, and may change <see cref="Reading"/>, which is how a test has
/// the records change between two particular looks. Once the reading is
/// <see cref="RecordsWatchReading.Changed"/> it stays and <see cref="OnRead"/> runs no
/// more, as the app's own watch keeps that answer; and once disposed of it reads
/// <see cref="RecordsWatchReading.Unestablished"/>, as the app's own does.
/// </summary>
internal sealed class FakeRecordsWatch : IRecordsWatch
{
    public RecordsWatchReading Reading { get; set; } = RecordsWatchReading.Unchanged;

    public Action<FakeRecordsWatch, int>? OnRead { get; set; }

    public int Reads { get; private set; }

    public int Disposals { get; private set; }

    public bool Disposed => Disposals > 0;

    public RecordsWatchReading Read()
    {
        Reads++;
        if (Disposed) return RecordsWatchReading.Unestablished;
        if (Reading != RecordsWatchReading.Changed) OnRead?.Invoke(this, Reads);
        return Reading;
    }

    public void Dispose() => Disposals++;
}
