namespace InstallerClean.Helpers;

/// <summary>
/// The page both of the app's donate controls open: the button on the card
/// shown at the end of a run, and the pill in the About window.
///
/// One constant, read by both, because the address moves. Written out twice,
/// the second copy goes stale on the day the first one changes, and a donate
/// button that opens a page which is no longer there raises no error, fails
/// no check and produces no complaint, because the person who meets it is not
/// the person who would report it. Anything else that needs this address,
/// including the check that holds the release notes to it, reads it from here.
/// </summary>
public static class SupportLink
{
    /// <summary>The address itself.</summary>
    public const string Url = "https://nofaff.netlify.app/support";
}
