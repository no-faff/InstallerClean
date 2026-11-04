namespace InstallerClean.Helpers;

/// <summary>
/// What a copy that updates itself passes the newer version it starts, and what every
/// version from 3.5.0 on takes in return: the window and the command line alike. A newer
/// version a copy has downloaded is asked through <see cref="ReplaceCopySwitch"/>,
/// <see cref="AfterProcessSwitch"/> and <see cref="StartedEventSwitch"/> to put itself at
/// that copy's path once the copy has ended (<see cref="Services.ExeReplacement"/>), and
/// says through <see cref="StartedEventSwitch"/> that its own code is running. A copy it
/// puts back after a failure is started with <see cref="UpdateFailedSwitch"/>.
/// </summary>
/// <remarks>
/// Every copy from 3.5.0 on that has the newer version put itself at its path starts that
/// version with these switches and waits for the event <see cref="StartedEventSwitch"/>
/// names. So every later version has to go on taking the switches and setting the event, or
/// an update to it from one of these fails. A switch added to the hand-over later is one an earlier version does not know, so
/// it needs a first version of its own.
/// </remarks>
internal static class HandOverContract
{
    /// <summary>
    /// The switch through which the copy that started this one passes, after the colon,
    /// the id of an event this one sets once its own code is running. Only a copy updating
    /// itself passes it, so it is not in the help.
    /// </summary>
    internal const string StartedEventSwitch = "/startedevent:";

    /// <summary>
    /// How long the copy that started the newer version waits for it to set the event
    /// before it ends the newer version and puts itself back. The wait ends at once where
    /// the newer version ends, so only a newer version that hangs reaches it. A first start
    /// is the slowest: a self-contained build writes out its native libraries the first
    /// time it runs, and an antivirus looks at a program it has not seen before. A bound
    /// a healthy start can reach fails the update on every attempt on that PC.
    /// </summary>
    internal static readonly TimeSpan StartBound = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The name of the event <paramref name="id"/> stands for, in the session's own
    /// namespace, which the newer version shares, being started by the copy that made it.
    /// </summary>
    internal static string StartedEventName(Guid id) => @"Local\InstallerClean_Started_" + id.ToString("N");

    /// <summary>The argument passing <paramref name="id"/> with <see cref="StartedEventSwitch"/>.</summary>
    internal static string StartedEventArgument(Guid id) => StartedEventSwitch + id.ToString("N");

    /// <summary>
    /// Creates the event <paramref name="id"/> names, unset, or answers null where it
    /// cannot, as <see cref="CreateEvent"/> does.
    /// </summary>
    internal static EventWaitHandle? CreateStartedEvent(Guid id) => CreateEvent(StartedEventName(id));

    /// <summary>
    /// Creates a manual-reset event named <paramref name="name"/>, unset, for one of the
    /// hand-over switches, or answers null where it cannot or where an event of that name
    /// exists already, which this process did not make. Each is written to crash.log.
    /// </summary>
    internal static EventWaitHandle? CreateEvent(string name)
    {
        try
        {
            var created = new EventWaitHandle(false, EventResetMode.ManualReset, name, out var createdNew);
            if (createdNew) return created;
            created.Dispose();
            throw new InvalidOperationException($"An event named {name} exists already.");
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return null;
        }
    }

    /// <summary>
    /// The switch through which a copy updating itself names, after the colon, the full
    /// path of its own exe, for the newer version it has downloaded to put itself there
    /// (<see cref="Services.ExeReplacement"/>). One of the three a replace is asked with
    /// (<see cref="ReadReplaceRequest"/>).
    /// </summary>
    internal const string ReplaceCopySwitch = "/replacecopy:";

    /// <summary>
    /// The switch through which a copy updating itself passes, after the colon, its own
    /// process ID, which the newer version waits on before it replaces the copy's file.
    /// One of the three a replace is asked with (<see cref="ReadReplaceRequest"/>).
    /// </summary>
    internal const string AfterProcessSwitch = "/afterprocess:";

    /// <summary>
    /// The crash.log line for a replace request to the window that also carries a command, which
    /// replaces nothing: only the command line runs a command standing for another copy.
    /// </summary>
    internal const string ReplaceWithCommandRefusal =
        "A request to put this version at another copy's path also carried a command, so nothing was replaced.";

    /// <summary>
    /// The crash.log line for a request to run a command standing for a copy whose process is
    /// not running from the file at the request's path, which runs nothing.
    /// </summary>
    internal const string NotTheCopyRefusal =
        "A request to run a command standing for another copy named a process that does not run from that copy's file, so nothing was run.";

