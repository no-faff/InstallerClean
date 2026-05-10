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
    /// Folder the user last picked for the Move-orphans operation.
    /// Empty until the first Move. The Move pill stays disabled while
    /// this is empty, so the user must Browse for a destination at
    /// least once before they can run their first Move.
    /// </summary>
    /// <remarks>
    /// Validation contract: the textbox accepts any string and the
    /// debounced write to settings.json never validates. The
    /// <c>IsInstallerFolderOrChild</c> / <c>IsSystemFolderOrChild</c>
    /// gates run at use time in <c>CleanupViewModel.MoveAllAsync</c>
    /// and in the CLI's <c>Program.cs</c>. The dual gate is the
    /// defence: removing either use-time gate without adding a
    /// write-time equivalent re-opens the system-folder-destination
    /// class of bugs because a hand-edited settings.json (or a
    /// settings.json carried in from a prior install with a since-
    /// invalidated destination) would silently bypass the missing
    /// gate.
    /// </remarks>
    public string MoveDestination { get; set; } = string.Empty;

    /// <summary>Last-saved size of the orphaned-files window. Null until the user resizes it.</summary>
    public WindowSize? OrphanedWindowSize { get; set; }

    /// <summary>Last-saved size of the registered-files window. Null until the user resizes it.</summary>
    public WindowSize? RegisteredWindowSize { get; set; }

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
}

/// <summary>Persisted Width/Height pair for a remembered window size.</summary>
public sealed class WindowSize
{
    public double Width { get; set; }
    public double Height { get; set; }
}
