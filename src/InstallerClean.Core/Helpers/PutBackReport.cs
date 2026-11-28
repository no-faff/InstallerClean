using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// The sentences saying what a scan did with the files an earlier Move or Delete had left
/// in the app's own folder inside the Installer folder (<see cref="IInHandRecovery"/>):
/// those it put back, those it left for a later scan to try again, and those it left as
/// the place a program or patch was installed from. One line for each of the three that
/// has a file, each naming its files, on the command line and in the window. Empty where a
/// list is empty.
///
/// THE FILES LEFT ARE SPLIT BY WHAT THE APP DOES WITH THEM, NEVER BY ONE CAUSE: a later
/// scan tries again with every file left for Windows Installer being busy, the records not
/// all reading, its name taken or a refusal, so that line names no cause; only the files
/// left as a source share one.
/// </summary>
internal static class PutBackReport
{
    /// <summary>The command line's line for the files <paramref name="putBack"/> put back.</summary>
    internal static string CommandLine(InHandRecoveryResult putBack) =>
        putBack.PutBack.Count == 0 ? string.Empty : string.Format(Strings.Cli_ScanPutBack, Names(putBack.PutBack));

    /// <summary>The command line's lines for what <paramref name="putBack"/> did, in order, each one there is.</summary>
    internal static IEnumerable<string> CommandLines(InHandRecoveryResult putBack)
    {
        if (putBack.PutBack.Count > 0) yield return CommandLine(putBack);
        if (putBack.LeftForALaterScan.Count > 0)
            yield return string.Format(Strings.Cli_ScanLeftInHand, Names(putBack.LeftForALaterScan));
        if (putBack.LeftAsSourceNames.Count > 0)
            yield return string.Format(Strings.Cli_ScanLeftAsSource, Names(putBack.LeftAsSourceNames));
    }

    /// <summary>The window's line for the files at <paramref name="paths"/>.</summary>
    internal static string WindowLine(IReadOnlyCollection<string> paths) =>
        paths.Count == 0 ? string.Empty : string.Format(Strings.Summary_PutBack, Names(paths));

    /// <summary>
    /// The window's lines for the files <paramref name="left"/> left where they lie, the
    /// files for a later scan first, one line to a list that has a file, joined by a line
    /// break. Empty where it left none.
    /// </summary>
    internal static string WindowLeftLines(InHandRecoveryResult left) =>
        string.Join(Environment.NewLine, new[]
        {
            left.LeftForALaterScan.Count == 0 ? string.Empty
                : string.Format(Strings.Summary_LeftInHand, Names(left.LeftForALaterScan)),
            left.LeftAsSourceNames.Count == 0 ? string.Empty
                : string.Format(Strings.Summary_LeftAsSource, Names(left.LeftAsSourceNames)),
        }.Where(line => line.Length > 0));

    /// <summary>The files' names, without their folder, separated by a comma and a space.</summary>
    internal static string Names(IEnumerable<string> paths) => string.Join(", ", paths.Select(PathText.GetFileName));
}
