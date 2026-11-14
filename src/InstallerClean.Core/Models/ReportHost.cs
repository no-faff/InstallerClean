namespace InstallerClean.Models;

/// <summary>Which of the two programs made the run an opt-in report describes.</summary>
public enum ReportHost
{
    /// <summary>InstallerClean, the window.</summary>
    Window,

    /// <summary>The command-line tool.</summary>
    CommandLine,
}
