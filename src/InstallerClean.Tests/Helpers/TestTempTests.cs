namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The test folders' sweep and delete: a run's folder goes once no live run holds its owner
/// file, a live run's folder stays, and a clean-up that cannot delete fails.
/// </summary>
public sealed class TestTempTests : IDisposable
{
    private readonly string _runs = TestTemp.NewFolder("ic-sweep-");

    public void Dispose() => TestTemp.Delete(_runs);

    [Fact]
    public void A_sweep_deletes_a_run_folder_nobody_holds_and_keeps_one_a_live_run_holds()
    {
        var ended = Path.Combine(_runs, "ended");
        TestTemp.Claim(ended).Dispose();
        File.WriteAllText(Path.Combine(ended, "left.txt"), "x");
        File.SetAttributes(Path.Combine(ended, "left.txt"), FileAttributes.ReadOnly);
        var live = Path.Combine(_runs, "live");

        using (TestTemp.Claim(live))
        {
            // A sweep that tried the live folder's delete would take this file before the held
            // owner file refused it.
            var work = Path.Combine(live, "a-work.txt");
            File.WriteAllText(work, "x");

            TestTemp.Sweep(_runs);

            Assert.False(Directory.Exists(ended), "The ended run's folder was left.");
            Assert.True(File.Exists(work), "The sweep reached into the live run's folder.");
        }
    }

    [Fact]
    public void A_sweep_never_enters_a_link_to_a_folder_outside_the_runs()
    {
        var outside = TestTemp.NewFolder("ic-sweep-outside-");
        try
        {
            var kept = Path.Combine(outside, "kept.txt");
            File.WriteAllText(kept, "x");
            File.SetAttributes(kept, FileAttributes.ReadOnly);
            var ended = Path.Combine(_runs, "ended");
            TestTemp.Claim(ended).Dispose();
            try
            {
                Directory.CreateSymbolicLink(Path.Combine(ended, "link"), outside);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return; // this host cannot create a directory symlink
            }

            TestTemp.Sweep(_runs);

            Assert.False(Directory.Exists(ended));
            Assert.True(File.Exists(kept), "The sweep deleted through the link.");
            Assert.True((File.GetAttributes(kept) & FileAttributes.ReadOnly) != 0,
                "The sweep changed a file through the link.");
        }
        catch (Exception) when (TestTemp.KeepOnFailure(outside)) { }
        finally
        {
            TestTemp.Delete(outside);
        }
    }

    [Fact]
    public void A_delete_refused_throws_and_names_the_folder()
    {
        var folder = TestTemp.NewFolder("ic-held-");
        using (new FileStream(Path.Combine(folder, "held.txt"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            var thrown = Assert.Throws<IOException>(() => TestTemp.Delete(folder));
            Assert.Contains(folder, thrown.Message, StringComparison.Ordinal);
            Assert.NotNull(thrown.InnerException);
        }
        TestTemp.Delete(folder);
        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public void A_delete_of_a_folder_that_is_not_there_does_nothing() =>
        TestTemp.Delete(Path.Combine(_runs, "never-made"));
}
