using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Asks each removal candidate what it is, then asks Windows about that, and
/// keeps back anything it cannot account for.
///
/// WHY IT EXISTS, AND IT IS NOT THAT ANYTHING IS KNOWN TO BE BROKEN. The scan
/// decides a cached file is unclaimed by comparing two spellings of a path: the
/// walk's, and the one the registration was written with. Every divergent
/// spelling anybody has identified is resolved before that comparison, in
/// <c>InstallerQueryService.NormaliseLocalPackagePath</c>, so the known classes
/// are closed where they arise and this is not the thing that closes them.
///
/// What a string comparison cannot do is report a spelling it did not recognise.
/// It has one negative answer and two situations behind it: a registration
/// written in a form the walk never produces, and a file that genuinely belongs
/// to nothing. Closing spellings one at a time leaves that intact, because each
/// fix removes a case and none of them removes the silence.
///
/// Turning the question round takes the spelling out of it. Every cached package
/// carries its own identity inside it, put there by whoever authored the package
/// and unchanged by how anything later wrote a path. Reading that and asking
/// Windows about it means a file the app cannot account for produces a
/// withholding rather than an offer. That is the whole of what it buys, and it is
/// worth having for the case nobody has come across rather than for any case
/// anybody has.
///
/// IT ONLY EVER WITHHOLDS, and every part of the design rests on that. It cannot
/// put a file on the list, cannot clear one the path comparison kept, and cannot
/// weaken any existing gate. A source it fails to reach, a file it cannot read, a
/// question it cannot put: all of them keep the file. So composing more sources
/// into it is always safe, and the worst a fault in it can do is offer fewer
/// files than the app could have offered.
///
/// WHAT IT DOES NOT DO. It does not decide what is CONSIDERED. The path
/// comparison still does that, and is allowed to be wrong in both directions: a
/// needed file wrongly in the candidate set is caught here, and a file wrongly
/// left out is never offered, which is a withholding and is safe. It is not a
/// replacement for the path-spelling work either, which stays as it is and is
/// now the belt to this brace.
/// </summary>
public interface IIdentityVeto
{
    /// <summary>
    /// Screens one scan's provisional candidates, in order, returning a verdict
    /// for each.
    ///
    /// The whole pass is one call so that everything per-scan lives inside it:
    /// the account ladder is read once, each distinct identity is asked about
    /// once however many files declare it, and none of that state outlives the
    /// scan that built it. It also gives the phase a single thing to time, which
    /// matters because on a large folder this is the longest part of a scan.
    /// </summary>
    /// <param name="progress">
    /// Reported through, and it is not decoration. This pass opens every
    /// candidate file, so on a folder of any size it is minutes during which the
    /// rest of the scan reports nothing at all; a static line while the app
    /// works is a complaint the project already has on record. The updates are
    /// tickers rather than milestones, so a screen reader is not made to
    /// announce one per file.
    /// </param>
    IdentityPassResult Screen(
        IReadOnlyList<IdentityCandidate> candidates,
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>One provisional candidate to screen.</summary>
/// <param name="FullPath">
/// The candidate's path, which the caller has already put through
/// <see cref="CandidateGuard.CheckSafeToRemove"/>. See
/// <see cref="IPackageIdentityReader.Read"/> for why that is a precondition and
/// not a courtesy.
/// </param>
/// <param name="IsPatch">
/// Decided by the caller from the extension. It selects which reading is taken,
/// and the two have nothing in common.
/// </param>
public readonly record struct IdentityCandidate(string FullPath, bool IsPatch);

/// <summary>
/// What one candidate's identity settled. Four states, and three of them keep the
/// file.
///
/// THE THREE KEEPING STATES ARE NOT ONE STATE AND MAY NEVER BE REPORTED AS ONE.
/// A positive claim, an inability to read the file and an inability to put the
/// question are three different things to have found out, and any sentence
/// covering all three either says nothing or says something false of two of them.
/// The app's rule on this is absolute: a message must not state a cause for a set
/// that can have mixed causes.
/// </summary>
public enum CandidateIdentityOutcome
{
    /// <summary>
    /// The file said what it was, every source was asked, and none of them knows
    /// it. The candidate may be offered, subject to everything else that already
    /// governs that.
    ///
    /// This is the ONLY value that permits, and it is deliberately the one that
    /// requires the most to have gone right.
    /// </summary>
    Unclaimed,

    /// <summary>
    /// Windows answers to this file's identity: some product or patch it still
    /// holds records for. Kept back.
    ///
    /// It is a positive claim on the file and nothing weaker. What it does NOT
    /// establish is that a program would break without this particular copy: a
    /// product that has cached a fresh package on each of twenty updates leaves
    /// nineteen files that answer to a live product code and are dead. Keeping
    /// them is the mechanism working, and any copy built on this count has to say
    /// what the count really means.
    /// </summary>
    Claimed,

    /// <summary>
    /// The file did not yield an identity to ask about. Kept back.
    ///
    /// An inability about the FILE. It covers everything from a package that
    /// would not open to a patch that names no product it targets; what they have
    /// in common, and the whole of what may be said, is that there was nothing
    /// here to ask.
    /// </summary>
    IdentityUnreadable,

    /// <summary>
    /// The identity was read and the question could not be put. Kept back.
    ///
    /// An inability about the RECORDS, which is a different thing to have found
    /// out from the one above and from a claim. It is reached when the account
    /// ladder could not be read, or when a source answered with something outside
    /// the returns that mean an answer.
    /// </summary>
    RecordsUnaskable,
}

/// <summary>
/// What one identity pass found. <see cref="Outcomes"/> is positional: entry
/// <c>i</c> is the verdict on candidate <c>i</c> of the list passed in.
/// </summary>
/// <param name="Outcomes">One verdict per candidate, in the order they were given.</param>
/// <param name="DistinctIdentitiesAsked">
/// How many distinct identities Windows was actually asked about, which on a
/// folder holding many versions of one product is far fewer than the candidate
/// count. Diagnostic: it is what makes a slow pass explicable.
/// </param>
/// <param name="IdentityCacheHits">
/// Candidates whose identity had already been asked about in this pass. With
/// <see cref="DistinctIdentitiesAsked"/> it gives the hit rate, and the two
/// together account for every candidate that got as far as being asked about.
/// </param>
public sealed record IdentityPassResult(
    IReadOnlyList<CandidateIdentityOutcome> Outcomes,
    int DistinctIdentitiesAsked,
    int IdentityCacheHits)
{
    /// <summary>Candidates a live registration answers for.</summary>
    public int ClaimedCount => Count(CandidateIdentityOutcome.Claimed);

    /// <summary>Candidates whose own identity could not be read.</summary>
    public int IdentityUnreadableCount => Count(CandidateIdentityOutcome.IdentityUnreadable);

    /// <summary>Candidates whose identity was read and could not be put to Windows.</summary>
    public int RecordsUnaskableCount => Count(CandidateIdentityOutcome.RecordsUnaskable);

    // THERE IS DELIBERATELY NO TOTAL OVER THE THREE ABOVE, and one stood here
    // until it was noticed that nothing outside a test had ever read it. A
    // confirmed claim, a file that yielded no identity and a question Windows
    // would not answer are three different things to have found out; the record
    // that receives them says in as many words that they are summed nowhere,
    // because the surface that has a total and no partition in front of it is the
    // surface that reaches for one sentence over all three, and any such sentence
    // is either empty or false of two of them.
    //
    // A caller wanting "how many did the pass keep back" wants three numbers and
    // a line each. If a total is ever genuinely needed it can be added back
    // BESIDE a partition that is already being shown, never instead of one.

    /// <summary>
    /// Derived rather than carried, so a count and the verdicts it describes
    /// cannot drift apart. There is one producer of both and it is this list.
    /// </summary>
    private int Count(CandidateIdentityOutcome outcome)
    {
        var n = 0;
        for (var i = 0; i < Outcomes.Count; i++)
            if (Outcomes[i] == outcome) n++;
        return n;
    }
}
