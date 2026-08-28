using InstallerClean.Interop;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Scriptable fake over <see cref="IMsiApi"/>. Reproduces the double-call
/// buffer contract of msi.dll: a sizing call with a null buffer returns the
/// char count (excluding the terminator) and MoreData; the second call
/// writes the value and returns Success.
///
/// IT SITS IN ITS OWN FILE BECAUSE MORE THAN ONE TEST CLASS NEEDS IT. It was a
/// private nested class for as long as only the query service's own tests drove
/// it, and the first test to join the enumeration to the scan that reads its
/// census could not reach it. Nothing about the fake changed in the move; it is
/// internal to the test assembly and nothing outside it can see it.
/// </summary>
internal sealed class FakeMsiApi : IMsiApi
{
    // The msi.dll return codes this fake speaks. Held here rather than shared with
    // the test class that used to own it: the fake is what gives them meaning, and
    // a file that never touches this API should not have to reach through it for a
    // number.
    private const uint Success = 0, AccessDenied = 5, MoreData = 234, NoMoreItems = 259;
    private const uint UnknownProperty = 1608, BadConfiguration = 1610;
    private const uint InvalidParameter = 87;

    public List<(string Code, uint Result)> Products { get; } = new();
    public bool NeverEndProducts { get; set; }
    public string? NeverEndPatchesFor { get; set; }
    public Dictionary<string, List<string>> PatchCodes { get; } = new();
    public Dictionary<string, uint> PatchEnumResult { get; } = new();

    /// <summary>
    /// ROUTE A: what the machine-wide patch enumeration returns. That call is the one made
    /// with NO product code, and until these three members existed no fixture could reach
    /// it: every other knob here is keyed by a product code, route A passes none, and the
    /// call fell through to the "this product holds no patches" branch and answered
    /// NoMoreItems whatever the fixture said. So every test in this suite ran against a
    /// route A that succeeded and named nothing, and the production behaviour that depends
    /// on it could not be pinned in either direction.
    ///
    /// A forced return code. Null leaves route A walking <see cref="RouteAHolders"/>, which
    /// is empty unless a fixture fills it, so the default is exactly what the fake answered
    /// before and no existing fixture moves.
    /// </summary>
    public uint? RouteAResult { get; set; }

    /// <summary>
    /// The (patch, target product) pairs route A names, in enumeration order. Route A is
    /// the only call that can name a product the PRODUCT enumeration never returned, so
    /// this is the only way a fixture can build the product the rest of the machine is
    /// blind to. Both GUID buffers are written, because the production code refuses a row
    /// that names one and not the other.
    /// </summary>
    public List<(string PatchCode, string TargetProductCode)> RouteAHolders { get; } = new();

