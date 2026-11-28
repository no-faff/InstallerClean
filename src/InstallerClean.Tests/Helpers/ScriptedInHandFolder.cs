using System.IO.Abstractions;
using InstallerClean.Interop.Native;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The by-path hand (<see cref="ByPathInHandFolder"/>) with answers a test lines up, by
/// file and by step. Where the script says nothing, the file really moves on the test's
/// filesystem, so a test can still assert where each file ended.
///
/// A substituted filesystem cannot refuse an open, so being in use, a refused delete and
/// a rename that will not go are all scripted here. What Windows itself refuses is shown
/// by the tests of the app's own hand.
/// </summary>
internal sealed class ScriptedInHandFolder(IFileSystem fileSystem) : IInHandFolder
{
    private readonly ByPathInHandFolder _inner = new(fileSystem);
    private readonly Dictionary<string, int> _inUse = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _inUseAtTheHold = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Step, string Path), int> _refusals = new();

    /// <summary>Every step made, in order, as "step name" with the file's name: hold, copy-open, take, put-back, delete, move-into, stamp.</summary>
    public List<string> Steps { get; } = [];

    /// <summary>Called after each step is recorded, with the step and the file's recorded path.</summary>
    public Action<string, string>? OnStep { get; set; }

    /// <summary>Where set, the error <see cref="IInHandFolder.OpenBatch"/> fails with.</summary>
    public int? OpenBatchError { get; set; }

    /// <summary>Where set, what every batch answers to <see cref="IInHandBatch.ReachOf"/>.</summary>
    public RenameReach? Reach { get; set; }

    /// <summary>The folder of the last batch opened.</summary>
    public string? BatchFolder { get; private set; }

    /// <summary>The next <paramref name="times"/> opens of <paramref name="path"/>, to hold it or to copy it, find it in use.</summary>
    public ScriptedInHandFolder InUse(string path, int times = 1)
    {
        _inUse[path] = times;
        return this;
    }

    /// <summary>
    /// The next <paramref name="times"/> opens of <paramref name="path"/> to hold it find it in use,
    /// while an open to copy it succeeds: the open to copy shares reading and the hold shares nothing.
    /// </summary>
    public ScriptedInHandFolder InUseAtTheHold(string path, int times = 1)
    {
        _inUseAtTheHold[path] = times;
        return this;
    }

    /// <summary>
    /// Every try at <paramref name="step"/> ("take", "put-back", "delete" or "move-into")
    /// on <paramref name="path"/> is refused with <paramref name="windowsError"/>.
    /// </summary>
    public ScriptedInHandFolder Refuse(string step, string path, int windowsError)
    {
        _refusals[(step, path.ToUpperInvariant())] = windowsError;
        return this;
    }

    public IInHandBatch? OpenBatch(string installerFolder, out int failureCode)
    {
        if (OpenBatchError is { } error)
        {
            failureCode = error;
            return null;
        }

        var batch = _inner.OpenBatch(installerFolder, out failureCode);
        if (batch is null) return null;
        BatchFolder = batch.Folder;
        return new Batch(this, batch);
    }

    private void Record(string step, string path)
    {
        Steps.Add($"{step} {fileSystem.Path.GetFileName(path)}");
        OnStep?.Invoke(step, path);
    }

    private bool Refused(string step, string path, out int code) =>
        _refusals.TryGetValue((step, path.ToUpperInvariant()), out code);

    private bool TakeInUse(string path) => TakeOne(_inUse, path);

    private static bool TakeOne(Dictionary<string, int> counts, string path)
    {
        if (!counts.TryGetValue(path, out var left) || left <= 0) return false;
        counts[path] = left - 1;
        return true;
    }

    private sealed class Batch(ScriptedInHandFolder script, IInHandBatch inner) : IInHandBatch
    {
        public string Folder => inner.Folder;

        public FileHold Hold(string path)
        {
            script.Record("hold", path);
            if (script.TakeInUse(path) || TakeOne(script._inUseAtTheHold, path))
                return new FileHold(HoldOutcome.InUse, WindowsError: Kernel32.ERROR_SHARING_VIOLATION);
            var hold = inner.Hold(path);
            return hold.File is null ? hold : hold with { File = new Held(script, hold.File) };
        }

        public CopySourceOpen OpenToCopy(string path)
        {
            script.Record("copy-open", path);
            return script.TakeInUse(path)
                ? new CopySourceOpen(HoldOutcome.InUse, WindowsError: Kernel32.ERROR_SHARING_VIOLATION)
                : inner.OpenToCopy(path);
        }

        public RenameReach ReachOf(DestinationFolder destination, out int failureCode)
        {
            var reach = inner.ReachOf(destination, out failureCode);
            return script.Reach ?? reach;
        }

        public void Dispose() => inner.Dispose();
    }

    private sealed class Held(ScriptedInHandFolder script, IHeldFile inner) : IHeldFile
    {
        public string RecordedPath => inner.RecordedPath;

        public bool IsOut => inner.IsOut;

        public bool TryReadStamp(out FileStamp stamp)
        {
            script.Record("stamp", RecordedPath);
            return inner.TryReadStamp(out stamp);
        }

        public bool TakeOut(out int failureCode)
        {
            script.Record("take", RecordedPath);
            return !script.Refused("take", RecordedPath, out failureCode) && inner.TakeOut(out failureCode);
        }

        public bool PutBack(out int failureCode)
        {
            script.Record("put-back", RecordedPath);
            return !script.Refused("put-back", RecordedPath, out failureCode) && inner.PutBack(out failureCode);
        }

        public bool Delete(out int failureCode)
        {
            script.Record("delete", RecordedPath);
            return !script.Refused("delete", RecordedPath, out failureCode) && inner.Delete(out failureCode);
        }

        public bool MoveInto(DestinationFolder destination, string name, out int failureCode)
        {
            script.Record("move-into", RecordedPath);
            return !script.Refused("move-into", RecordedPath, out failureCode)
                && inner.MoveInto(destination, name, out failureCode);
        }

        public void Dispose() => inner.Dispose();
    }
}
