using System.Security.Cryptography.X509Certificates;
using System.Text;
using InstallerClean.Interop;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Fetches optional display metadata for an individual .msi or .msp file.
/// All failure paths here are intentionally silent (return null / empty).
/// This metadata is decorative in the detail panels. A missing or
/// unreadable file should degrade the UI gracefully, not raise errors the
/// user would need to action.
/// </summary>
public sealed class MsiFileInfoService : IMsiFileInfoService
{
    public MsiSummaryInfo? GetSummaryInfo(string filePath)
    {
        IntPtr hSummary = IntPtr.Zero;
        try
        {
            var error = MsiNativeMethods.MsiGetSummaryInformation(
                IntPtr.Zero, filePath, 0, out hSummary);

            if (error != MsiError.Success)
                return null;

            var title    = GetStringProperty(hSummary, MsiSummaryProperty.Title);
            var subject  = GetStringProperty(hSummary, MsiSummaryProperty.Subject);
            var author   = GetStringProperty(hSummary, MsiSummaryProperty.Author);
            var comments = GetStringProperty(hSummary, MsiSummaryProperty.Comments);

            // Signature retrieval can fail independently (locked file etc.).
            // Don't lose the metadata we already have if that happens.
            var sig = GetDigitalSignature(filePath);

            return new MsiSummaryInfo(title, subject, author, comments, sig);
        }
        catch (Exception)
        {
            // MSI handle operations failed. No metadata available at all.
            return null;
        }
        finally
        {
            if (hSummary != IntPtr.Zero)
                MsiNativeMethods.MsiCloseHandle(hSummary);
        }
    }

    private static string GetStringProperty(IntPtr hSummary, uint propertyId)
    {
        uint dataType;
        int intValue;
        uint bufferLen = 0;

        // First call: get required buffer size.
        var error = MsiNativeMethods.MsiSummaryInfoGetProperty(
            hSummary, propertyId,
            out dataType, out intValue, IntPtr.Zero,
            null, ref bufferLen);

        // The first call returns MoreData (234) or Success when there is a value.
        if (error != MsiError.Success && error != MsiError.MoreData)
            return string.Empty;

        if (dataType != VtType.String || bufferLen == 0)
            return string.Empty;

        bufferLen++; // null terminator
        var buffer = new StringBuilder((int)bufferLen);

        error = MsiNativeMethods.MsiSummaryInfoGetProperty(
            hSummary, propertyId,
            out dataType, out intValue, IntPtr.Zero,
            buffer, ref bufferLen);

        return error == MsiError.Success ? buffer.ToString() : string.Empty;
    }

    private static string GetDigitalSignature(string filePath)
    {
        try
        {
            var cert = X509Certificate.CreateFromSignedFile(filePath);
            return cert.Subject;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
