using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IEarlierRunCheck"/>. Reads the mark first, then this
/// account's settings and <c>last-run.json</c>, then the Application log, and stops
/// at the first that answers.
/// </summary>
internal sealed class EarlierRunCheck : IEarlierRunCheck
{
#if DEBUG
    /// <summary>
    /// Set to 1, a Debug build's start check leaves the Application log unread and
    /// answers from the mark and this account's files alone, so a developer's PC whose
    /// log records a command-line Move or Delete can still be put back to its first run.
    /// The name and its read sit only inside <c>#if DEBUG</c> blocks, and
    /// <c>scripts/check-debug-only-switches.mjs</c> holds them there, so no Release build
    /// can be told to ignore the log.
    /// </summary>
    internal const string SkipApplicationLogVariable = "INSTALLERCLEAN_DEBUG_SKIP_APPLICATION_LOG";
#endif

    private readonly IFirstRunMark _mark;
    private readonly ISettingsService _settings;
    private readonly IResultLogService _resultLog;
    private readonly ICommandLineRunRecord _commandLine;
    private readonly bool _skipApplicationLog;

    /// <summary>
    /// The answer for this start, settled once, by whichever comes first: the check's
    /// own finding, or a bound passing in <see cref="ShowsAnEarlierRunWithinAsync"/>.
    /// </summary>
    private readonly TaskCompletionSource<bool> _settled =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The check itself, which completes with the settled answer once it has finished reading.</summary>
    private readonly Lazy<Task<bool>> _answer;

    public EarlierRunCheck(
        IFirstRunMark mark, ISettingsService settings, IResultLogService resultLog,
        ICommandLineRunRecord commandLine)
        : this(mark, settings, resultLog, commandLine, SkipsApplicationLog()) { }

    /// <summary>Test seam: takes the Debug switch's answer rather than reading it.</summary>
    internal EarlierRunCheck(
        IFirstRunMark mark, ISettingsService settings, IResultLogService resultLog,
        ICommandLineRunRecord commandLine, bool skipApplicationLog)
    {
        _mark = mark;
        _settings = settings;
        _resultLog = resultLog;
        _commandLine = commandLine;
        _skipApplicationLog = skipApplicationLog;
        _answer = new Lazy<Task<bool>>(() => Task.Run(async () =>
        {
            _settled.TrySetResult(Check());
            return await _settled.Task.ConfigureAwait(false);
        }));
    }

    private static bool SkipsApplicationLog()
    {
#if DEBUG
        return Environment.GetEnvironmentVariable(SkipApplicationLogVariable) == "1";
#else
        return false;
#endif
    }

    public Task<bool> ShowsAnEarlierRunAsync() => _answer.Value;

    public async Task<bool> ShowsAnEarlierRunWithinAsync(TimeSpan bound)
    {
        _ = ShowsAnEarlierRunAsync();
        using var stopWaiting = new CancellationTokenSource();
        var finished = await Task.WhenAny(_settled.Task, FrameworkTask.Delay(bound, stopWaiting.Token)).ConfigureAwait(false);
        if (finished == _settled.Task)
        {
            stopWaiting.Cancel();
        }
        else if (_settled.TrySetResult(true))
        {
            // The bound passed before the check answered. Yes is the answer from here on,
            // and the check's own finding, when it comes, changes nothing.
            _mark.Set();
        }
        // Where the check answered between the bound passing and the line above, its
        // finding is the answer.
        return await _settled.Task.ConfigureAwait(false);
    }

    private bool Check()
    {
        try
        {
            // Set, or unreadable: either way the screen asking is not the PC's first.
            if (_mark.Read() != FirstRunMarkState.NotSet)
                return true;

            // A settings file that is there and cannot be read says nothing either way,
            // so the check goes on to the sources after it.
            if (!_settings.TryLoad(out var settings))
                CrashLog.TryWrite(new IOException(
                    "settings.json could not be read for the start check; it went on without it."));
            else if (settings.HasSentResultLog)
                return SetMark();

            if (_resultLog.LastLogExists())
                return SetMark();

            if (_skipApplicationLog)
                return false;

            // A log that cannot be read is taken as recording an earlier run, as an
            // unreadable mark is.
            return _commandLine.Read() == CommandLineRunRecordState.NothingActedOn
                ? false
                : SetMark();
        }
        catch (Exception ex)
        {
            // Every call above is written not to throw. Should one, the check still
            // answers, and on the side an unreadable source takes.
            CrashLog.TryWrite(ex);
            return SetMark();
        }
    }

    private bool SetMark()
    {
        _mark.Set();
        return true;
    }
}
