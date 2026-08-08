namespace InstallerClean.Services;

/// <summary>
/// Reads the machine's 8dot3 short-name creation policy and answers with one of
/// <see cref="Models.ShortNameCreationLabels"/>.
///
/// It exists for the opt-in report and decides nothing. Every path this app acts
/// on comes out of the Windows Installer records and is resolved before it is
/// touched, so whether the volume also carries a short name for that file changes
/// no verdict; what nobody knows is how the machines running this are configured,
/// the one it was all measured on having creation off.
/// </summary>
public interface IShortNameCreationProbe
{
    /// <summary>
    /// The current policy as a stable label. Never throws and never returns null:
    /// a read it could not make answers
    /// <see cref="Models.ShortNameCreationLabels.Unreadable"/> rather than
    /// guessing at a default, because Microsoft's own reference documents the four
    /// settings and does not say which one a machine that has never been
    /// configured is at.
    /// </summary>
    string Read();
}
