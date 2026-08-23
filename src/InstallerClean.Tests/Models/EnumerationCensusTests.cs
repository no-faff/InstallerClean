using System.Reflection;
using InstallerClean.Models;
using Xunit;

namespace InstallerClean.Tests.Models;

/// <summary>
/// <see cref="EnumerationCensus.AnyRecordedPathUnestablished"/> as a STRUCTURE rather
/// than as an answer: which members of the census make a scan withhold its whole
/// walk-derived offer, and which deliberately do not.
///
/// WHY THIS IS REFLECTIVE. The property is a hand-written expression over two derived
/// totals, and the census is a positional record of ints that this release has already
/// grown twice. A population added to the record and forgotten in that expression
/// compiles, builds green, reports its count to the opt-in report and simply does not
/// withhold: files offered that the app meant to keep back, with nothing anywhere to
/// see. Writing the member names out again would leave the next one exactly as
/// forgettable. Enumerating the constructor is what makes the test notice.
///
/// AND IT IS AN EQUALITY RATHER THAN A SET OF ASSERTIONS, which is the point. A test
/// that only checked the nine would pass a property that answered true for every
/// member on the record. The must-miss half is carried by the same comparison, so
/// there is no separate control to forget: <see cref="EnumerationCensus.ProductCount"/>
/// and its twenty-odd neighbours are in the run and are required NOT to fire.
///
/// WHAT A FAILURE HERE MEANS. Either a new population needs adding to the property in
/// the same edit, or a member has been wired into it that is not a recorded path this
/// scan could not settle. The message names which member moved, because the count
/// alone would say nothing about where to look.
/// </summary>
public class EnumerationCensusTests
{
    /// <summary>
    /// The census members a scan withholds on, written out so the comparison has
    /// something to be wrong against. Nine, in two populations that are never added
    /// together: four values that could not be turned into a path at all, and five
    /// the final-path resolver was asked about and did not resolve.
    ///
    /// <see cref="EnumerationCensus.PathResolverAttemptCount"/> IS DELIBERATELY ABSENT
    /// and is the trap this list exists to hold. It counts the times the resolver was
    /// ASKED, which on a healthy machine carrying an 8dot3 spelling is a positive
    /// number with five clean answers behind it. A property that read the attempts
    /// would empty the offer on exactly the machines the resolution was added to help.
    /// </summary>
    private static readonly string[] WithholdingMembers =
    [
        "PathResolverNotAPathCount",
        "PathResolverNoAncestorCount",
        "PathResolverOpenRefusedCount",
        "PathResolverNoFinalNameCount",
        "PathResolverFaultedCount",
        "PathNormalisationRefusedAtExpansionCount",
        "PathNormalisationRefusedAtPrefixStripCount",
        "PathNormalisationRefusedAtFullPathCount",
        "PathNormalisationRefusedAtEmbeddedNullCount",
    ];

    [Fact]
    public void Exactly_the_nine_unsettled_path_members_make_a_scan_withhold()
    {
        var ctor = Primary();
        var parameters = ctor.GetParameters();

        // The denominator, printed as an assertion rather than assumed. A reflection
        // walk that found no members would report every one of the nine as absent,
        // and a walk that found the wrong constructor would report nothing at all;
        // both read exactly like a property that fires for nothing.
        Assert.True(parameters.Length >= WithholdingMembers.Length + 15,
            $"The census constructor has {parameters.Length} parameters, which is too few for this "
            + "walk to be measuring what it claims. Either reflection found the wrong constructor or "
            + "the record has been cut down, and either way the comparison below is over a set that "
            + "cannot show the must-miss half.");
        Assert.All(parameters, p => Assert.Equal(typeof(int), p.ParameterType));

        var fired = new List<string>();
        foreach (var parameter in parameters)
        {
            // One member at 1 and every other at 0, so what fired is attributable to
            // that member alone rather than to any combination of them.
            var args = new object[parameters.Length];
            for (var i = 0; i < args.Length; i++) args[i] = 0;
            args[parameter.Position] = 1;

            var census = (EnumerationCensus)ctor.Invoke(args);
            if (census.AnyRecordedPathUnestablished) fired.Add(parameter.Name!);
        }

        var expected = WithholdingMembers.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var actual = fired.OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.True(expected.SequenceEqual(actual, StringComparer.Ordinal),
            "AnyRecordedPathUnestablished fires on a different set of members than the withholding "
            + "was written for.\n"
            + $"  expected: {string.Join(", ", expected)}\n"
            + $"  actual  : {string.Join(", ", actual)}\n"
            + $"  missing : {string.Join(", ", expected.Except(actual, StringComparer.Ordinal))}\n"
            + $"  extra   : {string.Join(", ", actual.Except(expected, StringComparer.Ordinal))}\n"
            + "A MISSING member is a cause the scan counts and does not act on, which is files "
            + "offered that it meant to keep back. An EXTRA member is the scan refusing on a fact "
            + "that is not a recorded path it could not settle. Neither is a test to relax.");
    }

