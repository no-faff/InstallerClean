using System.Diagnostics;
using InstallerClean.Compatibility;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Starts a newer version or a newer setup, trying again where the start fails. An
/// antivirus can hold a program it has just seen written for a moment, so a start that
/// fails is tried <see cref="Attempts"/> times, <see cref="Interval"/> apart.
/// </summary>
internal static class ProcessStartRetry
{
    /// <summary>How many times a start is tried before the failure is the answer.</summary>
    public const int Attempts = 4;

    /// <summary>The wait between one failed try and the next.</summary>
    public static readonly TimeSpan Interval = Durations.FromMilliseconds(500);

    /// <summary>
    /// Starts <paramref name="info"/> and answers the process. A throw that
    /// <paramref name="retries"/> admits, on any try but the last, is written to crash.log
    /// and followed by <paramref name="wait"/> and another try. Any other throw, and the
    /// last try's, reaches the caller unwritten, and so does a throw from
    /// <paramref name="wait"/>, which ends the tries: that is how a caller cancels between
    /// them. Runs on the calling thread throughout.
    /// </summary>
    public static Process Start(ProcessStartInfo info, Func<Exception, bool> retries, Action<TimeSpan> wait)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return FrameworkProcess.Start(info)
                    ?? throw new InvalidOperationException("Windows started no process for the file.");
            }
            catch (Exception ex) when (attempt < Attempts && retries(ex))
            {
                CrashLog.TryWrite(ex);
                wait(Interval);
            }
        }
    }
}
