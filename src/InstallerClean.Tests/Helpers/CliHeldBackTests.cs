using InstallerClean.Cli;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The command line's half of the held-back reporting, which until this file
/// existed no test could reach: the suite did not reference that host at all, so
/// its folds, its stopped-batch byte arithmetic and its held-back stdout line
/// were checkable only by hand.
///
/// What makes them worth reaching rather than trusting to the window's own
/// coverage is that the two hosts do NOT share this code. The sentences and the
/// order they come in are Core's now, so the two cannot disagree about the words;
/// everything around them is still written twice, which is exactly the shape that
/// drifts. Three of the command line's paths were missing the fold while all three
/// of the window's had it.
/// </summary>
public class CliHeldBackTests
{
    private static OrphanedFile File(string name, long bytes) =>
        new($@"C:\Windows\Installer\{name}", bytes, false, false, false, Strings.Reason_Orphaned);

    [Fact]
    public void FoldHeldBack_drops_the_held_back_rows_and_keeps_the_rest_in_order()
    {
        var files = new[] { File("a.msi", 1024), File("b.msp", 2048), File("c.msi", 4096) };

        var folded = Program.FoldHeldBack(files, new[] { @"C:\Windows\Installer\b.msp" });

        Assert.Equal(
            new[] { @"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\c.msi" },
            folded.Select(f => f.FullPath));
    }

    [Fact]
    public void FoldHeldBack_matches_a_held_back_path_whatever_its_case()
    {
        // A claim's package path reaches the fold having been normalised the way
        // the registered rows are, and the scan matches paths case-insensitively
        // throughout. A fold that did not would silently keep a file the re-read
        // had just condemned inside the batch's own account of itself.
        var files = new[] { File("a.msi", 1024), File("b.msp", 2048) };

        var folded = Program.FoldHeldBack(files, new[] { @"c:\windows\installer\B.MSP" });

        Assert.Single(folded);
        Assert.Equal(@"C:\Windows\Installer\a.msi", folded[0].FullPath);
    }

    [Fact]
    public void FoldHeldBack_with_nothing_held_back_keeps_the_whole_batch()
    {
        var files = new[] { File("a.msi", 1024), File("b.msp", 2048) };

        var folded = Program.FoldHeldBack(files, Array.Empty<string>());

        Assert.Equal(files.Select(f => f.FullPath), folded.Select(f => f.FullPath));
    }

    [Fact]
    public void CompletedBytes_reads_the_list_positionally_so_the_fold_decides_the_rows()
    {
        // The sharpest fault this surface has had, and the reason the aborted path
        // needs the fold at least as much as the completed one. A stopped batch
        // reports how many files it reached, not which, so the bytes are taken from
        // the first that-many entries of the list it is handed. Hand it an
        // unfolded list and a held-back file sitting inside those positions has
        // its bytes counted as moved while a file that really moved drops off the
        // end: not an inflated total, the wrong rows.
        var held = File("held.msp", 4096);
        var moved = File("moved.msi", 1024);
        var untouched = File("untouched.msi", 2048);
        var unfolded = new[] { held, moved, untouched };
        var noErrors = Array.Empty<FileOperationError>();

        var wrong = Program.CompletedBytes(unfolded, completedCount: 1, noErrors);
        var right = Program.CompletedBytes(
            Program.FoldHeldBack(unfolded, new[] { held.FullPath }), completedCount: 1, noErrors);

        Assert.Equal(4096, wrong);
        Assert.Equal(1024, right);
    }

    [Fact]
    public void CompletedBytes_over_a_folded_list_excludes_the_files_that_errored()
    {
        var held = File("held.msp", 4096);
        var failed = File("failed.msi", 512);
        var moved = File("moved.msi", 1024);
        var untouched = File("untouched.msi", 2048);
        var errors = new FileOperationError[] { new AccessDenied(failed.FullPath) };

        var bytes = Program.CompletedBytes(
            Program.FoldHeldBack(new[] { held, failed, moved, untouched }, new[] { held.FullPath }),
            completedCount: 1, errors);

        // Reached two of the three left in the batch (one moved, one errored), and
        // only the one that moved counts.
        Assert.Equal(1024, bytes);
    }

