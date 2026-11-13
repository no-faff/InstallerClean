namespace InstallerClean.Models;

/// <summary>
/// An exception whose Message can end in a closing sentence saying what the stop left as
/// it was, such as "Nothing has been removed.". The sentence is true where the stop is
/// shown, at a scan or at the check made before a Move or Delete, and not in crash.log,
/// which also records the scan that runs after a Move or Delete. So
/// <see cref="Helpers.CrashLog.TryWrite"/> writes <see cref="MessageWithoutClosingSentence"/>
/// in place of the Message.
/// </summary>
public interface IClosingSentenceMessage
{
    /// <summary>The whole message, closing sentence included, as every screen shows it.</summary>
    string Message { get; }

    /// <summary>
    /// The message without its closing sentence. The same as <see cref="Message"/> where
    /// the exception was made without one.
    /// </summary>
    string MessageWithoutClosingSentence { get; }
}
