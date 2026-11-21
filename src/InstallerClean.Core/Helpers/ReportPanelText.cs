using InstallerClean.Resources;

namespace InstallerClean.Helpers;

/// <summary>
/// The words of the panel the report box's "i" opens: the list it draws, and the whole
/// panel as one line for the screen reader. The window builds the inlines and the two
/// links from the same strings, so what is read and what is drawn cannot drift apart.
/// </summary>
internal static class ReportPanelText
{
    /// <summary>
    /// The lines of the panel's list, one for each kind of thing the report carries, in
    /// the order they are shown.
    /// </summary>
    internal static string[] Lines =>
    [
        Strings.Completion_ReportPanel_Freed,
        Strings.Completion_ReportPanel_Destination,
        Strings.Completion_ReportPanel_Durations,
        Strings.Completion_ReportPanel_InstallerFiles,
        Strings.Completion_ReportPanel_LeftAlone,
        Strings.Completion_ReportPanel_Waits,
        Strings.Completion_ReportPanel_Records,
        Strings.Completion_ReportPanel_ShortNames,
        Strings.Completion_ReportPanel_Windows,
        Strings.Completion_ReportPanel_AppVersion,
        Strings.Completion_ReportPanel_Errors,
        Strings.Completion_ReportPanel_ReportId,
    ];

    /// <summary>
    /// The panel's words as lines for a console: the opening sentence, which names the chart
    /// by <paramref name="chartPhrase"/>, then each line of the list behind a hyphen, then
    /// the closing line. The window draws the chart's name as a link, which a console cannot,
    /// so the phrase there names the chart's address.
    /// </summary>
    internal static string[] ConsoleLines(string chartPhrase)
    {
        var intro = string.Format(Strings.Cli_ReportIntro, chartPhrase);
        return [intro, .. Lines.Select(line => "- " + line), Strings.Completion_ReportPanel_Closing];
    }

    /// <summary>
    /// The panel's words as one line for the screen reader: <paramref name="intro"/>, the
    /// opening sentence as the window draws it, then each line of the list, ended the way
    /// the displayed language ends a sentence, then the closing line and the name of the
    /// link to the report window. The opening sentence ends in its own colon and the
    /// closing line in its own stop, so each is followed by
    /// <see cref="DisplayHelpers.SentenceSpace"/>; the separator there would put a second
    /// stop after the first.
    /// </summary>
    internal static string SpokenLine(string intro)
    {
        var separator = Strings.Display_SentenceSeparator;
        var space = DisplayHelpers.SentenceSpace;
        return intro + space + string.Join(separator, Lines) + separator
            + Strings.Completion_ReportPanel_Closing + space + Strings.Completion_ReportPanel_SeeReport;
    }
}
