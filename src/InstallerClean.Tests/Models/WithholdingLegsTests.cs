using InstallerClean.Models;

namespace InstallerClean.Tests.Models;

/// <summary>
/// The expression that decides whether the walk-derived offer is withheld wholesale,
/// pinned over every combination of its three conditions.
///
/// THE TABLE IS WRITTEN OUT BY HAND AND NOT COMPUTED. Eight rows, each carrying the
/// legs it expects and the verdict it expects, both as literals. A test that worked
/// out its expectation from the thing under test would agree with itself over any
/// behaviour at all, and the assertions would look exactly the same. This is the one
/// place a reader can check what the gate does without reading the gate.
///
/// WHAT IT IS FOR IS THE REFACTOR AND NOT THE ARITHMETIC. The three conditions used to
/// be written out at the gate and would have been written out again at the host that
/// explains them; they are one call now, and this table says the behaviour did not move
/// when they became one. It is also what a fourth condition has to be added to, which
/// is the point at which somebody has to decide what the host says about it.
/// </summary>
public class WithholdingLegsTests
{
    /// <summary>A census whose recorded-path question answers the way the row wants.</summary>
    private static EnumerationCensus Census(bool recordedPath, bool secondInstance) =>
        new(PathResolverFaultedCount: recordedPath ? 1 : 0,
            InstanceProductCount: secondInstance ? 1 : 0);

    /// <summary>The registration side of the identity comparison, likewise.</summary>
    private static FileIdentityReadTally Reads(bool unestablished) =>
        new(AttemptCount: 1, OpenRefusedCount: unestablished ? 1 : 0);

    public static TheoryData<bool, bool, bool, WithholdingLeg[], bool> Table() => new()
    {
        // recordedPath, identity, secondInstance,  legs expected,  gate expected
        { false, false, false, [], false },
        { true,  false, false, [WithholdingLeg.RecordedPathUnestablished], true },
        { false, true,  false, [WithholdingLeg.FileIdentityUnestablished], true },
        { false, false, true,  [WithholdingLeg.SecondInstanceNotRuledOut], true },
        { true,  true,  false, [WithholdingLeg.RecordedPathUnestablished,
                                WithholdingLeg.FileIdentityUnestablished], true },
        { true,  false, true,  [WithholdingLeg.RecordedPathUnestablished,
                                WithholdingLeg.SecondInstanceNotRuledOut], true },
        { false, true,  true,  [WithholdingLeg.FileIdentityUnestablished,
                                WithholdingLeg.SecondInstanceNotRuledOut], true },
        { true,  true,  true,  [WithholdingLeg.RecordedPathUnestablished,
                                WithholdingLeg.FileIdentityUnestablished,
                                WithholdingLeg.SecondInstanceNotRuledOut], true },
    };

    [Theory]
    [MemberData(nameof(Table))]
    public void Every_combination_fires_the_legs_the_table_says(
        bool recordedPath, bool identity, bool secondInstance,
        WithholdingLeg[] expected, bool expectedGate)
    {
        var census = Census(recordedPath, secondInstance);
        var reads = Reads(identity);

        // Order as well as membership: the host prints them in this order, so a change
        // to it changes what a reader meets and is not an implementation detail.
        Assert.Equal(expected, WithholdingLegs.Fired(census, reads));
        Assert.Equal(expectedGate, WithholdingLegs.Any(census, reads));
    }

    [Theory]
    [MemberData(nameof(Table))]
    public void The_result_reads_the_same_legs_the_gate_was_given(
        bool recordedPath, bool identity, bool secondInstance,
        WithholdingLeg[] expected, bool expectedGate)
    {
        // The host reads them off the result rather than off the two values, so the
        // property is held to the same table. It calls the same static, which is what
        // makes a disagreement impossible rather than merely unlikely; this is what
        // says the wiring between them is right.
        var result = new ScanResult(
            Array.Empty<OrphanedFile>(),
            Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            Census: Census(recordedPath, secondInstance),
            RegistrationIdentityReads: Reads(identity));

        Assert.Equal(expected, result.WithholdingLegsFired);
        Assert.Equal(expectedGate, result.WithholdingLegsFired.Count > 0);
    }

