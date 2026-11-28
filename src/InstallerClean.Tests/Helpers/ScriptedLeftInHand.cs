using InstallerClean.Interop.Native;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A hand that holds no filesystem at all: what earlier batches left is what a test lists,
/// and each file answers its hold and its put-back as the test says. Every act is recorded
/// in <see cref="Steps"/>, in order, so a test reads what the recovery did and when.
/// </summary>
internal sealed class ScriptedLeftInHand : IInHandFolder
{
    private readonly List<LeftBatch> _batches = [];
    private readonly Dictionary<string, HoldOutcome> _holds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _putBackRefusals = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>False where there is no in-hand folder, and <see cref="OpenLeft"/> answers null.</summary>
    public bool HasInHandFolder { get; set; } = true;

    /// <summary>
    /// Every act, in order: "open", "hold name", "put-back name", "delete name",
    /// "let-go name" and "close".
    /// </summary>
    public List<string> Steps { get; } = [];

    /// <summary>The installer folder each <see cref="OpenLeft"/> was asked about.</summary>
    public List<string> Opened { get; } = [];

    /// <summary>A batch's folder holding <paramref name="files"/>, each a name and a length.</summary>
    public ScriptedLeftInHand Batch(string name, params (string Name, long Length)[] files) =>
        Batch(name, null, files);

    /// <summary>The same, with the folder's short name.</summary>
    public ScriptedLeftInHand Batch(string name, string? shortName, params (string Name, long Length)[] files)
    {
        _batches.Add(new LeftBatch(name, shortName, files.Select(f => new LeftFile(name, f.Name, f.Length)).ToList()));
        return this;
    }

    /// <summary>The hold of the file named <paramref name="file"/> answers <paramref name="outcome"/>.</summary>
    public ScriptedLeftInHand HoldAnswers(string file, HoldOutcome outcome)
    {
        _holds[file] = outcome;
        return this;
    }

    /// <summary>The put-back of the file named <paramref name="file"/> is refused with <paramref name="windowsError"/>.</summary>
    public ScriptedLeftInHand PutBackRefused(string file, int windowsError)
    {
        _putBackRefusals[file] = windowsError;
        return this;
    }

    public IInHandBatch? OpenBatch(string installerFolder, out int failureCode) =>
        throw new InvalidOperationException("A recovery opens no batch of its own.");

    public ILeftInHand? OpenLeft(string installerFolder)
    {
        Opened.Add(installerFolder);
        if (!HasInHandFolder) return null;
        Steps.Add("open");
        return new Left(this, installerFolder);
    }

    private sealed class Left(ScriptedLeftInHand owner, string installerFolder) : ILeftInHand
    {
        public IReadOnlyList<LeftBatch> Batches => owner._batches;

        public FileHold Hold(LeftFile file)
        {
            owner.Steps.Add("hold " + file.Name);
            var outcome = owner._holds.TryGetValue(file.Name, out var scripted) ? scripted : HoldOutcome.Held;
            return outcome == HoldOutcome.Held
                ? new FileHold(HoldOutcome.Held, new Held(owner, installerFolder + @"\" + file.Name, file.Name))
                : new FileHold(outcome, WindowsError: outcome == HoldOutcome.Refused ? Kernel32.ERROR_ACCESS_DENIED : 0);
        }

        public void Dispose() => owner.Steps.Add("close");
    }

    private sealed class Held(ScriptedLeftInHand owner, string recordedPath, string name) : IHeldFile
    {
        public string RecordedPath { get; } = recordedPath;

        public bool IsOut { get; private set; } = true;

        public bool TryReadStamp(out FileStamp stamp)
        {
            stamp = default;
            return false;
        }

        public bool TakeOut(out int failureCode) =>
            throw new InvalidOperationException("A file an earlier batch left is already out of its place.");

        public bool PutBack(out int failureCode)
        {
            owner.Steps.Add("put-back " + name);
            if (owner._putBackRefusals.TryGetValue(name, out failureCode)) return false;
            failureCode = 0;
            IsOut = false;
            return true;
        }

        public bool Delete(out int failureCode)
        {
            owner.Steps.Add("delete " + name);
            failureCode = 0;
            return true;
        }

        public bool MoveInto(DestinationFolder destination, string name, out int failureCode) =>
            throw new InvalidOperationException("A recovery sends no file on.");

        public void Dispose() => owner.Steps.Add("let-go " + name);
    }
}

/// <summary>Source records that answer what a test says, and count the times they were read.</summary>
internal sealed class ScriptedSourceRecords : ISourceRecords
{
    private readonly HashSet<string> _named = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>False where the records do not all read.</summary>
    public bool Reads { get; set; } = true;

    /// <summary>How many times the records were read.</summary>
    public int ReadCount { get; private set; }

    /// <summary>Called as each read begins.</summary>
    public Action? OnRead { get; set; }

    /// <summary>A record names the batch's folder <paramref name="batch"/>.</summary>
    public ScriptedSourceRecords Naming(string batch)
    {
        _named.Add(batch);
        return this;
    }

    public SourceRecordsRead NamedBatches(IReadOnlyList<LeftBatch> batches, CancellationToken cancellationToken)
    {
        ReadCount++;
        OnRead?.Invoke();
        return Reads ? new SourceRecordsRead(true, _named) : default;
    }
}
