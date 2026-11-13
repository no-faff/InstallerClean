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
        // Refuse symlinks at the API entry. The scan filters reparse
        // points out of the cache enumeration before reaching here,
        // but this is a public service surface: the boundary check
        // defends in depth against any caller that hasn't filtered
        // the path itself.
        if (Helpers.StorageHelpers.IsReparsePoint(filePath))
            return null;

        uint hSummary = 0;
        try
        {
            var error = Msi.MsiGetSummaryInformation(
                hDatabase: 0, filePath, 0, out hSummary);

            if (error != MsiError.Success)
                return null;

            var title    = GetStringProperty(hSummary, MsiSummaryProperty.Title);
            var subject  = GetStringProperty(hSummary, MsiSummaryProperty.Subject);
            var author   = GetStringProperty(hSummary, MsiSummaryProperty.Author);
            var comments = GetStringProperty(hSummary, MsiSummaryProperty.Comments);
            var keywords = GetStringProperty(hSummary, MsiSummaryProperty.Keywords);
            var appName  = GetStringProperty(hSummary, MsiSummaryProperty.AppName);

            var sig = GetDigitalSignature(filePath);

            return new MsiSummaryInfo(title, subject, author, comments, sig, keywords, appName);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (hSummary != 0)
                Msi.MsiCloseHandle(hSummary);
        }
    }

    private static string GetStringProperty(uint hSummary, uint propertyId)
    {
        uint bufferLen = 0;

        var error = Msi.MsiSummaryInfoGetProperty(
            hSummary, propertyId,
            out var dataType, out _, out _,
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
            out dataType, out _, out _,
            buffer, ref bufferLen);

        // Defensive clamp: a successful MsiSummaryInfoGetProperty
        // returns bufferLen as the count excluding the terminator and
        // never larger than the input. Math.Min bounds an unbounded
        // read even if the API ever violates that contract.
        return error == MsiError.Success
            ? new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length))
            : string.Empty;
    }

    // Returns the certificate Subject string only. The signature
    // chain is NOT validated and the file's hash is NOT verified
    // against the cert; this is descriptive metadata, not a trust
    // indicator. The matching UI label is "Signing certificate",
    // not "Verified by".
    private static string GetDigitalSignature(string filePath)
    {
        try
        {
            // CreateFromSignedFile is the BCL's managed Authenticode
            // cert-extraction entry point: it wraps Win32
            // CryptQueryObject + CryptMsgGetParam + a cert-store
            // lookup for the signer. A file with no signature, or one
            // it cannot read, throws, and the catch below leaves the
            // signing-cert field empty.
            //
            // Both certs are explicitly disposed: CreateFromSignedFile
            // returns a fresh cert handle, and the X509Certificate2
            // constructor duplicates that handle into a new instance,
            // so without the inner Dispose the original handle leaks
            // until finalisation.
            using var inner = X509Certificate.CreateFromSignedFile(filePath);
            using var cert = new X509Certificate2(inner);
            // Format() respects RFC-4514 escapes; a naive String.Split(',')
            // would corrupt subjects like CN="Acme, Inc.", O=Acme.
            return cert.SubjectName.Format(multiLine: true).TrimEnd('\r', '\n');
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
