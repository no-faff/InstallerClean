namespace InstallerClean.Services;

/// <summary>
/// Which of the two call sites asked for a check. It decides one thing:
/// how much of a failure reaches crash.log. Nothing else in the check
/// branches on it, and neither does the result.
/// </summary>
public enum UpdateCheckOrigin
{
    /// <summary>
    /// The once-per-session check the app starts at launch behind
    /// <c>AppSettings.AutoUpdateCheck</c>. Nobody asked for it, nothing is
    /// on screen, and every outcome but "newer version found" is invisible.
    /// </summary>
    Automatic,

    /// <summary>
    /// The main window's update button. The user is waiting on an answer
    /// and gets one either way, so a log entry has a dialog behind it.
    /// </summary>
    Manual,
}
