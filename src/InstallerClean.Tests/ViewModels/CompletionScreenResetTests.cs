using System.CodeDom.Compiler;
using System.Reflection;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The rule every public <c>Show*</c> method on <see cref="CompletionViewModel"/> keeps:
/// each one sets every field the completion card reads.
///
/// The view-model instance is reused across operations, so a method that left a field
/// alone would paint the previous run's value under this run's heading: a Move that
/// failed at two files, then a Delete that succeeded, would show the delete's green
/// heading over the move's "2 of 71 could not be moved". Several of the fields say so on
/// their own declaration, in the words "cleared everywhere else, because the view-model
/// instance is reused across operations".
///
/// <c>The_count_line_and_warning_heading_do_not_survive_into_the_next_operation</c> in
/// CompletionViewModelTests holds one transition and two of the fields. This class holds
/// the same rule for every screen and every field.
///
/// Each screen is painted twice rather than checked for an assignment, because an
/// assignment is not observable from outside the object. Reading a field after one call
/// cannot tell an assignment from a field that already held the right value, since a
/// fresh screen's defaults are empty and false and so is most of what these methods
/// write. PropertyChanged cannot tell either: CommunityToolkit's generated setter calls
/// SetProperty, which compares first and raises nothing when the value is unchanged.
///
/// So the same call is made twice with the same arguments, once on a fresh instance and
/// once on an instance whose every field has been set to a value no default holds, and
/// the two screens must come out identical. A field the method does not set keeps the
/// planted value in the second run and not in the first, and the failure names it.
///
/// The methods and the fields are both discovered, so a new screen or a new field is
/// walked without this file changing.
/// <c>The_walk_finds_the_screens_and_the_fields_rather_than_an_empty_set</c> is the
/// control under that, because a discovery that matches nothing makes every assertion
/// here pass over an empty set.
/// </summary>
public class CompletionScreenResetTests
{
    /// <summary>
    /// Which argument vector a screen is being painted with. Every field on these
    /// methods is assigned on a straight line, but the VALUES sit behind conditions
    /// (a run that reached no file swaps the heading, empties the summary and drops
    /// the restore line), so both sides of those conditions are walked rather than
    /// whichever one a single vector happened to take.
    /// </summary>
    private enum Run
    {
        /// <summary>
        /// Files moved or deleted, nothing failed, nothing held back by the check before
        /// acting, and one file held back by the scan the nothing-offered screen is given.
        /// </summary>
        DidWork,

        /// <summary>Reached no file and something failed, which is the warning-heading branch.</summary>
        GotNowhere,
    }

    /// <summary>
    /// The three observable properties a <c>Show*</c> method is right not to touch.
    /// They are the report box rather than the card's outcome. The first two are set
    /// by <see cref="CompletionViewModel.TakeReport"/> before the Show* method that
    /// reveals the PC's first card, and cleared as that card closes, so a screen
    /// method setting either would take the box off the one card that carries it. The
    /// third is the panel the box's "i" opens, which only the "i" opens and which
    /// closes as the box leaves the card.
    ///
    /// Named rather than discovered because nothing in the type tells them apart, and
    /// <c>nameof</c> rather than a string so a rename cannot leave a dead exclusion
    /// here silently widening what the walk skips.
    /// </summary>
    private static readonly string[] NotPaintedByAScreen =
    {
        nameof(CompletionViewModel.OffersReport),
        nameof(CompletionViewModel.SendsReport),
        nameof(CompletionViewModel.ReportPanelOpen),
    };

