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
/// abstraction over it to inject. The safety argument is different in kind and is
/// in <see cref="IFileIdentityReader"/>: a wrong answer here can only ever cost an
/// offer, never a file, so a fake in a test cannot make anything unsafe look safe.
/// </summary>
internal sealed class FileIdentityReader : IFileIdentityReader
{
    /// <inheritdoc />
    public bool TryRead(string path, out FileIdentity identity)
    {
        identity = default;
        if (string.IsNullOrEmpty(path)) return false;

        try
        {
            // No access bits at all, matching InstallerCacheHelpers.TryResolveFinalPath:
            // the metadata this asks for needs none, and requesting read access
            // would fail on a file another process has open without sharing it,
            // which for this folder means anything msiexec is working on.
            //
            // FILE_SHARE_ALL for the other half of that: an installer holding its
            // own cached package must not make this call fail, because a failure
            // here silently gives up a withholding.
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

            if (handle.IsInvalid) return false;

            if (!Kernel32.GetFileInformationByHandleEx(
                    handle,
                    Kernel32.FileIdInfo,
                    out var info,
                    (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<Kernel32.FILE_ID_INFO>()))
            {
                // A volume or a Windows build that will not answer this class
                // leaves the caller where it was. There is no weaker call to fall
                // back to: the 64-bit index from GetFileInformationByHandle can
                // collide on ReFS, and a comparison that can collide would claim
                // two files are one, which is the one error direction that costs a
                // file rather than an offer.
                return false;
            }

            identity = new FileIdentity(
                info.VolumeSerialNumber, info.FileIdLow, info.FileIdHigh);
            return true;
        }
        catch
        {
            // Every failure is the same answer. The path is one the app is about to
            // decide the fate of, so anything unexpected here has to leave the
            // decision exactly where it was rather than move it.
            return false;
        }
    }
}
