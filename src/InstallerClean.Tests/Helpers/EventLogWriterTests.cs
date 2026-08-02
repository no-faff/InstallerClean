using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Pins the guarantee the CLI's audit paths are built on: an event-log write
/// swallows every failure it can meet, the summary build included, and records
/// the miss in <c>EventLogUnavailable</c> instead of throwing.
/// </summary>
/// <remarks>
/// What a throw escaping would cost is stated where the constraint lives, at the
/// CLI's mutex-blocked exit (Program.cs); this pins the property that comment
/// depends on. The console host is not referenced by this project, so the test
/// drives Core's writer directly. A builder that throws is also the only one of
/// the write's failures reachable without a real event log: it fails before the
/// source check, so the assertions hold whatever the CI agent's
/// Application-channel permissions are.
/// </remarks>
public class EventLogWriterTests
{
    [Fact]
    public void Write_swallows_a_summary_builder_that_throws()
    {
        var built = false;

        var escaped = Record.Exception(() => EventLogWriter.Write(
            CliEventClass.TransientSkip,
            () =>
            {
                built = true;
                throw new FormatException("a resx template the summary interpolates");
            }));

        Assert.Null(escaped);
        // The writer has to have asked for the text, not merely declined to
        // rethrow: a Write that never invoked the builder would also not throw.
        Assert.True(built);
        Assert.True(EventLogWriter.EventLogUnavailable);
    }
}
