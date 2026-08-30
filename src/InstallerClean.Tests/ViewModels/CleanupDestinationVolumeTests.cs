using System.IO.Abstractions;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// What <see cref="CleanupViewModel"/> does with a volume answer belonging to a
/// path the Move box has moved on from.
/// </summary>
/// <remarks>
/// THREE STATES REACH THAT, THEY ARE STOPPED BY THREE DIFFERENT THINGS, AND
/// THERE IS A TEST FOR EACH. A keystroke cancels the resolve belonging to the
/// path it replaces, and one still parked in its debounce dies there without
/// asking anything. A resolve past its debounce is beyond a token, so it runs to
/// the end and is met on the way back by a guard with two arms: the source is
/// gone, which is what a newer resolve that has already answered leaves behind,
/// or the source is a different one, which is what a newer resolve still inside
/// its own debounce is holding.
///
/// THE TESTS ARE NAMED FOR THE STATE THEY CREATE AND NOT FOR WHAT COMES OF IT,
/// because what comes of it is the same in all three: the tooltip says the same
/// sentence and the flag holds the same value. Naming by outcome puts three
/// different things behind one description, and then one of them can go without
/// anything changing colour.
///
/// SO THEY ARE WATCHED THROUGH TWO DIFFERENT WINDOWS. The cancel is read from
/// which paths the resolve was ASKED ABOUT, which is the only place a resolve
/// that never runs shows up at all; the two arms are read from which answers were
/// PUBLISHED, and what separates them is where the box had got to by the time the
/// older answer came back.
///
/// THE ANSWER ARRIVES WHEN THIS FILE SAYS, WHICH IS WHY THE RESOLVE IS HANDED
/// IN. The view model takes the volume question as a function and defaults it to
/// the real one, so a test can hold an answer open across the keystroke that
/// makes it stale. Nothing about the view model's own timing is exposed: the
/// same debounce runs, the same hop off the dispatcher, the same guard on the
/// way back. A real resolve takes as long as Windows takes; this one takes as
/// long as the test wants.
/// </remarks>
public class CleanupDestinationVolumeTests
{
    /// <summary>The path the box starts on and then leaves.</summary>
    private const string LeftBehind = @"C:\ic-left-behind";

    /// <summary>The path the box holds for the rest of each test.</summary>
    private const string InTheBox = @"C:\ic-in-the-box";

