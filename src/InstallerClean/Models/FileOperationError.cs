using InstallerClean.Resources;

namespace InstallerClean.Models;

/// <summary>
/// Categorised per-file failure produced by Move and Delete operations.
///
/// The Move and Delete services collect these into a list and let the
/// caller decide how to surface them. The categorisation lets the UI
/// (and a future telemetry hook) group failures by cause - e.g. "3
/// access denied, 1 missing source" - rather than scrolling a list of
/// free-form sentences.
///
/// Each subtype carries the structured fields needed to reconstruct
/// the message, plus a <see cref="LocalisedMessage"/> property that
/// produces a culture-appropriate sentence for display. Consumers
/// should bind to <see cref="LocalisedMessage"/> for the UI string;
/// they should pattern-match on the subtype for grouping/counting.
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
/// constraints. <see cref="Detail"/> carries the underlying
/// exception message for diagnosis.
/// </summary>
public sealed record AccessDenied(string FilePath, string Detail)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Detail;
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

/// <summary>Generic IO failure (disk full, sharing violation, etc).</summary>
public sealed record IOFailure(string FilePath, string Detail)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Detail;
}

/// <summary>
/// Catch-all for exception types not covered by the specific
/// categories. <see cref="ExceptionTypeName"/> is the runtime type
/// name, useful for telemetry; <see cref="Detail"/> is the exception
/// message.
/// </summary>
public sealed record UnknownError(string FilePath, string ExceptionTypeName, string Detail)
    : FileOperationError(FilePath)
{
    public override string LocalisedMessage => Detail;
}
