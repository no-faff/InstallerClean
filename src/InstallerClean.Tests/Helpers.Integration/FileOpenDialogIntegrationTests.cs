using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// The folder picker's COM declarations, called on the shell's real Common Item Dialog
/// without showing it, and the wrapping its driver and the link launcher rest on. A method
/// declared in the wrong slot calls a different method of the object, which crashes or
/// answers something else, so where the dialog can read a value back, a test sets it
/// through one method and reads it back through another. The title cannot be read back,
/// and Show and GetResult need a dialog the user closes, so they are not called here.
/// Windows only: the dialog is the shell's.
/// </summary>
public class FileOpenDialogIntegrationTests
{
    /// <summary>
    /// A wrapper made by ComObjects keeps the object alive once Wrap has released the
    /// caller's reference, and disposing of it gives back every reference the wrapper
    /// holds, so the count ends one lower than it started, the one being the caller's.
    /// The test holds a second reference throughout, so every count is read from an object
    /// that is still alive. The wrapper may hold more than one of the object's interfaces,
    /// so while it lives the count is required to be at least what it was, not exactly
    /// one more.
    /// </summary>
    [Fact]
    public void A_wrapper_keeps_the_object_alive_and_gives_back_all_it_holds_when_released()
    {
        OnSta(() =>
        {
            var dialog = CreateDialog();
            Marshal.AddRef(dialog);
            try
            {
                var before = ReferenceCount(dialog);
                int during;
                using (var com = new ComObjects())
                {
                    var wrapper = (IFileDialog)com.Wrap(dialog);
                    during = ReferenceCount(dialog);
                    Assert.Equal(0, wrapper.SetOptions(FolderPicker.Options));
                }
                var after = ReferenceCount(dialog);

                Assert.True(during >= before,
                    $"before wrapping {before}, wrapped {during}: the wrapper holds no reference once the caller's is released");
                Assert.True(after == before - 1,
                    $"before wrapping {before}, wrapped {during}, released {after}: the count did not end one lower, the caller's reference");
            }
            finally
            {
                Marshal.Release(dialog);
            }
        });
    }

    [Fact]
    public void The_options_set_are_the_options_read_back()
    {
        OnDialog(dialog =>
        {
            Assert.Equal(0, dialog.SetOptions(FolderPicker.Options));
            Assert.Equal(0, dialog.GetOptions(out var options));

            Assert.True((options & FolderPicker.Options) == FolderPicker.Options, $"options read back 0x{options:X8}");
        });
    }

    /// <summary>
    /// The folder set before the dialog is shown is the folder GetFolder hands back, and
    /// its display name is the folder's path. The folder is the test's own, whose path is
    /// already in its long form.
    /// </summary>
    [Fact]
    public void The_start_folder_set_is_the_folder_read_back_by_its_path()
    {
        var folder = Folder();
        OnDialog(dialog =>
        {
            var item = ItemFor(folder);
            try
            {
                Assert.Equal(0, dialog.SetDefaultFolder(item));
                Assert.Equal(0, dialog.SetFolder(item));
            }
            finally
            {
                Marshal.Release(item);
            }

            Assert.Equal(0, dialog.GetFolder(out var pFolder));
            Assert.Equal(folder, DisplayName(pFolder), StringComparer.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void The_file_name_set_is_the_file_name_read_back()
    {
        OnDialog(dialog =>
        {
            var name = Bstrs.FromString("a name\u00e9");
            try
            {
                Assert.Equal(0, dialog.SetFileName(name));
            }
            finally
            {
                Bstrs.Free(name);
            }

            Assert.Equal(0, dialog.GetFileName(out var pName));
            try
            {
                Assert.Equal("a name\u00e9", Marshal.PtrToStringUni(pName));
            }
            finally
            {
                Ole32.CoTaskMemFree(pName);
            }
        });
    }

    [Fact]
    public void The_title_is_accepted()
    {
        OnDialog(dialog =>
        {
            var title = Bstrs.FromString("A title");
            try
            {
                Assert.Equal(0, dialog.SetTitle(title));
            }
            finally
            {
                Bstrs.Free(title);
            }
        });
    }

    // The test's own folder, without the separator a display name does not end in.
    private static string Folder() =>
        Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);

    private static IntPtr CreateDialog()
    {
        Assert.Equal(0, Ole32.CoCreateInstance(
            in FileOpenDialogNative.CLSID_FileOpenDialog, IntPtr.Zero,
            FileOpenDialogNative.CLSCTX_INPROC_SERVER,
            in FileOpenDialogNative.IID_IFileDialog, out var dialog));
        Assert.NotEqual(IntPtr.Zero, dialog);
        return dialog;
    }

    private static IntPtr ItemFor(string folder)
    {
        Assert.Equal(0, Shell32.SHCreateItemFromParsingName(
            folder, IntPtr.Zero, in FileOpenDialogNative.IID_IShellItem, out var item));
        Assert.NotEqual(IntPtr.Zero, item);
        return item;
    }

    // The display name of a shell item the caller holds one reference to, which is
    // released here.
    private static string DisplayName(IntPtr pItem)
    {
        using var com = new ComObjects();
        var item = (IShellItem)com.Wrap(pItem);
        Assert.Equal(0, item.GetDisplayName(FileOpenDialogNative.SIGDN_DESKTOPABSOLUTEPARSING, out var pName));
        try
        {
            return Marshal.PtrToStringUni(pName)!;
        }
        finally
        {
            Ole32.CoTaskMemFree(pName);
        }
    }

    // AddRef and Release each return the count they leave, a value Microsoft describes
    // as meant for tests. On one thread and an in-process object, the pair reads the
    // count as it is.
    private static int ReferenceCount(IntPtr pointer)
    {
        Marshal.AddRef(pointer);
        return Marshal.Release(pointer);
    }

    private static void OnDialog(Action<IFileDialog> body) =>
        OnSta(() =>
        {
            using var com = new ComObjects();
            body((IFileDialog)com.Wrap(CreateDialog()));
        });

    // The dialog is an apartment-threaded object, and xUnit's threads are not STA.
    private static void OnSta(Action body)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var co = Ole32.CoInitializeEx(IntPtr.Zero, ShellDispatchNative.COINIT_APARTMENTTHREADED);
            try
            {
                body();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                if (co >= 0) Ole32.CoUninitialize();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
