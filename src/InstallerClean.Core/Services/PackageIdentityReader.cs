using InstallerClean.Interop;
using InstallerClean.Interop.Native;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IPackageIdentityReader"/>: opens the cached package
/// through msi.dll and reads what it declares itself to be.
///
/// The two file kinds are read by different routes because they are different
/// files. An installation package keeps its ProductCode in the Property table, so
/// the database has to be opened and queried. A patch has no Property table at
/// all, and keeps both halves of what is needed in its summary-information
/// stream, which this app already reads for the detail panels.
///
/// EVERY FAILURE RETURNS NULL AND NULL WITHHOLDS A FILE, which is why nothing
/// here is tolerant. A reading that guessed, or that accepted a value it could
/// not prove was a GUID, would hand the veto a code Windows has never heard of;
/// Windows would answer that it does not know it, and the file would be offered
/// on the strength of a question that was never really asked. Refusing to answer
/// costs a withholding. Answering wrongly costs a file.
/// </summary>
public sealed class PackageIdentityReader : IPackageIdentityReader
{
    /// <summary>
    /// The canonical braced GUID surface form is 38 characters:
    /// <c>{8-4-4-4-12}</c>. Both codes read here are fixed at that width, and the
    /// patch reading depends on it (see <see cref="MsiSummaryProperty.RevisionNumber"/>).
    /// </summary>
    private const int BracedGuidLength = 38;

    /// <summary>
    /// The ProductCode row of the Property table, which
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/msi/productcode">Microsoft
    /// documents as REQUIRED</see>. Required is what makes its absence a signal
    /// worth withholding on rather than an ordinary state to shrug at.
    ///
    /// Backticks are the MSI SQL identifier quote. They are needed on
    /// <c>Property</c> in both positions: it is the table's name and also a column
    /// name inside it, and the grammar accepts neither unquoted.
    /// </summary>
    private const string ProductCodeQuery =
        "SELECT `Value` FROM `Property` WHERE `Property` = 'ProductCode'";

    /// <inheritdoc />
    public PackageIdentity? Read(string filePath, bool isPatch, out string detail) =>
        isPatch ? ReadPatch(filePath, out detail) : ReadProduct(filePath, out detail);

