using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// Renders what a Move or Delete held back, as one counted sentence naming no
/// cause.
///
/// It lives in Core rather than in either host because the two must answer
/// identically for one machine state and they do not share the code that prints
/// it: the window composes a block for the completion overlay, the command line
/// writes to stdout.
///
/// ONE SENTENCE OVER EVERY CAUSE, AND IT NAMES NONE OF THEM. It is not a
/// superordinate over the causes and needs none. Every file on this line arrived
/// the same way: the scan offered it, and the check made immediately before acting
/// did not confirm it. Both producers only ever drop out of the batch the scan
/// produced, <c>RemovableReverifier.ReverifyAsync</c> over the candidate paths it
/// was handed and the under-lease re-read over the action service's own list, so
/// that is true of every file by construction.
///
/// <see cref="HeldBackReasons"/> carries one count per cause, and the counts travel
/// in the opt-in result log, which is where a machine's causes are told apart.
///
/// WHO READS IT DECIDES THE WORDING, and it is a narrower audience than it looks.
/// Somebody who has already pressed Move or Delete, on the completion screen
/// beside the count and the size, and nobody else ever. Its whole job is that the
/// numbers add up: the heading says 26 deleted where the user selected 29. It is
/// not there to teach anybody about Windows Installer, and the files are not
/// stranded, the app running a full rescan before the overlay appears so they are
/// on the rebuilt list behind it, judged fresh.
/// </summary>
internal static class HeldBackReport
{
    /// <summary>
    /// The sentence, or empty when nothing was held back.
    ///
    /// ONE STRING RATHER THAN A LIST, so there is nothing for the two hosts to join
    /// differently. A second sentence would need a list and a rule for its order.
    ///
    /// A COUNT OF ZERO MUST NEVER REACH THE SENTENCE. A run that held nothing back
    /// prints nothing at all, which is the commonest run by far, and that is what
    /// the empty string here is for rather than a "0 files" line.
    /// </summary>
    internal static string Line(HeldBackReasons reasons)
    {
        var total = reasons.Total;
        if (total == 0) return string.Empty;

        return string.Format(
            DisplayHelpers.Pluralise(total,
                Strings.Completion_HeldBack_Singular,
                Strings.Completion_HeldBack_Plural,
                "Completion.HeldBack"),
            DisplayHelpers.FormatCount(total));
    }
}
