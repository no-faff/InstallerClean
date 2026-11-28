using System.Security.AccessControl;
using System.Security.Principal;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using Microsoft.Win32;
using WatchedKey = InstallerClean.Services.RecordsWatchFactory.WatchedKey;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The real <see cref="RecordsWatchFactory"/> against the real registry. Each test
/// watches a scratch key under HKCU that it creates under its own name and deletes
/// afterwards, through the factory's test constructor, which also takes the list of
/// loaded hives from the test, so nothing here turns on what the PC running it has
/// installed or which accounts are signed in. One test arms the app's own list of keys,
/// and needs administrator rights for that, as the app has.
/// </summary>
public sealed class RecordsWatchTests : IDisposable
{
    // One level under Software, so the key Dispose deletes is the only one a test creates.
    private readonly string _keyPath =
        @"Software\InstallerClean-test-RecordsWatch-" + Guid.NewGuid().ToString("N");

    public void Dispose() =>
        Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);

    private string Under(string name) => _keyPath + @"\" + name;

    private static IRecordsWatch Arm(params WatchedKey[] keys) =>
        new RecordsWatchFactory(_ => keys, () => ["S-1-5-18"]).Arm();

    private static WatchedKey Subtree(string path) =>
        new(RegistryHive.CurrentUser, path, Subtree: true, ParentWhereAbsent: false);

    private static WatchedKey NamesOnly(string path) =>
        new(RegistryHive.CurrentUser, path, Subtree: false, ParentWhereAbsent: false);

    private void Create(string path)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path);
        Assert.NotNull(key);
    }

    private static void SetValue(string path, string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(path, writable: true);
        Assert.NotNull(key);
        key.SetValue(name, 1, RegistryValueKind.DWord);
    }

    [Fact]
    public void A_watch_nothing_has_written_under_reads_unchanged_every_time()
    {
        Create(Under("Records"));

        using var watch = Arm(Subtree(_keyPath));

        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());
        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());
    }

    [Fact]
    public void A_value_written_below_a_watched_key_reads_changed_and_stays_changed()
    {
        Create(Under(@"Records\Deeper"));
        using var watch = Arm(Subtree(_keyPath));
        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());

        SetValue(Under(@"Records\Deeper"), "State");

        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    [Fact]
    public void A_key_made_below_a_watched_key_reads_changed()
    {
        Create(Under("Records"));
        using var watch = Arm(Subtree(_keyPath));

        Create(Under(@"Records\Added"));

        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    [Fact]
    public void A_key_deleted_below_a_watched_key_reads_changed()
    {
        Create(Under(@"Records\Going"));
        using var watch = Arm(Subtree(_keyPath));

        Registry.CurrentUser.DeleteSubKeyTree(Under(@"Records\Going"));

        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    /// <summary>
    /// A key watched for its subkeys' names alone, as each account's Software\Microsoft
    /// is, says nothing of a value written inside a subkey and does say that a subkey was
    /// made.
    /// </summary>
    [Fact]
    public void A_key_watched_for_names_alone_sees_a_subkey_made_and_not_a_value_written_inside_one()
    {
        Create(Under("Existing"));
        using var watch = Arm(NamesOnly(_keyPath));

        SetValue(Under("Existing"), "State");
        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());

        Create(Under("Installer"));
        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    [Fact]
    public void A_key_that_is_not_there_is_watched_through_the_key_above_it_where_that_is_asked_for()
    {
        Create(_keyPath);
        using var watch = Arm(new WatchedKey(
            RegistryHive.CurrentUser, Under("Installer"), Subtree: true, ParentWhereAbsent: true));
        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());

        Create(Under("Installer"));

        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    [Fact]
    public void A_key_that_is_not_there_with_no_key_above_it_leaves_the_watch_unestablished()
    {
        Create(Under("Present"));
        using var watch = Arm(
            Subtree(Under("Present")),
            new WatchedKey(RegistryHive.CurrentUser, Under(@"Missing\Installer"), Subtree: true, ParentWhereAbsent: true));

        Assert.Equal(RecordsWatchReading.Unestablished, watch.Read());
    }

    /// <summary>
    /// A key that is not there and asks for nothing in its place is left out, and the
    /// other keys are watched: each account's Installer key is given that way, the key
    /// above it being watched in its own right.
    /// </summary>
    [Fact]
    public void A_key_that_is_not_there_and_asks_for_nothing_in_its_place_is_left_out()
    {
        Create(Under("Present"));
        using var watch = Arm(Subtree(Under("Absent")), Subtree(Under("Present")));
        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());

        SetValue(Under("Present"), "State");

        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    [Fact]
    public void A_watched_key_that_is_itself_deleted_reads_changed()
    {
        Create(Under("Records"));
        using var watch = Arm(Subtree(Under("Records")));

        Registry.CurrentUser.DeleteSubKeyTree(Under("Records"));

        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    /// <summary>
    /// A hive loaded or unloaded since the watch was armed is under no watched key, so a
    /// read compares the loaded hives with those at arming, and the answer stays.
    /// </summary>
    [Fact]
    public void A_hive_loaded_since_the_watch_was_armed_reads_changed_and_stays_changed()
    {
        Create(Under("Records"));
        string[] loaded = ["S-1-5-18"];
        using var watch = new RecordsWatchFactory(_ => [Subtree(_keyPath)], () => loaded).Arm();
        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());

        loaded = ["S-1-5-18", "S-1-5-21-1-2-3-1001"];
        Assert.Equal(RecordsWatchReading.Changed, watch.Read());

        loaded = ["S-1-5-18"];
        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    [Fact]
    public void A_list_of_loaded_hives_that_will_not_read_leaves_the_watch_unestablished()
    {
        Create(Under("Records"));

        using var watch = new RecordsWatchFactory(_ => [Subtree(_keyPath)], () => null).Arm();

        Assert.Equal(RecordsWatchReading.Unestablished, watch.Read());
    }

    [Fact]
    public void A_watch_on_no_key_is_unestablished()
    {
        using var watch = Arm();

        Assert.Equal(RecordsWatchReading.Unestablished, watch.Read());
    }

    /// <summary>
    /// The watch is armed on one thread and read on others for the length of a batch, so
    /// a change made after the arming thread has ended is still reported.
    /// </summary>
    [Fact]
    public void A_watch_armed_on_a_thread_that_has_ended_still_reports_a_change()
    {
        Create(Under("Records"));
        IRecordsWatch? armed = null;
        var arming = new Thread(() => armed = Arm(Subtree(_keyPath)));
        arming.Start();
        Assert.True(arming.Join(TimeSpan.FromSeconds(30)), "the arming thread never finished");
        using var watch = armed!;
        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());

        SetValue(Under("Records"), "State");

        Assert.Equal(RecordsWatchReading.Changed, watch.Read());
    }

    /// <summary>
    /// A key that refuses either right the watch opens it with, the right to be told of
    /// a change or the right to be asked a question, leaves the watch unestablished. The
    /// refusal is made with a deny entry for this account on a key of the test's own, and
    /// a plain open asking for that right is shown to be refused first, so a set-up that
    /// did not take fails there.
    /// </summary>
    [Theory]
    [InlineData(RegistryRights.Notify)]
    [InlineData(RegistryRights.QueryValues)]
    public void A_key_that_refuses_a_right_the_watch_needs_leaves_it_unestablished(RegistryRights refused)
    {
        Create(Under("Records"));
        var account = WindowsIdentity.GetCurrent().User!;
        var deny = new RegistryAccessRule(account, refused, AccessControlType.Deny);
        using (var key = Registry.CurrentUser.OpenSubKey(
                   Under("Records"), RegistryKeyPermissionCheck.ReadWriteSubTree,
                   RegistryRights.ChangePermissions | RegistryRights.ReadPermissions))
        {
            Assert.NotNull(key);
            var security = key.GetAccessControl(AccessControlSections.Access);
            security.AddAccessRule(deny);
            key.SetAccessControl(security);
        }

        try
        {
            RegistryKey? plain = null;
            var refusal = Record.Exception(() => plain = Registry.CurrentUser.OpenSubKey(
                Under("Records"), RegistryKeyPermissionCheck.Default, refused));
            plain?.Dispose();
            Assert.Null(plain);
            Assert.NotNull(refusal);

            // Beside a key that is watched, so the refusal alone is what leaves the watch
            // unestablished.
            Create(Under("Present"));
            using var watch = Arm(Subtree(Under("Present")), Subtree(Under("Records")));

            Assert.Equal(RecordsWatchReading.Unestablished, watch.Read());
        }
        finally
        {
            // The entry comes off again so the key can be deleted with the others.
            using var key = Registry.CurrentUser.OpenSubKey(
                Under("Records"), RegistryKeyPermissionCheck.ReadWriteSubTree,
                RegistryRights.ChangePermissions | RegistryRights.ReadPermissions);
            if (key is not null)
            {
                var security = key.GetAccessControl(AccessControlSections.Access);
                security.RemoveAccessRule(deny);
                key.SetAccessControl(security);
            }
        }
    }

    [Fact]
    public void A_watch_that_has_been_disposed_of_reads_unestablished_and_takes_a_second_dispose()
    {
        Create(Under("Records"));
        var watch = Arm(Subtree(_keyPath));
        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());

        watch.Dispose();
        watch.Dispose();

        Assert.Equal(RecordsWatchReading.Unestablished, watch.Read());
    }

    /// <summary>
    /// The app's own keys, armed on the PC running the test: every one opens with both
    /// rights and takes the notification, so the watch is whole. It is read once, and
    /// whether anything was written under those keys meanwhile is not this test's to say.
    /// </summary>
    [AdministratorFact]
    public void The_apps_own_keys_arm_on_this_pc()
    {
        using var watch = new RecordsWatchFactory().Arm();

        Assert.NotEqual(RecordsWatchReading.Unestablished, watch.Read());
    }

    /// <summary>
    /// The app's own keys, armed, around the app's own take and put-back of a file in the
    /// real Installer folder: the watch reads unchanged, at once and a second later. A Move
    /// or Delete reads the watch after it takes each file, and a watch its own rename
    /// changed would have every batch check everything again after its first file.
    ///
    /// The file is one the test writes there, under a name no cached file has and with an
    /// extension the app never lists. Something else on the PC can write under the watched
    /// keys while the test runs, so it tries again with a new watch, and one clean try is
    /// the whole of the claim: the rename itself wrote nothing there.
    /// </summary>
    [AdministratorFact]
    public void The_apps_own_take_and_put_back_in_the_real_Installer_folder_leaves_the_apps_own_watch_unchanged()
    {
        const int Tries = 4;
        var installer = InstallerCacheHelpers.InstallerFolder;
        var name = "InstallerClean-test-" + Guid.NewGuid().ToString("N") + ".tmp";
        var path = Path.Combine(installer, name);
        var hand = new HandleInHandFolder();
        string? batchFolder = null;
        File.WriteAllBytes(path, [1, 2, 3]);
        try
        {
            var readings = new List<string>();
            for (var attempt = 1; attempt <= Tries; attempt++)
            {
                using var watch = new RecordsWatchFactory().Arm();
                var armed = watch.Read();

                using (var batch = hand.OpenBatch(installer, out var code))
                {
                    Assert.True(batch is not null, $"the batch's folder could not be made in {installer}: {code}");
                    batchFolder = batch.Folder;
                    var hold = batch.Hold(path);
                    Assert.Equal(HoldOutcome.Held, hold.Outcome);
                    using var held = hold.File!;
                    Assert.True(held.TakeOut(out var takeCode), $"the take was refused: {takeCode}");
                    Assert.False(File.Exists(path));
                    Assert.True(held.PutBack(out var backCode), $"the put-back was refused: {backCode}");
                }

                var atOnce = watch.Read();
                Thread.Sleep(1000);
                var later = watch.Read();
                readings.Add($"try {attempt}: {armed} when armed, {atOnce} at once, {later} a second later");

                if (armed == RecordsWatchReading.Unchanged
                    && atOnce == RecordsWatchReading.Unchanged
                    && later == RecordsWatchReading.Unchanged)
                    return;
            }

            Assert.Fail("The watch never read unchanged across a take and a put-back. " + string.Join("; ", readings));
        }
        finally
        {
            // Wherever a failure left the file: in its place, or in the batch's folder,
            // which is gone where the batch ended with nothing in it.
            File.Delete(path);
            if (batchFolder is not null && Directory.Exists(batchFolder))
                File.Delete(Path.Combine(batchFolder, name));
        }
    }
}
