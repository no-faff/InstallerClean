using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The folder picker's options and the thread it runs on.
/// </summary>
public class FolderPickerTests
{
    // FOS_ALLOWMULTISELECT (shobjidl_core.h), which the picker never sets.
    private const uint FOS_ALLOWMULTISELECT = 0x00000200;

    /// <summary>
    /// The dialog leaves the elevated process's working directory where it is, offers
    /// folders, hands back only a file system item, and lets one item be chosen, which is
    /// what GetResult reads.
    /// </summary>
    [Fact]
    public void The_options_keep_the_working_directory_and_allow_one_folder()
    {
        Assert.Equal(FileOpenDialogNative.FOS_NOCHANGEDIR, FolderPicker.Options & FileOpenDialogNative.FOS_NOCHANGEDIR);
        Assert.Equal(FileOpenDialogNative.FOS_PICKFOLDERS, FolderPicker.Options & FileOpenDialogNative.FOS_PICKFOLDERS);
        Assert.Equal(FileOpenDialogNative.FOS_FORCEFILESYSTEM, FolderPicker.Options & FileOpenDialogNative.FOS_FORCEFILESYSTEM);
        Assert.Equal(0u, FolderPicker.Options & FOS_ALLOWMULTISELECT);
    }

    /// <summary>
    /// The folder picker is shown from an STA thread only, and refuses any other before it
    /// makes the dialog, so nothing is created on a thread that cannot run it.
    /// </summary>
    [Fact]
    public void A_thread_that_is_not_STA_is_refused_before_the_dialog_is_made()
    {
        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                FolderPicker.Pick("A title", null, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();

        Assert.IsType<InvalidOperationException>(thrown);
    }
}
