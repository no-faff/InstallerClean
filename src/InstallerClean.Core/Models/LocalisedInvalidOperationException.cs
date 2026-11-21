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
public class LocalisedInvalidOperationException : InvalidOperationException, IClosingSentenceMessage
{
    public LocalisedInvalidOperationException(string message) : base(message)
    {
        MessageWithoutClosingSentence = message;
    }

    public LocalisedInvalidOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
        MessageWithoutClosingSentence = message;
    }

    /// <summary>
    /// <paramref name="message"/> followed by <paramref name="closingSentence"/>, joined
    /// as the language the app is showing joins two sentences. crash.log leaves the
    /// closing sentence out (<see cref="IClosingSentenceMessage"/>).
    /// </summary>
    public LocalisedInvalidOperationException(string message, string closingSentence)
        : base(message + Helpers.DisplayHelpers.SentenceSpace + closingSentence)
    {
        MessageWithoutClosingSentence = message;
    }

    /// <inheritdoc cref="LocalisedInvalidOperationException(string, string)"/>
    public LocalisedInvalidOperationException(string message, string closingSentence, Exception innerException)
        : base(message + Helpers.DisplayHelpers.SentenceSpace + closingSentence, innerException)
    {
        MessageWithoutClosingSentence = message;
    }

    /// <inheritdoc/>
    public string MessageWithoutClosingSentence { get; }
}
