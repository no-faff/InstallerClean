using InstallerClean.Interop;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The four lists the scan walks, for <see cref="FakeMsiApi.PassingError"/> and
/// <see cref="FakeMsiApi.WalksStarted"/>. Public so a theory can take one as a parameter.
/// </summary>
public enum MsiWalk { Products, RouteA, Patches, Instances }

/// <summary>
/// Scriptable fake over <see cref="IMsiApi"/>. Reproduces the double-call
/// buffer contract of msi.dll: a sizing call with a null buffer returns the
/// char count (excluding the terminator) and MoreData; the second call
/// writes the value and returns Success.
///
/// IT SITS IN ITS OWN FILE BECAUSE MORE THAN ONE TEST CLASS NEEDS IT, among them
/// the tests joining the enumeration to the scan that reads its census. It is
/// internal to the test assembly and nothing outside it can see it.
/// </summary>
internal sealed class FakeMsiApi : IMsiApi
{
    // The msi.dll return codes this fake speaks. Held here rather than shared with
    // a test class: the fake is what gives them meaning, and a file that never
    // touches this API should not have to reach through it for a number.
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
    /// with NO product code, and every other knob here is keyed by a product code, so
    /// these three members are the only way a fixture reaches it.
    ///
    /// A forced return code. Null leaves route A walking <see cref="RouteAHolders"/>, which
    /// is empty unless a fixture fills it, so a fixture that says nothing about route A
    /// gets one that succeeds and names nothing.
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
    /// The account and context the machine-wide product walk lists one product code
    /// under. A code with no entry here is listed per machine with no account. A fixture
    /// setting one sets the same installation in <see cref="KeyedInstances"/>, so the
    /// keyed query answers for the installation the walk listed.
    /// </summary>
    public Dictionary<string, (string Sid, MsiInstallContext Context)> WalkInstances { get; } =
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
    /// Fails every row of a product's patch enumeration from the given index on, past the
    /// end of its list as well. Rows before it come back as usual, so this builds a list
    /// that returns some patches and is then abandoned, which <see cref="PatchRowResult"/>
    /// cannot: it fails only rows the list holds.
    /// </summary>
    public Dictionary<string, uint> PatchRowsFailFrom { get; } = new();

    /// <summary>
    /// Every product whose own patch enumeration was started, once per start, so a test
    /// can show a list was asked for when its answer was a refusal that left nothing else
    /// behind.
    /// </summary>
    public List<string> PatchEnumerationsStarted { get; } = new();

    /// <summary>
    /// Scripts the SID-buffer retry for one product row, keyed by index: the
    /// first EnumProducts call at that index reports MoreData, and the retry
    /// returns the value given here (Success meaning the row then comes back
    /// normally, MoreData the row whose product key name no SID buffer can
    /// help). Nothing else in this fake can reach the retry, and what the retry
    /// RETURNS is the whole subject of the tests using it.
    /// </summary>
    public Dictionary<uint, uint> ProductSidRetryResult { get; } = new();

    /// <summary>
    /// The sidLength the retry call arrived with, so a test can pin the
    /// buffer contract: the sizing call reports 64 (excluding the null),
    /// so a correctly sized retry arrives with 65.
    /// </summary>
    public uint? RetrySidLengthSeen { get; private set; }

    private readonly HashSet<uint> _sidRetried = new();

    private readonly Dictionary<(MsiWalk Walk, string? ProductCode, uint Index), (uint Error, int Calls)> _passing = new();
    private readonly Dictionary<(MsiWalk Walk, string? ProductCode), int> _walksStarted = new();

    /// <summary>
    /// An error that passes: the next <paramref name="calls"/> calls for this row of the
    /// list answer <paramref name="error"/>, and every call after them answers as the
    /// fixture scripts the row. <paramref name="productCode"/> is null for the two
    /// machine-wide walks.
    /// </summary>
    public void PassingError(MsiWalk walk, string? productCode, uint index, uint error, int calls = 1) =>
        _passing[(walk, productCode, index)] = (error, calls);

    /// <summary>
    /// How many times a list was asked for its first row: once for each walk of it, and
    /// once more where a first row is asked again with a larger SID buffer.
    /// <paramref name="productCode"/> is null for the two machine-wide walks.
    /// </summary>
    public int WalksStarted(MsiWalk walk, string? productCode = null) =>
        _walksStarted.TryGetValue((walk, productCode), out var started) ? started : 0;

