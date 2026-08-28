using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Asks a cached PRODUCT PACKAGE which product it declares itself to belong to,
/// puts that product code to Windows, and reports whether Windows still holds a
/// record of it.
///
/// WHY IT EXISTS, AND IT IS ABOUT WHERE THE OTHER SOURCES START. Everything else
/// that decides whether a cached product package is spare begins at a
/// REGISTRATION and works towards a file. The path comparison asks whether any
/// recorded <c>LocalPackage</c> value is spelled the same as a walked file; the
/// file-identity match asks whether any recorded value NAMES the same file; and
/// the registry fallback contributes recorded values the API enumeration lost.
/// Those are three ways of finding a claim, and all three read the same recorded
/// value. Where a product's records hold no value to read, none of them has
/// anything to find, and the enumeration does not notice: a <c>LocalPackage</c>
/// that is present and zero-length merges no claim AND records no gap, so the
/// scan reports itself complete while short of a claim. The product's cached
/// package is then walked, matched against nothing, and offered while the product
/// is installed.
///
/// A PATCH ALREADY HAS THREE SOURCES AND TAKES THE WORST ANSWER. Its loop has the
/// identical silent-empty hole, and the registry patch-set read and the
/// all-products patch enumeration are what see what the loop misses. A product
/// has two, and they read the same underlying value. The genuinely independent
/// third view of a product's cached file is the FILE, which is this.
///
/// IT ONLY EVER WITHHOLDS, and the whole design rests on that. No answer it can
/// give puts a file on the list, clears one another gate kept, or weakens
/// anything upstream. A file it cannot read, a question it cannot put and a
/// source that answers off the allowlist all keep the file, so the worst a fault
/// in here can do is offer fewer files than the app could have offered.
///
/// PRODUCT PACKAGES ONLY, AND THAT RESTRICTION IS LOAD-BEARING RATHER THAN
/// INCIDENTAL. The same question asked of a patch keeps back every registered
/// superseded patch on every machine, for ever, with a green build: a superseded
/// patch is one Windows has a record of BY CONSTRUCTION, that being what makes it
/// superseded rather than unknown, so "Windows knows this code" is true of the
/// entire class the offer's other half is made of. The restriction is enforced
/// inside <see cref="Screen"/> rather than left to callers, so passing a patch in
/// cannot screen it.
/// </summary>
public interface IDeclaredProductCheck
{
    /// <summary>
    /// Screens one scan's provisional candidates, in order, returning a verdict
    /// for each: entry <c>i</c> answers candidate <c>i</c> of the list passed in.
    ///
    /// The whole pass is one call so that everything per-scan lives inside it.
    /// Several cached packages of one product declare one product code, so a
    /// folder holding six versions of the same program asks Windows once and not
    /// six times, and that cache dies with the pass rather than outliving the
    /// machine state it describes.
    /// </summary>
    /// <param name="candidates">
    /// The files the path comparison and the file-identity match between them
    /// left unclaimed. The caller has already put every one through
    /// <see cref="CandidateGuard.CheckSafeToRemove"/>; see
    /// <see cref="IPackageIdentityReader.Read"/> for why that is a precondition
    /// and not a courtesy.
    /// </param>
    /// <param name="recordRefusal">
    /// Where a reader refusal goes, given the exception to log and the reader's own
    /// short note on which refusal it was. Handed in by the scan that owns the crash
    /// log for the run rather than made here, so a test calling this pass directly
    /// has no run and writes nothing. A delegate rather than the log itself because
    /// this interface is public and that type is not.
    /// </param>
    IReadOnlyList<DeclaredProductOutcome> Screen(
        IReadOnlyList<OrphanedFile> candidates,
        CancellationToken cancellationToken = default,
        Action<Exception, string>? recordRefusal = null);
}

/// <summary>
/// What one candidate's own declaration settled. Two of the four keep the file,
/// and <see cref="Withholds"/> is the only place that says which.
/// </summary>
public enum DeclaredProductOutcome
{
    /// <summary>
    /// The file yielded no product code to ask about, or the code was read and
    /// the question could not be put. Kept back.
    ///
    /// IT IS FIRST SO THAT THE DEFAULT VALUE WITHHOLDS. A verdict nobody set is a
    /// verdict nobody established, and this enum's zero has to mean that rather
    /// than mean an answer.
    ///
    /// TWO INABILITIES UNDER ONE NAME, DELIBERATELY, AND THE NAME STATES NEITHER
    /// CAUSE. One is about the FILE: it would not open, it holds no Property
    /// table, its ProductCode row is absent or is not a GUID. The other is about
    /// the RECORDS: the keyed enumeration answered with something outside the
    /// returns that mean an answer. They are different things to have found out,
    /// which is exactly why they are not reported anywhere as one thing; what
    /// they share, and the whole of what this value claims, is that nothing was
    /// established. Nothing outside this pass reads which of the two it was.
    /// </summary>
    Unestablished,

    /// <summary>
    /// Not an installation package, so this check has nothing to say about it and
    /// says nothing. The candidate goes on being decided by everything else,
    /// exactly as it would have been had this pass never run.
    ///
    /// See the type's own note for why a patch may never be screened here.
    /// </summary>
    NotAProductPackage,

    /// <summary>
    /// The file declared a product code and Windows positively answered that no
    /// such product is installed, in any account and any context. The candidate
    /// goes on being decided by everything else.
    ///
    /// A POSITIVE ANSWER AND NOT AN ABSENCE OF ONE, which is the distinction the
    /// whole check turns on. Only a return documented to mean the product is not
    /// there reaches this; anything else is <see cref="Unestablished"/>.
    /// </summary>
    DeclaredProductNotInstalled,

    /// <summary>
    /// Windows still holds a record of the product this file declares it belongs
    /// to. Kept back.
    ///
    /// WHAT IT DOES NOT ESTABLISH, so no copy may be built on it: that a program
    /// would break without this particular copy. A product that cached a fresh
    /// package on each of twenty updates leaves nineteen files that answer to a
    /// live product code and are dead weight. Keeping them is this app working;
    /// the alternative is offering a file it cannot say is spare.
    /// </summary>
    DeclaredProductInstalled,
}

/// <summary>Reading a <see cref="DeclaredProductOutcome"/>.</summary>
public static class DeclaredProductOutcomes
{
    /// <summary>
    /// Whether this outcome keeps the file back.
    ///
    /// STATED AS "ANYTHING BUT THESE TWO" RATHER THAN BY NAMING THE WITHHOLDING
    /// MEMBERS, and that is the safety property rather than a style. Named
    /// positively, a member added later would silently not withhold: a green
    /// build, a verdict the pass sets, and files going on being offered. Named
    /// this way an unconsidered member keeps the file, which is the direction a
    /// mistake here has to fail in.
    /// </summary>
    public static bool Withholds(this DeclaredProductOutcome outcome) =>
        outcome is not (DeclaredProductOutcome.NotAProductPackage
            or DeclaredProductOutcome.DeclaredProductNotInstalled);
}
