using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// What a completed Move did to the disk. The completion card needs it in two
/// places that do not answer the same question: the heading verb ("freed" only
/// where the system drive is genuinely emptier) and the body's advice about
/// when the space comes back. A same-drive move is not the only one that frees
/// nothing at that instant, so the two cannot be derived from each other.
/// </summary>
public enum MoveSpaceOutcome
{
    /// <summary>Another volume: the system drive is genuinely emptier now.</summary>
    FreedSpace,

    /// <summary>
    /// The drive the files came from. A rename, so nothing is reclaimed until
    /// the backup folder is deleted or moved off the drive.
    /// </summary>
    SameDrive,

    /// <summary>
    /// A volume the classification could not read. The heading claims no
    /// freed space and the body makes no claim about the drive.
    /// </summary>
    Unclassified,
}

/// <summary>
/// Completion-screen slice. Holds what the card shows after a scan that
/// offers nothing and at the end of a Move or Delete, and the report box the
/// PC's first finished card carries.
///
/// THE REPORT IS THE PC'S FIRST RUN AND NO OTHER. A card that writes a report
/// asks <see cref="ReportIsFreeAsync"/> before it is revealed and takes the box
/// with <see cref="TakeReport"/>; from then on no later card in this sitting
/// carries it, and the PC-wide mark (<see cref="IFirstRunMark"/>) keeps every
/// later sitting, in any account, from carrying it either. The report is written
/// once the card is up (<see cref="WriteReportAsync"/>) and sent as the card
/// closes with the box ticked, whatever closes it. A cancelled or stopped run
/// that moved or deleted files carries no box and still counts as the first run
/// (<see cref="RecordFirstRun"/>), and so does a Move or Delete that finishes
/// after the window has been asked to close, its card going with the window
/// unseen.
/// </summary>
public partial class CompletionViewModel : ObservableObject
{
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _heading = string.Empty;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private string _restore = string.Empty;

    /// <summary>
    /// Move-only: the raw destination path embedded in <see cref="Summary"/>,
    /// so the WPF host can locate it within the formatted sentence and force
    /// it onto its own line (mirroring the move-confirmation dialog's
    /// destination-on-its-own-line treatment). Empty on every other
    /// completion path (delete, all-clear) so the host falls back to
    /// rendering Summary verbatim. Must be set before <see cref="Summary"/>
    /// in <see cref="ShowMoveSummary"/>: the WPF host rebuilds the summary
    /// line's inlines from Summary's PropertyChanged, so this needs its
    /// final value in place before that setter raises. Cleared everywhere
    /// else because the view-model instance is reused across operations.
    /// </summary>
    [ObservableProperty] private string _summaryDestination = string.Empty;

    /// <summary>
    /// The failure count line ("2 of 71 could not be moved."), shown in
    /// the warning colour directly under the heading and empty on every run
    /// that failed at nothing, which collapses the bound TextBlock. It exists
    /// as its own zone because either of the two lines that could carry it
    /// instead merges two messages into one: on the heading, the outcome plus a
    /// trailing "some files could not be processed" clause that clips; on the
    /// summary line, the destination path followed by an error count that reads
    /// as part of the folder name. Cleared everywhere else, because the
    /// view-model instance is reused across operations.
    /// </summary>
    [ObservableProperty] private string _failedCount = string.Empty;

    /// <summary>
    /// Paints the heading in the warning colour instead of the success green.
    /// True only where the operation acted on nothing and had errors, which is
    /// the one outcome the size headings cannot state honestly ("0 B freed" is
    /// a result, not a failure). A partial failure keeps the success heading:
    /// something was genuinely freed, and <see cref="FailedCount"/> carries the
    /// rest. A cancelled run is never a failure, so it keeps it too.
    /// </summary>
    [ObservableProperty] private bool _headingIsWarning;

    [ObservableProperty] private string _errors = string.Empty;

    /// <summary>
    /// Line shown when the act-time re-verify left some candidates out of the
    /// batch, and saying which of its two reasons applied: a program has needed
    /// them again since the scan, or the re-verify could not read the records
    /// well enough to judge. Empty on every path where the re-verify dropped
    /// nothing (the common case), which collapses the bound TextBlock. Set
    /// through the <c>reverify</c> argument of the Show* summary methods and
    /// cleared everywhere else, because the view-model instance is reused across
    /// operations.
    /// </summary>
    [ObservableProperty] private string _skipped = string.Empty;

    /// <summary>
    /// The lines saying why a scan held files back for something about the scan itself,
    /// shown under <see cref="Skipped"/>: how many files changed after it began
    /// (<see cref="RecentChangeReport.Line(ScanResult)"/>), and, after a Move or Delete, that
    /// it held back every file from the folder because Windows Installer was busy as it
    /// began or the clock went back while it ran (<see cref="ScanSignalReport.WindowLine(ScanResult)"/>).
    /// Each reason that applies has its own line. A scan that held files for a signal read
    /// no file's times, so the signal's line never meets the first; the clock's line can, and
    /// then both are shown. Empty on every card whose scan held no file back for any of them,
    /// which collapses the bound TextBlock, and on the all-clear, whose scan never does. Each
    /// card that carries it works it out as it is revealed, and every other card clears it,
    /// because the view-model instance is reused across operations.
    ///
    /// IT IS EMPTY ON THE NOTHING-OFFERED CARD, which carries the line about recently
    /// changed files above its receipt with its other lines (<see cref="ShowNothingOffered"/>).
    ///
    /// AFTER A MOVE OR DELETE IT SPEAKS FOR THE SCAN THAT RAN WHEN THE OPERATION FINISHED,
    /// not the one the operation started from (<see cref="AfterTheOperationLine"/>). That
    /// scan is the one on the window behind the card
    /// (<see cref="ScanViewModel.LastScanResult"/>, read through the constructor's
    /// <c>lastScan</c>).
    /// </summary>
    [ObservableProperty] private string _scanStartLine = string.Empty;

