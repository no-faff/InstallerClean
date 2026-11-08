using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// How far an update's download has got: the fraction downloaded, or null where the length
/// is not known, and then, once the download is complete, that it is being checked
/// (<paramref name="Checking"/>) until the update is ready to start.
/// </summary>
public readonly record struct UpdateProgress(double? Fraction, bool Checking = false);

/// <summary>
/// Gets a newer version ready to start: downloaded into a staging folder and checked.
/// </summary>
public interface IAppUpdater
{
    /// <summary>
    /// The kind of copy running, decided once at start. <see cref="AppCopy.CanUpdateItself"/>
    /// says whether a kind can put a newer version in its own place.
    /// </summary>
    AppCopyKind Kind { get; }

    /// <summary>
    /// Scoop's update command for this copy where <see cref="Kind"/> is
    /// <see cref="AppCopyKind.Scoop"/>, or null where the folder the copy runs from does
    /// not say which app Scoop installed it as, and null for every other kind.
    /// </summary>
    ScoopUpdate? ScoopUpdate { get; }

    /// <summary>
    /// Reads what the release of <paramref name="version"/> says its downloads need
    /// (<see cref="UpdateManifest"/>) and answers why this copy does not install it, or
    /// null where it may. The read of the release's file is given up after
    /// <paramref name="within"/>: null, <see cref="Timeout.InfiniteTimeSpan"/> or a time
    /// longer than a timer can be set for is no limit (<see cref="AppUpdater.IsNoLimit"/>),
    /// and less than a millisecond fetches nothing and fails as a read that could not be made. A
    /// file already kept for the version is answered from without a read, whatever the
    /// limit, and the rest of the check runs to its answer. Throws
    /// <see cref="UpdateFailedException"/> at the Download stage where the release's file
    /// could not be read, its time running out included,
    /// <see cref="InvalidOperationException"/> where the kind of copy cannot update itself,
    /// and <see cref="OperationCanceledException"/> where <paramref name="cancellationToken"/>
    /// is cancelled at any point before the answer is returned.
    /// </summary>
    Task<UpdateRefusal?> CheckRequirementsAsync(string version, TimeSpan? within, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads <paramref name="version"/> as the same kind of copy, where its release
    /// allows it (<see cref="CheckRequirementsAsync"/>), checks that the file carries that
    /// version, and answers with what to start. The check reads the file through a handle
    /// that shares only reading, kept open until <see cref="PreparedUpdate.Undo"/> or the
    /// end of the process, so the file started is the
    /// file checked. Reports the fraction downloaded, or null where the length is not known,
    /// and then that the download is being checked, which can take a while where another
    /// process, an antivirus say, is looking at the new file. Throws
    /// <see cref="UpdateFailedException"/> where any step fails, a download that does not
    /// carry the version included, <see cref="UpdateRefusedException"/> before anything is
    /// downloaded where the release rules this copy out,
    /// <see cref="InvalidOperationException"/> where the kind of copy cannot update itself,
    /// and <see cref="OperationCanceledException"/> where the token is cancelled; none of
    /// them leaves anything changed on disk.
    /// </summary>
    Task<PreparedUpdate> PrepareAsync(
        string version, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the command line of <paramref name="version"/> for the command line to run
    /// its command with, into a staging folder of its own, and answers with it as
    /// <see cref="PreparedUpdate.FileToStart"/>. The setup's copy first downloads the newer
    /// setup into the same folder, under the name the newer version looks for beside its own
    /// exe. Each file is checked for the version it was downloaded as, through a handle
    /// opened to read it that shares only reading, and that handle is kept open until
    /// <see cref="PreparedUpdate.Undo"/> or the end of the process, so no other process can
    /// write, rename or delete the file meanwhile. The setup's copy goes ahead only where the
    /// release allows both its setup and its command line on this PC; where one release entry
    /// cannot be installed from this copy, that is the answer, ahead of one needing .NET
    /// Framework 4.8. Throws as <see cref="PrepareAsync"/> does, and
    /// <see cref="InvalidOperationException"/> for every kind but the setup's copy and the
    /// command line's own download; none of them leaves anything on disk.
    /// </summary>
    Task<PreparedUpdate> PrepareStagedRunAsync(string version, CancellationToken cancellationToken);

    /// <summary>
    /// The check <see cref="PrepareStagedRunAsync"/> makes before it downloads anything,
    /// answered as <see cref="CheckRequirementsAsync"/> answers, with its time limit and its
    /// exceptions: on the setup's copy the release's setup and its command line are both
    /// judged, from one read of the release's file.
    /// </summary>
    Task<UpdateRefusal?> CheckStagedRunRequirementsAsync(
        string version, TimeSpan? within, CancellationToken cancellationToken);
}

/// <summary>
/// A newer version ready to start, and the step that takes away what getting it ready
/// left on disk if it is not started.
/// </summary>
public sealed class PreparedUpdate
{
    private Action? _undo;

    public PreparedUpdate(string version, string fileToStart, string arguments, Action undo, string? replaces = null)
    {
        Version = version;
        FileToStart = fileToStart;
        Arguments = arguments;
        Replaces = replaces;
        _undo = undo;
    }

    /// <summary>The version this is, as the update check gave it.</summary>
    public string Version { get; }

    /// <summary>
    /// The program to start: the newer setup, or the newer version itself, in the folder it
    /// was downloaded to. For the command line's run from the staging folder, the newer
    /// command line there.
    /// </summary>
    public string FileToStart { get; }

    /// <summary>The command line it is started with.</summary>
    public string Arguments { get; }

    /// <summary>
    /// Where the newer version at <see cref="FileToStart"/> is started to put itself at the
    /// running exe's path, that path. Null where the window's setup's copy runs the newer
    /// setup, which puts it there. For a run from the staging folder, the running exe's path,
    /// which the run stands for and the newer version, or on the setup's copy its setup, takes
    /// the place of.
    /// </summary>
    public string? Replaces { get; }

    /// <summary>
    /// Takes away what getting it ready left: the download and the folder it was saved in,
    /// where nothing still runs from them. Runs once, however often it is called. Never throws.
    /// </summary>
    public void Undo()
    {
        try { Interlocked.Exchange(ref _undo, null)?.Invoke(); }
        catch (Exception ex) { Helpers.CrashLog.TryWrite(ex); }
    }
}

/// <summary>
/// The update from inside the app. The setup's copy downloads the newer setup and runs
/// it; the portable, the slim and the command line's own download download the newer
/// version of the same file. The window starts it to put itself at the running exe's path;
/// the command line runs its command with the newer command line from the staging folder
/// (<see cref="PrepareStagedRunAsync"/>).
/// Each download is saved in a staging folder only Administrators and SYSTEM can write to,
/// and checked there for the version it was downloaded as before anything uses it. A build from source,
/// Scoop's copy and an undetermined copy have no download, and
/// <see cref="PrepareAsync"/> refuses them.
/// </summary>
public sealed class AppUpdater : IAppUpdater
{
    /// <summary>
    /// The longest time in whole milliseconds that
    /// <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/> takes,
    /// <see cref="int.MaxValue"/>. It throws from one millisecond more.
    /// </summary>
    private static readonly TimeSpan MaxTimerLimit = TimeSpan.FromTicks(int.MaxValue * TimeSpan.TicksPerMillisecond);

    /// <summary>
    /// Whether a time limit given to <see cref="IAppUpdater.CheckRequirementsAsync"/> is no
    /// limit: <see cref="Timeout.InfiniteTimeSpan"/>, or longer than a timer can be set for.
    /// </summary>
    public static bool IsNoLimit(TimeSpan within) => within == Timeout.InfiniteTimeSpan || within > MaxTimerLimit;

    /// <summary>
    /// What the window starts the newer setup with. /SILENT shows Setup's progress and
    /// asks nothing, /SP- leaves out the page asking whether to install, and /NORESTART
    /// keeps Setup from restarting Windows. /UPDATEFROMAPP is the app's own: seeing it,
    /// the setup script first waits for the app to close its handle to the single-instance
    /// mutex, so the setup's check for a running InstallerClean does not stop for it, and
    /// starts the app again once it has installed, which it otherwise never does on a
    /// silent install.
    /// </summary>
    public const string WindowSetupArguments = "/SILENT /SP- /NORESTART /UPDATEFROMAPP";

    private readonly string _exePath;
    private readonly string _setupArguments;
    private readonly UpdateDownloadService _downloader;
    private readonly string _stagingRoot;
    private readonly string _stagingDescriptor;
    private readonly IRegistryReader _registry;
    private readonly TimeSpan _holdRetryBound;
    private readonly Action<string>? _beforeHold;
    // Read and replaced whole, from whichever pool thread a check or an update runs on.
    private volatile KeptManifest? _manifest;

    /// <summary>A release's update.json read in full, and the version it was read for.</summary>
    private sealed record KeptManifest(string Version, byte[] Manifest);

    /// <summary>
    /// The handles holding every download this process has prepared and not undone, and
    /// the folder each is in, so each is held until then or until the process ends.
    /// </summary>
    private static readonly List<IDisposable> HeldForTheProcess = [];

    /// <param name="kind">The kind of copy running.</param>
    /// <param name="exePath">The running exe's path.</param>
    /// <param name="downloader">What fetches the newer version.</param>
    /// <param name="stagingRoot">
    /// The folder the download's own folder is made in: %ProgramData% everywhere but
    /// a test.
    /// </param>
    /// <param name="setupArguments">
    /// What the newer setup is started with, where the copy is the setup's:
    /// <see cref="WindowSetupArguments"/> for the window, and the command line's own,
    /// which leave the window closed.
    /// </param>
    public AppUpdater(
        AppCopyKind kind, string exePath, UpdateDownloadService downloader, string stagingRoot,
        string setupArguments)
        : this(kind, exePath, downloader, stagingRoot, setupArguments, UpdateStagingFolder.AdministratorsOnly,
            new RegistryReader()) { }

    /// <summary>
    /// Test seam: the staging folder's descriptor given as SDDL, so a suite without
    /// administrator rights can write into it, and the registry the PC's .NET Framework
    /// is read from. Also how long the hold on a download is tried for
    /// (<see cref="HoldRetryBound"/> where none is given), and what runs with a download's
    /// path just before its hold is first tried.
    /// </summary>
    internal AppUpdater(
        AppCopyKind kind, string exePath, UpdateDownloadService downloader, string stagingRoot,
        string setupArguments, string stagingDescriptor, IRegistryReader registry,
        TimeSpan? holdRetryBound = null, Action<string>? beforeHold = null)
    {
        _holdRetryBound = holdRetryBound ?? HoldRetryBound;
        _beforeHold = beforeHold;
        Kind = kind;
        _exePath = exePath;
        _setupArguments = setupArguments;
        _downloader = downloader;
        _stagingRoot = stagingRoot;
        _stagingDescriptor = stagingDescriptor;
        _registry = registry;
        _scoopUpdate = new Lazy<ScoopUpdate?>(ReadScoopUpdate);
    }

    public AppCopyKind Kind { get; }

    // Read the first time it is asked for, which is once a newer version has been found.
    private readonly Lazy<ScoopUpdate?> _scoopUpdate;

    public ScoopUpdate? ScoopUpdate => _scoopUpdate.Value;

    private static string? ScoopVariable(string name) => Environment.GetEnvironmentVariable(name);

    /// <summary>The largest Scoop settings file that is read.</summary>
    private const long MaxScoopConfigBytes = 1024 * 1024;

    private static string? ReadScoopConfig(string path)
    {
        // One read of the file's attributes and size, as .NET 10's FileInfo makes it: the
        // full path, with a separator at the end taken off.
        var fullPath = FullPaths.GetFullPath(path);
        if (!FrameworkFileSystem.FileExists(fullPath, out var data)) return null;
        if (FrameworkFileSystem.LengthOf(fullPath, data) > MaxScoopConfigBytes) throw new IOException("The settings file is larger than is read.");
        return FrameworkFileStream.ReadAllText(fullPath);
    }

    private ScoopUpdate? ReadScoopUpdate()
    {
        if (Kind != AppCopyKind.Scoop) return null;
        try
        {
            return AppCopy.ReadScoopUpdate(
                PathText.GetDirectoryName(_exePath) ?? string.Empty,
                ScoopVariable,
                ReadScoopConfig,
                KnownFolders.GetPath(Environment.SpecialFolder.CommonApplicationData));
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
            return null;
        }
    }

    /// <summary>
    /// A file the release has is kept for the version it was read for, so the read made as
    /// a newer version is found is not made again before its download. A release with no
    /// file is asked again each time, in case the file has been published since, and so is
    /// the PC, so a requirement met since the last answer is seen. A body that is not JSON,
    /// such as a page a network puts in GitHub's place, fails as a read that could not be
    /// made and is not kept.
    /// </summary>
    public async Task<UpdateRefusal?> CheckRequirementsAsync(
        string version, TimeSpan? within, CancellationToken cancellationToken)
    {
        if (UpdateManifest.DownloadKey(Kind) is null)
            throw new InvalidOperationException($"A copy of kind {Kind} has no download to update from.");

        // Everything after this runs on a pool thread, whatever thread called, so the
        // registry read stays off the window's thread. With the file kept nothing in the
        // check awaits anything unfinished, so the method leaves the caller's thread here
        // rather than at a read.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        return await CheckRequirementsHereAsync(version, within, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The check <see cref="CheckRequirementsAsync"/> makes, without moving off the thread
    /// that calls it. With the release's file kept, it runs to its answer on that thread.
    /// </summary>
    private async Task<UpdateRefusal?> CheckRequirementsHereAsync(
        string version, TimeSpan? within, CancellationToken cancellationToken)
    {
        var manifest = await ReadManifestHereAsync(version, within, cancellationToken).ConfigureAwait(false);
        var answer = UpdateManifest.Judge(manifest, version, Kind, () => UpdateManifest.HasNetFramework48(_registry));
        // The judgement reads the registry, and a cancel can come while it does.
        cancellationToken.ThrowIfCancellationRequested();
        return answer;
    }

    /// <summary>
    /// The check <see cref="PrepareStagedRunAsync"/> makes. The command line's own download
    /// is judged as <see cref="CheckRequirementsHereAsync"/> judges it. The setup's copy is
    /// judged against the release's setup and its command line, from one read of the
    /// release's file, because the newer command line runs on this PC before its setup does.
    /// Where either cannot be installed from this copy that is the answer, since installing
    /// .NET Framework 4.8 would not change it.
    /// </summary>
    private async Task<UpdateRefusal?> CheckStagedRunRequirementsHereAsync(
        string version, TimeSpan? within, CancellationToken cancellationToken)
    {
        var manifest = await ReadManifestHereAsync(version, within, cancellationToken).ConfigureAwait(false);
        AppCopyKind[] kinds = Kind == AppCopyKind.Setup ? [AppCopyKind.Setup, AppCopyKind.CommandLineDownload] : [Kind];
        // Read once, where any entry asks for it.
        var hasNetFramework48 = new Lazy<bool>(() => UpdateManifest.HasNetFramework48(_registry));
        var answers = kinds.Select(kind => UpdateManifest.Judge(manifest, version, kind, () => hasNetFramework48.Value))
            .ToList();
        // The judgements read the registry, and a cancel can come while they do.
        cancellationToken.ThrowIfCancellationRequested();
        return answers.Contains(UpdateRefusal.CannotInstallFromThisCopy)
            ? UpdateRefusal.CannotInstallFromThisCopy
            : answers.FirstOrDefault(answer => answer is not null);
    }

    /// <summary>
    /// The release's update.json, or null where the release has none: the file kept for
    /// <paramref name="version"/> where there is one, and otherwise read, giving up after
    /// <paramref name="within"/>. Throws <see cref="UpdateFailedException"/> at the Download
    /// stage where the file could not be read or is not JSON.
    /// </summary>
    private async Task<byte[]?> ReadManifestHereAsync(
        string version, TimeSpan? within, CancellationToken cancellationToken)
    {
        // A cancel is answered as the cancel wherever it comes, never as a refusal or a read
        // of the release that failed, the download service answering a read refused as the
        // cancel came with the cancel. A file read in full is kept first, so a cancel as the
        // read ends costs no second read.
        byte[]? manifest;
        if (_manifest is { } kept && kept.Version == version)
        {
            manifest = kept.Manifest;
            cancellationToken.ThrowIfCancellationRequested();
        }
        else
        {
#if DEBUG
            if (LocalManifest() is { } local)
                manifest = local.Absent ? null : ReadLocalManifest(local.Path);
            else
#endif
            manifest = await ReadWithinAsync(version, within, cancellationToken).ConfigureAwait(false);
            var isJson = manifest is null || UpdateManifest.IsJson(manifest);
            if (manifest is not null && isJson) _manifest = new KeptManifest(version, manifest);
            cancellationToken.ThrowIfCancellationRequested();
            if (!isJson)
                throw new UpdateFailedException(UpdateFailureStage.Download, "The release's update.json is not JSON.");
        }
        return manifest;
    }

    /// <summary>
    /// Reads the release's update.json, giving up after <paramref name="within"/> as
    /// <see cref="IAppUpdater.CheckRequirementsAsync"/> describes. Time running out is a read
    /// that failed, at the Download stage, and only a cancel of
    /// <paramref name="cancellationToken"/> is answered as a cancel.
    /// </summary>
    private async Task<byte[]?> ReadWithinAsync(string version, TimeSpan? within, CancellationToken cancellationToken)
    {
        if (within is not { } limit || IsNoLimit(limit))
            return await _downloader.ReadAsync(UpdateManifest.Address(version), UpdateManifest.MaxBytes, cancellationToken)
                .ConfigureAwait(false);
        // Less than a millisecond is a timer set to fire at once, so it fetches nothing. A
        // cancel is still answered as the cancel.
        if (limit < TimeSpan.FromMilliseconds(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new UpdateFailedException(UpdateFailureStage.Download,
                "The release's update.json was not read in the time given.");
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(limit);
        try
        {
            return await _downloader.ReadAsync(UpdateManifest.Address(version), UpdateManifest.MaxBytes, deadline.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UpdateFailedException(UpdateFailureStage.Download,
                "The release's update.json was not read in the time given.", ex);
        }
    }

    public async Task<PreparedUpdate> PrepareAsync(
        string version, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        if (AppCopy.DownloadFileName(Kind, version) is null)
            throw new InvalidOperationException($"A copy of kind {Kind} has no download to update from.");

        // Everything after this runs on a pool thread, whatever thread called, so the
        // requirements' registry read, the staging folder's creation and the start of the
        // download stay off the window's thread. With the release's file kept, nothing before the
        // download awaits anything unfinished.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        if (await CheckRequirementsHereAsync(version, null, cancellationToken).ConfigureAwait(false) is { } refusal)
            throw new UpdateRefusedException(refusal, version);

        // Everything above only reads, so an update that ends before this point, refused,
        // failed or cancelled, leaves nothing behind. The staging folder is the update's first
        // write, so a write added above this line moves the folder above it. A cancel that came
        // after the requirements were checked is answered here, before the folder is made.
        cancellationToken.ThrowIfCancellationRequested();
        var staging = MakeStaging();
        try
        {
            // Checked in the staging folder, which only Administrators and SYSTEM can write to,
            // and held from the check on, so the file started is the file checked. A file that
            // is not the version asked for is deleted here, before anything copies or starts it.
            var download = await StageHeldAsync(Kind, version, staging, progress, gated: true, cancellationToken)
                .ConfigureAwait(false);

            // The setup's copy starts the download as a setup; the window's other copies start it
            // to put itself at the running exe's path. Either runs it from the staging folder, so
            // the file and the folder stay held until the update is undone or this process ends.
            // The handle is kept in a list that lives as long as the process, as
            // a staged run's are.
            // Undo lets go of the holds and removes the download and its folder through a handle
            // on the folder this update made. A download held open by another process, an
            // antivirus looking at it say, keeps the folder: an undo tries it again, and
            // otherwise a later start's clean-up takes it.
            var isSetup = Kind == AppCopyKind.Setup;
            return staging.Prepared(
                version, download, isSetup ? _setupArguments : string.Empty, isSetup ? null : _exePath);
        }
        catch
        {
            // A throw from here on leaves nothing this update needs later, so the clean-up
            // may run again and remove whatever of it is still on disk.
            staging.Abandon();
            throw;
        }
    }

    public async Task<UpdateRefusal?> CheckStagedRunRequirementsAsync(
        string version, TimeSpan? within, CancellationToken cancellationToken)
    {
        ThrowIfNoStagedRun();
        // On a pool thread from here, as in CheckRequirementsAsync.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        return await CheckStagedRunRequirementsHereAsync(version, within, cancellationToken).ConfigureAwait(false);
    }

    private void ThrowIfNoStagedRun()
    {
        if (Kind is not (AppCopyKind.Setup or AppCopyKind.CommandLineDownload))
            throw new InvalidOperationException($"A copy of kind {Kind} has no command line to run from a download.");
    }

    public async Task<PreparedUpdate> PrepareStagedRunAsync(string version, CancellationToken cancellationToken)
    {
        ThrowIfNoStagedRun();

        // Everything after this runs on a pool thread, as in PrepareAsync.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        if (await CheckStagedRunRequirementsHereAsync(version, null, cancellationToken).ConfigureAwait(false) is { } refusal)
            throw new UpdateRefusedException(refusal, version);

        // The staging folder is the first write, as in PrepareAsync.
        cancellationToken.ThrowIfCancellationRequested();
        var staging = MakeStaging();
        try
        {
            // The setup comes first, so a setup that fails its check costs no download of the
            // command line. The folder is held throughout, so the start-up clean-up, which takes
            // files of both names, cannot take the first while the second downloads. A Debug
            // build's download gate holds only the command line's download, the last, so a
            // cancel there takes away a setup already checked and held as well as a file part
            // written.
            if (Kind == AppCopyKind.Setup)
                await StageHeldAsync(AppCopyKind.Setup, version, staging, null, gated: false, cancellationToken)
                    .ConfigureAwait(false);
            var commandLine = await StageHeldAsync(
                AppCopyKind.CommandLineDownload, version, staging, null, gated: true, cancellationToken)
                .ConfigureAwait(false);

            // The run starts the command line from the staging folder, and the newer version
            // puts itself, or on the setup's copy its setup, in this copy's place from there.
            // So once the run has started nothing is undone: the files and the folder stay held
            // until this process ends.
            return staging.Prepared(version, commandLine, string.Empty, _exePath);
        }
        catch
        {
            staging.Abandon();
            throw;
        }
    }

    /// <summary>Passes the download's fractions on as <see cref="UpdateProgress"/>, on the thread that reports them.</summary>
    private sealed class Fractions(IProgress<UpdateProgress> progress) : IProgress<double?>
    {
        public void Report(double? value) => progress.Report(new UpdateProgress(value));
    }

    /// <summary>
    /// How long the hold on a download is tried for while another process has it open. An
    /// antivirus can take several seconds over a program it has just seen written.
    /// </summary>
    internal static readonly TimeSpan HoldRetryBound = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Opens <paramref name="path"/> to hold it (<see cref="Helpers.StorageHelpers.OpenHeldToRead"/>).
    /// A sharing or lock violation, as from a scanner that opens a new program to write as it
    /// looks at it, is tried again every quarter second for up to <see cref="HoldRetryBound"/>.
    /// Throws <see cref="UpdateFailedException"/> at the Install stage, carrying the error
    /// Windows gave, where the file cannot be held.
    /// </summary>
    private async Task<FileStream> OpenHeldAsync(string path, CancellationToken cancellationToken)
    {
        _beforeHold?.Invoke(path);
        var tried = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            if (Helpers.StorageHelpers.OpenHeldToRead(path, out var error) is { } handle)
            {
                try
                {
                    return new FileStream(handle, FileAccess.Read);
                }
                catch
                {
                    handle.Dispose();
                    throw;
                }
            }
            if (error is not (Interop.Native.Kernel32.ERROR_SHARING_VIOLATION or Interop.Native.Kernel32.ERROR_LOCK_VIOLATION)
                || tried.Elapsed >= _holdRetryBound)
            {
                throw error == 0
                    ? new UpdateFailedException(UpdateFailureStage.Install,
                        "The downloaded file could not be opened to hold it: its name is not a plain file.")
                    : new UpdateFailedException(UpdateFailureStage.Install,
                        "The downloaded file could not be opened to hold it.",
                        new System.ComponentModel.Win32Exception(error));
            }
            await FrameworkTask.Delay(250, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// What one update has staged: the folder its downloads go in, held
    /// from the moment it is made, the path of each download, and the handles holding the
    /// downloads that passed their check.
    /// </summary>
    private sealed class Staging(UpdateStagingFolder folder)
    {
        private readonly List<string> _staged = [];
        private readonly List<FileStream> _holds = [];
        private readonly object _deleteGate = new();

        public string FolderPath => folder.FullPath;

        /// <summary>Records a download's path before anything is written to it.</summary>
        public void AddStaged(string path) => _staged.Add(path);

        /// <summary>Keeps the handle holding a download that passed its check.</summary>
        public void AddHold(FileStream held) => _holds.Add(held);

        /// <summary>
        /// The update to start, holding its downloads until <see cref="PreparedUpdate.Undo"/>
        /// or the end of the process. The handles are kept in a list that lives as long as the
        /// process, since a handle nothing references is closed by its finalizer, whatever the
        /// caller keeps. Its undo takes the downloads and the folder away.
        /// </summary>
        public PreparedUpdate Prepared(string version, string fileToStart, string arguments, string? replaces)
        {
            lock (HeldForTheProcess)
            {
                HeldForTheProcess.AddRange(_holds);
                HeldForTheProcess.Add(folder.Handle);
            }
            return new PreparedUpdate(version, fileToStart, arguments, Abandon, replaces);
        }

        /// <summary>
        /// Takes away everything staged: lets go of the holds on the downloads and the folder,
        /// and removes each download and then the folder through a handle on the folder this
        /// update made (<see cref="UpdateStagingFolder.TryRemove"/>), until the folder has
        /// gone. A folder or link somebody makes under the folder's name is never acted on.
        /// Never throws.
        /// </summary>
        public void Abandon()
        {
            lock (_deleteGate)
            {
                lock (HeldForTheProcess) HeldForTheProcess.RemoveAll(_holds.Contains);
                foreach (var file in _holds) file.Dispose();
                _holds.Clear();
                lock (HeldForTheProcess) HeldForTheProcess.Remove(folder.Handle);
                folder.TryRemove(_staged.Select(PathText.GetFileName)!);
            }
        }
    }

    /// <summary>
    /// Downloads the <paramref name="kind"/> download of <paramref name="version"/> into
    /// <paramref name="staging"/>'s folder, recording its path there before the download
    /// starts, and checks the version it carries through a handle that shares only reading,
    /// which <paramref name="staging"/> keeps. Answers the path. Reports the fraction
    /// downloaded to <paramref name="progress"/>, then that the download is being checked,
    /// and in a Debug build holds the download at
    /// the download gate where <paramref name="gated"/>. Throws <see cref="UpdateFailedException"/> at the Install
    /// stage where the file does not carry the version, including where it cannot be opened
    /// that way.
    /// </summary>
    private async Task<string> StageHeldAsync(
        AppCopyKind kind, string version, Staging staging, IProgress<UpdateProgress>? progress, bool gated,
        CancellationToken cancellationToken)
    {
        var fractions = progress is null ? null : new Fractions(progress);
        var path = PathText.Combine(staging.FolderPath, AppCopy.DownloadFileName(kind, version)!);
        var address = AppCopy.DownloadAddress(kind, version)!;
        staging.AddStaged(path);
#if DEBUG
        if (LocalFile(kind) is { } local)
            await new UpdateDownloadService(new LocalFileAnswer(local, gated ? DownloadGate() : null), null)
                .DownloadAsync(address, path, fractions, cancellationToken).ConfigureAwait(false);
        else
#endif
        await _downloader.DownloadAsync(address, path, fractions, cancellationToken).ConfigureAwait(false);
        progress?.Report(new UpdateProgress(null, Checking: true));

        // The bytes checked are the bytes held, read through the handle that holds them. The
        // open refuses a reparse point at the name, so the handle is on the file the name
        // starts, and shares only reading, so it fails where another process has the file
        // open to write or delete it.
        var held = await OpenHeldAsync(path, cancellationToken).ConfigureAwait(false);
        try
        {
            Version? carried;
            try
            {
                carried = Helpers.FileVersionResource.Read(held);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                carried = null;
            }
            if (carried is null || !Helpers.FileVersionResource.IsRelease(carried, version))
                throw new UpdateFailedException(UpdateFailureStage.Install,
                    "The downloaded file does not carry the version it was downloaded as.");
        }
        catch
        {
            held.Dispose();
            throw;
        }
        staging.AddHold(held);
        return path;
    }

    /// <summary>
    /// Makes and holds the folder the download is saved in, the first thing this update
    /// writes, so no pass of the start-up clean-up deletes it or anything written in it while
    /// the update runs (<see cref="UpdateStagingFolder"/>). Where the folder cannot be made,
    /// the update fails at the Install stage.
    /// </summary>
    private Staging MakeStaging()
    {
        try
        {
            return new Staging(UpdateStagingFolder.Create(_stagingRoot, _stagingDescriptor));
        }
        catch (Exception ex)
        {
            throw new UpdateFailedException(UpdateFailureStage.Install,
                "The folder the download is saved in could not be created.", ex);
        }
    }

#if DEBUG
    /// <summary>
    /// Set to the path of a setup, a Debug build whose copy is the setup's takes that
    /// setup in place of downloading one, so a setup built from this tree's script can
    /// be tried before any release carries it. The setup has to carry the version the
    /// update check names, as any download does, which
    /// <see cref="UpdateCheckService.PretendLatestVariable"/> can set. The name and its read
    /// sit only inside <c>#if DEBUG</c> blocks, and <c>scripts/check-debug-only-switches.mjs</c>
    /// holds them there.
    /// </summary>
    internal const string LocalSetupVariable = "INSTALLERCLEAN_DEBUG_LOCAL_SETUP";

    /// <summary>
    /// Set to the full path of a newer copy of the kind this one replaces itself with (the
    /// portable, the slim or the command line), a Debug build that replaces itself takes
    /// that file as the body of its download, and then stages and swaps it in as it would
    /// any download, so an update to a build from this tree can be tried offline. A run from
    /// the staging folder takes it as the newer command line, the setup's copy included, which
    /// takes its setup from <see cref="LocalSetupVariable"/>. It has to
    /// carry the version the update check names, as the setup does. Held inside
    /// <c>#if DEBUG</c> the same way.
    /// </summary>
    internal const string LocalDownloadVariable = "INSTALLERCLEAN_DEBUG_LOCAL_DOWNLOAD";

    /// <summary>
    /// Set to the full path of an update.json, a Debug build reads that file as the
    /// release's, and set to absent, it reads the release as having none, so the answer
    /// a release's file gives can be tried offline. Held inside <c>#if DEBUG</c> the same way.
    /// </summary>
    internal const string LocalManifestVariable = "INSTALLERCLEAN_DEBUG_LOCAL_MANIFEST";

    /// <summary>
    /// Set to the full path of a gate file, a Debug build's download of the file
    /// <see cref="LocalDownloadVariable"/> or <see cref="LocalSetupVariable"/> names passes the
    /// first part of it (<see cref="GatePoint"/>), writes a file named as the gate with
    /// <see cref="GateReachedSuffix"/> on the end, and waits until the gate file exists before
    /// passing the rest, so Cancel and Ctrl+C can be pressed while a partial download is on disk.
    /// A gate file and a reached file left by an earlier download are deleted as the download
    /// starts, so two downloads must not share a gate, and the gate's folder has to exist by
    /// then. The wait is a read of the download, so it ends where the download's own token
    /// does: on Cancel, on Ctrl+C, and on the download's stall timeout, which fails a download
    /// held at the gate longer than that timeout as one that stopped arriving. A download from
    /// GitHub is never held. A gate that is not a full path or whose folder is not there, or a
    /// reached file that cannot be written, fails the download. A run from the staging folder
    /// holds only its download of the command line, the last of its downloads, so a cancel
    /// there takes away a setup already checked and held as well as a file part written. Held
    /// inside <c>#if DEBUG</c> the same way.
    /// </summary>
    internal const string DownloadGateVariable = "INSTALLERCLEAN_DEBUG_DOWNLOAD_GATE";

    /// <summary>
    /// How much of a file of <paramref name="length"/> bytes a download held at the gate has
    /// passed when it stops: half the file, and never less than one byte or more than 8 MB, so
    /// a file of two bytes or more stops with part of it on disk, and no hold comes before
    /// the first byte.
    /// </summary>
    internal static long GatePoint(long length) => Math.Clamp(length / 2, 1, 8 * 1024 * 1024);

    /// <summary>Put on the end of the gate's path to name the file written as the download reaches the gate.</summary>
    internal const string GateReachedSuffix = ".reached";

    /// <summary>What <see cref="DownloadGateVariable"/> says, or null where it is unset.</summary>
    private static string? DownloadGate()
    {
        var path = Environment.GetEnvironmentVariable(DownloadGateVariable);
        return path is { Length: > 0 } ? path : null;
    }

    /// <summary>What <see cref="LocalManifestVariable"/> says, or null where it is unset.</summary>
    private static (bool Absent, string Path)? LocalManifest()
    {
        var value = Environment.GetEnvironmentVariable(LocalManifestVariable);
        if (value is not { Length: > 0 }) return null;
        return string.Equals(value, "absent", StringComparison.OrdinalIgnoreCase) ? (true, string.Empty) : (false, value);
    }

    /// <summary>
    /// The file a Debug build reads as the release's update.json, held to the read's size
    /// cap. A file that is not a full path, cannot be read or is over the cap fails as a
    /// read that could not be made.
    /// </summary>
    private static byte[] ReadLocalManifest(string path)
    {
        try
        {
            if (!PathText.IsPathFullyQualified(path)) throw new IOException($"The local file {path} is not a full path.");
            var bytes = FrameworkFileStream.ReadAllBytes(path);
            if (bytes.Length > UpdateManifest.MaxBytes) throw new IOException($"The local file {path} is over the cap.");
            return bytes;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new UpdateFailedException(UpdateFailureStage.Download, "The local update.json could not be read.", ex);
        }
    }

    /// <summary>
    /// The file <see cref="LocalSetupVariable"/> names for a download of the setup, or the one
    /// <see cref="LocalDownloadVariable"/> names for every other kind; null where it is unset.
    /// </summary>
    private static string? LocalFile(AppCopyKind kind)
    {
        var path = kind == AppCopyKind.Setup
            ? Environment.GetEnvironmentVariable(LocalSetupVariable)
            : Environment.GetEnvironmentVariable(LocalDownloadVariable);
        return path is { Length: > 0 } ? path : null;
    }

    /// <summary>
    /// Answers every request with the file at <paramref name="path"/>, so the file goes
    /// through every check, write and clean-up a download does, held at
    /// <paramref name="gate"/> where there is one (<see cref="DownloadGateVariable"/>). A
    /// path that is not a full path, or a file that cannot be opened, fails as a download
    /// that could not be made.
    /// </summary>
    internal sealed class LocalFileAnswer(string path, string? gate) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!PathText.IsPathFullyQualified(path))
                throw new HttpRequestException($"The local file {path} is not a full path.");
            FileStream file;
            try
            {
                file = FrameworkFileStream.OpenRead(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new HttpRequestException($"The local file {path} could not be opened.", ex);
            }
            if (gate is not null)
            {
                try
                {
                    if (!PathText.IsPathFullyQualified(gate)) throw new IOException($"The gate {gate} is not a full path.");
                    FrameworkFile.Delete(gate);
                    FrameworkFile.Delete(gate + GateReachedSuffix);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    file.Dispose();
                    throw new HttpRequestException($"The gate {gate} could not be made ready.", ex);
                }
            }
            var content = new StreamContent(gate is null ? file : new GatedStream(file, gate));
            content.Headers.ContentLength = file.Length;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content });
        }
    }

    /// <summary>
    /// Reads <paramref name="inner"/>, stopping at <see cref="GatePoint"/> until
    /// the file at <paramref name="gate"/> exists (<see cref="DownloadGateVariable"/>). The
    /// read that reaches the gate writes the file named with <see cref="GateReachedSuffix"/>
    /// and waits on the token it is given.
    /// </summary>
    private sealed class GatedStream(Stream inner, string gate) : Stream
    {
        private readonly long _gateAt = GatePoint(inner.Length);
        private long _passed;
        private bool _open;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _passed; set => throw new NotSupportedException(); }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!_open && _passed >= _gateAt)
            {
                using (StorageHelpers.OpenAtomic(gate + GateReachedSuffix, FileAccess.Write, StorageHelpers.AtomicOpenMode.CreateAlways)
                    ?? throw new IOException($"The file {gate}{GateReachedSuffix} could not be written.")) { }
                while (!FrameworkFile.Exists(gate))
                    await FrameworkTask.Delay(100, cancellationToken).ConfigureAwait(false);
                _open = true;
            }
            var room = _open ? count : (int)Math.Min(count, _gateAt - _passed);
            var read = await inner.ReadAsync(buffer, offset, room, cancellationToken).ConfigureAwait(false);
            _passed += read;
            return read;
        }

        // A wait at the gate ends only on a token, which a read with none cannot give it.
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
#endif
}
