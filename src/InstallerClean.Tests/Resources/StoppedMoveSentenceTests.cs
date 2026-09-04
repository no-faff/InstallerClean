using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

/// <summary>
/// The sentence both hosts show when a Move's destination guard stops a batch
/// part way. There are two of them because the two hosts close on different
/// actions, and everything before that has to say the same thing: a repair
/// applied to one and not the other is how this pair goes wrong.
/// </summary>
public class StoppedMoveSentenceTests
{
    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>
    /// The shared half, taken from the two values rather than written out here, so
    /// this cannot go stale against a rewording and cannot pass by agreeing with a
    /// copy of itself.
    /// </summary>
    private static string SharedOpening()
    {
        var window = Strings.Error_DestinationChangedMidBatch;
        var cli = Strings.Cli_DestinationChangedMidBatch;
        var i = 0;
        while (i < window.Length && i < cli.Length && window[i] == cli[i]) i++;
        return window[..i];
    }

    [Fact]
    public void The_two_hosts_agree_on_everything_before_the_action_they_close_on()
    {
        // Both keys are declared in every satellite, so the shared opening is in
        // whatever language the host is running under and an English fragment is
        // not in it. These two are read by a person in their own language, so the
        // pin is the displayed language rather than the machine-contract scope.
        using var scope = new LocalisationScope(British);

        // Divergence at the closing action and nowhere earlier. The window offers
        // Re-scan and the command line has no such button, which is the whole of
        // why there are two strings.
        Assert.EndsWith(", then ", SharedOpening(), StringComparison.Ordinal);
        Assert.NotEqual(
            Strings.Error_DestinationChangedMidBatch,
            Strings.Cli_DestinationChangedMidBatch);
    }

    [Fact]
    public void Neither_host_says_where_the_files_would_otherwise_have_gone()
    {
        // Two conditions reach this sentence. On one the folder has been replaced
        // or redirected and there is somewhere else the files could have landed;
        // on the other it has stopped resolving at all and there is nowhere. So
        // the sentence says what the run did and leaves what it avoided alone,
        // which is the same rule the note beside these values applies to the
        // cause. The assertion above is what makes this absence attributable: a
        // sentence that had stopped saying anything would fail it. Pinned for the
        // reason the test above gives, and this one leans on it harder: an English
        // phrase is absent from a French sentence for a reason that has nothing to
        // do with what the sentence says.
        using var scope = new LocalisationScope(British);

        Assert.DoesNotContain("wrong place", SharedOpening(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo culture) => Localisation.Set(culture, culture);

        public void Dispose() => Localisation.Reset();
    }
}
