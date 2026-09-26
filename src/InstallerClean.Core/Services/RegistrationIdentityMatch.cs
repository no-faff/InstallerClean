using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Matches walked files to registrations by the file each one names rather than by
/// how its path is spelled. The scan calls it on the files the path comparison left
/// unclaimed, and the check made just before a Move or Delete calls it again on the
/// same half of the batch, so both hold one rule.
///
/// The registration side is read first and the candidate side second, so a machine
/// whose registrations all resolve to files the walk already matched costs one handle
/// per registration and nothing more. Nothing is opened at all where there are no
/// candidates or no registrations, and no candidate is opened where no registration
/// yielded an identity.
///
/// A FAILED READ KEEPS FILES BACK, AND THE TWO SIDES KEEP DIFFERENT AMOUNTS. A
/// registration nobody could identify might name ANY candidate, and which one cannot
/// be established, so its tally arms the wholesale withholding (<see
/// cref="WithholdingLegs"/>) and the caller drops every walk-derived file. A candidate
/// nobody could identify is one file: every other candidate was compared against the
/// registrations by a read that answered, so that one alone is kept back and the rest
/// stand.
///
/// A FILE THAT IS NOT THERE IS NOT A FAILURE OF EITHER KIND. See
/// <see cref="FileIdentityRead.NamesNothing"/>: a registration whose cached file has
/// gone claims none of the walked files, and reading that as a give-up would empty the
/// offer on most machines that have ever uninstalled anything.
///
/// EVERY REGISTRATION IS READ, INCLUDING THE ONES THE TEXT COMPARISON ALREADY MATCHED,
/// so a single registration this cannot identify costs the whole walk-derived offer on
/// an otherwise ordinary machine. Do not narrow it to the registrations whose path
/// failed to match the walk by text. Take registration R whose recorded path matches
/// walked file W by text, so W is off the candidate list and R looks harmless. If R's
/// path is a reparse point resolving to candidate C, a successful read of R returns
/// C's identity and claims C. A failed read of R, skipped as already matched, would
/// leave C on the offer while it is the data behind a registered package.
///
/// WHAT REFUSES IS NOT A FILE SOMETHING ELSE HAS OPEN. The read asks for no access
/// bits, so there is nothing for another opener's share mode to exclude. Once absence
/// is carved out, what is left is an ACL refusing an already-elevated process, a call
/// that threw, and a volume or Windows build that will not answer <c>FileIdInfo</c>.
/// That last is a property of the volume rather than of a file, so a machine meeting it
/// is offered nothing from the folder walk on every scan until something about the
/// volume changes.
///
/// A HARD LINK TO A REGISTERED PACKAGE IS CLAIMED. Two names for one file share an
/// identity, so a candidate hard-linked to a registered package is treated as that
/// package and never offered, although removing one link would leave the data reachable
/// through the other.
/// </summary>
internal static class RegistrationIdentityMatch
{
    /// <summary>
    /// Reads every registration's recorded path and then each of
    /// <paramref name="candidatePaths"/>, and answers for each candidate, in order.
    /// </summary>
    internal static RegistrationIdentityComparison Compare(
        IFileIdentityReader fileIdentities,
        IReadOnlyList<RegisteredPackage> registered,
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken)
    {
        var registrations = new IdentityReadTally();
        var candidateReads = new IdentityReadTally();
        var answers = new CandidateIdentity[candidatePaths.Count];

        // Nothing was asked, so nothing was given up: the tallies leave here at zero
        // attempts, which tells a report the comparison was skipped rather than that
        // it answered cleanly, and every candidate stands.
        if (candidatePaths.Count == 0 || registered.Count == 0)
            return new(registrations.Taken(), candidateReads.Taken(), Unclaimed(answers));

        // Several rows can name one file: a path and a hard link to it, or two
        // spellings of one path the resolver settled differently.
        var registeredIds = new Dictionary<FileIdentity, List<RegisteredPackage>>();
        foreach (var pkg in registered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (registrations.Record(fileIdentities.ReadOutcome(pkg.LocalPackagePath, out var id))
                != FileIdentityRead.Read)
                continue;

            if (!registeredIds.TryGetValue(id, out var rows))
                registeredIds[id] = rows = new List<RegisteredPackage>(1);
            rows.Add(pkg);
        }

        // No identity to compare against, so the candidate side is not asked and its
        // tally says so. A give-up on the registration side has already been counted
        // and arms the wholesale withholding whichever way this returns.
        if (registeredIds.Count == 0)
            return new(registrations.Taken(), candidateReads.Taken(), Unclaimed(answers));

        for (var i = 0; i < candidatePaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = candidateReads.Record(fileIdentities.ReadOutcome(candidatePaths[i], out var id));

            answers[i] = outcome == FileIdentityRead.Read
                ? registeredIds.TryGetValue(id, out var rows)
                    ? new CandidateIdentity(CandidateIdentityVerdict.Claimed, rows)
                    : new CandidateIdentity(CandidateIdentityVerdict.Unclaimed, null)
                : outcome.GivesUpAWithholding()
                    ? new CandidateIdentity(CandidateIdentityVerdict.Unestablished, null)
                    : new CandidateIdentity(CandidateIdentityVerdict.Unclaimed, null);
        }

        return new(registrations.Taken(), candidateReads.Taken(), answers);
    }

