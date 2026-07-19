namespace InstallerClean.Models;

/// <summary>
/// An <see cref="InvalidOperationException"/> whose Message has been
/// deliberately constructed from a resx string with only arguments that
/// disclose nothing about the elevating session: fixed-shape values
/// (counts, MSI error codes) in the scan paths, and the caller's own
/// typed destination in the Move paths, which is theirs to be shown
/// back. That makes it safe to surface to a host's UI under elevation.
/// Sites that raise this opt in to having their message echoed;
/// BCL-raised InvalidOperationExceptions from deep in the framework fall
/// through to the generic catch path with a type-name + crash-log
/// breadcrumb.
///
/// Mirrors <see cref="LocalisedAccessException"/>, which does the same
/// for UnauthorizedAccessException. Pattern matches against
/// <see cref="InvalidOperationException"/> still bind via inheritance.
/// </summary>
public sealed class LocalisedInvalidOperationException : InvalidOperationException
{
    public LocalisedInvalidOperationException(string message) : base(message) { }

    public LocalisedInvalidOperationException(string message, Exception innerException)
        : base(message, innerException) { }
}