    [Fact]
    public void ReportHeldBack_prints_the_sentence_at_a_count_of_one()
    {
        var written = CaptureStdout(() =>
            Program.ReportHeldBack(new HeldBackReasons(Reclaimed: 1)));

        Assert.Equal(string.Format(Strings.Completion_HeldBack_Singular, 1), written.TrimEnd());
    }

    [Fact]
    public void ReportHeldBack_prints_ONE_line_for_a_batch_that_met_several_causes()
    {
        // A mixed batch, which is where four sentences used to print. It is one
        // line now and the number on it is the batch total, so stdout and the
        // window's overlay cannot disagree about how many files were kept back.
        var written = CaptureStdout(() =>
            Program.ReportHeldBack(
                new HeldBackReasons(Reclaimed: 2, RecordsChanged: 1, RecordsUnreadable: 1)));

        Assert.Equal(
            new[] { string.Format(Strings.Completion_HeldBack_Plural, 4) },
            written.TrimEnd().Split(Environment.NewLine));
    }

    [Fact]
    public void ReportHeldBack_prints_the_same_line_whichever_cause_a_batch_met()
    {
        // The line names no cause, pinned on this host too because it is the host
        // whose reader is likeliest to be scripting against the output. Two
        // batches of four kept-back files, reached by different causes, one of them
        // the cause that is about the machine rather than about any file.
        var perFile = CaptureStdout(() =>
            Program.ReportHeldBack(new HeldBackReasons(Reclaimed: 3, RecordsUnreadable: 1)));
        var machineWide = CaptureStdout(() =>
            Program.ReportHeldBack(new HeldBackReasons(OwnershipUnestablished: 4)));

        Assert.Equal(perFile, machineWide);
        Assert.Equal(string.Format(Strings.Completion_HeldBack_Plural, 4), perFile.TrimEnd());
    }

    [Fact]
    public void ReportHeldBack_says_nothing_when_nothing_was_held_back()
    {
        // The commonest run by far. A line reporting a count of zero on every
        // clean run is noise in a scheduled task's log.
        var written = CaptureStdout(() =>
            Program.ReportHeldBack(default));

        Assert.Equal(string.Empty, written);
    }

    [Fact]
    public void AbortedMoveEventLogLine_names_the_folder_the_files_are_in()
    {
        // The audit record an RMM keeps, and the half of this fault with no
        // companion warning: the window at least follows its summary with a dialog
        // saying the folder changed, where this line stands alone in the
        // Application channel. It exists to say where the files that left
        // C:\Windows\Installer went, so naming the path the run was given, which is
        // the one path now known NOT to lead there, is the one thing it must not do.
        var files = new[] { File("a.msi", 1024), File("b.msi", 2048) };
        var ex = new MoveAbortedException(
            "swapped", new MoveResult(1, Array.Empty<FileOperationError>()),
            @"E:\where-they-really-went", MoveAbortReason.ResolvesElsewhere);

        //
        // No paired "and not the path it was given" assertion, because there is
        // nothing to assert it against: that path is not a parameter of this
        // method, which is the guarantee itself and a stronger one than a string
        // comparison. A negative over a value the call could never have seen
        // passes whatever the code does.
        var line = Program.AbortedMoveEventLogLine("/m", ex, files.Length, files);

        Assert.Contains(@"E:\where-they-really-went", line);
        // The counts either side of it, so a line that happened to carry the path
        // in some other slot would not pass. Composed rather than written out,
        // because this method is called inside the en-GB scope in production and
        // outside it here: a literal would pin the noun to whatever language the
        // machine running the suite is in.
        Assert.Contains($"1 of 2 {DisplayHelpers.PluraliseFile(2)}", line);
    }

    /// <summary>
    /// Runs <paramref name="action"/> with stdout redirected and returns what it
    /// wrote, putting the console back afterwards whatever happens.
    ///
    /// Console.Out is process-global, so this is only safe because the assembly
    /// disables test parallelisation (AssemblyInfo.cs, for a different reason of
    /// its own). Anything that re-enables it has to give this its own
    /// non-parallel collection or these tests will read another test's output.
    /// </summary>
    private static string CaptureStdout(Action action)
    {
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            action();
        }
        finally
        {
            Console.SetOut(original);
        }
        return buffer.ToString();
    }
}
