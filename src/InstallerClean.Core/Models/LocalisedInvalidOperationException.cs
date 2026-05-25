namespace InstallerClean.Models;

/// <summary>
/// An <see cref="InvalidOperationException"/> whose Message has been
/// deliberately constructed from a resx string with only fixed-shape
/// template arguments (counts, error codes), making it safe to surface
/// to the WPF dialog under elevation. Sites that raise this opt in to
/// having their message echoed; BCL-raised InvalidOperationExceptions
/// from deep in the framework fall through to the generic catch path
/// with a type-name + crash-log breadcrumb.
///
/// Mirrors <see cref="LocalisedAccessException"/> for the CLI's
/// UnauthorizedAccessException catch. Pattern matches against
/// <see cref="InvalidOperationException"/> still bind via inheritance.
/// </summary>
public sealed class LocalisedInvalidOperationException : InvalidOperationException
{
    public LocalisedInvalidOperationException(string message) : base(message) { }

    public LocalisedInvalidOperationException(string message, Exception innerException)
        : base(message, innerException) { }
}
