using System.Security.Cryptography.X509Certificates;
using InstallerClean.Interop;
using InstallerClean.Interop.Native;
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
            var error = Msi.MsiGetSummaryInformation(
                IntPtr.Zero, filePath, 0, out hSummary);

            if (error != MsiError.Success)
                return null;

            var title    = GetStringProperty(hSummary, MsiSummaryProperty.Title);
            var subject  = GetStringProperty(hSummary, MsiSummaryProperty.Subject);
            var author   = GetStringProperty(hSummary, MsiSummaryProperty.Author);
            var comments = GetStringProperty(hSummary, MsiSummaryProperty.Comments);
            var keywords = GetStringProperty(hSummary, MsiSummaryProperty.Keywords);
            var appName  = GetStringProperty(hSummary, MsiSummaryProperty.AppName);

            // Signature retrieval can fail independently of the summary
            // properties, so capture it separately to avoid losing them.
            var sig = GetDigitalSignature(filePath);

            return new MsiSummaryInfo(title, subject, author, comments, sig, keywords, appName);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (hSummary != IntPtr.Zero)
                Msi.MsiCloseHandle(hSummary);
        }
    }

    private static string GetStringProperty(IntPtr hSummary, uint propertyId)
    {
        uint bufferLen = 0;

        var error = Msi.MsiSummaryInfoGetProperty(
            hSummary, propertyId,
            out var dataType, out _, IntPtr.Zero,
            null, ref bufferLen);

        // MoreData is the first-call "tell me the buffer size" code.
        if (error != MsiError.Success && error != MsiError.MoreData)
            return string.Empty;

        if (dataType != VtType.String || bufferLen == 0)
            return string.Empty;

        bufferLen++; // null terminator
        var buffer = new char[bufferLen];

        error = Msi.MsiSummaryInfoGetProperty(
            hSummary, propertyId,
            out dataType, out _, IntPtr.Zero,
            buffer, ref bufferLen);

        return error == MsiError.Success
            ? new string(buffer, 0, (int)bufferLen)
            : string.Empty;
    }

    // Returns the certificate Subject string only. The signature chain is
    // NOT validated and the file's hash is NOT verified against the cert.
    // Treat this purely as descriptive metadata, not a trust indicator;
    // the matching UI label is "Signing certificate", not "Verified by".
    private static string GetDigitalSignature(string filePath)
    {
        try
        {
            using var cert = new X509Certificate2(
                X509Certificate.CreateFromSignedFile(filePath));
            // Format() respects RFC-4514 escapes; naive String.Split(',')
            // would corrupt subjects like CN="Acme, Inc.", O=Acme.
            return cert.SubjectName.Format(multiLine: true).TrimEnd('\r', '\n');
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
