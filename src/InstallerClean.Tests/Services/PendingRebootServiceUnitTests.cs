using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>Unit tests for PendingRebootService: each branch of Check() exercised against fakes.</summary>
public class PendingRebootServiceUnitTests
{
    private readonly IRegistryReader _registry = Substitute.For<IRegistryReader>();
    private readonly IMutexProbe _mutex = Substitute.For<IMutexProbe>();

    /// <summary>Builds a service with a fixed Windows root so path comparisons don't depend on the host.</summary>
    private PendingRebootService Build(string windowsRoot = @"C:\Windows") =>
        new(_registry, _mutex, windowsRoot);

    // Positive: must Block.

    [Fact]
    public void Mutex_held_blocks()
    {
        _mutex.IsHeld(PendingRebootService.MsiExecuteMutexName).Returns(true);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.MsiExecuteMutexHeld, result.Reason);
    }

    [Fact]
    public void InProgress_key_exists_blocks()
    {
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(true);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
    }

    [Fact]
    public void Rename_targets_cache_blocks()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Windows\Installer\1234.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Multiple_renames_blocks_on_first_cache_match()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
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
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\\?\C:\Windows\Installer\1234.msi", "" });

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
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Users\elsewhere.tmp", @"!\??\C:\Windows\Installer\1234.msi" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Rename_targets_per_product_folder_blocks()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
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
        _registry.LocalMachineKeyExists(oldKey).Returns(true);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        _registry.DidNotReceive().LocalMachineKeyExists(oldKey);
    }

    [Fact]
    public void Pending_renames_not_in_cache_returns_clean()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
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
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
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
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
            {
                @"\??\C:\Windows\InstallerExtra\foo.dll", "",
            });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Empty_pending_file_renames_returns_clean()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(Array.Empty<string>());

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Missing_pending_file_renames_value_returns_clean()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns((string[]?)null);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Missing_in_progress_key_returns_clean()
    {
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(false);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    // Edge: must not throw, must fail open.

    [Fact]
    public void Registry_read_throws_fails_open_returns_clean()
    {
        _registry.LocalMachineKeyExists(Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));
        _registry.LocalMachineMultiStringValue(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Mutex_probe_throws_fails_open_continues_to_other_checks()
    {
        _mutex.IsHeld(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("transient"));
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(true);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
    }

    [Fact]
    public void Mutex_probe_access_denied_fails_open_continues_to_other_checks()
    {
        _mutex.IsHeld(Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
    }

    [Fact]
    public void Malformed_pending_rename_does_not_crash()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
            {
                "this is not a path",
                "",
                "corrupted text",
                "",
            });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Wrong_value_type_on_pending_file_renames_returns_clean()
    {
        // Production RegistryReader returns null when GetValue surfaces a non-string[]
        // via `as string[]`. Simulate the contract here.
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns((string[]?)null);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Windows_on_different_drive_still_detects_cache_rename()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\D:\Windows\Installer\foo.msi", "" });

        var result = Build(windowsRoot: @"D:\Windows").Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"D:\Windows\Installer\foo.msi", result.Detail);
    }

    // Ordering: signal precedence.

    [Fact]
    public void Mutex_wins_over_in_progress_and_pending_rename()
    {
        _mutex.IsHeld(PendingRebootService.MsiExecuteMutexName).Returns(true);
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(true);
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.MsiExecuteMutexHeld, result.Reason);
        _registry.DidNotReceive().LocalMachineKeyExists(Arg.Any<string>());
        _registry.DidNotReceive().LocalMachineMultiStringValue(
            Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void In_progress_wins_over_pending_rename()
    {
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(true);
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
        _registry.DidNotReceive().LocalMachineMultiStringValue(
            Arg.Any<string>(), Arg.Any<string>());
    }
}
