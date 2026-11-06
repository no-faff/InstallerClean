using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// New GUIDs, made by the call <see cref="Guid.NewGuid"/> makes on Windows, CoCreateGuid,
/// asked through this assembly's own ole32 import, which loads from System32.
/// </summary>
/// <remarks>
/// Where CoCreateGuid returns anything but zero, an <see cref="Exception"/> carrying its
/// HRESULT is thrown, which is what <see cref="Guid.NewGuid"/> throws for the same answer.
/// </remarks>
internal static class Guids
{
    /// <summary>Asks Windows for a new GUID: the HRESULT, and the GUID.</summary>
    internal delegate int Create(out Guid guid);

    /// <summary>A new GUID from Windows.</summary>
    internal static Guid NewGuid() => NewGuid(Ole32.CoCreateGuid);

    /// <summary>Test seam: the same, with the call to Windows supplied by the caller.</summary>
    internal static Guid NewGuid(Create create)
    {
        var hr = create(out var guid);
        if (hr != 0)
            throw new Exception { HResult = hr };
        return guid;
    }
}
