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
    /// the file did not yield all that the questions about it need.
    ///
    /// NULL IS NOT A DIAGNOSIS AND MUST NOT BE READ AS ONE. It covers a file that
    /// would not open, a database with no Property table, a missing or malformed
    /// code, a patch whose Template names no product, and a package whose shape
    /// this reader does not recognise. What every one of them has in common, and
    /// the whole of what a caller may conclude, is that the file did not yield all
    /// that the questions about it need. <paramref name="detail"/> carries the
    /// difference for the crash log, and <paramref name="refusal"/> sorts it into
    /// two kinds for the report's counts; no decision reads either.
    ///
    /// THE CALLER DECIDES WHAT THE PATH IS TAKEN TO NAME. This opens the file named
    /// and asks it what it is, so a path that resolves somewhere else answers about
    /// somewhere else. A candidate from the folder walk has been through
    /// <see cref="CandidateGuard.CheckSafeToRemove"/> against the real filesystem
    /// before it is read, which is the app's sanctioned source gate and settles
    /// reparse points and containment together. A path recorded by Windows Installer
    /// is read as the file it opens, which is the file Windows Installer opens
    /// through that record. Repeating the gate here would be a second
    /// real-filesystem round trip for every walked candidate, closing nothing the
    /// gate leaves open.
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
    /// <param name="refusal">
    /// Where the answer is null, which of two kinds of failure it was, for a caller
    /// that counts them. Nothing may decide anything on it: null keeps whatever the
    /// caller would keep for it either way.
    /// </param>
    PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal);

    /// <summary>
    /// The package code of the installation package at <paramref name="filePath"/>: the
    /// Revision Number of its summary information, in the braced upper-case form a product
    /// code is read in. Windows Installer records, for each installation, the package code
    /// of the package it cached for it, so a package carrying another package code is not
    /// that package. Null where the file or the value would not read, or the value is not
    /// one braced GUID, and from a reader that does not read package codes; a caller
    /// comparing package codes keeps the file then.
    /// </summary>
    string? PackageCodeOf(string filePath);
}

/// <summary>Reads made through <see cref="IPackageIdentityReader"/>'s own members.</summary>
public static class PackageIdentityReaderExtensions
{
    /// <summary>
    /// <see cref="IPackageIdentityReader.Read(string, bool, out string, out PackageReadRefusal)"/>,
    /// for a caller that does not count the refusals.
    /// </summary>
    public static PackageIdentity? Read(
        this IPackageIdentityReader reader, string filePath, bool isPatch, out string detail) =>
        reader.Read(filePath, isPatch, out detail, out _);
}

/// <summary>
/// Why <see cref="IPackageIdentityReader.Read(string, bool, out string, out PackageReadRefusal)"/>
/// answered null. Meaningless where it answered an identity.
/// </summary>
public enum PackageReadRefusal
{
    /// <summary>
    /// The file did not give up the value: it would not open, a table or stream in it would
    /// not read, a value would not read, or the read raised.
    /// </summary>
    WouldNotRead,

    /// <summary>
    /// The file read, and what it declares is not a code: an installation package whose
    /// <c>ProductCode</c> row is missing, empty or not a well-formed GUID, or a patch whose
    /// code or target list is not set, is stored as something other than text, is not made
    /// of well-formed GUIDs, or names no target.
    /// </summary>
    DeclaresNoCode,
}
