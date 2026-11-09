using System.ComponentModel;
using System.Diagnostics;
using InstallerClean.Helpers;

namespace InstallerClean.Compatibility;

/// <summary>
/// <see cref="Process.Start(ProcessStartInfo)"/> with .NET 10's exception for a start Windows
/// refuses, from <c>Process.Windows.cs</c>'s <c>StartWithCreateProcess</c> and
/// <c>Process.cs</c>'s <c>CreateExceptionForErrorStartingProcess</c> (dotnet/runtime
/// v10.0.0). .NET Framework throws a <see cref="Win32Exception"/> carrying only the text
/// Windows gives for the error; .NET 10's names the file and the working folder as well.
/// </summary>
internal static class FrameworkProcess
{
    private const int ErrorBadExeFormat = 193;
    private const int ErrorExeMachineTypeMismatch = 216;

    /// <summary>
    /// Starts <paramref name="info"/> and answers the process, or null where Windows started
    /// none. Where <paramref name="info"/> does not use the shell, a start Windows refuses is
    /// a <see cref="Win32Exception"/> with the same error code, whose message reads "An error
    /// occurred trying to start process '<i>file</i>' with working directory
    /// '<i>folder</i>'. <i>reason</i>", the folder being the current one where
    /// <paramref name="info"/> names none and the reason the text Windows gives for the
    /// error, as <see cref="WindowsErrors.SystemMessage"/> reads it. A start through the shell throws
    /// what .NET Framework throws, as .NET 10 wraps only a refusal of CreateProcess.
    /// </summary>
    public static Process? Start(ProcessStartInfo info)
    {
        try
        {
            return Process.Start(info);
        }
        catch (Win32Exception ex) when (!info.UseShellExecute)
        {
            var reason = ex.NativeErrorCode is ErrorBadExeFormat or ErrorExeMachineTypeMismatch
                ? "The specified executable is not a valid application for this OS platform."
                : WindowsErrors.SystemMessage(ex.NativeErrorCode);
            var folder = string.IsNullOrEmpty(info.WorkingDirectory)
                ? Environment.CurrentDirectory
                : info.WorkingDirectory;
            throw new Win32Exception(
                ex.NativeErrorCode,
                $"An error occurred trying to start process '{info.FileName}' with working directory '{folder}'. {reason}");
        }
    }
}
