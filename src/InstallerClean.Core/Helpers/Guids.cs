using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// New GUIDs, made by the call <see cref="Guid.NewGuid"/> makes on Windows, CoCreateGuid,
/// asked through this assembly's own ole32 import, which loads from System32.
/// </summary>
/// <remarks>
/// Where CoCreateGuid returns anything but zero, an <see cref="Exception"/> carrying its
/// HRESULT is thrown, which is what .NET 10's Guid.NewGuid throws for the same answer: a
/// plain Exception, never a type derived from it, with .NET 10's English message. .NET
/// Framework's Guid.NewGuid throws the exception Marshal.ThrowExceptionForHR maps the
/// HRESULT to instead.
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
            throw CarryingHResult(hr);
        return guid;
    }

    /// <summary>
    /// A plain <see cref="Exception"/> whose HResult is <paramref name="hr"/>. .NET Framework
    /// gives HResult a protected setter, which code outside a derived class reaches only by
    /// reflection. The message is given, because .NET Framework's default text for an
    /// Exception follows an installed language pack and .NET 10's is English.
    /// </summary>
    private static Exception CarryingHResult(int hr)
    {
        var exception = new Exception("Exception of type 'System.Exception' was thrown.");
        typeof(Exception).GetProperty(nameof(Exception.HResult))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(exception, [hr]);
        return exception;
    }
}