    /// <summary>
    /// The crash.log line for a replace switch in a request that does not carry the three once
    /// each and well formed, which replaces nothing.
    /// </summary>
    internal const string ReplaceNotWellFormedRefusal =
        "A request to put this version at another copy's path did not carry its three switches once each and well formed, so nothing was replaced.";

    /// <summary>
    /// The switch a newer version that could not put itself at a copy's path starts that
    /// copy with, having left it there: the window opens saying the update couldn't be
    /// installed. It carries nothing; what failed is in crash.log.
    /// </summary>
    /// <remarks>
    /// The copy given it is the older one, so what a window from 3.5.0 on reads it as has
    /// to stay the same in every later version, and a later version starts a copy it has put back with this
    /// switch and no other: a window given a switch it does not know opens without saying
    /// the update failed.
    /// </remarks>
    internal const string UpdateFailedSwitch = "/updatefailed";

    /// <summary>
    /// The arguments a copy updating itself starts the newer version it downloaded with,
    /// for that version to put itself at <paramref name="exePath"/> once the process
    /// <paramref name="processId"/> has ended, setting the event <paramref name="startedEventId"/>
    /// stands for once it is ready to.
    /// </summary>
    internal static List<string> ReplaceArguments(string exePath, int processId, Guid startedEventId) =>
    [
        ReplaceCopySwitch + exePath,
        AfterProcessSwitch + processId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        StartedEventArgument(startedEventId),
    ];

    /// <summary>
    /// Reads whether <paramref name="args"/> ask this copy to put itself at another copy's
    /// path. <see cref="ReplaceRead.Asked"/>, with <paramref name="request"/> set, where each
    /// of the three switches <see cref="ReplaceArguments"/> builds is there once, in any
    /// place and any case of the switch, the path fully qualified, the process ID a whole
    /// number above nought and the event's id a GUID in the form
    /// <see cref="StartedEventArgument"/> writes. Every other argument is left, in order, in
    /// <see cref="ReplaceRequest.Command"/>: none asks for the replace alone, and on the
    /// command line any asks for that command to be run first, standing for the copy at the
    /// path. <see cref="ReplaceRead.NotAsked"/> where no argument starts with
    /// <see cref="ReplaceCopySwitch"/> or <see cref="AfterProcessSwitch"/>, and
    /// <see cref="ReplaceRead.Refused"/> where one does and the three are not each there
    /// once and well formed: such a start replaces nothing and sets no event, the window
    /// ending at once and the command line refusing the switch as an argument it does not
    /// know.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Do not change what this reads. A copy from 3.5.0 on is to start the newer version it
    /// updates to with these three, and on the command line with the command as typed,
    /// <see cref="CliContract.CancelEventSwitch"/> and <see cref="CliContract.NoUpdateSwitch"/>
    /// beside them, and <see cref="CliContract.NoQuestionsSwitch"/> where the run asks nothing
    /// more at the console, for every newer version it ever updates to. A later version reading them
    /// another way is one no such copy can update to.
    /// </para>
    /// <para>
    /// What the command line's two sides hold each other to, which every later version keeps
    /// as well. The newer version runs the command standing for the copy at the path: it
    /// decides what kind of copy it is from that path, and runs only where the process
    /// <see cref="AfterProcessSwitch"/> names runs from the file there. It sets the started
    /// event before it runs anything, and once the command has ended it starts itself again
    /// with the path, the same process ID and an event of its own, to put itself in place
    /// once that process has ended, and exits with the command's exit code, which the older
    /// copy ends with. For <c>/d</c> and <c>/m</c> the older copy holds the single-instance
    /// mutex until the newer version has ended, so the newer version goes on where it cannot
    /// take it, and takes it where nothing holds it. On
    /// the setup's copy the newer setup waits in the newer version's own folder under
    /// <see cref="Services.AppCopy.DownloadFileName"/>'s name for it, and putting the newer
    /// version in place means running that setup, which opens nothing.
    /// </para>
    /// </remarks>
    internal static ReplaceRead ReadReplaceRequest(string[] args, out ReplaceRequest? request)
    {
        request = null;
        if (!args.Any(a => a.StartsWith(ReplaceCopySwitch, StringComparison.OrdinalIgnoreCase)
                || a.StartsWith(AfterProcessSwitch, StringComparison.OrdinalIgnoreCase)))
            return ReplaceRead.NotAsked;

        string? exePath = null, processId = null, startedEvent = null;
        var command = new List<string>(args.Length);
        foreach (var arg in args)
        {
            if (!TakeValue(arg, ReplaceCopySwitch, ref exePath, out var twice)
                && !TakeValue(arg, AfterProcessSwitch, ref processId, out twice)
                && !TakeValue(arg, StartedEventSwitch, ref startedEvent, out twice))
                command.Add(arg);
            if (twice) return ReplaceRead.Refused;
        }
        if (exePath is null || !Path.IsPathFullyQualified(exePath)
            || !int.TryParse(processId, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var id) || id <= 0
            || !Guid.TryParseExact(startedEvent, "N", out _))
            return ReplaceRead.Refused;
        request = new ReplaceRequest(exePath, id, startedEvent!, command.ToArray());
        return ReplaceRead.Asked;

        // Takes the value after switchName where arg starts with it; twice where one was taken already.
        static bool TakeValue(string arg, string switchName, ref string? value, out bool twice)
        {
            twice = false;
            if (!arg.StartsWith(switchName, StringComparison.OrdinalIgnoreCase)) return false;
            twice = value is not null;
            value = arg[switchName.Length..];
            return true;
        }
    }

