using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The whole message of a scan's or a re-check's stop as every screen shows it: the
/// stop's own text and its closing sentence, joined as the app's language joins two
/// sentences.
/// </summary>
internal static class StopMessages
{
    internal static string NothingRemoved(string message) =>
        message + DisplayHelpers.SentenceSpace + Strings.Error_NothingRemoved;

    internal static string NothingOfferedNothingRemoved(string message) =>
        message + DisplayHelpers.SentenceSpace + Strings.Error_NothingOfferedNothingRemoved;
}
