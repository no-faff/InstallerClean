using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>Unit tests for PendingRebootService: each branch of Check() exercised against fakes.</summary>
public class PendingRebootServiceUnitTests
{
    private readonly IRegistryReader _registry = Substitute.For<IRegistryReader>();
    private readonly IMutexProbe _mutex = Substitute.For<IMutexProbe>();
    private readonly IVolumeMountProbe _volumes = Substitute.For<IVolumeMountProbe>();
    private readonly IInstallerInProgressMarker _marker = Substitute.For<IInstallerInProgressMarker>();

    /// <summary>
    /// The quiet machine every test starts from, stated rather than left to the
    /// fakes: nothing holding the installer mutex, nothing suspended, no in-progress
    /// file and nothing queued. The in-progress file's own zero reads as present, so
    /// it is stated too. Both registry reads answer their type's own zero when nobody sets
    /// one, and that zero says the read did not answer, which is a refusal rather
    /// than a clean machine. A test about any of the three names it and overwrites
    /// what is set here.
    /// </summary>
    public PendingRebootServiceUnitTests()
    {
        _mutex.Sample(Arg.Any<string>()).Returns(MutexSample.NotHeld);
        InProgress(RegistryKeyPresence.Absent);
        _marker.Read().Returns(InstallerInProgressMarkerReading.Absent);
        Renames(new RegistryMultiStringRead(RegistryMultiStringState.Absent));
    }

    /// <summary>Builds a service with a fixed Windows root so path comparisons don't depend on the host.</summary>
    private PendingRebootService Build(string windowsRoot = @"C:\Windows") =>
        new(_registry, _mutex, _volumes, _marker, windowsRoot);

    /// <summary>
    /// Scripts one volume: the GUID name the enumeration hands back, the NT device it
    /// is a link to, and where it is mounted. The device name is passed trimmed the way
    /// the service trims it, so a change to that trimming shows up here rather than
    /// leaving every volume silently unmatched.
    /// </summary>
    private void Volume(string guid, string device, params string[] mountedAt)
    {
        var guidPath = $@"\\?\Volume{{{guid}}}\";
        _volumes.VolumeGuidPaths().Returns(new[] { guidPath });
        _volumes.DosDeviceTarget($"Volume{{{guid}}}").Returns(device);
        _volumes.MountPointsFor(guidPath).Returns(VolumeMountPoints.Answer(mountedAt));
    }

    /// <summary>The value Session Manager holds, as the registry hands it over.</summary>
    private void Queued(params string[] entries) =>
        Renames(new RegistryMultiStringRead(RegistryMultiStringState.Read, entries));

    /// <summary>
    /// How the PendingFileRenameOperations read turns out, for the cases that are
    /// about the read rather than about the entries. Every test names one: an
    /// unscripted fake answers the type's own zero, which is Unreadable, so a test
    /// that says nothing about this read is a test about a machine that refused it.
    /// </summary>
    private void Renames(RegistryMultiStringRead read) =>
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(read);

    /// <summary>
    /// What the InProgress key read answers. Absent is the quiet machine and is what
    /// most of these tests want; it is stated rather than left to the fake, for the
    /// reason above.
    /// </summary>
    private void InProgress(RegistryKeyPresence presence) =>
        _registry.LocalMachineKeyPresence(PendingRebootService.InstallerInProgressKey)
            .Returns(presence);

    // Positive: must Block.

