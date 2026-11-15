namespace InstallerClean.Models;

/// <summary>
/// Persisted user preferences. Serialised to
/// <c>settings.json</c> in the app's own folder (<see cref="Helpers.AppDataFolder"/>) by
/// <see cref="Services.ISettingsService"/>. New fields must be optional /
/// have a default so an older file deserialises cleanly into a newer
/// schema and a corrupt-file <c>.bad</c> backup is only triggered for
/// genuinely unreadable JSON, not version skew.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Folder last picked for the Move-orphans operation. Empty until
    /// the first Move, and Move is offered with it empty: the command
    /// asks for a destination at the point of use and writes the
    /// choice back here.
    /// </summary>
    /// <remarks>
    /// Validation contract: the textbox accepts any string and the
    /// debounced write to settings.json never validates, so a
    /// hand-edited settings.json, or one carried in from a prior
    /// install with a since-invalidated destination, reaches the Move
    /// with nothing having checked it. The
    /// <c>IsInstallerFolderOrChild</c> / <c>IsSystemFolderOrChild</c>
    /// gates therefore run at use time: in
    /// <c>CleanupViewModel.MoveAllAsync</c>, in the CLI's
    /// <c>Program.cs</c>, and again inside <c>MoveFilesService</c>,
    /// which is the one no caller can route around.
    /// </remarks>
    public string MoveDestination { get; set; } = string.Empty;

    /// <summary>
    /// True once this account's report has been sent and accepted. The report goes
    /// from the PC's first finished card, as the card closes with its box ticked,
    /// or from a later start retrying one that did not go
    /// (<see cref="ReportSendPending"/>). The window's start check also reads it as an
    /// earlier run in this account. Nothing clears it.
    /// </summary>
    public bool HasSentResultLog { get; set; }

    /// <summary>
    /// True from the person's yes to this account's report until the report is
    /// accepted: set as the first finished card closes with its box ticked, which
    /// closing the window with the card up does too, and as the command line's
    /// question is answered yes. It is set false once the report is accepted, and
    /// where the command line's send is cancelled. A start that finds it true with
    /// <see cref="HasSentResultLog"/> false sends <c>last-run.json</c>, and every
    /// later start tries again until one is accepted. A <c>last-run.json</c> with no
    /// true beside it is never sent: a box left unticked, a question answered no, a
    /// process that crashed or was ended while its card was still up.
    /// </summary>
    public bool ReportSendPending { get; set; }

    /// <summary>
    /// Whether the app asks GitHub for a newer version once per session,
    /// started at launch. Defaults to true (absent from
    /// an older settings file reads as true, so existing installs gain the
    /// check on upgrade, matching how mainstream desktop apps behave). A
    /// typical session lasts seconds, so the check races the startup scan
    /// rather than waiting behind it: any delay would routinely land the
    /// answer after the user had closed the window. The check itself stays
    /// quiet but for a newer version, which shows the notice at the top of the
    /// main window; an up-to-date result and a failure both say nothing. The off
    /// switch is the checkbox in the About window; the check reads this file as it
    /// fires, on the startup path, before there is a window to untick it
    /// from, so a change made in one session governs the next launch and
    /// every one after. It gates the automatic check only: the main
    /// window's update button checks on demand whatever this says.
    /// </summary>
    public bool AutoUpdateCheck { get; set; } = true;

    /// <summary>
    /// UI-language preference. <c>null</c> or absent means Automatic:
    /// follow the Windows display language. A non-null value is a culture
    /// name the app ships a translation for (an entry of
    /// <c>SupportedLanguages.CultureNames</c>), validated against that list
    /// at startup and otherwise ignored. Applied by <c>App.OnStartup</c> before any window
    /// loads; changing it in the bottom-bar language menu takes effect on the next launch.
    /// </summary>
    public string? Language { get; set; }
}
