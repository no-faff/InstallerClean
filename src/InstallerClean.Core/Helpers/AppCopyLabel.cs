using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// The kind of copy of the program that was running, as the opt-in report carries it.
/// </summary>
public static class AppCopyLabel
{
    /// <summary>
    /// The label for <paramref name="kind"/>: the setup's copy, the portable download, the
    /// command line's own download, Scoop's copy, a build from source, and a copy on a PC
    /// whose setup record could not say. A member this does not name throws, so a kind
    /// added to the enum cannot reach a report under another kind's label. The receiver
    /// holds the field to a fixed set of values, these six and "slim", which copies of
    /// earlier versions send for the slim download, so a label added here is sent only
    /// once the receiver accepts it.
    /// </summary>
    public static string For(AppCopyKind kind) => kind switch
    {
        AppCopyKind.Setup => "setup",
        AppCopyKind.Portable => "portable",
        AppCopyKind.CommandLineDownload => "commandLine",
        AppCopyKind.Scoop => "scoop",
        AppCopyKind.FromSource => "fromSource",
        AppCopyKind.Undetermined => "undetermined",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
