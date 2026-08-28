using InstallerClean.Interop;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IDeclaredProductCheck"/>: reads each candidate
/// installation package's own ProductCode through
/// <see cref="IPackageIdentityReader"/> and puts it to Windows through the same
/// keyed enumeration the patch-target route uses.
///
/// IT COMPOSES TWO THINGS THAT ALREADY EXIST AND ADDS NO THIRD. The reading is
/// the reader's, which has always been able to take the product reading and has
/// only ever been asked for the patch one. The asking is
/// <see cref="InstallerQueryService.ResolveProductInstance"/>, shared rather than
/// copied because the part of it that decides anything is which returns are
/// allowed to mean "not installed": that allowlist is the difference between a
/// file kept and a file offered, and a second copy of it is a second thing to
/// keep right.
/// </summary>
public sealed class DeclaredProductCheck : IDeclaredProductCheck
{
    private readonly IMsiApi _msi;
    private readonly IPackageIdentityReader _identityReader;

    public DeclaredProductCheck(IMsiApi msi, IPackageIdentityReader identityReader)
    {
        _msi = msi;
        _identityReader = identityReader;
    }

    /// <inheritdoc />
    public IReadOnlyList<DeclaredProductOutcome> Screen(
        IReadOnlyList<OrphanedFile> candidates,
        CancellationToken cancellationToken = default,
        Action<Exception, string>? recordRefusal = null)
    {
        var outcomes = new DeclaredProductOutcome[candidates.Count];

        // Per pass, so it cannot outlive the machine state it describes. A folder
        // holding six cached packages of one program declares one product code
        // six times, and the answer to a keyed enumeration does not change inside
        // a scan.
        //
        // ORDINAL, because the reader canonicalises every code it returns to the
        // braced upper-case form for exactly this: two readings of one code are
        // then the same string, and a comparer that folded case would be covering
        // for a reader that had stopped doing that.
        var asked = new Dictionary<string, DeclaredProductOutcome>(StringComparer.Ordinal);

        for (var i = 0; i < candidates.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = candidates[i];

            // THE RESTRICTION, AND IT IS ENFORCED HERE RATHER THAN AT THE CALL
            // SITE ON PURPOSE. Asked of a patch this question keeps back every
            // registered superseded patch on every machine for ever: Windows
            // holds a record of such a patch's code by construction, that being
            // what superseded means, so the keeping arm would be true of the
            // whole class. A caller that passes the whole candidate list in gets
            // the patches back untouched instead of screening them by accident.
            if (candidate.IsPatch)
            {
                outcomes[i] = DeclaredProductOutcome.NotAProductPackage;
                continue;
            }

            var identity = _identityReader.Read(candidate.FullPath, isPatch: false, out var detail);

            // THREE REFUSALS UNDER ONE ARM, and each of them is the file failing
            // to give this pass something to ask about. Null is the reader's own
            // "nothing here to ask", documented as covering everything from a
            // file that would not open to a code that is not a GUID. An empty
            // code is the same outcome reached without a null, which the seam's
            // do-nothing implementations produce. A reading that came back
            // marked as a patch is a reader that did not answer the question
            // asked, and a code of the wrong kind put to a keyed product
            // enumeration would be answered about nothing.
            if (identity is null
                || identity.Value.Code.Length == 0
                || identity.Value.IsPatch)
            {
                // ONLY THE NULL ARM HAS A DETAIL TO KEEP. The other two are answers
                // the reader gave rather than failures it had, so it wrote nothing down
                // about them and a note saying the file did not yield a code would be
                // untrue of one that did. What the detail is for, and why no path goes
                // with it, is at the sibling site in InstallerQueryService.
                if (identity is null)
                    recordRefusal?.Invoke(
                        new InvalidOperationException(
                            "A cached package did not yield the product code it declares, so it is "
                            + "kept rather than offered. Reader detail: "
                            + (detail.Length == 0 ? "none given" : detail) + "."),
                        detail);

                outcomes[i] = DeclaredProductOutcome.Unestablished;
                continue;
            }

            var code = identity.Value.Code;
            if (asked.TryGetValue(code, out var already))
            {
                outcomes[i] = already;
                continue;
            }

            var resolved = InstallerQueryService.ResolveProductInstance(_msi, code);

            // THE ORDER OF THE ARMS IS THE WHOLE OF IT, and the unaskable one is
            // first because it is the one that reads as an answer if it is left
            // last. A call that could not be made has not shown the product to be
            // absent, and treating "no answer" as "no product" would offer the
            // file on the strength of a question that was never really put.
            outcomes[i] = resolved.Unaskable
                ? DeclaredProductOutcome.Unestablished
                : resolved.Installed
                    ? DeclaredProductOutcome.DeclaredProductInstalled
                    : DeclaredProductOutcome.DeclaredProductNotInstalled;

            asked[code] = outcomes[i];
        }

        return outcomes;
    }
}
