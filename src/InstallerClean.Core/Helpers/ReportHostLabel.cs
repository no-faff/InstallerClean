using InstallerClean.Models;

namespace InstallerClean.Helpers;

/// <summary>
/// The program whose run the opt-in report describes, as the report carries it.
/// </summary>
public static class ReportHostLabel
{
    /// <summary>What <see cref="For"/> answers for <see cref="ReportHost.Window"/>.</summary>
    public const string Window = "window";

    /// <summary>What <see cref="For"/> answers for <see cref="ReportHost.CommandLine"/>.</summary>
    public const string CommandLine = "commandLine";

    /// <summary>
    /// The label for <paramref name="host"/>. A member this does not name throws, so a host
    /// added to the enum cannot reach a report under another host's label. The receiver
    /// holds the field to exactly these two values, so a label added here is sent only once
    /// the receiver accepts it.
    /// </summary>
    public static string For(ReportHost host) => host switch
    {
        ReportHost.Window => Window,
        ReportHost.CommandLine => CommandLine,
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, null),
    };
}
