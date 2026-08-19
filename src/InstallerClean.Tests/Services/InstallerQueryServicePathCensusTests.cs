using System.Reflection;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The path census as a STRUCTURE rather than as a number: that every counter on it
/// is folded when two halves of a scan are merged, and that every member of the two
/// enums behind it reaches a counter of its own.
///
/// WHY THESE ARE REFLECTIVE AND NOT WRITTEN OUT. The census is a hand-written merge
/// of ten int fields and two switch statements with no default arm, so a counter
/// added later and forgotten in any of the three compiles, builds green and reads
/// zero for whichever half of the scan was not folded. That direction is the unsafe
/// one: the four normalisation refusals are summed into the total the walk-derived
/// offer is WITHHELD on, so a refusal counted into a census that goes nowhere is a
/// withholding that does not happen and files offered that the app meant to keep
/// back. Writing the ten names out again would leave the eleventh exactly as
/// forgettable as it is today. Enumerating them is what makes the test notice.
///
/// FIELDS ONLY, NEVER PROPERTIES. <c>NormalisationRefusedTotal</c> is derived from
/// four of the fields, so a property sweep would either double-count it or need an
/// exclusion list somebody has to remember, which is the same fault one level up.
///
/// WHAT NO TEST HERE OR ANYWHERE CAN REACH, said plainly so this file is not read as
/// covering the counters end to end. Three of the five resolver outcomes
/// (<c>OpenRefused</c>, <c>FinalNameUnavailable</c>, <c>Faulted</c>) are decided by
/// a real handle: CreateFile refusing one, GetFinalPathNameByHandle returning a
/// zero-length name, or the call throwing. No fixture in this suite can produce any
/// of those deterministically on a CI runner, because the resolution is a static
/// call on the real filesystem with no seam in front of it. What is covered is that
/// each of the five is WIRED to a counter of its own, which is what these walks
/// establish, and that the two reachable ones move a real scan's census, which
/// <see cref="InstallerQueryServiceUnitTests"/> asserts. A zero in the other three
/// is not evidence of anything and must not be read as one.
/// </summary>
public class InstallerQueryServicePathCensusTests
{
    /// <summary>
    /// The census's counters: every mutable int field on it. Readonly ints are
    /// excluded and there is one, the owning thread the debug guard stamps at
    /// construction, which is a fact about the instance rather than a tally.
    /// </summary>
    private static FieldInfo[] Counters() =>
        typeof(InstallerQueryService.PathCensus)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(int) && !f.IsInitOnly)
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .ToArray();

    private static int Read(FieldInfo f, InstallerQueryService.PathCensus c) => (int)f.GetValue(c)!;

    private static string[] Moved(InstallerQueryService.PathCensus c) =>
        Counters().Where(f => Read(f, c) != 0).Select(f => f.Name).ToArray();

    [Fact]
    public void The_census_still_holds_the_ten_counters_this_file_walks()
    {
        // A COUNT ASSERTION THAT EXISTS TO BE READ BY WHOEVER ADDS THE ELEVENTH. It
        // is not here to keep the number at ten: it is here so that adding a counter
        // fails a test that tells you what else the counter needs, rather than
        // passing and reading zero on half of every scan.
        var counters = Counters();
        Assert.True(counters.Length == 10,
            $"The census now holds {counters.Length} int fields rather than 10. If you have added "
            + "a counter, this test is the checklist: fold it in PathCensus.Add, record it from "
            + "the switch that produces it, add it to the EnumerationCensus record (APPEND, never "
            + "insert: the arguments are positional and all int), carry it in the payload, and "
            + "put it in NormalisationRefusedTotal if it is a refusal, because that total is what "
            + "the walk-derived offer is withheld on. Then change the 10 here. Fields found: "
            + string.Join(", ", counters.Select(f => f.Name)));
    }

    [Fact]
    public void Add_folds_every_counter_on_the_census()
    {
        // THE MERGE IS WHAT THE REPORT'S CENSUS IS MADE OF. The API loop and the
        // registry fallback each normalise their own paths and neither can see the
        // other's, so a field Add does not fold reports the API half alone and the
        // fallback's refusals vanish silently.
        var counters = Counters();
        var target = new InstallerQueryService.PathCensus();
        var other = new InstallerQueryService.PathCensus();

        // Distinct values per field and per side, so a fold that reads the wrong
        // field is caught as well as one that reads none.
        for (var i = 0; i < counters.Length; i++)
        {
            counters[i].SetValue(target, 1000 * (i + 1));
            counters[i].SetValue(other, i + 1);
        }

        target.Add(other);

        foreach (var (f, i) in counters.Select((f, i) => (f, i)))
            Assert.True(Read(f, target) == 1000 * (i + 1) + (i + 1),
                $"PathCensus.Add did not fold {f.Name}: expected {1000 * (i + 1) + (i + 1)}, "
                + $"got {Read(f, target)}. Add one line for it to PathCensus.Add. Until then the "
                + "registry fallback's tally of this counter is dropped from every scan's census, "
                + "and if it is a normalisation refusal the withholding that reads the total will "
                + "not fire for it.");
    }

    [Fact]
    public void Add_of_null_is_the_identity()
    {
        // The fallback is optional, so half the scans in the suite fold a null.
        var counters = Counters();
        var target = new InstallerQueryService.PathCensus();
        for (var i = 0; i < counters.Length; i++) counters[i].SetValue(target, i + 1);

        target.Add(null);

        foreach (var (f, i) in counters.Select((f, i) => (f, i)))
            Assert.Equal(i + 1, Read(f, target));
    }

    [Fact]
    public void Every_resolution_outcome_reaches_a_counter_of_its_own()
    {
        // RecordResolution is a switch with no default arm, so a member added to
        // PathResolution and forgotten there is counted nowhere and reads as an
        // outcome that never happened. The five failures are read against
        // ResolverAttempts, and an outcome missing from the count makes the
        // attempts and the outcomes disagree with no way to see which is short.
        var seen = new Dictionary<string, PathResolution>(StringComparer.Ordinal);

        foreach (var outcome in Enum.GetValues<PathResolution>())
        {
            // Resolved is deliberately not counted: it is the attempts less the
            // five, and a stored copy could disagree with them. Asserted below as
            // this walk's must-miss control rather than merely skipped.
            if (outcome == PathResolution.Resolved) continue;

            var census = new InstallerQueryService.PathCensus();
            census.RecordResolution(outcome);
            var moved = Moved(census);

            Assert.True(moved.Length == 1,
                $"PathResolution.{outcome} moved {moved.Length} counters ({string.Join(", ", moved)}). "
                + "Every outcome needs exactly one arm in PathCensus.RecordResolution; the switch has "
                + "no default, so a missing arm counts the outcome nowhere and the report reads it as "
                + "an outcome that never occurred.");
            Assert.True(!seen.TryGetValue(moved[0], out var already),
                $"PathResolution.{outcome} and PathResolution.{already} both count into {moved[0]}, "
                + "so the report cannot tell the two apart.");
            seen[moved[0]] = outcome;
        }
    }

    [Fact]
    public void A_resolved_path_is_counted_as_no_failure_at_all()
    {
        // THE MUST-MISS CONTROL for the walk above. Without it, a RecordResolution
        // that incremented something unconditionally would satisfy every case in it.
        var census = new InstallerQueryService.PathCensus();

        census.RecordResolution(PathResolution.Resolved);

        Assert.Empty(Moved(census));
    }

    [Fact]
    public void Every_normalisation_stage_reaches_a_counter_of_its_own_and_the_total()
    {
        // The same shape as the walk above, and this one carries the derived total
        // with it. The total is the population the walk-derived offer is withheld
        // on, and a stage that reaches a counter outside it is a cause the
        // withholding silently does not act on: green build, counter reporting,
        // withholding just not firing. That is the fault this release found in the
        // embedded-null case.
        var seen = new Dictionary<string, InstallerQueryService.NormalisationStage>(StringComparer.Ordinal);

        foreach (var stage in Enum.GetValues<InstallerQueryService.NormalisationStage>())
        {
            var census = new InstallerQueryService.PathCensus();
            census.RecordNormalisationRefusal(stage);
            var moved = Moved(census);

            Assert.True(moved.Length == 1,
                $"NormalisationStage.{stage} moved {moved.Length} counters ({string.Join(", ", moved)}). "
                + "Every stage needs exactly one arm in PathCensus.RecordNormalisationRefusal, whose "
                + "switch has no default arm.");
            Assert.True(!seen.TryGetValue(moved[0], out var already),
                $"NormalisationStage.{stage} and NormalisationStage.{already} both count into "
                + $"{moved[0]}, so the four causes cannot be told apart and nothing may state a "
                + "cause for the set.");
            seen[moved[0]] = stage;

            Assert.True(census.NormalisationRefusedTotal == 1,
                $"NormalisationStage.{stage} moved {moved[0]} but left NormalisationRefusedTotal at "
                + $"{census.NormalisationRefusedTotal}. The total is what the walk-derived offer is "
                + "withheld on, so a refusal outside it is a cause the withholding does not act on.");
        }
    }

