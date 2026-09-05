using System.CodeDom.Compiler;
using System.Reflection;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The rule every public <c>Show*</c> method on <see cref="CompletionViewModel"/> keeps:
/// each one sets every field the completion card reads.
///
/// WHY IT IS LOAD-BEARING RATHER THAN TIDY. The view-model instance is REUSED across
/// operations, so a method that left one field alone would not leave it blank; it
/// would paint the PREVIOUS run's value under this run's heading. A Move that failed
/// at two files, then a Delete that succeeded, would show the delete's green heading
/// over the move's "2 of 71 could not be moved". Several of the fields say so on their
/// own declaration, in the words "cleared everywhere else, because the view-model
/// instance is reused across operations".
///
/// <c>The_count_line_and_warning_heading_do_not_survive_into_the_next_operation</c> in
/// CompletionViewModelTests holds one transition and two of the fields, and stays: it
/// is the specific case, and this is the same rule at its full width. The shape to
/// expect is an eighth screen, this class having gained two in 3.0.0.
///
/// WHY THE SHAPE IS TWO RUNS AND NOT AN ASSERTION THAT IT ASSIGNS. An assignment is
/// not observable from outside the object, and the two obvious instruments are both
/// blind. Reading the field after one call cannot tell an assignment from a field
/// that happened to hold the right value already, since a fresh screen's defaults are
/// empty and false and so is most of what these methods write. And PropertyChanged is
/// worse than useless here: CommunityToolkit's generated setter calls SetProperty,
/// which compares first and raises NOTHING when the value is unchanged, so a method
/// that correctly assigns a value equal to the one already there would read as a miss.
///
/// So the property tested is the one that actually matters: the screen a Show* method
/// paints must not depend on what the instance held before it. The same call is made
/// twice with the same arguments, once on a fresh instance and once on an instance
/// whose every field has been set to a value no default holds, and the two screens
/// must come out identical. A field the method forgets keeps the planted value in the
/// second run and not in the first, so it names itself.
///
/// THE METHODS AND THE FIELDS ARE BOTH DISCOVERED, which is the whole point: a screen
/// added next year is walked without anybody remembering this file, and so is a field.
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
        /// <summary>Files moved or deleted, nothing failed, nothing held back.</summary>
        DidWork,

        /// <summary>Reached no file and something failed, which is the warning-heading branch.</summary>
        GotNowhere,
    }

    /// <summary>
    /// The two observable properties a <c>Show*</c> method is right not to touch.
    /// They are the send-a-report lifecycle rather than the completion card: one is
    /// set by <see cref="CompletionViewModel.MarkResultLogReady"/> once the log has
    /// been written, the other only while a send is in flight, and a screen method
    /// clearing either would retract a button the user is part-way through pressing.
    ///
    /// Named rather than discovered because nothing in the type tells them apart, and
    /// <c>nameof</c> rather than a string so a rename cannot leave a dead exclusion
    /// here silently widening what the walk skips.
    /// </summary>
    private static readonly string[] NotPaintedByAScreen =
    {
        nameof(CompletionViewModel.IsResultLogReady),
        nameof(CompletionViewModel.IsSendingResultLog),
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

                var fresh = new CompletionViewModel();
                screen.Invoke(fresh, arguments);

                var reused = new CompletionViewModel();
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
        // The control under the test above, and it is not decoration: the plant is the
        // whole instrument. A value equal to a fresh screen's default makes a
        // forgotten field indistinguishable from a set one, the comparison finds no
        // difference, and the test reports every screen clean for the same reason it
        // would if it had never run.
        var fresh = new CompletionViewModel();
        var planted = new CompletionViewModel();
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
        // Both figures are here because both discoveries can silently return nothing.
        // The field walk keys on a source generator's own tool name, which a package
        // bump can rename, and a zero-length field set makes the first test pass over
        // every screen without comparing anything at all. The method walk keys on a
        // prefix, and a rename away from Show* would empty it the same way.
        //
        // A LEGITIMATE ADDITION IS MEANT TO FAIL HERE. Adding a ninth screen or a
        // fifteenth observable property should be a decision somebody takes with this
        // rule in front of them, which is what a figure that has to be moved by hand
        // buys. A new field joins the set every screen must paint; a new field that is
        // NOT part of the card goes in NotPaintedByAScreen with its reason.
        Assert.Equal(8, Screens().Length);
        Assert.Equal(12, ScreenFields().Length);
    }
}
