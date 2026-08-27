using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// Renders what a Move or Delete kept back, as one counted sentence naming no
/// cause.
///
/// It lives in Core rather than in either host because the two must answer
/// identically for one machine state and they do not share the code that prints
/// it: the window composes a block for the completion overlay, the command line
/// writes to stdout. The rule has drifted between them before, three of the
/// command line's held-back paths having gone without a fold the window had on
/// all three of its own.
///
/// IT WAS A PARTITION OF FOUR SENTENCES UNTIL 3.0.0, one per cause, on the
/// reading that the causes have no honest superordinate. They need none, because
/// the sentence is not a superordinate over the four. Every file on this line
/// arrived the same way: the scan offered it, and the check made immediately
/// before acting did not confirm it. Both producers only ever drop out of the
/// batch the scan produced, <c>RemovableReverifier.ReverifyAsync</c> over the
/// candidate paths it was handed and the under-lease re-read over the action
/// service's own list, so that is true of every file by construction.
///
/// THE FOUR COUNTS ARE NOT GONE, only the four sentences.
/// <see cref="HeldBackReasons"/> still carries one per cause and they still
/// travel in the opt-in result log, so nothing about diagnosing a machine is
/// lost.
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
    /// The sentence, or empty when nothing was kept back.
    ///
    /// ONE STRING RATHER THAN A LIST, which is what it returned while there were
    /// four sentences to put in order. Ordering was the only thing the list
    /// carried, so with one sentence a collection would be machinery outliving its
    /// reason. A string also makes the drift this class exists to prevent
    /// impossible rather than unlikely: there is nothing left for the two hosts to
    /// join differently. A second sentence would bring the list back with it, and
    /// with an ordering rule that is alive.
    ///
    /// A COUNT OF ZERO MUST NEVER REACH THE SENTENCE. A run that kept nothing back
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
            total);
    }
}