    /// <summary>
    /// An installation package's ProductCode, out of its own Property table.
    ///
    /// The database is opened read-only, which neither writes to the file nor
    /// takes a copy of it. That matters more here than it would elsewhere: the
    /// file being opened is one the app is about to offer to delete, on a machine
    /// where the same folder is what Windows Installer itself works from.
    /// </summary>
    private static PackageIdentity? ReadProduct(string filePath, out string detail)
    {
        uint hDatabase = 0;
        uint hView = 0;
        uint hRecord = 0;
        try
        {
            var error = Msi.MsiOpenDatabase(filePath, MsiDbOpen.ReadOnly, out hDatabase);
            if (error != MsiError.Success)
            {
                detail = $"package would not open as a database ({error})";
                return null;
            }

            // A database with no Property table fails HERE rather than at fetch
            // time, so this arm covers both "not the shape of an installation
            // package" and "a query this build of msi.dll would not prepare".
            error = Msi.MsiDatabaseOpenView(hDatabase, ProductCodeQuery, out hView);
            if (error != MsiError.Success)
            {
                detail = $"Property table query would not open ({error})";
                return null;
            }

            error = Msi.MsiViewExecute(hView, 0);
            if (error != MsiError.Success)
            {
                detail = $"Property table query would not execute ({error})";
                return null;
            }

            error = Msi.MsiViewFetch(hView, out hRecord);
            if (error != MsiError.Success)
            {
                // NoMoreItems is an empty result set: the table is there and
                // carries no ProductCode row. Documented as required, so this is
                // a malformed package rather than a package that happens not to
                // say.
                detail = error == MsiError.NoMoreItems
                    ? "package declares no ProductCode"
                    : $"Property table row would not fetch ({error})";
                return null;
            }

            if (!TryRecordString(hRecord, field: 1, out var raw))
            {
                detail = "ProductCode value would not read";
                return null;
            }

            var code = Canonicalise(raw);
            if (code is null)
            {
                detail = "ProductCode is not a well-formed GUID";
                return null;
            }

            detail = string.Empty;
            return new PackageIdentity(code, IsPatch: false, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            // The P/Invoke layer itself, rather than a return code: a path msi.dll
            // rejects before it has a return code to give, or a marshalling
            // failure. Named by type only, for the same reason nothing else here
            // records a message.
            detail = $"package read raised {ex.GetType().Name}";
            return null;
        }
        finally
        {
            // Reverse order, and each guarded on its own: a view outlives the row
            // it fetched and a database outlives both, so closing the database
            // first would leave the other two pointing at something gone.
            if (hRecord != 0) Msi.MsiCloseHandle(hRecord);
            if (hView != 0) Msi.MsiCloseHandle(hView);
            if (hDatabase != 0) Msi.MsiCloseHandle(hDatabase);
        }
    }

    /// <summary>
    /// A patch's own code and the products it declares it may be applied to, both
    /// out of its summary-information stream.
    ///
    /// The patch code alone is not enough and that is not a shortcoming of this
    /// reader. <c>MsiGetPatchInfoEx</c> takes a product code as a required
    /// parameter, so there is no way to ask Windows about a patch except through
    /// a product that might hold it, and the Template is where the file says which
    /// products those are. A patch that names none is one there is no way to ask
    /// about at all, so it is reported unread rather than returned as an identity
    /// whose question cannot be put.
    /// </summary>
    private static PackageIdentity? ReadPatch(string filePath, out string detail)
    {
        uint hSummary = 0;
        try
        {
            var error = Msi.MsiGetSummaryInformation(
                hDatabase: 0, filePath, 0, out hSummary);
            if (error != MsiError.Success)
            {
                detail = $"patch summary stream would not open ({error})";
                return null;
            }

            if (!TrySummaryString(hSummary, MsiSummaryProperty.RevisionNumber, out var revision))
            {
                detail = "patch declares no revision number";
                return null;
            }

            // Fixed-width, and the length test is what keeps it honest. The value
            // is a run of braced GUIDs with no separator, so the patch code is the
            // first 38 characters and only if the whole value divides by 38. A
            // value that does not is one this reading does not hold for, and
            // taking its first 38 characters anyway would manufacture a code.
            if (revision.Length < BracedGuidLength || revision.Length % BracedGuidLength != 0)
            {
                detail = "patch revision number is not a whole number of GUIDs";
                return null;
            }

            var code = Canonicalise(revision[..BracedGuidLength]);
            if (code is null)
            {
                detail = "patch code is not a well-formed GUID";
                return null;
            }

            if (!TrySummaryString(hSummary, MsiSummaryProperty.Template, out var template))
            {
                detail = "patch declares no target products";
                return null;
            }

            var targets = ParseTargets(template);
            if (targets is null)
            {
                detail = "patch target list is not a list of GUIDs";
                return null;
            }

            if (targets.Count == 0)
            {
                // A present but empty Template. Whether Windows reads that as
                // "any product" is not something this project has established,
                // and the difference does not change the outcome: with no product
                // named there is nothing to ask through either way.
                detail = "patch names no target product";
                return null;
            }

            detail = string.Empty;
            return new PackageIdentity(code, IsPatch: true, targets);
        }
        catch (Exception ex)
        {
            detail = $"patch read raised {ex.GetType().Name}";
            return null;
        }
        finally
        {
            if (hSummary != 0) Msi.MsiCloseHandle(hSummary);
        }
    }

    /// <summary>
    /// Splits a patch Template into product codes, or null where any part of it
    /// is not one.
    ///
    /// Empty parts are skipped rather than refused: a trailing separator is
    /// ordinary and carries no product, so it is nothing to fail over. A part
    /// that is present and is not a GUID is refused, because the value then means
    /// something this reading does not understand and the safe response to that
    /// is to stop.
    /// </summary>
    internal static IReadOnlyList<string>? ParseTargets(string template)
    {
        var targets = new List<string>();
        foreach (var part in template.Split(';'))
        {
            if (part.Length == 0) continue;
            var code = Canonicalise(part);
            if (code is null) return null;
            targets.Add(code);
        }
        return targets;
    }

    /// <summary>
    /// One code in the single spelling the rest of the app uses, or null where the
    /// value is not a braced GUID at all.
    ///
    /// Canonicalising rather than passing the value through is what lets a code
    /// key a cache: two packages of the same product declare the same code and
    /// must produce the same string, and nothing obliges a package author to have
    /// written it in any particular case. Upper case because that is the form both
    /// the API and the registry hand back, so a value here and a value in a log
    /// beside it can be compared by eye.
    /// </summary>
    internal static string? Canonicalise(string value)
    {
        // The width test is not belt and braces over TryParseExact; it is doing
        // work TryParseExact does not. That method TRIMS leading and trailing
        // white space before parsing, measured rather than assumed, so without
        // this a padded value is accepted and silently becomes the trimmed code.
        // Whether Windows Installer registers such a product under the padded
        // form or the trimmed one is not established, and the direction of the
        // guess matters: guess wrong and the veto asks about a code the machine
        // does not hold, Windows says it has never heard of it, and the file is
        // offered on the strength of a question that was never really asked.
        // Refusing the value costs a withholding, which costs nothing.
        if (value.Length != BracedGuidLength) return null;

        return Guid.TryParseExact(value, "B", out var guid)
            ? guid.ToString("B").ToUpperInvariant()
            : null;
    }

    /// <summary>
    /// One field of a fetched record, through the double-call buffer pattern.
    /// False for anything other than a value that was read in full.
    /// </summary>
    private static bool TryRecordString(uint hRecord, uint field, out string value)
    {
        value = string.Empty;
        uint bufferLen = 0;

        var error = Msi.MsiRecordGetString(hRecord, field, null, ref bufferLen);
        if (error != MsiError.Success && error != MsiError.MoreData) return false;
        if (bufferLen == 0) return false;

        bufferLen++; // space for the null terminator
        var buffer = new char[bufferLen];
        error = Msi.MsiRecordGetString(hRecord, field, buffer, ref bufferLen);
        if (error != MsiError.Success) return false;

        // Defensive clamp, as everywhere else this pattern is used: a successful
        // call returns the count excluding the terminator and never larger than
        // the input, and bounding it here means an API that broke that contract
        // could not reach the managed string constructor with an unbounded read.
        value = new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length));
        return true;
    }

    /// <summary>
    /// One summary-stream property as text. False for a property that is absent,
    /// stored as something other than text, or could not be read: the caller
    /// treats all three the same way and none of them is a value.
    ///
    /// A real receiver is passed for the FILETIME out-parameter rather than null,
    /// for the reason the P/Invoke declaration gives: the API writes through that
    /// pointer whenever the stored type is VT_FILETIME, which a malformed file can
    /// declare in a slot being read for text.
    /// </summary>
    private static bool TrySummaryString(uint hSummary, uint propertyId, out string value)
    {
        value = string.Empty;
        uint bufferLen = 0;

        var error = Msi.MsiSummaryInfoGetProperty(
            hSummary, propertyId, out var dataType, out _, out _, null, ref bufferLen);
        if (error != MsiError.Success && error != MsiError.MoreData) return false;
        if (dataType != VtType.String || bufferLen == 0) return false;

        bufferLen++; // space for the null terminator
        var buffer = new char[bufferLen];
        error = Msi.MsiSummaryInfoGetProperty(
            hSummary, propertyId, out dataType, out _, out _, buffer, ref bufferLen);
        if (error != MsiError.Success) return false;

        value = new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length));
        return true;
    }
}
