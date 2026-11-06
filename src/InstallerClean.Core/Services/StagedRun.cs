using InstallerClean.Helpers;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// A newer command line an older copy downloaded, running the command from the staging
/// folder standing for that copy (<see cref="HandOverContract.ReadReplaceRequest"/>): the
/// check that the copy it stands for is running, and the start of the replace once the
/// command has ended.
/// </summary>
internal static class StagedRun
{
    /// <summary>
    /// Opens the process <paramref name="request"/> names where it runs from the plain file
    /// at its path (<see cref="ExeReplacement.OpenCopy"/>), so this run may stand for that
    /// copy, and answers its handle, which the caller holds until the replace has been
    /// started: while it is open, no other process can be given that process's ID. Where it
    /// does not, answers null, and crash.log says what was found. Never throws.
    /// </summary>
    public static SafeProcessHandle? HoldCopy(ReplaceRequest request)
    {
        try
        {
            var copy = ExeReplacement.OpenCopy(request, out var refused);
            if (copy is null) CrashLog.TryWrite(refused!);
            return copy;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return null;
        }
    }

    /// <summary>
    /// Starts <paramref name="thisExe"/>, this run's own file, to put itself at
    /// <paramref name="request"/>'s path once the process the request names has ended:
    /// detached (<see cref="DetachedProcess"/>), working in the staging folder, with the
    /// request's path and process ID and an event of its own, never the request's. Waits up
    /// to <see cref="HandOverContract.StartBound"/> for that event, and answers as
    /// <see cref="NewerVersionProcess.StartDetachedAndAwait"/> does, with what failed in
    /// crash.log. Where the replace could not be started, or was ended for not setting its
    /// event in time, the Application log gets the notice that the update was not installed
    /// (<see cref="CommandLineReplacement.WriteNotInstalled"/>); a replace that ended by itself
    /// has written its own. Never throws.
    /// </summary>
    public static NewerVersionAnswer StartReplace(ReplaceRequest request, string thisExe) =>
        StartReplace(request, thisExe, [], HandOverContract.StartBound);

    /// <summary>
    /// Test seam: <see cref="StartReplace(ReplaceRequest, string)"/>, with <paramref name="before"/>
    /// ahead of the replace arguments and <paramref name="bound"/> for the wait.
    /// </summary>
    internal static NewerVersionAnswer StartReplace(
        ReplaceRequest request, string thisExe, IReadOnlyList<string> before, TimeSpan bound)
    {
        var answer = NewerVersionProcess.StartDetachedAndAwait(
            thisExe,
            id => [.. before, .. HandOverContract.ReplaceArguments(request.ExePath, request.ProcessId, id)],
            PathText.GetDirectoryName(thisExe) ?? string.Empty,
            bound,
            out var endedByItself);
        if (answer == NewerVersionAnswer.NotStarted && !endedByItself) CommandLineReplacement.WriteNotInstalled();
        return answer;
    }
}