    [Fact]
    public void A_census_with_nothing_wrong_withholds_nothing()
    {
        // The floor under the walk above. Every count at its default, which is the
        // shape the great majority of scans produce.
        Assert.False(new EnumerationCensus().AnyRecordedPathUnestablished);
    }

    [Fact]
    public void Asking_the_resolver_is_not_by_itself_a_reason_to_withhold()
    {
        // THE MEMBER MOST LIKELY TO BE FOLDED IN BY MISTAKE, so it is pinned by name
        // as well as by the walk. A machine carrying an 8dot3 spelling that resolved
        // cleanly reports attempts above zero and five zeros behind them, and it is
        // entitled to its offer.
        var asked = new EnumerationCensus(PathResolverAttemptCount: 4);

        Assert.False(asked.AnyRecordedPathUnestablished);
        Assert.Equal(0, asked.PathResolverRefusedTotal);
    }

    [Fact]
    public void Carrying_a_flagged_spelling_is_not_a_reason_to_withhold()
    {
        // THE OTHER MEMBER MOST LIKELY TO BE FOLDED IN BY MISTAKE, and it reads more
        // like a fault than the attempts count does, which is why it is pinned by
        // name as well as by the walk. A recorded value carrying an 8dot3 alias or a
        // volume-GUID prefix is the exact case the final-path resolution was built
        // for; a machine reporting several of them and no failures is one where the
        // mechanism did its job. Withholding there would empty the offer on the
        // machines the work was done for.
        var flagged = new EnumerationCensus(
            PathResolverAttemptCount: 6,
            PathFlaggedSpellingCount: 6);

        Assert.False(flagged.AnyRecordedPathUnestablished);
        Assert.Equal(0, flagged.PathResolverRefusedTotal);
    }

    [Fact]
    public void The_two_populations_are_counted_apart_and_are_never_added()
    {
        // One recorded value can be refused by both halves: the resolver declines to
        // settle its spelling, and the closing GetFullPath then refuses it too. The
        // totals are therefore counts of REFUSALS and not of paths, which is why the
        // rule asks a bool and no surface adds them.
        var both = new EnumerationCensus(
            PathResolverNotAPathCount: 1,
            PathNormalisationRefusedAtFullPathCount: 1);

        Assert.Equal(1, both.PathResolverRefusedTotal);
        Assert.Equal(1, both.PathNormalisationRefusedTotal);
        Assert.True(both.AnyRecordedPathUnestablished);
    }

    /// <summary>
    /// The census members the second-instance withholding fires on. Two, and they are
    /// opposite findings rather than two of a kind: one is a product that positively
    /// answered that it is a second instance of itself, the other a product that was
    /// asked and would not answer.
    ///
    /// <see cref="EnumerationCensus.RecoveredProductCount"/> IS DELIBERATELY ABSENT and
    /// is this list's trap, for the reason the attempts count is the other list's. A
    /// product the enumeration lost and the registry named is ASKED the question, one
    /// keyed read each, so a machine with recovered products and clean answers is a
    /// machine that answered. A property that read the recovered count would empty the
    /// offer on exactly the machines the recovery pass exists to rescue.
    /// </summary>
    private static readonly string[] SecondInstanceMembers =
    [
        "InstanceProductCount",
        "InstanceTypeUnreadableCount",
    ];