    /// <summary>
    /// Puts the large tip button and Close without tipping on the completion card in
    /// place of Done and the small tip button under it. True where a Move or Delete moved
    /// or deleted files, a run the user cancelled part-way included. An
    /// all-clear, a run the re-verify held back entirely, a run every file
    /// errored on, a Move or Delete that reached no file and a Move the app
    /// stopped itself all leave it false, and those cards keep Done and the
    /// small tip button. It measures the bytes the run moved or deleted, not whether the
    /// disk got any emptier, so a same-drive Move sets it while its heading
    /// says "moved" rather than "freed". Set from the bytes argument in each
    /// Show* method.
    /// </summary>
    [ObservableProperty] private bool _asksForDonation;

    /// <summary>
    /// Whether this card carries the "Send anonymous report" box. Set by
    /// <see cref="TakeReport"/> before the card is revealed, so the box is in place
    /// when the card is read out, and cleared as the card closes or where the
    /// report could not be written. No Show* method touches it.
    /// </summary>
    [ObservableProperty] private bool _offersReport;

    /// <summary>
    /// Whether the box is ticked. It starts ticked on every card that carries it; what it
    /// says as the card closes is what happens. No Show* method touches it.
    /// </summary>
    [ObservableProperty] private bool _sendsReport;

    /// <summary>
    /// The Country or region <see cref="TakeReport"/> read for the card, which the report
    /// built for the same card carries. Null until a card takes the report, and wherever
    /// Windows gave nothing.
    /// </summary>
    public string? ReportRegion { get; private set; }

    /// <summary>
    /// The kind of copy of the app that is running, which the report carries. It is the
    /// kind the app decided at start (<see cref="IAppUpdater.Kind"/>), so every report the
    /// window writes in a sitting names the same one.
    /// </summary>
    public AppCopyKind ReportCopy { get; }

    /// <summary>The program that sends the reports this window writes: the window.</summary>
    public ReportHost ReportHost => ReportHost.Window;

    /// <summary>
    /// Whether the panel saying what the report holds is open, the panel the small
    /// "i" beside the box opens and closes. It closes whenever the box leaves the card,
    /// which every close of the card does, so it is never open without the box. No
    /// Show* method touches it.
    /// </summary>
    [ObservableProperty] private bool _reportPanelOpen;

    partial void OnOffersReportChanged(bool value)
    {
        if (!value) ReportPanelOpen = false;
    }

    /// <summary>
    /// How long the PC's first card waits for the window's start check to answer
    /// before it is revealed. Where the bound passes first the check answers that
    /// the PC has had its first run, and sets the mark.
    /// </summary>
    internal static readonly TimeSpan ReportCheckBound = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long closing the app waits for the report's send and its settings saves
    /// before it lets the process go. A report cut off here is still waiting
    /// (<see cref="AppSettings.ReportSendPending"/>) and goes from a later start.
    /// </summary>
    internal static readonly TimeSpan ExitWaitBound = TimeSpan.FromSeconds(2);

    private readonly IResultLogService _resultLogService;
    private readonly ISettingsService _settingsService;
    private readonly IEarlierRunCheck _earlierRunCheck;
    private readonly IFirstRunMark _firstRunMark;
    private readonly IWindowsRegion _windowsRegion;
    private readonly IWindowService _windowService;
    private readonly Func<ScanResult?> _lastScan;

    /// <summary>
    /// True once this sitting has had the PC's first run: a card took the report,
    /// or a cancelled or stopped run moved or deleted files. The start check's
    /// answer is taken once per start and does not change when this sitting sets the
    /// mark, and the mark itself is not read back, so a mark that failed to save
    /// still leaves every later card in this sitting without the box.
    /// </summary>
    private bool _firstRunTaken;

    /// <summary>True from <see cref="TakeReport"/> until the card carrying the box closes.</summary>
    private bool _boxOpen;

    /// <summary>What the box last said while it was open.</summary>
    private volatile bool _reportTicked;

    /// <summary>
    /// Completes with whether this sitting's report was written. Set before the write
    /// is started, so a card closed while the write is still running finds it.
    /// </summary>
    private Task<bool>? _reportWritten;

    /// <summary>
    /// Every send and the settings save beside it still running, which closing the app
    /// waits for (<see cref="SettleReportOnExit"/>). None of them faults.
    /// </summary>
    private Task _reportWork = Task.CompletedTask;
    private readonly object _reportWorkGate = new();

    /// <summary>
    /// <paramref name="resultLogService"/> writes and sends the report and
    /// <paramref name="settingsService"/> records whether it is waiting to go or has
    /// gone, once the person has answered. <paramref name="earlierRunCheck"/> and
    /// <paramref name="firstRunMark"/> decide which card is the PC's first, and
    /// <paramref name="windowsRegion"/> gives the Country or region the report carries
    /// and the tip page is opened with. <paramref name="windowService"/> opens the tip
    /// page and the report window. <paramref name="copy"/> is the kind of copy of the
    /// app that is running, which the report carries. <paramref name="lastScan"/> answers
    /// with the scan the window shows at the moment it is asked, which the card after a
    /// Move or Delete speaks for.
    /// </summary>
    public CompletionViewModel(
        IResultLogService resultLogService,
        ISettingsService settingsService,
        IEarlierRunCheck earlierRunCheck,
        IFirstRunMark firstRunMark,
        IWindowsRegion windowsRegion,
        IWindowService windowService,
        AppCopyKind copy,
        Func<ScanResult?> lastScan)
    {
        _resultLogService = resultLogService;
        _settingsService = settingsService;
        _earlierRunCheck = earlierRunCheck;
        _firstRunMark = firstRunMark;
        _windowsRegion = windowsRegion;
        _windowService = windowService;
        ReportCopy = copy;
        _lastScan = lastScan;
    }

