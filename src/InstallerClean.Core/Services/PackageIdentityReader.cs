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
    public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal) =>
        isPatch ? ReadPatch(filePath, out detail, out refusal) : ReadProduct(filePath, out detail, out refusal);

    /// <summary>
    /// An installation package's ProductCode, out of its own Property table.
    ///
    /// The database is opened read-only, which neither writes to the file nor
    /// takes a copy of it. That matters more here than it would elsewhere: the
    /// file being opened is one the app is about to offer to delete, on a machine
    /// where the same folder is what Windows Installer itself works from.
    /// </summary>
    private static PackageIdentity? ReadProduct(string filePath, out string detail, out PackageReadRefusal refusal)
    {
        refusal = PackageReadRefusal.WouldNotRead;
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
                if (error == MsiError.NoMoreItems)
                {
                    detail = "package declares no ProductCode";
                    refusal = PackageReadRefusal.DeclaresNoCode;
                    return null;
                }

                detail = $"Property table row would not fetch ({error})";
                return null;
            }

            if (!TryRecordString(hRecord, field: 1, out var raw))
            {
                detail = "ProductCode value would not read";
                return null;
            }

            if (raw.Length == 0)
            {
                detail = "ProductCode is empty";
                refusal = PackageReadRefusal.DeclaresNoCode;
                return null;
            }

            var code = Canonicalise(raw);
            if (code is null)
            {
                detail = "ProductCode is not a well-formed GUID";
                refusal = PackageReadRefusal.DeclaresNoCode;
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

    /// <inheritdoc />
    /// <remarks>
    /// The summary information is opened read-only, by path, without opening the database,
    /// and only the one property is read.
    /// </remarks>
    public string? PackageCodeOf(string filePath)
    {
        uint hSummary = 0;
        try
        {
            if (Msi.MsiGetSummaryInformation(hDatabase: 0, filePath, 0, out hSummary) != MsiError.Success) return null;

            return ReadSummaryString(hSummary, MsiSummaryProperty.RevisionNumber, out var revision) == SummaryString.Text
                ? Canonicalise(revision)
                : null;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (hSummary != 0) Msi.MsiCloseHandle(hSummary);
        }
    }

    /// <inheritdoc />
    public bool HoldsNoRevisionNumber(string filePath)
    {
        uint hSummary = 0;
        try
        {
            if (Msi.MsiGetSummaryInformation(hDatabase: 0, filePath, 0, out hSummary) != MsiError.Success) return false;

            uint length = 0;
            return Msi.MsiSummaryInfoGetProperty(
                    hSummary, MsiSummaryProperty.RevisionNumber, out var dataType, out _, out _, null, ref length)
                == MsiError.Success
                && dataType == VtType.Empty;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (hSummary != 0) Msi.MsiCloseHandle(hSummary);
        }
    }

    /// <summary>
    /// A patch's own code and the products it declares it may be applied to, both
    /// out of its summary-information stream.
    ///
    /// The patch code alone is not enough for what is asked about a patch.
    /// <c>MsiGetPatchInfoEx</c> takes a product code as a required parameter, so the
    /// keyed patch read, which reaches a registration the machine-wide patch
    /// enumeration does not list, can only be put through a product that might hold
    /// the patch, and the Template is where the file says which products those are. A
    /// patch that names none is one that read cannot be put for, so it answers null
    /// rather than an identity whose questions cannot all be put.
    /// </summary>
    private static PackageIdentity? ReadPatch(string filePath, out string detail, out PackageReadRefusal refusal)
    {
        refusal = PackageReadRefusal.WouldNotRead;
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

            switch (ReadSummaryString(hSummary, MsiSummaryProperty.RevisionNumber, out var revision))
            {
                case SummaryString.WouldNotRead:
                    detail = "patch revision number would not read";
                    return null;
                case SummaryString.NotText:
                    detail = "patch declares no revision number";
                    refusal = PackageReadRefusal.DeclaresNoCode;
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
                refusal = PackageReadRefusal.DeclaresNoCode;
                return null;
            }

            var code = Canonicalise(revision[..BracedGuidLength]);
            if (code is null)
            {
                detail = "patch code is not a well-formed GUID";
                refusal = PackageReadRefusal.DeclaresNoCode;
                return null;
            }

            switch (ReadSummaryString(hSummary, MsiSummaryProperty.Template, out var template))
            {
                case SummaryString.WouldNotRead:
                    detail = "patch target list would not read";
                    return null;
                case SummaryString.NotText:
                    detail = "patch declares no target products";
                    refusal = PackageReadRefusal.DeclaresNoCode;
                    return null;
            }

            var targets = ParseTargets(template);
            if (targets is null)
            {
                detail = "patch target list is not a list of GUIDs";
                refusal = PackageReadRefusal.DeclaresNoCode;
                return null;
            }

            if (targets.Count == 0)
            {
                // An empty Template, or one of separators alone, names no product
                // to put the keyed patch read through.
                detail = "patch names no target product";
                refusal = PackageReadRefusal.DeclaresNoCode;
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
    /// False where the value would not read in full. An empty field reads as an
    /// empty string: Windows Installer holds an empty string and a null field
    /// alike, so neither is a failed read.
    /// </summary>
    private static bool TryRecordString(uint hRecord, uint field, out string value)
    {
        value = string.Empty;
        uint bufferLen = 0;

        var error = Msi.MsiRecordGetString(hRecord, field, null, ref bufferLen);
        if (error != MsiError.Success && error != MsiError.MoreData) return false;
        if (bufferLen == 0) return true;

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
    /// What one summary-stream property gives as text. A property that is not set,
    /// or is stored as something other than text, is <see cref="SummaryString.NotText"/>:
    /// the stream read and holds no text there. An empty string is
    /// <see cref="SummaryString.Text"/> with an empty <paramref name="value"/>.
    ///
    /// A real receiver is passed for the FILETIME out-parameter rather than null,
    /// for the reason the P/Invoke declaration gives: the API writes through that
    /// pointer whenever the stored type is VT_FILETIME, which a malformed file can
    /// declare in a slot being read for text.
    /// </summary>
    private static SummaryString ReadSummaryString(uint hSummary, uint propertyId, out string value)
    {
        value = string.Empty;
        uint bufferLen = 0;

        var error = Msi.MsiSummaryInfoGetProperty(
            hSummary, propertyId, out var dataType, out _, out _, null, ref bufferLen);
        if (error != MsiError.Success && error != MsiError.MoreData) return SummaryString.WouldNotRead;
        if (dataType != VtType.String) return SummaryString.NotText;
        if (bufferLen == 0) return SummaryString.Text;

        bufferLen++; // space for the null terminator
        var buffer = new char[bufferLen];
        error = Msi.MsiSummaryInfoGetProperty(
            hSummary, propertyId, out dataType, out _, out _, buffer, ref bufferLen);
        if (error != MsiError.Success) return SummaryString.WouldNotRead;

        value = new string(buffer, 0, (int)Math.Min(bufferLen, (uint)buffer.Length));
        return SummaryString.Text;
    }

    /// <summary>What <see cref="ReadSummaryString"/> gave.</summary>
    private enum SummaryString
    {
        /// <summary>Text, possibly empty.</summary>
        Text,

        /// <summary>The property is not set, or is stored as something other than text.</summary>
        NotText,

        /// <summary>The property would not read.</summary>
        WouldNotRead,
    }
}