    [Fact]
    public void Mutex_held_blocks()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns(MutexSample.Held);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.MsiExecuteMutexHeld, result.Reason);
    }

    /// <summary>
    /// A mutex whose security refuses the probe blocks under a reason of its own and
    /// never as held: nothing has been seen holding it, and the held reason's banner
    /// and command-line sentence both say something is using Windows Installer.
    /// </summary>
    [Fact]
    public void Mutex_access_refused_blocks_with_its_own_reason()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns(MutexSample.AccessRefused);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.MsiExecuteMutexAccessRefused, result.Reason);
        Assert.Null(result.Detail);
    }

    [Fact]
    public void InProgress_key_exists_blocks()
    {
        InProgress(RegistryKeyPresence.Present);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
    }

    // ---- Windows Installer's in-progress file ----

    [Fact]
    public void The_in_progress_file_blocks_with_its_own_reason()
    {
        _marker.Read().Returns(InstallerInProgressMarkerReading.Present);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.InstallerInProgressMarker, result.Reason);
        Assert.Null(result.Detail);
    }

    [Fact]
    public void A_refused_read_of_the_in_progress_file_blocks_with_its_own_reason()
    {
        // Never as the file being there: nothing has been seen, and the file's own
        // sentence says Windows Installer has something in progress.
        _marker.Read().Returns(InstallerInProgressMarkerReading.AccessRefused);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.InstallerInProgressMarkerAccessRefused, result.Reason);
    }

    [Fact]
    public void An_in_progress_file_reading_nobody_handles_blocks()
    {
        // A value the enum does not name, which is what a reading added later without a
        // rule of its own arrives as.
        _marker.Read().Returns((InstallerInProgressMarkerReading)99);

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.InstallerInProgressMarker, result.Reason);
    }

    [Fact]
    public void Only_an_absent_in_progress_file_lets_the_check_carry_on()
    {
        // Walked over the enum against a machine that is otherwise quiet, so a
        // reading that lets the check through is named here or fails.
        var through = Enum.GetValues<InstallerInProgressMarkerReading>()
            .Where(reading =>
            {
                _marker.Read().Returns(reading);
                return !Build().Check().IsBlocked;
            })
            .ToArray();

        Assert.Equal(new[] { InstallerInProgressMarkerReading.Absent }, through);
    }

    [Fact]
    public void A_read_of_the_in_progress_file_that_throws_leaves_the_check()
    {
        // The probe throws only for a failure none of its readings names. No
        // sentence of this gate is true of it, so it is not caught here: the
        // caller's own error path reports it and acts on nothing.
        _marker.Read().Returns(_ => throw new IOException("device not ready"));

        Assert.Throws<IOException>(() => Build().Check());
    }

    [Fact]
    public void A_held_mutex_is_reported_without_reading_the_in_progress_file()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns(MutexSample.Held);
        _marker.Read().Returns(_ => throw new InvalidOperationException("read after the mutex fired"));

        Assert.Equal(PendingRebootReason.MsiExecuteMutexHeld, Build().Check().Reason);
    }

    [Fact]
    public void A_suspended_transaction_is_reported_without_reading_the_in_progress_file()
    {
        InProgress(RegistryKeyPresence.Present);
        _marker.Read().Returns(_ => throw new InvalidOperationException("read after the key fired"));

        Assert.Equal(PendingRebootReason.InstallerInProgress, Build().Check().Reason);
    }

    [Fact]
    public void The_in_progress_file_is_reported_before_the_rename_queue_is_read()
    {
        _marker.Read().Returns(InstallerInProgressMarkerReading.Present);
        Queued(new[] { @"\??\C:\Windows\Installer\1234.msi", "" });

        Assert.Equal(PendingRebootReason.InstallerInProgressMarker, Build().Check().Reason);
    }

    [Fact]
    public void Rename_targets_cache_blocks()
    {
        Queued(new[] { @"\??\C:\Windows\Installer\1234.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Multiple_renames_blocks_on_first_cache_match()
    {
        Queued(new[]
        {
            @"\??\C:\Users\foo.tmp", "",
            @"\??\C:\Windows\Installer\1234.msp", "",
        });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msp", result.Detail);
    }

    [Fact]
    public void Long_path_prefix_still_matches()
    {
        Queued(new[] { @"\\?\C:\Windows\Installer\1234.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Replace_existing_destination_prefix_still_matches()
    {
        // A destination queued with MOVEFILE_REPLACE_EXISTING carries a
        // leading '!' before the NT prefix; the entry still targets the
        // cache and must block.
        Queued(new[] { @"\??\C:\Users\elsewhere.tmp", @"!\??\C:\Windows\Installer\1234.msi" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    /// <summary>
    /// The marker servicing writes in front of the prefix. Every digit is taken because
    /// every digit is what the reader in Chromium's installer takes, and pinning the
    /// range here is what stops it being narrowed later. The Detail assertion is the
    /// load-bearing one: an entry that does not block, or that blocks without naming
    /// its file, is the way this handling goes wrong.
    /// </summary>
    [Theory]
    [InlineData("*0")]
    [InlineData("*1")]
    [InlineData("*2")]
    [InlineData("*3")]
    [InlineData("*4")]
    [InlineData("*5")]
    [InlineData("*6")]
    [InlineData("*7")]
    [InlineData("*8")]
    [InlineData("*9")]
    public void Servicing_marker_before_the_prefix_still_matches(string marker)
    {
        Queued(new[] { $@"{marker}\??\C:\Windows\Installer\1234.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    /// <summary>
    /// The other half of the same handling, and the half that says the marker was read
    /// rather than merely carried through: an entry outside the cache is dismissed.
    /// </summary>
    [Fact]
    public void Servicing_marker_on_an_entry_outside_the_cache_returns_clean()
    {
        Queued(new[] { @"*1\??\C:\Users\foo.tmp", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        Assert.Null(result.Reason);
    }

    /// <summary>
    /// The marker comes off before the prefix decides which arm reads the rest, so it
    /// composes with a volume named rather than lettered exactly as a bare entry does.
    /// A marker and a volume-GUID spelling are not known to arrive together: this pins
    /// what the code does with one, not that the queue writes one.
    /// </summary>
    [Fact]
    public void Servicing_marker_on_a_volume_named_by_guid_that_lands_in_the_cache_blocks()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"*1\??\Volume{aaaaaaaa-1111-2222-3333-444444444444}\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    /// <summary>
    /// Both markers on one entry. The order they come off in is the one that reads
    /// such an entry rather than refusing it.
    /// </summary>
    [Fact]
    public void Servicing_marker_before_a_replace_existing_destination_still_matches()
    {
        Queued(new[] { @"\??\C:\Users\elsewhere.tmp", @"*1!\??\C:\Windows\Installer\1234.msi" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    /// <summary>
    /// The marker is taken off wherever it opens an entry, and not only in front of a
    /// prefix, so a drive-rooted path behind it is read like any other. Nothing is known
    /// to write a marker with no prefix behind it: this pins what the code does with
    /// one, not that the queue writes one.
    /// </summary>
    [Fact]
    public void Servicing_marker_without_the_prefix_still_matches()
    {
        Queued(new[] { @"*1C:\Windows\Installer\1234.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Servicing_marker_without_the_prefix_outside_the_cache_returns_clean()
    {
        Queued(new[] { @"*1C:\Users\foo.tmp", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        Assert.Null(result.Reason);
    }

    /// <summary>
    /// A second digit is not part of the marker, so what is left is not a path anyone
    /// can place and the run is held. Both of these pin the width: what is not read as
    /// a marker stays where an unplaceable entry stays.
    /// </summary>
    [Fact]
    public void A_marker_carrying_more_than_one_digit_blocks()
    {
        Queued(new[] { @"*12\??\C:\Users\foo.tmp", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    [Fact]
    public void An_asterisk_with_no_digit_behind_it_blocks()
    {
        Queued(new[] { @"*\??\C:\Users\foo.tmp", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    [Fact]
    public void Rename_targets_per_product_folder_blocks()
    {
        Queued(new[]
        {
            @"\??\C:\Windows\Installer\{12345678-1234-1234-1234-123456789abc}\foo.dll", "",
        });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
    }

    // Negative: must Clean.

    [Fact]
    public void All_signals_clean_returns_clean()
    {
        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        Assert.Null(result.Reason);
    }

    /// <summary>Pin: the legacy broad signals are never queried by Check(). Even with a fake forcing them true, the verdict is Clean.</summary>
    [Theory]
    [InlineData(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")]
    [InlineData(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending")]
    [InlineData(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\PostRebootReporting")]
    public void Legacy_broad_signals_are_never_queried(string oldKey)
    {
        _registry.LocalMachineKeyPresence(oldKey).Returns(RegistryKeyPresence.Present);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        _registry.DidNotReceive().LocalMachineKeyPresence(oldKey);
    }

    [Fact]
    public void Pending_renames_not_in_cache_returns_clean()
    {
        Queued(new[]
        {
            @"\??\C:\Users\foo.tmp", "",
            @"\??\C:\ProgramData\Vendor\update.dat", @"\??\C:\ProgramData\Vendor\update.dat.bak",
        });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    /// <summary>
    /// A poisoned entry that lexically starts with the cache prefix but resolves
    /// outside it (via \..\ traversal) must canonicalise before the boundary check.
    /// Without GetFullPath, the literal C:\Windows\Installer\..\..\Users\Other\secret
    /// would StartsWith-match the prefix and surface the user-profile path through
    /// the Detail field on the way to the CLI / event log.
    /// </summary>
    [Fact]
    public void Pending_rename_with_traversal_outside_cache_returns_clean()
    {
        Queued(new[]
        {
            @"\??\C:\Windows\Installer\..\..\Users\Other\secret.txt", "",
        });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        Assert.Null(result.Reason);
    }

    /// <summary>
    /// The boundary check rejects sibling folders that share the cache's name as a prefix.
    /// Without the trailing-separator anchor, C:\Windows\InstallerExtra would match
    /// C:\Windows\Installer.
    /// </summary>
    [Fact]
    public void Pending_rename_in_sibling_folder_returns_clean()
    {
        Queued(new[]
        {
            @"\??\C:\Windows\InstallerExtra\foo.dll", "",
        });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    /// <summary>
    /// An entry carrying an NT prefix over something that names no volume this can
    /// place. Nothing establishes that such a value is not a form the app does not
    /// understand, so it is a queued operation whose target is unknown rather than one
    /// that can be dismissed, and the gate says so.
    ///
    /// THE FIXTURE PUTS THE CACHE WHERE Path.GetFullPath WOULD COMPLETE THE VALUE TO,
    /// which is what makes this a test rather than a restatement: the Windows root is
    /// the working directory, so an entry naming "Installer" would complete to the very
    /// folder the comparison is looking for. That is why Detail is asserted null. A
    /// comparison reading a completed value would answer about wherever the app was
    /// started from and hand that path to the user as the queued rename it found. The
    /// entry carries no separator of its own so that the completion lands on the folder
    /// itself.
    /// </summary>
    [Fact]
    public void Pending_rename_naming_no_volume_this_can_place_blocks()
    {
        Queued(@"\??\Installer", "");

        var result = Build(windowsRoot: Directory.GetCurrentDirectory()).Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
        Assert.Null(result.Detail);
    }

    [Fact]
    public void Empty_pending_file_renames_returns_clean()
    {
        Queued(Array.Empty<string>());

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Missing_pending_file_renames_value_returns_clean()
    {
        Renames(new RegistryMultiStringRead(RegistryMultiStringState.Absent));

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Missing_in_progress_key_returns_clean()
    {
        InProgress(RegistryKeyPresence.Absent);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    // Edge: must not throw, and a read that did not answer must not pass as quiet.

    /// <summary>
    /// A reader that throws rather than reporting, which the interface says will not
    /// happen and which Check wraps anyway. The wrap answers the same state the
    /// reader's own failure return does, so the gate refuses and names the read.
    /// </summary>
    [Fact]
    public void Registry_read_that_throws_blocks_on_the_unreadable_check()
    {
        _registry.LocalMachineKeyPresence(Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));
        _registry.LocalMachineMultiStringValue(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.RegistryCheckUnreadable, result.Reason);
    }

    /// <summary>
    /// The InProgress key on its own: refused, so whether a transaction is suspended
    /// is not established and the pass never reaches the queued renames.
    /// </summary>
    [Fact]
    public void In_progress_key_that_would_not_read_blocks()
    {
        InProgress(RegistryKeyPresence.Unreadable);
        Queued(@"\??\C:\Users\elsewhere.tmp", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.RegistryCheckUnreadable, result.Reason);
    }

    /// <summary>
    /// The queued-renames value on its own: refused, so whether anything is queued
    /// against the cache is not established. The InProgress key answers absent here,
    /// which is what separates this from the test above it.
    /// </summary>
    [Fact]
    public void Pending_file_renames_value_that_would_not_read_blocks()
    {
        Renames(new RegistryMultiStringRead(RegistryMultiStringState.Unreadable));

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.RegistryCheckUnreadable, result.Reason);
    }

    /// <summary>
    /// A sample that did not establish the mutex's state blocks the check on its own,
    /// where every other signal reads clear.
    /// </summary>
    [Fact]
    public void An_unestablished_mutex_sample_blocks_a_quiet_machine()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns(MutexSample.Unestablished);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.MsiExecuteMutexUnreadable, result.Reason);
    }

    /// <summary>
    /// The same where the rename queue holds entries and none of them names the cache,
    /// the other way the check reaches its last answer.
    /// </summary>
    [Fact]
    public void An_unestablished_mutex_sample_blocks_when_every_queued_rename_lands_elsewhere()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns(MutexSample.Unestablished);
        Queued(new[] { @"\??\C:\Temp\a.tmp", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.MsiExecuteMutexUnreadable, result.Reason);
    }

    /// <summary>
    /// A probe that throws has not sampled anything, and Check reads the throw as the
    /// probe's own failure return: the check blocks on a quiet machine.
    /// </summary>
    [Fact]
    public void A_mutex_probe_that_throws_blocks_a_quiet_machine()
    {
        _mutex.Sample(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("transient"));

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.MsiExecuteMutexUnreadable, result.Reason);
    }

    /// <summary>
    /// A signal that blocks with a reason of its own is reported over a mutex that was
    /// not sampled, since it names what is happening.
    /// </summary>
    [Fact]
    public void A_mutex_probe_that_throws_still_reports_a_suspended_transaction()
    {
        _mutex.Sample(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("transient"));
        InProgress(RegistryKeyPresence.Present);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
    }

    [Fact]
    public void An_unestablished_sample_carries_its_Windows_error_to_the_block()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName)
            .Returns(new MutexSampling(MutexSample.Unestablished, 8));

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.MsiExecuteMutexUnreadable, result.Reason);
        Assert.Equal(8, result.WindowsError);
    }

    [Fact]
    public void A_probe_that_throws_for_a_Windows_error_carries_its_number_and_one_that_does_not_carries_none()
    {
        _mutex.Sample(Arg.Any<string>())
            .Returns(_ => throw new IOException("any text", unchecked((int)0x80070008)));
        Assert.Equal(8, Build().Check().WindowsError);

        _mutex.Sample(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("transient"));
        Assert.Null(Build().Check().WindowsError);
    }

    [Fact]
    public void Only_the_unsampled_lock_carries_a_Windows_error()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName)
            .Returns(new MutexSampling(MutexSample.Unestablished, 8));
        InProgress(RegistryKeyPresence.Present);

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
        Assert.Null(result.WindowsError);
    }

    /// <summary>
    /// A sample answer added to the enum later blocks, as an unestablished one does,
    /// until somebody gives it an arm of its own.
    /// </summary>
    [Fact]
    public void A_mutex_sample_nobody_handles_blocks()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns((MutexSample)99);

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.MsiExecuteMutexUnreadable, result.Reason);
    }

    /// <summary>
    /// The probe's contract answers a refused open with
    /// <see cref="MutexSample.AccessRefused"/> rather than throwing, which the test
    /// above for that answer covers. A probe that throws the refusal instead has not
    /// answered at all, so the signals after it are still read and one that blocks
    /// names the reason.
    /// </summary>
    [Fact]
    public void A_mutex_probe_throwing_access_denied_still_reports_a_rename_into_the_cache()
    {
        _mutex.Sample(Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));
        Queued(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
    }

    /// <summary>
    /// A value that is not a path at all still describes a queued file operation, and
    /// the app cannot say what it names. It reaches the same verdict as an exotic
    /// spelling, and deliberately: no rule tells the two apart that is not a guess about
    /// what the object manager might mean by a string, and the nearest neighbour of
    /// "corrupted text" is a bare "\Windows\Installer\9f05cba.msi", which is the cache
    /// path with its volume missing.
    ///
    /// Not throwing is the other half and is why this test was written. Check answers
    /// with a verdict for every input, and a value nobody can parse is one of them.
    /// </summary>
    [Fact]
    public void Malformed_pending_rename_blocks_rather_than_being_dismissed()
    {
        Queued("this is not a path", "", "corrupted text", "");

        var result = Record.Exception(() => Build().Check());

        Assert.Null(result);

        var verdict = Build().Check();
        Assert.Equal(PendingRebootVerdict.Block, verdict.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, verdict.Reason);
        Assert.Null(verdict.Detail);
    }

    /// <summary>
    /// The nearest neighbour named above, on its own, because it is the member of this
    /// class most likely to be a real cache rename recorded oddly rather than junk.
    /// </summary>
    [Fact]
    public void Pending_rename_naming_the_cache_path_without_its_volume_blocks()
    {
        Queued(@"\Windows\Installer\9f05cba.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    /// <summary>
    /// A value recorded at the name Windows queues renames under, in a type this does
    /// not read. Something is there and its contents cannot be seen, so whether one of
    /// them names the cache is not established and the run is held.
    ///
    /// It is the same standing as an entry the placing pass cannot resolve, one level
    /// up: Malformed_pending_rename_blocks_rather_than_being_dismissed refuses on an
    /// entry that is not a path, and dismissing the value holding it would contradict
    /// that inside one service.
    ///
    /// The fixture states the wrong type rather than the absence the production reader
    /// once reported for it, which is what lets this test fail at what it is named for.
    /// </summary>
    [Fact]
    public void Wrong_value_type_on_pending_file_renames_blocks()
    {
        Renames(new RegistryMultiStringRead(RegistryMultiStringState.WrongType));

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.RegistryCheckUnreadable, result.Reason);
    }

    [Fact]
    public void Windows_on_different_drive_still_detects_cache_rename()
    {
        Queued(new[] { @"\??\D:\Windows\Installer\foo.msi", "" });

        var result = Build(windowsRoot: @"D:\Windows").Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"D:\Windows\Installer\foo.msi", result.Detail);
    }

    // The two spellings that name a volume rather than carrying a drive root.

    [Fact]
    public void Rename_on_a_volume_named_by_guid_that_lands_in_the_cache_blocks()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"\??\Volume{aaaaaaaa-1111-2222-3333-444444444444}\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Rename_on_a_volume_named_by_guid_that_lands_elsewhere_returns_clean()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"\??\Volume{aaaaaaaa-1111-2222-3333-444444444444}\Users\foo.tmp", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    /// <summary>
    /// THE CASE THE HANDLING EXISTS FOR, AND THE ONE A GATE THAT REFUSED ON EXOTIC
    /// SPELLINGS WOULD GET WRONG. Windows names a volume this way when there is no
    /// mount point to name it by, and it does so in its own boot-file servicing, which
    /// is an ordinary state for a machine to be in. A volume mounted nowhere holds
    /// nothing inside the installer cache, because that path reaches its files through
    /// a mount point, so this is a settled answer and not a refusal.
    /// </summary>
    [Fact]
    public void Rename_on_a_volume_mounted_nowhere_returns_clean()
    {
        Volume("bbbbbbbb-1111-2222-3333-444444444444", @"\Device\HarddiskVolume1");
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume1\EFI\Microsoft\Boot\BOOTSTAT.DAT", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Rename_on_a_volume_named_by_device_that_lands_in_the_cache_blocks()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume3\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    /// <summary>
    /// A device name is not always two components, so the match is against the head of
    /// the entry rather than a fixed count: a volume reporting
    /// <c>\Device\Harddisk0\Partition3</c> is matched exactly as one reporting
    /// <c>\Device\HarddiskVolume3</c> is, and what follows it is the path on that volume.
    /// </summary>
    [Fact]
    public void A_device_name_of_more_than_two_components_is_matched_whole()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\Harddisk0\Partition3", @"C:\");
        Queued(@"\??\GLOBALROOT\Device\Harddisk0\Partition3\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
    }

    // The residue: the app knows something is queued and cannot say where.

    /// <summary>
    /// The mount-point query failing and the volume being mounted nowhere arrive as the
    /// same empty list. This is the first of the pair and its twin is
    /// Rename_on_a_volume_mounted_nowhere_returns_clean; the two fixtures differ only in
    /// whether the query answered, and they must reach opposite verdicts.
    /// </summary>
    [Fact]
    public void Rename_on_a_volume_whose_mount_points_would_not_read_blocks()
    {
        var guidPath = @"\\?\Volume{aaaaaaaa-1111-2222-3333-444444444444}\";
        _volumes.MountPointsFor(guidPath).Returns(VolumeMountPoints.NoAnswer);
        Queued(@"\??\Volume{aaaaaaaa-1111-2222-3333-444444444444}\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
        Assert.Null(result.Detail);
    }

    [Fact]
    public void Rename_on_a_device_no_volume_on_this_machine_claims_blocks()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume9\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    [Fact]
    public void Rename_on_a_device_blocks_when_the_volumes_would_not_enumerate()
    {
        _volumes.VolumeGuidPaths().Returns((IReadOnlyList<string>?)null);
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume1\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    /// <summary>
    /// A volume query that throws is that query failing, so it lands where a failure
    /// lands rather than reaching the caller. Check answers with a verdict for every
    /// input, and this is the one path where a dependency could break that.
    /// </summary>
    [Fact]
    public void A_volume_query_that_throws_blocks_rather_than_propagating()
    {
        _volumes.VolumeGuidPaths().Returns(_ => throw new InvalidOperationException("transient"));
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume1\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    /// <summary>
    /// AN ENTRY THAT NAMES THE CACHE WINS OVER ONE THAT COULD NOT BE PLACED, WHATEVER
    /// ORDER THEY SIT IN. The unplaceable entry is FIRST here deliberately: a pass that
    /// returned on the first thing it could not place would answer with the weaker
    /// verdict and never reach the entry that names a path. The stronger message is
    /// certainly true and it tells the user which file is queued.
    /// </summary>
    [Fact]
    public void An_entry_naming_the_cache_wins_over_one_that_could_not_be_placed()
    {
        Queued("corrupted text", "", @"\??\C:\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    /// <summary>
    /// A network path can never name %SystemRoot%\Installer, which is local on any
    /// machine that can boot, so the UNC form is placed and dismissed rather than
    /// refused. Refusing on it would stop the app for a reason that has nothing to do
    /// with its job.
    /// </summary>
    [Fact]
    public void A_unc_entry_is_placed_and_returns_clean()
    {
        Queued(@"\??\UNC\server\share\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        _volumes.DidNotReceive().VolumeGuidPaths();
    }

    // Ordering: signal precedence.

    [Fact]
    public void Mutex_wins_over_in_progress_and_pending_rename()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns(MutexSample.Held);
        InProgress(RegistryKeyPresence.Present);
        Queued(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.MsiExecuteMutexHeld, result.Reason);
        _registry.DidNotReceive().LocalMachineKeyPresence(Arg.Any<string>());
        _registry.DidNotReceive().LocalMachineMultiStringValue(
            Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>
    /// A refused open is read where a held one is and stops the check there too. The
    /// action services' acquire asks for the same rights and would refuse the batch
    /// whatever the other two signals said, so the refusal is the reason reported.
    /// </summary>
    [Fact]
    public void Mutex_access_refused_wins_over_in_progress_and_pending_rename()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns(MutexSample.AccessRefused);
        InProgress(RegistryKeyPresence.Present);
        Queued(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.MsiExecuteMutexAccessRefused, result.Reason);
        _registry.DidNotReceive().LocalMachineKeyPresence(Arg.Any<string>());
        _registry.DidNotReceive().LocalMachineMultiStringValue(
            Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void In_progress_wins_over_pending_rename()
    {
        InProgress(RegistryKeyPresence.Present);
        Queued(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
        _registry.DidNotReceive().LocalMachineMultiStringValue(
            Arg.Any<string>(), Arg.Any<string>());
    }
}
