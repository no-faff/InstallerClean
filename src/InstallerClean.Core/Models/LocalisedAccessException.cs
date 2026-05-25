namespace InstallerClean.Models;

/// <summary>
/// An <see cref="UnauthorizedAccessException"/> whose Message has been
/// deliberately constructed from a resx string with only user-controlled
/// template arguments (the caller's own typed path), making it safe to
/// surface to the CLI's stdout under elevation. Sites that raise this
/// opt in to having their message echoed; BCL-raised
/// UnauthorizedAccessExceptions that come from deep in the framework
/// fall through to the generic catch path with a type-name + crash-log
/// breadcrumb.
///
/// The WPF host's pattern matching against <see cref="UnauthorizedAccessException"/>
/// still binds via inheritance, so a switch arm that distinguishes
/// access-denied from other write failures does not need to know about
/// this subclass.
/// </summary>
public sealed class LocalisedAccessException : UnauthorizedAccessException
{
    public LocalisedAccessException(string message) : base(message) { }

    public LocalisedAccessException(string message, Exception innerException)
        : base(message, innerException) { }
}
