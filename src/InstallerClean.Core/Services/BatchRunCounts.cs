namespace InstallerClean.Services;

/// <summary>
/// What one Move or Delete did on its way through the files, as counts for the opt-in
/// report: the looks that were not clean and what each found, the files put back, the files
/// refused as in use, the files never reached and, for a Move, the files that went by copy.
/// Nothing a person reads is built from them.
/// </summary>
/// <param name="Settles">
/// Looks that were not clean and after which the batch checked again (<see cref="BatchRun.SettleAsync"/>).
/// A look reads one thing that makes it not clean at most, so the seven counts after this
/// one sum to it.
/// </param>
/// <param name="SettlesForRecordsChanged">
/// Of those, the ones whose look found the records' watch changed. The watch reads changed
/// where a watched key was written or no longer answers, and where the accounts whose
/// registry hives are loaded have changed, which an account signing in or out does.
/// </param>
/// <param name="SettlesForWatchUnestablished">Those whose look found the watch not established.</param>
/// <param name="SettlesForMutex">Those whose look found the Windows Installer mutex held.</param>
/// <param name="SettlesForInProgressKey">Those whose look found the InProgress registry key.</param>
/// <param name="SettlesForInProgressFile">Those whose look found Windows Installer's in-progress file.</param>
/// <param name="SettlesForService">Those whose look found the Windows Installer service running.</param>
/// <param name="SettlesForOtherSignal">
/// Those whose look found any other sign of Windows Installer, a read of a signal that was
/// refused or did not answer among them.
/// </param>
/// <param name="SettlesForRecordsChangedNotBusy">
/// Of <paramref name="SettlesForRecordsChanged"/>, the ones whose check found Windows
/// Installer not busy at every read it made of it (<see cref="BatchCheckCounts.CallsNeverBusy"/>):
/// a change of the records with no installation to be seen when the batch checked again.
/// </param>
/// <param name="ChecksForRecordsChangedNotBusy">The full checks those settles began.</param>
/// <param name="CheckMillisecondsForRecordsChangedNotBusy">The time spent in those checks.</param>
/// <param name="PutBacks">
/// Files put back in their place because the look made with the file out of its place was
/// not clean.
/// </param>
/// <param name="InUseAtTake">
/// Files refused as in use when they were opened or taken, on their first try.
/// </param>
/// <param name="InUseTakenAtSecondTry">Of those, the files moved or deleted at the one more try after the last file.</param>
/// <param name="FilesNotFinished">
/// Files of the batch that were neither moved nor deleted, filed as an error or held back by
/// a later check, because the batch ended or was cancelled first: the file it was at, back in
/// its place, and every file after it. Nought for a batch that went on to its end.
/// </param>
/// <param name="MovedByCopy">
/// Files a Move sent to its destination by copying them, as it does where the destination is
/// on another volume or which volume it is on was not established. Nought for a Delete.
/// </param>
/// <param name="CopiesMadeAgain">
/// Copies a Move began again because the file was not the one the last copy had been made
/// from when it came to be taken. Nought for a Delete.
/// </param>
public readonly record struct BatchRunCounts(
    int Settles = 0,
    int SettlesForRecordsChanged = 0,
    int SettlesForWatchUnestablished = 0,
    int SettlesForMutex = 0,
    int SettlesForInProgressKey = 0,
    int SettlesForInProgressFile = 0,
    int SettlesForService = 0,
    int SettlesForOtherSignal = 0,
    int SettlesForRecordsChangedNotBusy = 0,
    int ChecksForRecordsChangedNotBusy = 0,
    long CheckMillisecondsForRecordsChangedNotBusy = 0,
    int PutBacks = 0,
    int InUseAtTake = 0,
    int InUseTakenAtSecondTry = 0,
    int FilesNotFinished = 0,
    int MovedByCopy = 0,
    int CopiesMadeAgain = 0);
