using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IFileIdentityReader"/>: opens the path and asks the
/// filesystem which file the handle landed on.
///
/// IT USES THE REAL FILESYSTEM AND TAKES NO <c>IFileSystem</c>, in company with the
/// two containment guards and for a related but not identical reason. Theirs is
/// that a fake must not be able to make an out-of-bounds path look safe. This
/// one's is simpler: a file identity is a fact about a volume, and there is no
/// abstraction over it to inject. What a fake in a test can do here is hold a
/// scan's whole offer back, which is the direction <see cref="IFileIdentityReader"/>
/// says every fault in this class runs in.
/// </summary>
internal sealed class FileIdentityReader : IFileIdentityReader
{
    /// <inheritdoc />
    public FileIdentityRead ReadOutcome(string path, out FileIdentity identity)
    {
        identity = default;
        if (string.IsNullOrEmpty(path)) return FileIdentityRead.NotAPath;

        try
        {
            // No access bits at all, matching InstallerCacheHelpers.TryResolveFinalPath:
            // the metadata this asks for needs none, and requesting read access
            // would fail on a file another process has open without sharing it,
            // which for this folder means anything msiexec is working on.
            //
            // FILE_SHARE_ALL for the other half of that: an installer holding its
            // own cached package must not make this call fail, because a failure
            // here is a withholding given up and now costs the scan its whole
            // walk-derived offer.
            //
            // NO FILE_FLAG_OPEN_REPARSE_POINT, which is the whole point: links are
            // followed so a registration that reaches its package through a
            // junction resolves to the package. FILE_FLAG_BACKUP_SEMANTICS is
            // present so a path that turns out to name a directory opens rather
            // than failing; such a path yields an identity that matches no
            // candidate file and is therefore harmless.
            using var handle = Kernel32.CreateFile(
                path,
                0,
                Kernel32.FILE_SHARE_ALL,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                Kernel32.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                // Read before anything else can overwrite it, and read ONLY on the
                // failing branch: the value after a successful call is whatever the
                // last failure anywhere left behind.
                //
                // THE TWO ABSENCE CODES ARE THE WHOLE OF THE EXEMPTION AND
                // EVERYTHING ELSE WITHHOLDS. A registration whose cached file has
                // gone is ordinary, and on a large share of machines it is common,
                // so treating it as a give-up would empty those machines' offers
                // over a file that was not there to be claimed. Any other code
                // means something IS there and this call did not get to it, which
                // is the case the app cannot be sure about.
                var error = Marshal.GetLastWin32Error();
                return error is Kernel32.ERROR_FILE_NOT_FOUND or Kernel32.ERROR_PATH_NOT_FOUND
                    ? FileIdentityRead.NamesNothing
                    : FileIdentityRead.OpenRefused;
            }

            if (!Kernel32.GetFileInformationByHandleEx(
                    handle,
                    Kernel32.FileIdInfo,
                    out var info,
                    (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<Kernel32.FILE_ID_INFO>()))
            {
                // A volume or a Windows build that will not answer this class.
                // There is no weaker call to fall back to: the 64-bit index from
                // GetFileInformationByHandle can collide on ReFS, and a comparison
                // that can collide would claim two files are one, which is the one
                // error direction that costs a file rather than an offer.
                return FileIdentityRead.IdentityUnavailable;
            }

            identity = new FileIdentity(
                info.VolumeSerialNumber, info.FileIdLow, info.FileIdHigh);
            return FileIdentityRead.Read;
        }
        catch
        {
            // Kept apart from the answers above rather than folded into one of
            // them: this is the call failing to complete, and a machine reporting
            // it is reporting something different from a machine whose handles are
            // refused. Both withhold.
            return FileIdentityRead.Faulted;
        }
    }
}