    /// <summary>
    /// Product codes a KEYED product query answers ERROR_UNKNOWN_PRODUCT for. Without it
    /// every code this fake is asked about resolves as installed, because the product
    /// enumeration ignores the code it is given and answers from its own list. That is a
    /// machine that cannot exist, and it hides the branch that matters most to the
    /// per-product condition: a product a patch file DECLARES that this machine does not
    /// hold, which is a positive answer of "nothing here can reach the file" rather than a
    /// failure to establish one.
    /// </summary>
    public HashSet<string> NotInstalled { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The instances a KEYED product query enumerates for one product code, in
    /// enumeration order. One code can name more than one installation, per machine and
    /// per user at once or under two accounts, and each is its own row with its own
    /// account and context, so this is how a fixture builds the machine that has two.
    ///
    /// A code with no entry here is one ordinary per-machine instance, which is what
    /// every fixture that says nothing about instances is describing.
    /// </summary>
    public Dictionary<string, List<(string? Sid, MsiInstallContext Context)>> KeyedInstances { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// What one ROW of a keyed enumeration returns, keyed by (product, index). It wins
    /// over everything else, which is what builds a walk that reads an instance and then
    /// meets a return it cannot read.
    /// </summary>
    public Dictionary<(string ProductCode, uint Index), uint> KeyedRowResult { get; } = new();

    /// <summary>
    /// Fails ONE row of a product's patch enumeration, keyed by (product,
    /// index). Distinct from <see cref="PatchEnumResult"/>, which refuses the
    /// product's enumeration outright: this is the scattered-failure case the
    /// loop tolerates, where the rows either side of the bad one come back.
    /// </summary>
    public Dictionary<(string ProductCode, uint Index), uint> PatchRowResult { get; } = new();

    /// <summary>
    /// Forces a return code out of a property read, keyed by (product,
    /// property) and (patch, product, property). Needed because
    /// <see cref="DoubleCall"/> models an unset property as a readable empty
    /// value, which cannot express the distinction the LocalPackage reads
    /// turn on: a record that has no cached package and a record that could
    /// not be read both arrive as "" without it, and no test could reach the
    /// branch that tells them apart.
    /// </summary>
    /// <summary>
    /// Scripts the SID-buffer retry for one product row, keyed by index: the
    /// first EnumProducts call at that index reports MoreData, and the retry
    /// returns the value given here (Success meaning the row then comes back
    /// normally). The real API only asks for a bigger buffer for a SID past
    /// 256 characters, so nothing else in this fake can reach the retry, and
    /// what the retry RETURNS is the whole subject of the tests using it.
    /// </summary>
    public Dictionary<uint, uint> ProductSidRetryResult { get; } = new();

    /// <summary>
    /// The sidLength the retry call arrived with, so a test can pin the
    /// buffer contract: the sizing call reports 64 (excluding the null),
    /// so a correctly sized retry arrives with 65.
    /// </summary>
    public uint? RetrySidLengthSeen { get; private set; }

    private readonly HashSet<uint> _sidRetried = new();

    private static readonly List<(string? Sid, MsiInstallContext Context)> OneMachineInstance =
        new() { (null, MsiInstallContext.Machine) };

    public Dictionary<(string ProductCode, string Property), uint> ProductPropertyResult { get; } = new();
    public Dictionary<(string PatchCode, string ProductCode, string Property), uint> PatchPropertyResult { get; } = new();

    private readonly Dictionary<(string, string), string> _productProps = new();
    private readonly Dictionary<(string, string, string), string> _patchProps = new();

    public void AddProduct(string code, uint result = Success) => Products.Add((code, result));

    public void SetProductProperty(string code, string property, string value) =>
        _productProps[(code, property)] = value;

    public void SetPatchProperty(string patchCode, string productCode, string property, string value) =>
        _patchProps[(patchCode, productCode, property)] = value;

    public void AddPatch(string productCode, string patchCode, string localPackage, string state, string? uninstallable)
    {
        (PatchCodes.TryGetValue(productCode, out var list) ? list : PatchCodes[productCode] = new()).Add(patchCode);
        SetPatchProperty(patchCode, productCode, "LocalPackage", localPackage);
        SetPatchProperty(patchCode, productCode, "State", state);
        if (uninstallable is not null)
            SetPatchProperty(patchCode, productCode, "Uninstallable", uninstallable);
    }

    private static void WriteCode(char[]? buffer, string code)
    {
        if (buffer is null) return;
        for (int i = 0; i < code.Length && i < buffer.Length - 1; i++) buffer[i] = code[i];
        // The caller zeroes the buffer each iteration, so an empty code
        // leaves an all-null buffer that reads back as "".
    }

    public uint EnumProducts(string? productCode, string? userSid, MsiInstallContext context, uint index,
        char[]? installedProductCode, out MsiInstallContext installedContext, char[]? sid, ref uint sidLength)
    {
        installedContext = MsiInstallContext.Machine;

        // A KEYED QUERY IS ANSWERED IN FULL HERE, BEFORE THE MACHINE-WIDE LIST IS
        // CONSULTED, because it is a question about one code rather than a walk:
        // falling through to that list would answer from whichever product happened to
        // sit at the index. NotInstalled uses the production constant rather than a
        // local copy, so the fake cannot drift from the allowlist that decides which
        // returns may be read as absence.
        if (productCode is not null)
        {
            if (KeyedRowResult.TryGetValue((productCode, index), out var forcedRow)) return forcedRow;
            if (NotInstalled.Contains(productCode)) return MsiError.UnknownProduct;

            var instances = KeyedInstances.TryGetValue(productCode, out var scripted)
                ? scripted
                : OneMachineInstance;
            if (index >= instances.Count) return NoMoreItems;

            var (keyedSid, keyedContext) = instances[(int)index];
            WriteCode(installedProductCode, productCode);
            installedContext = keyedContext;
            if (keyedSid is not null && sid is not null)
            {
                for (var i = 0; i < keyedSid.Length && i < sid.Length; i++) sid[i] = keyedSid[i];
                sidLength = (uint)keyedSid.Length;
            }
            return Success;
        }

        if (NeverEndProducts)
        {
            WriteCode(installedProductCode, "{FFFFFFFF-0000-0000-0000-000000000000}");
            return Success;
        }
        if (index >= Products.Count) return NoMoreItems;
        if (ProductSidRetryResult.TryGetValue(index, out var afterRetry))
        {
            // MoreData reports the required SID length EXCLUDING the
            // terminator, as msi.dll documents for pcchSid, so the
            // correct retry buffer is one larger than the report.
            if (_sidRetried.Add(index)) { sidLength = 64; return MoreData; }
            RetrySidLengthSeen = sidLength;
            if (afterRetry != Success) return afterRetry;
        }
        var (code, result) = Products[(int)index];
        if (result != Success) return result;
        WriteCode(installedProductCode, code);
        return Success;
    }

    public uint EnumPatches(string? productCode, string? userSid, MsiInstallContext context, MsiPatchFilter filter,
        uint index, char[]? patchCode, char[]? targetProductCode, out MsiInstallContext targetProductContext,
        char[]? targetUserSid, ref uint targetUserSidLength)
    {
        targetProductContext = MsiInstallContext.Machine;
        // ROUTE A, WHICH IS THE CALL WITH NO PRODUCT CODE. Everything below this branch is
        // keyed by a product, so route A used to reach the "no patches for this product"
        // return and answer NoMoreItems on every fixture. With both members left alone it
        // still does, which is why nothing already written moves.
        if (productCode is null)
        {
            if (RouteAResult is { } forcedRouteA) return forcedRouteA;
            if (index >= RouteAHolders.Count) return NoMoreItems;
            var (heldCode, heldTarget) = RouteAHolders[(int)index];
            WriteCode(patchCode, heldCode);
            WriteCode(targetProductCode, heldTarget);
            return Success;
        }
        if (productCode is not null && productCode == NeverEndPatchesFor)
        {
            WriteCode(patchCode, "{FFFFFFFF-0000-0000-0000-000000000001}");
            return Success;
        }
        if (productCode is not null && PatchEnumResult.TryGetValue(productCode, out var err))
            return err;
        var list = (productCode is not null && PatchCodes.TryGetValue(productCode, out var l)) ? l : null;
        if (list is null || index >= list.Count) return NoMoreItems;
        if (productCode is not null && PatchRowResult.TryGetValue((productCode, index), out var rowErr))
            return rowErr;
        WriteCode(patchCode, list[(int)index]);
        return Success;
    }

    public uint GetProductInfo(string productCode, string? userSid, MsiInstallContext context, string property,
        char[]? value, ref uint valueLength)
    {
        // A forced code answers the sizing call, so the real API's second
        // call never happens either: nothing survives a failed first call.
        if (ProductPropertyResult.TryGetValue((productCode, property), out var forced))
        {
            valueLength = 0;
            return forced;
        }
        return DoubleCall(_productProps.GetValueOrDefault((productCode, property), ""), value, ref valueLength);
    }

    public uint GetPatchInfo(string patchCode, string productCode, string? userSid, MsiInstallContext context,
        string property, char[]? value, ref uint valueLength)
    {
        if (PatchPropertyResult.TryGetValue((patchCode, productCode, property), out var forced))
        {
            valueLength = 0;
            return forced;
        }

        // A PAIRING THIS FIXTURE NEVER SET UP ANSWERS ERROR_UNKNOWN_PATCH, and
        // the distinction is between the PAIRING and the PROPERTY. This fell
        // through to DoubleCall("") for both, so a product asked about a patch it
        // does not hold answered "present but empty" instead of "no such
        // registration". 1608 is on IsBenignPropertyRead's allowlist and 1647 is
        // on IsRecordAbsent's, and the confirmation pass reads the two in
        // opposite directions: absence says nothing either way and moves on,
        // while a benign empty read is an answer, fails IsRemovablePatch and
        // downgrades a row that should have survived. Every cross-product ask
        // this fake served was therefore answering the wrong question, and the
        // fake beside it in the truncation tests already gets this right.
        //
        // An unset property on a pairing that DOES exist still reads as a
        // readable empty value, which is deliberate and is what lets a test
        // reach the branch separating a record with no cached package from one
        // that would not read.
        var pairingExists =
            (PatchCodes.TryGetValue(productCode, out var held) && held.Contains(patchCode))
            || _patchProps.ContainsKey((patchCode, productCode, "LocalPackage"))
            || _patchProps.ContainsKey((patchCode, productCode, "State"))
            || _patchProps.ContainsKey((patchCode, productCode, "Uninstallable"));

        if (!pairingExists)
        {
            // The production constant rather than a local copy, so the fake
            // cannot drift from the allowlist that reads it.
            valueLength = 0;
            return MsiError.UnknownPatch;
        }

        return DoubleCall(_patchProps.GetValueOrDefault((patchCode, productCode, property), ""), value, ref valueLength);
    }

    private static uint DoubleCall(string val, char[]? value, ref uint valueLength)
    {
        if (val.Length == 0) { valueLength = 0; return Success; } // readable but empty
        if (value is null) { valueLength = (uint)val.Length; return MoreData; }
        int n = Math.Min(val.Length, value.Length);
        for (int i = 0; i < n; i++) value[i] = val[i];
        valueLength = (uint)val.Length;
        return Success;
    }
}