    [Fact]
    public void Exactly_the_two_second_instance_members_make_a_scan_withhold()
    {
        // THE SAME WALK AS ABOVE AND FOR THE SAME REASON. The rule is a hand-written
        // expression over a positional record of ints that has grown twice already, and
        // a member added to the record and forgotten in the expression compiles, builds
        // green, reports its count and simply does not withhold. Writing the names out
        // again in the property would leave the next one exactly as forgettable.
        //
        // AND THE MUST-MISS HALF IS CARRIED BY THE SAME COMPARISON, which is the point
        // of an equality rather than a set of assertions: every other member on the
        // record is in this run and is required NOT to fire.
        var ctor = Primary();
        var parameters = ctor.GetParameters();

        // The denominator, printed as an assertion rather than assumed: a walk that
        // found the wrong constructor would report nothing fired, which reads exactly
        // like a property that answers false for everything.
        Assert.True(parameters.Length >= SecondInstanceMembers.Length + 15,
            $"The census constructor has {parameters.Length} parameters, which is too few for this "
            + "walk to be measuring what it claims.");

        var fired = new List<string>();
        foreach (var parameter in parameters)
        {
            var args = new object[parameters.Length];
            for (var i = 0; i < args.Length; i++) args[i] = 0;
            args[parameter.Position] = 1;

            var census = (EnumerationCensus)ctor.Invoke(args);
            if (census.SecondInstanceNotRuledOut) fired.Add(parameter.Name!);
        }

        var expected = SecondInstanceMembers.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var actual = fired.OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.True(expected.SequenceEqual(actual, StringComparer.Ordinal),
            "SecondInstanceNotRuledOut fires on a different set of members than the withholding "
            + "was written for.\n"
            + $"  expected: {string.Join(", ", expected)}\n"
            + $"  actual  : {string.Join(", ", actual)}\n"
            + $"  missing : {string.Join(", ", expected.Except(actual, StringComparer.Ordinal))}\n"
            + $"  extra   : {string.Join(", ", actual.Except(expected, StringComparer.Ordinal))}\n"
            + "A MISSING member is a machine the scan reads as ordinary and cannot show to be, "
            + "which is files offered that it meant to keep back. An EXTRA member is the scan "
            + "emptying an offer on a fact that is not the second-instance question. Neither is a "
            + "test to relax.");
    }

    [Fact]
    public void A_census_with_nothing_wrong_rules_the_second_instance_question_out()
    {
        // The floor under the walk above, and the one that keeps this rule off every
        // ordinary machine: an absent InstanceType is documented as meaning an ordinary
        // installation and reaches the census as neither count.
        Assert.False(new EnumerationCensus().SecondInstanceNotRuledOut);
    }

    [Fact]
    public void Recovering_a_product_the_enumeration_lost_is_not_by_itself_a_reason_to_withhold()
    {
        // THE MEMBER MOST LIKELY TO BE FOLDED IN BY MISTAKE, pinned by name as well as
        // by the walk. It is tempting because a recovered product was not in the loop
        // that reads the property, and the answer is that it is asked separately rather
        // than assumed unanswerable. A machine whose registry named two products the
        // enumeration missed, both of which answered, is entitled to its offer.
        var recovered = new EnumerationCensus(RecoveredProductCount: 2);

        Assert.False(recovered.SecondInstanceNotRuledOut);
    }

    /// <summary>
    /// The record's own constructor, chosen by arity rather than by position in the
    /// reflection result, so a compiler-generated parameterless one cannot be picked
    /// up and walked instead.
    /// </summary>
    private static ConstructorInfo Primary() =>
        typeof(EnumerationCensus).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
}
