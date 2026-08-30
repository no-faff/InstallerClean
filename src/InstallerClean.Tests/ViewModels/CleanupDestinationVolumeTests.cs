using System.IO.Abstractions;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The volume answer that comes back after the box has moved on, and what
/// <see cref="CleanupViewModel"/> does with it.
/// </summary>
/// <remarks>
/// TWO THINGS KEEP A STALE ANSWER OFF THE TOOLTIP AND THIS IS THE SECOND. A
/// keystroke cancels the resolve belonging to the path it replaces, which stops
/// one still sitting in its debounce. A resolve whose debounce has already
/// elapsed is past the point a token can stop it, so it runs to the end, comes
/// back with an answer in its hand, and asks on the way in whether that answer
/// is still wanted. That question is what this pins.
///
/// THE ANSWER ARRIVES WHEN THIS FILE SAYS, WHICH IS WHY THE RESOLVE IS HANDED
/// IN. The view model takes the volume question as a function and defaults it to
/// the real one, so a test can hold an answer open across the keystroke that
/// makes it stale. Nothing about the view model's own timing is exposed: the
/// same debounce runs, the same hop off the dispatcher, the same check on the
/// way back. A real resolve takes as long as Windows takes; this one takes as
/// long as the test wants.
/// </remarks>
public class CleanupDestinationVolumeTests
{
    /// <summary>
    /// How long a wait is given before it is reported as a failure naming the
    /// signal that never came. Generous, because it is only ever reached when
    /// something has gone wrong, and a test that hangs says nothing to anybody.
    /// </summary>
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long the held answer is given to land after it has been let go. A
    /// published answer raises PropertyChanged as the resolve returns, so this
    /// is the wait that ends early whenever there is anything to report; the
    /// full window is spent only when nothing is published, which is the point.
    /// </summary>
    private static readonly TimeSpan SilenceWindow = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task An_answer_arriving_after_the_box_has_moved_on_is_not_published()
    {
        // Told apart by the fake: the first is held open until this test lets
        // it go and then calls the folder same-drive, the second replies at once
        // and says somewhere else. Neither is looked at on disk.
        const string leftBehind = @"C:\ic-left-behind";
        const string inTheBox = @"C:\ic-in-the-box";

        using var resolveStarted = new SemaphoreSlim(0);
        var releaseTheResolve = new TaskCompletionSource();
        var resolveReturned = new TaskCompletionSource<bool>();

        bool? Resolve(string destination)
        {
            if (!string.Equals(destination, leftBehind, StringComparison.Ordinal)) return false;

            resolveStarted.Release();
            // Never throws, whatever happens to the wait. This runs inside a
            // Task.Run whose task the view model does not await, so an
            // exception here would surface later and somewhere else; the wait's
            // outcome is reported through the assertion below instead.
            resolveReturned.TrySetResult(releaseTheResolve.Task.Wait(SignalTimeout));
            return true;
        }

        var published = new List<bool?>();
        var publishedLock = new object();
        var firstAnswer = new TaskCompletionSource();
        var secondAnswer = new TaskCompletionSource();

        using var vm = CreateViewModel(Resolve);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(CleanupViewModel.DestinationIsOnCacheVolume)) return;

            int count;
            lock (publishedLock)
            {
                published.Add(vm.DestinationIsOnCacheVolume);
                count = published.Count;
            }

            if (count == 1) firstAnswer.TrySetResult();
            else secondAnswer.TrySetResult();
        };

        // Waiting for the fake to be entered is what makes this the case it is
        // named for: the debounce is over and the resolve is genuinely in
        // flight, rather than parked where a cancel would still reach it.
        vm.MoveDestination = leftBehind;
        Assert.True(
            await resolveStarted.WaitAsync(SignalTimeout),
            $"the resolve for '{leftBehind}' never started");

        // The keystroke that leaves that path behind, with its resolve still
        // inside the fake. This one's own answer takes the ordinary route.
        vm.MoveDestination = inTheBox;
        await Reached(firstAnswer.Task, $"'{inTheBox}' never got an answer");

        // And now the held resolve comes back, carrying an answer about a folder
        // nobody is looking at any more.
        releaseTheResolve.SetResult();
        Assert.True(
            await resolveReturned.Task,
            $"the resolve for '{leftBehind}' was never released");

        // It must go nowhere, so what is waited on is the publish that must not
        // happen. Where the answer does land it lands as the resolve returns,
        // and this ends on that rather than on the clock.
        var staleAnswerLanded =
            await Task.WhenAny(secondAnswer.Task, Task.Delay(SilenceWindow)) == secondAnswer.Task;

        bool?[] answers;
        lock (publishedLock) answers = published.ToArray();

        Assert.False(
            staleAnswerLanded,
            "a resolve for a path the box had left published its answer over the current one. " +
            $"Answers published, in order: {string.Join(", ", answers.Select(Describe))}");
        Assert.Equal(new bool?[] { false }, answers);
        Assert.False(vm.DestinationIsOnCacheVolume);
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