    private bool PassingErrorAt(MsiWalk walk, string? productCode, uint index, out uint error)
    {
        if (index == 0)
            _walksStarted[(walk, productCode)] = WalksStarted(walk, productCode) + 1;

        error = Success;
        if (!_passing.TryGetValue((walk, productCode, index), out var passing) || passing.Calls <= 0) return false;

        _passing[(walk, productCode, index)] = (passing.Error, passing.Calls - 1);
        error = passing.Error;
        return true;
    }

    // Keyed by (patch code or null, product code, property, whether the value call).
    private readonly Dictionary<(string? PatchCode, string ProductCode, string Property, bool ValueCall), (uint Error, int Calls)>
        _passingReads = new();

    private readonly Dictionary<(string? PatchCode, string ProductCode, string Property), int> _reads = new();

    private readonly Dictionary<(string? PatchCode, string ProductCode, string Property), Queue<string>> _passingValues = new();
    private readonly Dictionary<(string? PatchCode, string ProductCode, string Property), string> _valueBeingRead = new();

    /// <summary>
    /// Values that pass: the next reads of <paramref name="property"/> that no passing error
    /// answers give <paramref name="values"/>, one a read in order, and every read after them
    /// answers as the fixture scripts the property. <paramref name="patchCode"/> is null for
    /// a product's own property.
    /// </summary>
    public void PassingReadValues(string? patchCode, string productCode, string property, params string[] values) =>
        _passingValues[(patchCode, productCode, property)] = new Queue<string>(values);

    // The value a passing value gives this read: taken from the queue at the sizing call and
    // kept for the value call after it.
    private bool PassingValueAt(string? patchCode, string productCode, string property, char[]? value, out string passing)
    {
        var key = (patchCode, productCode, property);
        if (value is null)
        {
            _valueBeingRead.Remove(key);
            if (_passingValues.TryGetValue(key, out var queue) && queue.Count > 0)
                _valueBeingRead[key] = queue.Dequeue();
        }

        return _valueBeingRead.TryGetValue(key, out passing!);
    }

    /// <summary>
    /// An error that passes, on a property read: the next <paramref name="calls"/> reads of
    /// <paramref name="property"/> answer <paramref name="error"/>, and every read after
    /// them answers as the fixture scripts the property. <paramref name="patchCode"/> is
    /// null for a product's own property. Where <paramref name="onValueCall"/> is true the
    /// error answers the call that writes the value, after a sizing call that answered
    /// with the value's length.
    /// </summary>
    public void PassingReadError(string? patchCode, string productCode, string property, uint error,
        int calls = 1, bool onValueCall = false) =>
        _passingReads[(patchCode, productCode, property, onValueCall)] = (error, calls);

    /// <summary>
    /// How many times <paramref name="property"/> was read: one for each sizing call, which
    /// every read makes once, whether or not a value call follows it.
    /// <paramref name="patchCode"/> is null for a product's own property.
    /// </summary>
    public int Reads(string? patchCode, string productCode, string property) =>
        _reads.TryGetValue((patchCode, productCode, property), out var reads) ? reads : 0;

    private bool PassingReadErrorAt(string? patchCode, string productCode, string property, char[]? value, out uint error)
    {
        var valueCall = value is not null;
        if (!valueCall) _reads[(patchCode, productCode, property)] = Reads(patchCode, productCode, property) + 1;

        error = Success;
        var key = (patchCode, productCode, property, valueCall);
        if (!_passingReads.TryGetValue(key, out var passing) || passing.Calls <= 0) return false;

        _passingReads[key] = (passing.Error, passing.Calls - 1);
        error = passing.Error;
        return true;
    }

    private static readonly List<(string? Sid, MsiInstallContext Context)> OneMachineInstance =
        new() { (null, MsiInstallContext.Machine) };

    /// <summary>
    /// Forces a return code out of a property read, keyed by (product,
    /// property) and (patch, product, property). Needed because
    /// <see cref="DoubleCall"/> models an unset property as a readable empty
    /// value, which cannot express the distinction the LocalPackage reads
    /// turn on: a record that has no cached package and a record that could
    /// not be read both arrive as "" without it, and no test could reach the
    /// branch that tells them apart.
    /// </summary>
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
            if (PassingErrorAt(MsiWalk.Instances, productCode, index, out var passingInstance)) return passingInstance;
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

