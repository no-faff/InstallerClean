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
    /// <summary>Human-readable description in the current UI culture.</summary>
    public abstract string LocalisedMessage { get; }
}

/// <summary>The source file disappeared between the scan and the operation.</summary>
public sealed record MissingSourceFile(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_MissingSourceFile;
}

/// <summary>
/// Windows refused the operation due to permission, sharing or path
/// constraints. <see cref="Detail"/> carries the underlying exception
/// message for diagnosis but is NEVER returned to the UI: with the
/// app running elevated, framework-provided messages can include
/// paths from other users' profiles. The displayed message is
/// category-only via the resx; the Detail is captured for crash log
/// / telemetry consumers.
/// </summary>
public sealed record AccessDenied(string FilePath, string Detail)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_AccessDenied;
}

/// <summary>
/// Move couldn't pick a unique filename in the destination folder
/// (the unique-suffix pattern was exhausted - thousands of collisions).
/// Move only.
/// </summary>
public sealed record DestinationCollision(string FilePath, string FileName)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage =>
        string.Format(Strings.Error_NoUniqueFilename, FileName);
}

/// <summary>
/// SHFileOperationW returned a non-zero shell error code while
/// recycling the file. Delete only.
/// </summary>
public sealed record ShellRefused(string FilePath, int ShellResult)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage =>
        string.Format(Strings.Error_ShellRecycleFailed, ShellResult);
}

/// <summary>
/// Source file is a symlink or junction. Move refuses these so the
/// move can't follow a reparse point out of C:\Windows\Installer.
/// </summary>
public sealed record SourceIsReparsePoint(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_SourceIsReparsePoint;
}

/// <summary>
/// Generic IO failure (disk full, sharing violation, etc).
/// <see cref="Detail"/> stays for crash-log / telemetry; the UI sees
/// only a category-only sentence so framework-provided paths stay
/// out of the displayed error list (see <see cref="AccessDenied"/>).
/// </summary>
public sealed record IOFailure(string FilePath, string Detail)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_IOFailure;
}

/// <summary>
/// Catch-all for exception types not covered by the specific
/// categories. <see cref="ExceptionTypeName"/> is the runtime type
/// name, useful for telemetry; <see cref="Detail"/> is the exception
/// message. Both stay out of the UI; <see cref="LocalisedMessage"/>
/// returns a category-only sentence (see <see cref="AccessDenied"/>
/// for the path-leak rationale).
/// </summary>
public sealed record UnknownError(string FilePath, string ExceptionTypeName, string Detail)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_UnknownError;
}
