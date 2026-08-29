using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// The production <see cref="IVolumeMountProbe"/>: a thin pass-through to the
/// Kernel32 volume and device calls, carrying the double-call buffer pattern and
/// the multi-string decoding those calls use and nothing else. Every decision
/// about what an answer means belongs to the caller.
/// </summary>
/// <remarks>
/// Each method turns a Win32 failure into a null or a
/// <see cref="VolumeMountPoints.NoAnswer"/> and never throws, so the gate above
/// keeps its own "never throws" contract without wrapping every call. That is
/// the same shape <see cref="RegistryReader"/> takes.
/// </remarks>
internal sealed class VolumeMountProbe : IVolumeMountProbe
{
    /// <summary>
    /// A volume GUID path is a fixed 49 characters plus its terminator, and
    /// Win32's own sample sizes this buffer at MAX_PATH. Taking the larger of
    /// the two costs nothing and leaves no arithmetic to get wrong.
    /// </summary>
    private const int VolumeNameBufferLength = 260;

    /// <summary>
    /// Opening size for the two multi-string outputs. Both grow on demand: the
    /// mount-point query reports the size it wants, and the device query is
    /// retried at successively larger sizes because it reports only that the
    /// buffer was too small.
    /// </summary>
    private const int MultiStringBufferLength = 1024;

    /// <summary>
    /// The ceiling on those retries. A device name's target is a short string
    /// and a volume's mount points are a handful of paths, so reaching this
    /// means the answer is not the shape this reader is for, and growing
    /// without a bound would turn that into an allocation loop.
    /// </summary>
    private const int MaxMultiStringBufferLength = 1 << 20;

    public VolumeMountPoints MountPointsFor(string volumeGuidPath)
    {
        var length = MultiStringBufferLength;

        while (true)
        {
            var buffer = new char[length];
            if (Kernel32.GetVolumePathNamesForVolumeName(
                    volumeGuidPath, buffer, (uint)buffer.Length, out var required))
            {
                return VolumeMountPoints.Answer(ReadMultiString(buffer));
            }

            // ERROR_MORE_DATA is the first half of the documented double call and
            // says how much room the answer needs; anything else is a failure and
            // the caller has to be able to tell the two apart.
            if (Marshal.GetLastWin32Error() != Kernel32.ERROR_MORE_DATA)
                return VolumeMountPoints.NoAnswer;

            if (required <= length || required > MaxMultiStringBufferLength)
                return VolumeMountPoints.NoAnswer;

            length = (int)required;
        }
    }

    public IReadOnlyList<string>? VolumeGuidPaths()
    {
        var buffer = new char[VolumeNameBufferLength];
        var handle = Kernel32.FindFirstVolume(buffer, (uint)buffer.Length);
        if (handle == InvalidHandle)
            return null;

        try
        {
            var volumes = new List<string> { ReadFirstString(buffer) };

            while (true)
            {
                buffer = new char[VolumeNameBufferLength];
                if (Kernel32.FindNextVolume(handle, buffer, (uint)buffer.Length))
                {
                    volumes.Add(ReadFirstString(buffer));
                    continue;
                }

                // The end of the list and a real failure both arrive as false, and
                // only the error code separates them. A partial list handed back as
                // though it were the whole one would let a caller conclude that no
                // volume matches when the walk simply stopped early.
                return Marshal.GetLastWin32Error() == Kernel32.ERROR_NO_MORE_FILES
                    ? volumes
                    : null;
            }
        }
        finally
        {
            Kernel32.FindVolumeClose(handle);
        }
    }

    public string? DosDeviceTarget(string dosDeviceName)
    {
        var length = MultiStringBufferLength;

        while (true)
        {
            var buffer = new char[length];
            if (Kernel32.QueryDosDevice(dosDeviceName, buffer, (uint)buffer.Length) != 0)
            {
                var target = ReadFirstString(buffer);
                return target.Length == 0 ? null : target;
            }

            // This call reports only that the buffer was too small, never how
            // large it should have been, so the retry doubles rather than sizing
            // itself from the answer.
            if (Marshal.GetLastWin32Error() != ErrorInsufficientBuffer)
                return null;

            length *= 2;
            if (length > MaxMultiStringBufferLength)
                return null;
        }
    }

    /// <summary>FindFirstVolume's failure return.</summary>
    private static readonly IntPtr InvalidHandle = new(-1);

    /// <summary>What QueryDosDevice reports when the buffer was too small.</summary>
    private const int ErrorInsufficientBuffer = 122;

    /// <summary>
    /// Every entry in a double-null-terminated character buffer. Stops at the
    /// empty string that ends the list, so trailing buffer contents are not read
    /// as entries.
    /// </summary>
    private static List<string> ReadMultiString(char[] buffer)
    {
        var entries = new List<string>();
        var start = 0;

        while (start < buffer.Length)
        {
            var end = Array.IndexOf(buffer, '\0', start);
            if (end < 0) break;
            if (end == start) break;

            entries.Add(new string(buffer, start, end - start));
            start = end + 1;
        }

        return entries;
    }

    /// <summary>
    /// The first entry of a double-null-terminated buffer, empty where there is
    /// none. Used where the answer is one string but the API still writes it in
    /// the list form.
    /// </summary>
    private static string ReadFirstString(char[] buffer)
    {
        var end = Array.IndexOf(buffer, '\0');
        return end <= 0 ? string.Empty : new string(buffer, 0, end);
    }
}
