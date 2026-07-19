namespace InstallerClean.Models;

/// <summary>
/// Persisted user preferences. Serialised to
/// <c>%LOCALAPPDATA%\NoFaff\InstallerClean\settings.json</c> by
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
    /// Set to true once the user has successfully sent a result log
    /// to No Faff. The Send button is then hidden across sessions and
    /// across version upgrades, so the receiving cohort is each
    /// machine's first-ever submission rather than a low-impact rerun.
    /// One report per intact settings file: if the JSON ever becomes
    /// unreadable, <see cref="Services.SettingsService"/>.Load renames
    /// it to <c>settings.json.bad</c> and returns defaults, which
    /// re-enables the prompt. The receiver does not deduplicate, so a
    /// machine whose settings file is corrupted, deleted, or roaming-
    /// profile-clobbered between sessions can submit again.
    /// </summary>
    public bool HasSentResultLog { get; set; }

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