    private static CandidateIdentity[] Unclaimed(CandidateIdentity[] answers)
    {
        Array.Fill(answers, new CandidateIdentity(CandidateIdentityVerdict.Unclaimed, null));
        return answers;
    }

    /// <summary>
    /// One side's running count of what the identity reader answered, folded into
    /// the immutable <see cref="FileIdentityReadTally"/> the result carries.
    ///
    /// <see cref="Record"/> hands the outcome straight back so that counting it and
    /// acting on it are one expression at both call sites, and a later edit cannot
    /// separate them.
    /// </summary>
    internal sealed class IdentityReadTally
    {
        private int _attempts;
        private int _namesNothing;
        private int _notAPath;
        private int _openRefused;
        private int _identityUnavailable;
        private int _faulted;

        internal FileIdentityRead Record(FileIdentityRead outcome)
        {
            _attempts++;
            switch (outcome)
            {
                case FileIdentityRead.NamesNothing: _namesNothing++; break;
                case FileIdentityRead.NotAPath: _notAPath++; break;
                case FileIdentityRead.OpenRefused: _openRefused++; break;
                case FileIdentityRead.IdentityUnavailable: _identityUnavailable++; break;
                case FileIdentityRead.Faulted: _faulted++; break;
                    // Read is not counted: it is the attempts less the five, and a
                    // stored copy could disagree with them.
            }

            return outcome;
        }

        internal FileIdentityReadTally Taken() => new(
            _attempts, _namesNothing, _notAPath, _openRefused, _identityUnavailable, _faulted);
    }
}

/// <summary>What one <see cref="RegistrationIdentityMatch.Compare"/> read.</summary>
/// <param name="Registrations">
/// The registration side's reads. Its <see cref="FileIdentityReadTally.AnyUnestablished"/>
/// is the third of the <see cref="WithholdingLegs"/>.
/// </param>
/// <param name="Candidates">The candidate side's reads.</param>
/// <param name="Answers">One per candidate, in the order the candidates were given.</param>
internal readonly record struct RegistrationIdentityComparison(
    FileIdentityReadTally Registrations,
    FileIdentityReadTally Candidates,
    IReadOnlyList<CandidateIdentity> Answers);

/// <summary>What the comparison found for one candidate.</summary>
/// <param name="Verdict">Which of the three answers it was.</param>
/// <param name="ClaimedBy">
/// Every registration whose recorded path opens as this candidate, where
/// <paramref name="Verdict"/> is <see cref="CandidateIdentityVerdict.Claimed"/>;
/// null otherwise.
/// </param>
internal readonly record struct CandidateIdentity(
    CandidateIdentityVerdict Verdict,
    IReadOnlyList<RegisteredPackage>? ClaimedBy);

/// <summary>What the comparison found for one candidate.</summary>
internal enum CandidateIdentityVerdict
{
    /// <summary>
    /// The candidate's own identity would not read while the file is there, so no
    /// registration can be shown not to name it. The caller keeps it back.
    ///
    /// THE ZERO, so an answer nobody set keeps the file.
    /// </summary>
    Unestablished,

    /// <summary>
    /// No registration that yielded an identity names this file, or the comparison
    /// was not made, or the file went before it could be read. The candidate goes on
    /// being decided by everything else.
    /// </summary>
    Unclaimed,

    /// <summary>A registration's recorded path opens as this file.</summary>
    Claimed,
}