#if DEBUG
    /// <summary>
    /// THE CENSUS IS MUTABLE AND THREADED BY ARGUMENT, so what keeps its counts
    /// honest is the shape of the call graph rather than anything the type enforces.
    /// The enumeration is single-threaded by construction of its entry point today,
    /// and that is provable rather than read off: the whole synchronous core runs
    /// inside one Task.Run with no await in it and the file holds no other
    /// concurrency primitive. What this pins is the FIRST change to that. Every
    /// increment is a read-modify-write on a plain int, so two threads on one census
    /// lose counts, and a lost normalisation refusal is a withholding that does not
    /// fire: files offered that the app meant to keep back, on a machine whose scan
    /// reads clean.
    ///
    /// A real thread rather than Task.Run, because a pool thread can be the one the
    /// test itself was running on and the assertion would then pass for the wrong
    /// reason. Debug builds only, which is where the suite runs; a release build must
    /// not acquire a new way to throw on a user's machine for this.
    /// </summary>
    [Fact]
    public void Every_increment_is_held_to_the_thread_that_built_the_census()
    {
        var census = new InstallerQueryService.PathCensus();
        var other = new InstallerQueryService.PathCensus();

        var offenders = new (string Name, Action Act)[]
        {
            ("RecordResolverAttempt", () => census.RecordResolverAttempt()),
            ("RecordResolution", () => census.RecordResolution(PathResolution.NotAPath)),
            ("RecordNormalisationRefusal",
                () => census.RecordNormalisationRefusal(InstallerQueryService.NormalisationStage.FullPath)),
            ("Add", () => census.Add(other)),
        };

        foreach (var (name, act) in offenders)
        {
            Exception? captured = null;
            var thread = new Thread(() => captured = Record.Exception(act));
            thread.Start();
            thread.Join();

            Assert.True(captured is InvalidOperationException,
                $"PathCensus.{name} accepted an increment from another thread (got "
                + $"{captured?.GetType().Name ?? "no exception"}). Every entry point that mutates a "
                + "census has to carry the guard, or a parallelising change loses counts silently "
                + "through the one that does not.");
        }
    }

    [Fact]
    public void The_thread_that_built_the_census_increments_it_freely()
    {
        // THE MUST-MISS CONTROL. A guard that refused every caller would satisfy the
        // test above and stop every scan, so this drives the same four entry points
        // from the owning thread and reads the counts back.
        var census = new InstallerQueryService.PathCensus();
        var other = new InstallerQueryService.PathCensus();
        other.RecordResolverAttempt();

        census.RecordResolverAttempt();
        census.RecordResolution(PathResolution.NotAPath);
        census.RecordNormalisationRefusal(InstallerQueryService.NormalisationStage.FullPath);
        census.Add(other);

        Assert.Equal(2, census.ResolverAttempts);
        Assert.Equal(1, census.ResolverNotAPath);
        Assert.Equal(1, census.NormalisationRefusedTotal);
    }
#endif
}