    /// <summary>
    /// Every member of the census and of the identity tally, one at a time, asserting
    /// that the gate is exactly "some leg fired" for each.
    ///
    /// THE EIGHT ROWS ABOVE CANNOT CATCH A CONDITION ADDED TO THE GATE THAT IS NOT A
    /// LEG. They set the members the three legs read and leave the rest at zero, so a
    /// gate that grew a fourth condition on any other member would withhold on machines
    /// the breakdown has nothing to say about, and every row would still pass. This is
    /// the test that closes that, and it needs no maintenance: the members are read off
    /// the types, so one added later is covered the day it lands.
    ///
    /// IT IS AN EQUIVALENCE AND NOT A TABLE, which is safe here for the reason it is
    /// unsafe above: what it compares are two DIFFERENT expressions, the gate and the
    /// list, where the table compares one expression against a literal. A test deriving
    /// its expectation from its subject is the trap; this derives one subject's
    /// expectation from another subject.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryCensusMember))]
    public void The_gate_is_exactly_some_leg_fired_for_every_member_on_its_own(string member)
    {
        var census = CensusWithOnly(member);
        var reads = default(FileIdentityReadTally);

        Assert.Equal(WithholdingLegs.Fired(census, reads).Count > 0,
            WithholdingLegs.Any(census, reads));
    }

    [Theory]
    [MemberData(nameof(EveryTallyMember))]
    public void The_gate_is_exactly_some_leg_fired_for_every_read_outcome_on_its_own(string member)
    {
        var census = default(EnumerationCensus);
        var reads = TallyWithOnly(member);

        Assert.Equal(WithholdingLegs.Fired(census, reads).Count > 0,
            WithholdingLegs.Any(census, reads));
    }

    public static TheoryData<string> EveryCensusMember() => Members<EnumerationCensus>();

    public static TheoryData<string> EveryTallyMember() => Members<FileIdentityReadTally>();

    /// <summary>
    /// The names of a record struct's positional members, read off its primary
    /// constructor so the set cannot go stale.
    /// </summary>
    private static TheoryData<string> Members<T>()
    {
        var data = new TheoryData<string>();
        foreach (var p in Primary<T>().GetParameters()) data.Add(p.Name!);
        return data;
    }

    private static EnumerationCensus CensusWithOnly(string member) => With<EnumerationCensus>(member);

    private static FileIdentityReadTally TallyWithOnly(string member) => With<FileIdentityReadTally>(member);

    /// <summary>One member set to one and every other left at its default.</summary>
    private static T With<T>(string member)
    {
        var ctor = Primary<T>();
        var args = ctor.GetParameters()
            .Select(p => p.Name == member ? (object)1 : p.DefaultValue!)
            .ToArray();
        return (T)ctor.Invoke(args);
    }

    /// <summary>
    /// The primary constructor: the widest one, which for these record structs is the
    /// positional one and not the parameterless one the runtime also provides.
    /// </summary>
    private static System.Reflection.ConstructorInfo Primary<T>() =>
        typeof(T).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();

    [Fact]
    public void Every_member_is_reachable_so_neither_theory_runs_over_nothing()
    {
        // A derived member list that came back empty would leave both theories above
        // passing over no cases at all, which reads exactly like a clean result. The
        // figures are asserted as floors rather than as counts, so adding a member to
        // either type does not make this fail for the wrong reason.
        Assert.True(EveryCensusMember().Count >= 30, "the census member list came back short");
        Assert.True(EveryTallyMember().Count >= 6, "the tally member list came back short");
    }

    [Fact]
    public void The_table_covers_every_combination_of_the_legs_there_are()
    {
        // A row per combination, and the number of combinations derived from the enum
        // rather than written as eight: a leg added to the enum leaves this table
        // short, and a table that no longer covers its subject is the failure this
        // whole class exists to prevent.
        var legs = Enum.GetValues<WithholdingLeg>().Length;

        Assert.Equal(1 << legs, Table().Count);
    }
}
