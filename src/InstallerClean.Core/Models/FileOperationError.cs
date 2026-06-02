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
/// constraints. The displayed message is category-only via the resx;
/// the underlying exception message is intentionally not retained on
/// the record because under elevation it can include paths from other
/// users' profiles, and a record field that's only ever written can
/// leak via accidental serialisation or logging.
/// </summary>
public sealed record AccessDenied(string FilePath)
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
/// The shell IFileOperation API returned a failure HRESULT while
/// recycling the file, and the file was left in place. Delete only.
/// <see cref="HResult"/> is the raw shell code, retained for telemetry;
/// the displayed sentence stays category-level for the same path-leak
/// reason as <see cref="AccessDenied"/>.
/// </summary>
public sealed record RecycleFailed(string FilePath, int HResult)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage =>
        // HResult is a COM HRESULT; hex keeps a top-bit-set code
        // recognisable (E_FAIL as 0x80004005, not the signed decimal
        // -2147467259 the bare {0} would render).
        string.Format(Strings.Error_ShellRecycleFailed, $"0x{HResult:X8}");
}

/// <summary>
/// The file was deleted but could not be sent to the Recycle Bin, so it
/// is gone permanently. The shell IFileOperation recycle is
/// recycle-or-permanently-delete: when the bin is unavailable a file is
/// nuked while every HRESULT still reports success. This category
/// records that honestly when it happens without the user having
/// consented to permanent deletion. Delete only.
/// </summary>
public sealed record PermanentlyDeleted(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_DeletedNotRecycled;
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
/// Generic IO failure (disk full, sharing violation, etc). The UI sees
/// only a category-only sentence; the underlying exception message
/// stays off the record for the same reason as <see cref="AccessDenied"/>.
/// </summary>
public sealed record IOFailure(string FilePath)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Strings.Error_IOFailure;
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
    public override string LocalisedMessage => Strings.Error_UnknownError;
}
