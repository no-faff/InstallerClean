namespace InstallerClean.Services;

/// <summary>
/// A watch on the registry keys Windows Installer keeps its records in: which program
/// and which patch uses which cached file. A read of those records made after the watch
/// is armed stays true of them for as long as the watch reads
/// <see cref="RecordsWatchReading.Unchanged"/>.
///
/// IT ANSWERS ONCE. The notification behind it reports a single change, so a watch that
/// has answered <see cref="RecordsWatchReading.Changed"/> is disposed of, and the next
/// check arms a new one. Nothing arms one a second time.
/// </summary>
public interface IRecordsWatch : IDisposable
{
    /// <summary>
    /// Whether anything has been written under the watched keys since the watch was
    /// armed. It answers at once and never waits.
    /// </summary>
    RecordsWatchReading Read();
}

/// <summary>What a read of an <see cref="IRecordsWatch"/> found.</summary>
public enum RecordsWatchReading
{
    /// <summary>
    /// The watch could not be set on every key, or a key refused one of the rights the
    /// watch needs, so whether the records have changed is not established. First member
    /// so that it is what the type's own zero carries.
    /// </summary>
    Unestablished,

    /// <summary>Nothing has been written under any watched key since the watch was armed.</summary>
    Unchanged,

    /// <summary>
    /// Something has been written under a watched key, a watched key no longer answers,
    /// or the accounts whose registry hives are loaded are not the ones loaded when the
    /// watch was armed.
    /// </summary>
    Changed,
}

/// <summary>Arms an <see cref="IRecordsWatch"/>.</summary>
public interface IRecordsWatchFactory
{
    /// <summary>
    /// Arms a watch on every key Windows Installer keeps its records in, and returns it.
    /// Never throws and never returns null: a watch that could not be armed is returned
    /// reading <see cref="RecordsWatchReading.Unestablished"/>. The caller disposes of
    /// it.
    /// </summary>
    IRecordsWatch Arm();
}

/// <summary>
/// A factory whose watches always read <see cref="RecordsWatchReading.Unchanged"/>, for
/// a test that has no registry to watch.
/// </summary>
internal sealed class NullRecordsWatchFactory : IRecordsWatchFactory
{
    public static NullRecordsWatchFactory Instance { get; } = new();

    public IRecordsWatch Arm() => Quiet.Instance;

    private sealed class Quiet : IRecordsWatch
    {
        public static Quiet Instance { get; } = new();

        public RecordsWatchReading Read() => RecordsWatchReading.Unchanged;

        public void Dispose() { }
    }
}
