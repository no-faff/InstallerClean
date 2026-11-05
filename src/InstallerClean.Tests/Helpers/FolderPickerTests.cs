using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The folder picker is shown from an STA thread only, and refuses any other before it
/// makes the dialog, so nothing is created on a thread that cannot run it.
/// </summary>
public class FolderPickerTests
{
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
