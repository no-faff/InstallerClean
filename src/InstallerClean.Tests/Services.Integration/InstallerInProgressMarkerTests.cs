using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The production <see cref="InstallerInProgressMarker"/> against the real filesystem,
/// pointed at a scratch folder standing in for the Installer folder.
///
/// THE ABSENT AND PRESENT TESTS ARE A PAIR IN ONE FOLDER, so a probe that answered the
/// same thing whatever was there fails one of them.
/// </summary>
public class InstallerInProgressMarkerTests : IDisposable
{
    private readonly string _folder = TestTemp.NewFolder("ic-marker-");


    public void Dispose()
    {
        TestTemp.Delete(_folder);
    }

    private string MarkerPath => Path.Combine(_folder, InstallerInProgressMarker.FileName);

    [Fact]
    public void A_folder_without_the_file_reads_absent_and_the_same_folder_with_it_reads_present()
    {
        var probe = new InstallerInProgressMarker(_folder);

        Assert.Equal(InstallerInProgressMarkerReading.Absent, probe.Read());

        File.WriteAllBytes(MarkerPath, new byte[16]);

        Assert.Equal(InstallerInProgressMarkerReading.Present, probe.Read());
    }

    [Fact]
    public void A_folder_that_is_not_there_reads_absent()
    {
        var probe = new InstallerInProgressMarker(Path.Combine(_folder, "not-there"));

        Assert.Equal(InstallerInProgressMarkerReading.Absent, probe.Read());
    }

    [Fact]
    public void Something_other_than_a_file_at_the_name_reads_present()
    {
        // Whatever is at the name, the name is taken, and that is all that is read.
        Directory.CreateDirectory(MarkerPath);

        Assert.Equal(InstallerInProgressMarkerReading.Present, new InstallerInProgressMarker(_folder).Read());
    }

    [Fact]
    public void A_file_another_handle_holds_without_sharing_reads_present()
    {
        File.WriteAllBytes(MarkerPath, new byte[16]);
        using var held = new FileStream(MarkerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Assert.Equal(InstallerInProgressMarkerReading.Present, new InstallerInProgressMarker(_folder).Read());
    }

    [Fact]
    public void The_name_is_the_one_Windows_Installer_writes()
    {
        Assert.Equal("inprogressinstallinfo.ipi", InstallerInProgressMarker.FileName);
    }
}