    /// <summary>
    /// How long a wait is given before it is reported as a failure naming the
    /// signal that never came. Generous, because it is only ever reached when
    /// something has gone wrong, and a test that hangs says nothing to anybody.
    /// </summary>
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long the newer resolve is given to finish after its answer appears.
    /// The flag is raised from inside the publish and the source that resolve was
    /// holding is let go on the two lines after it, so the state named "after the
    /// newer one has answered" begins a moment later than the property change
    /// announcing it. Wildly longer than two statements, and spent once.
    /// </summary>
    private static readonly TimeSpan PublishFinishes = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// How long an answer belonging to the path the box left is given to appear
    /// after it has had its chance. An answer that is published raises
    /// PropertyChanged as the resolve returns, so this ends early whenever there
    /// is anything to report and is spent in full only when nothing lands.
    /// </summary>
    private static readonly TimeSpan SilenceWindow = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task The_box_moves_on_while_the_resolve_is_parked_in_its_debounce()
    {
        var askedAbout = new List<string>();
        var askedLock = new object();
        var theParkedPathWasAsked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var somethingWasAnswered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        bool? Resolve(string destination)
        {
            lock (askedLock) askedAbout.Add(destination);
            if (string.Equals(destination, LeftBehind, StringComparison.Ordinal))
                theParkedPathWasAsked.TrySetResult();
            return false;
        }

        using var vm = CreateViewModel(Resolve);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CleanupViewModel.DestinationIsOnCacheVolume) &&
                vm.DestinationIsOnCacheVolume is not null)
                somethingWasAnswered.TrySetResult();
        };

        // Adjacent, so the first resolve is still inside its debounce when the
        // second arrives. The second set is also what makes this the state at
        // all: a first assignment has no resolve behind it to cancel.
        vm.MoveDestination = LeftBehind;
        vm.MoveDestination = InTheBox;

        // The surviving path answering is the signal that the debounce window
        // has been and gone. Both resolves were armed within microseconds of
        // each other and the abandoned one was armed first, so one that lived
        // through its debounce would have reached the fake by now.
        await Reached(somethingWasAnswered.Task, $"'{InTheBox}' never got an answer");

        var theParkedResolveRan =
            await Task.WhenAny(theParkedPathWasAsked.Task, Task.Delay(SilenceWindow))
                == theParkedPathWasAsked.Task;

        string[] asked;
        lock (askedLock) asked = askedAbout.ToArray();

        Assert.False(
            theParkedResolveRan,
            "the volume was asked about a path the box had already left. " +
            $"Asked about, in order: {string.Join(", ", asked)}");
        Assert.Equal(new[] { InTheBox }, asked);
    }

    [Fact]
    public async Task The_earlier_resolve_returns_while_the_newer_one_is_still_in_its_debounce()
    {
        using var held = new HeldResolve(LeftBehind);
        using var vm = CreateViewModel(held.Answer);
        var published = PublishedAnswers.Watching(vm);

        vm.MoveDestination = LeftBehind;
        Assert.True(await held.Entered(), $"the resolve for '{LeftBehind}' never started");

        // Released in the same breath as the keystroke, so the newer resolve is
        // barely into its own debounce and has not published anything. The source
        // the guard reads is that newer resolve's, and telling it apart from the
        // one this answer belongs to is what turns the answer away.
        vm.MoveDestination = InTheBox;
        held.Release();
        Assert.True(await held.Returned, $"the resolve for '{LeftBehind}' was never released");

        await SettleAsync(published);
        AssertTheBoxsOwnAnswerStandsAlone(published);
    }

    [Fact]
    public async Task The_earlier_resolve_returns_after_the_newer_one_has_answered()
    {
        using var held = new HeldResolve(LeftBehind);
        using var vm = CreateViewModel(held.Answer);
        var published = PublishedAnswers.Watching(vm);

        vm.MoveDestination = LeftBehind;
        Assert.True(await held.Entered(), $"the resolve for '{LeftBehind}' never started");

        // Held across the keystroke and past the newer resolve's whole debounce
        // and query, so by the time it is let go the newer answer has been
        // published and the source it was holding has been let go with it. The
        // guard meets nothing to compare against, and that is its other arm.
        vm.MoveDestination = InTheBox;
        await Reached(published.TheBoxsOwnAnswer, $"'{InTheBox}' never got an answer");
        await Task.Delay(PublishFinishes);

        held.Release();
        Assert.True(await held.Returned, $"the resolve for '{LeftBehind}' was never released");

        await SettleAsync(published);
        AssertTheBoxsOwnAnswerStandsAlone(published);
    }

    /// <summary>
    /// Waits for an answer to be published and then for anything else to follow,
    /// so an answer belonging to the path the box left has had its chance to land
    /// before the list is read. Neither wait asserts and neither cares which
    /// answer arrived: what did and did not arrive is read off the list itself,
    /// which is also what lets a run that has already gone wrong end quickly
    /// rather than sitting out a wait for a signal that is not coming.
    /// </summary>
    private static async Task SettleAsync(PublishedAnswers published)
    {
        await Task.WhenAny(published.AnyAnswer, Task.Delay(SignalTimeout));
        await Task.WhenAny(published.AnythingFurther, Task.Delay(SilenceWindow));
    }

    /// <summary>
    /// The one thing all three states have to come to, whichever of them was
    /// built: the flag carries the answer for the path in the box and no other
    /// answer was ever published over it.
    /// </summary>
    private static void AssertTheBoxsOwnAnswerStandsAlone(PublishedAnswers published)
    {
        var answers = published.Snapshot();
        Assert.True(
            answers.Length == 1 && answers[0] == false,
            "the only answer published should be the one for the path in the box. " +
            $"Published, in order: {string.Join(", ", answers.Select(Describe))}");
    }

    /// <summary>Names the three states the flag can hold for a failure message.</summary>
    private static string Describe(bool? answer) => answer?.ToString() ?? "unanswered";

    /// <summary>
    /// Awaits <paramref name="signal"/>, failing with <paramref name="whatNeverCame"/>
    /// rather than hanging. A hang is the one outcome that tells nobody anything.
    /// </summary>
    private static async Task Reached(Task signal, string whatNeverCame) =>
        Assert.True(await Task.WhenAny(signal, Task.Delay(SignalTimeout)) == signal, whatNeverCame);

    /// <summary>
    /// The volume question, driven by the test. One path is held: the answer for
    /// it is not given until the test lets go, and then it is "on the cache
    /// volume". Every other path is answered at once with "somewhere else", so
    /// the two are told apart by value as well as by which was asked.
    /// </summary>
    private sealed class HeldResolve : IDisposable
    {
        private readonly string _held;
        private readonly SemaphoreSlim _entered = new(0);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _returned =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal HeldResolve(string held) => _held = held;

        /// <summary>
        /// Runs on the thread pool inside a task the view model does not await,
        /// so it never throws: what became of its wait is carried out through
        /// <see cref="Returned"/> and asserted where an assertion is read.
        /// </summary>
        internal bool? Answer(string destination)
        {
            if (!string.Equals(destination, _held, StringComparison.Ordinal)) return false;

            _entered.Release();
            _returned.TrySetResult(_release.Task.Wait(SignalTimeout));
            return true;
        }

        /// <summary>
        /// Completes once the held path's resolve has started, which is what says
        /// its debounce is over and it is genuinely running.
        /// </summary>
        internal Task<bool> Entered() => _entered.WaitAsync(SignalTimeout);

        internal void Release() => _release.SetResult();

        /// <summary>True once the held resolve has been let go and has returned.</summary>
        internal Task<bool> Returned => _returned.Task;

        public void Dispose()
        {
            // Frees the pool thread the fake sits on if a test leaves before
            // letting go, so a failure is reported by the assertion that failed.
            _release.TrySetResult();
            _entered.Dispose();
        }
    }

    /// <summary>
    /// Every value published to <see cref="CleanupViewModel.DestinationIsOnCacheVolume"/>,
    /// in order, with signals for the two moments the tests wait on.
    /// </summary>
    private sealed class PublishedAnswers
    {
        private readonly List<bool?> _answers = new();
        private readonly object _gate = new();
        private readonly TaskCompletionSource _anyAnswer =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _theBoxsOwn =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _anythingFurther =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completes on the first answer published, whatever it is.</summary>
        internal Task AnyAnswer => _anyAnswer.Task;

        /// <summary>Completes when the answer for the path in the box is published.</summary>
        internal Task TheBoxsOwnAnswer => _theBoxsOwn.Task;

        /// <summary>Completes on any answer published after the first.</summary>
        internal Task AnythingFurther => _anythingFurther.Task;

        internal static PublishedAnswers Watching(CleanupViewModel vm)
        {
            var published = new PublishedAnswers();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(CleanupViewModel.DestinationIsOnCacheVolume)) return;

                var answer = vm.DestinationIsOnCacheVolume;
                int count;
                lock (published._gate)
                {
                    published._answers.Add(answer);
                    count = published._answers.Count;
                }

                published._anyAnswer.TrySetResult();
                if (answer == false) published._theBoxsOwn.TrySetResult();
                if (count > 1) published._anythingFurther.TrySetResult();
            };
            return published;
        }

        internal bool?[] Snapshot()
        {
            lock (_gate) return _answers.ToArray();
        }
    }

    /// <summary>
    /// A <see cref="CleanupViewModel"/> over substituted services, with the
    /// volume question answered by <paramref name="resolveIsOnCacheVolume"/>.
    /// Nothing here reaches a disk: the settings service is asked for a default
    /// AppSettings and reports its debounced write-back as having succeeded.
    /// </summary>
    private static CleanupViewModel CreateViewModel(Func<string, bool?> resolveIsOnCacheVolume)
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.Load().Returns(new AppSettings());
        settingsService.Update(Arg.Any<Action<AppSettings>>()).Returns(true);

        var scan = new ScanViewModel(
            Substitute.For<IFileSystemScanService>(),
            Substitute.For<IPendingRebootService>(),
            Substitute.For<IDialogService>());

        return new CleanupViewModel(
            Substitute.For<IMoveFilesService>(),
            Substitute.For<IDeleteFilesService>(),
            settingsService,
            Substitute.For<IDialogService>(),
            Substitute.For<IConfirmationService>(),
            Substitute.For<IFileSystem>(),
            scan,
            new CompletionViewModel(),
            Substitute.For<IResultLogService>(),
            Substitute.For<IRemovableReverifier>(),
            resolveIsOnCacheVolume);
    }
}
