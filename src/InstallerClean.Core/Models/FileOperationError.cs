using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Models;

/// <summary>
/// Categorised per-file failure produced by Move and Delete operations.
/// The services collect these into a result list and let the caller
/// decide how to surface them. The categorisation lets the completion
/// overlay group failures by cause ("3 access denied, 1 missing
/// source") rather than scrolling a list of free-form sentences.
///
/// Each subtype carries the structured fields needed to reconstruct
/// the message, plus a <see cref="LocalisedMessage"/> property that
/// produces a culture-appropriate sentence for display. The UI binds
/// to <see cref="LocalisedMessage"/>; counters and grouping pattern-
/// match on the subtype.
/// </summary>
public abstract record FileOperationError(string FilePath)
{
    /// <summary>
    /// Human-readable description of ONE file's failure, in the current UI
    /// culture. Singular throughout: the CLI prints it per file after a
    /// filename and a colon, so a category that also has a plural form still
    /// answers here with the sentence about a single file.
    /// </summary>
    public abstract string LocalisedMessage { get; }

    /// <summary>
    /// Heading the completion overlay puts above a bucket of
    /// <paramref name="count"/> filenames that all failed this way. The default
    /// is the singular sentence, which is right for the categories that are
    /// either rare or already read as a complete statement whatever the count
    /// (a missing source, a reparse point, an out-of-cache candidate). A
    /// category that reads wrong over a list overrides this with a properly
    /// pluralised introducer, which also gives an inflecting language the
    /// satellite-only .One/.Few/.Many override slot it needs.
    /// </summary>
    public virtual string LocalisedGroupHeading(int count) => LocalisedMessage;
}

/// <summary>The source file disappeared between the scan and the operation.</summary>
public sealed record MissingSourceFile(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_MissingSourceFile;
}

/// <summary>
/// Windows refused the operation due to permission, sharing or path
/// constraints. The displayed message is category-only via the resx;
/// the underlying exception message is intentionally not retained on
/// the record because under elevation it can include paths from other
/// users' profiles, and a record field that's only ever written can
/// leak via accidental serialisation or logging.
/// </summary>
public sealed record AccessDenied(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_AccessDenied_Singular;

    public override string LocalisedGroupHeading(int count) =>
        DisplayHelpers.Pluralise(count, Strings.Error_AccessDenied_Singular,
            Strings.Error_AccessDenied_Plural, "Error.AccessDenied");
}

/// <summary>
/// A file of that name was already in the destination folder, so Move refused
/// this one and left it where it was. Move only.
///
/// IT USED TO MEAN THE OPPOSITE OF WHAT IT MEANS NOW. Move appended " (1)",
/// " (2)" and so on until it found a free name, and this category was raised
/// only when 10,000 of those all collided. The renaming made the completion
/// screen's own restore line false for the file it renamed, Windows Installer
/// looking for a cached package by the exact path it recorded, so the renaming
/// went and the first collision is now the refusal.
///
/// THE MESSAGE STILL DESCRIBES THE OLD MEANING AND THAT IS DELIBERATE FOR NOW.
/// <c>Error.NoUniqueFilename</c> names 10,000 attempts, which this can no longer
/// be, and it is a known-false line logged and left rather than quietly weakened:
/// the English value and its fifteen translations move together in the
/// translation round, and changing the English alone would leave every other
/// language carrying the false sentence on its own.
///
/// The key name has the same problem and stays for the same reason.
/// </summary>
public sealed record DestinationCollision(string FilePath, string FileName)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage =>
        string.Format(Strings.Error_NoUniqueFilename, FileName);
}

/// <summary>
/// Source file is a symlink or junction. Move and Delete refuse these so the
/// operation can't follow a reparse point out of C:\Windows\Installer.
/// </summary>
public sealed record SourceIsReparsePoint(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_SourceIsReparsePoint;
}

/// <summary>
/// The candidate does not resolve to a file directly in
/// <c>C:\Windows\Installer</c> (it is outside the cache, or below its root), so
/// Move and Delete refuse it at the service boundary. A candidate should never
/// reach here (both are already filtered where they are created), but a corrupt
/// <c>LocalPackage</c> registration pointing outside the cache would otherwise
/// make an arbitrary file a removal target; this is the choke point that makes
/// that structurally impossible. The path is kept off the displayed sentence
/// for the same elevated path-leak reason as <see cref="AccessDenied"/>.
/// </summary>
public sealed record CandidateOutsideCache(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_CandidateOutsideCache;
}

/// <summary>
/// The file is open or locked by another program, so it could not be
/// removed and was left in place. Move and Delete both reach it, both
/// discriminating the same two HRESULTs off an IOException, which is why the
/// sentence says "remove" rather than naming either verb.
///
/// Split out of <see cref="IOFailure"/> because it is the one IO failure
/// with a cause the user can act on, and the only one that is not a fault:
/// closing the holding program and running again fixes it. The underlying
/// exception message stays off the record for the same path-leak reason as
/// <see cref="AccessDenied"/>; the full exception goes to crash.log at the
/// catch site.
/// </summary>
public sealed record FileInUse(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_FileInUse_Singular;

    public override string LocalisedGroupHeading(int count) =>
        DisplayHelpers.Pluralise(count, Strings.Error_FileInUse_Singular,
            Strings.Error_FileInUse_Plural, "Error.FileInUse");
}

/// <summary>
/// Generic IO failure (disk full, path too long, a device that went away).
/// A sharing or lock violation is NOT one of these: it is discriminated at
/// the catch site and filed as <see cref="FileInUse"/>, so this category
/// means "an IO failure with no cause we can name". The UI sees
/// only a category-only sentence; the underlying exception message
/// stays off the record for the same reason as <see cref="AccessDenied"/>.
/// </summary>
public sealed record IOFailure(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_IOFailure_Singular;

    public override string LocalisedGroupHeading(int count) =>
        DisplayHelpers.Pluralise(count, Strings.Error_IOFailure_Singular,
            Strings.Error_IOFailure_Plural, "Error.IOFailure");
}

/// <summary>
/// Catch-all for exception types not covered by the specific
/// categories. The displayed message is category-only via the resx;
/// the underlying exception message and runtime type name stay off
/// the record for the same reason as <see cref="AccessDenied"/>.
/// </summary>
public sealed record UnknownError(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_UnknownError_Singular;

    public override string LocalisedGroupHeading(int count) =>
        DisplayHelpers.Pluralise(count, Strings.Error_UnknownError_Singular,
            Strings.Error_UnknownError_Plural, "Error.UnknownError");
}
