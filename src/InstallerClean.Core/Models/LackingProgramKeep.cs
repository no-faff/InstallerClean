namespace InstallerClean.Models;

/// <summary>
/// What keeps a superseded patch's cached file for a program the patch is for that does
/// not hold it, by the answer Windows Installer gave when asked whether the patch would
/// apply to that program (<c>MsiDeterminePatchSequence</c>, put with the cached file in
/// the program's own account and context).
///
/// The scan asks it of a program the file's own Template names, whose records are under
/// the same <c>UserData</c> key as a registration of the file. The installation that
/// applies the patch to such a program opens the registered cached file, and with the
/// file gone it can fail, or finish with custom actions that never ran.
/// </summary>
public enum LackingProgramKeep
{
    /// <summary>
    /// No such program keeps the file: there is none, or Windows Installer answered for
    /// each that the patch cannot be applied to it.
    /// </summary>
    None,

    /// <summary>Windows Installer answered that it would apply the patch to the program.</summary>
    WouldApply,

    /// <summary>
    /// Windows Installer answered that it would not apply the patch to the program, with
    /// a status of success for the patch: the answer it gives for a patch that something
    /// the program holds has superseded or made obsolete.
    /// </summary>
    SupersededThere,

    /// <summary>
    /// The question was not answered: the call failed, it gave the patch a status that is
    /// neither success nor that the patch cannot be applied, or gave it none; the program
    /// did not read as installed; or the program's records or a registration of the file
    /// could not be placed under a <c>UserData</c> key.
    /// </summary>
    AnswerNotHad,
}

/// <summary>Which of two <see cref="LackingProgramKeep"/> answers one file is recorded under.</summary>
public static class LackingProgramKeeps
{
    /// <summary>
    /// The answer a file kept by two programs is recorded under: that the patch would
    /// apply, before an answer that was not had, before the patch being superseded
    /// there. A file is counted once, and in this order the count of files kept as
    /// superseded there holds only files no other answer keeps.
    /// </summary>
    public static LackingProgramKeep Stronger(LackingProgramKeep a, LackingProgramKeep b) =>
        Rank(a) >= Rank(b) ? a : b;

    private static int Rank(LackingProgramKeep keep) => keep switch
    {
        LackingProgramKeep.WouldApply => 3,
        LackingProgramKeep.AnswerNotHad => 2,
        LackingProgramKeep.SupersededThere => 1,
        _ => 0,
    };
}