    /// <summary>
    /// Takes every <see cref="UpdateFailedSwitch"/> out of <paramref name="args"/>, in any
    /// case, and answers what is left, with <paramref name="updateFailed"/> saying whether
    /// there was one.
    /// </summary>
    internal static string[] TakeUpdateFailed(string[] args, out bool updateFailed)
    {
        var rest = args.Where(a => !a.Equals(UpdateFailedSwitch, StringComparison.OrdinalIgnoreCase)).ToArray();
        updateFailed = rest.Length != args.Length;
        return rest;
    }

    /// <summary>
    /// Takes every <paramref name="switchName"/> carrying an event's id out of
    /// <paramref name="args"/>, wherever it stands and in any case, and answers what is
    /// left. <paramref name="id"/> is the first one's id, or null where there is none. One
    /// carrying anything else is left in.
    /// </summary>
    internal static string[] TakeEventSwitch(string[] args, string switchName, out string? id)
    {
        id = null;
        var rest = new List<string>(args.Length);
        foreach (var arg in args)
        {
            if (arg.StartsWith(switchName, StringComparison.OrdinalIgnoreCase)
                && Guid.TryParseExact(arg[switchName.Length..], "N", out _))
                id ??= arg[switchName.Length..];
            else
                rest.Add(arg);
        }
        return rest.ToArray();
    }

    /// <summary>
    /// Takes every <see cref="StartedEventSwitch"/> carrying an event's id out of
    /// <paramref name="args"/>, as <see cref="TakeEventSwitch"/> does.
    /// </summary>
    internal static string[] TakeStartedEvent(string[] args, out string? startedEvent) =>
        TakeEventSwitch(args, StartedEventSwitch, out startedEvent);

    /// <summary>
    /// Sets the event <paramref name="id"/> stands for, telling the copy that started this
    /// one that its own code is running, and answers whether this version goes on: true
    /// where the event is set, and where <paramref name="id"/> is null, as on every start
    /// that is not an update's. An event this process cannot open or set is written to
    /// crash.log and answers false, and the caller then ends without running anything.
    /// Never throws.
    /// </summary>
    internal static bool SignalStarted(string? id)
    {
        if (id is null) return true;
        try
        {
            if (!Guid.TryParseExact(id, "N", out var parsed))
                throw new FormatException("The value passed with the started-event switch is not an event's id.");
            if (!EventWaitHandle.TryOpenExisting(StartedEventName(parsed), out var started))
                throw new InvalidOperationException("The event passed with the started-event switch does not exist.");
            using (started) started.Set();
            return true;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return false;
        }
    }
}

/// <summary>What <see cref="HandOverContract.ReadReplaceRequest"/> found.</summary>
internal enum ReplaceRead
{
    /// <summary>Nothing asks this copy to replace another.</summary>
    NotAsked,

    /// <summary>The three switches ask it, well formed.</summary>
    Asked,

    /// <summary>A replace switch is there and the three are not: this start replaces nothing and sets no event.</summary>
    Refused,
}

/// <summary>
/// A request to put this copy at <paramref name="ExePath"/> once the process
/// <paramref name="ProcessId"/> has ended, setting the event <paramref name="StartedEvent"/>
/// names once it is ready to, and on the command line to run <paramref name="Command"/>
/// first where it holds anything (<see cref="HandOverContract.ReadReplaceRequest"/>).
/// </summary>
internal sealed record ReplaceRequest(string ExePath, int ProcessId, string StartedEvent, IReadOnlyList<string> Command);