    /// <summary>Shows the "All clean" state after a scan finds no orphans.
    /// <paramref name="scannedFileCount"/> is how many cached files the scan
    /// accounted for, surfaced as the receipt; <paramref name="scanDurationMs"/>
    /// is the elapsed scan time. The count and the duration together stop
    /// the all-clean overlay from reading as "did nothing" on a fast
    /// machine where the duration alone shows as a fraction of a second.
    ///
    /// THE NOUN IS FILES BECAUSE THE NUMBER IS OF FILES, and the caller hands over
    /// the same count the main window's left-alone line prints, which already says
    /// that word. A registration is not a product and several of them can name one
    /// program's cached packages, so the noun follows the count rather than the
    /// records it came out of. A file the scan held back is one of them: it is in
    /// the folder and this scan judged it.</summary>
    public void ShowAllClear(int scannedFileCount, long scanDurationMs)
    {
        HeadingIsWarning = false;
        Heading = Strings.Completion_AllClean;
        FailedCount = string.Empty;
        SummaryDestination = string.Empty;
        Summary = Strings.Completion_NothingToCleanUp;
        Restore = string.Format(
            Strings.Completion_NothingToCleanUpReceipt,
            DisplayHelpers.FormatCount(scannedFileCount),
            DisplayHelpers.PluraliseFile(scannedFileCount),
            DisplayHelpers.FormatElapsedLong(TimeSpan.FromMilliseconds(scanDurationMs)));
        Errors = string.Empty;
        Skipped = string.Empty;
        ScanStartLine = string.Empty;
        AsksForDonation = false;
        IsComplete = true;
    }

    /// <summary>
    /// The second empty-offer screen, for a run that offered nothing and held back files
    /// it could not establish are unneeded: files from the folder walk, whether a rule
    /// about the RECORDS kept them all in one go or each was judged and kept, and
    /// superseded patches. A run that carried on without a drive or share, and left a
    /// file alone because of it, gets this screen too.
    ///
    /// TWO FINDINGS BOTH END WITH AN EMPTY OFFER AND THEY ARE OPPOSITE THINGS TO TELL
    /// SOMEBODY. <see cref="ShowAllClear"/> says the folder holds nothing to remove.
    /// This says nothing was offered, on a machine whose folder may be full of files
    /// nobody has vouched for. The caller chooses between them through
    /// <see cref="ScanResult.IsAllClear"/>. A file kept because Windows holds a record of
    /// the program or patch it declares does not choose this screen, except where its
    /// check stopped at a drive or share the scan gave up. Every other file held back
    /// chooses it, a file whose times are not before the scan's clock, a file the scan held
    /// because an in-progress signal was set, a file the containment check refused or could
    /// not answer for and a superseded patch included.
    ///
    /// UNDER THE HEADING THE CARD CARRIES EACH LINE SAYING WHAT TO DO THAT APPLIES, then
    /// the receipt. The line about recently changed files comes first, then the line saying
    /// which signal Windows Installer had set or that the clock went back
    /// (<see cref="ScanSignalReport.WindowLine(ScanResult)"/>, read off the scan as the
    /// window behind the card reads it), then the line naming the drives and shares the
    /// scan carried on without. Each is true of every file it speaks of. No line counts
    /// the files held back as a whole or names a cause for them: several conditions reach
    /// this screen, they are different facts about a machine, and a sentence naming one of
    /// them is false of the files the others contribute.
    ///
    /// THE RECEIPT LINE STAYS, and it is the same one the all-clear carries. A screen
    /// with a heading and no evidence that a scan ran reads as a failure rather than as a
    /// result, which is the opposite of what it has to say.
    /// </summary>
    /// <param name="result">
    /// The scan the screen is for. The line about recently changed files and the signal's
    /// line are read off it here.
    /// </param>
    /// <param name="scannedFileCount">
    /// The receipt's own count, on the terms <see cref="ShowAllClear"/> sets out: how
    /// many cached files the scan accounted for, in files rather than in programs. A FILE
    /// HELD BACK IS AMONG THEM: it is in the folder and this scan judged it, so the
    /// receipt for what was examined covers it.
    /// </param>
    /// <param name="sourcesGivenUp">
    /// The line naming the drives and shares the scan carried on without, as the main
    /// window shows it (<see cref="ScanViewModel.SourcesGivenUpText"/>), or empty. It is
    /// the last of the lines above the receipt.
    /// </param>
    public void ShowNothingOffered(
        ScanResult result, int scannedFileCount, long scanDurationMs, string sourcesGivenUp)
    {
        HeadingIsWarning = false;
        Heading = Strings.Completion_NothingOffered;
        FailedCount = string.Empty;
        SummaryDestination = string.Empty;
        Summary = JoinLines(
            RecentChangeLine(result),
            JoinLines(ScanSignalReport.WindowLine(result), sourcesGivenUp));
        Restore = string.Format(
            Strings.Completion_NothingToCleanUpReceipt,
            DisplayHelpers.FormatCount(scannedFileCount),
            DisplayHelpers.PluraliseFile(scannedFileCount),
            DisplayHelpers.FormatElapsedLong(TimeSpan.FromMilliseconds(scanDurationMs)));
        Errors = string.Empty;
        Skipped = string.Empty;
        ScanStartLine = string.Empty;
        AsksForDonation = false;
        IsComplete = true;
    }

    /// <summary>
    /// The held-back line for a completion overlay, followed on a line of its own by the
    /// line naming the drives and shares the check carried on without, or empty when the
    /// check made just before acting held nothing back. Shown alongside the Move or Delete
    /// summary so the totals add up: acted on + held back = what the user selected, and
    /// that is the whole of what the count is for.
    ///
    /// THE COUNT IS ONE SENTENCE, NAMING NO CAUSE, AND IT IS CORE'S
    /// (<see cref="HeldBackReport.Line"/>) because the command line prints the same
    /// one and the two hosts must not answer differently for one machine state.
    /// The sentence is not chosen for the batch: every file on the line was offered
    /// by the scan and not confirmed by the check made immediately before acting,
    /// which is true of every cause by construction. The causes are carried as
    /// counts on <see cref="HeldBackReasons"/> and in the opt-in result log.
    ///
    /// THE SECOND LINE NAMES THE DRIVES AND SHARES THE CHECK STOPPED AT AND COUNTS NO FILE
    /// (<see cref="SourcesGivenUpReport.WindowLine"/>). Every file it speaks of is among
    /// those the count above it counts, and the count's own sentence still names no
    /// cause.
    ///
    /// The two lines are joined with a newline, which a wrapping TextBlock draws as a
    /// line break. The all-skipped overlay routes them through <see cref="Summary"/>
    /// instead, whose inlines are composed in the window's code-behind, and that path
    /// turns each newline into an explicit break.
    /// </summary>
    private static string SkippedText(ReverifyResult? reverify) =>
        reverify is null
            ? string.Empty
            : JoinLines(
                HeldBackReport.Line(reverify.Reasons),
                SourcesGivenUpReport.WindowLine(reverify.SourceRootsGivenUpKeepingFiles));

