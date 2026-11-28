using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// The sentences saying that a scan put back into the Installer folder files an earlier
/// Move or Delete had left in the app's own folder there
/// (<see cref="IInHandRecovery"/>): the command line's line and the window's line, each
/// naming the files. Empty where nothing was put back.
/// </summary>
internal static class PutBackReport
{
    /// <summary>The command line's line for <paramref name="putBack"/>.</summary>
    internal static string CommandLine(InHandRecoveryResult putBack) =>
        putBack.PutBack.Count == 0 ? string.Empty : string.Format(Strings.Cli_ScanPutBack, Names(putBack.PutBack));

    /// <summary>The window's line for the files at <paramref name="paths"/>.</summary>
    internal static string WindowLine(IReadOnlyCollection<string> paths) =>
        paths.Count == 0 ? string.Empty : string.Format(Strings.Summary_PutBack, Names(paths));

    /// <summary>The files' names, without their folder, separated by a comma and a space.</summary>
    internal static string Names(IEnumerable<string> paths) => string.Join(", ", paths.Select(PathText.GetFileName));
}