        if (PassingErrorAt(MsiWalk.Products, null, index, out var passingProduct)) return passingProduct;
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
        if (WalkInstances.TryGetValue(code, out var walked))
        {
            installedContext = walked.Context;
            if (sid is not null)
            {
                for (var i = 0; i < walked.Sid.Length && i < sid.Length; i++) sid[i] = walked.Sid[i];
                sidLength = (uint)walked.Sid.Length;
            }
        }
        return Success;
    }

    public uint EnumPatches(string? productCode, string? userSid, MsiInstallContext context, MsiPatchFilter filter,
        uint index, char[]? patchCode, char[]? targetProductCode, out MsiInstallContext targetProductContext,
        char[]? targetUserSid, ref uint targetUserSidLength)
    {
        targetProductContext = MsiInstallContext.Machine;
        // ROUTE A, WHICH IS THE CALL WITH NO PRODUCT CODE. Everything below this branch is
        // keyed by a product, so route A is answered here. With both members left alone it
        // answers NoMoreItems at once: a route A that succeeds and names nothing.
        if (productCode is null)
        {
            if (PassingErrorAt(MsiWalk.RouteA, null, index, out var passingHolder)) return passingHolder;
            if (RouteAResult is { } forcedRouteA) return forcedRouteA;
            if (index >= RouteAHolders.Count) return NoMoreItems;
            var (heldCode, heldTarget) = RouteAHolders[(int)index];
            WriteCode(patchCode, heldCode);
            WriteCode(targetProductCode, heldTarget);
            return Success;
        }
        // A patch of a product asked about in one context is that product's, in that
        // context, which is what every row below answers.
        targetProductContext = context;
        if (productCode is not null && index == 0) PatchEnumerationsStarted.Add(productCode);
        if (PassingErrorAt(MsiWalk.Patches, productCode, index, out var passingPatch)) return passingPatch;
        if (productCode is not null && productCode == NeverEndPatchesFor)
        {
            WriteCode(patchCode, "{FFFFFFFF-0000-0000-0000-000000000001}");
            return Success;
        }
        if (productCode is not null && PatchEnumResult.TryGetValue(productCode, out var err))
            return err;
        if (productCode is not null && PatchRowsFailFrom.TryGetValue(productCode, out var failFrom) && index >= failFrom)
            return BadConfiguration;
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
        if (PassingReadErrorAt(null, productCode, property, value, out var passingRead))
        {
            valueLength = 0;
            return passingRead;
        }

        if (PassingValueAt(null, productCode, property, value, out var passingValue))
            return DoubleCall(passingValue, value, ref valueLength);

        // A forced code answers the sizing call, so the real API's second
        // call never happens either: nothing survives a failed first call.
        if (ProductPropertyResult.TryGetValue((productCode, property), out var forced))
        {
            valueLength = 0;
            return forced;
        }
        return DoubleCall(_productProps.GetValueOrDefault((productCode, property), ""), value, ref valueLength);
    }

    public uint EnumSources(string productCode, string? userSid, MsiInstallContext context, uint options,
        uint index, char[]? source, ref uint sourceLength) =>
        throw new InvalidOperationException("the query service reads no source lists");

    public uint EnumMediaDisks(string productCodeOrPatchCode, string? userSid, MsiInstallContext context,
        uint options, uint index, out uint diskId) =>
        throw new InvalidOperationException("the query service reads no source lists");

    public uint GetSourceListInfo(string productCodeOrPatchCode, string? userSid, MsiInstallContext context,
        uint options, string property, char[]? value, ref uint valueLength) =>
        throw new InvalidOperationException("the query service reads no source lists");

    /// <summary>
    /// Every patch property read put to this fake, with the account and context it was
    /// put in. The answers below do not depend on the account, so this is how a test
    /// shows WHICH installation of a product was asked.
    /// </summary>
    public List<(string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context, string Property)>
        PatchInfoReads { get; } = new();

    public uint GetPatchInfo(string patchCode, string productCode, string? userSid, MsiInstallContext context,
        string property, char[]? value, ref uint valueLength)
    {
        PatchInfoReads.Add((patchCode, productCode, userSid, context, property));

        if (PassingReadErrorAt(patchCode, productCode, property, value, out var passingRead))
        {
            valueLength = 0;
            return passingRead;
        }

        if (PassingValueAt(patchCode, productCode, property, value, out var passingValue))
            return DoubleCall(passingValue, value, ref valueLength);

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
