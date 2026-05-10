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
    public string MoveDestination { get; set; } = string.Empty;

    /// <summary>Last-saved size of the orphaned-files window. Null until the user resizes it.</summary>
    public WindowSize? OrphanedWindowSize { get; set; }

    /// <summary>Last-saved size of the registered-files window. Null until the user resizes it.</summary>
    public WindowSize? RegisteredWindowSize { get; set; }

    /// <summary>
    /// Set to true once the user has successfully sent a result log
    /// to No Faff. The Send button is then hidden forever (across
    /// sessions and across version upgrades) so we capture one report
    /// per machine, never two. Eliminates the bias where a user who
    /// freed nothing on a re-run would skew the aggregate downward.
    /// </summary>
    public bool HasSentResultLog { get; set; }
}

/// <summary>Persisted Width/Height pair for a remembered window size.</summary>
public sealed class WindowSize
{
    public double Width { get; set; }
    public double Height { get; set; }
}