    /// <summary>
    /// <paramref name="first"/> and <paramref name="second"/> on lines of their own, leaving
    /// out whichever is empty, so a card never draws a blank line.
    /// </summary>
    private static string JoinLines(string first, string second) =>
        first.Length == 0 ? second
        : second.Length == 0 ? first
        : first + Environment.NewLine + second;

    /// <summary>
    /// The failure count line for a completion overlay, or empty when nothing
    /// failed. It names the number that failed and nothing beside it, one
    /// sentence for one thing on the cancelled card and on the finished one
    /// alike, in every language the app ships. The cancelled summary line below
    /// is what names the full batch the run was working through, which is a
    /// different sentence with a different job.
    /// </summary>
    private static string FailedCountText(int failedCount, bool deleting)
    {
        if (failedCount == 0) return string.Empty;

        var (singular, plural, key) = deleting
            ? (Strings.Completion_FailedCountDelete_Singular,
               Strings.Completion_FailedCountDelete_Plural,
               "Completion.FailedCountDelete")
            : (Strings.Completion_FailedCount_Singular,
               Strings.Completion_FailedCount_Plural,
               "Completion.FailedCount");

        // The count of files that failed, and nothing else. Every value in this
        // family spells one placeholder, so a second argument would be handed to
        // string.Format and read by none of them.
        return string.Format(
            DisplayHelpers.Pluralise(failedCount, singular, plural, key),
            DisplayHelpers.FormatCount(failedCount));
    }

    /// <summary>
    /// The line under a Move summary: when to delete the backup folder, and for
    /// a move that stayed on the drive the files came from, why the space has
    /// not come back yet. Picks between two strings on the one thing that
    /// varies.
    /// </summary>
    // Neither form names the files, so neither agrees with a count and the
    // moved count is not a parameter here.
    private static string MoveRestoreText(MoveSpaceOutcome space) =>
        space == MoveSpaceOutcome.SameDrive
            ? Strings.Completion_MoveRestoreHintSameDrive
            : Strings.Completion_MoveRestoreHint;

    /// <summary>
    /// Shows the post-Move summary including any per-file errors.
    /// <paramref name="space"/> picks the heading verb and the restore line: a
    /// move to another volume genuinely frees space on the system drive, so it
    /// reads "freed"; a same-volume move is a rename that frees nothing until
    /// the parked folder is deleted, so it reads "moved" and its restore line
    /// says how to get the space back. An unclassifiable destination takes the
    /// claim-less verb and the restore line that names no drive.
    /// </summary>
    public void ShowMoveSummary(int movedCount, long movedBytes, string destination,
        IReadOnlyList<FileOperationError> errors, MoveSpaceOutcome space, ReverifyResult? reverify = null) =>
        ShowMoveCard(movedCount, movedBytes, destination, errors, space, reverify, stopped: false);

    /// <summary>
    /// Shows the card after a Move the app stopped itself, which is what the
    /// reader is left looking at once the dialog naming the reason is dismissed.
    ///
    /// Everything above the last line is <see cref="ShowMoveSummary"/>'s, and it
    /// is all still true: files did move, the space they took did come back, and
    /// the summary names where they went. The last line is the difference. After
    /// a Move that ran to the end that line says to delete the backup folder once
    /// satisfied all is well, and here the app has just said it could no longer
    /// confirm that folder, so a reader who had just been told to check a folder
    /// would be told to delete one. The sentence in its place is the guard's own,
    /// formatted with <see cref="MoveAbortedException.Destination"/>: the folder
    /// the files are in, and the one the rest of the card already names. The
    /// dialog builds the same sentence from the folder the caller asked for,
    /// because that is the one they configured and can go and look at, and the
    /// two are not always the same place.
    /// </summary>
    public void ShowMoveStoppedSummary(int movedCount, long movedBytes, string destination,
        IReadOnlyList<FileOperationError> errors, MoveSpaceOutcome space, ReverifyResult? reverify = null) =>
        ShowMoveCard(movedCount, movedBytes, destination, errors, space, reverify, stopped: true);