    /// <summary>
    /// Every field the completion card reads, taken from the generator rather than
    /// from a list: CommunityToolkit stamps each property it writes for an
    /// <c>[ObservableProperty]</c> field with its own tool name, and those are exactly
    /// the ones bound on the overlay.
    /// </summary>
    private static PropertyInfo[] ScreenFields() =>
        typeof(CompletionViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite
                && p.GetCustomAttribute<GeneratedCodeAttribute>()?.Tool
                    == "CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator")
            .Where(p => !NotPaintedByAScreen.Contains(p.Name))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// Every screen the completion card can be put into. <c>IsSpecialName</c> drops
    /// property accessors, which is what would otherwise let a future property called
    /// something beginning "Show" in through its getter.
    /// </summary>
    private static MethodInfo[] Screens() =>
        typeof(CompletionViewModel)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.Name.StartsWith("Show", StringComparison.Ordinal))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// Sets every field to something a fresh screen never holds, so that a field a
    /// method leaves alone is visible afterwards.
    /// <c>The_planted_values_are_ones_a_fresh_screen_never_holds</c> is what keeps
    /// that true; a plant equal to the default would make a forgotten field invisible
    /// and every assertion in the first test would still pass.
    /// </summary>
    private static void Plant(CompletionViewModel screen)
    {
        foreach (var field in ScreenFields())
            field.SetValue(screen, Planted(field.PropertyType, field.Name));
    }

    private static object Planted(Type type, string name)
    {
        if (type == typeof(bool)) return true;
        if (type == typeof(string)) return "left behind by the previous run";
        throw new NotSupportedException(
            $"{name} is a {type.Name}, which this file has no planted value for. Add one that a "
            + "fresh CompletionViewModel cannot hold, or the field is walked but never checked.");
    }

    private static object?[] ArgumentsFor(MethodInfo screen, Run run) =>
        screen.GetParameters().Select(p => Argument(p, run)).ToArray();

    /// <summary>
    /// One argument per parameter, by type. Optional parameters are supplied rather
    /// than defaulted, so the held-back line is exercised on both vectors instead of
    /// only on whichever screens declare a re-verify.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// A parameter type nothing here builds. It throws rather than skipping the
    /// screen, because a screen quietly dropped out of the walk is the one shape this
    /// file exists to prevent.
    /// </exception>
    private static object Argument(ParameterInfo parameter, Run run)
    {
        var type = parameter.ParameterType;

        if (type == typeof(int)) return run == Run.DidWork ? 3 : 0;
        if (type == typeof(long)) return run == Run.DidWork ? 4096L : 0L;
        if (type == typeof(bool)) return run == Run.DidWork;
        if (type == typeof(string)) return @"D:\InstallerClean backup";

        // First member on one vector and last on the other, so a screen picking its
        // wording off an enum is painted at both ends of it rather than at one.
        if (type.IsEnum)
        {
            var members = Enum.GetValues(type);
            return members.GetValue(run == Run.DidWork ? 0 : members.Length - 1)!;
        }

        if (type == typeof(IReadOnlyList<FileOperationError>))
            return run == Run.DidWork
                ? Array.Empty<FileOperationError>()
                : new FileOperationError[] { new FileInUse(@"C:\Windows\Installer\a.msi") };

        if (type == typeof(ReverifyResult))
            return run == Run.DidWork
                ? new ReverifyResult([], [])
                : new ReverifyResult([], [@"C:\Windows\Installer\b.msi"],
                    new HeldBackReasons(Reclaimed: 1));

        // A scan holding one file back on one vector and none on the other, so the
        // nothing-offered screen is painted both with a body counting a file and with
        // the line naming a drive or share as its whole body.
        if (type == typeof(ScanResult))
            return run == Run.DidWork
                ? new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                    WithheldFiles: [new OrphanedFile(@"C:\Windows\Installer\c.msi", 1024, false, false, false, "unclaimed")],
                    WithheldBy: new WithholdingSplit(WholesaleCount: 1))
                : new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);

        throw new NotSupportedException(
            $"{parameter.Member.Name} takes a {type.Name} for {parameter.Name}, which this file has "
            + "no argument for. Add one; a screen that cannot be called is a screen that is not checked.");
    }

    [Fact]
    public void Every_screen_paints_every_field_it_owns()
    {
        foreach (var screen in Screens())
        {
            foreach (var run in Enum.GetValues<Run>())
            {
                var arguments = ArgumentsFor(screen, run);

                var fresh = TestCompletion.Create();
                screen.Invoke(fresh, arguments);

                var reused = TestCompletion.Create();
                Plant(reused);
                screen.Invoke(reused, arguments);

                var stale = ScreenFields()
                    .Where(f => !Equals(f.GetValue(fresh), f.GetValue(reused)))
                    .Select(f => f.Name)
                    .ToArray();

                Assert.True(stale.Length == 0,
                    $"{screen.Name} ({run}) left {string.Join(", ", stale)} holding the previous "
                    + "run's value. The completion view model is reused across operations, so every "
                    + "Show* method has to set every field on the card; one it does not set paints "
                    + "the last operation's value under this operation's heading.");
            }
        }
    }

    [Fact]
    public void The_planted_values_are_ones_a_fresh_screen_never_holds()
    {
        // The control under the test above. A planted value equal to a fresh screen's
        // default makes a field the screen does not set indistinguishable from one it
        // sets, the comparison finds no difference, and the test reports every screen
        // clean.
        var fresh = TestCompletion.Create();
        var planted = TestCompletion.Create();
        Plant(planted);

        var invisible = ScreenFields()
            .Where(f => Equals(f.GetValue(fresh), f.GetValue(planted)))
            .Select(f => f.Name)
            .ToArray();

        Assert.True(invisible.Length == 0,
            "Planted with a value a fresh screen already holds, so a screen forgetting one would go "
            + $"unseen: {string.Join(", ", invisible)}. Plant something else.");
    }

    [Fact]
    public void The_walk_finds_the_screens_and_the_fields_rather_than_an_empty_set()
    {
        // Both figures are here because either discovery can return nothing without
        // failing. The field walk keys on a source generator's own tool name, which a
        // package update can rename, and an empty field set makes the first test pass
        // over every screen without comparing anything. The method walk keys on a
        // prefix, and a rename away from Show* empties it the same way.
        //
        // A new screen or a new observable property fails here until the figure is
        // moved by hand, so it is added with this rule in front of whoever adds it. A new
        // field the card shows joins the set every screen must paint; a new field that is
        // not part of the card goes in NotPaintedByAScreen with its reason.
        Assert.Equal(9, Screens().Length);
        Assert.Equal(11, ScreenFields().Length);
    }
}
