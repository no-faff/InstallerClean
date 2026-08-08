using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Reads a cached package's own identity out of the file.
///
/// A seam for the same reason <c>IMsiApi</c> is one: what the scan does with an
/// unreadable package decides whether a file is offered for permanent deletion,
/// and that branch cannot be reached on a machine where every package reads
/// cleanly. A fake here drives it in a unit test; the production implementation
/// is a thin wrapper over msi.dll.
/// </summary>
public interface IPackageIdentityReader
{
    /// <summary>
    /// The identity of the package at <paramref name="filePath"/>, or null where
    /// the file did not yield one that can be put to Windows as a question.
    ///
    /// NULL IS NOT A DIAGNOSIS AND MUST NOT BE READ AS ONE. It covers a file that
    /// would not open, a database with no Property table, a missing or malformed
    /// code, a patch whose Template names no product, and a package whose shape
    /// this reader does not recognise. What every one of them has in common, and
    /// the whole of what a caller may conclude, is that there is nothing here to
    /// ask about. <paramref name="detail"/> carries the difference for the crash
    /// log alone.
    ///
    /// THE CALLER MUST HAVE GATED THE PATH. This opens the file named and asks it
    /// what it is, so a path that resolves somewhere else answers about somewhere
    /// else. Every production call site runs
    /// <see cref="CandidateGuard.CheckSafeToRemove"/> against the real filesystem
    /// first, which is the app's sanctioned source gate and settles reparse
    /// points and containment together. Repeating that check here would be a
    /// second real-filesystem round trip per candidate, on the one pass whose
    /// cost is measured, closing nothing the gate leaves open.
    /// </summary>
    /// <param name="isPatch">
    /// Which reading to take, decided by the caller from the extension rather
    /// than guessed at here. The two are different files with different
    /// structures: an installation package carries its code in the Property
    /// table, and a patch carries a Property table not at all.
    /// </param>
    /// <param name="detail">
    /// A short, deliberately unlocalised note on which of the failures occurred,
    /// for a crash-log breadcrumb. Empty on success. It names no path: the app
    /// runs elevated and this is written to a log read after a report about some
    /// other file entirely.
    /// </param>
    PackageIdentity? Read(string filePath, bool isPatch, out string detail);
}