    // One body for both, because everything except the last line is the same
    // card and a second copy of it would drift. The flag is read once, at that
    // line, and every property is settled before IsComplete reveals the overlay:
    // the window turns that raise into the announcement, so a value assigned
    // after it would be drawn but never spoken.
    private void ShowMoveCard(int movedCount, long movedBytes, string destination,
        IReadOnlyList<FileOperationError> errors, MoveSpaceOutcome space, ReverifyResult? reverify,
        bool stopped)
    {
        // The heading states one outcome and only one. A partial failure still
        // freed what it freed, so it keeps the size heading and lets the count
        // line below carry the failures; only a Move that got nowhere swaps to
        // the warning heading, because "0 B freed" would report that as a
        // result.
        HeadingIsWarning = errors.Count > 0 && movedCount == 0;
        Heading = HeadingIsWarning
            ? Strings.Completion_NothingMoved
            : string.Format(
                space == MoveSpaceOutcome.FreedSpace ? Strings.Completion_Freed : Strings.Completion_Moved,
                DisplayHelpers.FormatSize(movedBytes));
        FailedCount = FailedCountText(errors.Count, deleting: false);
        var movedLabel = DisplayHelpers.PluraliseFile(movedCount);
        // SummaryDestination must be set before Summary: the WPF host rebuilds
        // the summary line's inlines (splitting the sentence at this substring
        // to force the path onto its own line) from Summary's PropertyChanged,
        // so SummaryDestination needs its final value in place before that
        // setter raises.
        SummaryDestination = HeadingIsWarning ? string.Empty : destination;
        // One summary sentence: what moved and where it went. The error count
        // lives in FailedCount now, because appended to this line it read as
        // part of the destination folder's name. Suppressed entirely when
        // nothing moved, because "0 files moved to: D:\Backup" describes files
        // arriving somewhere none of them reached; the destination is still on
        // screen in the backup-folder box behind the overlay.
        Summary = HeadingIsWarning
            ? string.Empty
            : string.Format(DisplayHelpers.Pluralise(movedCount,
                    Strings.Completion_MoveSummary_Singular,
                    Strings.Completion_MoveSummary_Plural,
                    "Completion.MoveSummary"),
                DisplayHelpers.FormatCount(movedCount), movedLabel, destination);
        // No restore line when nothing moved: it tells the reader when to delete
        // the backup folder, and a move that put nothing there has not made one
        // worth naming. A stopped Move takes its sentence on every arm instead,
        // because that sentence is the news rather than advice about what is in
        // the folder, and it is the only line on the card saying the run did not
        // reach the end.
        Restore = stopped
            ? string.Format(Strings.Error_DestinationChangedMidBatch, destination)
            : HeadingIsWarning ? string.Empty : MoveRestoreText(space);
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        Skipped = SkippedText(reverify);
        ScanStartLine = AfterTheOperationLine(_lastScan());
        // The card after a Move the app stopped keeps Done.
        AsksForDonation = movedBytes > 0 && !stopped;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the post-Delete summary including any per-file errors. Two lines
    /// and no third: the heading reads "freed", because a delete reclaims the
    /// disk at the instant it happens, and the summary states what was done.
    ///
    /// <see cref="Restore"/> is deliberately left empty, which is the one thing
    /// about this screen worth defending. The app's claim is not that a delete
    /// can be undone, it is that these files do not need undoing, so a recovery
    /// line here would be the app hedging against the thing it has just said
    /// will not happen.
    /// </summary>
    public void ShowDeleteSummary(int deletedCount, long deletedBytes,
        IReadOnlyList<FileOperationError> errors, ReverifyResult? reverify = null)
    {
        // See ShowMoveSummary for why a partial failure keeps the size heading
        // and only a delete that reached no file at all swaps to the warning.
        HeadingIsWarning = errors.Count > 0 && deletedCount == 0;
        Heading = HeadingIsWarning
            ? Strings.Completion_NothingDeleted
            : string.Format(Strings.Completion_Freed, DisplayHelpers.FormatSize(deletedBytes));
        FailedCount = FailedCountText(errors.Count, deleting: true);
        SummaryDestination = string.Empty;
        var deletedLabel = DisplayHelpers.PluraliseFile(deletedCount);
        // Suppressed when nothing was deleted: see ShowMoveSummary. "0 files
        // permanently deleted" reports an act that did not happen.
        Summary = HeadingIsWarning
            ? string.Empty
            : string.Format(DisplayHelpers.Pluralise(deletedCount,
                    Strings.Completion_PermanentDeleteSummary_Singular,
                    Strings.Completion_PermanentDeleteSummary_Plural,
                    "Completion.PermanentDeleteSummary"),
                DisplayHelpers.FormatCount(deletedCount), deletedLabel);
        Restore = string.Empty;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        Skipped = SkippedText(reverify);
        ScanStartLine = AfterTheOperationLine(_lastScan());
        AsksForDonation = deletedBytes > 0;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the completion summary for a Move the user cancelled part-way
    /// through. <paramref name="movedCount"/> of <paramref name="totalCount"/>
    /// files were moved before the stop, and <paramref name="movedBytes"/> is the
    /// size of just those. The heading states what was accomplished, not the
    /// "some files could not be processed" of a partial FAILURE, because a cancel
    /// is not one; the summary line says it was a cancel, and the moved files still
    /// get the restore reassurance because they are the ones that reached the
    /// destination.
    ///
    /// <paramref name="destination"/> IS NAMED IN THE SENTENCE AND NOT ON A LINE OF
    /// ITS OWN, which is the one thing this screen composes differently from
    /// <see cref="ShowMoveSummary"/>. The two sentences below the heading are a
    /// pair: the first says where the files that moved have gone and the second
    /// says where to put them back, so the reader is told how to undo a run they
    /// stopped rather than when to delete a backup they meant to keep.
    ///
    /// THE ONE EXCEPTION IS A CANCEL THAT ACCOMPLISHED NOTHING AND HIT AN ERROR,
    /// and it is the same exception <see cref="ShowMoveSummary"/> makes. There is
    /// no accomplishment to state, so the size heading would be reporting "0 B
    /// freed" as a result over a line saying a file could not be processed. That is
    /// the only shape that takes the warning heading here: a cancel on its own
    /// never does, however little it moved.
    /// </summary>
    public void ShowMoveCancelledSummary(int movedCount, int totalCount, long movedBytes,
        string destination, IReadOnlyList<FileOperationError> errors, MoveSpaceOutcome space,
        ReverifyResult? reverify = null)
    {
        // The same test ShowMoveSummary applies, and for the same reason: a cancel
        // that reached no file and hit an error has nothing to put in a size
        // heading, so "0 B freed" in the success colour states a result over a line
        // saying a file could not be processed. A cancel with no errors is not a
        // failure and keeps its heading whatever it moved, which is what the
        // errors conjunct is for.
        HeadingIsWarning = errors.Count > 0 && movedCount == 0;
        Heading = HeadingIsWarning
            ? Strings.Completion_NothingMoved
            : string.Format(
                space == MoveSpaceOutcome.FreedSpace ? Strings.Completion_Freed : Strings.Completion_Moved,
                DisplayHelpers.FormatSize(movedBytes));
        FailedCount = FailedCountText(errors.Count, deleting: false);
        // Left empty on purpose, which is what keeps the destination in the flow of
        // the sentence: the WPF host forces SummaryDestination onto a line of its
        // own, and here the path sits mid-sentence with the cancel after it. The
        // completed screen sets it because there the path ends the sentence.
        SummaryDestination = string.Empty;
        // AND THE SUMMARY STAYS, WHICH IS WHERE THIS PARTS COMPANY WITH
        // ShowMoveSummary. That method blanks its summary under the warning heading
        // because "0 files moved to: D:\Backup" describes files arriving somewhere
        // none of them reached. This sentence says something else: that the user
        // stopped the run. It is the only thing on the screen that does, so
        // blanking it here would trade one wrong screen for another.
        Summary = string.Format(
            DisplayHelpers.Pluralise(totalCount, Strings.Completion_MoveCancelledSummary, "Completion.MoveCancelledSummary"),
            DisplayHelpers.FormatCount(movedCount), DisplayHelpers.FormatCount(totalCount),
            DisplayHelpers.PluraliseFile(totalCount), destination);
        // The undo, and it is this screen's own line rather than the completed
        // screen's. Somebody who stopped a Move part-way wants the files back where
        // they were, and a Move only ever moved them, so naming the folder they
        // came from is the whole of it. One string for both drives: the same-drive
        // variant says the space has not come back until the backup is deleted,
        // which is the opposite of what this screen asks for.
        //
        // Still silent where nothing moved, on ShowMoveSummary's rule and for the
        // same reason: there is nothing to put back. Reachable because a cancel
        // with zero moved and a non-zero error count still raises a summary.
        Restore = movedCount == 0 ? string.Empty : Strings.Completion_MoveCancelledRestoreHint;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        Skipped = SkippedText(reverify);
        ScanStartLine = AfterTheOperationLine(_lastScan());
        AsksForDonation = movedBytes > 0;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the completion summary for a Delete the user cancelled part-way
    /// through: <paramref name="deletedCount"/> of <paramref name="totalCount"/>
    /// files were deleted before the stop. The heading states what was
    /// accomplished rather than reading as a failure, because a cancel is not
    /// one, and the screen carries no restore line for the same reason
    /// <see cref="ShowDeleteSummary"/> does not.
    ///
    /// The exception is <see cref="ShowMoveCancelledSummary"/>'s, on the same
    /// terms: a cancel that reached no file and hit an error has no accomplishment
    /// to state, so it takes the warning heading rather than reporting "0 B freed"
    /// over a line saying a file could not be processed.
    /// </summary>
    public void ShowDeleteCancelledSummary(int deletedCount, int totalCount, long deletedBytes,
        IReadOnlyList<FileOperationError> errors, ReverifyResult? reverify = null)
    {
        // See ShowMoveCancelledSummary, which this mirrors line for line: the
        // warning heading only where the cancel reached no file AND something
        // failed, and the cancelled summary sentence kept either way because it is
        // the only line saying the run was stopped.
        HeadingIsWarning = errors.Count > 0 && deletedCount == 0;
        Heading = HeadingIsWarning
            ? Strings.Completion_NothingDeleted
            : string.Format(Strings.Completion_Freed, DisplayHelpers.FormatSize(deletedBytes));
        FailedCount = FailedCountText(errors.Count, deleting: true);
        SummaryDestination = string.Empty;
        Summary = string.Format(
            DisplayHelpers.Pluralise(totalCount, Strings.Completion_PermanentDeleteCancelledSummary, "Completion.PermanentDeleteCancelledSummary"),
            DisplayHelpers.FormatCount(deletedCount), DisplayHelpers.FormatCount(totalCount),
            DisplayHelpers.PluraliseFile(totalCount));
        Restore = string.Empty;
        Errors = errors.Count > 0 ? FormatErrorBreakdown(errors) : string.Empty;
        Skipped = SkippedText(reverify);
        ScanStartLine = AfterTheOperationLine(_lastScan());
        AsksForDonation = deletedBytes > 0;
        IsComplete = true;
    }

    /// <summary>
    /// Shows the completion overlay when the act-time re-verify kept EVERY
    /// candidate back, so nothing was moved or deleted. No freed-size heading
    /// (nothing was freed); the summary IS <see cref="SkippedText"/>, the held-back
    /// count and any line naming a drive or share the check carried on without, as the
    /// other cards carry them under their own summary.
    /// <paramref name="deleting"/> picks the heading, on the same rule as
    /// <see cref="FailedCountText"/>: this screen only ever follows one of the two
    /// buttons, so it says which one.
    /// </summary>
    public void ShowReverifyAllSkipped(ReverifyResult reverify, bool deleting)
    {
        // Every candidate being kept back is the check working, not the run
        // failing, so this heading is not a warning however it reads. That is the
        // one thing this screen does NOT share with the two above, where the same
        // two strings mean a Move or a Delete that got nowhere.
        HeadingIsWarning = false;
        // THE HEADING ANSWERS TWO QUESTIONS AND BOTH HAVE TO BE IN IT. The second
        // went unanswered here for a release because the first was answered well.
        //
        // Not Completion_AllClean, which ShowAllClear uses correctly for a machine
        // with nothing to do. Here everything the user confirmed was kept back,
        // which is not the same as there having been nothing to remove.
        //
        // And per button rather than one word for both, because the user pressed
        // Move or pressed Delete and a heading naming neither is a word they never
        // asked for. Both strings are already in the file and already picked between
        // by ShowMoveSummary and ShowDeleteSummary above, so DO NOT REPLACE THE PAIR
        // WITH ONE WORD: the wording and the per-button choice are two decisions and
        // changing the first must not quietly settle the second.
        Heading = deleting ? Strings.Completion_NothingDeleted : Strings.Completion_NothingMoved;
        FailedCount = string.Empty;
        SummaryDestination = string.Empty;
        // EVERY CAUSE REACHES THE COUNT AND NO CONDITION OVERRIDES IT.
        //
        // A WHOLE-BATCH CONDITION ARRIVING LATER NEEDS ONLY A COUNT THAT REACHES
        // Total, LIKE EVERY OTHER. The line names no cause, so such a condition needs
        // no sentence of its own and no override.
        Summary = SkippedText(reverify);
        Restore = string.Empty;
        Errors = string.Empty;
        Skipped = string.Empty;
        ScanStartLine = AfterTheOperationLine(_lastScan());
        AsksForDonation = false;
        IsComplete = true;
    }

    /// <summary>
    /// The line about files <paramref name="scan"/> held back because their times are not
    /// before the moment it began (<see cref="RecentChangeReport.Line(ScanResult)"/>), or
    /// empty. "Nothing offered on this PC" gets it here for its own scan.
    /// </summary>
    private static string RecentChangeLine(ScanResult? scan) => RecentChangeReport.Line(scan);

    /// <summary>
    /// <see cref="ScanStartLine"/> for a card after a Move or Delete: the line saying
    /// Windows Installer was busy as <paramref name="scan"/> began, or that the clock went
    /// back while it ran, where it held files back for that, then the line about files
    /// changed after it began, each where it applies, or empty. The scan is the one the
    /// window shows as the card is revealed, which is empty where there is none, as on a
    /// window that is closing.
    ///
    /// A CARD AFTER A MOVE OR DELETE IS SHOWN AFTER THE REFRESH THAT ENDS THE BATCH
    /// (<see cref="CleanupViewModel"/>'s <c>RefreshAfterBatchAsync</c>). Shown before it, the
    /// card would speak for the scan the operation started from.
    /// </summary>
    private static string AfterTheOperationLine(ScanResult? scan) =>
        JoinLines(ScanSignalReport.WindowLine(scan), RecentChangeLine(scan));

    /// <summary>
    /// Whether the card about to be revealed can carry the report: false at once where
    /// this sitting has already had the PC's first run, and otherwise the window's start
    /// check, waited for no longer than <see cref="ReportCheckBound"/>. Resumes on the
    /// caller's thread, so a caller on the dispatcher can reveal the card straight after.
    /// Nothing is taken: <see cref="TakeReport"/> does that, once the caller has
    /// confirmed the card is still wanted.
    /// </summary>
    public async Task<bool> ReportIsFreeAsync()
    {
        if (_firstRunTaken) return false;
        return !await _earlierRunCheck.ShowsAnEarlierRunWithinAsync(ReportCheckBound);
    }

    /// <summary>
    /// Takes the report for the card about to be revealed: records the PC's first run,
    /// claims the PC-wide mark, and puts the box on the card, ticked, reading the Country or
    /// region set in Windows into <see cref="ReportRegion"/> for the report. Called before
    /// the Show* method, so the box is there when the card is read out. False where this
    /// sitting has already had its first run, which a second card reaching here behind the
    /// same answer from <see cref="ReportIsFreeAsync"/> finds, and where the claim is
    /// refused because the mark is already set, which another program that ran meanwhile
    /// leaves, or cannot be written. Either way this sitting's first run has passed, so no
    /// later card carries a box.
    /// </summary>
    public bool TakeReport()
    {
        if (_firstRunTaken) return false;
        _firstRunTaken = true;
        if (!_firstRunMark.TryClaim()) return false;
        ReportRegion = _windowsRegion.Read();
        _reportTicked = true;
        _boxOpen = true;
        SendsReport = true;
        OffersReport = true;
        return true;
    }

    /// <summary>
    /// Records the PC's first run where a cancelled or stopped Move or Delete moved or
    /// deleted files, or a finished one did after the window was asked to close, or one
    /// ended where how far it got is not known. Its card carries no box and no report
    /// is written, and no later card in this sitting carries one.
    /// </summary>
    public void RecordFirstRun()
    {
        if (_firstRunTaken) return;
        _firstRunTaken = true;
        _firstRunMark.Set();
    }

    /// <summary>
    /// Writes the report of the card that took it, once that card is up. The write runs
    /// off the dispatcher. Nothing is recorded as waiting to go here: that is done as the
    /// card closes, once the box has its answer. Where the write fails, a box still on the
    /// card is taken away, there being nothing to send. Completes once the write has, and
    /// never throws.
    /// </summary>
    public async Task WriteReportAsync(ResultLogEntry entry)
    {
        // In place before the write starts, because the card can be closed while the
        // write is being started, and the send that close begins waits on this.
        var written = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _reportWritten = written.Task;

        Task<bool> write;
        try
        {
            write = _resultLogService.WriteAsync(entry);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            write = Task.FromResult(false);
        }

        // Settled on a thread-pool thread rather than on the dispatcher, so a send
        // waiting on it while the app closes is not held behind a dispatcher that has
        // stopped. A write that faulted is written to crash.log here, where it ends, and
        // counts as not written.
        Track(write.ContinueWith(
            t =>
            {
                if (t.Exception is { } fault) CrashLog.TryWrite(fault);
                written.SetResult(t.Status == TaskStatus.RanToCompletion && t.Result);
            },
            TaskScheduler.Default));

        if (await written.Task) return;
        if (_boxOpen)
        {
            _boxOpen = false;
            OffersReport = false;
        }
    }

    /// <summary>
    /// Sends this account's saved report where it is still waiting from an earlier
    /// start: <see cref="AppSettings.ReportSendPending"/> true and
    /// <see cref="AppSettings.HasSentResultLog"/> false. Runs in the background and
    /// waits for the window's start check first, so its settings read and save never
    /// meet the check's read. Every start tries until one send is accepted.
    /// </summary>
    public void StartSavedReportRetry() => Track(Task.Run(async () =>
    {
        try
        {
            await _earlierRunCheck.ShowsAnEarlierRunAsync().ConfigureAwait(false);
            if (!_settingsService.TryLoad(out var settings)
                || !settings.ReportSendPending || settings.HasSentResultLog)
                return;
            await SendSavedReportAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
    }));

    /// <summary>
    /// Settles a card still up as the app closes, as closing the card would, then waits
    /// no longer than <see cref="ExitWaitBound"/> for every send and save still running.
    /// </summary>
    /// <remarks>
    /// Runs on the window's thread as the app closes, an update's hand-over included. The
    /// wait dispatches nothing (<see cref="HandleWait"/>), so no message sent to the thread
    /// is handled while it waits. Work still running at the bound is written to crash.log.
    /// Where Windows refuses the wait, that is written instead and nothing is waited for.
    /// </remarks>
    public void SettleReportOnExit()
    {
        SettleReport();
        Task work;
        lock (_reportWorkGate) work = _reportWork;
        if (work.IsCompleted) return;
        try
        {
            if (HandleWait.One(((IAsyncResult)work).AsyncWaitHandle.SafeWaitHandle, ExitWaitBound) == HandleWaitResult.TimedOut)
                CrashLog.TryWrite(new TimeoutException(
                    $"A send or save of the report was still running {ExitWaitBound.TotalSeconds:0} seconds after the app began to close, and was left."));
        }
        catch (Exception ex) { CrashLog.TryWrite(ex); }
    }

    /// <summary>
    /// Whatever the box says as its card closes is what happens: ticked, the report is
    /// recorded as waiting to go and sent once it is on disk, and a send that does not
    /// finish leaves it waiting for a later start; unticked, nothing is recorded or sent,
    /// now or later. The box leaves the card either way. A card that never closes, because
    /// the process ended before it could, leaves nothing recorded and nothing sent.
    /// </summary>
    private void SettleReport()
    {
        if (!_boxOpen) return;
        _boxOpen = false;
        OffersReport = false;
        if (!_reportTicked) return;

        var written = _reportWritten;
        Track(Task.Run(async () =>
        {
            try
            {
                if (written is null || !await written.ConfigureAwait(false)) return;
                if (!_settingsService.Update(s => s.ReportSendPending = true))
                    CrashLog.TryWrite(new InvalidOperationException(
                        "settings.json could not record that the report is waiting to be sent."));
                await SendSavedReportAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CrashLog.TryWrite(ex);
            }
        }));
    }

    partial void OnSendsReportChanged(bool value)
    {
        if (!_boxOpen) return;
        _reportTicked = value;
    }

    /// <summary>
    /// Reads <c>last-run.json</c> and sends it, and where the send is accepted records
    /// that this account's report has gone. On a thread-pool thread throughout, so the
    /// exit wait on the dispatcher cannot hold it up.
    /// </summary>
    private async Task SendSavedReportAsync()
    {
        var body = await _resultLogService.ReadLastLogAsync().ConfigureAwait(false);
        if (body is null) return;

        var outcome = await _resultLogService.SendAsync(body).ConfigureAwait(false);
        if (outcome != ResultLogSendOutcome.Sent) return;

        if (!_settingsService.Update(s =>
            {
                s.HasSentResultLog = true;
                s.ReportSendPending = false;
            }))
            CrashLog.TryWrite(new InvalidOperationException(
                "The report was sent and settings.json could not record it."));
    }

    /// <summary>The sends and saves still running, for a test to wait on.</summary>
    internal Task ReportWork
    {
        get { lock (_reportWorkGate) return _reportWork; }
    }

    private void Track(Task task)
    {
        lock (_reportWorkGate) _reportWork = Task.WhenAll(_reportWork, task);
    }

    /// <summary>
    /// The report panel's "See exactly what's sent" link: waits for the report's write,
    /// reads <c>last-run.json</c> with the call the send reads it with, and opens the
    /// window showing what that read returned, the file's own text, or null where the
    /// file could not be read. The write is started in the same dispatcher turn as the
    /// card carrying the box is revealed, so it is in hand before the link can be
    /// clicked; a write that failed has taken the box, the panel and the link off the
    /// card. Opens nothing where the box has left the card by the time the read is
    /// back. Reads only: nothing is sent, saved or marked here.
    /// </summary>
    [RelayCommand]
    private async Task ShowReportAsync()
    {
        try
        {
            var written = _reportWritten;
            if (written is null || !await written) return;
            var report = await _resultLogService.ReadLastLogAsync();
            if (!_boxOpen) return;
            _windowService.ShowReport(report);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
    }

    /// <summary>
    /// The card's large tip button: opens the tip page in the browser and closes
    /// the card, which returns the user to the main window as Done does.
    /// </summary>
    [RelayCommand]
    private void Donate()
    {
        _windowService.OpenUrl(SupportLink.TipPage(_windowsRegion));
        Dismiss();
    }

    /// <summary>
    /// The small tip button under Done: opens the tip page in the browser and
    /// leaves the card up.
    /// </summary>
    [RelayCommand]
    private void DonateSmall() => _windowService.OpenUrl(SupportLink.TipPage(_windowsRegion));

    [RelayCommand]
    private void Dismiss()
    {
        SettleReport();
        IsComplete = false;
        Errors = string.Empty;
        FailedCount = string.Empty;
    }

    /// <summary>
    /// Renders the per-file error list shown on the completion screen: one
    /// sentence per cause, then each file it applies to on a line of its own
    /// behind a hyphen. Internal so CompletionViewModelTests can verify the
    /// grouping without going through the live UI binding.
    /// </summary>
    internal static string FormatErrorBreakdown(IReadOnlyList<FileOperationError> errors)
    {
        if (errors.Count == 0) return string.Empty;

        // Group by type AND message, not type alone. No category tailors its
        // sentence to the file today, so the two keys agree, but the message is
        // part of the key because a bucket's heading is taken from its first
        // member: a type that ever varies its wording would otherwise print one
        // file's sentence over files that failed differently. Within a bucket,
        // list each file by name; the heading is shown once per bucket.
        var buckets = errors
            .GroupBy(e => (Type: e.GetType(), e.LocalisedMessage))
            .OrderByDescending(g => g.Count());

        var sb = new System.Text.StringBuilder();
        foreach (var bucket in buckets)
        {
            // The bucket's own count picks singular or plural through the resx
            // plural machinery, so an inflecting language gets its .One/.Few/
            // .Many form. The alternative, a "(3)" bracket appended to a
            // singular sentence, is one no language can inflect and reads as a
            // reference number.
            var count = bucket.Count();
            sb.Append(bucket.First().LocalisedGroupHeading(count)).AppendLine();
            // Leading spaces alone vanish in a proportional font, which every
            // font the app draws in is, so the hyphen is what separates a
            // filename from the sentence above it.
            foreach (var err in bucket)
                sb.Append("- ").Append(Path.GetFileName(err.FilePath)).AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}
